using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using System.Threading;
using Unity.Cinemachine;
using Unity.VisualScripting;

// 1. 상태를 정의하는 Enum (필요에 따라 추가/수정하세요)
public enum GameSceneUIState
{
    Prologue = 0,
    InGame = 1,     // 게임 플레이 중 HUD
    Settings = 2,
    GameOver = 3,
    None
}

public enum FadeState
{
    FadeIn = 0,
    FadeOut = 1
}

public class GameSceneUIManager : MonoBehaviour
{
    public static GameSceneUIManager Instance { get; private set; }

    // 2. 인스펙터에서 Enum과 오브젝트를 짝지을 수 있게 만든 클래스
    [System.Serializable]
    public class UIStateMapping
    {
        public GameSceneUIState state;       // 어떤 상태일 때?
        public GameObject uiObject; // 어떤 UI를 켤 것인가?
    }

    [Header("UI 등록 설정")]
    // 이 리스트에 UI 오브젝트들을 등록하고 Enum을 지정하세요.
    public List<UIStateMapping> uiList = new List<UIStateMapping>();

    [Header("초기 상태")]
    public GameSceneUIState startState = GameSceneUIState.Prologue;
    public CanvasGroup prologuePanel;

    [Header("UI 설정")]
    [Tooltip("화면을 가릴 검은색 이미지의 CanvasGroup 컴포넌트")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("페이드 인 되는 시간 (초)")]

    public float fadeDuration = 1.0f;

    // 현재 상태를 저장하는 변수
    public GameSceneUIState currentState;

    [Header("프롤로그")]
    [SerializeField] private Sprite prologue_Happy;
    [SerializeField] private Sprite prologue_Hope;
    [SerializeField] private Sprite prologue_Angry;
    [SerializeField] private Sprite prologue_Sad;

    [SerializeField] private float prologueScale_Happy;
    [SerializeField] private float prologueScale_Hope;
    [SerializeField] private float prologueScale_Angry;
    [SerializeField] private float prologueScale_Sad;

    [Header("감정 아이콘")]
    [SerializeField] private Sprite happyIcon;
    [SerializeField] private Sprite hopeIcon;
    [SerializeField] private Sprite angryIcon;
    [SerializeField] private Sprite sadIcon;

    [SerializeField] private float happyIconScale;
    [SerializeField] private float hopeIconScale;
    [SerializeField] private float angryIconScale;
    [SerializeField] private float sadIconScale;

    [SerializeField] private Image iconImage;

    [SerializeField] private Image[] prologueBlackImages;

    [Header("미션 텍스트")]
    [SerializeField] private ResultStarsUI resultStarsUI;

    [SerializeField] private TMP_Text mission1Text;
    [SerializeField] private TMP_Text mission2Text;
    [SerializeField] private TMP_Text mission3Text;

    [Header("감정 점수 이미지")]
    [SerializeField] private Image emotionScoreFillImage;

    [Header("게임 오버")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private bool gameCleared;
    [SerializeField] private bool epilogueActived;
    [SerializeField] private bool epilogueEnded;
    [SerializeField] private CanvasGroup epilogueImage;

    private bool isEpilogueRoutineStarted = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 시작하자마자 검은 화면(Alpha 1)으로 세팅하고 시작해야 자연스럽습니다.
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.gameObject.SetActive(true);
        }

        if (emotionScoreFillImage != null)
        {
            emotionScoreFillImage.fillAmount = 0.0f;
        }
        StartCoroutine(SceneFade(FadeState.FadeIn));
        SetState(startState);
    }

    public void FinishPrologue()
    {
        if (prologueBlackImages != null) 
        {
            foreach (Image image in prologueBlackImages)
            {
                image.gameObject.SetActive(false);
            }
        }

        StartCoroutine(ProcessPrologueEnd());
    }

    private IEnumerator ProcessPrologueEnd()
    {
        // 2. 상태 변경 (인게임)
        SetState(GameSceneUIState.InGame);

        // 3. 다시 화면 밝아짐 (FadeIn = Alpha 1 -> 0)
        yield return StartCoroutine(Fade(prologuePanel, FadeState.FadeIn, fadeDuration));
        prologuePanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        // 매 프레임 현재 상태에 따른 로직 실행
        UpdateState();
    }

    // ==========================================
    // 핵심 기능 1: 상태 변경 (SetState)
    // ==========================================
    public void SetState(GameSceneUIState newState)
    {
        currentState = newState;
        Debug.Log($"상태 변경: {currentState}");

        // 등록된 모든 UI를 순회하며 상태에 맞는 것만 켜고, 나머지는 끕니다.
        SetOnlyState(newState);

        // 상태 진입 시 1회성 로직이 필요하다면 여기에 작성 (예: 점수 초기화 등)
        OnEnterState(newState);
    }

    public void SetOnlyState(GameSceneUIState newState)
    {
        foreach (var mapping in uiList)
        {
            if (mapping.uiObject != null)
            {
                // 현재 상태와 매핑된 상태가 같으면 true(켜짐), 아니면 false(꺼짐)
                bool isActive = (mapping.state == newState);
                mapping.uiObject.SetActive(isActive);
            }
        }
    }
    public void SetAllDisable()
    {
        foreach (var mapping in uiList)
        {
            if (mapping.uiObject != null)
            {
                mapping.uiObject.SetActive(false);
            }
        }
    }

    // ==========================================
    // 핵심 기능 2: 상태별 프레임 로직 (UpdateState)
    // ==========================================
    private void UpdateState()
    {
        switch (currentState)
        {
            //case UIState.MainMenu:
            //    // 메인 메뉴에서의 로직 (예: 아무 키나 누르면 게임 시작)
            //    if (Input.GetKeyDown(KeyCode.Space))
            //    {
            //        Debug.Log("게임 시작!");
            //        SetState(UIState.InGame);
            //    }
            //    break;

            case GameSceneUIState.InGame:
                // 게임 중 로직 (예: ESC 누르면 일시정지/설정)
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    SetState(GameSceneUIState.Settings);
                }
                break;

            case GameSceneUIState.Settings:
                // 설정 창 로직 (예: ESC 누르면 다시 게임으로)
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    SetState(GameSceneUIState.InGame);
                }
                break;

            case GameSceneUIState.GameOver:

                if (gameCleared && !epilogueActived && !isEpilogueRoutineStarted)
                {
                    StartCoroutine(WaitForEpilogue());
                }

                // 에필로그가 활성화(이미지가 다 뜸) 되었고, 아직 종료(클릭) 안 했을 때
                if (epilogueActived && !epilogueEnded)
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        // 클릭 시 에필로그 끄기 (FadeIn = Alpha 1 -> 0)
                        StartCoroutine(Fade(epilogueImage, FadeState.FadeIn, fadeDuration));
                        epilogueEnded = true;
                    }
                }

                break;
            case GameSceneUIState.Prologue:
                break;
        }
    }

    private void UpdateEmotionUI(Image targetImage, Sprite happy, float happyScale, Sprite hope, float hopeScale, Sprite angry, float angryScale, Sprite sad, float sadScale)
    {
        if (targetImage == null) return;

        EmotionState currentEmotion = DataManager.Instance.targetEmotion;
        Sprite selectedSprite = null;
        float selectedScale = 1f;

        switch (currentEmotion)
        {
            case EmotionState.Happy:
                selectedSprite = happy;
                selectedScale = happyScale;
                break;
            case EmotionState.Hope:
                selectedSprite = hope;
                selectedScale = hopeScale;
                break;
            case EmotionState.Angry:
                selectedSprite = angry;
                selectedScale = angryScale;
                break;
            case EmotionState.Sad:
                selectedSprite = sad;
                selectedScale = sadScale;
                break;
        }

        targetImage.sprite = selectedSprite;
        targetImage.rectTransform.localScale = Vector3.one * selectedScale;
        targetImage.SetNativeSize();
    }

    private void OnEnterState(GameSceneUIState state)
    {
        switch (state)
        {
            case GameSceneUIState.InGame:
                Time.timeScale = 1f;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                // 공통 함수 사용 (아이콘 설정)
                UpdateEmotionUI(iconImage, happyIcon, happyIconScale, hopeIcon, hopeIconScale, angryIcon, angryIconScale, sadIcon, sadIconScale);
                GUI.enabled = true;
                break;

            case GameSceneUIState.Settings:
                Time.timeScale = 0f;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                GUI.enabled = false;
                break;

            case GameSceneUIState.Prologue:
                Time.timeScale = 0f;
                // 공통 함수 사용 (프롤로그 이미지 설정)
                //UpdateEmotionUI(prologueImage, prologue_Happy, prologueScale_Happy, prologue_Hope, prologueScale_Hope, prologue_Angry, prologueScale_Angry, prologue_Sad, prologueScale_Sad);
                break;

            case GameSceneUIState.GameOver:
                Time.timeScale = 0f;
                DataManager.Instance.mouseLook.enabled = false;
                CharacterFocus.Instance.ApplyAnimationOnCharacter();
                playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

                mission1Text.text = DataManager.Instance.mission1;
                mission2Text.text = DataManager.Instance.mission2;
                mission3Text.text = DataManager.Instance.mission3;

                if (DataManager.Instance.limitTime - DataManager.Instance.currentTime <= DataManager.Instance.targetTime)
                {
                    DataManager.Instance.missionDict[DataManager.Instance.mission3] = true;
                    DataManager.Instance.mission3Success = true;
                }

                int successedCount = 0;

                foreach (bool successed in DataManager.Instance.missionDict.Values)
                {
                    if (successed) 
                    {
                        successedCount++;
                    }
                }

                if (successedCount == 0)
                {
                    gameCleared = false;
                    DataManager.Instance.playerAnimator.SetTrigger("GameFail");
                    PlaySFXAudio.Instance.PlayFail();
                }
                else if (successedCount >= 2)
                {
                    gameCleared = true;
                    DataManager.Instance.playerAnimator.SetTrigger("GameClear");
                    PlaySFXAudio.Instance.PlayMissionComplete();
                }
                resultStarsUI.SetStarIndex(successedCount);

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
        }
    }

    public IEnumerator SceneFade(FadeState fadeInOut)
    {
        if (fadeCanvasGroup == null) yield break;
        fadeCanvasGroup.blocksRaycasts = true; // 입력 차단 시작

        bool fadeIn = fadeInOut == FadeState.FadeIn;
        bool fadeOut = fadeInOut == FadeState.FadeOut;

        yield return StartCoroutine(Fade(fadeCanvasGroup, fadeInOut, fadeDuration));

        if (fadeIn)
        {
            fadeCanvasGroup.alpha = 0f; // 확실하게 투명하게
            fadeCanvasGroup.blocksRaycasts = false; // [중요!] 페이드인이 끝나면 반드시 입력을 다시 허용해야 합니다.
        }
        if (fadeOut)
        {
            //SceneManager.LoadScene("Game");
        }
    }

    public void StartFade(FadeStateComponent fadeStateComponent)
    {
        FadeState fadeState = fadeStateComponent.fadeState;
        StartCoroutine(SceneFade(fadeState));
    }

    public IEnumerator Fade(CanvasGroup fadeCanvasGroup, FadeState fadeInOut, float fadeDuration)
    {
        fadeCanvasGroup.gameObject.SetActive(true);

        // 시작/목표 알파값 정의
        float startAlpha = (fadeInOut == FadeState.FadeIn) ? 1f : 0f;
        float endAlpha = (fadeInOut == FadeState.FadeIn) ? 0f : 1f;

        // 초기값 적용
        fadeCanvasGroup.alpha = startAlpha;

        // 0.5초 대기 (이 시간 동안 렉이 걸리든 말든 상관없음)
        yield return new WaitForSecondsRealtime(0.5f);

        if (fadeDuration > 0f)
        {
            // [해결 핵심] 누적(+=) 방식 대신, 현재 시각을 기록합니다.
            // 0.5초 대기가 끝난 '지금'이 바로 애니메이션 시작 시점입니다.
            float startTime = Time.unscaledTime;

            // 경과 시간이 duration보다 작은 동안 반복
            while (Time.unscaledTime < startTime + fadeDuration)
            {
                // [해결 핵심] (현재 시간 - 시작 시간)으로 순수한 경과 시간을 구합니다.
                // 이렇게 하면 이전 프레임의 델타타임이 아무리 커도 영향을 받지 않습니다.
                float timer = Time.unscaledTime - startTime;

                float progress = timer / fadeDuration;

                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);

                yield return null;
            }
        }

        // 최종값 확정
        fadeCanvasGroup.alpha = endAlpha;

        if (fadeInOut == FadeState.FadeIn)
        {
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }

    public void UpdateEmotionScoreImage()
    {
        emotionScoreFillImage.fillAmount = DataManager.Instance.currentEmotionScore / DataManager.Instance.maxEmotionScore;
    }

    public IEnumerator WaitForEpilogue()
    {
        // 1. 중복 실행 방지 플래그 On
        isEpilogueRoutineStarted = true;

        // 2. 3초 대기
        yield return new WaitForSecondsRealtime(3.0f);

        // 3. 페이드 효과 실행 (FadeOut = Alpha 0 -> 1 : 이미지 나타남)
        // yield return을 써서 페이드가 끝날 때까지 기다릴 수도 있고,
        // 그냥 실행만 시킬 수도 있습니다. 여기선 실행만 시켜도 무방합니다.
        yield return StartCoroutine(Fade(epilogueImage, FadeState.FadeOut, fadeDuration));

        // 4. 입력 허용 (Update에서 클릭 체크 시작)
        epilogueActived = true;
    }
}