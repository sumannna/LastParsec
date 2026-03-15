using UnityEngine;

[CreateAssetMenu(fileName = "NewChestData", menuName = "LastParsec/ChestData")]
public class ChestData : ItemData
{
    [Header("チェスト設定")]
    public int slotCount = 10;
    public GameObject chestPrefab;
}