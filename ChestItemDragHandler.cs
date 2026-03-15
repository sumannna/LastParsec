using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// チェストスロットからのドラッグ処理。
/// </summary>
public class ChestItemDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ChestUI chestUI;
    public ChestInventory chestInventory;
    public Inventory playerInventory;
    public InventoryUI inventoryUI;
    public Inventory.Slot chestSlot;

    [HideInInspector] public int dragAmount;

    private GameObject dragIcon;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Image sourceIcon;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvasRect = canvas.GetComponent<RectTransform>();
        CacheSourceIcon();
    }

    void CacheSourceIcon()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == "ItemIcon") { sourceIcon = child.GetComponent<Image>(); break; }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (chestSlot == null || chestSlot.item == null) return;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        canvasRect = canvas.GetComponent<RectTransform>();
        CacheSourceIcon();
        if (sourceIcon == null) return;

        dragAmount = chestSlot.amount;

        dragIcon = new GameObject("DragIcon_Chest");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        RectTransform rt = dragIcon.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sourceIcon.GetComponent<RectTransform>().sizeDelta;
        rt.localScale = Vector3.one;

        Image img = dragIcon.AddComponent<Image>();
        img.sprite = chestSlot.item.icon;
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;

        sourceIcon.gameObject.SetActive(false);
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null || canvasRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        dragIcon.GetComponent<RectTransform>().localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null) { Destroy(dragIcon); dragIcon = null; }
        if (sourceIcon != null) sourceIcon.gameObject.SetActive(true);

        // インベントリへのドロップ判定
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            DropHandler dh = result.gameObject.GetComponent<DropHandler>();
            if (dh != null)
            {
                // チェスト→インベントリ
                MoveChestSlotToInventory(dh);
                return;
            }
        }

        StartCoroutine(RefreshNextFrame());
    }

    void MoveChestSlotToInventory(DropHandler dropHandler)
    {
        if (chestSlot == null || chestSlot.item == null) return;

        bool added = playerInventory.AddItemAtIndex(chestSlot.item, dropHandler.targetIndex);
        if (!added) added = playerInventory.AddItem(chestSlot.item);

        if (added)
        {
            chestInventory.RemoveSlot(chestSlot);
            chestSlot = null;
        }

        StartCoroutine(RefreshNextFrame());
    }

    System.Collections.IEnumerator RefreshNextFrame()
    {
        yield return null;
        chestUI?.RefreshAll();
        inventoryUI?.RefreshAll();
    }
}