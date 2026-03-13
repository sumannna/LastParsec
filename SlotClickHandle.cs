using UnityEngine;
using UnityEngine.EventSystems;

public class SlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    // インベントリスロット用
    public Inventory.Slot inventorySlot;
    public InventoryUI inventoryUI;

    // 装備スロット用
    public EquipmentSlotData equipmentSlotData;
    public EquipmentUI equipmentUI;

    // 共通参照
    public EquipmentSystem equipmentSystem;
    public Inventory inventory;

    // 分割ウィンドウ
    public SplitWindowUI splitWindowUI;

    private float lastLeftClickTime = -1f;
    private float lastRightClickTime = -1f;
    private float lastMiddleClickTime = -1f;
    private const float doubleClickThreshold = 0.3f;

    public void OnPointerClick(PointerEventData eventData)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        switch (eventData.button)
        {
            // ── 左クリック ──
            case PointerEventData.InputButton.Left:
                {
                    // シングルクリック：分割ウィンドウにスロット選択を通知
                    if (inventorySlot != null)
                        splitWindowUI?.Select(inventorySlot, inventory, inventoryUI);

                    bool isDouble = IsDoubleClick(ref lastLeftClickTime);
                    if (!isDouble) return;

                    if (inventorySlot != null)
                    {
                        if (shift && !IsGaugeSlot(inventorySlot))
                        {
                            SplitToEmpty(Mathf.Max(1, inventorySlot.amount / 2));
                        }
                        else
                        {
                            // ① リサーチテーブルが開いていてBPなら → リサーチテーブルにセット
                            if (TrySetBlueprintToResearchTable()) return;

                            // ② 通常の装備処理
                            TryEquipFromInventory();
                        }
                    }
                    else if (equipmentSlotData != null)
                    {
                        TryUnequipToInventory();
                    }
                    break;
                }

            // ── 右クリック ──
            case PointerEventData.InputButton.Right:
                {
                    bool isDouble = IsDoubleClick(ref lastRightClickTime);
                    if (!isDouble) return;

                    if (inventorySlot != null && !IsGaugeSlot(inventorySlot))
                        SplitToEmpty(1);
                    break;
                }

            // ── ホイールクリック ──
            case PointerEventData.InputButton.Middle:
                {
                    if (!shift) return;

                    bool isDouble = IsDoubleClick(ref lastMiddleClickTime);
                    if (!isDouble) return;

                    if (inventorySlot != null && !IsGaugeSlot(inventorySlot))
                        SplitToEmpty(Mathf.Max(1, inventorySlot.amount / 3));
                    break;
                }
        }
    }

    // -----------------------------------------------
    // リサーチテーブルへのBPセット
    // -----------------------------------------------

    /// <summary>
    /// リサーチテーブルが開いていてスロットがBlueprintDataなら
    /// リサーチテーブルにセットする。
    /// セットに成功したらtrueを返し、以降の処理をスキップさせる。
    /// </summary>
    bool TrySetBlueprintToResearchTable()
    {
        Debug.Log("[SlotClickHandler] TrySetBlueprintToResearchTable 開始");

        if (inventorySlot == null)
        {
            Debug.Log("[SlotClickHandler] inventorySlot が null");
            return false;
        }

        if (inventorySlot.item is not BlueprintData)
        {
            Debug.Log($"[SlotClickHandler] Blueprintではない item={inventorySlot.item?.itemName}");
            return false;
        }

        if (ResearchTableSystem.Instance == null)
        {
            Debug.Log("[SlotClickHandler] ResearchTableSystem.Instance が null");
            return false;
        }

        if (!ResearchTableSystem.Instance.IsOpen)
        {
            Debug.Log("[SlotClickHandler] ResearchTable が閉じている");
            return false;
        }

        bool success = ResearchTableSystem.Instance.TrySetBlueprint(inventorySlot);
        Debug.Log($"[SlotClickHandler] TrySetBlueprint 結果={success}");

        if (success)
        {
            Debug.Log($"[SlotClickHandler] BPをリサーチテーブルにセット：{inventorySlot.item.itemName}");
            inventoryUI?.RefreshAll();
        }
        return success;
    }

    // -----------------------------------------------
    // ダブルクリック判定
    // -----------------------------------------------

    bool IsDoubleClick(ref float lastTime)
    {
        float now = Time.time;
        bool isDouble = (now - lastTime) <= doubleClickThreshold;
        lastTime = now;
        return isDouble;
    }

    // -----------------------------------------------
    // 分割操作
    // -----------------------------------------------

    void SplitToEmpty(int splitAmount)
    {
        if (inventorySlot == null || inventorySlot.amount < 2) return;
        splitAmount = Mathf.Clamp(splitAmount, 1, inventorySlot.amount - 1);
        bool ok = inventory.SplitSlotToEmpty(inventorySlot, splitAmount);
        if (!ok) Debug.Log("インベントリ満杯：分割できない");
        inventoryUI.RefreshAll();
    }

    // -----------------------------------------------
    // 装備操作
    // -----------------------------------------------

    void TryEquipFromInventory()
    {
        ItemData item = inventorySlot.item;
        EquipmentSlotData targetSlot = equipmentSystem.GetSlotByType(item.itemType);

        if (targetSlot == null)
        {
            Debug.Log("このアイテムは装備できない");
            return;
        }

        if (item.itemType == ItemType.OxygenTank && !equipmentSystem.HasSpacesuit())
        {
            Debug.Log("宇宙服を先に装備してください");
            return;
        }

        ItemData currentEquipped = equipmentSystem.GetEquipped(targetSlot);
        int currentSlotIndex = GetSlotIndex();

        if (currentEquipped != null)
        {
            var (oldItem, oldOxy, oldThruster, oldSuit) = item.itemType == ItemType.Spacesuit
                ? equipmentSystem.UnequipSpacesuitOnly(targetSlot)
                : equipmentSystem.Unequip(targetSlot);

            inventory.RemoveSlot(inventorySlot);

            if (oldOxy != null) inventory.AddItemWithTankAtIndex(oldItem, oldOxy, currentSlotIndex);
            else if (oldThruster != null) inventory.AddItemWithThrusterAtIndex(oldItem, oldThruster, currentSlotIndex);
            else if (oldSuit != null) inventory.AddItemWithSpacesuitAtIndex(oldItem, oldSuit, currentSlotIndex);
            else inventory.AddItemAtIndex(oldItem, currentSlotIndex);

            equipmentSystem.Equip(targetSlot, item,
                inventorySlot.tankInstance,
                inventorySlot.thrusterInstance,
                inventorySlot.spacesuitInstance);
        }
        else
        {
            bool success = equipmentSystem.Equip(targetSlot, item,
                inventorySlot.tankInstance,
                inventorySlot.thrusterInstance,
                inventorySlot.spacesuitInstance);
            if (success)
                inventory.RemoveSlot(inventorySlot);
        }

        inventoryUI.RefreshAll();
    }

    int GetSlotIndex()
    {
        var slots = inventory.GetSlots();
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == inventorySlot) return i;
        return -1;
    }

    void TryUnequipToInventory()
    {
        var (item, oxyTank, thrusterTank, spacesuitInst) =
            equipmentSystem.Unequip(equipmentSlotData);
        if (item == null) return;

        bool added;
        if (oxyTank != null) added = inventory.AddItemWithTank(item, oxyTank);
        else if (thrusterTank != null) added = inventory.AddItemWithThruster(item, thrusterTank);
        else if (spacesuitInst != null) added = inventory.AddItemWithSpacesuit(item, spacesuitInst);
        else added = inventory.AddItem(item);

        if (!added) Debug.Log("インベントリ満杯");
        inventoryUI.RefreshAll();
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
}