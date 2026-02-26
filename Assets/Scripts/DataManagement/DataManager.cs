using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public enum EmotionState
{
    Happy = 0,
    Hope = 1,
    Angry = 2,
    Sad = 3
}

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public EmotionState targetEmotion = EmotionState.Happy;

    [Header("현재 선택된 캐릭터 (자동 할당)")]
    public GameObject selectedCharacter;

    [Header("플레이어 참조 컴포넌트")]
    public CinemachineCamera playerCam;
    public MouseLook mouseLook;
    public Transform playerTransform;
    public Transform playerLineTransform;
    public Animator playerAnimator;
    public ZeroGravityMovement playerMovementScript;
    public CinemachineBrain brain;

    [Header("맵")]
    public CinemachineCamera targetMapCam;

    [Header("제한시간")]
    public float limitTime = 300f;
    public float targetTime = 120f;
    public float currentTime;

    [Header("게임 완료")]
    public bool gameEnded = false;
    public bool gameCleared = false;
    public ResultStarsUI resultStarsUI;

    [Header("감정 게이지")]
    public float maxEmotionScore;
    public float currentEmotionScore = 0;

    [Header("커지는 캐릭터")]
    public float playerOriginalScale = 1;
    public int maxScaleLevel = 5;
    public List<float> playerScalePerLevel = new List<float>(5);
    public int emotionForLevelUp = 5;
    public int currentEmotionCount = 0;
    public int currentScaleLevel = 1;
    public float scaleIncreaseDuration = 1.5f;

    public Vector3 happyTextureOffset = new Vector3(0, 2f, 0); // 머리 위 2미터를 바라보게 설정
    public Vector3 hopeTextureOffset = new Vector3(-85, 196.399994f, -201.800003f); // 머리 위 2미터를 바라보게 설정
    public Vector3 angryTextureOffset = new Vector3(-85, 196.399994f, -201.800003f); // 머리 위 2미터를 바라보게 설정
    public Vector3 sadTextureOffset = new Vector3(0, 2f, 0); // 머리 위 2미터를 바라보게 설정

    [Header("게임 오버")]
    public CinemachineCamera camToClearFail;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ResetGameData();
    }

    public void SwitchToMapCamera()
    {
        DataManager.Instance.mouseLook.enabled = false;
        DataManager.Instance.playerCam.transform.rotation = targetMapCam.transform.rotation;
        if (DataManager.Instance.playerCam != null && targetMapCam != null)
        {
            DataManager.Instance.playerCam.Priority = 0;
            targetMapCam.Priority = 10;
        }
    }

    public void SwitchToPlayerCamera()
    {
        DataManager.Instance.mouseLook.enabled = true;
        if (DataManager.Instance.playerCam != null && targetMapCam != null)
        {
            DataManager.Instance.playerCam.Priority = 10;
            targetMapCam.Priority = 0;
        }
    }

    public IEnumerator GameOver()
    {
        DisableAllChild.Instance.DisableAll();
        GameSceneUIManager.Instance.SetAllDisable();
        SwitchToMapCamera();
        yield return StartCoroutine(WaitForBlendToFinish());
        Debug.Log("맵 카메라로 전환 완료");

        yield return GameSceneUIManager.Instance.StartCoroutine(GameSceneUIManager.Instance.WaitForEpilogue());
    }

    IEnumerator WaitForBlendToFinish()
    {
        yield return new WaitForSecondsRealtime(1f);
        while (brain.IsBlending)
        {

            Debug.Log("블렌딩 중");
            yield return null;

        }
    }

    public IEnumerator AfterEpilogue()
    {
        // [4. 카메라 바꿈]
        camToClearFail.Priority = 20;
        yield return StartCoroutine(WaitForBlendToFinish());
        Debug.Log("맵 카메라2로 전환 완료");

        // [5. 캐릭터 지정된 위치로 이동하면서 커짐]
        // 🚨 핵심 수정: 코루틴이 끝날 때까지 기다립니다!
        yield return StartCoroutine(CharacterFocus.Instance.AnimateCharacter());
        Debug.Log("캐릭터 키우기 완료");

        // [6. 결과 UI 내려옴 및 애니메이션 재생]
        if (DataManager.Instance.gameCleared)
        {
            resultStarsUI.SetStarIndex(3);
            DataManager.Instance.playerAnimator.SetTrigger("GameClear");
            PlaySFXAudio.Instance.PlayMissionComplete();
        }
        else
        {
            resultStarsUI.SetStarIndex(0);
            DataManager.Instance.playerAnimator.SetTrigger("GameFail");
            PlaySFXAudio.Instance.PlayFail();
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // CharacterFocus에서 지웠던 마무리 작업을 여기서 최종 호출합니다.
        OnTransitionComplete();
    }

    public void OnTransitionComplete()
    {
        if (GameSceneUIManager.Instance != null)
        {
            playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            CharacterFocus.Instance.ApplyAnimationOnCharacter();
            GameSceneUIManager.Instance.SetState(GameSceneUIState.GameOver);
        }
    }

    public void ResetGameData()
    {
        currentEmotionScore = 0;
        currentTime = limitTime;
        currentEmotionCount = 0;
        currentScaleLevel = 1;
    }
}
