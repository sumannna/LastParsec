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
        // スナップショット対応：Drag開始時に機械スロットから除去済みのためスナップショットからデータ取得
        Inventory.Slot srcSlot = drag.GetDraggedMachineSlot();
        if (srcSlot == null || srcSlot.item == null) return;
        if (IsMaterial(srcSlot.item)) return;

        int amount = Mathf.Min(drag.dragAmount > 0 ? drag.dragAmount : srcSlot.amount, srcSlot.amount);
        Hotbar.Slot target = hotbar.GetSlot(hotbarIndex);

        // ホットバーに既存アイテムがあればインベントリへ返す
        if (target.item != null)
        {
            inventory.AddItemAmount(target.item, target.amount);
        }

        // ホットバーへ移動
        target.item = srcSlot.item;
        target.amount = amount;
        target.toolInstance = srcSlot.toolInstance;
        target.tankInstance = srcSlot.tankInstance;
        target.thrusterInstance = srcSlot.thrusterInstance;
        target.waterTankInstance = srcSlot.waterTankInstance;

        // ApplyDragSourceRemoval で既に元スロットから dragAmount 分を除去済みのため、
        // ここでは減算不要。ReadOnly/非ReadOnly 問わず同様。
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

        // ホットバーに既存アイテムがあればインベントリへ返す
        if (target.item != null)
        {
            if (target.toolInstance != null)
            {
                int emptyIdx = GetFirstEmptyInventoryIndex();
                bool added = emptyIdx >= 0 && inventory.AddItemAtIndex(target.item, emptyIdx);
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

        // ホットバーへ移動
        target.item = sourceSlot.item;
        target.amount = sourceSlot.amount;
        target.toolInstance = sourceSlot.toolInstance;
        target.tankInstance = sourceSlot.tankInstance;
        target.thrusterInstance = sourceSlot.thrusterInstance;
        target.waterTankInstance = sourceSlot.waterTankInstance;

        // インベントリから除去（フルDragで既に配列外なら RemoveSlot は空振り → 安全）
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

        // スナップショット対応：Drag開始時にスロットクリア済みなので実スロットではなくスナップショットを使う
        Hotbar.Slot srcData = dragHandler.GetDraggedHotbarSlot();
        if (srcData == null || srcData.item == null) return;

        // 実スロット（クリア済み）と移動先スロット
        Hotbar.Slot srcActual = hotbar.GetSlot(srcIndex);
        Hotbar.Slot dst = hotbar.GetSlot(hotbarIndex);

        // dstの内容をsrcActualに移す（スワップ）
        if (srcActual != null)
        {
            srcActual.item = dst.item;
            srcActual.amount = dst.amount;
            srcActual.toolInstance = dst.toolInstance;
            srcActual.tankInstance = dst.tankInstance;
            srcActual.thrusterInstance = dst.thrusterInstance;
            srcActual.waterTankInstance = dst.waterTankInstance;
        }

        // スナップショットの内容をdstに移す
        dst.item = srcData.item;
        dst.amount = srcData.amount;
        dst.toolInstance = srcData.toolInstance;
        dst.tankInstance = srcData.tankInstance;
        dst.thrusterInstance = srcData.thrusterInstance;
        dst.waterTankInstance = srcData.waterTankInstance;

        // 成功：参照をクリア（OnEndDragでhotbarSlotSnapshot = null される）
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