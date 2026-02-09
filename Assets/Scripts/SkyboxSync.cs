using UnityEngine;

public class SkyboxSync : MonoBehaviour
{
    public Camera mainCamera; // Overlay로 설정된 메인 카메라

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // 위치는 따라가되(선택), 회전은 반드시 따라가야 함
            transform.rotation = mainCamera.transform.rotation;

            // 만약 스카이박스에 위치 이동 느낌도 주고 싶다면 아래 주석 해제 (보통 스카이박스는 무한원이라 필요 없음)
            // transform.position = mainCamera.transform.position;
        }
    }
}