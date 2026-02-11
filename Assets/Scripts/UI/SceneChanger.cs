using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChanger : MonoBehaviour
{
    public string HappyCharacterSceneName = "HappyCharacterLevel";
    public string HopeCharacterSceneName = "HopeCharacterLevel";
    public string AngryCharacterSceneName = "AngryCharacterLevel";
    public string SadCharacterSceneName = "SadCharacterLevel";

    public string titleSceneName = "Title";

    [Header("UI 설정")]
    [Tooltip("화면을 가릴 검은색 이미지의 CanvasGroup 컴포넌트")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("페이드 인 되는 시간 (초)")]
    public float fadeDuration = 1.0f;

    public void ChangeToGameScene(EmotionStateComponent emotionStateComponent)
    {
        EmotionState emotionState = emotionStateComponent.emotionState;
        string sceneName = "";
        switch (emotionState)
        {
            case EmotionState.Happy:
                DataManager.Instance.targetEmotion = EmotionState.Happy;
                sceneName = HappyCharacterSceneName;
                break;
            case EmotionState.Hope:
                DataManager.Instance.targetEmotion = EmotionState.Hope;
                sceneName = HopeCharacterSceneName;
                break;
            case EmotionState.Angry:
                DataManager.Instance.targetEmotion = EmotionState.Angry;
                sceneName = AngryCharacterSceneName;
                break;
            case EmotionState.Sad:
                DataManager.Instance.targetEmotion = EmotionState.Sad;
                sceneName = SadCharacterSceneName;
                break;
        }
        StartCoroutine(SceneFade(FadeState.FadeOut, sceneName));
        DataManager.Instance.ResetGameData();
    }

    public void ChangeToTitleScene()
    {
        StartCoroutine(SceneFade(FadeState.FadeOut, titleSceneName));
    }

    public IEnumerator SceneFade(FadeState fadeInOut, string fadeOutSceneName = null)
    {
        if (fadeCanvasGroup == null) yield break;
        fadeCanvasGroup.blocksRaycasts = true; // 입력 차단 시작

        bool fadeIn = fadeInOut == FadeState.FadeIn;
        bool fadeOut = fadeInOut == FadeState.FadeOut;

        yield return StartCoroutine(Fade(fadeCanvasGroup, fadeInOut, fadeDuration));

        if (fadeIn)
        {
            fadeCanvasGroup.gameObject.SetActive(false);
        }
        if (fadeOut)
        {
            SceneManager.LoadScene(fadeOutSceneName);
        }
    }

    public IEnumerator Fade(CanvasGroup fadeCanvasGroup, FadeState fadeInOut, float fadeDuration)
    {
        fadeCanvasGroup.gameObject.SetActive(true);

        float timer = 0f;
        while (timer < fadeDuration)
        {
            // Time.deltaTime 대신 unscaledDeltaTime 사용 (아래 2번 이유 참조)
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

        if (fadeInOut == FadeState.FadeIn)
        {
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }
}