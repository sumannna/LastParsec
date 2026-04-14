using UnityEngine;

[CreateAssetMenu(fileName = "NewPickaxe", menuName = "Inventory/PickaxeData")]
public class PickaxeData : ToolData
{
    [Header("採掘設定")]
    public float miningDamage = 25f;   // 1回の採掘で与えるダメージ
    public float miningRange = 3f;     // 採掘可能距離
    public LayerMask miningLayer;      // 採掘対象レイヤー（MiningTargetを含むレイヤー）
}