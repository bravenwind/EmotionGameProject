using System.Collections.Generic;
using UnityEngine;

public class Debug_ShapeActiveManager : MonoBehaviour
{
    // 인스펙터에서 보기 좋게 설정하기 위한 구조체
    [System.Serializable]
    public struct ShapeDebugSet
    {
        public string label;        // 알아보기 쉬운 이름 (예: Heart)
        public KeyCode key;         // 누를 키 (예: H)
        public PathManager manager; // 연결할 스크립트
    }

    [Header("모양 디버그 설정")]
    public List<ShapeDebugSet> shapeSets; // 리스트로 관리

    void Update()
    {
        // 등록된 모든 세트를 순회하며 키 입력을 체크
        foreach (var set in shapeSets)
        {
            if (Input.GetKeyDown(set.key))
            {
                set.manager.FinishPath();
                //ActivateOnly(set.manager);
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            DataManager.Instance.SwitchToPlayerCamera();
        }
    }

    // ★ 핵심 로직: 선택된 녀석만 켜고, 나머지는 다 끈다
    public void ActivateOnly(PathManager targetManager)
    {
        bool foundTarget = false;

        foreach (var set in shapeSets)
        {
            if (set.manager == null) continue;

            if (set.manager == targetManager)
            {
                // 타겟 발견!
                set.manager.gameObject.SetActive(true);
                DataManager.Instance.SwitchToMapCamera(); // 카메라 전환
                foundTarget = true;
            }
            else
            {
                // 타겟 아님: 끄기
                set.manager.gameObject.SetActive(false);
                // 혹시 모르니 카메라 우선순위도 확실히 낮춤
                if (set.manager.mapCam != null) set.manager.mapCam.Priority = 0;
            }
        }

        if (!foundTarget)
        {
            Debug.LogError($"[오류] 리스트에 {targetManager.name}가 등록되어 있지 않습니다!");
        }
    }
}