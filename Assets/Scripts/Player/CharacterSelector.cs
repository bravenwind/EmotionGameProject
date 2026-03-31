using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 씬에 배치된 4개 캐릭터 중 DataManager.targetEmotion에 맞는 캐릭터만 활성화하고
/// DataManager에 씬별 참조를 연결해주는 스크립트.
/// 씬의 빈 오브젝트에 붙이고 캐릭터 4개를 등록하면 됩니다.
/// </summary>
public class CharacterSelector : MonoBehaviour
{
    [System.Serializable]
    public struct CharacterSet
    {
        public EmotionState emotion;
        public GameObject characterRoot;
        public CinemachineCamera playerCam;
        public Transform playerTransform;
        public Transform playerLineTransform;
        public Animator playerAnimator;
        public ZeroGravityMovement movementScript;
        public MouseLook mouseLook;
        public CinemachineCamera camToClearFail;
        public GameObject trailParticle;
        public Material emotionSkybox;
        public CinemachineCamera introCam;
    }

    [Header("감정별 캐릭터 세트 (4개 등록)")]
    public List<CharacterSet> characters;

    void Start()
    {
        foreach (var c in characters)
        {
            bool isTarget = c.emotion == DataManager.Instance.targetEmotion;
            c.characterRoot.SetActive(isTarget);
            c.playerCam.gameObject.SetActive(isTarget);
            c.introCam.gameObject.SetActive(isTarget);

            if (isTarget)
            {
                DataManager.Instance.playerCam              = c.playerCam;
                DataManager.Instance.playerTransform        = c.playerTransform;
                DataManager.Instance.playerLineTransform    = c.playerLineTransform;
                DataManager.Instance.playerAnimator         = c.playerAnimator;
                DataManager.Instance.playerMovementScript   = c.movementScript;
                DataManager.Instance.mouseLook              = c.mouseLook;
                DataManager.Instance.selectedCharacter      = c.characterRoot;
                DataManager.Instance.camToClearFail         = c.camToClearFail;
                DataManager.Instance.characterFocus         = c.characterRoot.GetComponent<CharacterFocus>();
                DataManager.Instance.introCam               = c.introCam;
                ParticlePoolManager.Instance.particlePrefab = c.trailParticle;
                RenderSettings.skybox = c.emotionSkybox;
            }
        }

        DataManager.Instance.brain = Camera.main.GetComponent<CinemachineBrain>();
        DataManager.Instance.introCam.Priority = 15;
        ParticlePoolManager.Instance.InitializePool();
    }
}
