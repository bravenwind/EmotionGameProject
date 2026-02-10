using UnityEngine;
public enum Emotion
{
    Happy = 0,
    Hope = 1,
    Angry = 2,
    Sad = 3
}

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public Emotion targetEmotion = Emotion.Happy;

    private void Awake()
    {
        Instance = this;
    }
}
