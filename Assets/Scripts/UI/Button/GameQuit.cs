using UnityEngine;

public class GameQuit : MonoBehaviour
{
    public void QuitGame()
    {
        PlaySFXAudio.Instance.PlayButtonClick(1);
        Debug.Log("게임 종료");
        Application.Quit();
    }
}
