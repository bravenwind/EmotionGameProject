using UnityEngine;
using UnityEngine.UI;

public class FillImage : MonoBehaviour
{
    [SerializeField]
    private Image emotionScoreFillImage;

    private void OnEnable()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.OnEmotionScoreChanged += UpdateEmotionScoreImage;
    }

    private void Start()
    {
        emotionScoreFillImage.fillAmount = 0.0f;
    }

    private void OnDisable()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.OnEmotionScoreChanged -= UpdateEmotionScoreImage;
    }

    private void UpdateEmotionScoreImage(float score)
    {
        if (emotionScoreFillImage == null) return;
        emotionScoreFillImage.fillAmount = score / DataManager.Instance.maxEmotionScore;
        Debug.Log($"[FillImage] : score {score}");
    }
}
