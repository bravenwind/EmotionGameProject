using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class ZeroGravityMovement : MonoBehaviour
{
    [Header("기본 이동 설정 (Movement)")]
    [SerializeField] private Transform cameraTransform;
    public float acceleration = 15f;
    public float normalMaxSpeed = 10f;
    public float dampingOnIdle = 0.5f;
    public float dampingOnBrake = 3.0f;

    [Header("스프린트 (Sustain Dash)")]
    public bool infiniteStamina = false;
    public float sprintAcceleration = 25f;
    public float sprintMaxSpeed = 25f;
    public float staminaMax = 100f;
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 10f;
    public float tiredSeconds = 2.0f;
    public float tiredTimer = 0.0f;
    public bool tired = false;
    public Image staminaImage;

    [Range(0, 100)]
    public float currentStamina;
    private bool isSprinting;

    [Header("애니메이션")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string speedParamName = "Speed";
    private int speedHash;

    [Header("이펙트 - 포스트 프로세싱")]
    public Volume globalVolume;
    private MotionBlur motionBlur;
    public float maxBlurIntensity = 1f;

    [Header("이펙트 - 카메라 FOV (스프린트 전용)")]
    public CinemachineCamera characterCamera;
    public float sprintFovBoost = 10f;
    public float fovChangeSpeed = 5f;

    private float defaultFov;
    private float targetFov;

    [Header("이펙트 - 주기적 파티클 생성")]
    public float particleSpawnInterval = 0.1f;
    public bool enableParticleSpawning = true;
    private float particleTimer = 0f;

    [Header("캐릭터 크기 설정")]
    public Transform scaleTransform;
    public Vector3 currentScale;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        speedHash = Animator.StringToHash(speedParamName);

        scaleTransform.localScale = Vector3.one * DataManager.Instance.playerOriginalScale;
        currentScale = scaleTransform.localScale;

        currentStamina = staminaMax;

        if (globalVolume != null && globalVolume.profile.TryGet<MotionBlur>(out var blur))
            motionBlur = blur;

        if (characterCamera != null)
        {
            defaultFov = characterCamera.Lens.FieldOfView;
            targetFov = defaultFov;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        HandleInput();

        if (tired)
        {
            tiredTimer += dt;
            if (tiredTimer > tiredSeconds)
            {
                tired = false;
                tiredTimer = 0.0f;
            }
        }
        else
        {
            HandleStamina(dt);
        }

        HandleVisualEffects(dt);
        HandleParticleSpawn(dt);
    }

    void FixedUpdate()
    {
        MovePhysics();
    }

    void HandleInput()
    {
        bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S);
        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        isSprinting = shiftPressed && (currentStamina > 0) && isMoving;
    }

    void HandleStamina(float dt)
    {
        if (isSprinting)
        {
            if (!infiniteStamina)
            {
                currentStamina -= staminaDrainRate * dt;
                if (currentStamina < 0) tired = true;
            }
        }
        else
        {
            currentStamina += staminaRegenRate * dt;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0, staminaMax);
        staminaImage.fillAmount = currentStamina / staminaMax;
    }

    void MovePhysics()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            rb.linearDamping = dampingOnBrake;
            return;
        }
        else
        {
            rb.linearDamping = dampingOnIdle;
        }

        if (Input.GetKey(KeyCode.W))
        {
            float currentAccel = isSprinting ? sprintAcceleration : acceleration;
            rb.AddForce(cameraTransform.forward * currentAccel, ForceMode.Acceleration);
        }

        float currentSpeedLimit = isSprinting ? sprintMaxSpeed : normalMaxSpeed;

        if (rb.linearVelocity.magnitude > currentSpeedLimit)
        {
            rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, currentSpeedLimit);
        }
    }

    void HandleVisualEffects(float dt)
    {
        if (characterCamera != null)
        {
            float finalTargetFov = isSprinting ? (defaultFov + sprintFovBoost) : defaultFov;
            targetFov = Mathf.Lerp(targetFov, finalTargetFov, dt * fovChangeSpeed);
            characterCamera.Lens.FieldOfView = Mathf.Lerp(characterCamera.Lens.FieldOfView, targetFov, dt * fovChangeSpeed);
        }

        if (playerAnimator != null)
        {
            float currentSpeed = rb.linearVelocity.magnitude;
            playerAnimator.SetFloat(speedHash, currentSpeed, 0.1f, dt);
            float targetAnimMult = isSprinting ? 1.5f : 1f;
            playerAnimator.speed = Mathf.Lerp(playerAnimator.speed, targetAnimMult, dt * 5f);
        }

        if (motionBlur != null)
        {
            float speedRatio = rb.linearVelocity.magnitude / sprintMaxSpeed;
            float targetBlur = Mathf.Clamp01(speedRatio) * maxBlurIntensity;
            motionBlur.intensity.value = Mathf.Lerp(motionBlur.intensity.value, targetBlur, dt * 5f);
        }
    }

    void HandleParticleSpawn(float dt)
    {
        if (!isSprinting || !enableParticleSpawning) return;

        particleTimer += dt;

        if (particleTimer >= particleSpawnInterval)
        {
            SpawnParticle();
            particleTimer = 0f;
        }
    }

    void SpawnParticle()
    {
        // ✨ [수정] Instantiate 대신 PoolManager의 함수 호출
        if (ParticlePoolManager.Instance != null)
        {
            ParticlePoolManager.Instance.SpawnParticle(transform.position, transform.rotation);
        }
        else
        {
            Debug.LogWarning("씬에 ParticlePoolManager가 없습니다! 생성해주세요.");
        }
    }
}