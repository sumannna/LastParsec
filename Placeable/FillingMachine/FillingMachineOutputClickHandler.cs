using UnityEngine;
using UnityEngine.EventSystems;

public class FillingMachineOutputClickHandler : MonoBehaviour, IPointerClickHandler
{
    public FillingMachine machine;
    public int slotIndex;
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

        Inventory.Slot slot = machine.outputSlots[slotIndex];
        if (slot == null) return;

        bool added = false;
        if (slot.tankInstance != null)
            added = playerInventory.AddItemWithTank(slot.item, slot.tankInstance);
        else if (slot.thrusterInstance != null)
            added = playerInventory.AddItemWithThruster(slot.item, slot.thrusterInstance);
        else if (slot.waterTankInstance != null)
            added = playerInventory.AddItemWithWaterTank(slot.item, slot.waterTankInstance);
        else
            added = playerInventory.AddItem(slot.item);

        if (added)
        {
            machine.outputSlots[slotIndex] = null;
            machine.NotifySlotsChanged();
        }
    }
}