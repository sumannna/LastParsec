/// <summary>
/// 酸素を船内に供給するコンポーネントが実装するインターフェース。
/// AlgaeTank・FillingMachine大気放出モードが実装する。
/// </summary>
public interface IOxygenSupplier
{
    /// <summary>現在供給中かどうか</summary>
    bool IsSupplying { get; }

    /// <summary>供給量（m³/h）</summary>
    float OxygenGeneration { get; }
}