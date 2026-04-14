/// <summary>
/// インベントリ・チェスト・機械のスロットを統一的に扱うインターフェース。
/// </summary>
public interface ISlotOwner
{
    Inventory.Slot GetSlot(int index);
    void SetSlot(int index, Inventory.Slot slot);
    int SlotCount { get; }

    /// <summary>このスロットへのアイテム受け入れ可否（Falseなら読み取り専用）</summary>
    bool CanAcceptItem(ItemData item);

    /// <summary>Trueのとき、DropHandlerはドロップを拒否する（アウトプットスロット等）</summary>
    bool IsReadOnly { get; }

    /// <summary>スロット内容変更後に呼ぶ（UI再描画トリガー）</summary>
    void NotifyChanged();
}