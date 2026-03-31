using UnityEngine;

[CreateAssetMenu(fileName = "NewOxygenTank", menuName = "Inventory/OxygenTankData")]
public class OxygenTankData : ItemData
{
    [Header("タンク設定")]
    public float maxTankOxygen = 100f;
}
