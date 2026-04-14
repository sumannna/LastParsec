using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// リサーチテーブルのブループリントスロットのダブルクリック処理。
/// ダブルクリックでインベントリへ返却する。
/// </summary>
public class BlueprintSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    public ResearchTableSystem researchTable;
    public InventoryUI inventoryUI;

    private float lastClickTime = -1f;
    private const float doubleClickThreshold = 0.3f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        float now = Time.time;
        bool isDouble = (now - lastClickTime) <= doubleClickThreshold;
        lastClickTime = now;

        if (!isDouble) return;

        Debug.Log("[BlueprintSlotClickHandler] ダブルクリック → TryTakeBlueprint");
        bool success = researchTable.TryTakeBlueprint();
        Debug.Log($"[BlueprintSlotClickHandler] TryTakeBlueprint result={success}");

        if (success && inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.RefreshAll();
    }
}