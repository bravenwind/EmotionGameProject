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
        lineRenderer.numCornerVertices = 50;
        lineRenderer.numCapVertices = 50;

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
            DataManager.Instance.maxEmotionScore = pathNodes.Count;
        }

        if (pathNodes.Count > 0)
        {
            InitializePath();

            if (isTargetPath)
                StartCoroutine(FaceFirstNodeOnStart());
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

        for (int i = 0; i < pathNodes.Count; i++)
        {
            pathNodes[i].manager = this;
            pathNodes[i].myIndex = i;
            pathNodes[i].emotion = completedEmotion;

            pathNodes[i].transform.localScale = originalNodeScale * nonTargetNodeScaleMultiplier;
            pathNodes[i].NodeRenderer.enabled = true;
        }

        if (finalNode != null)
        {
            finalNode.manager = this;
            finalNode.myIndex = 9999;
            finalPosition = finalNode.transform.position;
            finalNode.NodeRenderer.enabled = false;
            finalNode.NodeCollider.enabled = false;
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
        if (Input.GetKeyDown(KeyCode.M) && !isFinished && isTargetPath)
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
                    pathNodes[i].SetState(true, activeMaterial, defaultMaterial);
                    Debug.Log(gameObject.name + " 활성화됨");
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
        finalNode.NodeCollider.enabled = true;
        finalNode.NodeCollider.isTrigger = true;
        ReplaceNodeWithPrefab(finalNode);
        finalNode.transform.localScale = originalNodeScale * targetNodeScaleMultiplier;
        finalNode.SetState(true, activeMaterial, defaultMaterial);

        if (activeNodeInstances.TryGetValue(finalNode, out GameObject instance))
            PlayActiveAnimation(instance);
    }

    // 라인/파티클 업데이트 (OnNodeCollected에서만 호출)
    private void UpdateLine(PathNode collectedNode)
    {
        if (useParticleMode)
        {
            if (!isLineStarted && drawingParticles != null)
            {
                var emission = drawingParticles.emission;
                emission.enabled = true;
                drawingParticles.Play();
            }
        }
        else
        {
            if (!isLineStarted)
            {
                lineRenderer.positionCount = 1;
                lineRenderer.SetPosition(0, collectedNode.originalPosition);
            }
            else
            {
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, collectedNode.originalPosition);
            }
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
            UpdateLine(node);

        currentIndex++;

        if (currentIndex >= pathNodes.Count)
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
            if (lineRenderer.positionCount > 0)
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, finalPosition);
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
        int startIdx = currentIndex;
        int count = pathNodes.Count;

        for (int i = startIdx; i < count; i++)
        {
            pathNodes[i].OnAbsorbed(); // OnAbsorbed 내부에서 OnNodeCollected 호출
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
