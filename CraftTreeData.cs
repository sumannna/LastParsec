using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クラフトツリー1本分のデータ。
/// WBレベルごとに複数本存在する（例：Lv1に3本）。
/// </summary>
[CreateAssetMenu(fileName = "NewCraftTree", menuName = "LastParsec/CraftTreeData")]
public class CraftTreeData : ScriptableObject
{
    [Header("ツリー情報")]
    public string treeName;         // 表示名（例：「採掘系」）
    public WBLevel wbLevel;         // どのWBレベルに属するか
    public string treeId;           // 一意ID（例："wb1_mining"）

    [Header("ノード一覧")]
    public List<CraftTreeNode> nodes = new List<CraftTreeNode>();

    /// <summary>IDでノードを取得</summary>
    public CraftTreeNode GetNode(string nodeId)
    {
        return nodes.Find(n => n.nodeId == nodeId);
    }

    /// <summary>前提ノードが全て解放済みか確認</summary>
    public bool CanUnlock(CraftTreeNode node, RecipeKnowledgeManager km)
    {
        foreach (var prereqId in node.prerequisites)
        {
            CraftTreeNode prereq = GetNode(prereqId);
            if (prereq == null || !prereq.isUnlocked)
                return false;
        }
        return true;
    }
}

/// <summary>
/// ツリーの1ノード。
/// 前提ノード・解放コスト・解放済みレシピを保持する。
/// </summary>
[System.Serializable]
public class CraftTreeNode
{
    [Header("ノード情報")]
    public string nodeId;                           // 一意ID
    public string nodeName;                         // 表示名
    public Sprite nodeIcon;                         // ノードアイコン

    [Header("前提")]
    public List<string> prerequisites = new List<string>(); // 前提ノードID

    [Header("解放コスト")]
    public List<UnlockCost> unlockCosts = new List<UnlockCost>();

    [Header("解放されるレシピ")]
    public List<RecipeData> unlockedRecipes = new List<RecipeData>();

    [Header("状態（セーブ対象）")]
    public bool isUnlocked = false;

    /// <summary>インベントリのアイテムが解放コストを満たすか確認</summary>
    public bool CanAfford(Inventory inventory)
    {
        foreach (var cost in unlockCosts)
        {
            if (inventory.GetAmount(cost.item) < cost.count)
                return false;
        }
        return true;
    }
}

/// <summary>ノード解放に必要なアイテムと個数</summary>
[System.Serializable]
public class UnlockCost
{
    public ItemData item;
    public int count;
}