using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

// 1. ���¸� �����ϴ� Enum (�ʿ信 ���� �߰�/�����ϼ���)
public enum TitleSceneUIState
{
    None,
    Pause,
    Settings,   // ���� â
    InGame,     // ���� �÷��� �� HUD
    GameSuccess,    // ���� ���� â
    GameFail,
    Menu,
}

public class TitleSceneUIManager : MonoBehaviour
{
    public static TitleSceneUIManager Instance { get; private set; }

    // 2. �ν����Ϳ��� Enum�� ������Ʈ�� ¦���� �� �ְ� ���� Ŭ����
    [System.Serializable]
    public class UIStateMapping
    {
        public TitleSceneUIState state;       // � ������ ��?
        public GameObject uiObject; // � UI�� �� ���ΰ�?
    }

    [Header("UI ��� ����")]
    // �� ����Ʈ�� UI ������Ʈ���� ����ϰ� Enum�� �����ϼ���.
    public List<UIStateMapping> uiList = new List<UIStateMapping>();

    [Header("�ʱ� ����")]
    public TitleSceneUIState startState = TitleSceneUIState.InGame;

    [Header("UI ����")]
    [Tooltip("ȭ���� ���� ������ �̹����� CanvasGroup ������Ʈ")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("���̵� �� �Ǵ� �ð� (��)")]
    public float fadeDuration = 1.0f;

    // ���� ���¸� �����ϴ� ����
    private TitleSceneUIState currentState;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(SceneFade(FadeState.FadeIn));
        // ���� ���� �� �ʱ� ���·� ����
        SetState(startState);
    }

    private void Update()
    {
        // �� ������ ���� ���¿� ���� ���� ����
        UpdateState();
    }

    // ==========================================
    // �ٽ� ��� 1: ���� ���� (SetState)
    // ==========================================
    public void SetState(TitleSceneUIState newState)
    {
        currentState = newState;
        Debug.Log($"���� ����: {currentState}");

        // ��ϵ� ��� UI�� ��ȸ�ϸ� ���¿� �´� �͸� �Ѱ�, �������� ���ϴ�.
        foreach (var mapping in uiList)
        {
            if (mapping.uiObject != null)
            {
                // ���� ���¿� ���ε� ���°� ������ true(����), �ƴϸ� false(����)
                bool isActive = (mapping.state == newState);
                mapping.uiObject.SetActive(isActive);
            }
        }

        // ���� ���� �� 1ȸ�� ������ �ʿ��ϴٸ� ���⿡ �ۼ� (��: ���� �ʱ�ȭ ��)
        OnEnterState(newState);
    }

    // ==========================================
    // �ٽ� ��� 2: ���º� ������ ���� (UpdateState)
    // ==========================================
    private void UpdateState()
    {
        switch (currentState)
        {
            //case UIState.MainMenu:
            //    // ���� �޴������� ���� (��: �ƹ� Ű�� ������ ���� ����)
            //    if (Input.GetKeyDown(KeyCode.Space))
            //    {
            //        Debug.Log("���� ����!");
            //        SetState(UIState.InGame);
            //    }
            //    break;

            case TitleSceneUIState.InGame:
                // ���� �� ���� (��: ESC ������ �Ͻ�����/����)
                //if (Input.GetKeyDown(KeyCode.Escape))
                //{
                //    SetState(UIState.Settings);
                //}
                break;

            case TitleSceneUIState.Settings:
                // ���� â ���� (��: ESC ������ �ٽ� ��������)
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    SetState(TitleSceneUIState.InGame);
                }
                break;

            case TitleSceneUIState.GameSuccess:
                // ���� ���� ���� (��: RŰ�� �����)
                if (Input.GetKeyDown(KeyCode.R))
                {
                    SceneManager.LoadScene("LevelDesign");
                }
                break;
            case TitleSceneUIState.Pause:
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    SetState(TitleSceneUIState.InGame);
                }
                break;
        }
    }

    // (���� ����) ���� ���� �� �߰� ó���� ���� �Լ�
    private void OnEnterState(TitleSceneUIState state)
    {
        switch (state)
        {
            case TitleSceneUIState.InGame:
                Time.timeScale = 1f; // ���� �ӵ� ����ȭ
                GUI.enabled = true;
                break;
            case TitleSceneUIState.Settings:
                Time.timeScale = 0f; // ���� �Ͻ� ����
                GUI.enabled = false;
                break;
            case TitleSceneUIState.Pause:
                Time.timeScale = 0f;
                break;
            case TitleSceneUIState.GameFail:
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
            SceneManager.LoadScene("Main");
        }
    }

    public IEnumerator Fade(CanvasGroup fadeCanvasGroup, FadeState fadeInOut, float fadeDuration)
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            // Time.deltaTime ��� unscaledDeltaTime ��� (�Ʒ� 2�� ���� ����)
            timer += Time.unscaledDeltaTime;

            if (fadeInOut == FadeState.FadeIn)
            {
                fadeCanvasGroup.alpha = 1 - Mathf.Clamp01(timer / fadeDuration);
            }
            if (fadeInOut == FadeState.FadeOut)
            {
                fadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            }
            yield return null;
        }
    }
}