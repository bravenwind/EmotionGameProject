using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderIdentifier : MonoBehaviour
{
    [SerializeField] Slider slider_BGM;
    [SerializeField] Slider slider_SFX;

    private void Start()
    {
        AudioManager.Instance.volumeSlider_BGM = slider_BGM;
        AudioManager.Instance.volumeSlider_SFX = slider_SFX;

        AudioManager.Instance.volumeSlider_BGM.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);        
        AudioManager.Instance.volumeSlider_SFX.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);

        AudioManager.Instance.ResetVolumeUI();
    }
}
