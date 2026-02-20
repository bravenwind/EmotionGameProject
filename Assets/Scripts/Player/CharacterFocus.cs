using UnityEngine;
using System.Collections;

public class CharacterFocus : MonoBehaviour
{
    public static CharacterFocus Instance;

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

    public void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 외부에서 이 함수를 호출하면 연출이 시작됩니다.
    /// </summary>
    public void FocusCharacter()
    {
        StartCoroutine(AnimateCharacter());
    }

    IEnumerator AnimateCharacter()
    {
        // 1. 시작 상태 저장
        startPos = transform.position;
        startScale = transform.localScale;
        startRot = transform.rotation;

        Quaternion targetRot = Quaternion.Euler(targetRotation);

        // 3. 시간 흐름에 따른 보간(Lerp) 실행
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            // 0~1 사이의 진행률 (AnimationCurve를 적용해 부드럽게)
            float t = motionCurve.Evaluate(elapsedTime / duration);

            // 위치 이동
            transform.position = Vector3.Lerp(startPos, targetPosition.position, t);

            // 크기 변경
            transform.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, t);

            // 회전 (카메라 정면 보기)
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null; // 한 프레임 대기
        }

        // 4. 연출 끝난 후 최종 값 강제 고정 (오차 방지)
        transform.position = targetPosition.position;
        transform.localScale = Vector3.one * targetScale;
        transform.rotation = targetRot;

        DataManager.Instance.OnTransitionComplete();
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