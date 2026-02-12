using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    private int lastSecond = -1;

    // ★ 게임이 이미 종료되었는지 체크하는 변수 추가
    private bool isGameEnded = false;

    [SerializeField]
    private TMP_Text timerText;

    void Update()
    {
        // ★ 게임이 종료되었다면 더 이상 타이머 코드를 실행하지 않음
        if (isGameEnded) return;

        if (DataManager.Instance.currentTime <= 0)
        {
            DataManager.Instance.targetMapCam.GetComponentInParent<PathManager>().ActivateThis();
            DataManager.Instance.StartCoroutine("GameOver");
            return; // GameFail() 실행 후 아래 코드 실행 방지
        }

        DataManager.Instance.currentTime -= Time.deltaTime;
        if (DataManager.Instance.currentTime < 0) DataManager.Instance.currentTime = 0;

        int currentSecond = Mathf.FloorToInt(DataManager.Instance.currentTime);

        if (currentSecond != lastSecond)
        {
            UpdateTimerText(currentSecond);
            lastSecond = currentSecond;
        }
    }

    void UpdateTimerText(int secondsLeft)
    {
        int minutes = secondsLeft / 60;
        int seconds = secondsLeft % 60;

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
