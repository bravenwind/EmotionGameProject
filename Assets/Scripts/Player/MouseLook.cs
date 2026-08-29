using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("설정 (Settings)")]
    public float mouseSensitivity = 100f; // 마우스 감도
    public Transform playerBody;          // 회전시킬 플레이어 몸체

    [Header("카메라 위치 설정")]
    // 캐릭터 기준 카메라 오프셋 (x:좌우, y:상하, z:뒤로 물러난 거리)
    // 예: (0, 2, -5) -> 캐릭터 중심에서 위로 2, 뒤로 5만큼 떨어진 위치
    public Vector3 cameraOffset = new Vector3(0f, 2.0f, -5.0f);

    // 카메라가 캐릭터보다 살짝 위를 보게 하는 보정 각도 (0이면 정면)
    public float lookAngleOffset = 0f;

    [Header("입력 제어")]
    [Tooltip("false이면 마우스 입력을 완전히 무시합니다. 카메라의 캐릭터 추적은 계속 동작합니다. " +
             "게임 시작 연출이 끝나는 시점에 EnableInput()으로 열립니다.")]
    public bool inputEnabled = false;

    private float xRotation = 0f; // 상하 (Pitch)
    private float yRotation = 0f; // 좌우 (Yaw)

    // 게임 시작 시 바라봐야 하는 기준 각도 (PathManager가 첫 노드 방향으로 지정)
    private float initialYaw = 0f;
    private float initialPitch = 0f;
    private bool hasInitialRotation = false;

    // 커서 잠금/해제 직후 튀는 마우스 델타를 무시할 프레임 수
    private int suppressFrames = 0;

    /// <summary>
    /// 시선 각도를 직접 지정한다. 여기서 지정한 값이 '시작 각도'로 기억되어
    /// EnableInput() 시점에 복구된다.
    /// </summary>
    public void SetRotation(float yaw, float pitch = 0f)
    {
        yRotation = yaw;
        xRotation = Mathf.Clamp(pitch, -90f, 90f);

        initialYaw = yRotation;
        initialPitch = xRotation;
        hasInitialRotation = true;

        ApplyRotation();
    }

    /// <summary>기억해 둔 시작 각도로 되돌린다.</summary>
    public void ResetToInitialRotation()
    {
        if (!hasInitialRotation) return;

        yRotation = initialYaw;
        xRotation = initialPitch;
        ApplyRotation();
    }

    /// <summary>
    /// 플레이어에게 조작권을 넘긴다.
    /// 연출 중 흘러간 각도를 시작 각도로 복구하고, 쌓인 마우스 델타를 버린 뒤 입력을 연다.
    /// </summary>
    public void EnableInput()
    {
        ResetToInitialRotation();
        SuppressInput();
        inputEnabled = true;
    }

    /// <summary>마우스 입력만 잠근다. 카메라의 캐릭터 추적은 계속 유지된다.</summary>
    public void DisableInput()
    {
        inputEnabled = false;
    }

    /// <summary>
    /// 커서 잠금 상태가 바뀐 직후(일시정지 해제 등) 몇 프레임 동안 마우스 입력을 버린다.
    /// Cursor.lockState 변경 시 Unity가 큰 델타를 한 번 뱉기 때문에 시점이 튀는 것을 막는다.
    /// </summary>
    public void SuppressInput(int frames = 3)
    {
        suppressFrames = Mathf.Max(suppressFrames, frames);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // 씬에 배치된 초기 회전값을 그대로 이어받아 첫 프레임에 시점이 튀지 않게 한다.
        if (playerBody != null)
        {
            Vector3 euler = playerBody.rotation.eulerAngles;
            xRotation = Mathf.Clamp(Mathf.DeltaAngle(0f, euler.x), -90f, 90f);
            yRotation = euler.y;
        }
    }

    void LateUpdate()
    {
        if (playerBody == null) return;

        if (inputEnabled)
        {
            if (suppressFrames > 0)
            {
                // 값을 읽어서 버리기만 한다 (누적 델타 비우기)
                Input.GetAxis("Mouse X");
                Input.GetAxis("Mouse Y");
                suppressFrames--;
            }
            else
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

                yRotation += mouseX;
                xRotation -= mouseY;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f); // 상하 각도 제한
            }
        }

        // 입력이 잠겨 있어도 카메라는 계속 캐릭터를 따라간다.
        // (연출 중 카메라가 제자리에 멈춰 있다가 순간이동하는 문제 방지)
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        if (playerBody == null) return;

        // 캐릭터 몸체를 시선 방향으로 회전
        Quaternion targetRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        playerBody.rotation = targetRotation;

        // 회전한 캐릭터 기준으로 카메라 위치 계산 (로컬 오프셋 -> 월드 좌표)
        transform.position = playerBody.TransformPoint(cameraOffset);
        transform.rotation = targetRotation * Quaternion.Euler(lookAngleOffset, 0, 0);
    }
}
