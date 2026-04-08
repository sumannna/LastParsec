using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 機械・チェストスロットのダブルクリック処理。
/// DC でスロットのアイテムをインベントリへ移動。
/// IceMelterSlotClickHandler / FillingMachineSlotClickHandler / ChestSlotClickHandler を統合。
/// </summary>
public class MachineSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    private ISlotOwner owner;
    private int slotIndex;
    private Inventory playerInventory;
    private InventoryUI inventoryUI;

    private float lastClickTime = -1f;
    private const float doubleClickThreshold = 0.3f;

    public void Init(ISlotOwner owner, int slotIndex, Inventory playerInventory, InventoryUI inventoryUI)
    {
        this.owner = owner;
        this.slotIndex = slotIndex;
        this.playerInventory = playerInventory;
        this.inventoryUI = inventoryUI;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        float now = Time.time;
        bool isDouble = (now - lastClickTime) <= doubleClickThreshold;
        lastClickTime = now;
        if (!isDouble) return;

        Inventory.Slot slot = owner.GetSlot(slotIndex);
        if (slot == null || slot.item == null) return;

        bool moved = MoveSlotToInventory(slot);
        if (moved)
        {
            owner.SetSlot(slotIndex, null);
            owner.NotifyChanged();
            inventoryUI?.RefreshAll();
        }
    }

    /// <summary>インスタンス付きアイテムに対応した移動。</summary>
    bool MoveSlotToInventory(Inventory.Slot slot)
    {
        // インスタンス付きアイテムは専用メソッドで追加（インスタンスを保持）
        if (slot.tankInstance != null)
            return playerInventory.AddItemWithTank(slot.item, slot.tankInstance);
        if (slot.thrusterInstance != null)
            return playerInventory.AddItemWithThruster(slot.item, slot.thrusterInstance);
        if (slot.waterTankInstance != null)
            return playerInventory.AddItemWithWaterTank(slot.item, slot.waterTankInstance);
        if (slot.spacesuitInstance != null)
            return playerInventory.AddItemWithSpacesuit(slot.item, slot.spacesuitInstance);

        // 通常アイテム（複数個まとめて移動）
        playerInventory.AddItemAmount(slot.item, slot.amount);
        return true;
    }
}