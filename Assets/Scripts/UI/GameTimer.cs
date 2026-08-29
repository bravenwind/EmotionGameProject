using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    private int lastSecond = -1;

    // �� ������ �̹� ����Ǿ����� üũ�ϴ� ���� �߰�
    private bool isGameEnded = false;

    [SerializeField]
    private TMP_Text timerText;

    private void Start()
    {
        UpdateTimerText((int)DataManager.Instance.limitTime);
    }

    void Update()
    {
        if (GameSceneUIManager.Instance.currentState != GameSceneUIState.InGame) 
        {
            return;
        }

        // �� ������ ����Ǿ��ٸ� �� �̻� Ÿ�̸� �ڵ带 �������� ����
        // [중요] GameOver 연출이 진행되는 동안에도 currentState는 InGame으로 남아있다.
        // 이 가드가 없으면 매 프레임 GameOver() 코루틴이 새로 시작된다.
        if (isGameEnded || DataManager.Instance.gameEnded)
        {
            isGameEnded = true;
            return;
        }

        if (DataManager.Instance.currentTime <= 0)
        {
            isGameEnded = true;
            DataManager.Instance.targetMapCam.GetComponentInParent<PathManager>().ActivateThis();
            DataManager.Instance.gameEnded = true;
            DataManager.Instance.gameCleared = false;
            DataManager.Instance.StartCoroutine(DataManager.Instance.GameOver());
            return; // GameFail() ���� �� �Ʒ� �ڵ� ���� ����
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
