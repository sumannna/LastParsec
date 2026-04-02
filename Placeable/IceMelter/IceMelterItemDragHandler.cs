using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IceMelterItemDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private IceMelter machine;
    [SerializeField] private int slotIndex;
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private IceMelterUI ui;

    private GameObject dragIcon;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Image sourceIcon;
    private TMPro.TextMeshProUGUI amountText;

    public void Init(IceMelter machine, int slotIndex, Inventory playerInventory, IceMelterUI ui)
    {
        this.machine = machine;
        this.slotIndex = slotIndex;
        this.playerInventory = playerInventory;
        this.ui = ui;
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "ItemIcon") sourceIcon = t.GetComponent<Image>();
            if (t.name == "AmountText") amountText = t.GetComponent<TMPro.TextMeshProUGUI>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        Inventory.Slot slot = machine.slots[slotIndex];
        if (slot == null || slot.item == null) return;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        canvasRect = canvas.GetComponent<RectTransform>();

        dragIcon = new GameObject("DragIcon_IceMelter");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();
        RectTransform rt = dragIcon.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sourceIcon != null
            ? sourceIcon.GetComponent<RectTransform>().sizeDelta
            : new Vector2(50, 50);
        Image img = dragIcon.AddComponent<Image>();
        img.sprite = slot.item.icon;
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;

        if (sourceIcon != null) sourceIcon.gameObject.SetActive(false);
        if (amountText != null) amountText.gameObject.SetActive(false);
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null || canvasRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 lp);
        dragIcon.GetComponent<RectTransform>().localPosition = lp;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null) { Destroy(dragIcon); dragIcon = null; }
        if (sourceIcon != null) sourceIcon.gameObject.SetActive(true);
        if (amountText != null) amountText.gameObject.SetActive(true);

        Inventory.Slot slot = machine.slots[slotIndex];
        if (slot == null) { StartCoroutine(RefreshNextFrame()); return; }

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var result in results)
        {
            // IceMelterの別スロットへのドロップ
            IceMelterDropHandler iceDrop = result.gameObject.GetComponent<IceMelterDropHandler>();
            if (iceDrop != null)
            {
                iceDrop.OnDropFromMachine(machine, slotIndex);
                return;
            }
            // Inventoryへのドロップ
            DropHandler dh = result.gameObject.GetComponent<DropHandler>();
            if (dh != null)
            {
                MoveToInventory(slot, dh);
                return;
            }
        }

        // UI外へのドロップ → ワールドドロップ
        if (PickupSpawner.Instance != null)
        {
            PickupSpawner.Instance.SpawnItem(slot.item, slot.amount);
            machine.slots[slotIndex] = null;
            machine.NotifySlotsChanged();
        }
        StartCoroutine(RefreshNextFrame());
    }

    void MoveToInventory(Inventory.Slot slot, DropHandler dropHandler)
    {
        int amount = slot.amount;
        int moved = 0;

        var invSlots = playerInventory.GetSlots();
        if (dropHandler.targetIndex >= 0 &&
            dropHandler.targetIndex < invSlots.Length &&
            invSlots[dropHandler.targetIndex] == null)
        {
            playerInventory.AddItemAtIndex(slot.item, dropHandler.targetIndex);
            if (invSlots[dropHandler.targetIndex] != null)
            {
                invSlots[dropHandler.targetIndex].amount = amount;
                moved = amount;
            }
        }
        else
        {
            for (int i = 0; i < amount; i++)
            {
                if (playerInventory.AddItem(slot.item)) moved++;
                else break;
            }
        }

        if (moved > 0)
        {
            machine.ReduceSlot(slot, moved);
            machine.NotifySlotsChanged();
        }
        ui.RequestInventoryRefresh();
    }

    IEnumerator RefreshNextFrame()
    {
        yield return null;
        FindObjectOfType<InventoryUI>()?.RefreshAll();
        ui?.RefreshSlots();
    }
}