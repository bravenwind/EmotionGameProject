using UnityEngine;
using System.Collections;

public class CharacterFocus : MonoBehaviour
{
    [Header("설정 값")]
    public Transform targetPosition; // 이동할 목표 위치 (빈 오브젝트 등으로 위치 지정)
    public float targetScale = 90.0f; // 목표 크기 (예: 2배)
    public Vector3 targetRotation = new Vector3(-90, 90, 90);
    public float duration = 1.5f;    // 걸리는 시간 (초)
    public AnimationCurve motionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 부드러운 움직임 곡선

    public Transform animationPosition;
    public float failAnimationMinusY;
    public Vector3 animationRotation = new Vector3(-90, 90, 90);

    private Vector3 startPos;
    private Vector3 startScale;
    private Quaternion startRot;

    // 테스트용: 게임 시작 시 자동 실행하려면 주석 해제
    // void Start() { FocusCharacter(); }

    // IEnumerator를 public으로 변경합니다.
    public IEnumerator AnimateCharacter()
    {
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(targetRotation);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = motionCurve.Evaluate(elapsedTime / duration);

            transform.position = Vector3.Lerp(startPos, targetPosition.position, t);
            transform.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        transform.position = targetPosition.position;
        transform.localScale = Vector3.one * targetScale;
        transform.rotation = targetRot;
    }

    public void ApplyAnimationOnCharacter()
    {
        if (DataManager.Instance.gameCleared)
        {
            transform.position = animationPosition.position;
            transform.rotation = Quaternion.Euler(animationRotation);
        }
        else
        {
            transform.position = animationPosition.position + new Vector3(0.0f, failAnimationMinusY, 0.0f);
        }
    }
}