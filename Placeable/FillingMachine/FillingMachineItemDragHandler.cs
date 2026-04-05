using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FillingMachineItemDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public FillingMachine machine;
    public int slotIndex;
    public bool isInputSlot;
    public Inventory playerInventory;
    public FillingMachineUI ui;

    private GameObject dragIcon;
    private Canvas rootCanvas;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Inventory.Slot[] slots = isInputSlot ? machine.inputSlots : machine.outputSlots;
        Inventory.Slot slot = slots[slotIndex];
        if (slot == null) { eventData.pointerDrag = null; return; }

        rootCanvas = GetComponentInParent<Canvas>();
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(rootCanvas.transform, false);
        dragIcon.transform.SetAsLastSibling();
        Image img = dragIcon.AddComponent<Image>();
        img.sprite = slot.item.icon;
        img.raycastTarget = false;
        RectTransform rt = dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(50, 50);

        GetComponent<Image>().color = Color.clear;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            eventData.position, rootCanvas.worldCamera,
            out Vector2 pos);
        dragIcon.GetComponent<RectTransform>().localPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null) Destroy(dragIcon);
        GetComponent<Image>().color = Color.white;

        Inventory.Slot[] slots = isInputSlot ? machine.inputSlots : machine.outputSlots;
        Inventory.Slot slot = slots[slotIndex];
        if (slot == null) return;

        // ドロップ先がインベントリスロットでなければ元に戻す
        if (eventData.pointerEnter == null) return;
        DropHandler drop = eventData.pointerEnter.GetComponent<DropHandler>();
        if (drop == null) return;

        playerInventory.AddItemAmount(slot.item, slot.amount);
        slots[slotIndex] = null;
        machine.NotifySlotsChanged();
        ui?.RefreshSlots();
        FindObjectOfType<InventoryUI>()?.RefreshAll();
    }
}