using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 다음에 이어야 할 감정 노드가 화면 밖에 있을 때, 화면 가장자리에 방향 화살표를 띄운다.
///
/// [배치 방법]
///  1. InGame UI 캔버스 아래에 Image 하나를 만들고(화살표 스프라이트) 이 스크립트를 붙인다.
///  2. 화살표 Image의 Anchor / Pivot을 모두 중앙(0.5, 0.5)으로 맞춘다.
///  3. arrowRect / canvasGroup 에 자기 자신을, targetCanvas 에 부모 캔버스를 넣는다.
///  4. 화살표 스프라이트가 '위쪽'을 향하고 있으면 spriteAngleOffset 은 -90 그대로 둔다.
/// </summary>
public class NextEmotionIndicator : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("움직일 화살표의 RectTransform (보통 자기 자신)")]
    [SerializeField] private RectTransform arrowRect;

    [Tooltip("페이드용 CanvasGroup (없으면 자동 추가)")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("화살표가 속한 캔버스")]
    [SerializeField] private Canvas targetCanvas;

    [Header("배치")]
    [Tooltip("화면 테두리에서 안쪽으로 띄울 여백 (픽셀)")]
    [SerializeField] private float edgePadding = 90f;

    [Tooltip("타겟에 적용할 월드 오프셋")]
    [SerializeField] private Vector3 worldOffset = Vector3.zero;

    [Header("표시 조건")]
    [Tooltip("이 비율만큼 화면 안쪽으로 들어와야 '보인다'고 판정 (0.05 = 가장자리 5% 는 밖으로 취급)")]
    [Range(0f, 0.3f)]
    [SerializeField] private float screenMargin = 0.06f;

    [Tooltip("깜빡임 방지용 여유. 사라질 때는 이만큼 더 안쪽으로 들어와야 한다.")]
    [Range(0f, 0.3f)]
    [SerializeField] private float hideHysteresis = 0.04f;

    [Header("연출")]
    [Tooltip("나타나고 사라지는 속도")]
    [SerializeField] private float fadeSpeed = 8f;

    [Tooltip("화살표가 회전해서 방향을 가리킬지")]
    [SerializeField] private bool rotateArrow = true;

    [Tooltip("스프라이트가 위쪽을 향하면 -90, 오른쪽을 향하면 0")]
    [SerializeField] private float spriteAngleOffset = -90f;

    [Tooltip("화살표 위치가 부드럽게 따라오는 속도. 0이면 즉시 이동")]
    [SerializeField] private float followSmoothing = 20f;

    [Header("거리 표시 (선택)")]
    [Tooltip("남은 거리를 표시할 텍스트. 필요 없으면 비워둔다.")]
    [SerializeField] private TMPro.TMP_Text distanceText;
    [SerializeField] private string distanceFormat = "{0:0}m";

    private Camera cam;
    private RectTransform canvasRect;
    private Vector2 currentScreenPos;
    private bool hasScreenPos;
    private bool isShowing;

    private void Awake()
    {
        if (arrowRect == null) arrowRect = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (targetCanvas == null) targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas != null)
            canvasRect = targetCanvas.transform as RectTransform;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void OnEnable()
    {
        canvasGroup.alpha = 0f;
        isShowing = false;
        hasScreenPos = false;
    }

    private void LateUpdate()
    {
        Transform target = GetCurrentTarget();

        if (target == null || !IsPlayable())
        {
            Fade(0f);
            return;
        }

        if (cam == null || !cam.isActiveAndEnabled)
            cam = Camera.main;

        if (cam == null)
        {
            Fade(0f);
            return;
        }

        Vector3 targetPos = target.position + worldOffset;

        // 카메라 로컬 좌표: z > 0 이면 앞, z < 0 이면 뒤
        Vector3 local = cam.transform.InverseTransformPoint(targetPos);

        // 화면 안에 들어와 있는지 판정 (뒤에 있으면 무조건 화면 밖)
        bool onScreen = false;
        if (local.z > 0f)
        {
            Vector3 vp = cam.WorldToViewportPoint(targetPos);
            // 이미 보이는 중이면 조금 더 안쪽까지 들어와야 사라지도록(히스테리시스)
            float m = isShowing ? screenMargin + hideHysteresis : screenMargin;
            onScreen = vp.x > m && vp.x < 1f - m && vp.y > m && vp.y < 1f - m;
        }

        if (onScreen)
        {
            isShowing = false;
            Fade(0f);
            return;
        }

        isShowing = true;

        // 화면상 방향. 앞/뒤 모두 (local.x, local.y) 부호가 '어느 쪽으로 돌아야 하는가'와 일치한다.
        Vector2 dir = new Vector2(local.x, local.y);
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.down; // 정확히 정면 뒤 -> 방향이 모호하므로 아래로
        dir.Normalize();

        Vector2 screenPos = ClampToScreenEdge(dir);

        if (!hasScreenPos || followSmoothing <= 0f)
        {
            currentScreenPos = screenPos;
            hasScreenPos = true;
        }
        else
        {
            currentScreenPos = Vector2.Lerp(currentScreenPos, screenPos,
                1f - Mathf.Exp(-followSmoothing * Time.unscaledDeltaTime));
        }

        PlaceArrow(currentScreenPos);

        if (rotateArrow)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteAngleOffset;
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (distanceText != null)
        {
            float dist = Vector3.Distance(cam.transform.position, targetPos);
            distanceText.text = string.Format(distanceFormat, dist);
        }

        Fade(1f);
    }

    /// <summary>방향 벡터를 화면 테두리(여백 적용) 위의 한 점으로 변환한다.</summary>
    private Vector2 ClampToScreenEdge(Vector2 dir)
    {
        Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
        Vector2 half = center - Vector2.one * edgePadding;

        half.x = Mathf.Max(half.x, 1f);
        half.y = Mathf.Max(half.y, 1f);

        // dir 방향으로 뻗었을 때 사각형 경계에 닿는 배율
        float sx = Mathf.Abs(dir.x) > 0.0001f ? half.x / Mathf.Abs(dir.x) : float.MaxValue;
        float sy = Mathf.Abs(dir.y) > 0.0001f ? half.y / Mathf.Abs(dir.y) : float.MaxValue;

        return center + dir * Mathf.Min(sx, sy);
    }

    private void PlaceArrow(Vector2 screenPos)
    {
        if (arrowRect == null) return;

        if (targetCanvas == null || targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            arrowRect.position = screenPos;
            return;
        }

        if (canvasRect == null) return;

        Camera uiCam = targetCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCam, out Vector2 local))
            arrowRect.anchoredPosition = local;
    }

    private Transform GetCurrentTarget()
    {
        if (DataManager.Instance == null) return null;

        PathManager pm = DataManager.Instance.targetPathManager;
        return pm != null ? pm.CurrentTargetNode : null;
    }

    private bool IsPlayable()
    {
        if (DataManager.Instance == null || DataManager.Instance.gameEnded) return false;
        if (GameSceneUIManager.Instance == null) return false;

        return GameSceneUIManager.Instance.currentState == GameSceneUIState.InGame;
    }

    private void Fade(float targetAlpha)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
    }
}
