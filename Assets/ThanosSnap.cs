using UnityEngine;
using System.Collections.Generic;

public class ThanosSnap : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("체크하면 파괴 대신 비활성화(Hide)만 합니다.")]
    public bool nonLethalMode = false;

    // 우클릭 메뉴에서 실행할 수 있도록 ContextMenu 추가
    [ContextMenu("Execute Snap (Play Mode Only)")]
    public void DoSnap()
    {
        // 1. 자식이 없으면 중단
        if (transform.childCount == 0)
        {
            Debug.Log("파괴할 자식 오브젝트가 없습니다.");
            return;
        }

        // 2. 모든 자식 오브젝트를 리스트에 담기
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }

        // 3. 리스트를 무작위로 섞기 (운명은 랜덤이어야 하니까요)
        Shuffle(children);

        // 4. 절반의 개수 계산
        int countToRemove = children.Count / 2;

        // 5. 절반 삭제 (또는 비활성화)
        for (int i = 0; i < countToRemove; i++)
        {
            GameObject victim = children[i];

            if (nonLethalMode)
            {
                victim.SetActive(false); // 단순히 끄기만 함
            }
            else
            {
                DestroyImmediate(victim); // 완전히 삭제 (메모리에서 제거)
            }
        }

        Debug.Log($"타노스 핑거 스냅! {children.Count}명 중 {countToRemove}명이 먼지가 되었습니다.");
    }

    // 리스트를 무작위로 섞는 함수 (Fisher-Yates Shuffle)
    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}