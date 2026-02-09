using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody))]
public class ZeroGravityMovement : MonoBehaviour
{
    [Header("설정 (Settings)")]
    [SerializeField]
    private Transform cameraTransform;
    public float acceleration = 15f;
    public float dampingOnIdle = 0.5f;
    public float dampingOnBrake = 3.0f;

    [Header("대쉬 설정 (Dash Settings)")]
    public float dashForce = 20f;      // 대쉬 힘 (순간 가속)
    public float dashCooldown = 1.5f;  // 대쉬 재사용 대기시간
    private float lastDashTime = -100f; // 마지막 대쉬 시점 저장

    [Header("애니메이션 (Animation)")]
    [SerializeField]
    private Animator playerAnimator;
    [SerializeField]
    private string speedParamName = "Speed";
    private int speedHash;
    private int dashSpeedHash; // 블렌드 트리 Multiplier용 파라미터

    // public string dashTriggerName = "Dash"; // 대쉬 애니메이션이 따로 있다면 주석 해제

    // 애니메이션 속도 제어
    private float currentDashAnimMultiplier = 1f;
    public float dashAnimBoost = 2.5f; // 대쉬 시 애니메이션 배속 (예: 2.5배속)
    public float animRestoreSpeed = 2f; // 원래 속도로 돌아오는 속도

    [Header("이펙트 (VFX)")]
    public Volume globalVolume; // Post-Processing Volume 연결
    private MotionBlur motionBlur;
    public float maxBlurIntensity = 1f; // 대쉬 시 블러 강도

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        speedHash = Animator.StringToHash(speedParamName);

        if (globalVolume.profile.TryGet<MotionBlur>(out var blur))
        {
            motionBlur = blur;
        }
    }

    void Update()
    {
        // --- 입력 감지 (대쉬) ---
        // GetKeyDown은 물리 루프(FixedUpdate)보다 Update에서 감지하는 것이 정확합니다.
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            TryDash();
        }

        HandleVisualEffects();
    }

    void HandleVisualEffects()
    {
        // 1. 애니메이션 속도 복구 로직 (Lerp)
        currentDashAnimMultiplier = Mathf.Lerp(currentDashAnimMultiplier, 1f, Time.deltaTime * animRestoreSpeed);
        if (playerAnimator != null)
        {
            playerAnimator.speed = currentDashAnimMultiplier;

            float currentSpeed = rb.linearVelocity.magnitude;
            playerAnimator.SetFloat(speedHash, currentSpeed, 0.1f, Time.deltaTime);
        }

        // 2. 모션 블러 강도 복구 로직
        if (motionBlur != null)
        {
            // 대쉬 배속에 비례해서 블러 강도 조절 (배속이 1이면 블러 0, 배속이 높으면 블러 증가)
            float blurTarget = (currentDashAnimMultiplier - 1f) / (dashAnimBoost - 1f) * maxBlurIntensity;
            motionBlur.intensity.value = Mathf.Clamp(blurTarget, 0, maxBlurIntensity);
        }
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

    private void TryDash()
    {
        // 쿨타임 확인
        if (Time.time >= lastDashTime + dashCooldown)
        {
            // 카메라가 바라보는 방향으로 순간적인 힘 가하기
            // ForceMode.Impulse는 질량을 고려한 순간적인 충격을 줍니다.
            rb.AddForce(cameraTransform.forward * dashForce, ForceMode.Impulse);

            // 마지막 대쉬 시간 업데이트
            lastDashTime = Time.time;

            currentDashAnimMultiplier = dashAnimBoost;
            // 대쉬 애니메이션 트리거 (필요 시)
            // if (playerAnimator != null) playerAnimator.SetTrigger("Dash");

            Debug.Log("Dash!");
        }
        else
        {
            Debug.Log("Dash on Cooldown...");
        }
    }
}