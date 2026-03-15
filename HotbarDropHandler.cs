using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ホットバースロットへのD&Dを受け取るハンドラ。
/// インベントリ→ホットバー、ホットバー→ホットバーのスワップに対応。
/// ToolInstanceも正しく引き継ぐ。
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

        // インベントリ → ホットバー
        if (dragHandler.inventorySlot != null)
        {
            HandleInventoryToHotbar(dragHandler);
            return;
        }

        // ホットバー → ホットバー（スワップ）
        if (dragHandler.hotbarSlot != null)
        {
            HandleHotbarToHotbar(dragHandler);
            return;
        }
    }

    // -----------------------------------------------
    // インベントリ → ホットバー
    // -----------------------------------------------

    void HandleInventoryToHotbar(ItemDragHandler dragHandler)
    {
        Inventory.Slot sourceSlot = dragHandler.inventorySlot;
        if (sourceSlot == null || sourceSlot.item == null) return;

        // マテリアル系はホットバー不可
        if (IsMaterial(sourceSlot.item))
        {
            Debug.Log("マテリアル系アイテムはホットバーに配置できない");
            return;
        }

        Hotbar.Slot target = hotbar.GetSlot(hotbarIndex);

        // ホットバーに既存アイテムがあればインベントリに返す（ToolInstanceも含む）
        if (target.item != null)
        {
            if (target.toolInstance != null)
            {
                // ToolInstanceを保持したままインベントリへ戻す
                var slot = new Inventory.Slot(target.item, target.amount);
                slot.toolInstance = target.toolInstance;
                // Inventoryの内部配列に直接追加するためAddItemAmountは使えない
                // AddItemを使いToolInstanceは後から設定
                bool added = inventory.AddItemAtIndex(target.item, GetFirstEmptyInventoryIndex());
                if (added)
                {
                    // 追加されたスロットを探してToolInstanceをセット
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

        // インベントリ → ホットバーへ移動（ToolInstanceも引き継ぐ）
        target.item = sourceSlot.item;
        target.amount = sourceSlot.amount;
        target.toolInstance = sourceSlot.toolInstance;

        inventory.RemoveSlot(sourceSlot);
        dragHandler.inventorySlot = null;

        // DragIconの残存を防ぐ
        ItemDragHandler.CancelDrag();

        inventoryUI.RefreshAll();
        hotbarUI.RefreshAll();
    }
    // -----------------------------------------------

    void HandleHotbarToHotbar(ItemDragHandler dragHandler)
    {
        if (dragHandler.hotbar != hotbar) return;
        int srcIndex = dragHandler.hotbarIndex;
        if (srcIndex == hotbarIndex) return;

        Hotbar.Slot src = hotbar.GetSlot(srcIndex);
        Hotbar.Slot dst = hotbar.GetSlot(hotbarIndex);

        // スワップ（ToolInstanceごと）
        ItemData tmpItem = dst.item;
        int tmpAmount = dst.amount;
        ToolInstance tmpTool = dst.toolInstance;

        dst.item = src.item;
        dst.amount = src.amount;
        dst.toolInstance = src.toolInstance;

        src.item = tmpItem;
        src.amount = tmpAmount;
        src.toolInstance = tmpTool;

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