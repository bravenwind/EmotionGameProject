using Unity.Cinemachine;
using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    [Header("씬에 배치된 캐릭터 오브젝트 연결")]
    public GameObject happyCharacter;
    public GameObject hopeCharacter;
    public GameObject angryCharacter;
    public GameObject sadCharacter;
    public GameObject tempCharacter;

    private void Start()
    {
        // 1. 씬에 있는 오브젝트 정보를 DataManager에 주입(Injection)
        if (DataManager.Instance != null)
        {
            DataManager.Instance.happyCharacter = happyCharacter;
            DataManager.Instance.hopeCharacter = hopeCharacter;
            DataManager.Instance.angryCharacter = angryCharacter;
            DataManager.Instance.sadCharacter = sadCharacter;
        }

        // 2. 일단 모든 캐릭터 비활성화 (초기화)
        DeactivateAllLocalCharacters();

        // 3. 활성화할 캐릭터 결정 (DataManager의 감정 상태에 따라)
        GameObject charToActivate = null;

        switch (DataManager.Instance.targetEmotion)
        {
            case EmotionState.Happy:
                charToActivate = happyCharacter;
                break;
            case EmotionState.Hope:
                charToActivate = hopeCharacter;
                break;
            case EmotionState.Angry:
                charToActivate = angryCharacter;
                break;
            case EmotionState.Sad:
                charToActivate = sadCharacter;
                break;
        }

        // 4. 캐릭터 위치 설정 및 활성화
        if (charToActivate != null)
        {
            // 4-1. 위치 이동
            if (DataManager.Instance.spawnPoint != null)
            {
                charToActivate.transform.position = DataManager.Instance.spawnPoint.position;
                charToActivate.transform.rotation = DataManager.Instance.spawnPoint.rotation;
                Debug.Log("스폰 포인트 위치로 캐릭터 이동 완료");
            }
            else
            {
                Debug.Log("Spawn Point가 없어 기존 배치된 위치를 사용합니다.");
            }

            // 4-2. 오브젝트 활성화
            charToActivate.SetActive(true);
            tempCharacter.SetActive(false);

            // 4-3. DataManager에 현재 선택된 캐릭터 정보 등록
            DataManager.Instance.selectedCharacter = charToActivate;
            DataManager.Instance.selectedCharacter.name = "PlayerCharacter";

            // 5. 플레이어 관련 컴포넌트 정보 DataManager에 갱신
            DataManager.Instance.playerTransform = charToActivate.transform;
            DataManager.Instance.playerCam = charToActivate.GetComponentInChildren<CinemachineCamera>();
            DataManager.Instance.mouseLook = charToActivate.GetComponentInChildren<MouseLook>();
            DataManager.Instance.playerLineTransform = charToActivate.transform.Find("LineTransform");
        }
        else
        {
            Debug.LogError("PlayerSpawn 스크립트에 캐릭터 오브젝트가 연결되지 않았거나, 해당하는 감정이 없습니다.");
        }
    }

    // 로컬 변수를 이용해 끄는 함수 (DataManager를 거치지 않음)
    private void DeactivateAllLocalCharacters()
    {
        if (happyCharacter != null) happyCharacter.SetActive(false);
        if (hopeCharacter != null) hopeCharacter.SetActive(false);
        if (angryCharacter != null) angryCharacter.SetActive(false);
        if (sadCharacter != null) sadCharacter.SetActive(false);
    }
}