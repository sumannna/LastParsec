using UnityEngine;
using UnityEngine.EventSystems;

public class IceMelterInventorySlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private IceMelter machine;
    [SerializeField] private Inventory inventory;
    [SerializeField] private int slotIndex;
    [SerializeField] private IceMelterUI ui;

    private float lastClickTime = -1f;
    private const float doubleClickThreshold = 0.3f;

    public void Init(IceMelter machine, Inventory inventory, int slotIndex, IceMelterUI ui)
    {
        this.machine = machine;
        this.inventory = inventory;
        this.slotIndex = slotIndex;
        this.ui = ui;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        float now = Time.time;
        bool isDouble = (now - lastClickTime) <= doubleClickThreshold;
        lastClickTime = now;
        if (!isDouble) return;
        if (!ui.IsOpen) return;

        Inventory.Slot slot = inventory.GetSlots()[slotIndex];
        if (slot == null || slot.item == null) return;
        if (machine.iceItemData != null && slot.item != machine.iceItemData) return;

        int moved = 0;
        for (int i = 0; i < slot.amount; i++)
        {
            if (machine.AddItem(slot.item)) moved++;
            else break;
        }
        if (moved > 0)
        {
            inventory.ReduceSlot(slot, moved);
            machine.NotifySlotsChanged();
            FindObjectOfType<InventoryUI>()?.RefreshAll();
            ui?.RefreshSlots();
        }
    }
}