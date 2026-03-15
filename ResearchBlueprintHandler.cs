using UnityEngine;
using UnityEngine.EventSystems;

public class ResearchBlueprintHandler : MonoBehaviour, IDropHandler
{
    private ResearchTableSystem researchTable;

    public void OnDrop(PointerEventData eventData)
    {
        ItemDragHandler dragHandler = eventData.pointerDrag?.GetComponent<ItemDragHandler>();
        if (dragHandler == null || dragHandler.inventorySlot == null) return;

        bool result = researchTable != null && researchTable.TrySetBlueprint(dragHandler.inventorySlot);
        Debug.Log($"[ResearchBlueprintHandler] TrySetBlueprint result={result}");

        if (result)
        {
            Debug.Log($"[ResearchBlueprintHandler] inventorySlot null‘O: {dragHandler.inventorySlot?.item?.itemName}");
            dragHandler.inventorySlot = null;
            Debug.Log($"[ResearchBlueprintHandler] inventorySlot nullŒã: {dragHandler.inventorySlot}");
        }
    }

    public void SetResearchTable(ResearchTableSystem table)
    {
        researchTable = table;
    }
}