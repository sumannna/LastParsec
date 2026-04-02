using UnityEngine;

[CreateAssetMenu(fileName = "NewWaterTank", menuName = "Inventory/WaterTankData")]
public class WaterTankData : ItemData
{
    [Header("水タンク")]
    public float maxWater = 100f;
    public float waterPerClick = 10f; // 1クリックで回復する水分量
}