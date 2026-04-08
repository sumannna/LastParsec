using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ホットバースロットのダブルクリックでインベントリへアイテムを移動する。
/// </summary>
public class HotbarSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public Hotbar hotbar;
    [HideInInspector] public int hotbarIndex;
    [HideInInspector] public Inventory inventory;
    [HideInInspector] public InventoryUI inventoryUI;
    [HideInInspector] public HotbarUI hotbarUI;

    private float lastClickTime = -1f;
    private const float doubleClickThreshold = 0.3f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        float now = Time.time;
        bool isDouble = (now - lastClickTime) <= doubleClickThreshold;
        lastClickTime = now;
        if (!isDouble) return;

        Hotbar.Slot slot = hotbar.GetSlot(hotbarIndex);
        if (slot == null || slot.item == null) return;

        // インベントリに追加（toolInstanceも引き継ぐ）
        bool added;
        if (slot.toolInstance != null)
        {
            // AddItemAtIndexでamountとtoolInstanceを手動設定
            int idx = FindFirstEmptyIndex();
            if (idx < 0) return;
            added = inventory.AddItemAtIndex(slot.item, idx);
            if (added)
            {
                var newSlot = inventory.GetSlots()[idx];
                if (newSlot != null)
                {
                    newSlot.amount = slot.amount;
                    newSlot.toolInstance = slot.toolInstance;
                }
            }
        }
        else
        {
            inventory.AddItemAmount(slot.item, slot.amount);
            added = true;
        }

        if (added)
        {
            hotbar.ClearSlot(hotbarIndex);
            hotbarUI?.RefreshAll();
            inventoryUI?.RefreshAll();
        }
    }

    int FindFirstEmptyIndex()
    {
        var slots = inventory.GetSlots();
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) return i;
        return -1;
    }
}