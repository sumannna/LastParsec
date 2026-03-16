using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// チェストUIの管理。
/// インベントリUIの左側に表示。装備欄は非表示。
/// </summary>
public class ChestUI : MonoBehaviour
{
    public static ChestUI Instance { get; private set; }

    [Header("UI")]
    public GameObject chestPanel;
    public Transform contentParent;
    public GameObject slotPrefab;
    public Button closeButton;

    [Header("参照")]
    public InventoryUI inventoryUI;
    public EquipmentUI equipmentUI;

    private ChestInteraction currentChest;
    public ChestInteraction CurrentChest => currentChest;

    private List<GameObject> slotObjects = new List<GameObject>();
    private bool isOpen = false;
    public bool IsOpen => isOpen;
    private bool closedThisFrame = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (chestPanel != null) chestPanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    void Update()
    {
        closedThisFrame = false;

        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Q) ||
            Input.GetKeyDown(KeyCode.Tab) ||
            Input.GetKeyDown(KeyCode.E))
        {
            closedThisFrame = true;
            Close();
            return;
        }
    }

    public bool ClosedThisFrame => closedThisFrame;

    public void Open(ChestInteraction chest)
    {
        currentChest = chest;
        isOpen = true;

        if (chestPanel != null) chestPanel.SetActive(true);

        // インベントリを開く（装備欄は非表示）
        if (inventoryUI != null && !inventoryUI.IsOpen)
            inventoryUI.OpenInventoryExternal();

        if (equipmentUI != null)
            equipmentUI.equipmentPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshAll();
    }

    public void Close()
    {
        if (!isOpen) return;
        Debug.Log("[ChestUI] Close開始");
        isOpen = false;

        var chest = currentChest;
        currentChest = null;
        chest?.CloseChest();

        if (chestPanel != null) chestPanel.SetActive(false);

        if (inventoryUI != null && inventoryUI.IsOpen)
        {
            Debug.Log("[ChestUI] inventoryUI.CloseInventory呼び出し");
            inventoryUI.CloseInventory();
        }
        else
        {
            Debug.Log($"[ChestUI] inventoryUI.CloseInventoryスキップ inventoryUI={inventoryUI != null} IsOpen={inventoryUI?.IsOpen}");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ClearSlots();
        Debug.Log("[ChestUI] Close完了");
    }

    public void RefreshAll()
    {
        ClearSlots();
        if (currentChest == null) return;

        ChestInventory chestInv = currentChest.chestInventory;
        if (chestInv == null) return;

        Inventory.Slot[] slots = chestInv.GetSlots();

        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, contentParent);
            slotObjects.Add(slotObj);

            Inventory.Slot slot = slots[i];

            if (slot != null)
            {
                Image icon = FindChild<Image>(slotObj, "ItemIcon");
                if (icon != null)
                {
                    icon.sprite = slot.item?.icon;
                    icon.color = slot.item?.icon != null ? Color.white : Color.clear;
                }

                TextMeshProUGUI amount = FindChild<TextMeshProUGUI>(slotObj, "AmountText");
                if (amount != null)
                    amount.text = slot.amount > 1 ? slot.amount.ToString() : "";
            }
            else
            {
                Image icon = FindChild<Image>(slotObj, "ItemIcon");
                if (icon != null) icon.color = Color.clear;

                TextMeshProUGUI amount = FindChild<TextMeshProUGUI>(slotObj, "AmountText");
                if (amount != null) amount.text = "";
            }

            // D&Dハンドラ
            ChestDropHandler dropHandler = slotObj.AddComponent<ChestDropHandler>();
            dropHandler.chestUI = this;
            dropHandler.chestInventory = currentChest.chestInventory;
            dropHandler.playerInventory = currentChest.playerInventory;
            dropHandler.inventoryUI = inventoryUI;
            dropHandler.targetSlot = slot;
            dropHandler.targetIndex = i;

            // ドラッグハンドラ
            ChestItemDragHandler dragHandler = slotObj.AddComponent<ChestItemDragHandler>();
            dragHandler.chestUI = this;
            dragHandler.chestInventory = currentChest.chestInventory;
            dragHandler.playerInventory = currentChest.playerInventory;
            dragHandler.inventoryUI = inventoryUI;
            dragHandler.chestSlot = slot;

            // クリックハンドラ
            ChestSlotClickHandler clickHandler = slotObj.AddComponent<ChestSlotClickHandler>();
            clickHandler.chestUI = this;
            clickHandler.chestInventory = currentChest.chestInventory;
            clickHandler.playerInventory = currentChest.playerInventory;
            clickHandler.inventoryUI = inventoryUI;
            clickHandler.chestSlot = slot;
            clickHandler.slotIndex = i;
        }
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