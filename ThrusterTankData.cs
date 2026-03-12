using UnityEngine;

[CreateAssetMenu(fileName = "NewThrusterTank", menuName = "Inventory/ThrusterTankData")]
public class ThrusterTankData : ItemData
{
    [Header("タンク設定")]
    public float maxTankFuel = 100f;
}
