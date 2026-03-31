using UnityEngine;

/// <summary>
/// ホットバーのデータ管理。インベントリとは独立した10スロット。
/// </summary>
public class Hotbar : MonoBehaviour
{
    public const int SlotCount = 10;

    [System.Serializable]
    public class Slot
    {
        public ItemData item;
        public int amount;
        public ToolInstance toolInstance; // ← 追加：道具の耐久インスタンス
    }

    private Slot[] slots = new Slot[SlotCount];
    private int selectedIndex = 0;

    public int SelectedIndex => selectedIndex;

    void Awake()
    {
        for (int i = 0; i < SlotCount; i++)
            slots[i] = new Slot();
    }

    public Slot[] GetSlots() => slots;

    public Slot GetSlot(int index)
    {
        if (index < 0 || index >= SlotCount) return null;
        return slots[index];
    }

    public Slot GetSelected() => slots[selectedIndex];

    public void SetSelected(int index)
    {
        if (index < 0 || index >= SlotCount) return;
        selectedIndex = index;
    }

    /// <summary>
    /// インベントリからアイテムをホットバーに移動する。
    /// ホットバー側の既存アイテムはインベントリに返す。
    /// ToolInstanceもインベントリスロットから引き継ぐ。
    /// </summary>
    public bool SetSlotFromInventory(int hotbarIndex, Inventory.Slot inventorySlot, Inventory inventory, InventoryUI inventoryUI)
    {
        if (hotbarIndex < 0 || hotbarIndex >= SlotCount) return false;
        if (inventorySlot == null || inventorySlot.item == null) return false;

        Slot target = slots[hotbarIndex];

        // ホットバーに既存アイテムがあればインベントリに返す
        if (target.item != null)
        {
            inventory.AddItemAmount(target.item, target.amount);
            // ToolInstanceはインベントリ側では管理しないため破棄
        }

        // インベントリからホットバーへ移動（ToolInstanceも引き継ぐ）
        target.item = inventorySlot.item;
        target.amount = inventorySlot.amount;
        target.toolInstance = inventorySlot.toolInstance; // ← 追加

        inventory.RemoveSlot(inventorySlot);

        inventoryUI.RefreshAll();
        return true;
    }

    /// <summary>
    /// ホットバースロットを空にする。
    /// </summary>
    public void ClearSlot(int index)
    {
        if (index < 0 || index >= SlotCount) return;
        slots[index].item = null;
        slots[index].amount = 0;
        slots[index].toolInstance = null; // ← 追加
    }

    /// <summary>
    /// アイテム使用後などにamountを減らす。0になったらスロットをクリア。
    /// </summary>
    public void ReduceSelected(int amount = 1)
    {
        Slot s = slots[selectedIndex];
        if (s.item == null) return;
        s.amount -= amount;
        if (s.amount <= 0)
        {
            s.item = null;
            s.amount = 0;
            s.toolInstance = null; // ← 追加
        }
    }
}