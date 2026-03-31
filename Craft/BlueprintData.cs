using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ブループリントアイテムのデータ。
/// リサーチテーブルに納品することでレシピを解放する。
/// </summary>
[CreateAssetMenu(fileName = "NewBlueprint", menuName = "LastParsec/BlueprintData")]
public class BlueprintData : ItemData
{
    [Header("リサーチ設定")]
    public RecipeData targetRecipe;                         // 解放されるレシピ
    public float researchTime = 10f;                        // リサーチ時間（秒）

    [Header("追加コスト（ブループリント以外に必要な素材）")]
    public List<Ingredient> researchCosts = new List<Ingredient>();

    /// <summary>ブループリント＋追加コストが揃っているか確認</summary>
    public bool CanResearch(Inventory inventory)
    {
        // ブループリント自体の所持確認
        if (inventory.GetAmount(this) < 1) return false;

        // 追加コストの確認
        foreach (var cost in researchCosts)
        {
            if (inventory.GetAmount(cost.item) < cost.count)
                return false;
        }
        return true;
    }
}