using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("基本情報")]
    public string itemName = "アイテム";
    public Sprite icon;
    public int maxStack = 10;

    [Header("種類")]
    public ItemType itemType;
    public ItemCategory itemCategory;
}

public enum ItemType
{
    // 素材・消耗品（0〜49）
    Oxygen = 0,
    Fuel = 1,
    Metal = 2,
    Stone = 3,
    Wood = 4,
    Organic = 5,
    Ice = 6,

    // 装備品（50〜）
    Spacesuit = 50,
    OxygenTank = 51,
    ThrusterTank = 52,

    // 道具（100〜）
    Tool = 100,

    // 消耗品（150〜）
    Consumable = 150,

    // ブループリント（200〜）← 追加
    Blueprint = 200,
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