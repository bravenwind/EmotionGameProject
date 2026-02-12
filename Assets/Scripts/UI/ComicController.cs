using UnityEngine;
using UnityEngine.UI;

public class ComicController : MonoBehaviour
{
    [Header("만화 컷 이미지들 (순서대로 넣으세요)")]
    public GameObject[] comicPanels; // 컷들을 담을 배열

    private int currentIndex = 0; // 현재 몇 번째 컷인지 확인

    // ★ 추가: UI가 켜질 때마다 상태를 초기화해줍니다.
    private void OnEnable()
    {
        currentIndex = 0;

        // 안전 장치: 패널이 비어있지 않다면
        if (comicPanels != null && comicPanels.Length > 0)
        {
            // 첫 번째 컷만 켜고 나머지는 다 끕니다.
            for (int i = 0; i < comicPanels.Length; i++)
            {
                comicPanels[i].SetActive(i == 0);
            }
        }

        // 다음 컷을 가리키도록 인덱스 증가 (첫 컷은 이미 켜져 있으므로)
        currentIndex = 1;
    }

    void Update()
    {
        if (GameSceneUIManager.Instance.currentState != GameSceneUIState.Prologue) return;

        // 마우스 왼쪽 클릭 또는 스페이스바
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            ShowNextPanel();
        }
    }

    void ShowNextPanel()
    {
        // 아직 보여줄 컷이 남아있다면
        if (currentIndex < comicPanels.Length)
        {
            comicPanels[currentIndex].SetActive(true); // 해당 순서의 컷 켜기
            currentIndex++; // 다음 순서로 넘김
        }
        else
        {
            // 모든 컷을 다 보여준 상태에서 클릭했다면 -> 프롤로그 종료 요청
            EndPrologue();
        }
    }

    void EndPrologue()
    {
        Debug.Log("프롤로그 종료! 매니저에게 인게임 전환 요청");

        // ★ 수정: 매니저에게 부드러운 전환을 요청합니다.
        if (GameSceneUIManager.Instance != null)
        {
            GameSceneUIManager.Instance.FinishPrologue();
        }
        else
        {
            // 매니저가 없을 경우 비상용 코드
            gameObject.SetActive(false);
        }
    }
}