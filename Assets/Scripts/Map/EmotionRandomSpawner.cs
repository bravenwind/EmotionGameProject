using UnityEngine;

public class EmotionRandomSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject emotionPrefab;

    [SerializeField]
    private Transform emotionSpawnerTransform;

    [SerializeField]
    private int spawnCount = 100;

    [Header("설정")]
    [SerializeField, Tooltip("스폰 영역의 반지름")]
    private float spawnRadius = 3.0f; // 구의 크기 (반지름)

    [ContextMenu("랜덤으로 감정 스폰")]
    void SpawnEmotionRandomly()
    {
        for (int i = 0; i < spawnCount; i++) 
        {
            if (emotionSpawnerTransform != null && emotionPrefab != null)
            {
                // 1. 반지름 1인 구 내부의 랜덤한 위치(Vector3)를 가져옵니다.
                // 2. 여기에 우리가 설정한 반지름(spawnRadius)을 곱해 범위를 키웁니다.
                Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;

                // 3. 스포너의 현재 위치에 랜덤 오프셋을 더해 최종 위치를 결정합니다.
                Vector3 spawnPosition = emotionSpawnerTransform.position + randomOffset;

                // 4. 해당 위치에 프리팹을 생성합니다. (회전은 기본값인 Quaternion.identity)
                GameObject emotionGO = Instantiate(emotionPrefab, spawnPosition, Quaternion.identity);
                emotionGO.transform.SetParent(emotionSpawnerTransform);

                Debug.Log($"감정 스폰 완료: {spawnPosition}");
            }
            else
            {
                Debug.LogWarning("프리팹이나 스포너 Transform이 할당되지 않았습니다.");
            }

        }
    }

    [ContextMenu("모두 삭제")]
    void DeleteSpawnedEmotions()
    {
        if (emotionSpawnerTransform == null) return;

        // [중요] 삭제 로직 수정
        // 1. spawnCount가 아닌 실제 자식 개수(childCount)를 기준으로 합니다.
        // 2. 리스트를 수정하며 삭제할 때는 반드시 역순(뒤에서부터)으로 지워야 안전합니다.
        // 3. 자기 자신이 아닌, 자식 오브젝트만 정확히 타겟팅합니다.

        int childCount = emotionSpawnerTransform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            // 즉시 삭제는 DestroyImmediate를 사용합니다.
            DestroyImmediate(emotionSpawnerTransform.GetChild(i).gameObject);
        }

        Debug.Log("모든 자식 오브젝트 삭제 완료.");
    }

    // 에디터에서 스폰 범위를 눈으로 확인하기 위한 코드 (게임 실행엔 영향 없음)
    private void OnDrawGizmosSelected()
    {
        if (emotionSpawnerTransform != null)
        {
            Gizmos.color = Color.yellow; // 노란색 선으로 표시
            Gizmos.DrawWireSphere(emotionSpawnerTransform.position, spawnRadius);
        }
    }
}