using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    private int lastSecond = -1;

    // �� ������ �̹� ����Ǿ����� üũ�ϴ� ���� �߰�
    private bool isGameEnded = false;

    [SerializeField]
    private TMP_Text timerText;

    void Update()
    {
        // �� ������ ����Ǿ��ٸ� �� �̻� Ÿ�̸� �ڵ带 �������� ����
        if (isGameEnded) return;

        if (DataManager.Instance.currentTime <= 0)
        {
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
