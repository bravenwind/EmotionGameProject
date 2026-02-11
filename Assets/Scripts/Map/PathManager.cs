using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(LineRenderer))]
public class PathManager : MonoBehaviour
{
    [Header("Camera Settings")]
    public CinemachineCamera mapCam;
    public CinemachineBrain brain;
    public GameObject skyboxCam;
    public Camera mainCam;
    public float mapCamZpos = -6000f;

    [Header("Settings")]
    public float lineWidth = 0.1f;
    public float nodeScaleMultiplier = 2.0f;

    [Header("Materials")]
    public Material defaultMaterial;
    public Material activeMaterial;
    public Material completedMaterial;

    [Header("Path Nodes")]
    public List<PathNode> pathNodes = new List<PathNode>();

    // ★ [추가됨] 리스트를 다 돌고 나서 마지막으로 닫아줄 점 (보통 시작점인 pathNodes[0]을 넣으면 됨)
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

    // ★ [추가됨] 모든 노드를 다 돌고 마지막 점 연결을 기다리는 상태인지 확인
    private bool isWaitingForFinal = false;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 0;
        lineRenderer.material = completedMaterial;

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
        isWaitingForFinal = false; // 초기화
        lineRenderer.positionCount = 0;

        for (int i = 0; i < pathNodes.Count; i++)
        {
            pathNodes[i].manager = this;
            pathNodes[i].myIndex = i;
            pathNodes[i].emotion = completedEmotion;
        }

        // 만약 finalNode가 설정되어 있다면 그것도 매니저 연결 (보통 리스트 안에 있어서 중복되겠지만 안전하게)
        if (finalNode != null) finalNode.manager = this;

        UpdateNodeStates();

        if (DataManager.Instance.playerCam != null && mapCam != null)
        {
            DataManager.Instance.playerCam.Priority = 10;
            mapCam.Priority = 0;
        }

        Debug.Log($"게임 초기화 완료! 노드 개수: {pathNodes.Count}");
    }

    void Update()
    {
        if (isLineStarted && !isFinished && DataManager.Instance.playerLineTransform != null)
        {
            int lastIndex = lineRenderer.positionCount - 1;
            if (lastIndex >= 0)
            {
                lineRenderer.SetPosition(lastIndex, DataManager.Instance.playerLineTransform.position);
            }
        }
    }

    // ★ [수정됨] 상태 업데이트 로직 (마지막 연결 대기 상태 처리 추가)
    void UpdateNodeStates()
    {
        // 1. 일반 리스트 노드 처리
        for (int i = 0; i < pathNodes.Count; i++)
        {
            if (i < currentIndex)
            {
                // 이미 지나간 노드
                pathNodes[i].GetComponent<Renderer>().material = completedMaterial;
                pathNodes[i].GetComponent<Collider>().isTrigger = true;
            }
            else if (i == currentIndex && !isWaitingForFinal)
            {
                // 현재 목표 노드 (활성화)
                pathNodes[i].SetState(true, activeMaterial, defaultMaterial);
                if (completedEmotion == DataManager.Instance.targetEmotion)
                {
                    pathNodes[i].transform.localScale *= nodeScaleMultiplier;
                }
            }
            else
            {
                // 아직 순서가 아닌 노드 (벽)
                pathNodes[i].SetState(false, activeMaterial, defaultMaterial);
                pathNodes[i].GetComponent<Collider>().isTrigger = false;
            }
        }

        // 2. ★ [추가됨] 모든 리스트를 다 돌고, 마지막 finalNode를 열어줄 차례인지 확인
        if (isWaitingForFinal && finalNode != null)
        {
            // finalNode를 활성화 (목표 지점으로 표시)
            finalNode.gameObject.SetActive(true);
            finalNode.SetState(true, activeMaterial, defaultMaterial);
            finalNode.GetComponent<Collider>().isTrigger = true; // 닿을 수 있게 트리거 켬
        }
        else if (finalNode != null && currentIndex < pathNodes.Count)
        {
            // 아직 리스트 도는 중이라면 finalNode가 리스트 순서에 포함된 게 아니면 꺼둬야 함
            // (보통 finalNode가 pathNodes[0]인 경우가 많으므로, 위 loop에서 처리되었을 수 있음.
            //  여기서는 특별히 덮어쓰지 않고 둠)
        }
    }

    public void OnNodeCollected(PathNode node)
    {
        // ★ 1. 마지막 연결 대기 상태일 때 처리
        if (isWaitingForFinal)
        {
            // 닿은 노드가 우리가 기다리던 '최종 노드'가 맞는지 확인
            if (node == finalNode)
            {
                FinishPath(); // 진짜 끝!
            }
            return; // 다른 노드면 무시
        }

        // ★ 2. 일반 진행 상태 (순서 체크)
        if (node.myIndex != currentIndex) return;

        if (completedEmotion == DataManager.Instance.targetEmotion)
        {
            // --- 라인 그리기 로직 ---
            if (currentIndex == 0)
            {
                isLineStarted = true;
                lineRenderer.positionCount = 1;
                lineRenderer.SetPosition(0, node.transform.position);
            }
            else
            {
                // 이전 점 확정
                int lastIndex = lineRenderer.positionCount - 1;
                lineRenderer.SetPosition(lastIndex, node.transform.position);
            }
        }

        currentIndex++;

        // --- 다음 단계 결정 ---
        if (currentIndex >= pathNodes.Count)
        {
            if (completedEmotion == DataManager.Instance.targetEmotion)
            {
                // 리스트는 다 돌았음.
                // ★ 만약 FinalNode가 설정되어 있다면, 바로 끝내지 않고 '마지막 연결 대기' 상태로 진입
                if (finalNode != null)
                {
                    isWaitingForFinal = true;

                    // 플레이어를 따라다닐 마지막 라인 하나 추가
                    lineRenderer.positionCount++;
                    lineRenderer.SetPosition(lineRenderer.positionCount - 1, DataManager.Instance.playerLineTransform.position);


                    // 노드 상태 업데이트 (finalNode 활성화)
                    UpdateNodeStates();

                    Debug.Log("모든 경유지 통과! 마지막 점을 연결하세요.");
                }
                else
                {
                    // FinalNode가 없으면 기존처럼 바로 종료
                    FinishPath();
                }
            }
        }
        else
        {
            // 아직 리스트가 남았음 -> 다음 점 추적 라인 생성
            if (completedEmotion == DataManager.Instance.targetEmotion)
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

        // ★ 마지막 라인을 최종 목적지(FinalNode 혹은 마지막 리스트 노드)에 딱 붙이기
        if (lineRenderer.positionCount > 0)
        {
            int lastIndex = lineRenderer.positionCount - 1;
            // Final 모드였다면 FinalNode 위치로, 아니면 리스트 마지막 위치로
            Vector3 targetPos = (isWaitingForFinal && finalNode != null)
                                ? finalNode.transform.position
                                : pathNodes[pathNodes.Count - 1].transform.position;

            lineRenderer.SetPosition(lastIndex, targetPos);
        }

        Debug.Log("한붓그리기 완성!");

        DataManager.Instance.missionDict[DataManager.Instance.mission2] = true;
        DataManager.Instance.mission2Success = true;

        // 모든 노드(FinalNode 포함) 완료 색상으로
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
        DataManager.Instance.playerTransform.rotation = mapCam.transform.rotation;
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
        // 중요: 블렌딩이 시작되기까지 1프레임 정도 딜레이가 있을 수 있음
        yield return null;

        // 블렌딩이 진행 중이라면 계속 대기
        while (brain.IsBlending)
        {
            yield return null;
        }

        // 블렌딩이 끝난 후 실행할 함수
        OnTransitionComplete();
    }

    public void OnTransitionComplete()
    {
        GameSceneUIManager.Instance.SetState(GameSceneUIState.GameOver);
    }
}