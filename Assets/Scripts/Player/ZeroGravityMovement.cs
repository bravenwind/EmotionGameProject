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
    public float sprintFovBoost = 10f;       // 스프린트 시 추가될 FOV
    public float fovChangeSpeed = 5f;        // FOV 변화 속도

    private float defaultFov;
    private float targetFov;

    [Header("이펙트 - 거대화 카메라 거리 설정")]
    public MouseLook mouseLookScript;        // [필수] MouseLook 스크립트를 연결해주세요!

    // 1레벨당 늘어날 거리 (예: y는 1만큼 위로, z는 -2만큼 뒤로)
    public Vector3 offsetIncreasePerLevel = new Vector3(0f, 1.0f, -2.0f);

    private Vector3 originalCameraOffset;    // 게임 시작 시의 기본 오프셋 저장용
    public UIFollowTarget scaleIncreaseEffect;

    // ✨ [추가] UI가 생성될 캔버스의 Transform (Hierarchy에 있는 Canvas 지정)
    public Transform uiCanvasRoot;

    public UIFollowTarget dashEffectPrefab;

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

        currentStamina = staminaMax;

        if (globalVolume != null && globalVolume.profile.TryGet<MotionBlur>(out var blur))
            motionBlur = blur;

        if (characterCamera != null)
        {
            defaultFov = characterCamera.Lens.FieldOfView;
            targetFov = defaultFov;
        }

        // [추가] 초기 카메라 오프셋 저장 및 현재 레벨 반영
        if (mouseLookScript != null)
        {
            // MouseLook 스크립트의 현재 값을 원본으로 저장
            // 주의: MouseLook 스크립트에서 초기값을 Start에서 세팅한다면, 실행 순서에 주의해야 함.
            // 여기서는 Inspector에 설정된 값을 원본으로 간주합니다.
            originalCameraOffset = mouseLookScript.cameraOffset;

            // 만약 게임 시작 시 레벨이 0이 아니라면 바로 적용
            //Vector3 startLevelOffset = originalCameraOffset + (offsetIncreasePerLevel * DataManager.Instance.currentScaleLevel);
            //mouseLookScript.cameraOffset = startLevelOffset;
        }
        else
        {
            Debug.LogError("MouseLook 스크립트가 연결되지 않았습니다! Inspector에서 할당해주세요.");
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
    }

    void FixedUpdate()
    {
        MovePhysics();
    }

    void HandleInput()
    {
        bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S);

        // ✨ [추가] Shift 키를 '누르는 순간' (GetKeyDown) 감지
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            // (옵션) 이동 중이고 스테미너가 남아있을 때만 이펙트가 나오게 하려면 조건 추가
            if (isMoving && currentStamina > 0)
            {
                if (dashEffectPrefab != null)
                {
                    //UIPoolManager.Instance.SpawnUI(dashEffectPrefab, transform);
                }
            }
        }

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
        // --- 1. FOV 제어 (이제 스프린트만 담당) ---
        if (characterCamera != null)
        {
            // 거대화 로직 제거됨: 오직 스프린트 여부에 따라 FOV 변경
            float finalTargetFov = isSprinting ? (defaultFov + sprintFovBoost) : defaultFov;

            targetFov = Mathf.Lerp(targetFov, finalTargetFov, dt * fovChangeSpeed);
            characterCamera.Lens.FieldOfView = Mathf.Lerp(characterCamera.Lens.FieldOfView, targetFov, dt * fovChangeSpeed);
        }

        // --- 2. 애니메이션 ---
        if (playerAnimator != null)
        {
            float currentSpeed = rb.linearVelocity.magnitude;
            playerAnimator.SetFloat(speedHash, currentSpeed, 0.1f, dt);

            float targetAnimMult = isSprinting ? 1.5f : 1f;
            playerAnimator.speed = Mathf.Lerp(playerAnimator.speed, targetAnimMult, dt * 5f);
        }

        // --- 3. 모션 블러 ---
        if (motionBlur != null)
        {
            float speedRatio = rb.linearVelocity.magnitude / sprintMaxSpeed;
            float targetBlur = Mathf.Clamp01(speedRatio) * maxBlurIntensity;

            motionBlur.intensity.value = Mathf.Lerp(motionBlur.intensity.value, targetBlur, dt * 5f);
        }
    }

    //public IEnumerator IncreaseScale(float increaseTime)
    //{
    //    if (DataManager.Instance.currentScaleLevel >= DataManager.Instance.maxScaleLevel)
    //    {
    //        yield break;
    //    }

    //    // 레벨 증가
    //    DataManager.Instance.currentScaleLevel++;

    //    // ✨ 1. 이펙트 생성 (UI Canvas 아래에 생성해야 보입니다)
    //    if (scaleIncreaseEffectPrefab != null && uiCanvasRoot != null)
    //    {
    //        // 프리팹 생성
    //        UIFollowTarget effectInstance = Instantiate(scaleIncreaseEffectPrefab, uiCanvasRoot);

    //        // 타겟 설정 (현재 플레이어)
    //        effectInstance.SetTarget(this.transform);

    //        // (옵션) 사운드 재생
    //        // if (PlaySFXAudio.Instance != null) PlaySFXAudio.Instance.PlayScaleUpSound();
    //    }
    //    else
    //    {
    //        Debug.LogWarning("ScaleEffectPrefab 또는 UICanvasRoot가 연결되지 않았습니다.");
    //    }

    //    // 2. 크기(Scale) 계산
    //    Vector3 startScale = currentScale;
    //    // DataManager 배열 인덱스 에러 방지용 Clamp
    //    int levelIndex = Mathf.Clamp(DataManager.Instance.currentScaleLevel, 0, DataManager.Instance.playerScalePerLevel.Count - 1);
    //    Vector3 targetScale = Vector3.one * DataManager.Instance.playerScalePerLevel[levelIndex];

    //    // 3. 카메라 오프셋(거리) 계산
    //    Vector3 startOffset = mouseLookScript.cameraOffset;
    //    Vector3 targetOffset = originalCameraOffset + (offsetIncreasePerLevel * DataManager.Instance.currentScaleLevel);

    //    float t = 0f;

    //    while (t < increaseTime)
    //    {
    //        t += Time.deltaTime;
    //        float progress = t / increaseTime;

    //        // 크기 보간
    //        currentScale = Vector3.Lerp(startScale, targetScale, progress);
    //        scaleTransform.localScale = currentScale;

    //        // 카메라 거리 보간
    //        if (mouseLookScript != null)
    //        {
    //            mouseLookScript.cameraOffset = Vector3.Lerp(startOffset, targetOffset, progress);
    //        }

    //        yield return null;
    //    }

    //    // 최종 값 확정
    //    scaleTransform.localScale = targetScale;
    //    currentScale = targetScale;

    //    if (mouseLookScript != null)
    //    {
    //        mouseLookScript.cameraOffset = targetOffset;
    //    }
    //}

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

        MeshFilter meshFilter = ghostObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = ghostObj.AddComponent<MeshRenderer>();

        Mesh snapshotMesh = new Mesh();
        characterMesh.BakeMesh(snapshotMesh);
        meshFilter.mesh = snapshotMesh;
        meshRenderer.material = ghostMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

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