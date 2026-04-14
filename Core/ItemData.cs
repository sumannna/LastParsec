using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("基本情報")]
    public string itemName = "アイテム";
    public Sprite icon;
    public int maxStack = 1;

    [Header("種類")]
    public ItemType itemType;
    public ItemCategory itemCategory;
}

public enum ItemType
{
    // 素材（0〜49）
    Material = 2,

    // 装備品（50〜）
    Spacesuit = 50,
    OxygenTank = 51,
    ThrusterTank = 52,

    // 道具（100〜）
    Tool = 100,

    // 消耗品（150〜）
    Consumable = 150,

    // ブループリント（200〜）
    Blueprint = 200,

    // 設置物（300〜）
    Placeable = 300,

    // 水タンク（400〜）
    WaterTank = 400,
}

public enum ItemCategory
{
    Material,
    Equipment,
    Tool,
    Consumable,
    Placeable,
    Blueprint,   // ← 追加
}