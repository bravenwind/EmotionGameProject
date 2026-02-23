using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HoveredLevelManager : MonoBehaviour
{
    [SerializeField] private Sprite thumbnail_happy;
    [SerializeField] private Sprite thumbnail_hope;
    [SerializeField] private Sprite thumbnail_angry;
    [SerializeField] private Sprite thumbnail_sad;
    [SerializeField] private Sprite thumbnail_null;

    [SerializeField]
    private Image[] levelThumbnailImages = new Image[4];
    [SerializeField]
    private Image levelThumbnailImage_happy;
    [SerializeField]
    private Image levelThumbnailImage_hope;
    [SerializeField]
    private Image levelThumbnailImage_angry;
    [SerializeField]
    private Image levelThumbnailImage_sad;

    [SerializeField]
    private TMP_Text levelNameText;

    [SerializeField]
    private TMP_Text levelDesriptionText;

    public void OnEnterHoverLevel(EmotionState emotion)
    {
        switch (emotion)
        {
            case EmotionState.Happy:
                levelThumbnailImage_happy.color = new Color(1, 1, 1, 1);
                levelThumbnailImage_happy.sprite = thumbnail_happy;
                //levelNameText.text = "행복";
                //levelDesriptionText.text = "스테이지 설명";
                break;
            case EmotionState.Hope:
                levelThumbnailImage_hope.color = new Color(1, 1, 1, 1);
                levelThumbnailImage_hope.sprite = thumbnail_hope;
                //levelNameText.text = "희망";
                //levelDesriptionText.text = "스테이지 설명";
                break;
            case EmotionState.Angry:
                levelThumbnailImage_angry.color = new Color(1, 1, 1, 1);
                levelThumbnailImage_angry.sprite = thumbnail_angry;
                //levelNameText.text = "분노";
                //levelDesriptionText.text = "스테이지 설명";
                break;
            case EmotionState.Sad:
                levelThumbnailImage_sad.color = new Color(1, 1, 1, 1);
                levelThumbnailImage_sad.sprite = thumbnail_sad;
                //levelNameText.text = "슬픔";
                //levelDesriptionText.text = "스테이지 설명";
                break;
            default:
                break;
        }
    }

    public void OnExitHoverLevel()
    {
        //levelDesriptionText.text = string.Empty;
        //levelNameText.text = string.Empty;
    }

    public void DisableAll()
    {
        foreach (Image image in levelThumbnailImages) 
        {
            image.sprite = thumbnail_null;
            image.color = new Color(1, 1, 1, 0);
        }
    }
}
