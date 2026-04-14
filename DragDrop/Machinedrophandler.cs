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

        bool isInstance = src.tankInstance != null || src.waterTankInstance != null
                       || src.thrusterInstance != null || src.spacesuitInstance != null;
        int amount = isInstance
            ? src.amount
            : Mathf.Min(drag.dragAmount > 0 ? drag.dragAmount : src.amount, src.amount);

        if (existing == null)
        {
            var newSlot = CreateSlotWithInstances(src.item, amount, src);
            owner.SetSlot(slotIndex, newSlot);
            // フルDragで既に配列外なら ReduceSlot は空振り → 安全
            playerInventory.ReduceSlot(src, amount);
            drag.inventorySlot = null;
        }
        else if (!isInstance && existing.item == src.item)
        {
            int space = existing.item.maxStack - existing.amount;
            int toAdd = Mathf.Min(amount, space);
            if (toAdd <= 0) return;
            existing.amount += toAdd;
            playerInventory.ReduceSlot(src, toAdd);
            drag.inventorySlot = null;
        }
        else return;

        owner.NotifyChanged();
        StartCoroutine(RefreshNextFrame());
    }

    // -----------------------------------------------
    // ホットバー → 機械
    // -----------------------------------------------

    void HandleHotbarToMachine(ItemDragHandler drag)
    {
        // スナップショット対応：Drag開始時にホットバースロットはクリア済み
        Hotbar.Slot src = drag.GetDraggedHotbarSlot();
        if (src == null || src.item == null) return;
        if (!owner.CanAcceptItem(src.item)) return;
        if (owner.GetSlot(slotIndex) != null) return; // 埋まっていたら拒否

        int amount = Mathf.Min(drag.dragAmount > 0 ? drag.dragAmount : src.amount, src.amount);

        var newSlot = new Inventory.Slot(src.item, amount);
        if (src.toolInstance != null) newSlot.toolInstance = src.toolInstance;
        if (src.tankInstance != null) newSlot.tankInstance = src.tankInstance;
        if (src.thrusterInstance != null) newSlot.thrusterInstance = src.thrusterInstance;
        if (src.waterTankInstance != null) newSlot.waterTankInstance = src.waterTankInstance;

        owner.SetSlot(slotIndex, newSlot);

        // 実ホットバースロットはDrag開始時に既にクリア済み
        // 部分Dragの残余がある場合のみ量を調整
        if (drag.hotbar != null && drag.hotbarIndex >= 0)
        {
            Hotbar.Slot actualSlot = drag.hotbar.GetSlot(drag.hotbarIndex);
            if (actualSlot != null && actualSlot.item != null)
            {
                // 部分Drag：残余が実スロットに残っている
                actualSlot.amount -= amount;
                if (actualSlot.amount <= 0)
                    drag.hotbar.ClearSlot(drag.hotbarIndex);
            }
            // フルDrag：既にクリア済み → 何もしない
        }

        // 成功：参照をクリア（OnEndDragでhotbarSlotSnapshot = null される）
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
            Debug.Log($"[MachineDropHandler] HandleMachineToMachine: 別Owner return");
            return;
        }
        if (drag.machineSlotIndex == slotIndex) return; // 同スロット

        // スナップショット対応：Drag開始時に元スロットから除去済み
        Inventory.Slot src = drag.GetDraggedMachineSlot();
        if (src == null || src.item == null) return;

        Inventory.Slot dst = owner.GetSlot(slotIndex);
        int amount = src.amount; // スナップショット内の移動量

        if (dst == null)
        {
            // 空きスロットへ移動（スナップショットをそのまま配置）
            owner.SetSlot(slotIndex, src);
            // ソーススロットの後処理はApplyDragSourceRemovalで完了済み
        }
        else if (dst.item == src.item && dst.tankInstance == null && src.tankInstance == null)
        {
            // 同種マージ（インスタンスなし）
            int space = dst.item.maxStack - dst.amount;
            int toAdd = Mathf.Min(amount, space);
            if (toAdd <= 0) return;
            dst.amount += toAdd;
            src.amount -= toAdd; // スナップショットの残余を更新

            if (src.amount > 0)
            {
                // マージしきれなかった → OnEndDragでRestoreDraggedSlotが残余を復元
                owner.NotifyChanged();
                return; // machineOwnerをnullにしないことで失敗を通知
            }
            // src.amount == 0 → 全部マージ成功 → 下の成功処理へ
        }
        else
        {
            // スワップ（フルDragのみ可）
            // フルDrag判定：Drag開始時に元スロットがnullにされている場合
            bool wasFullDrag = drag.machineOwner.GetSlot(drag.machineSlotIndex) == null;
            if (!wasFullDrag) return; // 部分DragはスワップN不可

            // ソーススロットにdstを置き、dstスロットにsrcを置く
            drag.machineOwner.SetSlot(drag.machineSlotIndex, dst);
            owner.SetSlot(slotIndex, src);
        }

        // 成功：参照クリア
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