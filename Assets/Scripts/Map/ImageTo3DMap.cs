using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Buffers.Text;

public class ImageTo3DMap : MonoBehaviour
{
    [Header("References")]
    public PathManager pathManager;
    public PathNode nodePrefab;
    public Texture2D sourceImage;

    [Header("Generation Settings")]
    [Range(1, 50)] public int resolutionStep = 2;
    [Range(0f, 1f)] public float whiteThreshold = 0.9f;
    public float pixelScale = 1.0f;
    public float depthMultiplier = 5f;
    public float prefabSize = 0.5f;

    [Header("Sorting (순서 설정)")]
    [Tooltip("체크하면 시작점부터 가장 가까운 점을 찾아가며 연결합니다.")]
    public bool usePathFinding = true;
    [Tooltip("점이 뚝 끊겼을 때 얼마나 멀리까지 연결할지 (픽셀 단위)")]
    public float maxConnectionDist = 10f;

    [Header("랜덤 노이즈 설정")]
    public bool enableRandomZ = true;        // 랜덤 높이 활성화 여부
    public float randomNoiseStrength = 1.0f; // 랜덤 범위 (+- 이 값만큼 랜덤)

    // 내부 연산용 클래스
    private class PixelPoint
    {
        public int x;
        public int y;
        public Color color;
        public Vector3 worldPos;
    }

    private Transform mapHolder;

    void Start()
    {
        GenerateLevel();
    }

    [ContextMenu("Generate Level")]
    public void GenerateLevel()
    {
        if (pathManager == null || nodePrefab == null || sourceImage == null) return;

        // 1. 초기화
        if (mapHolder != null)
        {
            if (Application.isPlaying) Destroy(mapHolder.gameObject);
            else DestroyImmediate(mapHolder.gameObject);
        }
        pathManager.pathNodes.Clear();

        mapHolder = new GameObject("Generated_Level").transform;
        mapHolder.parent = this.transform;
        mapHolder.localPosition = Vector3.zero;

        int width = sourceImage.width;
        int height = sourceImage.height;
        float offsetX = width / 2f * pixelScale;
        float offsetY = height / 2f * pixelScale;

        // 2. 유효한 모든 픽셀을 리스트에 담기 (순서 상관 없이 일단 수집)
        List<PixelPoint> rawPixels = new List<PixelPoint>();

        for (int x = 0; x < width; x += resolutionStep)
        {
            for (int y = 0; y < height; y += resolutionStep)
            {
                Color c = sourceImage.GetPixel(x, y);
                if (c.a < 0.1f || c.grayscale > whiteThreshold) continue;

                float zPos = c.grayscale * depthMultiplier;

                if (enableRandomZ)
                {
                    // -strength ~ +strength 사이의 랜덤 값을 더함
                    zPos += Random.Range(-randomNoiseStrength, randomNoiseStrength);
                }

                Vector3 position = new Vector3(
                    x * pixelScale - offsetX,
                y * pixelScale - offsetY,
                    zPos
                );

                PixelPoint p = new PixelPoint();
                p.x = x;
                p.y = y;
                p.color = c;
                p.worldPos = new Vector3(
                    x * pixelScale - offsetX,
                    y * pixelScale - offsetY,
                    zPos
                );

                rawPixels.Add(p);
            }
        }

        if (rawPixels.Count == 0) return;

        // 3. 거리 기반 정렬 (한붓그리기 알고리즘)
        List<PixelPoint> sortedPixels = new List<PixelPoint>();

        if (usePathFinding)
        {
            // 3-1. 시작점 찾기 (보통 가장 아래, 혹은 가장 왼쪽)
            // 여기서는 Y가 가장 낮고, 그 중 X가 가장 낮은 점을 시작점으로 잡음
            PixelPoint current = rawPixels.OrderBy(p => p.y).ThenBy(p => p.x).First();

            sortedPixels.Add(current);
            rawPixels.Remove(current);

            // 3-2. 가장 가까운 다음 점 찾기 반복
            while (rawPixels.Count > 0)
            {
                PixelPoint nearest = null;
                float minDist = float.MaxValue;

                // 남은 점들 중 현재 점과 가장 가까운 점 검색
                foreach (var p in rawPixels)
                {
                    // 3D 거리 대신 2D 그리드 거리(X,Y)로 계산해야 순서가 정확함
                    float d = Vector2.Distance(new Vector2(current.x, current.y), new Vector2(p.x, p.y));

                    if (d < minDist)
                    {
                        minDist = d;
                        nearest = p;
                    }
                }

                // 너무 멀면(끊긴 구간) 연결할지 말지 결정 (옵션)
                // if (minDist > maxConnectionDist) break; 

                if (nearest != null)
                {
                    sortedPixels.Add(nearest);
                    current = nearest;
                    rawPixels.Remove(nearest);
                }
                else
                {
                    break;
                }
            }
        }
        else
        {
            sortedPixels = rawPixels; // 정렬 안 함 (기존 방식)
        }

        // 4. 오브젝트 생성 및 매니저 등록
        for (int i = 0; i < sortedPixels.Count; i++)
        {
            PixelPoint p = sortedPixels[i];

            PathNode newNode = Instantiate(nodePrefab, p.worldPos, Quaternion.identity, mapHolder);
            newNode.name = $"Node_{i}";
            newNode.transform.localScale = Vector3.one * prefabSize;

            Renderer rend = newNode.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial.color = p.color;

            pathManager.pathNodes.Add(newNode);
        }

        Debug.Log($"생성 완료! {sortedPixels.Count}개의 노드 연결됨.");
        pathManager.InitializePath();
    }
}