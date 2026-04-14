using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ElectrolyzerUI : MonoBehaviour
{
    public static ElectrolyzerUI Instance { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public Button toggleButton;
    public TextMeshProUGUI toggleButtonText;
    public Button closeButton;

    [Header("状態表示")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI inletText;
    public TextMeshProUGUI oxygenOutletText;
    public TextMeshProUGUI hydrogenOutletText;

    [Header("水蓄積ゲージ")]
    public Image waterGaugeFill;
    public TextMeshProUGUI waterAmountText;
    public float maxDisplayAmount = 50f;

    private Electrolyzer currentMachine;
    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        panel.SetActive(false);
        toggleButton.onClick.AddListener(OnTogglePressed);
        closeButton.onClick.AddListener(Close);
    }

    void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            Close();
        UpdateStatus();
    }

    public void Open(Electrolyzer machine)
    {
        currentMachine = machine;
        IsOpen = true;
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        machine.OnSlotsChanged += RefreshAll;
        RefreshAll();
    }

    public void Close()
    {
        if (!IsOpen) return;
        if (currentMachine != null)
            currentMachine.OnSlotsChanged -= RefreshAll;
        IsOpen = false;
        panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentMachine = null;
    }

    void RefreshAll()
    {
        UpdateToggleButton();
        UpdateStatus();
    }

    void UpdateToggleButton()
    {
        if (currentMachine == null) return;
        toggleButtonText.text = currentMachine.IsOn ? "ON" : "OFF";
    }

    void UpdateStatus()
    {
        if (currentMachine == null) return;

        if (statusText != null)
        {
            if (!currentMachine.IsOn)
                statusText.text = "停止中";
            else if (!currentMachine.IsRunning)
                statusText.text = "電力不足";
            else if (!currentMachine.inletConnector.IsConnected)
                statusText.text = "パイプ未接続";
            else if (currentMachine.inletConnector.currentLiquidType != currentMachine.waterLiquid)
                statusText.text = "液体種不一致";
            else
                statusText.text = "稼働中";
        }

        if (inletText != null)
            inletText.text = currentMachine.inletConnector.IsConnected
                ? "入口：接続済"
                : "入口：未接続";

        if (waterGaugeFill != null)
            waterGaugeFill.rectTransform.localScale = new Vector3(
                Mathf.Clamp01(currentMachine.storedWater / maxDisplayAmount), 1f, 1f);
        if (waterAmountText != null)
            waterAmountText.text = $"水：{currentMachine.storedWater:F1}L";

        if (oxygenOutletText != null)
            oxygenOutletText.text = currentMachine.oxygenOutlet.IsConnected
                ? "酸素出口：接続済"
                : "酸素出口：未接続";

        if (hydrogenOutletText != null)
            hydrogenOutletText.text = currentMachine.hydrogenOutlet.IsConnected
                ? "水素出口：接続済"
                : "水素出口：未接続";
    }

    void OnTogglePressed()
    {
        if (currentMachine == null) return;
        currentMachine.SetOn(!currentMachine.IsOn);
        UpdateToggleButton();
    }
}