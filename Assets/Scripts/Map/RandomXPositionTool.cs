using UnityEngine;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomXPositionTool : MonoBehaviour
{
    [Header("X축 랜덤 범위")]
    public float minX = -10.0f;
    public float maxX = 10.0f;

    [Header("오브젝트")]
    public GameObject objectPrefab;
    public List<Transform> objectTransforms;

    // ---------------------------------------------------------
    // 3. Z축만 랜덤 변경
    // ---------------------------------------------------------
    [ContextMenu("랜덤 배치 실행 (X축만)")]
    private void RandomizeX()
    {
        ApplyRandomPosition(true, false, false);
    }

    //private void CreateAndApplyYandZ()
    //{
    //    if (objectTransforms.Count == 0)
    //    {
    //        Debug.LogWarning("선택된 오브젝트가 없습니다.");
    //        return;
    //    }
    //    foreach (Transform t in objectTransforms)
    //    {
    //        Vector3 pos = t.position;

    //        float newX = pos.x;
    //        float newY = pos.y;
    //        float newZ = pos.z;

    //        pos = new Vector3(newX, newY, newZ);

    //        Instantiate(objectPrefab, pos, Quaternion.identity);
    //    }
    //}

    // 실제 로직을 처리하는 함수
    private void ApplyRandomPosition(bool changeX, bool changeY, bool changeZ)
    {
#if UNITY_EDITOR
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("선택된 오브젝트가 없습니다.");
            return;
        }

        // 실행 취소(Ctrl+Z) 등록
        Undo.RecordObjects(selectedObjects, "Randomize Position");

        int count = 0;
        foreach (GameObject obj in selectedObjects)
        {
            // 도구 자신은 제외
            if (obj == this.gameObject) continue;

            Vector3 pos = obj.transform.position;

            // 각 축별로 변경 여부에 따라 새 값 할당
            float newX = changeX ? Random.Range(minX, maxX) : pos.x;
            float newY = pos.y;
            float newZ = pos.z;

            obj.transform.position = new Vector3(newX, newY, newZ);
            count++;
        }

        Debug.Log($"총 {count}개 오브젝트 위치 변경 완료 (X:{changeX}, Y:{changeY}, Z:{changeZ})");
#endif
    }
}