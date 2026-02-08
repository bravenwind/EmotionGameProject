using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PolygonCollider2D))]
public class OutLineSphereSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject spherePrefab;
    public float spacing = 0.5f;
    public float zRandomRange = 2.0f;
    public float scaleFactor = 1.0f;

    [ContextMenu("맵 소환")]
    void SpawnSpheresAlongCollider()
    {
        ClearSpheres();

        GetComponent<SpriteRenderer>().enabled = false;
        PolygonCollider2D polygonCollider = GetComponent<PolygonCollider2D>();

        // 1. 가장 긴 경로(Path) 찾기
        // (두꺼운 선 때문에 생기는 안쪽 테두리나, 자잘한 노이즈를 제거하기 위함)
        int bestPathIndex = -1;
        float maxPathLength = 0f;

        for (int i = 0; i < polygonCollider.pathCount; i++)
        {
            float currentLength = GetPathLength(polygonCollider.GetPath(i));
            if (currentLength > maxPathLength)
            {
                maxPathLength = currentLength;
                bestPathIndex = i;
            }
        }

        // 경로가 하나도 없으면 중단
        if (bestPathIndex == -1) return;


        // 2. 가장 긴 경로(바깥쪽 테두리) 하나에만 스피어 생성
        Vector2[] pathPoints = polygonCollider.GetPath(bestPathIndex);

        // 월드 좌표 변환 및 배치 시작
        Vector2 lastPos = transform.TransformPoint(pathPoints[0]);
        CreateSphere(lastPos);

        float distanceTravelled = 0f;

        for (int j = 0; j < pathPoints.Length; j++)
        {
            Vector2 start = transform.TransformPoint(pathPoints[j]);
            Vector2 end = transform.TransformPoint(pathPoints[(j + 1) % pathPoints.Length]);

            float segmentDistance = Vector3.Distance(start, end);

            while (distanceTravelled + segmentDistance >= spacing)
            {
                float remainingDist = spacing - distanceTravelled;
                Vector3 newPos = Vector3.MoveTowards(start, end, remainingDist);

                CreateSphere(newPos);

                start = newPos;
                segmentDistance -= remainingDist;
                distanceTravelled = 0f;
            }

            distanceTravelled += segmentDistance;
        }
    }

    // 경로의 전체 길이를 계산하는 함수
    float GetPathLength(Vector2[] points)
    {
        float length = 0f;
        if (points.Length < 2) return 0f;

        for (int i = 0; i < points.Length; i++)
        {
            // 현재 점과 다음 점 사이의 거리 누적
            length += Vector2.Distance(points[i], points[(i + 1) % points.Length]);
        }
        return length;
    }

    [ContextMenu("맵 지우기")]
    public void ClearSpheres()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        if (GetComponent<SpriteRenderer>() != null)
        {
            GetComponent<SpriteRenderer>().enabled = true;
        }
    }

    void CreateSphere(Vector3 position)
    {
        float randomZ = Random.Range(-zRandomRange, zRandomRange);
        Vector3 finalPos = new Vector3(position.x, position.y, randomZ);

        GameObject obj = Instantiate(spherePrefab, finalPos, Quaternion.identity, transform);
        obj.transform.localScale = Vector3.one * scaleFactor;
    }
}