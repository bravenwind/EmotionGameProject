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
    public Mesh targetMesh;

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
        lineRenderer.enabled = !useParticleMode; // 모드에 따라 활성/비활성

        if (pathNodes.Count <= 0)
        {
            PathNode[] pathNodesInChildren = GetComponentsInChildren<PathNode>();
            foreach (PathNode node in pathNodesInChildren)
            {
                pathNodes.Add(node);
            }
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

        // --- 모드별 초기화 ---
        if (useParticleMode)
        {
            // [파티클 모드] 라인 렌더러 끄고 파티클 세팅
            lineRenderer.enabled = false;

            if (drawingParticles != null)
            {
                var main = drawingParticles.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World; // 월드 좌표 필수
                main.startLifetime = Mathf.Infinity; // 영구 보존
                main.startSpeed = 0;

                var emission = drawingParticles.emission;
                emission.enabled = false; // 일단 멈춤

                if (clearParticlesOnStart) drawingParticles.Clear();
            }
        }
        else
        {
            // [라인 모드] 라인 렌더러 켜고 초기화
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 0;
        }
        // -------------------

        // Final Node가 리스트 끝에 중복되면 제거
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
        }

        if (finalNode != null)
        {
            finalNode.manager = this;
            finalNode.myIndex = 9999;
            finalNode.gameObject.GetComponent<MeshRenderer>().enabled = false;
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
        // [치트키] M키를 누르면 즉시 클리어
        if (Input.GetKeyDown(KeyCode.M) && !isFinished && completedEmotion == DataManager.Instance.targetEmotion)
        {
            CheatCompletePath();
        }

        // ★ 라인 모드일 때만 매 프레임 선을 갱신합니다.
        if (!useParticleMode && isLineStarted && !isFinished && DataManager.Instance.playerLineTransform != null)
        {
            int lastIndex = lineRenderer.positionCount - 1;
            if (lastIndex >= 0)
            {
                lineRenderer.SetPosition(lastIndex, DataManager.Instance.playerLineTransform.position);
            }
        }
    }

    void UpdateNodeStates()
    {
        for (int i = 0; i < pathNodes.Count; i++)
        {
            if (i < currentIndex)
            {
                pathNodes[i].GetComponent<Renderer>().material = completedMaterial;
                pathNodes[i].GetComponent<Collider>().isTrigger = true;
            }
            else if (i == currentIndex && !isWaitingForFinal)
            {
                pathNodes[i].SetState(true, activeMaterial, defaultMaterial);
                if (completedEmotion == DataManager.Instance.targetEmotion)
                {
                    pathNodes[i].transform.localScale = originalNodeScale * targetNodeScaleMultiplier;
                    pathNodes[i].GetComponent<MeshFilter>().mesh = targetMesh;

                    // ★ 애니메이션 재생 함수 호출
                    PlayActiveAnimation(pathNodes[i]);
                }
            }
            else
            {
                pathNodes[i].SetState(false, activeMaterial, defaultMaterial);
                pathNodes[i].GetComponent<Collider>().isTrigger = false;
            }
        }

        if (isWaitingForFinal && finalNode != null)
        {
            finalNode.gameObject.SetActive(true);
            finalNode.gameObject.GetComponent<MeshRenderer>().enabled = true;
            finalNode.gameObject.GetComponent<Collider>().enabled = true;
            finalNode.SetState(true, activeMaterial, defaultMaterial);
            finalNode.GetComponent<Collider>().isTrigger = true;
        }
    }

    public void OnNodeCollected(PathNode node)
    {
        // 1. 마지막 연결 대기 상태 처리
        if (isWaitingForFinal)
        {
            if (node == finalNode)
            {
                FinishPath();
            }
            return;
        }

        // 2. 순서 체크
        if (node.myIndex != currentIndex) return;

        // 3. 그리기 로직 (모드 분기)
        if (completedEmotion == DataManager.Instance.targetEmotion)
        {
            if (currentIndex == 0) // 첫 시작
            {
                isLineStarted = true;

                if (useParticleMode)
                {
                    // [파티클 모드] 파티클 켜기
                    if (drawingParticles != null)
                    {
                        var emission = drawingParticles.emission;
                        emission.enabled = true;
                        drawingParticles.Play();
                    }
                }
                else
                {
                    // [라인 모드] 첫 점과 플레이어 따라다닐 점 생성
                    lineRenderer.positionCount = 1;
                    lineRenderer.SetPosition(0, node.transform.position);
                }
            }
            else
            {
                // 중간 노드들
                if (!useParticleMode)
                {
                    // [라인 모드] 이전 점 확정
                    int lastIndex = lineRenderer.positionCount - 1;
                    lineRenderer.SetPosition(lastIndex, node.transform.position);
                }
                // [파티클 모드]는 아무것도 안 해도 됨 (계속 나오는 중)
            }
            //PlaySFXAudio.Instance.PlayEmotionConnect();
        }

        currentIndex++;

        // 4. 다음 단계 결정
        if (currentIndex >= pathNodes.Count)
        {
            if (completedEmotion == DataManager.Instance.targetEmotion)
            {
                if (finalNode != null)
                {
                    isWaitingForFinal = true;

                    // 라인 모드일 경우 마지막 추적용 선 하나 추가
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
            // 라인 모드일 경우 다음 점 추적용 선 추가
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
        isFinished = true;

        // --- 종료 처리 (모드 분기) ---
        if (useParticleMode)
        {
            // [파티클 모드] 더 이상 나오지 않게 끔
            if (drawingParticles != null)
            {
                var emission = drawingParticles.emission;
                emission.enabled = false;
            }
        }
        else
        {
            // [라인 모드] 마지막 선을 목표 지점에 스냅
            if (lineRenderer.positionCount > 0)
            {
                int lastIndex = lineRenderer.positionCount - 1;
                Vector3 targetPos = (isWaitingForFinal && finalNode != null)
                                    ? finalNode.transform.position
                                    : pathNodes[pathNodes.Count - 1].transform.position;
                lineRenderer.SetPosition(lastIndex, targetPos);
            }
        }
        // ---------------------------

        Debug.Log($"한붓그리기 완성! (모드: {(useParticleMode ? "파티클" : "라인")})");

        DataManager.Instance.missionDict[DataManager.Instance.mission2] = true;
        DataManager.Instance.mission2Success = true;

        foreach (var node in pathNodes)
        {
            node.GetComponent<Renderer>().material = completedMaterial;
        }
        if (finalNode != null)
        {
            finalNode.GetComponent<Renderer>().material = completedMaterial;
        }

        if (activeManager != null)
        {
            activeManager.ActivateOnly(this);
        }
        else
        {
            SwitchToMapCamera();
        }
    }

    public void SwitchToMapCamera()
    {
        DataManager.Instance.mouseLook.enabled = false;
        DataManager.Instance.playerCam.transform.rotation = mapCam.transform.rotation;
        if (DataManager.Instance.playerCam != null && mapCam != null)
        {
            DataManager.Instance.playerCam.Priority = 0;
            mapCam.Priority = 10;
        }
        StartCoroutine(WaitForBlendToFinish());
    }

    public void SwitchToPlayerCamera()
    {
        DataManager.Instance.mouseLook.enabled = true;
        if (DataManager.Instance.playerCam != null && mapCam != null)
        {
            DataManager.Instance.playerCam.Priority = 10;
            mapCam.Priority = 0;
        }
    }

    IEnumerator WaitForBlendToFinish()
    {
        yield return new WaitForSecondsRealtime(1f);
        while (brain.IsBlending)
        {
            yield return null;
        }
        OnTransitionComplete();
    }

    public void OnTransitionComplete()
    {
        GameSceneUIManager.Instance.SetState(GameSceneUIState.GameOver);
    }

    /// <summary>
    /// 해당 노드의 Animator를 찾아 활성화 애니메이션을 재생합니다.
    /// </summary>
    private void PlayActiveAnimation(PathNode node)
    {
        if (node == null) return;

        Animator anim = node.GetComponent<Animator>();
        if (anim == null)
        {
            // 혹시 자식 오브젝트에 Animator가 있을 경우를 대비해InChildren으로 검색
            anim = node.GetComponentInChildren<Animator>();
        }

        if (anim != null)
        {
            // 현재 애니메이션이 이미 재생 중인지 체크하는 로직이 필요하다면 추가 가능하지만,
            // Trigger 방식은 중복 호출되어도 상태 전이 조건만 맞으면 자연스럽게 처리됩니다.
            anim.SetTrigger(activeAnimTrigger);
        }
    }

    void CheatCompletePath()
    {
        Debug.Log("치트 사용: 경로 강제 완성");

        // 1. 모든 인덱스 강제 통과 처리
        currentIndex = pathNodes.Count;

        // 2. Final Node가 있다면 대기 상태로 변경
        if (finalNode != null)
        {
            isWaitingForFinal = true;
            finalNode.gameObject.SetActive(true); // 혹시 꺼져있다면 켜기
        }

        // 3. 라인 렌더러 모드라면, 모든 점을 강제로 연결해서 그림
        if (!useParticleMode)
        {
            lineRenderer.enabled = true;

            // 필요한 점의 개수 계산 (경유지 노드들 + 마지막 노드)
            int totalCount = pathNodes.Count + (finalNode != null ? 1 : 0);
            lineRenderer.positionCount = totalCount;

            // 경유지 좌표 입력
            for (int i = 0; i < pathNodes.Count; i++)
            {
                lineRenderer.SetPosition(i, pathNodes[i].transform.position);
            }

            // 마지막 노드 좌표 입력
            if (finalNode != null)
            {
                lineRenderer.SetPosition(pathNodes.Count, finalNode.transform.position);
            }
        }
        else
        {
            // 파티클 모드라면 단순히 파티클을 켜서 흔적만 남기거나, 
            // 즉시 클리어 처리하므로 별도 드로잉 로직은 생략해도 됩니다.
            // 필요하다면 여기서 drawingParticles.Play() 등을 호출할 수 있습니다.
        }

        // 4. 최종 종료 함수 호출 (성공 처리)
        FinishPath();
    }
}