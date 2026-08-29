using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

// 1. ���¸� �����ϴ� Enum (�ʿ信 ���� �߰�/�����ϼ���)
public enum GameSceneUIState
{
    Prologue = 0,
    InGame = 1,     // ���� �÷��� �� HUD
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

    // 2. �ν����Ϳ��� Enum�� ������Ʈ�� ¦���� �� �ְ� ���� Ŭ����
    [System.Serializable]
    public class UIStateMapping
    {
        public GameSceneUIState state;       // � ������ ��?
        public GameObject uiObject; // � UI�� �� ���ΰ�?
    }

    [Header("UI ��� ����")]
    // �� ����Ʈ�� UI ������Ʈ���� ����ϰ� Enum�� �����ϼ���.
    public List<UIStateMapping> uiList = new List<UIStateMapping>();

    [Header("�ʱ� ����")]
    public GameSceneUIState startState = GameSceneUIState.Prologue;
    public CanvasGroup prologuePanel;
    public GameObject manualPanel;

    [Header("UI ����")]
    [Tooltip("ȭ���� ���� ������ �̹����� CanvasGroup ������Ʈ")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("���̵� �� �Ǵ� �ð� (��)")]

    public float fadeDuration = 1.0f;

    // ���� ���¸� �����ϴ� ����
    public GameSceneUIState currentState;

    [Header("���ѷα�")]
    [SerializeField] private Sprite prologue_Happy;
    [SerializeField] private Sprite prologue_Hope;
    [SerializeField] private Sprite prologue_Angry;
    [SerializeField] private Sprite prologue_Sad;

    [SerializeField] private float prologueScale_Happy;
    [SerializeField] private float prologueScale_Hope;
    [SerializeField] private float prologueScale_Angry;
    [SerializeField] private float prologueScale_Sad;
    [SerializeField] private Image[] prologueBlackImages;

    // 감정 게이지 FillImage는 FillImage.cs에서 직접 관리

    [Header("���� ����")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private bool epilogueActived;
    [SerializeField] private bool epilogueEnded;
    [SerializeField] private CanvasGroup epilogueImage;
    [SerializeField] private ResultStarsUI resultStarsUI;

    [Header("����")]
    [SerializeField] private Image level1to2CutIn;
    [SerializeField] private Image level2to3CutIn;
    [SerializeField] private Image level3to4CutIn;

    private bool isEpilogueRoutineStarted = false;
    private bool isPrologueFinished = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        DataManager.Instance.resultStarsUI = resultStarsUI;

        // �������ڸ��� ���� ȭ��(Alpha 1)���� �����ϰ� �����ؾ� �ڿ��������ϴ�.
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.gameObject.SetActive(true);
        }

        StartCoroutine(SceneFade(FadeState.FadeIn));
        SetState(startState);
    }

    public void FinishPrologue()
    {
        if (isPrologueFinished) return;
        isPrologueFinished = true;

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
        // 프롤로그 패널 페이드 아웃 (Alpha 1 → 0)
        yield return StartCoroutine(Fade(prologuePanel, FadeState.FadeIn, fadeDuration));
        prologuePanel.gameObject.SetActive(false);

        // 커서 표시 (매뉴얼 조작용) - manualPanel은 OnEnterState(Prologue)에서 이미 활성화됨
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator ProcessEpilogueEnd()
    {
        // �ٽ� ȭ�� ����� (FadeIn = Alpha 1 -> 0)
        yield return StartCoroutine(Fade(epilogueImage, FadeState.FadeIn, fadeDuration));

        epilogueImage.gameObject.SetActive(false);

        DataManager.Instance.StartCoroutine(DataManager.Instance.AfterEpilogue());
    }

    private void Update()
    {
        // �� ������ ���� ���¿� ���� ���� ����
        UpdateState();

        if (epilogueActived)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                StartCoroutine(ProcessEpilogueEnd());
                epilogueActived = false;
            }
        }
    }

    // ==========================================
    // �ٽ� ��� 1: ���� ���� (SetState)
    // ==========================================
    public void SetState(GameSceneUIState newState)
    {
        currentState = newState;
        Debug.Log($"���� ����: {currentState}");

        // ��ϵ� ��� UI�� ��ȸ�ϸ� ���¿� �´� �͸� �Ѱ�, �������� ���ϴ�.
        SetOnlyState(newState);

        // ���� ���� �� 1ȸ�� ������ �ʿ��ϴٸ� ���⿡ �ۼ� (��: ���� �ʱ�ȭ ��)
        OnEnterState(newState);
    }

    public void SetOnlyState(GameSceneUIState newState)
    {
        foreach (var mapping in uiList)
        {
            if (mapping.uiObject != null)
            {
                // ���� ���¿� ���ε� ���°� ������ true(����), �ƴϸ� false(����)
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
    // �ٽ� ��� 2: ���º� ������ ���� (UpdateState)
    // ==========================================
    private void UpdateState()
    {
        switch (currentState)
        {
            case GameSceneUIState.InGame:
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (!manualPanel.activeSelf && !DataManager.Instance.gameEnded)
                    {
                        SetState(GameSceneUIState.Settings);
                    }
                }
                break;

            case GameSceneUIState.Settings:
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    SetState(GameSceneUIState.InGame);
                }
                break;

            case GameSceneUIState.GameOver:
                break;
            case GameSceneUIState.Prologue:
                break;
        }
    }

    //private void UpdateEmotionUI(Image targetImage, Sprite happy, float happyScale, Sprite hope, float hopeScale, Sprite angry, float angryScale, Sprite sad, float sadScale)
    //{
    //    if (targetImage == null) return;

    //    EmotionState currentEmotion = DataManager.Instance.targetEmotion;
    //    Sprite selectedSprite = null;
    //    float selectedScale = 1f;

    //    switch (currentEmotion)
    //    {
    //        case EmotionState.Happy:
    //            selectedSprite = happy;
    //            selectedScale = happyScale;
    //            break;
    //        case EmotionState.Hope:
    //            selectedSprite = hope;
    //            selectedScale = hopeScale;
    //            break;
    //        case EmotionState.Angry:
    //            selectedSprite = angry;
    //            selectedScale = angryScale;
    //            break;
    //        case EmotionState.Sad:
    //            selectedSprite = sad;
    //            selectedScale = sadScale;
    //            break;
    //    }

    //    targetImage.sprite = selectedSprite;
    //    targetImage.rectTransform.localScale = Vector3.one * selectedScale;
    //    targetImage.SetNativeSize();
    //}

    private void OnEnterState(GameSceneUIState state)
    {
        switch (state)
        {
            case GameSceneUIState.InGame:
                Time.timeScale = 1f;
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                // 커서를 다시 잠그는 프레임에 마우스 델타가 크게 튀는 것을 방지
                if (DataManager.Instance != null && DataManager.Instance.mouseLook != null)
                    DataManager.Instance.mouseLook.SuppressInput();
                // ���� �Լ� ��� (������ ����)
                //UpdateEmotionUI(iconImage, happyIcon, happyIconScale, hopeIcon, hopeIconScale, angryIcon, angryIconScale, sadIcon, sadIconScale);
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
                //
                // ���� �Լ� ��� (���ѷα� �̹��� ����)
                //UpdateEmotionUI(prologueImage, prologue_Happy, prologueScale_Happy, prologue_Hope, prologueScale_Hope, prologue_Angry, prologueScale_Angry, prologue_Sad, prologueScale_Sad);
                break;

            case GameSceneUIState.GameOver:
                Time.timeScale = 0f;
                DataManager.Instance.mouseLook.enabled = false;
                break;
        }
    }

    public IEnumerator SceneFade(FadeState fadeInOut)
    {
        if (fadeCanvasGroup == null) yield break;
        fadeCanvasGroup.blocksRaycasts = true; // �Է� ���� ����

        bool fadeIn = fadeInOut == FadeState.FadeIn;
        bool fadeOut = fadeInOut == FadeState.FadeOut;

        yield return StartCoroutine(Fade(fadeCanvasGroup, fadeInOut, fadeDuration));

        if (fadeIn)
        {
            fadeCanvasGroup.alpha = 0f; // Ȯ���ϰ� �����ϰ�
            fadeCanvasGroup.blocksRaycasts = false; // [�߿�!] ���̵����� ������ �ݵ�� �Է��� �ٽ� ����ؾ� �մϴ�.
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

        // ����/��ǥ ���İ� ����
        float startAlpha = (fadeInOut == FadeState.FadeIn) ? 1f : 0f;
        float endAlpha = (fadeInOut == FadeState.FadeIn) ? 0f : 1f;

        // �ʱⰪ ����
        fadeCanvasGroup.alpha = startAlpha;

        // 0.5�� ��� (�� �ð� ���� ���� �ɸ��� ���� �������)
        yield return new WaitForSecondsRealtime(0.5f);

        if (fadeDuration > 0f)
        {
            // [�ذ� �ٽ�] ����(+=) ��� ���, ���� �ð��� ����մϴ�.
            // 0.5�� ��Ⱑ ���� '����'�� �ٷ� �ִϸ��̼� ���� �����Դϴ�.
            float startTime = Time.unscaledTime;

            // ��� �ð��� duration���� ���� ���� �ݺ�
            while (Time.unscaledTime < startTime + fadeDuration)
            {
                // [�ذ� �ٽ�] (���� �ð� - ���� �ð�)���� ������ ��� �ð��� ���մϴ�.
                // �̷��� �ϸ� ���� �������� ��ŸŸ���� �ƹ��� Ŀ�� ������ ���� �ʽ��ϴ�.
                float timer = Time.unscaledTime - startTime;

                float progress = timer / fadeDuration;

                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);

                yield return null;
            }
        }

        // ������ Ȯ��
        fadeCanvasGroup.alpha = endAlpha;

        if (fadeInOut == FadeState.FadeIn)
        {
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }

    public IEnumerator WaitForEpilogue()
    {
        if (isEpilogueRoutineStarted)
        {
            yield break;
        }
        // 1. �ߺ� ���� ���� �÷��� On
        isEpilogueRoutineStarted = true;

        // 2. 3�� ���
        yield return new WaitForSecondsRealtime(1.0f);

        // 3. ���̵� ȿ�� ���� (FadeOut = Alpha 0 -> 1 : �̹��� ��Ÿ��)
        // yield return�� �Ἥ ���̵尡 ���� ������ ��ٸ� ���� �ְ�,
        // �׳� ���ุ ��ų ���� �ֽ��ϴ�. ���⼱ ���ุ ���ѵ� �����մϴ�.
        yield return StartCoroutine(Fade(epilogueImage, FadeState.FadeOut, fadeDuration));

        // 4. �Է� ��� (Update���� Ŭ�� üũ ����)
        epilogueActived = true;
    }

}