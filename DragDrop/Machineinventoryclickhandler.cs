using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// インベントリスロットのダブルクリックで機械・チェストへアイテムを移動。
/// 機械/チェストUI開放中に InventoryUI.AddMachineHandlers() で動的に追加される。
/// IceMelterInventorySlotClickHandler を統合・汎用化。
/// </summary>
public class MachineInventoryClickHandler : MonoBehaviour, IPointerClickHandler
{
    private ISlotOwner targetOwner;
    private Inventory inventory;
    private int slotIndex;
    private InventoryUI inventoryUI;

    private float lastClickTime = -1f;
    private const float doubleClickThreshold = 0.3f;

    public void Init(ISlotOwner targetOwner, Inventory inventory, int slotIndex, InventoryUI inventoryUI)
    {
        this.targetOwner = targetOwner;
        this.inventory = inventory;
        this.slotIndex = slotIndex;
        this.inventoryUI = inventoryUI;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        float now = Time.time;
        bool isDouble = (now - lastClickTime) <= doubleClickThreshold;
        lastClickTime = now;
        if (!isDouble) return;

        if (targetOwner == null || targetOwner.IsReadOnly) return;

        Inventory.Slot slot = inventory.GetSlots()[slotIndex];
        if (slot == null || slot.item == null) return;
        if (!targetOwner.CanAcceptItem(slot.item)) return;

        // 機械側の空きスロットを探す
        int emptyMachineIndex = -1;
        for (int i = 0; i < targetOwner.SlotCount; i++)
        {
            if (targetOwner.GetSlot(i) == null) { emptyMachineIndex = i; break; }
        }
        if (emptyMachineIndex < 0) return; // 機械がいっぱい

        // スロットごと移動（amount・インスタンスそのまま）
        var newSlot = new Inventory.Slot(slot.item, slot.amount);
        if (slot.tankInstance != null) newSlot.tankInstance = slot.tankInstance;
        if (slot.thrusterInstance != null) newSlot.thrusterInstance = slot.thrusterInstance;
        if (slot.waterTankInstance != null) newSlot.waterTankInstance = slot.waterTankInstance;
        if (slot.spacesuitInstance != null) newSlot.spacesuitInstance = slot.spacesuitInstance;
        if (slot.toolInstance != null) newSlot.toolInstance = slot.toolInstance;

        targetOwner.SetSlot(emptyMachineIndex, newSlot);
        inventory.RemoveSlot(slot);

        targetOwner.NotifyChanged();
        inventoryUI?.RefreshAll();
    }
}