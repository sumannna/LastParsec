using System;

/// <summary>
/// Inventory.Slot[] を包む ISlotOwner 実装。MonoBehaviourなし。
/// UI側で new ArraySlotOwner(...) して各ハンドラに渡す。
/// </summary>
public class ArraySlotOwner : ISlotOwner
{
    private readonly Inventory.Slot[] slots;
    private readonly Func<ItemData, bool> canAcceptFunc;
    private readonly Action onChanged;

    public bool IsReadOnly { get; }
    public int SlotCount => slots.Length;

    /// <param name="slots">対象スロット配列（直接参照）</param>
    /// <param name="readOnly">Trueならドロップ不可（アウトプット専用スロット）</param>
    /// <param name="canAccept">追加受け入れ判定（null=全アイテム受け入れ）</param>
    /// <param name="onChanged">内容変更時に呼ぶアクション（UI再描画等）</param>
    public ArraySlotOwner(
        Inventory.Slot[] slots,
        bool readOnly,
        Func<ItemData, bool> canAccept,
        Action onChanged)
    {
        this.slots = slots;
        IsReadOnly = readOnly;
        canAcceptFunc = canAccept;
        this.onChanged = onChanged;
    }

    public Inventory.Slot GetSlot(int index) =>
        (index >= 0 && index < slots.Length) ? slots[index] : null;

    public void SetSlot(int index, Inventory.Slot slot)
    {
        if (index >= 0 && index < slots.Length)
            slots[index] = slot;
    }

    public bool CanAcceptItem(ItemData item)
    {
        if (IsReadOnly || item == null) return false;
        return canAcceptFunc == null || canAcceptFunc(item);
    }

    public void NotifyChanged() => onChanged?.Invoke();
}