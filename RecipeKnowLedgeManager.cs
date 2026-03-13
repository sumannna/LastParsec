using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 習得済みレシピを管理するシングルトン。
/// - ゲーム開始時にInitialレシピを自動登録
/// - CraftTree / ResearchTable 経由の解放もここを通す
/// - 習得状態はセーブ対象（現時点はPlayerPrefsで仮実装）
/// </summary>
public class RecipeKnowledgeManager : MonoBehaviour
{
    public static RecipeKnowledgeManager Instance { get; private set; }

    [Header("全レシピDB")]
    [SerializeField] private RecipeDatabase recipeDatabase;

    // 習得済みレシピのセット（実行時管理）
    private HashSet<RecipeData> knownRecipes = new HashSet<RecipeData>();

    // 解放済みノードID
    private HashSet<string> unlockedNodeIds = new HashSet<string>();

    // セーブ用キー（PlayerPrefs）
    private const string SaveKey = "KnownRecipes";

    // -----------------------------------------------
    // 初期化
    // -----------------------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadKnowledge();
        RegisterInitialRecipes();
    }

    // -----------------------------------------------
    // 公開API
    // -----------------------------------------------

    /// <summary>レシピを習得済みか確認する</summary>
    public bool IsKnown(RecipeData recipe)
    {
        return knownRecipes.Contains(recipe);
    }

    /// <summary>レシピを習得させる（CraftTree・ResearchTable経由で呼ぶ）</summary>
    public void Learn(RecipeData recipe)
    {
        if (recipe == null) return;
        if (knownRecipes.Add(recipe))
        {
            SaveKnowledge();
            Debug.Log($"[RecipeKnowledge] 習得: {recipe.name}");
        }
    }

    /// <summary>習得済みレシピの一覧を返す</summary>
    public IReadOnlyCollection<RecipeData> GetKnownRecipes()
    {
        return knownRecipes;
    }

    /// <summary>ノードが解放済みか確認</summary>
    public bool IsNodeUnlocked(string nodeId)
    {
        return unlockedNodeIds.Contains(nodeId);
    }

    /// <summary>ノードを解放済みにする</summary>
    public void UnlockNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return;

        if (unlockedNodeIds.Add(nodeId))
        {
            Debug.Log($"[RecipeKnowledge] ノード解放: {nodeId}");
        }
    }

    /// <summary>
    /// クラフト画面向け：習得済みレシピをCanCraft状態付きで返す
    /// </summary>
    public List<(RecipeData recipe, bool canCraft)> GetKnownRecipesWithCraftability(Inventory inventory)
    {
        var result = new List<(RecipeData, bool)>();
        foreach (var recipe in knownRecipes)
        {
            result.Add((recipe, recipe.CanCraft(inventory)));
        }
        return result;
    }

    // -----------------------------------------------
    // 内部処理
    // -----------------------------------------------

    /// <summary>learnSource == Initial のレシピを自動登録する</summary>
    private void RegisterInitialRecipes()
    {
        Debug.Log("[CraftUI] RefreshRecipeList 実行");
        if (recipeDatabase == null)
        {
            Debug.LogWarning("[RecipeKnowledge] RecipeDatabaseが未アサインです");
            return;
        }

        foreach (var recipe in recipeDatabase.AllRecipes)
        {
            if (recipe == null) continue;
            if ((recipe.learnSource & LearnSource.Initial) != 0)
                knownRecipes.Add(recipe);
        }
    }

    // -----------------------------------------------
    // セーブ / ロード（PlayerPrefs仮実装）
    // 本実装時はセーブシステムに差し替える
    // -----------------------------------------------

    private void SaveKnowledge()
    {
        // 習得済みレシピ名をカンマ区切りで保存
        var names = new List<string>();
        foreach (var recipe in knownRecipes)
            names.Add(recipe.name);

        PlayerPrefs.SetString(SaveKey, string.Join(",", names));
        PlayerPrefs.Save();
    }

    private void LoadKnowledge()
    {
        if (recipeDatabase == null) return;
        if (!PlayerPrefs.HasKey(SaveKey)) return;

        string saved = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(saved)) return;

        var savedNames = new HashSet<string>(saved.Split(','));

        foreach (var recipe in recipeDatabase.AllRecipes)
        {
            if (savedNames.Contains(recipe.name))
                knownRecipes.Add(recipe);
        }
    }

    // -----------------------------------------------
    // デバッグ用
    // -----------------------------------------------

#if UNITY_EDITOR
    [ContextMenu("全レシピを習得（デバッグ）")]
private void DebugLearnAll()
{
    Debug.Log("[RecipeKnowledge] DebugLearnAll 呼び出し");

    if (recipeDatabase == null)
    {
        Debug.Log("[RecipeKnowledge] recipeDatabase が null");
        return;
    }

    Debug.Log($"[RecipeKnowledge] 実行前 knownRecipes={knownRecipes.Count}, allRecipes={recipeDatabase.AllRecipes.Count}");

    foreach (var recipe in recipeDatabase.AllRecipes)
    {
        if (recipe == null) continue;
        knownRecipes.Add(recipe);
    }

    Debug.Log($"[RecipeKnowledge] 実行後 knownRecipes={knownRecipes.Count}");
    Debug.Log("[RecipeKnowledge] 全レシピを習得しました");
}

[ContextMenu("習得リセット（デバッグ）")]
private void DebugResetAll()
{
    Debug.Log($"[RecipeKnowledge] Reset前 knownRecipes={knownRecipes.Count}");

    knownRecipes.Clear();
    unlockedNodeIds.Clear();
    PlayerPrefs.DeleteKey(SaveKey);
    PlayerPrefs.DeleteKey("UnlockedNodes");

    Debug.Log($"[RecipeKnowledge] Clear後 knownRecipes={knownRecipes.Count}");

    RegisterInitialRecipes();

    Debug.Log($"[RecipeKnowledge] Initial再登録後 knownRecipes={knownRecipes.Count}");
    Debug.Log("[RecipeKnowledge] 習得リセット完了");
}
#endif
}