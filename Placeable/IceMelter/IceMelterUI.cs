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

    [Header("éQè∆")]
    public InventoryUI inventoryUI;

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

    private bool openedThisFrame = false;

    void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
        if (!openedThisFrame && Input.GetKeyDown(KeyCode.E))
            Close();
        openedThisFrame = false;
    }

    public void Open(IceMelter machine)
    {
        Debug.Log($"[IceMelterUI] Open called / panel={panel != null} / machine={machine != null}");
        currentMachine = machine;
        IsOpen = true;
        openedThisFrame = true;
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (inventoryUI != null && !inventoryUI.IsOpen)
            inventoryUI.OpenInventoryExternal();
        machine.OnSlotsChanged += RefreshSlots;
        RefreshAll();
        Debug.Log("[IceMelterUI] Open complete");
    }

    public void Close()
    {
        if (!IsOpen) return;
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
            DropHandler drop = obj.AddComponent<DropHandler>();
            drop.inventory = FindObjectOfType<Inventory>();
            drop.inventoryUI = inventoryUI;
            drop.targetSlot = slot;
            drop.targetIndex = capturedIndex;
        }
        UpdateToggleButton();
    }

    void UpdateToggleButton()
    {
        if (currentMachine == null) return;
        toggleButtonText.text = currentMachine.IsOn ? "ON" : "OFF";
    }

    void OnTogglePressed()
    {
        if (currentMachine == null) return;
        currentMachine.SetOn(!currentMachine.IsOn);
        UpdateToggleButton();
    }

    void ClearSlots()
    {
        foreach (var obj in slotObjects)
            if (obj != null) Destroy(obj);
        slotObjects.Clear();
    }

    T FindChild<T>(GameObject root, string name) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<T>();
        return null;
    }
}