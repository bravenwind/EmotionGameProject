using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ZeroGravityMovement : MonoBehaviour
{
    [Header("설정 (Settings)")]
    [SerializeField]
    private Transform cameraTransform; // 메인 카메라
    public float acceleration = 15f;   // 가속력
    public float dampingOnIdle = 0.5f; // 평소 저항
    public float dampingOnBrake = 3.0f; // 브레이크 저항

    [Header("애니메이션 (Animation)")]
    [SerializeField]
    private Animator playerAnimator;
    [SerializeField]
    private string speedParamName = "Speed"; // 애니메이터의 파라미터 이름

    private Rigidbody rb;
    private int speedHash; // 파라미터 ID 최적화용

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 중력 끄기 및 회전 잠금
        rb.useGravity = false;
        rb.freezeRotation = true;

        // 문자열 비교 비용을 줄이기 위해 Hash로 변환
        speedHash = Animator.StringToHash(speedParamName);
    }

    void FixedUpdate()
    {
        // --- 물리 이동 로직 ---

        // 1. 공기 저항(Drag) 처리
        if (Input.GetKey(KeyCode.Space))
        {
            rb.linearDamping = dampingOnBrake;
        }
        else
        {
            rb.linearDamping = dampingOnIdle;
        }

        // 2. 이동 처리 (W 키)
        if (Input.GetKey(KeyCode.W))
        {
            rb.AddForce(cameraTransform.forward * acceleration, ForceMode.Acceleration);
        }
    }

    void Update()
    {
        // --- 애니메이션 처리 (매 프레임) ---
        if (playerAnimator != null)
        {
            // 현재 리지드바디의 속력(크기)을 구함
            // Unity 6에서는 rb.linearVelocity, 이전 버전은 rb.velocity
            float currentSpeed = rb.linearVelocity.magnitude;

            // 애니메이터에 전달 (0.1f는 값이 부드럽게 바뀌도록 하는 댐핑 시간)
            playerAnimator.SetFloat(speedHash, currentSpeed, 0.1f, Time.deltaTime);
        }
    }
}