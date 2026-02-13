using UnityEngine;
using UnityEngine.UI;

public class DisableSelfButton : MonoBehaviour
{
    public void DisableSelf()
    {
        PlaySFXAudio.Instance.PlayButtonClick(2);
        gameObject.SetActive(false);
    }

    public void DisableSelfManual()
    {
        PlaySFXAudio.Instance.PlayButtonClick(2);
        gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
