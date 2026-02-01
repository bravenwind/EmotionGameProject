using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("설정 (Settings)")]
    public float mouseSensitivity = 100f; // 마우스 감도
    public Transform playerBody;          // 플레이어 몸통 (전체 회전용)

    private float xRotation = 0f; // 위아래 각도 누적
    private float yRotation = 0f; // 좌우 각도 누적

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // 마우스 숨김 및 고정

        // 시작할 때 현재 플레이어의 회전값을 기준으로 잡음 (갑자기 튀는 현상 방지)
        if (playerBody != null)
        {
            Vector3 currentRotation = playerBody.localRotation.eulerAngles;
            yRotation = currentRotation.y;
            xRotation = currentRotation.x;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            Application.Quit();
        }

        // 1. 마우스 입력 받기
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 2. 회전값 계산
        // 마우스 좌우 이동 -> Y축 회전 (Yaw)
        yRotation += mouseX;

        // 마우스 위아래 이동 -> X축 회전 (Pitch)
        xRotation -= mouseY;

        // 무중력에서는 고개를 완전히 뒤로 젖혀 한바퀴 돌 수도 있으므로 
        // Clamp(제한)을 -90~90으로 할지, 제한을 풀지 결정해야 합니다.
        // 편의상 90도로 제한하되, 원하시면 아래 Clamp 줄을 지우셔도 됩니다.
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // 3. 회전 적용 (가장 중요한 부분!)
        // 기존과 다르게 '카메라'만 돌리는 게 아니라 '플레이어 몸통 전체'를 돌립니다.
        // Z축(Roll)은 0으로 고정하여 화면이 옆으로 기울어지는 멀미 현상을 막습니다.
        if (playerBody != null)
        {
            playerBody.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);

            // 카메라는 플레이어 몸통을 따라가기만 하면 되므로 
            // 카메라 자체의 로컬 회전은 0으로 초기화해줍니다. (혹시 모를 꼬임 방지)
            transform.localRotation = Quaternion.identity;
        }
    }
}