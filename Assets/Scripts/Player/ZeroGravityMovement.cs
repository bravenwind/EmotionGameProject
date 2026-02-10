using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody))]
public class ZeroGravityMovement : MonoBehaviour
{
    [Header("기본 설정 (Settings)")]
    [SerializeField]
    private Transform cameraTransform;
    public float acceleration = 15f;
    public float dampingOnIdle = 0.5f;
    public float dampingOnBrake = 3.0f;

    [Header("대쉬 설정 (Dash Settings)")]
    public float dashForce = 20f;       // 대쉬 힘 (순간 가속)
    public float dashCooldown = 1.5f;   // 대쉬 재사용 대기시간
    private float lastDashTime = -100f; // 마지막 대쉬 시점 저장

    [Header("애니메이션 (Animation)")]
    [SerializeField]
    private Animator playerAnimator;
    [SerializeField]
    private string speedParamName = "Speed";
    private int speedHash;

    // 애니메이션 속도 제어
    private float currentDashAnimMultiplier = 1f;
    public float dashAnimBoost = 2.5f; // 대쉬 시 애니메이션 배속
    public float animRestoreSpeed = 2f; // 원래 속도로 돌아오는 속도

    [Header("이펙트 - 포스트 프로세싱 (VFX)")]
    public Volume globalVolume;
    private MotionBlur motionBlur;
    public float maxBlurIntensity = 1f;

    // ---------------- [추가된 기능 시작] ----------------
    [Header("이펙트 - 카메라 FOV (Camera Effect)")]
    public float fovBoostAmount = 10f; // 대쉬할 때 늘어날 FOV 양 (예: 60 -> 70)
    public float fovRestoreSpeed = 5f; // 원래 FOV로 복구되는 속도
    private Camera mainCamera;
    private float defaultFov;
    private float currentTargetFov;

    [Header("이펙트 - 잔상 (Ghost Trail)")]
    public SkinnedMeshRenderer characterMesh; // 캐릭터의 메쉬 (Inspector에서 연결 필수)
    public Material ghostMaterial;            // 잔상에 쓰일 반투명 머티리얼 (Inspector에서 연결 필수)
    public float ghostDuration = 0.5f;        // 잔상이 생성되는 총 시간
    public float ghostSpawnInterval = 0.05f;  // 잔상 생성 간격
    public float ghostLifeTime = 0.5f;        // 생성된 잔상이 사라지는데 걸리는 시간
    // ---------------- [추가된 기능 끝] ----------------

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        speedHash = Animator.StringToHash(speedParamName);

        // Motion Blur 가져오기
        if (globalVolume != null && globalVolume.profile.TryGet<MotionBlur>(out var blur))
        {
            motionBlur = blur;
        }

        // [FOV 초기화]
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            defaultFov = mainCamera.fieldOfView;
            currentTargetFov = defaultFov;
        }
        else
        {
            Debug.LogError("Main Camera를 찾을 수 없습니다. 태그를 확인하세요.");
        }
    }

    void Update()
    {
        // --- 입력 감지 ---
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            TryDash();
        }

        HandleVisualEffects();
    }

    void HandleVisualEffects()
    {
        float dt = Time.deltaTime;

        // 1. 애니메이션 속도 복구
        currentDashAnimMultiplier = Mathf.Lerp(currentDashAnimMultiplier, 1f, dt * animRestoreSpeed);
        if (playerAnimator != null)
        {
            playerAnimator.speed = currentDashAnimMultiplier;
            float currentSpeed = rb.linearVelocity.magnitude;
            playerAnimator.SetFloat(speedHash, currentSpeed, 0.1f, dt);
        }

        // 2. 모션 블러 강도 제어
        if (motionBlur != null)
        {
            float blurTarget = (currentDashAnimMultiplier - 1f) / (dashAnimBoost - 1f) * maxBlurIntensity;
            if (motionBlur.intensity.value != blurTarget) // 최적화: 값 변화 있을때만 할당
                motionBlur.intensity.value = Mathf.Clamp(blurTarget, 0, maxBlurIntensity);
        }

        // 3. [추가됨] FOV 제어 로직
        if (mainCamera != null)
        {
            // 목표 FOV는 시간이 지남에 따라 원래 값(defaultFov)으로 돌아가려 함
            currentTargetFov = Mathf.Lerp(currentTargetFov, defaultFov, dt * fovRestoreSpeed);
            // 실제 카메라 FOV 적용
            mainCamera.fieldOfView = currentTargetFov;
        }
    }

    void FixedUpdate()
    {
        // --- 물리 이동 로직 ---
        if (Input.GetKey(KeyCode.Space))
        {
            rb.linearDamping = dampingOnBrake;
        }
        else
        {
            rb.linearDamping = dampingOnIdle;
        }

        if (Input.GetKey(KeyCode.W))
        {
            rb.AddForce(cameraTransform.forward * acceleration, ForceMode.Acceleration);
        }
    }

    private void TryDash()
    {
        if (Time.time >= lastDashTime + dashCooldown)
        {
            // 1. 물리 힘 적용
            rb.AddForce(cameraTransform.forward * dashForce, ForceMode.Impulse);
            lastDashTime = Time.time;

            // 2. 애니메이션 부스트 설정
            currentDashAnimMultiplier = dashAnimBoost;

            // 3. [추가됨] FOV 부스트 (순간적으로 목표 FOV를 높임)
            currentTargetFov = defaultFov + fovBoostAmount;

            // 4. [추가됨] 잔상 효과 코루틴 시작
            if (characterMesh != null && ghostMaterial != null)
            {
                StartCoroutine(ShowGhostTrail());
            }

            Debug.Log("Dash!");
        }
        else
        {
            Debug.Log("Dash on Cooldown...");
        }
    }

    // --- [추가됨] 잔상(Ghost Trail) 관련 코루틴 ---
    IEnumerator ShowGhostTrail()
    {
        float timeElapsed = 0f;

        while (timeElapsed < ghostDuration)
        {
            // 현재 캐릭터의 포즈를 베이크하여 잔상 생성
            CreateGhostMesh();

            timeElapsed += ghostSpawnInterval;
            yield return new WaitForSeconds(ghostSpawnInterval);
        }
    }

    void CreateGhostMesh()
    {
        // 1. 빈 게임오브젝트 생성
        GameObject ghostObj = new GameObject("GhostTrail");
        ghostObj.transform.position = characterMesh.transform.position;
        ghostObj.transform.rotation = characterMesh.transform.rotation;
        ghostObj.transform.localScale = characterMesh.transform.localScale; // 스케일도 맞춤

        // 2. 메쉬 필터 & 렌더러 추가
        MeshFilter meshFilter = ghostObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = ghostObj.AddComponent<MeshRenderer>();

        // 3. 현재 포즈(Skinned Mesh)를 스냅샷 찍어서 새 메쉬로 만듦
        Mesh snapshotMesh = new Mesh();
        characterMesh.BakeMesh(snapshotMesh);
        meshFilter.mesh = snapshotMesh;

        // 4. 머티리얼 할당
        meshRenderer.material = ghostMaterial;

        // 5. 서서히 사라지게 하기
        StartCoroutine(FadeAndDestroyGhost(ghostObj, meshRenderer.material));
    }

    IEnumerator FadeAndDestroyGhost(GameObject ghostObj, Material mat)
    {
        float fadeTime = 0f;
        Color startColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
        float startAlpha = startColor.a; // 머티리얼의 초기 알파값

        while (fadeTime < ghostLifeTime)
        {
            fadeTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, fadeTime / ghostLifeTime);

            // URP Lit 셰이더 기준 알파값 변경
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