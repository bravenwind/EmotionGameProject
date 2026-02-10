using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 필수

public class DebugMenuController : MonoBehaviour
{
    [Header("타겟 스크립트")]
    public ZeroGravityMovement playerScript;
    public Debug_ShapeActiveManager debug;

    [Header("UI 패널")]
    public GameObject uiPanel;

    [Header("입력 필드 (Input Fields)")]
    // 슬라이더 대신 InputField를 연결합니다.
    public InputField walkSpeedInput;
    public InputField sprintSpeedInput;
    public InputField fovBoostInput;
    public InputField walkMaxSpeedInput;
    public InputField sprintMaxSpeedInput;
    public InputField fovBoostSpeedInput;
    public Text pressESC;
    public Toggle infiniteStaminaToggle;

    private bool isMenuOpen = false;

    void Start()
    {
        playerScript = DataManager.Instance.playerTransform.GetComponent<ZeroGravityMovement>();
        // 2. 현재 플레이어 값을 가져와서 InputField에 텍스트로 넣어줌
        UpdateUIFromPlayerValues();
        pressESC.text = "ESC 눌러서 마우스 보이기";

        // 3. 입력이 끝났을 때(엔터 or 포커스 아웃) 실행될 함수 연결
        // onEndEdit: 타이핑 중에는 실행 안 되고, 다 쓰고 엔터 칠 때 실행됨
        // 이동 관련
        walkSpeedInput.onValueChanged.AddListener(OnWalkAccelChanged);
        walkSpeedInput.onValueChanged.AddListener(OnWalkMaxSpeedChanged);
        sprintSpeedInput.onValueChanged.AddListener(OnSprintAccelChanged);
        sprintSpeedInput.onValueChanged.AddListener(OnSprintMaxSpeedChanged);

        // 카메라 관련
        fovBoostInput.onValueChanged.AddListener(OnFovBoostAmountChanged);
        fovBoostSpeedInput.onValueChanged.AddListener(OnFovChangeSpeedChanged);

        // 토글
        infiniteStaminaToggle.onValueChanged.AddListener(OnInfiniteStaminaToggle);
    }

    void Update()
    {
        // ESC 키로 메뉴 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (isMenuOpen)
        {
            // 메뉴 열림: 마우스 보이기 + 값 최신화
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            debug.enabled = false;
            pressESC.text = "ESC 눌러서 마우스 감추기";
        }
        else
        {
            // 메뉴 닫힘: 마우스 숨기기
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            debug.enabled = true;
            pressESC.text = "ESC 눌러서 마우스 보이기";
        }
    }

    void UpdateUIFromPlayerValues()
    {
        if (playerScript != null)
        {
            // 이동
            walkSpeedInput.text = playerScript.acceleration.ToString();
            walkMaxSpeedInput.text = playerScript.normalMaxSpeed.ToString();
            sprintSpeedInput.text = playerScript.sprintAcceleration.ToString();
            sprintMaxSpeedInput.text = playerScript.sprintMaxSpeed.ToString();

            // 카메라
            fovBoostInput.text = playerScript.sprintFovBoost.ToString();
            fovBoostSpeedInput.text = playerScript.fovChangeSpeed.ToString();

            // 토글
            infiniteStaminaToggle.isOn = playerScript.infiniteStamina;
        }
    }

    // 1. 걷기 가속도
    void OnWalkAccelChanged(string input)
    {
        if (float.TryParse(input, out float result))
        {
            playerScript.acceleration = result;
            Debug.Log($"Walk Accel 변경: {result}");
        }
        else walkSpeedInput.text = playerScript.acceleration.ToString();
    }

    // 2. 걷기 최대 속도
    void OnWalkMaxSpeedChanged(string input)
    {
        if (float.TryParse(input, out float result))
        {
            playerScript.normalMaxSpeed = result;
            Debug.Log($"Walk MaxSpeed 변경: {result}");
        }
        else walkMaxSpeedInput.text = playerScript.normalMaxSpeed.ToString();
    }

    // 3. 달리기 가속도
    void OnSprintAccelChanged(string input)
    {
        if (float.TryParse(input, out float result))
        {
            playerScript.sprintAcceleration = result;
            Debug.Log($"Sprint Accel 변경: {result}");
        }
        else sprintSpeedInput.text = playerScript.sprintAcceleration.ToString();
    }

    // 4. 달리기 최대 속도
    void OnSprintMaxSpeedChanged(string input)
    {
        if (float.TryParse(input, out float result))
        {
            playerScript.sprintMaxSpeed = result;
            Debug.Log($"Sprint MaxSpeed 변경: {result}");
        }
        else sprintMaxSpeedInput.text = playerScript.sprintMaxSpeed.ToString();
    }

    // 5. FOV 증가량
    void OnFovBoostAmountChanged(string input)
    {
        if (float.TryParse(input, out float result))
        {
            playerScript.sprintFovBoost = result;
            Debug.Log($"FOV Boost Amount 변경: {result}");
        }
        else fovBoostInput.text = playerScript.sprintFovBoost.ToString();
    }

    // 6. FOV 변화 속도
    void OnFovChangeSpeedChanged(string input)
    {
        if (float.TryParse(input, out float result))
        {
            playerScript.fovChangeSpeed = result;
            Debug.Log($"FOV Change Speed 변경: {result}");
        }
        else fovBoostSpeedInput.text = playerScript.fovChangeSpeed.ToString();
    }

    // 7. 무한 스태미나 토글
    void OnInfiniteStaminaToggle(bool isOn)
    {
        if (playerScript != null)
        {
            playerScript.infiniteStamina = isOn;
            Debug.Log($"무한 스태미나: {isOn}");
        }
    }
}