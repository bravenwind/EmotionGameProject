using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Rendering.Universal; // 필수 네임스페이스

public enum CompletedShape
{
    Happy = 0,
    Hope = 1,
    Angry = 2,
    Sad = 3
}

[RequireComponent(typeof(LineRenderer))]
public class PathManager : MonoBehaviour
{
    [Header("Camera Settings")]
    public CinemachineCamera playerCam;
    public CinemachineCamera mapCam;
    public GameObject skyboxCam;
    public Camera mainCam;
    public float mapCamZpos = -6000f;

    [Header("Settings")]
    public Transform playerTransform;
    public float lineWidth = 0.1f;

    [Header("Materials")]
    public Material defaultMaterial;
    public Material activeMaterial;
    public Material completedMaterial;

    [Header("Path Nodes")]
    // Generator가 채워줄 리스트
    public List<PathNode> pathNodes = new List<PathNode>();
    public CompletedShape completedShape;

    [Header("Debug")]
    public CinemachineCamera heartMapCam;
    public CinemachineCamera cloverMapCam;
    public CinemachineCamera lightningMapCam;
    public CinemachineCamera tearMapCam;
    public Debug_ShapeActiveManager activeManager;


    private int currentIndex = 0;
    private LineRenderer lineRenderer;
    private bool isFinished = false;
    private bool isLineStarted = false;

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

        // 만약 미리 배치된 노드가 있다면 시작 시 초기화
        if (pathNodes.Count > 0)
        {
            InitializePath();
        }
    }

    // ★ [수정됨] 외부(LevelGenerator)에서 맵 생성 후 호출할 함수
    public void InitializePath()
    {
        currentIndex = 0;
        isFinished = false;
        isLineStarted = false;
        lineRenderer.positionCount = 0;

        // 노드 초기화
        for (int i = 0; i < pathNodes.Count; i++)
        {
            pathNodes[i].manager = this;
            pathNodes[i].myIndex = i;
        }

        UpdateNodeStates();

        // 카메라 초기화
        if (playerCam != null && mapCam != null)
        {
            playerCam.Priority = 10;
            mapCam.Priority = 0;
        }

        Debug.Log($"게임 초기화 완료! 노드 개수: {pathNodes.Count}");
    }

    void Update()
    {
        // 라인 그리기 로직 (플레이어 위치 추적)
        if (isLineStarted && !isFinished && playerTransform != null)
        {
            int lastIndex = lineRenderer.positionCount - 1;
            if (lastIndex >= 0)
            {
                lineRenderer.SetPosition(lastIndex, playerTransform.position);
            }
        }
    }

    void UpdateNodeStates()
    {
        for (int i = 0; i < pathNodes.Count; i++)
        {
            if (i < currentIndex)
            {
                // 이미 지나간 노드
                pathNodes[i].GetComponent<Renderer>().material = completedMaterial;
                pathNodes[i].GetComponent<Collider>().isTrigger = true;
            }
            else if (i == currentIndex)
            {
                // 현재 목표 노드 (활성화)
                pathNodes[i].SetState(true, activeMaterial, defaultMaterial);
            }
            else
            {
                // 미래의 노드 (비활성화/벽)
                pathNodes[i].SetState(false, activeMaterial, defaultMaterial);
                pathNodes[i].GetComponent<Collider>().isTrigger = false; // 못 지나가게 벽으로 설정
            }
        }
    }

    public void OnNodeCollected(PathNode node)
    {
        if (node.myIndex != currentIndex) return;

        // 라인 그리기 추가 로직
        if (currentIndex == 0)
        {
            isLineStarted = true;
            lineRenderer.positionCount = 1;
            lineRenderer.SetPosition(0, node.transform.position);
        }
        else
        {
            int lastIndex = lineRenderer.positionCount - 1;
            lineRenderer.SetPosition(lastIndex, node.transform.position);
        }

        currentIndex++;

        if (currentIndex >= pathNodes.Count)
        {
            FinishPath();
        }
        else
        {
            // 다음 점을 위해 라인 포지션 하나 추가 (플레이어 추적용)
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, playerTransform.position);
            UpdateNodeStates();
        }
    }

    public void FinishPath()
    {
        isFinished = true;

        // 마지막 노드 연결 마무리
        if (pathNodes.Count > 0)
        {
            // 현재 플레이어 추적중인 마지막 점을 마지막 노드 위치로 고정
            int lastIndex = lineRenderer.positionCount - 1;
            lineRenderer.SetPosition(lastIndex, pathNodes[pathNodes.Count - 1].transform.position);

            // 만약 시작점으로 돌아가야 한다면 아래 주석 해제
            // lineRenderer.positionCount++;
            // lineRenderer.SetPosition(lineRenderer.positionCount - 1, pathNodes[0].transform.position);
        }

        Debug.Log("한붓그리기 완성!");

        foreach (var node in pathNodes)
        {
            node.GetComponent<Renderer>().material = completedMaterial;
        }

        // ★ [수정] null 체크를 추가하여 에러 방지
        if (activeManager != null)
        {
            // 매니저에게 "나만 남기고 나머지는 정리해줘"라고 요청
            // (이 안에서 SwitchToMapCamera가 호출되므로 여기서 또 부를 필요 없음)
            activeManager.ActivateOnly(this);
        }
        else
        {
            // 매니저가 없으면 혼자서라도 카메라 전환
            SwitchToMapCamera();
            Debug.LogWarning("ActiveManager가 연결되지 않았습니다!");
        }
    }

    public void SwitchToMapCamera()
    {
        if (playerCam != null && mapCam != null)
        {
            playerCam.Priority = 0;
            mapCam.Priority = 10;
        }
    }

    public void SwitchToPlayerCamera()
    {
        if (playerCam != null && mapCam != null)
        {
            playerCam.Priority = 10;
            mapCam.Priority = 0;
        }
    }
}