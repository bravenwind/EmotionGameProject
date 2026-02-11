using UnityEngine;

public class PathNode : MonoBehaviour
{
    public Transform target;          // Player
    public float destroyDistance = 0.3f;
    public Vector3 originalPosition = Vector3.zero;
    public EmotionState emotion;

    public float absorbTimer = 0.0f;
    public float absorbMaxSpeed = 100f;   // [변경] 빨려 들어가는 최고 속도
    private float completelyAbsorbedTime = 0.6f;

    private Rigidbody rb;
    public bool absorbing = false;
    public bool isCurrentTarget = false;

    [HideInInspector] public PathManager manager; // 매니저가 자동으로 할당함
    [HideInInspector] public int myIndex;         // 나의 순서 번호

    private Collider myCollider;
    private Renderer myRenderer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;

        myCollider = GetComponent<Collider>();
        myRenderer = GetComponent<Renderer>();

        originalPosition = transform.position;
    }

    private void OnEnable()
    {
        rb.linearVelocity = Vector3.zero;
        transform.position = originalPosition;
    }

    // 매니저가 호출하는 함수: 내 상태를 설정함
    public void SetState(bool isTarget, Material targetMat, Material defaultMat)
    {
        // 1. 순서가 되면 Trigger를 켜서 통과 가능하게 함. 아니면 꺼서 물리 충돌(벽)로 만듦
        isCurrentTarget = isTarget;
        myCollider.isTrigger = isTarget;
        emotion = manager.completedEmotion;

        // 2. 머터리얼 변경 (내 차례면 지정된 색, 아니면 기본 색)
        if (myRenderer != null)
        {
            myRenderer.material = isTarget ? targetMat : defaultMat;
        }
    }

    // 플레이어가 닿았을 때
    void OnTriggerEnter(Collider other)
    {
        if (!isCurrentTarget)
        {
            return;
        }

        // 태그 확인 (필요시 "Player" 태그를 가진 오브젝트만 반응하도록 설정)
        if (other.CompareTag("Player"))
        {
            // 매니저에게 "나 먹혔어!"라고 알림
            StartAbsorb(other.transform.root.transform);
            manager.OnNodeCollected(this);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // ... (기존 내용 유지) ...
        if (other.gameObject.CompareTag("PlayerPhysicalCollider") && absorbing)
        {
            absorbTimer += Time.deltaTime;
            if (absorbTimer >= completelyAbsorbedTime)
            {
                OnAbsorbed();
                absorbing = false;
            }
        }
    }

    // 외부 혹은 충돌 감지에서 호출할 함수
    public void StartAbsorb(Transform player)
    {
        if (absorbing) return;
        if (rb != null)
        {
            // 1. 물리 설정
            rb.useGravity = false;

            rb.isKinematic = false;

            // ✅ [핵심 수정 1] 흡수 시작 시 기존에 튀어가던 관성을 제거합니다.
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        target = player;
        absorbing = true;
        absorbTimer = 0f;
    }

    void FixedUpdate()
    {
        if (!absorbing || target == null) return;

        Vector3 toTarget = target.position - transform.position;

        // 2. 시간 경과에 따른 가속도 계산 (기존 로직 활용)
        absorbTimer += Time.fixedDeltaTime;

        Vector3 dir = toTarget.normalized; // 방향

        // 시간이 지날수록 빠르게 (0 ~ 1 사이 값)
        float t = Mathf.Clamp01(absorbTimer / completelyAbsorbedTime);
        t = Mathf.Pow(t, 2.0f); // 2차 함수 그래프로 가속 느낌 주기

        // ✅ [핵심 수정 2] AddForce 대신 속도를 직접 제어합니다.
        // 기존 maxForce 대신 absorbSpeed 변수를 사용하여 속도를 보간합니다.
        float currentSpeed = Mathf.Lerp(2f, absorbMaxSpeed, t); // 최소 속도 2f에서 시작하여 빨라짐

        // 젤리의 속도를 "플레이어 방향 * 현재 속도"로 고정합니다.
        // 이렇게 하면 옆으로 새지 않고 무조건 플레이어에게 직선으로 날아갑니다.
        rb.linearVelocity = dir * currentSpeed;
    }

    void OnAbsorbed()
    {
        if (emotion == DataManager.Instance.targetEmotion)
        {
            DataManager.Instance.currentEmotionScore += DataManager.Instance.emotionPlusScorePerObject;
        }
        else
        {
            DataManager.Instance.currentEmotionScore -= DataManager.Instance.emotionMinusScorePerObject;
        }

        DataManager.Instance.currentEmotionScore = Mathf.Clamp(DataManager.Instance.currentEmotionScore, 0, DataManager.Instance.maxEmotionScore);
        DataManager.Instance.currentEmotionCount++;

        if (DataManager.Instance.currentEmotionCount >= DataManager.Instance.emotionForLevelUp)
        {
            DataManager.Instance.currentScaleLevel++;
        }

        if (DataManager.Instance.currentEmotionScore >= DataManager.Instance.maxEmotionScore)
        {
            DataManager.Instance.missionDict[DataManager.Instance.mission1] = true;
            DataManager.Instance.mission1Success = true;
        }

        GameSceneUIManager.Instance.UpdateEmotionScoreImage();
        gameObject.SetActive(false);
    }
}