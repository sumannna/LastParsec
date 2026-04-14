using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropHandler : MonoBehaviour, IDropHandler
{
    public Inventory inventory;
    public InventoryUI inventoryUI;
    public Inventory.Slot targetSlot;
    public int targetIndex;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null) return;
        ItemDragHandler dragHandler = eventData.pointerDrag?.GetComponent<ItemDragHandler>();
        if (dragHandler == null) return;
        ReceiveDrop(dragHandler);
    }

    public void ReceiveDrop(ItemDragHandler dragHandler)
    {
        if (dragHandler == null) return;

        if (dragHandler.IsMachineDrag())
        {
            HandleMachineDrop(dragHandler);
            return;
        }

        if (dragHandler.inventorySlot != null)
        {
            HandleInventoryDrop(dragHandler);
            return;
        }

        if (dragHandler.equipmentSlotData != null)
        {
            HandleEquipmentDrop(dragHandler);
            return;
        }

        if (dragHandler.hotbarSlot != null)
        {
            HandleHotbarDrop(dragHandler);
            return;
        }
    }

    // -----------------------------------------------
    // 機械スロット → インベントリ
    // -----------------------------------------------

    void HandleMachineDrop(ItemDragHandler drag)
    {
        ISlotOwner owner = drag.machineOwner;
        int srcIdx = drag.machineSlotIndex;

        // スナップショット対応：配列外の場合もスナップショットからデータ取得
        Inventory.Slot src = drag.GetDraggedMachineSlot();
        if (src == null || src.item == null) return;

        bool isInstance = src.tankInstance != null || src.waterTankInstance != null
                       || src.thrusterInstance != null || src.spacesuitInstance != null;

        int amount = isInstance
            ? src.amount
            : Mathf.Clamp(drag.dragAmount > 0 ? drag.dragAmount : src.amount, 1, src.amount);

        if (targetSlot == null)
        {
            var invSlots = inventory.GetSlots();
            if (targetIndex < 0 || targetIndex >= invSlots.Length || invSlots[targetIndex] != null) return;

            var newSlot = new Inventory.Slot(src.item, amount);
            if (src.tankInstance != null) newSlot.tankInstance = src.tankInstance;
            if (src.thrusterInstance != null) newSlot.thrusterInstance = src.thrusterInstance;
            if (src.waterTankInstance != null) newSlot.waterTankInstance = src.waterTankInstance;
            if (src.spacesuitInstance != null) newSlot.spacesuitInstance = src.spacesuitInstance;
            if (src.toolInstance != null) newSlot.toolInstance = src.toolInstance;
            invSlots[targetIndex] = newSlot;
        }
        else
        {
            if (isInstance || targetSlot.item != src.item) return;
            int space = targetSlot.item.maxStack - targetSlot.amount;
            int toAdd = Mathf.Min(amount, space);
            if (toAdd <= 0) return;
            targetSlot.amount += toAdd;
            amount = toAdd;
        }

        // ApplyDragSourceRemoval で既に元スロットから dragAmount 分を除去済みのため、
        // ここでは減算不要。ReadOnly/非ReadOnly 問わず同様。
        drag.machineOwner = null;
        drag.machineSlotIndex = -1;
        owner.NotifyChanged();
        StartCoroutine(RefreshNextFrame());
    }

    // -----------------------------------------------
    // インベントリ → インベントリ
    // -----------------------------------------------

    void HandleInventoryDrop(ItemDragHandler dragHandler)
    {
        if (dragHandler == null || dragHandler.inventorySlot == null) return;

        Inventory.Slot sourceSlot = dragHandler.inventorySlot;
        int dragAmt = dragHandler.dragAmount;
        bool isGauge = IsGaugeSlot(sourceSlot);

        if (targetSlot == null)
        {
            // 空きスロットへ
            if (dragAmt >= sourceSlot.amount)
            {
                // フルDrag：MoveSlotToIndex が「配列外スロット」にも対応
                inventory.MoveSlotToIndex(sourceSlot, targetIndex);
            }
            else
            {
                // 部分Drag：ソースは配列内のまま
                inventory.SplitSlotToIndex(sourceSlot, dragAmt, targetIndex);
            }
        }
        else
        {
            // 埋まったスロットへ
            if (sourceSlot == targetSlot)
            {
                Debug.Log($"[Drop] 同一スロットへのドロップ");
                return;
            }

            bool isSameItem = targetSlot.item == sourceSlot.item;
            bool canMerge = isSameItem && !isGauge && !IsGaugeSlot(targetSlot);

            if (canMerge)
            {
                inventory.MergeIntoSlot(sourceSlot, dragAmt, targetSlot);

                // フルDragでマージしきれなかった残余アイテムを配列に戻す
                // （MergeIntoSlotはオブジェクトのamountを直接減じるため、
                //   sourceSlot.amount > 0 なら残余あり）
                bool srcInArray = false;
                var allSlots = inventory.GetSlots();
                for (int i = 0; i < allSlots.Length; i++)
                    if (allSlots[i] == sourceSlot) { srcInArray = true; break; }

                if (!srcInArray && sourceSlot.amount > 0)
                {
                    // 配列外かつ残余あり → 元インデックスを優先して配置
                    inventory.PlaceSlotInInventory(sourceSlot, dragHandler.InventorySourceIndex);
                }
            }
            else
            {
                if (dragAmt >= sourceSlot.amount)
                {
                    // フルDrag：SwapSlots が「配列外スロット」にも対応
                    inventory.SwapSlots(sourceSlot, targetSlot);
                }
                else
                {
                    // 部分Dragは占有スロットへ移動不可
                    return;
                }
            }
        }

        dragHandler.inventorySlot = null;
        StartCoroutine(RefreshNextFrame());
    }

    // -----------------------------------------------
    // 装備 → インベントリ
    // -----------------------------------------------

    void HandleEquipmentDrop(ItemDragHandler dragHandler)
    {
        if (dragHandler == null || dragHandler.equipmentSystem == null || dragHandler.equipmentSlotData == null)
            return;

        ItemData item = dragHandler.equipmentSystem.GetEquipped(dragHandler.equipmentSlotData);
        if (item == null) return;

        SpacesuitInstance spacesuitInst = dragHandler.equipmentSystem.GetSpacesuitInstance(dragHandler.equipmentSlotData);
        OxygenTankInstance oxyTank = dragHandler.equipmentSystem.GetTankInstance(dragHandler.equipmentSlotData);
        ThrusterTankInstance thrusterTank = dragHandler.equipmentSystem.GetThrusterInstance(dragHandler.equipmentSlotData);

        if (targetSlot != null)
            HandleEquipmentDropToFilledSlot(dragHandler, item, oxyTank, thrusterTank, spacesuitInst);
        else
            HandleEquipmentDropToEmptySlot(dragHandler, item);

        dragHandler.equipmentSlotData = null;
        StartCoroutine(RefreshNextFrame());
    }

    void HandleEquipmentDropToFilledSlot(
        ItemDragHandler dragHandler,
        ItemData equippedItem,
        OxygenTankInstance equippedOxy,
        ThrusterTankInstance equippedThruster,
        SpacesuitInstance equippedSuit)
    {
        if (targetSlot == null || targetSlot.item == null || equippedItem == null) return;
        if (targetSlot.item.itemType != equippedItem.itemType) return;

        ItemData invItem = targetSlot.item;
        OxygenTankInstance invOxy = targetSlot.tankInstance;
        ThrusterTankInstance invThruster = targetSlot.thrusterInstance;
        SpacesuitInstance invSuit = targetSlot.spacesuitInstance;
        int idx = targetIndex;

        var (oldItem, oldOxy, oldThruster, oldSuit) =
            equippedItem.itemType == ItemType.Spacesuit
            ? dragHandler.equipmentSystem.UnequipSpacesuitOnly(dragHandler.equipmentSlotData)
            : dragHandler.equipmentSystem.Unequip(dragHandler.equipmentSlotData);

        dragHandler.equipmentSystem.Equip(dragHandler.equipmentSlotData, invItem, invOxy, invThruster, invSuit);
        inventory.RemoveSlot(targetSlot);

        if (oldOxy != null) inventory.AddItemWithTankAtIndex(oldItem, oldOxy, idx);
        else if (oldThruster != null) inventory.AddItemWithThrusterAtIndex(oldItem, oldThruster, idx);
        else if (oldSuit != null) inventory.AddItemWithSpacesuitAtIndex(oldItem, oldSuit, idx);
        else inventory.AddItemAtIndex(oldItem, idx);
    }

    void HandleEquipmentDropToEmptySlot(ItemDragHandler dragHandler, ItemData equippedItem)
    {
        if (equippedItem == null) return;

        if (equippedItem.itemType == ItemType.Spacesuit)
        {
            var oxySlot = dragHandler.equipmentSystem.GetSlotByType(ItemType.OxygenTank);
            ItemData equippedOxy = dragHandler.equipmentSystem.GetEquipped(oxySlot);
            OxygenTankInstance equippedOxyInst = dragHandler.equipmentSystem.GetTankInstance(oxySlot);

            inventory.ReserveIndex(targetIndex);
            if (equippedOxy != null)
            {
                dragHandler.equipmentSystem.Unequip(oxySlot);
                inventory.AddItemWithTank(equippedOxy, equippedOxyInst);
            }
            inventory.UnreserveIndex(targetIndex);

            var (oldItem, _, _, oldSuit) =
                dragHandler.equipmentSystem.UnequipSpacesuitOnly(dragHandler.equipmentSlotData);

            if (oldSuit != null) inventory.AddItemWithSpacesuitAtIndex(oldItem, oldSuit, targetIndex);
            else inventory.AddItemAtIndex(oldItem, targetIndex);
        }
        else
        {
            var (oldItem, oldOxy, oldThruster, oldSuit) =
                dragHandler.equipmentSystem.Unequip(dragHandler.equipmentSlotData);

            if (oldOxy != null) inventory.AddItemWithTankAtIndex(oldItem, oldOxy, targetIndex);
            else if (oldThruster != null) inventory.AddItemWithThrusterAtIndex(oldItem, oldThruster, targetIndex);
            else if (oldSuit != null) inventory.AddItemWithSpacesuitAtIndex(oldItem, oldSuit, targetIndex);
            else inventory.AddItemAtIndex(oldItem, targetIndex);
        }
    }

    // -----------------------------------------------
    // ホットバー → インベントリ
    // -----------------------------------------------

    void HandleHotbarDrop(ItemDragHandler dragHandler)
    {
        if (dragHandler == null || dragHandler.hotbarSlot == null) return;

        // スナップショット対応：クリア済みスロットでもデータを取得
        Hotbar.Slot srcHotbar = dragHandler.GetDraggedHotbarSlot();
        if (srcHotbar == null || srcHotbar.item == null) return;

        ItemData item = srcHotbar.item;

        if (targetSlot == null)
        {
            bool added = inventory.AddItemAtIndex(item, targetIndex);
            if (added)
            {
                var newSlot = inventory.GetSlots()[targetIndex];
                if (newSlot != null)
                {
                    newSlot.amount = srcHotbar.amount;
                    if (srcHotbar.toolInstance != null) newSlot.toolInstance = srcHotbar.toolInstance;
                    if (srcHotbar.tankInstance != null) newSlot.tankInstance = srcHotbar.tankInstance;
                    if (srcHotbar.thrusterInstance != null) newSlot.thrusterInstance = srcHotbar.thrusterInstance;
                    if (srcHotbar.waterTankInstance != null) newSlot.waterTankInstance = srcHotbar.waterTankInstance;
                }
                // ホットバーは Drag開始時に ClearSlot 済み
                dragHandler.hotbarSlot = null;
                dragHandler.hotbarSlotSnapshot = null; // スナップショットをクリア（復元不要）
            }
        }
        else
        {
            // 埋まったスロットへ → スワップ
            ItemData invItem = targetSlot.item;
            int invAmount = targetSlot.amount;
            ToolInstance invTool = targetSlot.toolInstance;
            OxygenTankInstance invTank = targetSlot.tankInstance;
            ThrusterTankInstance invThruster = targetSlot.thrusterInstance;
            SpacesuitInstance invSuit = targetSlot.spacesuitInstance;
            WaterTankInstance invWater = targetSlot.waterTankInstance;

            targetSlot.item = item;
            targetSlot.amount = srcHotbar.amount;
            targetSlot.toolInstance = srcHotbar.toolInstance;
            targetSlot.tankInstance = srcHotbar.tankInstance;
            targetSlot.thrusterInstance = srcHotbar.thrusterInstance;
            targetSlot.spacesuitInstance = null;
            targetSlot.waterTankInstance = srcHotbar.waterTankInstance;

            // ホットバー実スロットへインベントリアイテムをセット
            Hotbar.Slot actualHotbarSlot = dragHandler.hotbar.GetSlot(dragHandler.hotbarIndex);
            if (actualHotbarSlot != null)
            {
                actualHotbarSlot.item = invItem;
                actualHotbarSlot.amount = invAmount;
                actualHotbarSlot.toolInstance = invTool;
                actualHotbarSlot.tankInstance = invTank;
                actualHotbarSlot.thrusterInstance = invThruster;
                actualHotbarSlot.waterTankInstance = invWater;
            }

            dragHandler.hotbarSlot = null;
            dragHandler.hotbarSlotSnapshot = null;
        }

        ItemDragHandler.CancelDrag();

        StartCoroutine(RefreshHotbarNextFrame(dragHandler.hotbarUI));
        StartCoroutine(RefreshNextFrame());
    }

    // -----------------------------------------------
    // ユーティリティ
    // -----------------------------------------------

    bool IsGaugeSlot(Inventory.Slot slot)
    {
        return slot?.item is OxygenTankData
            || slot?.item is ThrusterTankData
            || slot?.item is SpacesuitData;
    }

    IEnumerator RefreshNextFrame()
    {
        yield return null;
        if (inventoryUI != null)
            inventoryUI.RefreshAll();
    }

    IEnumerator RefreshHotbarNextFrame(HotbarUI hotbarUI)
    {
        yield return null;
        if (hotbarUI != null)
            hotbarUI.RefreshAll();
    }
}