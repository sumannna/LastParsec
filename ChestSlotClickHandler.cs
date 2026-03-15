using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// チェストスロットのクリック処理。
/// ダブルクリックでインベントリ⇔チェスト間移動。
/// Shift+ダブルクリックで全移動。
/// </summary>
public class ChestSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    public ChestUI chestUI;
    public ChestInventory chestInventory;
    public Inventory playerInventory;
    public InventoryUI inventoryUI;
    public Inventory.Slot chestSlot;
    public int slotIndex;

    private float lastClickTime = -1f;
    private const float doubleClickThreshold = 0.3f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float now = Time.time;
        bool isDouble = (now - lastClickTime) <= doubleClickThreshold;
        lastClickTime = now;

        if (!isDouble) return;
        if (chestSlot == null || chestSlot.item == null) return;

        // チェスト→インベントリへ移動
        int amount = shift ? chestSlot.amount : chestSlot.amount;
        int moved = 0;

        for (int i = 0; i < amount; i++)
        {
            if (playerInventory.AddItem(chestSlot.item))
                moved++;
            else
                break;
        }

        if (moved > 0)
        {
            chestInventory.ReduceSlot(chestSlot, moved);
            chestUI?.RefreshAll();
            inventoryUI?.RefreshAll();
        }
    }
}