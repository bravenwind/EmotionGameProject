using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(LineRenderer))]
public class PathManager : MonoBehaviour
{
    [Header("Camera Settings")]
    public CinemachineCamera playerCam; // 1번 카메라 (PlayerCam 연결)
    public CinemachineCamera mapCam;    // 2번 카메라 (MapCam 연결)

    // ... (기존 변수들: playerTransform, lineWidth, Materials 등등 그대로 유지) ...
    [Header("Settings")]
    public Transform playerTransform;
    public float lineWidth = 0.1f;

    [Header("Materials")]
    public Material defaultMaterial;
    public Material activeMaterial;
    public Material completedMaterial;

    [Header("Path Nodes")]
    public List<PathNode> pathNodes = new List<PathNode>();

    private int currentIndex = 0;
    private LineRenderer lineRenderer;
    private bool isFinished = false;
    private bool isLineStarted = false;

    void Start()
    {
        // ... (기존 Start 내용 그대로) ...
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 0;

        for (int i = 0; i < pathNodes.Count; i++)
        {
            pathNodes[i].manager = this;
            pathNodes[i].myIndex = i;
        }
        UpdateNodeStates();

        // ★ 시작할 땐 플레이어 카메라가 우선순위 높게 설정
        if (playerCam != null && mapCam != null)
        {
            playerCam.Priority = 10;
            mapCam.Priority = 0;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            SwitchToMapCamera();
        }

        // ... (기존 Update 내용 그대로) ...
        if (isLineStarted && !isFinished && playerTransform != null)
        {
            int lastIndex = lineRenderer.positionCount - 1;
            if (lastIndex >= 0)
            {
                lineRenderer.SetPosition(lastIndex, playerTransform.position);
            }
        }
    }

    // ... (UpdateNodeStates, OnNodeCollected 등 기존 함수들 그대로 유지) ...
    void UpdateNodeStates()
    {
        // ... (내용 동일) ...
        for (int i = 0; i < pathNodes.Count; i++)
        {
            if (i < currentIndex)
            {
                pathNodes[i].GetComponent<Renderer>().material = completedMaterial;
                pathNodes[i].GetComponent<Collider>().isTrigger = true;
            }
            else if (i == currentIndex)
            {
                pathNodes[i].SetState(true, activeMaterial, defaultMaterial);
            }
            else
            {
                pathNodes[i].SetState(false, activeMaterial, defaultMaterial);
                pathNodes[i].GetComponent<Collider>().isTrigger = true;
            }
        }
    }

    public void OnNodeCollected(PathNode node)
    {
        // ... (내용 동일) ...
        if (node.myIndex != currentIndex) return;

        // ... (점 찍는 로직 동일) ...
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
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, playerTransform.position);
            UpdateNodeStates();
        }
    }

    void FinishPath()
    {
        isFinished = true;

        if (pathNodes.Count > 0)
        {
            lineRenderer.positionCount++;
            int lastIndex = lineRenderer.positionCount - 1;
            lineRenderer.SetPosition(lastIndex, pathNodes[0].transform.position);
        }

        Debug.Log("한붓그리기 완성!");

        foreach (var node in pathNodes)
        {
            node.GetComponent<Renderer>().material = completedMaterial;
        }

        // ★ 게임 클리어 시 카메라 전환!
        SwitchToMapCamera();
    }

    // ★ 카메라 전환 함수 추가
    public void SwitchToMapCamera()
    {
        if (playerCam != null && mapCam != null)
        {
            // MapCam의 우선순위를 높여서 자연스럽게 전환되게 함
            playerCam.Priority = 0;
            mapCam.Priority = 10;
        }
    }
}