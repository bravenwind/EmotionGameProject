using UnityEngine;

public class PathNode : MonoBehaviour
{
    [HideInInspector] public PathManager manager; // 매니저가 자동으로 할당함
    [HideInInspector] public int myIndex;         // 나의 순서 번호

    private Collider myCollider;
    private Renderer myRenderer;

    void Awake()
    {
        myCollider = GetComponent<Collider>();
        myRenderer = GetComponent<Renderer>();
    }

    // 매니저가 호출하는 함수: 내 상태를 설정함
    public void SetState(bool isCurrentTarget, Material targetMat, Material defaultMat)
    {
        // 1. 순서가 되면 Trigger를 켜서 통과 가능하게 함. 아니면 꺼서 물리 충돌(벽)로 만듦
        myCollider.isTrigger = isCurrentTarget;

        // 2. 머터리얼 변경 (내 차례면 지정된 색, 아니면 기본 색)
        if (myRenderer != null)
        {
            myRenderer.material = isCurrentTarget ? targetMat : defaultMat;
        }
    }

    // 플레이어가 닿았을 때
    void OnTriggerEnter(Collider other)
    {
        // 태그 확인 (필요시 "Player" 태그를 가진 오브젝트만 반응하도록 설정)
        if (other.CompareTag("Player"))
        {
            // 매니저에게 "나 먹혔어!"라고 알림
            manager.OnNodeCollected(this);
        }
    }
}