using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeDatabase", menuName = "LastParsec/RecipeDatabase")]
public class RecipeDatabase : ScriptableObject
{
    [SerializeField]
    private List<RecipeData> recipes = new List<RecipeData>();

    // 全レシピを返す
    public IReadOnlyList<RecipeData> AllRecipes => recipes;

    // 特定のアイテムを作るレシピを返す（複数レシピが存在する場合を考慮）
    public RecipeData GetRecipeFor(ItemData item)
    {
        return recipes.Find(r => r.itemResult == item);
    }

    // WBレベルでフィルタリング
    public List<RecipeData> GetRecipesByWBLevel(WBLevel level)
    {
        return recipes.FindAll(r => r.requiredWBLevel == level);
    }

    // 習得経路でフィルタリング
    public List<RecipeData> GetRecipesByLearnSource(LearnSource source)
    {
        return recipes.FindAll(r => r.learnSource == source);
    }

#if UNITY_EDITOR
    // Editor上でレシピを追加（開発用）
    public void AddRecipe(RecipeData recipe)
    {
        if (!recipes.Contains(recipe))
            recipes.Add(recipe);
    }
#endif
}