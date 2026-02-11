using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public enum EmotionState
{
    Happy = 0,
    Hope = 1,
    Angry = 2,
    Sad = 3
}

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public EmotionState targetEmotion = EmotionState.Happy;

    [Header("캐릭터 프리팹 설정")]
    public GameObject happyCharacter;
    public GameObject hopeCharacter;
    public GameObject angryCharacter;
    public GameObject sadCharacter;

    [Header("생성 위치 설정 (비워두면 프리팹 기본 위치 사용)")]
    public Transform spawnPoint;

    [Header("현재 선택된 캐릭터 (자동 할당)")]
    public GameObject selectedCharacter;

    [Header("플레이어 참조 컴포넌트")]
    public CinemachineCamera playerCam;
    public MouseLook mouseLook;
    public Transform playerTransform;
    public Transform playerLineTransform;

    [Header("제한시간")]
    public float limitTime = 300f;
    public float targetTime = 120f;
    public float currentTime;

    [Header("미션 내용")]
    public Dictionary<string, bool> missionDict = new Dictionary<string, bool>();

    public string mission1 = "목표감정 도달";
    public string mission2 = "감정을 모두 연결";
    public string mission3 = "2분 내 클리어";

    public bool mission1Success = false;
    public bool mission2Success = false;
    public bool mission3Success = false;

    [Header("감정 게이지")]
    public float maxEmotionScore = 150;
    public float currentEmotionScore = 0;
    public float emotionPlusScorePerObject = 10;
    public float emotionMinusScorePerObject = 5;

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
            return;
        }

        missionDict.Add(mission1, mission1Success);
        missionDict.Add(mission2, mission2Success);
        missionDict.Add(mission3, mission3Success);

        targetTime = (float)char.GetNumericValue(mission3[0]) * 60.0f;
        ResetGameData();
    }

    public void GameOver()
    {
        if (GameSceneUIManager.Instance != null)
        {
            GameSceneUIManager.Instance.SetState(GameSceneUIState.GameOver);
        }
    }

    public void ResetGameData()
    {
        currentEmotionScore = 0;
        currentTime = limitTime;
    }
}
