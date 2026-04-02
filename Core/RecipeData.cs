using System.Collections.Generic;
using UnityEngine;

// ワークベンチの要求レベル
public enum WBLevel
{
    None,   // 手クラフト（WB不要）
    Lv1,
    Lv2,
    Lv3
}

// レシピの習得経路
[System.Flags]
public enum LearnSource
{
    None = 0,
    Initial = 1,
    CraftTree = 2,
    ResearchTable = 4
}

// 素材1種のデータ（アイテム + 必要数）
[System.Serializable]
public class Ingredient
{
    public ItemData item;
    public int count;
}

[CreateAssetMenu(fileName = "NewRecipe", menuName = "LastParsec/RecipeData")]
public class RecipeData : ScriptableObject
{
    [Header("完成品")]
    public ItemData itemResult;          // 完成アイテム
    public int resultCount = 1;          // 1回のクラフトで完成する個数

    [Header("クラフト設定")]
    public float craftTime = 3f;         // クラフト時間（秒）
    public WBLevel requiredWBLevel = WBLevel.None; // 必要WBレベル

    [Header("素材")]
    public List<Ingredient> ingredients = new List<Ingredient>();

    [Header("習得設定")]
    public LearnSource learnSource = LearnSource.Initial;

    [Header("表示設定")]
    [TextArea]
    public string description;           // クラフト画面に表示する説明文

    // インベントリ内の素材が揃っているか確認する
    // inventory引数はInventoryコンポーネントを渡す想定
    // Inventory.GetAmount() を使用（GetItemCountは存在しない）
    public bool CanCraft(Inventory inventory)
    {
        foreach (var ingredient in ingredients)
        {
            if (inventory.GetAmount(ingredient.item) < ingredient.count)
                return false;
        }
        return true;
    }
}