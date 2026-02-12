using UnityEngine;

public class PlaySFXAudio : MonoBehaviour
{
    public static PlaySFXAudio Instance;

    [Header("Audio Source")]
    public AudioSource fxAudioSource;

    [Header("UI & Game State")]
    public AudioClip missionCompleteAudio;
    public AudioClip failAudio;
    public AudioClip[] buttonClickAudios; // 인스펙터에서 Size를 3으로 설정하세요.

    [Header("Actions")]
    public AudioClip emotionConnectAudio; // 감정연결음
    public AudioClip dash1Audio;
    public AudioClip dash2Audio;
    public AudioClip crashAudio;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 1. 감정 연결음
    public void PlayEmotionConnect()
    {
        if (emotionConnectAudio != null) fxAudioSource.PlayOneShot(emotionConnectAudio);
    }

    // 2. 대쉬 소리 (type 1 또는 2)
    public void PlayDashSound(int type)
    {
        AudioClip clip = (type == 1) ? dash1Audio : dash2Audio;
        if (clip != null) fxAudioSource.PlayOneShot(clip);
    }

    // 3. 미션 달성
    public void PlayMissionComplete()
    {
        if (missionCompleteAudio != null) fxAudioSource.PlayOneShot(missionCompleteAudio);
    }

    // 4. 부딪힘 (Crash)
    public void PlayCrash()
    {
        if (crashAudio != null) fxAudioSource.PlayOneShot(crashAudio);
    }

    // 5. 실패
    public void PlayFail()
    {
        if (failAudio != null) fxAudioSource.PlayOneShot(failAudio);
    }

    // 6. 버튼 클릭 (1, 2, 3번)
    public void PlayButtonClick(int number)
    {
        int index = number - 1;
        if (buttonClickAudios != null && index >= 0 && index < buttonClickAudios.Length)
        {
            if (buttonClickAudios[index] != null) fxAudioSource.PlayOneShot(buttonClickAudios[index]);
        }
    }
}