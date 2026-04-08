using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ホットバースロットへのD&Dを受け取るハンドラ。
/// インベントリ→ホットバー、ホットバー→ホットバー、機械→ホットバーに対応。
/// ReceiveDrop() で右クリックドラッグからも呼べる。
/// </summary>
public class HotbarDropHandler : MonoBehaviour, IDropHandler
{
    public Hotbar hotbar;
    public int hotbarIndex;
    public Inventory inventory;
    public InventoryUI inventoryUI;
    public HotbarUI hotbarUI;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null) return;
        ItemDragHandler dragHandler = eventData.pointerDrag?.GetComponent<ItemDragHandler>();
        if (dragHandler == null) return;
        ReceiveDrop(dragHandler);
    }

    /// <summary>右クリックドラッグ終了時など、外部から直接呼ぶエントリポイント。</summary>
    public void ReceiveDrop(ItemDragHandler drag)
    {
        if (drag == null) return;

        if (drag.IsMachineDrag()) { HandleMachineToHotbar(drag); return; }
        if (drag.inventorySlot != null) { HandleInventoryToHotbar(drag); return; }
        if (drag.hotbarSlot != null) { HandleHotbarToHotbar(drag); return; }
    }

    // -----------------------------------------------
    // 機械 → ホットバー
    // -----------------------------------------------

    void HandleMachineToHotbar(ItemDragHandler drag)
    {
        var srcSlot = drag.machineOwner.GetSlot(drag.machineSlotIndex);
        if (srcSlot == null || srcSlot.item == null) return;
        if (IsMaterial(srcSlot.item)) return;

        int amount = Mathf.Min(drag.dragAmount > 0 ? drag.dragAmount : srcSlot.amount, srcSlot.amount);
        Hotbar.Slot target = hotbar.GetSlot(hotbarIndex);

        // ホットバーに既存アイテムがあればインベントリへ返す
        if (target.item != null)
        {
            if (target.toolInstance != null)
                inventory.AddItemAmount(target.item, target.amount); // ToolInstance保持は省略（簡略化）
            else
                inventory.AddItemAmount(target.item, target.amount);
        }

        target.item = srcSlot.item;
        target.amount = amount;
        target.toolInstance = srcSlot.toolInstance;

        // 機械スロットを減らす
        srcSlot.amount -= amount;
        if (srcSlot.amount <= 0)
            drag.machineOwner.SetSlot(drag.machineSlotIndex, null);

        drag.machineOwner.NotifyChanged();
        drag.machineOwner = null;
        drag.machineSlotIndex = -1;

        inventoryUI?.RefreshAll();
        hotbarUI?.RefreshAll();
    }

    // -----------------------------------------------
    // インベントリ → ホットバー
    // -----------------------------------------------

    void HandleInventoryToHotbar(ItemDragHandler dragHandler)
    {
        Inventory.Slot sourceSlot = dragHandler.inventorySlot;
        if (sourceSlot == null || sourceSlot.item == null) return;

        if (IsMaterial(sourceSlot.item))
        {
            Debug.Log("マテリアル系アイテムはホットバーに配置できない");
            return;
        }

        Hotbar.Slot target = hotbar.GetSlot(hotbarIndex);

        if (target.item != null)
        {
            if (target.toolInstance != null)
            {
                bool added = inventory.AddItemAtIndex(target.item, GetFirstEmptyInventoryIndex());
                if (added)
                {
                    foreach (var s in inventory.GetSlots())
                    {
                        if (s != null && s.item == target.item && s.toolInstance == null)
                        {
                            s.toolInstance = target.toolInstance;
                            break;
                        }
                    }
                }
            }
            else
            {
                inventory.AddItemAmount(target.item, target.amount);
            }
        }

        target.item = sourceSlot.item;
        target.amount = sourceSlot.amount;
        target.toolInstance = sourceSlot.toolInstance;
        target.tankInstance = sourceSlot.tankInstance;
        target.thrusterInstance = sourceSlot.thrusterInstance;
        target.waterTankInstance = sourceSlot.waterTankInstance;

        inventory.RemoveSlot(sourceSlot);
        dragHandler.inventorySlot = null;

        ItemDragHandler.CancelDrag();

        inventoryUI.RefreshAll();
        hotbarUI.RefreshAll();
    }

    // -----------------------------------------------
    // ホットバー → ホットバー（スワップ）
    // -----------------------------------------------

    void HandleHotbarToHotbar(ItemDragHandler dragHandler)
    {
        if (dragHandler.hotbar != hotbar) return;
        int srcIndex = dragHandler.hotbarIndex;
        if (srcIndex == hotbarIndex) return;

        Hotbar.Slot src = hotbar.GetSlot(srcIndex);
        Hotbar.Slot dst = hotbar.GetSlot(hotbarIndex);

        ItemData tmpItem = dst.item;
        int tmpAmount = dst.amount;
        ToolInstance tmpTool = dst.toolInstance;
        OxygenTankInstance tmpTank = dst.tankInstance;
        ThrusterTankInstance tmpThruster = dst.thrusterInstance;
        WaterTankInstance tmpWater = dst.waterTankInstance;

        dst.item = src.item;
        dst.amount = src.amount;
        dst.toolInstance = src.toolInstance;
        dst.tankInstance = src.tankInstance;
        dst.thrusterInstance = src.thrusterInstance;
        dst.waterTankInstance = src.waterTankInstance;

        src.item = tmpItem;
        src.amount = tmpAmount;
        src.toolInstance = tmpTool;
        src.tankInstance = tmpTank;
        src.thrusterInstance = tmpThruster;
        src.waterTankInstance = tmpWater;

        dragHandler.hotbarSlot = null;

        hotbarUI.RefreshAll();
    }

    // -----------------------------------------------
    // ユーティリティ
    // -----------------------------------------------

    int GetFirstEmptyInventoryIndex()
    {
        var slots = inventory.GetSlots();
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) return i;
        return -1;
    }

    bool IsMaterial(ItemData item)
    {
        if (item == null) return false;
        return item.itemType == ItemType.Material;
    }
}