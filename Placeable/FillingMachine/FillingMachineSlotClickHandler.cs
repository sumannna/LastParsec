using UnityEngine;
using UnityEngine.EventSystems;

public class FillingMachineSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    public FillingMachine machine;
    public int slotIndex;
    public bool isInputSlot;
    public Inventory playerInventory;
    public FillingMachineUI ui;

    private float lastClickTime = -1f;
    private const float doubleClickThreshold = 0.3f;

    public void OnPointerClick(PointerEventData eventData)
    {
        float now = Time.time;
        bool isDouble = (now - lastClickTime) <= doubleClickThreshold;
        lastClickTime = now;
        if (!isDouble) return;

        Inventory.Slot[] slots = isInputSlot ? machine.inputSlots : machine.outputSlots;
        Inventory.Slot slot = slots[slotIndex];
        if (slot == null) return;

        playerInventory.AddItemAmount(slot.item, slot.amount);
        bool added = true;
        if (added)
        {
            slots[slotIndex] = null;
            machine.NotifySlotsChanged();
            ui?.RefreshSlots();
            FindObjectOfType<InventoryUI>()?.RefreshAll();
        }
    }
}