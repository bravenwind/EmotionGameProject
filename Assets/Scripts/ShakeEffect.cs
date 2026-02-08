using UnityEngine;
using System.Collections;

public class ShakeEffect : MonoBehaviour
{
    public float shakeAmount = 0.1f; // 떨림 강도 (픽셀 단위 아님, 월드 좌표 단위)

    Coroutine currentCoroutine = null;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S)) 
        { 
            if (currentCoroutine == null)
            {
                StartShaking();
            }
        }    
    }

    public void StartShaking()
    {
        currentCoroutine = StartCoroutine(ShakeCoroutine());
    }

    IEnumerator ShakeCoroutine()
    {
        Vector3 originalPos = transform.localPosition; // 원래 위치 저장
        float duration = 0.5f; // 0.5초 동안 떨기
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // 랜덤한 원 안의 좌표를 생성하여 위치에 더함
            float x = Random.Range(-1f, 1f) * shakeAmount;
            float y = Random.Range(-1f, 1f) * shakeAmount;

            transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        transform.localPosition = originalPos; // 끝나면 원래 위치로 복귀

        currentCoroutine = null;
    }
}