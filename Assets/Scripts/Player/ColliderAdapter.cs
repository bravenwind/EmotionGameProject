using UnityEngine;

public class ColliderAdapter : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("속도를 측정할 리지드바디 (비워두면 현재 객체에서 찾음)")]
    public Rigidbody targetRigidbody;

    [Header("Speed Settings (Blend Parameters)")]
    public float minSpeed = 0f;
    public float maxSpeed = 25f;

    [Header("Position Blending")]
    // 0일 때의 위치 (Start)
    public Vector3 posAtMinSpeed = new Vector3(0f, 0.035f, 0.136f);
    // 25일 때의 위치 (End)
    public Vector3 posAtMaxSpeed = new Vector3(0f, 0.188f, -0.299f);

    [Header("Rotation Blending (X Axis)")]
    // 0일 때의 X 회전
    public float rotXAtMinSpeed = 12.47f;
    // 25일 때의 X 회전
    public float rotXAtMaxSpeed = 78.9f;

    void Start()
    {
        // 리지드바디가 할당되지 않았다면 부모나 현재 객체에서 찾기
        if (targetRigidbody == null)
            targetRigidbody = GetComponentInParent<Rigidbody>();
    }

    void Update()
    {
        if (targetRigidbody == null) return;

        // 1. 현재 속도 측정 (Unity 6 이상: linearVelocity, 이전 버전: velocity)
        // 사용자가 요청한 linearVelocity 사용
        float currentSpeed = targetRigidbody.linearVelocity.magnitude;

        // 2. 속도를 0~1 사이의 값(t)으로 정규화 (Blend Tree의 핵심 로직)
        // 속도가 0이면 t=0, 25면 t=1, 그 사이는 비율에 따라 계산됨
        float t = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);

        // 3. 위치 보간 (Lerp)
        // t값에 따라 두 벡터 사이를 부드럽게 이동
        transform.localPosition = Vector3.Lerp(posAtMinSpeed, posAtMaxSpeed, t);

        // 4. 회전 보간 (Lerp)
        // t값에 따라 X축 각도 계산
        float currentXAngle = Mathf.Lerp(rotXAtMinSpeed, rotXAtMaxSpeed, t);

        // 현재 객체의 Y, Z 회전은 유지하면서 X축만 변경
        Vector3 currentEuler = transform.localEulerAngles;
        currentEuler.x = currentXAngle;
        transform.localEulerAngles = currentEuler;
    }
}