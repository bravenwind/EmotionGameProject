using UnityEngine;

public class PathNode : MonoBehaviour
{
    public Transform target;
    public Vector3 originalPosition = Vector3.zero;
    public EmotionState emotion;

    public float absorbTimer = 0.0f;
    public float absorbMaxSpeed = 100f;
    [SerializeField] private float completelyAbsorbedTime = 0.3f;

    private Rigidbody rb;
    public bool absorbing = false;
    public bool isCurrentTarget = false;

    [HideInInspector] public PathManager manager;
    [HideInInspector] public int myIndex;

    private Collider myCollider;
    private Renderer myRenderer;

    // PathManager가 GetComponent 없이 접근하도록 노출
    public Collider NodeCollider => myCollider;
    public Renderer NodeRenderer => myRenderer;

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
        absorbing = false;
        absorbTimer = 0f;
        transform.position = originalPosition;
    }

    public void SetState(bool isTarget, Material targetMat, Material defaultMat)
    {
        isCurrentTarget = isTarget;
        myCollider.isTrigger = isTarget;
        emotion = manager.completedEmotion;

        if (myRenderer != null)
            myRenderer.material = isTarget ? targetMat : defaultMat;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isCurrentTarget || absorbing) return;

        if (other.CompareTag("Player"))
            StartAbsorb(other.transform.root);
    }

    private void OnTriggerStay(Collider other)
    {
        // 트리거 안에 이미 있을 때 타겟으로 활성화된 경우 처리
        if (isCurrentTarget && !absorbing && other.CompareTag("Player"))
            StartAbsorb(other.transform.root);
    }

    private void Update()
    {
        if (isCurrentTarget && !absorbing)
        {
            transform.LookAt(DataManager.Instance.playerTransform.position);
            transform.Rotate(manager.nodeRotationOffset);
        }
    }

    public void StartAbsorb(Transform player)
    {
        if (absorbing) return;

        rb.useGravity = false;
        rb.isKinematic = true;

        target = player;
        absorbing = true;
        absorbTimer = 0f;
    }

    void FixedUpdate()
    {
        if (!absorbing || target == null) return;

        absorbTimer += Time.fixedDeltaTime;

        float t = Mathf.Clamp01(absorbTimer / completelyAbsorbedTime);
        t = t * t; // ease-in

        float currentSpeed = Mathf.Lerp(2f, absorbMaxSpeed, t);

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            currentSpeed * Time.fixedDeltaTime
        );

        // 흡수 완료: 트리거 이탈 여부와 무관하게 타이머로 확정
        if (absorbTimer >= completelyAbsorbedTime)
        {
            absorbing = false;
            OnAbsorbed();
        }
    }

    public void OnAbsorbed()
    {
        if (emotion == DataManager.Instance.targetEmotion)
            DataManager.Instance.currentEmotionScore = Mathf.Clamp(
                DataManager.Instance.currentEmotionScore + 1, 0, DataManager.Instance.maxEmotionScore);
        DataManager.Instance.currentEmotionCount++;

        PlaySFXAudio.Instance.PlayEmotionConnect();

        // 완전히 흡수된 후 다음 노드 활성화
        manager?.OnNodeCollected(this);

        gameObject.SetActive(false);
    }
}
