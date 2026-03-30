using UnityEngine;

public class BGMSelector : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource_BGM;

    public AudioClip happyBGM;
    public AudioClip hopeBGM;
    public AudioClip angryBGM;
    public AudioClip sadBGM;

    void Start()
    {
        audioSource_BGM = GetComponent<AudioSource>();

        switch (DataManager.Instance.targetEmotion)
        {
            case EmotionState.Happy:
                audioSource_BGM.clip = happyBGM;
                break;
            case EmotionState.Hope:
                audioSource_BGM.clip = hopeBGM;
                break;
            case EmotionState.Angry:
                audioSource_BGM.clip = angryBGM;
                break;
            case EmotionState.Sad:
                audioSource_BGM.clip = sadBGM;
                break;
        }

        audioSource_BGM.Play();
    }
}
