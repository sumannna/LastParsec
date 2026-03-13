using UnityEngine;
using UnityEngine.EventSystems;

public class ResearchBlueprintHandler : MonoBehaviour, IDropHandler
{
    private ResearchTableSystem researchTable;

    public void OnDrop(PointerEventData eventData)
    {
        ItemDragHandler dragHandler = eventData.pointerDrag?.GetComponent<ItemDragHandler>();

        Debug.Log($"[ResearchBlueprintHandler] OnDrop dragHandlerNull={dragHandler == null}, targetObj={(researchTable != null ? researchTable.gameObject.name : "null")}, targetId={(researchTable != null ? researchTable.GetInstanceID() : 0)}");

        if (dragHandler == null || dragHandler.inventorySlot == null)
        {
            Debug.Log("[ResearchBlueprintHandler] inventorySlot ‚ª null");
            return;
        }

        Debug.Log($"[ResearchBlueprintHandler] item={(dragHandler.inventorySlot.item != null ? dragHandler.inventorySlot.item.itemName : "null")}, amount={dragHandler.inventorySlot.amount}");

        bool result = researchTable != null && researchTable.TrySetBlueprint(dragHandler.inventorySlot);
        Debug.Log($"[ResearchBlueprintHandler] TrySetBlueprint result={result}, targetObj={(researchTable != null ? researchTable.gameObject.name : "null")}, targetId={(researchTable != null ? researchTable.GetInstanceID() : 0)}");
    }

    public void SetResearchTable(ResearchTableSystem table)
    {
        researchTable = table;
    }
}