using UnityEngine;
using UnityEngine.UI;

public class ComicController : MonoBehaviour
{
    [System.Serializable]
    public class EmotionComicSet
    {
        public EmotionState emotion;
        public GameObject panel;      // 컷들의 부모 패널
        public GameObject[] cuts;     // 순서대로 등록할 컷 이미지들
    }

    [Header("감정별 만화 패널 세트")]
    public EmotionComicSet[] emotionComicSets;

    private GameObject[] comicCuts;
    private int currentIndex = 0;

    private void OnEnable()
    {
        currentIndex = 0;

        var set = System.Array.Find(emotionComicSets,
            s => s.emotion == DataManager.Instance.targetEmotion);

        // 모든 패널 비활성화 후 해당 패널만 활성화
        foreach (var e in emotionComicSets)
        {
            if (e.panel != null)
                e.panel.SetActive(false);
        }

        if (set?.panel == null)
        {
            comicCuts = System.Array.Empty<GameObject>();
            return;
        }

        set.panel.SetActive(true);
        comicCuts = set.cuts ?? System.Array.Empty<GameObject>();

        for (int i = 0; i < comicCuts.Length; i++)
            comicCuts[i].SetActive(i == 0);

        currentIndex = 1;
    }

    void Update()
    {
        if (GameSceneUIManager.Instance.currentState != GameSceneUIState.Prologue) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            ShowNextPanel();
        }
    }

    void ShowNextPanel()
    {
        if (currentIndex < comicCuts.Length)
        {
            comicCuts[currentIndex].SetActive(true);
            currentIndex++;
        }
        else
        {
            EndPrologue();
        }
    }

    void EndPrologue()
    {
        Debug.Log("���ѷα� ����! �Ŵ������� �ΰ��� ��ȯ ��û");

        // �� ����: �Ŵ������� �ε巯�� ��ȯ�� ��û�մϴ�.
        if (GameSceneUIManager.Instance != null)
        {
            GameSceneUIManager.Instance.FinishPrologue();
        }
        else
        {
            // �Ŵ����� ���� ��� ���� �ڵ�
            gameObject.SetActive(false);
        }
    }
}