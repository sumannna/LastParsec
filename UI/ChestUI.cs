using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// チェストUIの管理。
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
    private bool openedThisFrame = false;

    // 現在のチェストスロットオーナー（InventoryUI のMachineHandlers用）
    private ISlotOwner currentChestOwner;

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
            (!openedThisFrame && Input.GetKeyDown(KeyCode.E)))
        {
            closedThisFrame = true;
            Close();
            return;
        }

        openedThisFrame = false;
    }

    public bool ClosedThisFrame => closedThisFrame;

    public void Open(ChestInteraction chest)
    {
        currentChest = chest;
        isOpen = true;
        openedThisFrame = true;

        if (chestPanel != null) chestPanel.SetActive(true);

        // インベントリを開く（装備欄は非表示）
        if (inventoryUI != null && !inventoryUI.IsOpen)
            inventoryUI.OpenInventoryExternalNoEquipment();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshAll();
    }

    public void Close()
    {
        if (!isOpen) return;
        Debug.Log("[ChestUI] Close開始");
        isOpen = false;

        // インベントリのDCハンドラを削除
        inventoryUI?.RemoveMachineHandlers();
        currentChestOwner = null;

        var chest = currentChest;
        currentChest = null;
        chest?.CloseChest();

        if (chestPanel != null) chestPanel.SetActive(false);

        if (inventoryUI != null && inventoryUI.IsOpen)
        {
            Debug.Log("[ChestUI] inventoryUI.CloseInventory呼び出し");
            inventoryUI.CloseInventory();
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

        // ArraySlotOwner でチェストスロットを抽象化。
        // onChanged は同期呼び出しすると dragIcon が残るバグになるため 1 フレーム遅延する。
        currentChestOwner = new ArraySlotOwner(
            chestInv.GetSlots(),
            false,
            null,
            () => StartCoroutine(DelayedRefreshAll())
        );

        Inventory playerInventory = inventoryUI?.inventory ?? currentChest.playerInventory;
        Inventory.Slot[] slots = chestInv.GetSlots();

        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, contentParent);
            slotObjects.Add(slotObj);

            Inventory.Slot slot = slots[i];

            // ビジュアル設定
            Image icon = FindChild<Image>(slotObj, "ItemIcon");
            if (icon != null)
            {
                icon.sprite = slot?.item?.icon;
                icon.color = (slot?.item?.icon != null) ? Color.white : Color.clear;
            }
            TextMeshProUGUI amount = FindChild<TextMeshProUGUI>(slotObj, "AmountText");
            if (amount != null)
                amount.text = slot != null ? slot.amount.ToString() : "";

            int capturedIndex = i;

            // ── ドロップハンドラ（インベントリ→チェスト、チェスト→チェスト）
            MachineDropHandler drop = slotObj.AddComponent<MachineDropHandler>();
            drop.Init(currentChestOwner, capturedIndex, playerInventory, inventoryUI);

            // ── ドラッグハンドラ（チェスト→どこへでも）
            ItemDragHandler drag = slotObj.AddComponent<ItemDragHandler>();
            drag.machineOwner = currentChestOwner;
            drag.machineSlotIndex = capturedIndex;
            drag.inventory = playerInventory;
            drag.inventoryUI = inventoryUI;

            // ── クリックハンドラ（DC：チェスト→インベントリ）
            MachineSlotClickHandler click = slotObj.AddComponent<MachineSlotClickHandler>();
            click.Init(currentChestOwner, capturedIndex, playerInventory, inventoryUI);
        }

        // インベントリスロットにDCハンドラを追加（DC：インベントリ→チェスト）
        inventoryUI?.AddMachineHandlers(currentChestOwner);
    }

    void ClearSlots()
    {
        foreach (var obj in slotObjects)
            if (obj != null) Destroy(obj);
        slotObjects.Clear();
    }

    System.Collections.IEnumerator DelayedRefreshAll()
    {
        yield return null;
        RefreshAll();
    }

    T FindChild<T>(GameObject root, string name) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<T>();
        return null;
    }
}