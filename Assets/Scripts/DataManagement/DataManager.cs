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
    public CharacterFocus characterFocus;

    [Header("맵")]
    public CinemachineCamera targetMapCam;
    [Tooltip("현재 플레이 중인 감정의 경로 (PathManager가 자동 등록)")]
    public PathManager targetPathManager;

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

    public event System.Action<float> OnEmotionScoreChanged;

    [SerializeField]
    private float _currentEmotionScore = 0;
    public float currentEmotionScore
    {
        get => _currentEmotionScore;
        set
        {
            _currentEmotionScore = value;
            OnEmotionScoreChanged?.Invoke(_currentEmotionScore);
        }
    }

    [Header("커지는 캐릭터")]
    public float playerOriginalScale = 1;
    public int maxScaleLevel = 5;
    public List<float> playerScalePerLevel = new List<float>(5);
    public int emotionForLevelUp = 5;
    public int currentEmotionCount = 0;
    public int currentScaleLevel = 1;
    public float scaleIncreaseDuration = 1.5f;

    [Header("게임 시작 인트로 카메라")]
    [Tooltip("첫번째 노드 앞에 설치한 씨네머신 카메라")]
    public CinemachineCamera introCam;
    [Tooltip("인트로 카메라에서 대기하는 시간(초)")]
    public float introCameraWaitTime = 3f;

    [Header("게임 오버")]
    public CinemachineCamera camToClearFail;
    [Tooltip("실패 시 맵 카메라에서 대기하는 시간(초)")]
    public float failMapCameraWaitTime = 2f;

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

    public IEnumerator StartGameWithIntroCamera()
    {
        // Cinemachine 블렌딩이 동작하도록 시간 진행
        Time.timeScale = 1f;

        // 컴포넌트를 끄면 카메라 추적까지 멈춰서 연출 후 시점이 튄다.
        // 마우스 입력만 잠그고 카메라는 계속 캐릭터를 따라가게 둔다.
        mouseLook.DisableInput();

        if (introCam != null)
        {
            // 씬 시작부터 introCam이 활성화된 상태이므로 바로 n초 대기
            yield return new WaitForSecondsRealtime(introCameraWaitTime);

            // 플레이어 카메라로 전환
            introCam.Priority = 0;
            yield return StartCoroutine(WaitForBlendToFinish());
            Debug.Log("플레이어 카메라로 전환 완료");
        }

        // 연출 중 흘러간 시점을 시작 각도로 복구한 뒤 조작권을 넘긴다.
        mouseLook.EnableInput();
        GameSceneUIManager.Instance.SetState(GameSceneUIState.InGame);
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
        DataManager.Instance.mouseLook.SuppressInput();
        if (DataManager.Instance.playerCam != null && targetMapCam != null)
        {
            DataManager.Instance.playerCam.Priority = 10;
            targetMapCam.Priority = 0;
        }
    }

    private bool isGameOverRunning = false;

    public IEnumerator GameOver()
    {
        // 클리어/실패 연출이 이미 돌고 있으면 중복 실행하지 않는다.
        if (isGameOverRunning) yield break;
        isGameOverRunning = true;

        Collider[] characterCols = selectedCharacter.GetComponentsInChildren<Collider>();
        foreach (Collider col in characterCols) 
        {
            col.enabled = false;
        }

        DisableAllChild.Instance.DisableAll();
        GameSceneUIManager.Instance.SetAllDisable();
        SwitchToMapCamera();
        yield return StartCoroutine(WaitForBlendToFinish());
        Debug.Log("맵 카메라로 전환 완료");

        if (gameCleared)
        {
            // 클리어 성공: 에필로그 표시 후 결과 화면
            yield return GameSceneUIManager.Instance.StartCoroutine(GameSceneUIManager.Instance.WaitForEpilogue());
        }
        else
        {
            // 클리어 실패: n초 대기 후 바로 결과 화면
            yield return new WaitForSecondsRealtime(failMapCameraWaitTime);
            yield return StartCoroutine(AfterEpilogue());
        }
    }

    IEnumerator WaitForBlendToFinish()
    {
        yield return new WaitForSecondsRealtime(1f);
        while (brain.IsBlending)
        {
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
        yield return StartCoroutine(characterFocus.AnimateCharacter());
        Debug.Log("캐릭터 키우기 완료");

        // [6. 결과 UI 내려옴 및 애니메이션 재생]
        if (gameCleared)
        {
            resultStarsUI.SetStarIndex(3);
            playerAnimator.SetTrigger("GameClear");
            PlaySFXAudio.Instance.PlayMissionComplete();
        }
        else
        {
            resultStarsUI.SetStarIndex(0);
            playerAnimator.SetTrigger("GameFail");
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
            characterFocus.ApplyAnimationOnCharacter();
            GameSceneUIManager.Instance.SetState(GameSceneUIState.GameOver);
        }
    }

    /// <summary>
    /// 새 판을 시작할 때의 초기화. DataManager는 DontDestroyOnLoad라 이전 판의 상태가 남으므로
    /// 종료 플래그까지 반드시 함께 되돌려야 한다.
    /// </summary>
    public void ResetGameData()
    {
        currentEmotionScore = 0;
        currentTime = limitTime;
        currentEmotionCount = 0;
        currentScaleLevel = 1;

        gameEnded = false;
        gameCleared = false;
        isGameOverRunning = false;
    }
}
