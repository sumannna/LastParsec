using UnityEngine;
using UnityEngine.EventSystems;

public class FillingMachineDropHandler : MonoBehaviour, IDropHandler
{
    public FillingMachine machine;
    public int slotIndex;
    public bool isInputSlot;
    public Inventory playerInventory;
    public FillingMachineUI ui;

    public void OnDrop(PointerEventData eventData)
    {
        ItemDragHandler drag = eventData.pointerDrag?.GetComponent<ItemDragHandler>();
        if (drag == null || drag.inventorySlot == null) return;
        if (!isInputSlot) return;

        Inventory.Slot src = drag.inventorySlot;
        if (machine.inputSlots[slotIndex] != null) return;

        machine.inputSlots[slotIndex] = new Inventory.Slot(src.item, src.amount);
        playerInventory.RemoveSlot(src);
        drag.inventorySlot = null;
        ui?.RefreshSlots();
        FindObjectOfType<InventoryUI>()?.RefreshAll();
    }
}