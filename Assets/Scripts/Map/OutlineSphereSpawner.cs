using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

[RequireComponent(typeof(PolygonCollider2D))]
public class OutLineSphereSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject spherePrefab;

    [Tooltip("체크하면 개수(Target Count)에 맞춰 간격을 자동 조절합니다.")]
    public bool useFixedCount = true;

    [Tooltip("useFixedCount가 켜져있을 때 생성할 총 스피어 개수")]
    public int targetSphereCount = 100;

    [Tooltip("useFixedCount가 꺼져있을 때 사용하는 간격")]
    public float spacing = 0.5f;

    public float randomRange = 2.0f;
    public float scaleFactor = 1.0f;

    [Header("Camera Settings")]
    public float cameraDistance = 6000f;

    [ContextMenu("맵 소환")]
    void SpawnSpheresAlongCollider()
    {
        ClearSpheres();

        GetComponent<SpriteRenderer>().enabled = false;
        PolygonCollider2D polygonCollider = GetComponent<PolygonCollider2D>();

        // 1. 가장 긴 경로 찾기 (월드 좌표 기준 길이 계산)
        int bestPathIndex = -1;
        float maxWorldPathLength = 0f;

        for (int i = 0; i < polygonCollider.pathCount; i++)
        {
            // ★ 수정됨: 월드 좌표 기준으로 길이를 잰다
            float currentLength = GetWorldPathLength(polygonCollider.GetPath(i));
            if (currentLength > maxWorldPathLength)
            {
                maxWorldPathLength = currentLength;
                bestPathIndex = i;
            }
        }

        if (bestPathIndex == -1) return;

        // ★ [수정됨] 월드 전체 길이를 기준으로 간격 계산
        if (useFixedCount && targetSphereCount > 0)
        {
            spacing = maxWorldPathLength / targetSphereCount;
            // 로그로 실제 계산된 간격 확인
            Debug.Log($"[Info] 월드 전체 길이: {maxWorldPathLength}, 목표 개수: {targetSphereCount}, 간격: {spacing}");
        }

        // 2. 스피어 생성 로직
        Vector2[] pathPoints = polygonCollider.GetPath(bestPathIndex);

        int sphereCount = 0;
        Vector2 lastPos = transform.TransformPoint(pathPoints[0]);

        sphereCount++;
        CreateSphere(lastPos, sphereCount);

        float distanceTravelled = 0f;

        for (int j = 0; j < pathPoints.Length; j++)
        {
            Vector2 start = transform.TransformPoint(pathPoints[j]);
            Vector2 end = transform.TransformPoint(pathPoints[(j + 1) % pathPoints.Length]);

            float segmentDistance = Vector3.Distance(start, end);

            // 무한 루프 방지용 안전장치 (간격이 너무 작을 경우)
            if (spacing <= 0.001f) spacing = 0.1f;

            while (distanceTravelled + segmentDistance >= spacing)
            {
                float remainingDist = spacing - distanceTravelled;
                Vector3 newPos = Vector3.MoveTowards(start, end, remainingDist);

                // 목표 개수 초과 방지
                if (useFixedCount && sphereCount >= targetSphereCount) break;

                sphereCount++;
                CreateSphere(newPos, sphereCount);

                start = newPos;
                segmentDistance -= remainingDist;
                distanceTravelled = 0f;
            }

            distanceTravelled += segmentDistance;
        }

        SetupMapCamera();
    }

    // ★ [핵심 수정] 로컬 좌표가 아닌 월드 좌표로 변환해서 길이를 재는 함수
    float GetWorldPathLength(Vector2[] localPoints)
    {
        float length = 0f;
        if (localPoints.Length < 2) return 0f;

        for (int i = 0; i < localPoints.Length; i++)
        {
            // 점들을 월드 좌표로 변환 (스케일 적용됨)
            Vector2 p1 = transform.TransformPoint(localPoints[i]);
            Vector2 p2 = transform.TransformPoint(localPoints[(i + 1) % localPoints.Length]);

            length += Vector2.Distance(p1, p2);
        }
        return length;
    }

    [ContextMenu("맵 지우기")]
    public void ClearSpheres()
    {
        var children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (!child.name.Contains("MapCamera"))
                children.Add(child.gameObject);
        }
        children.ForEach(child => DestroyImmediate(child));

        if (GetComponent<SpriteRenderer>() != null)
        {
            GetComponent<SpriteRenderer>().enabled = true;
        }
    }

    void CreateSphere(Vector3 position, int number)
    {
        float randomDepth = Random.Range(-randomRange, randomRange);
        Vector3 finalPos = position + (transform.forward * randomDepth);

        GameObject obj = Instantiate(spherePrefab, finalPos, Quaternion.identity, transform);
        obj.name = "Emotion_" + number;
        obj.transform.localScale = Vector3.one * scaleFactor;
    }

    [ContextMenu("씨네머신 카메라 생성 / 설정")]
    public void SetupMapCamera()
    {
        GameObject camObj = null;
        Transform existingCam = transform.Find("MapCamera_Generated");

        if (existingCam != null)
        {
            camObj = existingCam.gameObject;
        }
        else
        {
            camObj = new GameObject("MapCamera_Generated");
            camObj.transform.SetParent(transform);
        }

        CinemachineCamera cam = camObj.GetComponent<CinemachineCamera>();
        if (cam == null) cam = camObj.AddComponent<CinemachineCamera>();

        camObj.transform.localPosition = new Vector3(0, 0, -cameraDistance);
        camObj.transform.localRotation = Quaternion.identity;

        cam.Lens.FieldOfView = 1f;
        cam.Lens.NearClipPlane = 0.1f;
        cam.Lens.FarClipPlane = cameraDistance * 2.5f;
    }
}