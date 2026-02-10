using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HoveredLevelManager : MonoBehaviour
{
    [SerializeField] private Sprite thumbnail_happy;
    [SerializeField] private Sprite thumbnail_hope;
    [SerializeField] private Sprite thumbnail_angry;
    [SerializeField] private Sprite thumbnail_sad;

    [SerializeField]
    private Image levelThumbnailImage;

    [SerializeField]
    private TMP_Text levelNameText;

    [SerializeField]
    private TMP_Text levelDesriptionText;

    public void OnEnterHoverLevel(EmotionState emotion)
    {
        switch (emotion)
        {
            case EmotionState.Happy:
                levelThumbnailImage.sprite = thumbnail_happy;
                levelNameText.text = "행복";
                levelDesriptionText.text = "스테이지 설명";
                break;
            case EmotionState.Hope:
                levelThumbnailImage.sprite = thumbnail_hope;
                levelNameText.text = "희망";
                levelDesriptionText.text = "스테이지 설명";
                break;
            case EmotionState.Angry:
                levelThumbnailImage.sprite = thumbnail_angry;
                levelNameText.text = "분노";
                levelDesriptionText.text = "스테이지 설명";
                break;
            case EmotionState.Sad:
                levelThumbnailImage.sprite = thumbnail_sad;
                levelNameText.text = "슬픔";
                levelDesriptionText.text = "스테이지 설명";
                break;
        }
    }

    public void OnExitHoverLevel()
    {
        levelDesriptionText.text = string.Empty;
        levelNameText.text = string.Empty;
        levelThumbnailImage.sprite = null;
    }
}
