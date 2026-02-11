using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using UnityEngine.UI;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody))]
public class ZeroGravityMovement : MonoBehaviour
{
    [Header("기본 이동 설정 (Movement)")]
    [SerializeField] private Transform cameraTransform;
    public float acceleration = 15f;
    public float normalMaxSpeed = 10f; // 평상시 최대 속도
    public float dampingOnIdle = 0.5f;
    public float dampingOnBrake = 3.0f;

    [Header("스프린트 (Sustain Dash)")]
    public bool infiniteStamina = false;

    public float sprintAcceleration = 25f; // 달리기 가속도
    public float sprintMaxSpeed = 25f;     // 달리기 최대 속도
    public float staminaMax = 100f;        // 최대 스태미나
    public float staminaDrainRate = 20f;   // 초당 소모량
    public float staminaRegenRate = 10f;   // 초당 회복량

    public float tiredSeconds = 2.0f;
    public float tiredTimer = 0.0f;
    public bool tired = false;

    public Image staminaImage;

    [Range(0, 100)]
    public float currentStamina;           // 현재 스태미나 (Inspector 확인용)
    private bool isSprinting;

    [Header("애니메이션 (Animation)")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string speedParamName = "Speed";
    private int speedHash;

    // 애니메이션 속도 제어
    private float currentAnimSpeedMult = 1f;
    public float animRestoreSpeed = 2f;

    [Header("이펙트 - 포스트 프로세싱")]
    public Volume globalVolume;
    private MotionBlur motionBlur;
    public float maxBlurIntensity = 1f;

    [Header("이펙트 - 카메라 FOV")]
    public CinemachineCamera characterCamera;
    public float sprintFovBoost = 10f;     // 스프린트 시 늘어날 FOV
    public float fovChangeSpeed = 5f;      // FOV가 변하는 속도 (Lerp)

    private float defaultFov;
    private float targetFov;               // 목표 FOV

    [Header("이펙트 - 잔상 (Ghost Trail)")]
    public SkinnedMeshRenderer characterMesh;
    public Material ghostMaterial;
    public float ghostDuration = 0.5f;
    public float ghostSpawnInterval = 0.05f;
    public float ghostLifeTime = 0.5f;

    private Rigidbody rb;

    [Header("캐릭터 크기 설정")]
    public Transform scaleTransform;
    public Vector3 currentScale;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        speedHash = Animator.StringToHash(speedParamName);
        scaleTransform.localScale = Vector3.one * DataManager.Instance.playerOriginalScale;
        currentScale = scaleTransform.localScale;

        currentStamina = staminaMax; // 스태미나 초기화

        if (globalVolume != null && globalVolume.profile.TryGet<MotionBlur>(out var blur))
            motionBlur = blur;

        if (characterCamera != null)
        {
            defaultFov = characterCamera.Lens.FieldOfView;
            targetFov = defaultFov;
        }
        else
        {
            Debug.LogError("Cinemachine Camera를 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 1. 입력 감지
        HandleInput();

        // 2. 스태미나 관리
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

        // 3. 시각 효과 (FOV, Blur, Anim)
        HandleVisualEffects(dt);
    }

    void FixedUpdate()
    {
        MovePhysics();
    }

    // --- 로직 분리 ---

    void HandleInput()
    {
        // 스프린트 상태 체크 (Shift 누름 + 스태미나 있음 + 앞뒤 이동 중)
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
                if (currentStamina < 0)
                {
                    tired = true;
                }
            }
        }
        else
        {
            // 스태미나 회복
            currentStamina += staminaRegenRate * dt;
        }

        // 0 ~ Max 사이로 클램프
        currentStamina = Mathf.Clamp(currentStamina, 0, staminaMax);
        staminaImage.fillAmount = currentStamina / staminaMax;
    }

    void MovePhysics()
    {
        // 1. 브레이크 (Space)
        if (Input.GetKey(KeyCode.Space))
        {
            rb.linearDamping = dampingOnBrake;
            return; // 브레이크 중엔 가속 안함
        }
        else
        {
            rb.linearDamping = dampingOnIdle;
        }

        // 2. 가속력 적용 (W 키)
        if (Input.GetKey(KeyCode.W))
        {
            // 스프린트 여부에 따라 다른 가속도 적용
            float currentAccel = isSprinting ? sprintAcceleration : acceleration;
            rb.AddForce(cameraTransform.forward * currentAccel, ForceMode.Acceleration);
        }

        // 3. [핵심] 최대 속도 제한 로직
        // 현재 상태에 따른 속도 제한값 결정
        float currentSpeedLimit = isSprinting ? sprintMaxSpeed : normalMaxSpeed;

        // 현재 속도가 제한을 넘으면 잘라냄 (Clamping)
        if (rb.linearVelocity.magnitude > currentSpeedLimit)
        {
            rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, currentSpeedLimit);
        }
    }

    void HandleVisualEffects(float dt)
    {
        // --- 1. FOV 제어 ---
        if (characterCamera != null)
        {
            // 기본 목표: 스프린트 중이면 넓게, 아니면 원래대로
            float baseTarget = isSprinting ? (defaultFov + sprintFovBoost) : defaultFov;

            // Impulse 효과 등으로 인해 설정된 targetFov와 부드럽게 섞기
            // (Impulse 코루틴이 targetFov를 잠시 건드릴 수 있음, 여기서는 베이스로 복귀하려는 힘)
            targetFov = Mathf.Lerp(targetFov, baseTarget, dt * fovChangeSpeed);

            characterCamera.Lens.FieldOfView = Mathf.Lerp(characterCamera.Lens.FieldOfView, targetFov, dt * fovChangeSpeed);
        }

        // --- 2. 애니메이션 ---
        if (playerAnimator != null)
        {
            // 실제 속도 기반 파라미터 전달
            float currentSpeed = rb.linearVelocity.magnitude;
            playerAnimator.SetFloat(speedHash, currentSpeed, 0.1f, dt);

            // (선택) 스프린트 중일 때 재생 속도를 좀 더 빠르게?
            float targetAnimMult = isSprinting ? 1.5f : 1f;
            playerAnimator.speed = Mathf.Lerp(playerAnimator.speed, targetAnimMult, dt * 5f);
        }

        // --- 3. 모션 블러 ---
        if (motionBlur != null)
        {
            // 속도가 빠를수록 블러 강해짐 (최대 속도 기준 비율)
            float speedRatio = rb.linearVelocity.magnitude / sprintMaxSpeed;
            float targetBlur = Mathf.Clamp01(speedRatio) * maxBlurIntensity;

            motionBlur.intensity.value = Mathf.Lerp(motionBlur.intensity.value, targetBlur, dt * 5f);
        }
    }

    public IEnumerator IncreaseScale(float increaseTime)
    {
        if (DataManager.Instance.currentScaleLevel == DataManager.Instance.maxScaleLevel)
        {
            yield break;
        }

        //DataManager.Instance.detectRadius = DataManager.Instance.originalDetectRadius + DataManager.Instance.detectPlusRadiusPerLevel * DataManager.Instance.playerCurrentScaleLevel;

        //if (softBody3D != null) softBody3D.DisableCloth();

        if (PlaySFXAudio.Instance != null) PlaySFXAudio.Instance.PlayScaleUpSound();

        //uIPoolManager.SpawnUI(scaleIncreasedEffect, transform);

        Vector3 startScale = currentScale;
        Vector3 originalScale = Vector3.one * DataManager.Instance.playerOriginalScale;
        Vector3 targetScale = originalScale * DataManager.Instance.playerScalePerLevel[DataManager.Instance.currentScaleLevel];

        Debug.Log(startScale);
        Debug.Log(originalScale);
        Debug.Log(targetScale);

        float t = 0f;

        while (t < increaseTime)
        {
            t += Time.deltaTime;
            float progress = t / increaseTime;

            currentScale = Vector3.Lerp(startScale, targetScale, progress);
            Debug.Log(currentScale);
            scaleTransform.localScale = currentScale;

            yield return null;
        }

        scaleTransform.localScale = targetScale;
        currentScale = targetScale;
        //playerController.moveSpeed *= 1.2f;

        //if (softBody3D != null)
        //{
        //    StartCoroutine(softBody3D.EnableAndRebuildCloth());
        //}
    }

    // --- 잔상 코루틴 (기존 유지) ---
    IEnumerator ShowGhostTrail()
    {
        float timeElapsed = 0f;
        while (timeElapsed < ghostDuration)
        {
            CreateGhostMesh();
            timeElapsed += ghostSpawnInterval;
            yield return new WaitForSeconds(ghostSpawnInterval);
        }
    }

    void CreateGhostMesh()
    {
        GameObject ghostObj = new GameObject("GhostTrail");
        ghostObj.transform.position = characterMesh.transform.position;
        ghostObj.transform.rotation = characterMesh.transform.rotation;
        ghostObj.transform.localScale = characterMesh.transform.localScale;
        // ghostObj.layer = LayerMask.NameToLayer("Ignore Raycast"); // 필요시 레이어 설정

        MeshFilter meshFilter = ghostObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = ghostObj.AddComponent<MeshRenderer>();

        Mesh snapshotMesh = new Mesh();
        characterMesh.BakeMesh(snapshotMesh);
        meshFilter.mesh = snapshotMesh;
        meshRenderer.material = ghostMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off; // 그림자 끄기 (최적화)

        StartCoroutine(FadeAndDestroyGhost(ghostObj, meshRenderer.material));
    }

    IEnumerator FadeAndDestroyGhost(GameObject ghostObj, Material mat)
    {
        float fadeTime = 0f;
        Color startColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
        float startAlpha = startColor.a;

        while (fadeTime < ghostLifeTime)
        {
            fadeTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, fadeTime / ghostLifeTime);

            if (mat.HasProperty("_BaseColor"))
            {
                Color color = startColor;
                color.a = alpha;
                mat.SetColor("_BaseColor", color);
            }
            yield return null;
        }
        Destroy(ghostObj);
    }
}