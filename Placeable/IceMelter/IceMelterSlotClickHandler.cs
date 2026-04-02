using UnityEngine;
using UnityEngine.EventSystems;

public class IceMelterSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private IceMelter machine;
    [SerializeField] private int slotIndex;
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private IceMelterUI ui;

    private float lastClickTime = -1f;
    private const float doubleClickThreshold = 0.3f;

    public void Init(IceMelter machine, int slotIndex, Inventory playerInventory, IceMelterUI ui)
    {
        this.machine = machine;
        this.slotIndex = slotIndex;
        this.playerInventory = playerInventory;
        this.ui = ui;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        float now = Time.time;
        bool isDouble = (now - lastClickTime) <= doubleClickThreshold;
        lastClickTime = now;
        if (!isDouble) return;

        Inventory.Slot slot = machine.slots[slotIndex];
        if (slot == null || slot.item == null) return;

        int moved = 0;
        for (int i = 0; i < slot.amount; i++)
        {
            if (playerInventory.AddItem(slot.item)) moved++;
            else break;
        }
        if (moved > 0)
        {
            machine.ReduceSlot(slot, moved);
            machine.NotifySlotsChanged();
            FindObjectOfType<InventoryUI>()?.RefreshAll();
            ui?.RefreshSlots();
        }
    }
}