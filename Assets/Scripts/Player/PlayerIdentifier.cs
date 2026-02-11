using Unity.Cinemachine;
using UnityEngine;

public class PlayerIdentifier : MonoBehaviour
{
    [Header("씬에 배치된 캐릭터 오브젝트 연결")]
    public GameObject charToUse;

    private void Start()
    {
        // 4. 캐릭터 위치 설정 및 활성화
        if (charToUse != null)
        {
            // 4-3. DataManager에 현재 선택된 캐릭터 정보 등록
            DataManager.Instance.selectedCharacter = charToUse;

            // 5. 플레이어 관련 컴포넌트 정보 DataManager에 갱신
            DataManager.Instance.playerTransform = charToUse.transform;
            DataManager.Instance.playerCam = charToUse.GetComponentInChildren<CinemachineCamera>();
            DataManager.Instance.mouseLook = charToUse.GetComponentInChildren<MouseLook>();
            DataManager.Instance.playerLineTransform = charToUse.transform.Find("LineTransform");
            DataManager.Instance.playerAnimator = charToUse.GetComponentInChildren<Animator>();
        }
        else
        {
            Debug.LogError("PlayerSpawn 스크립트에 캐릭터 오브젝트가 연결되지 않았습니다.");
        }
    }
}