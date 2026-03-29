using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro �ʼ�

public class DebugMenuController : MonoBehaviour
{
    [Header("Ÿ�� ��ũ��Ʈ")]
    public ZeroGravityMovement playerScript;
    public Debug_ShapeActiveManager debug;

    [Header("UI �г�")]
    public GameObject uiPanel;

    [Header("�Է� �ʵ� (Input Fields)")]
    // �����̴� ��� InputField�� �����մϴ�.
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
        // 2. ���� �÷��̾� ���� �����ͼ� InputField�� �ؽ�Ʈ�� �־���
        UpdateUIFromPlayerValues();
        pressESC.text = "ESC ������ ���콺 ���̱�";

        // 3. �Է��� ������ ��(���� or ��Ŀ�� �ƿ�) ����� �Լ� ����
        // onEndEdit: Ÿ���� �߿��� ���� �� �ǰ�, �� ���� ���� ĥ �� �����
        // �̵� ����
        walkSpeedInput.onValueChanged.AddListener(OnWalkAccelChanged);
        walkMaxSpeedInput.onValueChanged.AddListener(OnWalkMaxSpeedChanged);
        sprintSpeedInput.onValueChanged.AddListener(OnSprintAccelChanged);
        sprintMaxSpeedInput.onValueChanged.AddListener(OnSprintMaxSpeedChanged);

        // ī�޶� ����
        fovBoostInput.onValueChanged.AddListener(OnFovBoostAmountChanged);
        fovBoostSpeedInput.onValueChanged.AddListener(OnFovChangeSpeedChanged);

        // ���
        infiniteStaminaToggle.onValueChanged.AddListener(OnInfiniteStaminaToggle);
    }

    void Update()
    {
        // ESC Ű�� �޴� ���
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
            // �޴� ����: ���콺 ���̱� + �� �ֽ�ȭ
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            debug.enabled = false;
            pressESC.text = "ESC ������ ���콺 ���߱�";
        }
        else
        {
            // �޴� ����: ���콺 �����
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            debug.enabled = true;
            pressESC.text = "ESC ������ ���콺 ���̱�";
        }
    }

    void UpdateUIFromPlayerValues()
    {
        if (playerScript != null)
        {
            // �̵�
            walkSpeedInput.text = playerScript.acceleration.ToString();
            walkMaxSpeedInput.text = playerScript.normalMaxSpeed.ToString();
            sprintSpeedInput.text = playerScript.sprintAcceleration.ToString();
            sprintMaxSpeedInput.text = playerScript.sprintMaxSpeed.ToString();

            // ī�޶�
            fovBoostInput.text = playerScript.sprintFovBoost.ToString();
            fovBoostSpeedInput.text = playerScript.fovChangeSpeed.ToString();

            // ���
            infiniteStaminaToggle.isOn = playerScript.infiniteStamina;
        }
    }

    // 1. �ȱ� ���ӵ�
    void OnWalkAccelChanged(string input)
    {
        if (float.TryParse(input, out float result))
        {
            playerScript.acceleration = result;
            Debug.Log($"Walk Accel ����: {result}");
        }
        else walkSpeedInput.text = playerScript.acceleration.ToString();
    }

    // 2. �ȱ� �ִ� �ӵ�
    void OnWalkMaxSpeedChanged(string input)
    {
        if (float.TryParse(input, out float result))
        {
            playerScript.normalMaxSpeed = result;
            Debug.Log($"Walk MaxSpeed ����: {result}");
        }
        else walkMaxSpeedInput.text = playerScript.normalMaxSpeed.ToString();
    }

    // 3. �޸��� ���ӵ�
    void OnSprintAccelChanged(string input)
    {
        if (float.TryParse(input, out float result))
        {
            playerScript.sprintAcceleration = result;
            Debug.Log($"Sprint Accel ����: {result}");
        }
        else sprintSpeedInput.text = playerScript.sprintAcceleration.ToString();
    }

    // 4. �޸��� �ִ� �ӵ�
    void OnSprintMaxSpeedChanged(string input)
    {
        if (float.TryParse(input, out float result))
        {
            playerScript.sprintMaxSpeed = result;
            Debug.Log($"Sprint MaxSpeed ����: {result}");
        }
        else sprintMaxSpeedInput.text = playerScript.sprintMaxSpeed.ToString();
    }

    // 5. FOV ������
    void OnFovBoostAmountChanged(string input)
    {
        if (float.TryParse(input, out float result))
        {
            playerScript.sprintFovBoost = result;
            Debug.Log($"FOV Boost Amount ����: {result}");
        }
        else fovBoostInput.text = playerScript.sprintFovBoost.ToString();
    }

    // 6. FOV ��ȭ �ӵ�
    void OnFovChangeSpeedChanged(string input)
    {
        if (float.TryParse(input, out float result))
        {
            playerScript.fovChangeSpeed = result;
            Debug.Log($"FOV Change Speed ����: {result}");
        }
        else fovBoostSpeedInput.text = playerScript.fovChangeSpeed.ToString();
    }

    // 7. ���� ���¹̳� ���
    void OnInfiniteStaminaToggle(bool isOn)
    {
        if (playerScript != null)
        {
            playerScript.infiniteStamina = isOn;
            Debug.Log($"���� ���¹̳�: {isOn}");
        }
    }
}