using UnityEngine;

/// <summary>
/// 耐久値を持つ道具の基底データ。
/// PickaxeDataなど具体的な道具はこれを継承する。
/// </summary>
public class ToolData : ItemData
{
    [Header("耐久設定")]
    public float maxDurability = 50f;
    public float durabilityPerUse = 1f;
}