using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class EquipmentDropHandler : MonoBehaviour, IDropHandler
{
    public EquipmentSystem equipmentSystem;
    public Inventory inventory;
    public InventoryUI inventoryUI;
    public EquipmentUI equipmentUI;
    public EquipmentSlotData slotData;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null) return;

        ItemDragHandler dragHandler = eventData.pointerDrag?.GetComponent<ItemDragHandler>();
        if (dragHandler == null) return;

        // EquipmentDropHandler は「インベントリ → 装備」のみ担当
        if (dragHandler.inventorySlot == null) return;
        if (dragHandler.inventorySlot.item == null) return;
        if (equipmentSystem == null || inventory == null) return;
        if (slotData == null) return;

        ItemData item = dragHandler.inventorySlot.item;

        // 装備可能タイプチェック
        if (item.itemType != slotData.acceptedType)
            return;

        // 宇宙服なしでは酸素タンク装備不可
        if (item.itemType == ItemType.OxygenTank && !equipmentSystem.HasSpacesuit())
        {
            Debug.Log("宇宙服を先に装備してください");
            return;
        }

        HandleInventoryToEquipmentDrop(dragHandler, item);
    }

    void HandleInventoryToEquipmentDrop(ItemDragHandler dragHandler, ItemData item)
    {
        Inventory.Slot sourceSlot = dragHandler.inventorySlot;
        if (sourceSlot == null || sourceSlot.item == null) return;

        // インスタンス退避
        OxygenTankInstance oxyTank = sourceSlot.tankInstance;
        ThrusterTankInstance thrusterTank = sourceSlot.thrusterInstance;
        SpacesuitInstance spacesuitInst = sourceSlot.spacesuitInstance;

        // 元のインベントリ位置を取得
        // フルDragで既に配列外の場合、GetSlotIndexは-1を返す
        // → drag.InventorySourceIndex をフォールバックとして使用
        int sourceIndex = GetSlotIndex(sourceSlot);
        if (sourceIndex < 0)
            sourceIndex = dragHandler.InventorySourceIndex;

        // 既存装備
        ItemData currentEquipped = equipmentSystem.GetEquipped(slotData);

        if (currentEquipped != null)
        {
            SwapInventoryItemWithEquippedItem(
                sourceSlot,
                sourceIndex,
                item,
                oxyTank,
                thrusterTank,
                spacesuitInst
            );
        }
        else
        {
            // 単純装備
            // フルDragで既に配列外なら RemoveSlot は空振り → 安全
            inventory.RemoveSlot(sourceSlot);
            equipmentSystem.Equip(slotData, item, oxyTank, thrusterTank, spacesuitInst);
        }

        // ドロップ成功後に参照を切る
        dragHandler.inventorySlot = null;

        StartCoroutine(RefreshNextFrame());
    }

    void SwapInventoryItemWithEquippedItem(
        Inventory.Slot sourceSlot,
        int sourceIndex,
        ItemData newItem,
        OxygenTankInstance newOxy,
        ThrusterTankInstance newThruster,
        SpacesuitInstance newSuit)
    {
        if (sourceSlot == null || newItem == null) return;

        // 現在装備中のものを外す
        var (oldItem, oldOxy, oldThruster, oldSuit) =
            newItem.itemType == ItemType.Spacesuit
            ? equipmentSystem.UnequipSpacesuitOnly(slotData)
            : equipmentSystem.Unequip(slotData);

        // ドラッグ元のインベントリスロットを空にする
        // フルDragで既に配列外なら RemoveSlot は空振り → 安全
        inventory.RemoveSlot(sourceSlot);

        // 新しいアイテムを装備
        equipmentSystem.Equip(slotData, newItem, newOxy, newThruster, newSuit);

        // 外した装備を元のインベントリ位置へ戻す
        // sourceIndex が -1 の場合（インベントリ満杯など）は AddItemXxx が失敗するため安全
        if (oldItem != null)
        {
            if (oldOxy != null)
                inventory.AddItemWithTankAtIndex(oldItem, oldOxy, sourceIndex);
            else if (oldThruster != null)
                inventory.AddItemWithThrusterAtIndex(oldItem, oldThruster, sourceIndex);
            else if (oldSuit != null)
                inventory.AddItemWithSpacesuitAtIndex(oldItem, oldSuit, sourceIndex);
            else
                inventory.AddItemAtIndex(oldItem, sourceIndex);
        }
    }

    /// <summary>
    /// インベントリ配列内でスロットのインデックスを探す。
    /// フルDragで既に配列外の場合は -1 を返す。
    /// </summary>
    int GetSlotIndex(Inventory.Slot slot)
    {
        if (slot == null || inventory == null) return -1;

        var slots = inventory.GetSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == slot)
                return i;
        }

        return -1;
    }

    IEnumerator RefreshNextFrame()
    {
        yield return null;

        if (inventoryUI != null)
            inventoryUI.RefreshAll();
    }
}