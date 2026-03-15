using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// リサーチテーブルのブループリントスロットのD&D処理。
/// インベントリスロットへドロップで返却する。
/// </summary>
public class BlueprintSlotDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ResearchTableSystem researchTable;
    public Inventory inventory;
    public InventoryUI inventoryUI;

    private GameObject dragIcon;
    private Canvas canvas;
    private RectTransform canvasRect;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (researchTable == null || researchTable.IsResearching) return;

        // ブループリントが存在するか確認
        if (!researchTable.TryGetCurrentBlueprintName(out string bpName)) return;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        canvasRect = canvas.GetComponent<RectTransform>();

        // ドラッグアイコン生成
        Image sourceIcon = GetComponent<Image>();
        if (sourceIcon == null || sourceIcon.sprite == null) return;

        dragIcon = new GameObject("DragIcon_Blueprint");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        RectTransform rt = dragIcon.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(60f, 60f);
        rt.localScale = Vector3.one;

        Image img = dragIcon.AddComponent<Image>();
        img.sprite = sourceIcon.sprite;
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;

        // ドラッグ中はスロットアイコンを半透明に
        sourceIcon.color = new Color(1f, 1f, 1f, 0.3f);

        Debug.Log($"[BlueprintSlotDragHandler] OnBeginDrag bp={bpName}");
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
        // アイコン色を戻す
        Image sourceIcon = GetComponent<Image>();
        if (sourceIcon != null)
            sourceIcon.color = Color.white;

        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }

        // ドロップ先をレイキャストで検出
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        Debug.Log($"[BlueprintSlotDragHandler] OnEndDrag hits={results.Count}");

        foreach (var result in results)
        {
            DropHandler dh = result.gameObject.GetComponent<DropHandler>();
            if (dh != null)
            {
                Debug.Log($"[BlueprintSlotDragHandler] DropHandler found on {result.gameObject.name}");
                bool taken = researchTable.TryTakeBlueprint();
                Debug.Log($"[BlueprintSlotDragHandler] TryTakeBlueprint result={taken}");
                if (taken && inventoryUI != null && inventoryUI.IsOpen)
                    inventoryUI.RefreshAll();
                return;
            }
        }

        Debug.Log("[BlueprintSlotDragHandler] インベントリ外にドロップ → キャンセル");
    }
}