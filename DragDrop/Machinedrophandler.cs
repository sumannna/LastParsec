using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 機械・チェストスロットへのドロップを受け取る統合ハンドラ。
/// インベントリ→機械、ホットバー→機械、機械→機械（同Owner内）をサポート。
/// IceMelterDropHandler / ChestDropHandler / FillingMachineDropHandler を統合。
/// </summary>
public class MachineDropHandler : MonoBehaviour, IDropHandler
{
    private ISlotOwner owner;
    private int slotIndex;
    private Inventory playerInventory;
    private InventoryUI inventoryUI;

    public void Init(ISlotOwner owner, int slotIndex, Inventory playerInventory, InventoryUI inventoryUI)
    {
        this.owner = owner;
        this.slotIndex = slotIndex;
        this.playerInventory = playerInventory;
        this.inventoryUI = inventoryUI;
    }

    // IDropHandler（左クリックドラッグの着地点）
    public void OnDrop(PointerEventData eventData)
    {
        ItemDragHandler drag = eventData.pointerDrag?.GetComponent<ItemDragHandler>();
        if (drag != null) ReceiveDrop(drag);
    }

    /// <summary>右クリックドラッグ終了時など外部から直接呼ぶエントリポイント。</summary>
    public void ReceiveDrop(ItemDragHandler drag)
    {
        if (drag == null || owner == null || owner.IsReadOnly) return;

        if (drag.IsMachineDrag()) { HandleMachineToMachine(drag); return; }
        if (drag.inventorySlot != null) { HandleInventoryToMachine(drag); return; }
        if (drag.hotbarSlot != null) { HandleHotbarToMachine(drag); return; }
    }

    // -----------------------------------------------
    // インベントリ → 機械
    // -----------------------------------------------

    void HandleInventoryToMachine(ItemDragHandler drag)
    {
        Inventory.Slot src = drag.inventorySlot;
        if (src == null || src.item == null) return;
        if (!owner.CanAcceptItem(src.item)) return;

        Inventory.Slot existing = owner.GetSlot(slotIndex);

        // インスタンス付きアイテムは常に全量移動
        bool isInstance = src.tankInstance != null || src.waterTankInstance != null
                       || src.thrusterInstance != null || src.spacesuitInstance != null;
        int amount = isInstance
            ? src.amount
            : Mathf.Min(drag.dragAmount > 0 ? drag.dragAmount : src.amount, src.amount);

        if (existing == null)
        {
            // 空きスロットへ移動（インスタンスを保持）
            var newSlot = CreateSlotWithInstances(src.item, amount, src);
            owner.SetSlot(slotIndex, newSlot);
            playerInventory.ReduceSlot(src, amount);
            drag.inventorySlot = null;
        }
        else if (!isInstance && existing.item == src.item)
        {
            // 同種マージ（インスタンスなしのみ）
            int space = existing.item.maxStack - existing.amount;
            int toAdd = Mathf.Min(amount, space);
            if (toAdd <= 0) return;
            existing.amount += toAdd;
            playerInventory.ReduceSlot(src, toAdd);
            drag.inventorySlot = null;
        }
        else return; // 埋まり・異種はスキップ

        owner.NotifyChanged();
        StartCoroutine(RefreshNextFrame());
    }

    // -----------------------------------------------
    // ホットバー → 機械
    // -----------------------------------------------

    void HandleHotbarToMachine(ItemDragHandler drag)
    {
        Hotbar.Slot src = drag.hotbarSlot;
        if (src == null || src.item == null) return;
        if (!owner.CanAcceptItem(src.item)) return;
        if (owner.GetSlot(slotIndex) != null) return; // 埋まっていたら拒否

        int amount = Mathf.Min(drag.dragAmount > 0 ? drag.dragAmount : src.amount, src.amount);

        var newSlot = new Inventory.Slot(src.item, amount);
        if (src.toolInstance != null) newSlot.toolInstance = src.toolInstance;

        owner.SetSlot(slotIndex, newSlot);

        src.amount -= amount;
        if (src.amount <= 0)
            drag.hotbar.ClearSlot(drag.hotbarIndex);

        drag.hotbarSlot = null;

        owner.NotifyChanged();
        drag.hotbarUI?.RefreshAll();
        StartCoroutine(RefreshNextFrame());
    }

    // -----------------------------------------------
    // 機械 → 機械（同じISlotOwner内のみ）
    // -----------------------------------------------

    void HandleMachineToMachine(ItemDragHandler drag)
    {
        if (drag.machineOwner != owner)
        {
            Debug.Log($"[MachineDropHandler] HandleMachineToMachine: 別Owner return drag.machineOwner={drag.machineOwner != null} owner={owner != null}");
            return;
        }
        if (drag.machineSlotIndex == slotIndex) return; // 同スロット

        Inventory.Slot src = drag.machineOwner.GetSlot(drag.machineSlotIndex);
        if (src == null || src.item == null) return;

        Inventory.Slot dst = owner.GetSlot(slotIndex);

        if (dst == null)
        {
            // 空きスロットへ移動
            int amount = Mathf.Min(drag.dragAmount > 0 ? drag.dragAmount : src.amount, src.amount);
            if (amount >= src.amount)
            {
                owner.SetSlot(slotIndex, src);
                drag.machineOwner.SetSlot(drag.machineSlotIndex, null);
            }
            else
            {
                owner.SetSlot(slotIndex, new Inventory.Slot(src.item, amount));
                src.amount -= amount;
            }
        }
        else if (dst.item == src.item && dst.tankInstance == null && src.tankInstance == null)
        {
            // 同種マージ（インスタンスなし）。部分移動（右クリック等）はdragAmountを優先。
            int space = dst.item.maxStack - dst.amount;
            int moveAmount = drag.dragAmount > 0 ? drag.dragAmount : src.amount;
            int toAdd = Mathf.Min(moveAmount, space);
            if (toAdd <= 0) return;
            dst.amount += toAdd;
            src.amount -= toAdd;
            if (src.amount <= 0) drag.machineOwner.SetSlot(drag.machineSlotIndex, null);
        }
        else
        {
            // スワップ（全量のみ）
            if (drag.dragAmount > 0 && drag.dragAmount < src.amount) return;
            drag.machineOwner.SetSlot(drag.machineSlotIndex, dst);
            owner.SetSlot(slotIndex, src);
        }

        drag.machineOwner = null;
        drag.machineSlotIndex = -1;
        owner.NotifyChanged();
        StartCoroutine(RefreshNextFrame());
    }

    // -----------------------------------------------
    // ユーティリティ
    // -----------------------------------------------

    /// <summary>srcのインスタンスを引き継いだ新しいSlotを作る。</summary>
    static Inventory.Slot CreateSlotWithInstances(ItemData item, int amount, Inventory.Slot src)
    {
        var slot = new Inventory.Slot(item, amount);
        // コンストラクタが生成した新規インスタンスをsrcのインスタンスで上書き
        if (src.tankInstance != null) slot.tankInstance = src.tankInstance;
        if (src.thrusterInstance != null) slot.thrusterInstance = src.thrusterInstance;
        if (src.waterTankInstance != null) slot.waterTankInstance = src.waterTankInstance;
        if (src.spacesuitInstance != null) slot.spacesuitInstance = src.spacesuitInstance;
        if (src.toolInstance != null) slot.toolInstance = src.toolInstance;
        return slot;
    }

    IEnumerator RefreshNextFrame()
    {
        yield return null;
        inventoryUI?.RefreshAll();
    }
}