using UnityEngine;
using UnityEngine.UI;

public class DisableSelfButton : MonoBehaviour
{
    public void DisableSelf()
    {
        PlaySFXAudio.Instance.PlayButtonClick(2);
        gameObject.SetActive(false);
    }
}
