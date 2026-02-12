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

    [Header("현재 선택된 캐릭터 (자동 할당)")]
    public GameObject selectedCharacter;

    [Header("플레이어 참조 컴포넌트")]
    public CinemachineCamera playerCam;
    public MouseLook mouseLook;
    public Transform playerTransform;
    public Transform playerLineTransform;
    public Animator playerAnimator;
    public ZeroGravityMovement playerMovementScript;

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

    [Header("커지는 캐릭터")]
    public float playerOriginalScale = 1;
    public int maxScaleLevel = 5;
    public List<float> playerScalePerLevel = new List<float>(5);
    public int emotionForLevelUp = 5;
    public int currentEmotionCount = 0;
    public int currentScaleLevel = 1;
    public float scaleIncreaseDuration = 1.5f;

    public Vector3 happyTextureOffset = new Vector3(0, 2f, 0); // 머리 위 2미터를 바라보게 설정
    public Vector3 hopeTextureOffset = new Vector3(-85, 196.399994f, -201.800003f); // 머리 위 2미터를 바라보게 설정
    public Vector3 angryTextureOffset = new Vector3(-85, 196.399994f, -201.800003f); // 머리 위 2미터를 바라보게 설정
    public Vector3 sadTextureOffset = new Vector3(0, 2f, 0); // 머리 위 2미터를 바라보게 설정

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
        ResetGameData();

        missionDict.Add(mission1, mission1Success);
        missionDict.Add(mission2, mission2Success);
        missionDict.Add(mission3, mission3Success);

        targetTime = (float)char.GetNumericValue(mission3[0]) * 60.0f;
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
        currentEmotionCount = 0;
        currentScaleLevel = 1;

        mission1Success = false;
        mission2Success = false;
        mission3Success = false;
    }
}
