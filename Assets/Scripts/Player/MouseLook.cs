using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("설정 (Settings)")]
    public float mouseSensitivity = 100f; // 마우스 감도
    public Transform playerBody;          // 플레이어 몸통 (회전시킬 대상)

    [Header("카메라 위치 조정")]
    // 캐릭터 등 뒤에서의 오프셋 (x:좌우, y:높이, z:뒤로갈 거리)
    // 예: (0, 2, -5) -> 캐릭터 중심에서 위로 2, 뒤로 5만큼 떨어진 위치
    public Vector3 cameraOffset = new Vector3(0f, 2.0f, -5.0f);

    // 카메라가 캐릭터보다 살짝 위를 보게 할지 (0이면 정면, 값이 있으면 각도 조절)
    public float lookAngleOffset = 0f;

    private float xRotation = 0f; // 위아래 (Pitch)
    private float yRotation = 0f; // 좌우 (Yaw)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // 마우스 커서 고정
    }

    void LateUpdate()
    {
        if (playerBody == null) return;

        // 1. 마우스 입력
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 2. 회전값 누적
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // 고개 꺾임 제한

        // 3. 플레이어 몸통 회전 (핵심 변경점!)
        // 이제 캐릭터 몸통 자체가 마우스 방향(위아래 포함)을 완전히 따라갑니다.
        Quaternion targetRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        playerBody.rotation = targetRotation;

        // 4. 카메라 위치 잡기 (슈퍼맨 등 뒤)
        // TransformPoint는 로컬 좌표를 월드 좌표로 변환해줍니다.
        // 즉, 캐릭터가 회전한 상태 기준으로 등 뒤 위치를 찾아냅니다.
        Vector3 desiredPosition = playerBody.TransformPoint(cameraOffset);

        // 5. 카메라 적용
        transform.position = desiredPosition;

        // 카메라는 캐릭터와 똑같은 방향을 보거나, 약간의 각도 조절을 줍니다.
        transform.rotation = targetRotation * Quaternion.Euler(lookAngleOffset, 0, 0);
    }
}