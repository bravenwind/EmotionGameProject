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
    public GameObject activeNodePrefab; // ★ 추가됨: 활성화 시 생성될 프리팹
    public Vector3 activeNodeRotation;

    [Header("Materials")]
    public Material defaultMaterial;
    public Material activeMaterial;
    public Material completedMaterial;
    public Material lineMaterial;

    [Header("Path Nodes")]
    public List<PathNode> pathNodes = new List<PathNode>();
    public PathNode finalNode;

    public EmotionState completedEmotion;

    [Header("Debug")]
    public CinemachineCamera happyMapCam;
    public CinemachineCamera hopeMapCam;
    public CinemachineCamera angryMapCam;
    public CinemachineCamera sadMapCam;
    public Debug_ShapeActiveManager activeManager;

    private int currentIndex = 0;
    private LineRenderer lineRenderer;
    private bool isFinished = false;
    private bool isLineStarted = false;
    private bool isWaitingForFinal = false;

    // ★ 추가됨: 현재 생성되어 있는 활성 프리팹들을 추적하기 위한 딕셔너리
    private Dictionary<PathNode, GameObject> activeNodeInstances = new Dictionary<PathNode, GameObject>();

    [Header("Animation Settings")]
    [Tooltip("PathNode의 Animator에 설정된 Trigger 파라미터 이름")]
    public string activeAnimTrigger = "Targetted";

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // 라인 렌더러 기본 설정
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 0;
        lineRenderer.material = lineMaterial;
        lineRenderer.enabled = !useParticleMode;
        lineRenderer.numCornerVertices = 50;
        lineRenderer.numCapVertices = 50;

        pathNodes.Clear();

        if (pathNodes.Count <= 0)
        {
            PathNode[] pathNodesInChildren = GetComponentsInChildren<PathNode>();
            foreach (PathNode node in pathNodesInChildren)
            {
                pathNodes.Add(node);
            }
        }

        if (completedEmotion == DataManager.Instance.targetEmotion)
        {
            DataManager.Instance.targetMapCam = mapCam;
            DataManager.Instance.maxEmotionScore = pathNodes.Count;
        }

        if (pathNodes.Count > 0)
        {
            InitializePath();
        }
    }

    public void InitializePath()
    {
        currentIndex = 0;
        isFinished = false;
        isLineStarted = false;
        isWaitingForFinal = false;

        // 초기화 시 기존에 남아있을 수 있는 프리팹 제거
        foreach (var kvp in activeNodeInstances)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        activeNodeInstances.Clear();

        // --- 모드별 초기화 ---
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
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 0;
        }

        // Final Node 중복 제거
        if (finalNode != null && pathNodes.Count > 0)
        {
            if (pathNodes[pathNodes.Count - 1] == finalNode)
            {
                pathNodes.RemoveAt(pathNodes.Count - 1);
            }
        }

        if (pathNodes.Count > 0)
        {
            originalNodeScale = pathNodes[0].transform.localScale;
        }

        for (int i = 0; i < pathNodes.Count; i++)
        {
            pathNodes[i].manager = this;
            pathNodes[i].myIndex = i;
            pathNodes[i].emotion = completedEmotion;

            if (i == 0 && originalNodeScale == Vector3.zero)
                originalNodeScale = pathNodes[i].transform.localScale;

            pathNodes[i].transform.localScale = originalNodeScale * nonTargetNodeScaleMultiplier;

            // 초기화 시 렌더러 켜두기
            pathNodes[i].GetComponent<Renderer>().enabled = true;
        }

        if (finalNode != null)
        {
            finalNode.manager = this;
            finalNode.myIndex = 9999;
            finalNode.gameObject.GetComponent<Renderer>().enabled = false;
            finalNode.gameObject.GetComponent<Collider>().enabled = false;
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
        if (Input.GetKeyDown(KeyCode.M) && !isFinished && completedEmotion == DataManager.Instance.targetEmotion)
        {
            CheatCompletePath();
        }

        if (!useParticleMode && isLineStarted && !isFinished && DataManager.Instance.playerLineTransform != null)
        {
            int lastIndex = lineRenderer.positionCount - 1;
            if (lastIndex >= 0)
            {
                lineRenderer.SetPosition(lastIndex, DataManager.Instance.playerLineTransform.position);
            }
        }
    }

    // ★ 수정된 핵심 함수: 프리팹 생성/교체 로직 적용
    void UpdateNodeStates()
    {
        for (int i = 0; i < pathNodes.Count; i++)
        {
            // 1. 이미 지나온 노드 (완료 상태)
            if (i < currentIndex)
            {
                pathNodes[i].GetComponent<Renderer>().material = completedMaterial;
                pathNodes[i].transform.localScale = originalNodeScale * targetNodeScaleMultiplier;
            }
            // 2. 현재 타겟 노드 (활성 상태)
            else if (i == currentIndex && !isWaitingForFinal)
            {
                // 마지막 노드(Final Node) 처리
                if (isWaitingForFinal && finalNode != null)
                {
                    finalNode.gameObject.SetActive(true);

                    // ★ [수정됨] 렌더러와 콜라이더를 확실하게 켭니다.
                    // 프리팹(activeNodePrefab)이 없더라도 기본 모습이 보여야 하므로 여기서 켜줘야 합니다.
                    if (finalNode.GetComponent<Renderer>() != null)
                        finalNode.GetComponent<Renderer>().enabled = true;

                    if (finalNode.GetComponent<Collider>() != null)
                    {
                        finalNode.GetComponent<Collider>().enabled = true;
                        finalNode.GetComponent<Collider>().isTrigger = true;
                    }

                    // Final Node도 활성 상태면 프리팹 적용
                    // (ReplaceNodeWithPrefab 내부에서 프리팹이 있으면 렌더러를 다시 끄겠지만, 없으면 위에서 켠 상태가 유지됨)
                    ReplaceNodeWithPrefab(finalNode);
                    finalNode.SetState(true, activeMaterial, defaultMaterial);
                    pathNodes[i].transform.localScale = originalNodeScale * targetNodeScaleMultiplier;

                    if (activeNodeInstances.ContainsKey(finalNode))
                    {
                        PlayActiveAnimation(activeNodeInstances[finalNode]);
                    }
                }
                else
                {
                    ReplaceNodeWithPrefab(pathNodes[i]);
                    pathNodes[i].transform.localScale = originalNodeScale * targetNodeScaleMultiplier;
                    pathNodes[i].SetState(true, activeMaterial, defaultMaterial);
                    Debug.Log(gameObject.name + "활성화됨");
                }
            }
            // 3. 아직 오지 않은 노드
            else
            {
                pathNodes[i].SetState(false, activeMaterial, defaultMaterial);
                pathNodes[i].GetComponent<Collider>().isTrigger = false;
                pathNodes[i].transform.localScale = originalNodeScale * nonTargetNodeScaleMultiplier;
            }
        }

        // 마지막 노드(Final Node) 처리
        if (isWaitingForFinal && finalNode != null)
        {
            finalNode.gameObject.SetActive(true);
            finalNode.gameObject.GetComponent<Collider>().enabled = true;
            finalNode.gameObject.GetComponent<Collider>().isTrigger = true;

            // Final Node도 활성 상태면 프리팹 적용
            ReplaceNodeWithPrefab(finalNode);
            finalNode.transform.localScale = originalNodeScale * targetNodeScaleMultiplier;
            finalNode.SetState(true, activeMaterial, defaultMaterial);

            if (activeNodeInstances.ContainsKey(finalNode))
            {
                PlayActiveAnimation(activeNodeInstances[finalNode]);
            }
        }
    }

    // ★ 헬퍼 함수 1: 노드를 프리팹으로 대체
    void ReplaceNodeWithPrefab(PathNode node)
    {
        if (activeNodePrefab == null) return;

        // 이미 생성된 프리팹이 없다면 생성
        if (!activeNodeInstances.ContainsKey(node))
        {
            // 기존 렌더러 끄기 (숨기기)
            var meshRenderer = node.GetComponent<Renderer>();
            if (meshRenderer != null) meshRenderer.enabled = false;

            // 프리팹 생성 (노드의 자식으로)
            GameObject instance = Instantiate(activeNodePrefab, node.transform.position, node.transform.rotation, node.transform);
            instance.GetComponentInChildren<Renderer>().material = activeMaterial;
            instance.transform.Rotate(activeNodeRotation);

            // 프리팹 크기 조정이 필요하다면 여기서 설정 (예: 1,1,1)
            // instance.transform.localScale = Vector3.one; 

            activeNodeInstances.Add(node, instance);
        }
    }

    // ★ 헬퍼 함수 2: 프리팹을 지우고 원래 노드 모습으로 복구
    void RestoreOriginalNode(PathNode node)
    {
        // 딕셔너리에 기록된 프리팹이 있다면
        if (activeNodeInstances.ContainsKey(node))
        {
            GameObject instance = activeNodeInstances[node];
            if (instance != null)
            {
                Destroy(instance); // 프리팹 파괴
            }
            activeNodeInstances.Remove(node); // 목록에서 제거

            // 숨겼던 기존 렌더러 다시 켜기
            var meshRenderer = node.GetComponent<Renderer>();
            if (meshRenderer != null) meshRenderer.enabled = true;
        }
        else
        {
            // 혹시 딕셔너리엔 없지만 렌더러가 꺼져있을 수 있으니 안전하게 켜줌
            var meshRenderer = node.GetComponent<Renderer>();
            if (meshRenderer != null && !meshRenderer.enabled) meshRenderer.enabled = true;
        }
    }

    public void OnNodeCollected(PathNode node)
    {
        if (isWaitingForFinal)
        {
            if (node == finalNode)
            {
                FinishPath();
            }
            return;
        }

        if (node.myIndex != currentIndex) return;

        if (completedEmotion == DataManager.Instance.targetEmotion)
        {
            if (currentIndex == 0)
            {
                isLineStarted = true;

                if (useParticleMode)
                {
                    if (drawingParticles != null)
                    {
                        var emission = drawingParticles.emission;
                        emission.enabled = true;
                        drawingParticles.Play();
                    }
                }
                else
                {
                    lineRenderer.positionCount = 1;
                    lineRenderer.SetPosition(0, node.transform.position);
                }
            }
            else
            {
                if (!useParticleMode)
                {
                    int lastIndex = lineRenderer.positionCount - 1;
                    lineRenderer.SetPosition(lastIndex, node.transform.position);
                }
            }
        }

        currentIndex++;

        if (currentIndex >= pathNodes.Count)
        {
            if (completedEmotion == DataManager.Instance.targetEmotion)
            {
                if (finalNode != null)
                {
                    isWaitingForFinal = true;

                    if (!useParticleMode)
                    {
                        lineRenderer.positionCount++;
                        lineRenderer.SetPosition(lineRenderer.positionCount - 1, DataManager.Instance.playerLineTransform.position);
                    }

                    UpdateNodeStates();
                    Debug.Log("모든 경유지 통과! 마지막 점을 연결하세요.");
                }
                else
                {
                    FinishPath();
                }
            }
        }
        else
        {
            if (!useParticleMode && completedEmotion == DataManager.Instance.targetEmotion)
            {
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, DataManager.Instance.playerLineTransform.position);
            }
            UpdateNodeStates();
        }
    }

    public void FinishPath()
    {
        // 1. 가장 먼저 종료 플래그를 세워 Update() 문이 더 이상 라인을 건드리지 못하게 합니다.
        isFinished = true;

        // --- 종료 처리 (모드 분기) ---
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
            // [라인 모드] 마지막 선 처리를 확실하게 수행
            if (lineRenderer.positionCount > 0)
            {
                int lastIndex = lineRenderer.positionCount - 1;
                Vector3 targetPos;

                targetPos = finalNode.transform.position;


                // ★ 강제 위치 할당: 이 코드가 실행되면 라인 끝이 플레이어에서 목표점으로 '스냅'됩니다.
                lineRenderer.SetPosition(lastIndex, targetPos);
            }
        }
        // ---------------------------

        Debug.Log($"한붓그리기 완성! (모드: {(useParticleMode ? "파티클" : "라인")})");

        
        DataManager.Instance.gameCleared = true;

        // 완료 시 모든 노드 정리 (프리팹 제거 및 완료 메테리얼 적용)
        foreach (var node in pathNodes)
        {
            RestoreOriginalNode(node);
            node.GetComponent<Renderer>().material = completedMaterial;
        }

        if (finalNode != null)
        {
            RestoreOriginalNode(finalNode);
            // 만약 렌더러가 꺼져있다면 다시 켜줍니다.
            if (finalNode.GetComponent<Renderer>() != null)
                finalNode.GetComponent<Renderer>().enabled = true;

            finalNode.GetComponent<Renderer>().material = completedMaterial;
        }

        
        ActivateThis();

        // activeManager 유무와 상관없이 카메라 전환이 필요하다면 아래 주석을 해제하거나 activeManager 내부에서 처리해야 함
        // SwitchToMapCamera(); 

        // (기존 코드 흐름상 activeManager가 없으면 여기서 전환)
        DataManager.Instance.StartCoroutine("GameOver");
    }

    /// <summary>
    /// 대상 오브젝트(또는 프리팹)의 Animator를 찾아 활성화 애니메이션 재생
    /// </summary>
    private void PlayActiveAnimation(GameObject targetObj)
    {
        if (targetObj == null) return;

        // PathNode 자체일 수도 있고, 생성된 프리팹일 수도 있음
        Animator anim = targetObj.GetComponent<Animator>();
        if (anim == null)
        {
            anim = targetObj.GetComponentInChildren<Animator>();
        }

        if (anim != null)
        {
            anim.SetTrigger(activeAnimTrigger);
        }
    }

    // PathNode를 받는 오버로드 유지
    private void PlayActiveAnimation(PathNode node)
    {
        PlayActiveAnimation(node.gameObject);
    }

    // ... (이전 코드들)

    void CheatCompletePath()
    {
        Debug.Log("치트 사용: 경로 순차 진행 시작");
        if (isFinished) return;

        // 코루틴을 시작하여 순서대로 노드를 획득하는 과정을 시뮬레이션합니다.
        StartCoroutine(CheatSequenceRoutine());
    }

    IEnumerator CheatSequenceRoutine()
    {
        // 1. 현재 인덱스부터 마지막 일반 노드까지 순서대로 처리
        // (리스트를 복사하거나 인덱스로 접근하여 안전하게 순회)
        int startIdx = currentIndex;
        int count = pathNodes.Count;

        for (int i = startIdx; i < count; i++)
        {
            PathNode node = pathNodes[i];

            // 실제 플레이어가 먹은 것처럼 로직 실행
            OnNodeCollected(node);
            node.OnAbsorbed();

            // 시각적으로 순서대로 켜지는 것을 보여주기 위해 약간의 딜레이 (0.1초)
            // 너무 빠르면 물리 연산이나 상태 갱신이 씹힐 수 있으므로 최소한의 딜레이 권장
            yield return new WaitForSeconds(0.1f);
        }

        // 2. 마지막 Final Node 처리
        // 위의 반복문이 끝나면 로직상 isWaitingForFinal 상태가 되어 있어야 함
        if (finalNode != null && isWaitingForFinal)
        {
            yield return new WaitForSeconds(0.1f);
            OnNodeCollected(finalNode);
        }
    }

    public void ActivateThis()
    {
        if (activeManager != null)
        {
            activeManager.ActivateOnly(this);
        }
    }
}