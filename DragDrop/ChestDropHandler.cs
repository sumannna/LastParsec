using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// チェストスロットへのドロップ処理。
/// インベントリ→チェスト、チェスト→チェストに対応。
/// </summary>
public class ChestDropHandler : MonoBehaviour, IDropHandler
{
    public ChestUI chestUI;
    public ChestInventory chestInventory;
    public Inventory playerInventory;
    public InventoryUI inventoryUI;
    public Inventory.Slot targetSlot;
    public int targetIndex;

    public void OnDrop(PointerEventData eventData)
    {
        ItemDragHandler dragHandler = eventData.pointerDrag?.GetComponent<ItemDragHandler>();
        ChestItemDragHandler chestDragHandler = eventData.pointerDrag?.GetComponent<ChestItemDragHandler>();

        if (dragHandler != null && dragHandler.inventorySlot != null)
        {
            HandleInventoryToChest(dragHandler);
            return;
        }

        if (chestDragHandler != null && chestDragHandler.chestSlot != null)
        {
            HandleChestToChest(chestDragHandler);
            return;
        }
    }

    void HandleInventoryToChest(ItemDragHandler dragHandler)
    {
        Inventory.Slot sourceSlot = dragHandler.inventorySlot;
        int dragAmt = dragHandler.dragAmount;

        if (targetSlot == null)
        {
            if (dragAmt >= sourceSlot.amount)
                MoveInventorySlotToChest(sourceSlot);
            else
                SplitInventorySlotToChest(sourceSlot, dragAmt);
        }
        else
        {
            if (sourceSlot.item == targetSlot.item && !(sourceSlot.item is OxygenTankData) && !(sourceSlot.item is ThrusterTankData))
                chestInventory.MergeIntoSlot(sourceSlot, dragAmt, targetSlot);
            else if (dragAmt >= sourceSlot.amount)
                SwapInventoryWithChest(sourceSlot);
        }

        dragHandler.inventorySlot = null;
        StartCoroutine(RefreshNextFrame());
    }

    void MoveInventorySlotToChest(Inventory.Slot sourceSlot)
    {
        if (chestInventory.IsFull()) return;
        ItemData item = sourceSlot.item;
        int amount = sourceSlot.amount;
        playerInventory.RemoveSlot(sourceSlot);
        var newSlot = new Inventory.Slot(item, amount);
        chestInventory.GetSlots()[targetIndex] = newSlot;
    }

    void SplitInventorySlotToChest(Inventory.Slot sourceSlot, int amount)
    {
        if (chestInventory.IsFull()) return;
        sourceSlot.amount -= amount;
        if (sourceSlot.amount <= 0) playerInventory.RemoveSlot(sourceSlot);
        chestInventory.GetSlots()[targetIndex] = new Inventory.Slot(sourceSlot.item, amount);
    }

    void SwapInventoryWithChest(Inventory.Slot sourceSlot)
    {
        if (targetSlot == null) return;
        ItemData chestItem = targetSlot.item;
        int chestAmount = targetSlot.amount;

        chestInventory.GetSlots()[targetIndex] = new Inventory.Slot(sourceSlot.item, sourceSlot.amount);
        playerInventory.RemoveSlot(sourceSlot);
        playerInventory.AddItemAtIndex(chestItem, playerInventory.GetSlots().Length - 1);
    }

    void HandleChestToChest(ChestItemDragHandler dragHandler)
    {
        Inventory.Slot sourceSlot = dragHandler.chestSlot;
        if (sourceSlot == null) return;

        if (targetSlot == null)
            chestInventory.MoveSlotToIndex(sourceSlot, targetIndex);
        else if (sourceSlot == targetSlot)
            return;
        else if (sourceSlot.item == targetSlot.item)
            chestInventory.MergeIntoSlot(sourceSlot, sourceSlot.amount, targetSlot);
        else
            chestInventory.SwapSlots(sourceSlot, targetSlot);

        dragHandler.chestSlot = null;
        StartCoroutine(RefreshNextFrame());
    }

    IEnumerator RefreshNextFrame()
    {
        yield return null;
        chestUI?.RefreshAll();
        inventoryUI?.RefreshAll();
    }
}