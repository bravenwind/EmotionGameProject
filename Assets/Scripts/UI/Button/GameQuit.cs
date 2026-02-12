using UnityEngine;

public class GameQuit : MonoBehaviour
{
    public void QuitGame()
    {
        PlaySFXAudio.Instance.PlayButtonClick(1);
        Application.Quit();
        Debug.Log("게임 종료");
    }
}
