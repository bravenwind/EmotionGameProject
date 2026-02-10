using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 이벤트 시스템 필수

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image targetImage;
    private Vector3 originalScale;

    [Header("설정")]
    public Color hoverColor = Color.gray;
    public float scaleMultiplier = 1.1f;
    public float duration = 0.1f; // 효과가 적용되는 속도
    public EmotionState hoveredEmotion;
    public HoveredLevelManager hoveredLevelManager;

    private Color originalColor;

    void Awake()
    {
        targetImage = GetComponent<Image>();
        originalScale = transform.localScale;
        originalColor = targetImage.color;
        hoveredLevelManager.OnExitHoverLevel();
    }

    // 마우스가 들어왔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines(); // 기존 동작 멈춤
        targetImage.color = hoverColor;
        transform.localScale = originalScale * scaleMultiplier;
        hoveredLevelManager.OnEnterHoverLevel(hoveredEmotion);

        // 더 부드러운 연출을 원한다면 여기서 DoTween 같은 라이브러리를 쓰면 좋습니다.
    }

    // 마우스가 나갔을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        targetImage.color = originalColor;
        transform.localScale = originalScale;
        hoveredLevelManager.OnExitHoverLevel();
    }
}