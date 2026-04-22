using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "LastParsec/BuildingData")]
public class BuildingData : ScriptableObject
{
    [Header("基本情報")]
    public string buildingName = "建築物";
    public BuildingType buildingType;
    public GameObject placePrefab;     // 実体Prefab

    [Header("建築コスト")]
    public BuildingCost[] costs;
}

[System.Serializable]
public class BuildingCost
{
    public ItemData item;
    public int amount = 1;
}

public enum BuildingType
{
    SpaceModule,
    GravityModule,
    PartitionWall,
    Staircase,
}