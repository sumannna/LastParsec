using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HandCrankerUI : MonoBehaviour
{
    public static HandCrankerUI Instance { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public Image chargeGaugeFill;         // 充電ゲージ
    public TextMeshProUGUI chargeText;    // 現在充電量テキスト
    public TextMeshProUGUI instructionText; // 操作説明
    public Button closeButton;

    private HandCranker currentCranker;
    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        panel.SetActive(false);
        closeButton.onClick.AddListener(Close);
    }

    void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Close();

        // ゲージ更新
        RefreshGauge();

        // 操作説明表示
        if (instructionText != null)
        {
            instructionText.text = currentCranker != null && currentCranker.IsCranking
                ? "充電中..."
                : "左クリック長押しで充電";
        }
    }

    public void Open(HandCranker cranker)
    {
        currentCranker = cranker;
        IsOpen = true;
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cranker.OnChargeChanged += RefreshGauge;
        RefreshGauge();
    }

    public void Close()
    {
        if (!IsOpen) return;
        if (currentCranker != null)
            currentCranker.OnChargeChanged -= RefreshGauge;
        IsOpen = false;
        panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentCranker = null;
    }

    void RefreshGauge()
    {
        if (currentCranker == null) return;

        if (chargeGaugeFill != null)
            chargeGaugeFill.rectTransform.localScale =
                new Vector3(currentCranker.ChargeRatio, 1f, 1f);

        if (chargeText != null)
            chargeText.text =
                $"{currentCranker.CurrentCharge:F1} / {currentCranker.MaxCapacity:F1} kW";
    }
}