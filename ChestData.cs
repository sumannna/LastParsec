using UnityEngine;

[CreateAssetMenu(fileName = "NewChestData", menuName = "LastParsec/ChestData")]
public class ChestData : PlaceableData
{
    [Header("チェスト設定")]
    public int slotCount = 10;
}