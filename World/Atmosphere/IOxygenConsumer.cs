/// <summary>
/// 酸素を消費するPlaceableが実装するインターフェース。
/// IPowerConsumer と対称的な設計。
/// ShipAtmosphereSystem が一元管理する。
/// </summary>
public interface IOxygenConsumer
{
    /// <summary>稼働時の酸素消費量（m³/h）</summary>
    float OxygenConsumption { get; }

    /// <summary>現在稼働中かどうか（酸素消費すべき状態か）</summary>
    bool IsOxygenConsuming { get; }

    /// <summary>酸素が供給された → 稼働再開</summary>
    void OnOxygenSupplied();

    /// <summary>酸素が枯渇した → 即座に停止</summary>
    void OnOxygenCutOff();
}