using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;    

    [Header("����")]
    public AudioMixer audioMixer;
    public string parameter_BGM = "BGMVolume";
    public string parameter_SFX = "SFXVolume";

    [Header("UI ������Ʈ")]
    public Slider volumeSlider_BGM;
    public Slider volumeSlider_SFX;
    public TMP_Text volumeText_BGM;
    public TMP_Text volumeText_SFX;

    [Header("���� ���� (dB)")]
    public float minVolume_BGM = -40f; // -20�� �ʹ� Ŭ �� �־� ���� -40~-60 ��õ
    public float maxVolume_BGM = 0f;   // 5�� �Ҹ��� ���� �� �־� 0 ��õ

    public float minVolume_SFX = -40f;
    public float maxVolume_SFX = 0f;

    [Header("����")]
    public GameSceneUIManager uiManager;

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

    private void Start()
    {
        ResetVolumeUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) 
        {
            SetBGMVolume(0);
        }
    }

    // [�߿� ����] BGM ���� �Լ�
    public void SetBGMVolume(float value)
    {
        float targetVolume = (value <= 0.001f) ? -80f : Mathf.Lerp(minVolume_BGM, maxVolume_BGM, value);

        audioMixer.SetFloat(parameter_BGM, targetVolume);
        UpdateVolumeText(volumeText_BGM, value);
    }

    // [�߿� ����] SFX ���� �Լ�
    public void SetSFXVolume(float value)
    {
        float targetVolume = (value <= 0.001f) ? -80f : Mathf.Lerp(minVolume_SFX, maxVolume_SFX, value);

        audioMixer.SetFloat(parameter_SFX, targetVolume);
        UpdateVolumeText(volumeText_SFX, value);
    }

    // �ؽ�Ʈ ������Ʈ�� ���� �Լ�
    private void UpdateVolumeText(TMP_Text textComponent, float value)
    {
        if (textComponent != null)
        {
            textComponent.text = ((int)(value * 100)).ToString();
        }
    }

    public void ResetVolumeUI()
    {
        // 1. BGM �����̴� �ʱ�ȭ
        if (volumeSlider_BGM != null)
        {
            if (audioMixer.GetFloat(parameter_BGM, out float currentBGMDb))
            {
                // -80dB ���϶�� ���Ұŷ� �����Ͽ� �����̴� 0
                volumeSlider_BGM.value = (currentBGMDb <= -80f)
                    ? 0
                    : Mathf.InverseLerp(minVolume_BGM, maxVolume_BGM, currentBGMDb);
            }
            // �ʱ� �ؽ�Ʈ ����
            UpdateVolumeText(volumeText_BGM, volumeSlider_BGM.value);
        }

        // 2. SFX �����̴� �ʱ�ȭ (���� �ڵ��� ���� ����: ���������� ����ǰ� ����)
        if (volumeSlider_SFX != null)
        {
            if (audioMixer.GetFloat(parameter_SFX, out float currentSFXDb))
            {
                // [���� ����] ���� �ڵ忡�� currentBGMDb�� üũ�ϴ� ���� ���� -> currentSFXDb üũ
                volumeSlider_SFX.value = (currentSFXDb <= -80f)
                    ? 0
                    : Mathf.InverseLerp(minVolume_SFX, maxVolume_SFX, currentSFXDb);
            }
            // �ʱ� �ؽ�Ʈ ����
            UpdateVolumeText(volumeText_SFX, volumeSlider_SFX.value);
        }
    }
}