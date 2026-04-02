using UnityEngine;
using UnityEngine.EventSystems;

public class IceMelterDropHandler : MonoBehaviour, IDropHandler
{
    [SerializeField] private IceMelter machine;
    [SerializeField] private int slotIndex;
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private IceMelterUI ui;

    public void Init(IceMelter machine, int slotIndex, Inventory playerInventory, IceMelterUI ui)
    {
        this.machine = machine;
        this.slotIndex = slotIndex;
        this.playerInventory = playerInventory;
        this.ui = ui;
    }

    public void OnDrop(PointerEventData eventData)
    {
        ItemDragHandler drag = eventData.pointerDrag?.GetComponent<ItemDragHandler>();
        if (drag == null || drag.inventorySlot == null) return;

        Inventory.Slot src = drag.inventorySlot;

        // 氷以外は弾く
        if (machine.iceItemData != null && src.item != machine.iceItemData) return;

        // スロットが埋まっていたら弾く
        if (machine.slots[slotIndex] != null) return;

        machine.slots[slotIndex] = new Inventory.Slot(src.item, src.amount);
        playerInventory.RemoveSlot(src);
        drag.inventorySlot = null;
        StartCoroutine(RefreshNextFrame());
    }

    public void OnDropFromMachine(IceMelter sourceMachine, int sourceSlotIndex)
    {
        Inventory.Slot sourceSlot = sourceMachine.slots[sourceSlotIndex];
        if (sourceSlot == null) return;

        // 同スロットへのドロップは無視
        if (sourceMachine == machine && sourceSlotIndex == slotIndex) return;

        // ドロップ先が空でない場合は無視
        if (machine.slots[slotIndex] != null) return;

        machine.slots[slotIndex] = new Inventory.Slot(sourceSlot.item, sourceSlot.amount);
        sourceMachine.slots[sourceSlotIndex] = null;
        sourceMachine.NotifySlotsChanged();
        if (sourceMachine != machine) machine.NotifySlotsChanged();

        StartCoroutine(RefreshNextFrame());
    }

    System.Collections.IEnumerator RefreshNextFrame()
    {
        yield return null;
        FindObjectOfType<InventoryUI>()?.RefreshAll();
        ui?.RefreshSlots();
    }
}