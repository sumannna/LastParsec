/// <summary>
/// ToolDataの実行時インスタンス。OxygenTankInstanceと同じパターン。
/// スロットに保持され、耐久値を個別管理する。
/// </summary>
public class ToolInstance
{
    public ToolData data;
    public float currentDurability;

    public ToolInstance(ToolData data)
    {
        this.data = data;
        this.currentDurability = data.maxDurability;
    }

    public bool IsBroken => currentDurability <= 0f;
    public float Ratio => currentDurability / data.maxDurability; // 0〜1（UI用）

    /// <summary>
    /// 使用時に耐久を消費する。戻り値：消費後に破損したか。
    /// </summary>
    public bool ConsumeOnUse()
    {
        currentDurability -= data.durabilityPerUse;
        currentDurability = UnityEngine.Mathf.Clamp(currentDurability, 0f, data.maxDurability);
        return IsBroken;
    }
}