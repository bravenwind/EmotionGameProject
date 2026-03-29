using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("���� (Settings)")]
    public float mouseSensitivity = 100f; // ���콺 ����
    public Transform playerBody;          // �÷��̾� ���� (ȸ����ų ���)

    [Header("ī�޶� ��ġ ����")]
    // ĳ���� �� �ڿ����� ������ (x:�¿�, y:����, z:�ڷΰ� �Ÿ�)
    // ��: (0, 2, -5) -> ĳ���� �߽ɿ��� ���� 2, �ڷ� 5��ŭ ������ ��ġ
    public Vector3 cameraOffset = new Vector3(0f, 2.0f, -5.0f);

    // ī�޶� ĳ���ͺ��� ��¦ ���� ���� ���� (0�̸� ����, ���� ������ ���� ����)
    public float lookAngleOffset = 0f;

    private float xRotation = 0f; // ���Ʒ� (Pitch)
    private float yRotation = 0f; // �¿� (Yaw)

    public void SetRotation(float yaw, float pitch = 0f)
    {
        yRotation = yaw;
        xRotation = pitch;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // ���콺 Ŀ�� ����
    }

    void LateUpdate()
    {
        if (playerBody == null) return;

        // 1. ���콺 �Է�
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 2. ȸ���� ����
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // ���� ���� ����

        // 3. �÷��̾� ���� ȸ�� (�ٽ� ������!)
        // ���� ĳ���� ���� ��ü�� ���콺 ����(���Ʒ� ����)�� ������ ���󰩴ϴ�.
        Quaternion targetRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        playerBody.rotation = targetRotation;

        // 4. ī�޶� ��ġ ��� (���۸� �� ��)
        // TransformPoint�� ���� ��ǥ�� ���� ��ǥ�� ��ȯ���ݴϴ�.
        // ��, ĳ���Ͱ� ȸ���� ���� �������� �� �� ��ġ�� ã�Ƴ��ϴ�.
        Vector3 desiredPosition = playerBody.TransformPoint(cameraOffset);

        // 5. ī�޶� ����
        transform.position = desiredPosition;

        // ī�޶�� ĳ���Ϳ� �Ȱ��� ������ ���ų�, �ణ�� ���� ������ �ݴϴ�.
        transform.rotation = targetRotation * Quaternion.Euler(lookAngleOffset, 0, 0);
    }
}