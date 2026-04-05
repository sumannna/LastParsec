using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IceMelterUI : MonoBehaviour
{
    public static IceMelterUI Instance { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public Transform slotsParent;
    public GameObject slotPrefab;
    public Button toggleButton;
    public TextMeshProUGUI toggleButtonText;
    public Button closeButton;

    [Header("参照")]
    public InventoryUI inventoryUI;

    [Header("状態表示")]
    public TextMeshProUGUI powerStatusText;
    public TextMeshProUGUI powerConsumptionText;

    private IceMelter currentMachine;
    private List<GameObject> slotObjects = new List<GameObject>();
    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[IceMelterUI] Awake / Instance set");
    }

    void Start()
    {
        Debug.Log($"[IceMelterUI] Start / panel={panel != null} / toggleButton={toggleButton != null} / closeButton={closeButton != null}");
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

    public void Open(IceMelter machine)
    {
        Debug.Log($"[IceMelterUI] Open called / panel={panel != null} / machine={machine != null}");
        currentMachine = machine;
        IsOpen = true;
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (inventoryUI != null && !inventoryUI.IsOpen)
            inventoryUI.OpenInventoryExternalNoEquipment();
        machine.OnSlotsChanged += RefreshSlots;
        machine.OnStatusChanged += UpdateStatus;
        UpdateStatus(machine.IsPowered ? "待機中" : "未接続");
        RefreshAll();
        Debug.Log("[IceMelterUI] Open complete");
    }

    public void Close()
    {
        if (!IsOpen) return;
        if (currentMachine != null)
        {
            currentMachine.OnSlotsChanged -= RefreshSlots;
            currentMachine.OnStatusChanged -= UpdateStatus;
        }
        IsOpen = false;
        panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.CloseInventory();
        inventoryUI?.RemoveIceMelterHandlers();
        ClearSlots();
        currentMachine = null;
    }

    void RefreshAll()
    {
        RefreshSlots();
        UpdateToggleButton();
        UpdateStatus();
    }

    public void RefreshSlots()
    {
        ClearSlots();
        if (currentMachine == null) return;
        for (int i = 0; i < currentMachine.slotCount; i++)
        {
            GameObject obj = Instantiate(slotPrefab, slotsParent);
            slotObjects.Add(obj);
            Inventory.Slot slot = currentMachine.slots[i];
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

            int capturedIndex = i;
            Inventory inv = FindObjectOfType<Inventory>();

            IceMelterDropHandler drop = obj.AddComponent<IceMelterDropHandler>();
            drop.Init(currentMachine, capturedIndex, inv, this);

            IceMelterItemDragHandler drag = obj.AddComponent<IceMelterItemDragHandler>();
            drag.Init(currentMachine, capturedIndex, inv, this);

            IceMelterSlotClickHandler click = obj.AddComponent<IceMelterSlotClickHandler>();
            click.Init(currentMachine, capturedIndex, inv, this);
        }
        UpdateToggleButton();
        if (IsOpen) inventoryUI?.AddIceMelterHandlers(currentMachine, this);
    }

    void UpdateToggleButton()
    {
        if (currentMachine == null) return;
        toggleButtonText.text = currentMachine.IsOn ? "ON" : "OFF";
    }

    void UpdateStatus()
    {
        if (currentMachine == null) return;
        if (powerStatusText != null)
            powerStatusText.text = currentMachine.IsPowered ? "電源：接続済" : "電源：未接続";
        if (powerConsumptionText != null)
            powerConsumptionText.text = $"消費電力：{currentMachine.PowerConsumption}kWh";
    }

    void OnTogglePressed()
    {
        if (currentMachine == null) return;
        if (!currentMachine.IsOn)
        {
            if (currentMachine.FindIceSlot() == null)
            {
                UpdateStatus("氷がありません");
                return;
            }
            if (!currentMachine.outletConnector.IsConnected)
            {
                UpdateStatus("パイプ未接続");
                return;
            }
        }
        currentMachine.SetOn(!currentMachine.IsOn);
        UpdateToggleButton();
    }

    void ClearSlots()
    {
        foreach (var obj in slotObjects)
            if (obj != null) Destroy(obj);
        slotObjects.Clear();
    }

    public void RequestInventoryRefresh()
    {
        StartCoroutine(DoInventoryRefresh());
    }

    private IEnumerator DoInventoryRefresh()
    {
        yield return null;
        inventoryUI?.RefreshAll();
        if (IsOpen) inventoryUI?.AddIceMelterHandlers(currentMachine, this);
    }

    T FindChild<T>(GameObject root, string name) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<T>();
        return null;
    }

    void UpdateStatus(string msg)
    {
        if (powerStatusText != null) powerStatusText.text = msg;
    }
}