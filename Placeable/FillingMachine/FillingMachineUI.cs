using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FillingMachineUI : MonoBehaviour
{
    public static FillingMachineUI Instance { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public Transform inputSlotsParent;
    public Transform outputSlotsParent;
    public GameObject slotPrefab;
    public Button toggleButton;
    public TextMeshProUGUI toggleButtonText;
    public Button closeButton;

    [Header("参照")]
    public InventoryUI inventoryUI;
    public TextMeshProUGUI pipeInfoText;

    [Header("液体ゲージ")]
    public Image waterGaugeFill;
    public Image oxygenGaugeFill;
    public Image hydrogenGaugeFill;
    public TextMeshProUGUI waterAmountText;
    public TextMeshProUGUI oxygenAmountText;
    public TextMeshProUGUI hydrogenAmountText;
    public float maxDisplayAmount = 50f;

    [Header("液体定義")]
    public LiquidData waterLiquid;
    public LiquidData oxygenLiquid;
    public LiquidData hydrogenLiquid;

    [Header("液体ゲージ親オブジェクト")]
    public GameObject waterGaugeRoot;
    public GameObject oxygenGaugeRoot;
    public GameObject hydrogenGaugeRoot;

    private FillingMachine currentMachine;
    public FillingMachine CurrentMachine => currentMachine;
    private List<GameObject> inputSlotObjects = new List<GameObject>();
    private List<GameObject> outputSlotObjects = new List<GameObject>();
    public bool IsOpen { get; private set; }
    private bool openedThisFrame = false;
    private bool closedThisFrame = false;
    public bool ClosedThisFrame => closedThisFrame;

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
        closedThisFrame = false;
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
        if (!openedThisFrame && Input.GetKeyDown(KeyCode.E)) Close();
        openedThisFrame = false;
        UpdatePipeInfo();
    }

    public void Open(FillingMachine machine)
    {
        currentMachine = machine;
        IsOpen = true;
        openedThisFrame = true;
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (inventoryUI != null && !inventoryUI.IsOpen)
            inventoryUI.OpenInventoryExternalNoEquipment();
        machine.OnSlotsChanged += RefreshSlots;
        RefreshAll();
    }

    public void Close()
    {
        if (!IsOpen) return;
        closedThisFrame = true;
        if (currentMachine != null)
            currentMachine.OnSlotsChanged -= RefreshSlots;
        IsOpen = false;
        panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.CloseInventory();
        ClearSlots();
        currentMachine = null;
    }

    void RefreshAll()
    {
        RefreshSlots();
        UpdateToggleButton();
    }

    public void RefreshSlots()
    {
        ClearSlots();
        if (currentMachine == null) return;

        Inventory playerInventory = FindObjectOfType<Inventory>();

        for (int i = 0; i < currentMachine.slotCount; i++)
        {
            GameObject obj = Instantiate(slotPrefab, inputSlotsParent);
            inputSlotObjects.Add(obj);
            Inventory.Slot slot = currentMachine.inputSlots[i];
            SetSlotVisual(obj, slot);

            int capturedIndex = i;
            FillingMachineDropHandler drop = obj.AddComponent<FillingMachineDropHandler>();
            drop.machine = currentMachine;
            drop.slotIndex = capturedIndex;
            drop.isInputSlot = true;
            drop.playerInventory = playerInventory;
            drop.ui = this;

            FillingMachineItemDragHandler drag = obj.AddComponent<FillingMachineItemDragHandler>();
            drag.machine = currentMachine;
            drag.slotIndex = capturedIndex;
            drag.isInputSlot = true;
            drag.playerInventory = playerInventory;
            drag.ui = this;

            FillingMachineSlotClickHandler click = obj.AddComponent<FillingMachineSlotClickHandler>();
            click.machine = currentMachine;
            click.slotIndex = capturedIndex;
            click.isInputSlot = true;
            click.playerInventory = playerInventory;
            click.ui = this;
        }

        for (int i = 0; i < currentMachine.slotCount; i++)
        {
            GameObject obj = Instantiate(slotPrefab, outputSlotsParent);
            outputSlotObjects.Add(obj);
            Inventory.Slot slot = currentMachine.outputSlots[i];
            SetSlotVisual(obj, slot);

            int capturedIndex = i;
            FillingMachineItemDragHandler drag = obj.AddComponent<FillingMachineItemDragHandler>();
            drag.machine = currentMachine;
            drag.slotIndex = capturedIndex;
            drag.isInputSlot = false;
            drag.playerInventory = playerInventory;
            drag.ui = this;

            FillingMachineSlotClickHandler click = obj.AddComponent<FillingMachineSlotClickHandler>();
            click.machine = currentMachine;
            click.slotIndex = capturedIndex;
            click.isInputSlot = false;
            click.playerInventory = playerInventory;
            click.ui = this;
        }
    }

    void SetSlotVisual(GameObject obj, Inventory.Slot slot)
    {
        Image icon = FindChild<Image>(obj, "ItemIcon");
        TextMeshProUGUI amount = FindChild<TextMeshProUGUI>(obj, "AmountText");
        if (slot != null)
        {
            if (icon != null) { icon.sprite = slot.item.icon; icon.color = Color.white; }
            if (amount != null) amount.text = slot.amount > 1 ? slot.amount.ToString() : "";
        }
        else
        {
            if (icon != null) icon.color = Color.clear;
            if (amount != null) amount.text = "";
        }
    }

    void UpdateToggleButton()
    {
        if (currentMachine == null) return;
        toggleButtonText.text = currentMachine.IsOn ? "ON" : "OFF";
        UpdatePipeInfo();
    }

    void UpdatePipeInfo()
    {
        if (currentMachine == null) return;

        if (pipeInfoText != null)
            pipeInfoText.text = currentMachine.inletConnector.IsConnected
                ? "パイプ：接続済"
                : "パイプ：未接続（蓄積量で動作可）";

        float w = currentMachine.storedWater;
        float o = currentMachine.storedOxygen;
        float h = currentMachine.storedHydrogen;

        bool hasAny = w > 0f || o > 0f || h > 0f;
        bool showWater = hasAny && w >= o && w >= h;
        bool showOxygen = hasAny && !showWater && o >= h;
        bool showHydrogen = hasAny && !showWater && !showOxygen;

        if (waterGaugeRoot != null) waterGaugeRoot.SetActive(showWater);
        if (oxygenGaugeRoot != null) oxygenGaugeRoot.SetActive(showOxygen);
        if (hydrogenGaugeRoot != null) hydrogenGaugeRoot.SetActive(showHydrogen);

        if (showWater) UpdateGauge(waterGaugeFill, waterAmountText, w);
        if (showOxygen) UpdateGauge(oxygenGaugeFill, oxygenAmountText, o);
        if (showHydrogen) UpdateGauge(hydrogenGaugeFill, hydrogenAmountText, h);
    }

    void UpdateGauge(Image fill, TextMeshProUGUI text, float value)
    {
        if (fill != null)
            fill.rectTransform.localScale = new Vector3(
                Mathf.Clamp01(value / maxDisplayAmount), 1f, 1f);
        if (text != null)
            text.text = $"{value:F1}L";
    }

    void OnTogglePressed()
    {
        if (currentMachine == null) return;
        currentMachine.SetOn(!currentMachine.IsOn);
        UpdateToggleButton();
    }

    void ClearSlots()
    {
        foreach (var obj in inputSlotObjects) if (obj != null) Destroy(obj);
        foreach (var obj in outputSlotObjects) if (obj != null) Destroy(obj);
        inputSlotObjects.Clear();
        outputSlotObjects.Clear();
    }

    T FindChild<T>(GameObject root, string name) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<T>();
        return null;
    }
}