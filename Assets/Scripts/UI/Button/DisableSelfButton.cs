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

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 인게임 UI 즉시 표시
        GameSceneUIManager.Instance.SetOnlyState(GameSceneUIState.InGame);

        // 인트로 카메라 → 플레이어 카메라 전환 후 InGame 상태 전환 + 커서 잠금
        DataManager.Instance.StartCoroutine(DataManager.Instance.StartGameWithIntroCamera());
    }
}
