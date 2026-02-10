using UnityEngine;

public class GameQuit : MonoBehaviour
{
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료");
    }
}
