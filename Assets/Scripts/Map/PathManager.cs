using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(LineRenderer))]
public class PathManager : MonoBehaviour
{
    [Header("Drawing Mode Settings")]
    [Tooltip("체크하면 파티클 자취 모드, 체크 해제하면 라인 렌더러(선) 모드로 작동합니다.")]
    public bool useParticleMode = false;

    [Tooltip("파티클 모드일 때 사용할 파티클 시스템 (플레이어 자식으로 배치)")]
    public ParticleSystem drawingParticles;
    public bool clearParticlesOnStart = true;

    [Header("Camera Settings")]
    public CinemachineCamera mapCam;
    public CinemachineBrain brain;
    public GameObject skyboxCam;
    public Camera mainCam;
    public float mapCamZpos = -6000f;

    [Header("Line Settings")]
    public float lineWidth = 0.1f;

    [Tooltip("선이 꺾이는 지점의 둥글기. 값이 클수록 정점 수가 폭증하고, 끝점이 매 프레임 갱신되므로 메시 전체가 매 프레임 재생성된다. 2~4 권장.")]
    [Range(0, 16)]
    public int cornerVertices = 4;

    [Tooltip("선 끝단의 둥글기. 2~4 권장.")]
    [Range(0, 16)]
    public int capVertices = 4;

    [Header("Node Settings")]
    public Vector3 originalNodeScale;
    public float targetNodeScaleMultiplier = 2.4f;
    public float nonTargetNodeScaleMultiplier = 0.7f;

    [Tooltip("활성화(타겟) 상태일 때 노드를 대체할 프리팹")]
    public GameObject activeNodePrefab;
    public Vector3 activeNodeRotation;

    [Header("Node LookAt Offset")]
    [Tooltip("플레이어를 바라볼 때 LookAt 후 추가로 적용할 회전 오프셋 (텍스처 정방향 보정용)")]
    public Vector3 nodeRotationOffset;

    [Header("Materials")]
    public Material defaultMaterial;
    public Material activeMaterial;
    public Material nonTargetActiveMaterial;
    public Material completedMaterial;
    public Material lineMaterial;

    [Header("Node Reduction")]
    [Tooltip("몇 칸마다 하나씩만 '실제로 닿아야 하는 노드'로 쓸지. 1=전부, 2=절반, 3=1/3. 건너뛴 노드는 숨겨지지만 선의 꼭짓점으로는 그대로 사용되어 완성 모양은 동일하게 유지됩니다.")]
    [Min(1)]
    public int nodeStep = 2;

    [Header("Path Nodes")]
    public List<PathNode> pathNodes = new List<PathNode>();
    public PathNode finalNode;
    public Vector3 finalPosition;

    public EmotionState completedEmotion;

    [Header("Debug")]
    public CinemachineCamera happyMapCam;
    public CinemachineCamera hopeMapCam;
    public CinemachineCamera angryMapCam;
    public CinemachineCamera sadMapCam;
    public Debug_ShapeActiveManager activeManager;

    private bool isTargetPath;
    private int currentIndex = 0;
    private LineRenderer lineRenderer;

    // 실제로 플레이어가 닿아야 하는 노드(체크포인트)의 pathNodes 인덱스 목록
    private readonly List<int> checkpointIndices = new List<int>();
    private readonly HashSet<int> checkpointSet = new HashSet<int>();
    private int checkpointCursor = 0;
    // LineRenderer에 이미 확정 반영된 마지막 pathNodes 인덱스
    private int lastCommittedIndex = -1;
    private bool isFinished = false;
    private bool isLineStarted = false;
    private bool isWaitingForFinal = false;

    private Dictionary<PathNode, GameObject> activeNodeInstances = new Dictionary<PathNode, GameObject>();

    [Header("Animation Settings")]
    [Tooltip("PathNode의 Animator에 설정된 Trigger 파라미터 이름")]
    public string activeAnimTrigger = "Targetted";

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 0;
        lineRenderer.material = lineMaterial;
        lineRenderer.numCornerVertices = cornerVertices;
        lineRenderer.numCapVertices = capVertices;

        isTargetPath = completedEmotion == DataManager.Instance.targetEmotion;
        lineRenderer.enabled = isTargetPath && !useParticleMode;

        if (pathNodes.Count <= 0)
        {
            foreach (PathNode node in GetComponentsInChildren<PathNode>())
                pathNodes.Add(node);
        }

        if (isTargetPath)
        {
            DataManager.Instance.targetMapCam = mapCam;
            DataManager.Instance.targetPathManager = this;
            // maxEmotionScore는 체크포인트 개수가 확정되는 InitializePath에서 설정
        }

        if (pathNodes.Count > 0)
        {
            InitializePath();

            if (isTargetPath)
                StartCoroutine(FaceFirstNodeOnStart());
        }
    }

    /// <summary>
    /// 지금 플레이어가 닿아야 하는 노드의 Transform. 없으면 null.
    /// (UI 지시 화살표 등 외부에서 참조)
    /// </summary>
    public Transform CurrentTargetNode
    {
        get
        {
            if (isFinished) return null;

            if (isWaitingForFinal)
                return finalNode != null ? finalNode.transform : null;

            if (currentIndex >= 0 && currentIndex < pathNodes.Count)
                return pathNodes[currentIndex].transform;

            return null;
        }
    }

    IEnumerator FaceFirstNodeOnStart()
    {
        yield return null;

        if (DataManager.Instance.mouseLook == null || pathNodes.Count == 0) yield break;

        Vector3 nodePos   = pathNodes[0].transform.position;
        Vector3 playerPos = DataManager.Instance.playerTransform.position;
        Vector3 pivotPos  = DataManager.Instance.mouseLook.transform.position;

        Vector3 horizontal = nodePos - playerPos;
        float yaw = Mathf.Atan2(horizontal.x, horizontal.z) * Mathf.Rad2Deg;

        Vector3 fromPivot = nodePos - pivotPos;
        float horizontalDist = new Vector2(fromPivot.x, fromPivot.z).magnitude;
        float pitch = -Mathf.Atan2(fromPivot.y, horizontalDist) * Mathf.Rad2Deg;
        pitch -= DataManager.Instance.mouseLook.lookAngleOffset;

        DataManager.Instance.mouseLook.SetRotation(yaw, pitch);
    }

    public void InitializePath()
    {
        currentIndex = 0;
        checkpointCursor = 0;
        lastCommittedIndex = -1;
        isFinished = false;
        isLineStarted = false;
        isWaitingForFinal = false;

        foreach (var kvp in activeNodeInstances)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        activeNodeInstances.Clear();

        if (useParticleMode)
        {
            lineRenderer.enabled = false;
            if (drawingParticles != null)
            {
                var main = drawingParticles.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = Mathf.Infinity;
                main.startSpeed = 0;

                var emission = drawingParticles.emission;
                emission.enabled = false;

                if (clearParticlesOnStart) drawingParticles.Clear();
            }
        }
        else
        {
            lineRenderer.enabled = isTargetPath;
            lineRenderer.positionCount = 0;
        }

        // Final Node 중복 제거
        if (finalNode != null && pathNodes.Count > 0 && pathNodes[pathNodes.Count - 1] == finalNode)
            pathNodes.RemoveAt(pathNodes.Count - 1);

        if (pathNodes.Count > 0)
            originalNodeScale = pathNodes[0].transform.localScale;

        BuildCheckpoints();

        for (int i = 0; i < pathNodes.Count; i++)
        {
            pathNodes[i].manager = this;
            pathNodes[i].myIndex = i;
            pathNodes[i].emotion = completedEmotion;

            if (!IsCheckpoint(i))
            {
                // 건너뛰는 노드: 선의 꼭짓점으로만 사용되고 화면에서는 완전히 숨김
                pathNodes[i].gameObject.SetActive(false);
                continue;
            }

            pathNodes[i].gameObject.SetActive(true);
            pathNodes[i].transform.localScale = originalNodeScale * nonTargetNodeScaleMultiplier;
            pathNodes[i].NodeRenderer.enabled = true;

            // 현재 타겟이 된 노드만 UpdateNodeStates/ActivateNode에서 다시 켠다.
            // 컴포넌트가 꺼져 있으면 Update/FixedUpdate/트리거 콜백이 모두 호출되지 않는다.
            pathNodes[i].enabled = false;
        }

        currentIndex = checkpointIndices.Count > 0 ? checkpointIndices[0] : 0;

        if (isTargetPath)
            DataManager.Instance.maxEmotionScore = Mathf.Max(1, checkpointIndices.Count);

        if (finalNode != null)
        {
            finalNode.manager = this;
            finalNode.myIndex = 9999;
            finalPosition = finalNode.transform.position;
            finalNode.NodeRenderer.enabled = false;
            finalNode.NodeCollider.enabled = false;
            finalNode.enabled = false;
        }

        UpdateNodeStates();

        if (DataManager.Instance.playerCam != null && mapCam != null)
        {
            DataManager.Instance.playerCam.Priority = 10;
            mapCam.Priority = 0;
        }

        Debug.Log($"게임 초기화 완료! 노드 개수: {pathNodes.Count}, 모드: {(useParticleMode ? "파티클" : "라인")}");
    }

    void Update()
    {
        if (CheatSettings.Enabled && Input.GetKeyDown(KeyCode.M) && !isFinished && isTargetPath)
            CheatCompletePath();

        if (!useParticleMode && isLineStarted && !isFinished && DataManager.Instance.playerLineTransform != null)
        {
            int lastIndex = lineRenderer.positionCount - 1;
            if (lastIndex >= 0)
                lineRenderer.SetPosition(lastIndex, DataManager.Instance.playerLineTransform.position);
        }
    }

    // 전체 노드 상태 초기화용 (InitializePath에서만 호출)
    void UpdateNodeStates()
    {
        for (int i = 0; i < pathNodes.Count; i++)
        {
            if (!IsCheckpoint(i)) continue;

            if (i < currentIndex)
            {
                pathNodes[i].NodeRenderer.material = completedMaterial;
                pathNodes[i].transform.localScale = originalNodeScale * targetNodeScaleMultiplier;
            }
            else if (i == currentIndex && !isWaitingForFinal)
            {
                if (isTargetPath)
                {
                    ReplaceNodeWithPrefab(pathNodes[i]);
                    pathNodes[i].transform.localScale = originalNodeScale * targetNodeScaleMultiplier;
                    pathNodes[i].enabled = true;
                    pathNodes[i].SetState(true, activeMaterial, defaultMaterial);
                }
                else
                {
                    pathNodes[i].transform.localScale = originalNodeScale * targetNodeScaleMultiplier;
                    pathNodes[i].SetState(false, activeMaterial, defaultMaterial);
                    if (nonTargetActiveMaterial != null)
                        pathNodes[i].NodeRenderer.material = nonTargetActiveMaterial;
                }
            }
            else
            {
                pathNodes[i].SetState(false, activeMaterial, defaultMaterial);
                pathNodes[i].NodeCollider.isTrigger = false;
                pathNodes[i].transform.localScale = originalNodeScale * nonTargetNodeScaleMultiplier;
            }
        }

        if (isWaitingForFinal)
            ActivateFinalNode();
    }

    // 단일 노드 활성화 (OnNodeCollected에서 호출)
    private void ActivateNode(int index)
    {
        if (index < 0 || index >= pathNodes.Count) return;
        PathNode node = pathNodes[index];

        if (isTargetPath)
        {
            ReplaceNodeWithPrefab(node);
            node.transform.localScale = originalNodeScale * targetNodeScaleMultiplier;
            node.enabled = true;
            node.SetState(true, activeMaterial, defaultMaterial);
        }
        else
        {
            node.transform.localScale = originalNodeScale * targetNodeScaleMultiplier;
            node.SetState(false, activeMaterial, defaultMaterial);
            if (nonTargetActiveMaterial != null)
                node.NodeRenderer.material = nonTargetActiveMaterial;
        }
    }

    private void ActivateFinalNode()
    {
        if (finalNode == null) return;

        finalNode.gameObject.SetActive(true);
        finalNode.enabled = true;
        finalNode.NodeCollider.enabled = true;
        finalNode.NodeCollider.isTrigger = true;
        ReplaceNodeWithPrefab(finalNode);
        finalNode.transform.localScale = originalNodeScale * targetNodeScaleMultiplier;
        finalNode.SetState(true, activeMaterial, defaultMaterial);

        if (activeNodeInstances.TryGetValue(finalNode, out GameObject instance))
            PlayActiveAnimation(instance);
    }

    // 체크포인트 구성: nodeStep 간격으로 '실제로 닿아야 하는 노드'를 고른다.
    // 건너뛴 노드도 pathNodes에는 그대로 남아 선의 꼭짓점으로 쓰이므로 완성 모양은 변하지 않는다.
    private void BuildCheckpoints()
    {
        checkpointIndices.Clear();
        checkpointSet.Clear();

        if (pathNodes.Count == 0) return;

        // 타겟 경로가 아니면(배경 장식용) 감축하지 않고 기존 그대로 유지
        int step = isTargetPath ? Mathf.Max(1, nodeStep) : 1;

        for (int i = 0; i < pathNodes.Count; i += step)
        {
            checkpointIndices.Add(i);
            checkpointSet.Add(i);
        }

        Debug.Log($"[PathManager] {gameObject.name} 노드 {pathNodes.Count}개 중 {checkpointIndices.Count}개를 체크포인트로 사용 (step={step})");
    }

    private bool IsCheckpoint(int index)
    {
        return checkpointSet.Contains(index);
    }

    // 직전 확정 지점 다음부터 이번 체크포인트까지의 꼭짓점들을 모은다.
    // 건너뛴 노드의 위치가 여기에 포함되기 때문에 선의 형태가 원본과 동일하게 유지된다.
    private List<Vector3> BuildSegmentPoints(int checkpointIndex)
    {
        List<Vector3> points = new List<Vector3>();

        for (int i = lastCommittedIndex + 1; i <= checkpointIndex && i < pathNodes.Count; i++)
            points.Add(pathNodes[i].originalPosition);

        if (checkpointIndex > lastCommittedIndex)
            lastCommittedIndex = checkpointIndex;

        return points;
    }

    // 라인/파티클 업데이트 (OnNodeCollected / FinishPath에서만 호출)
    private void CommitLinePositions(List<Vector3> points)
    {
        if (points == null || points.Count == 0) return;

        if (useParticleMode)
        {
            if (!isLineStarted && drawingParticles != null)
            {
                var emission = drawingParticles.emission;
                emission.enabled = true;
                drawingParticles.Play();
            }

            isLineStarted = true;
            return;
        }

        if (!isLineStarted)
        {
            lineRenderer.positionCount = 1;
            lineRenderer.SetPosition(0, points[0]);
        }
        else
        {
            // 플레이어를 따라다니던 마지막 점을 첫 꼭짓점으로 확정
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, points[0]);
        }

        for (int i = 1; i < points.Count; i++)
        {
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, points[i]);
        }

        isLineStarted = true;
    }

    public void OnNodeCollected(PathNode node)
    {
        if (isWaitingForFinal)
        {
            if (node == finalNode)
                FinishPath();
            return;
        }

        if (node.myIndex != currentIndex) return;

        if (isTargetPath)
            CommitLinePositions(BuildSegmentPoints(node.myIndex));

        checkpointCursor++;

        if (checkpointCursor >= checkpointIndices.Count)
        {
            if (isTargetPath && finalNode != null)
            {
                isWaitingForFinal = true;
                if (!useParticleMode)
                {
                    lineRenderer.positionCount++;
                    lineRenderer.SetPosition(lineRenderer.positionCount - 1, DataManager.Instance.playerLineTransform.position);
                }
                ActivateFinalNode();
                Debug.Log("모든 경유지 통과! 마지막 점을 연결하세요.");
            }
            else
            {
                FinishPath();
            }
        }
        else
        {
            currentIndex = checkpointIndices[checkpointCursor];

            if (!useParticleMode && isTargetPath)
            {
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, DataManager.Instance.playerLineTransform.position);
            }
            ActivateNode(currentIndex);
        }
    }

    public void FinishPath()
    {
        isFinished = true;

        if (useParticleMode)
        {
            if (drawingParticles != null)
            {
                var emission = drawingParticles.emission;
                emission.enabled = false;
            }
        }
        else
        {
            // 마지막 체크포인트 이후 남아있는 꼭짓점들을 모두 채운 뒤 최종점으로 마감
            List<Vector3> tail = BuildSegmentPoints(pathNodes.Count - 1);
            tail.Add(finalPosition);
            CommitLinePositions(tail);
        }

        Debug.Log($"한붓그리기 완성! (모드: {(useParticleMode ? "파티클" : "라인")})");

        DataManager.Instance.gameEnded = true;
        DataManager.Instance.gameCleared = true;

        foreach (var node in pathNodes)
        {
            RestoreOriginalNode(node);
            node.NodeRenderer.material = completedMaterial;
        }

        if (finalNode != null)
        {
            RestoreOriginalNode(finalNode);
            finalNode.NodeRenderer.enabled = true;
            finalNode.NodeRenderer.material = completedMaterial;
        }

        ActivateThis();
        DataManager.Instance.StartCoroutine(DataManager.Instance.GameOver());
    }

    void ReplaceNodeWithPrefab(PathNode node)
    {
        if (activeNodePrefab == null || activeNodeInstances.ContainsKey(node)) return;

        node.NodeRenderer.enabled = false;

        GameObject instance = Instantiate(activeNodePrefab, node.transform.position, node.transform.rotation, node.transform);
        instance.GetComponentInChildren<Renderer>().material = activeMaterial;
        instance.transform.Rotate(activeNodeRotation);

        activeNodeInstances.Add(node, instance);
    }

    void RestoreOriginalNode(PathNode node)
    {
        if (activeNodeInstances.TryGetValue(node, out GameObject instance))
        {
            if (instance != null) Destroy(instance);
            activeNodeInstances.Remove(node);
        }

        if (node.NodeRenderer != null)
            node.NodeRenderer.enabled = true;
    }

    private void PlayActiveAnimation(GameObject targetObj)
    {
        if (targetObj == null) return;

        Animator anim = targetObj.GetComponent<Animator>()
                     ?? targetObj.GetComponentInChildren<Animator>();
        anim?.SetTrigger(activeAnimTrigger);
    }

    void CheatCompletePath()
    {
        Debug.Log("치트 사용: 경로 순차 진행 시작");
        if (isFinished) return;
        StartCoroutine(CheatSequenceRoutine());
    }

    IEnumerator CheatSequenceRoutine()
    {
        List<int> remaining = new List<int>();
        for (int c = checkpointCursor; c < checkpointIndices.Count; c++)
            remaining.Add(checkpointIndices[c]);

        foreach (int idx in remaining)
        {
            if (isFinished) break;
            if (idx < 0 || idx >= pathNodes.Count) continue;

            pathNodes[idx].OnAbsorbed(); // OnAbsorbed 내부에서 OnNodeCollected 호출
            yield return new WaitForSeconds(0.1f);
        }

        if (finalNode != null && isWaitingForFinal)
        {
            yield return new WaitForSeconds(0.1f);
            finalNode.OnAbsorbed();
        }
    }

    public void ActivateThis()
    {
        if (activeManager != null)
            activeManager.ActivateOnly(this);
    }
}
