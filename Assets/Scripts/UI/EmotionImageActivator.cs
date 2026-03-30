using UnityEngine;

/// <summary>
/// 감정별로 다른 자식 이미지를 활성화하는 컴포넌트.
/// EpiloguePanel, ProloguePanel 등 감정에 따라 다른 이미지를 보여줘야 하는 패널에 부착합니다.
/// </summary>
public class EmotionImageActivator : MonoBehaviour
{
    [System.Serializable]
    public class EmotionImageSet
    {
        public EmotionState emotion;
        public GameObject[] images;
    }

    [Header("감정별 이미지 매핑")]
    [SerializeField] private EmotionImageSet[] emotionImages;

    private void OnEnable()
    {
        ActivateForCurrentEmotion();
    }

    public void ActivateForCurrentEmotion()
    {
        if (DataManager.Instance == null) return;

        EmotionState current = DataManager.Instance.targetEmotion;

        foreach (var set in emotionImages)
        {
            bool isActive = set.emotion == current;
            foreach (var image in set.images)
            {
                if (image != null)
                    image.SetActive(isActive);
            }
        }
    }
}
