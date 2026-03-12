using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// WBのEキーで開くクラフトツリー画面。
/// - ノードをアイコンで表示（解放済み：通常表示、未解放：鍵アイコン）
/// - ノード間をラインで接続
/// - 右パネル：選択ノードの詳細・解放コスト・UNLOCKボタン
/// </summary>
public class CraftTreeUI : MonoBehaviour
{
    public static CraftTreeUI Instance { get; private set; }

    [Header("UI")]
    public GameObject treePanel;                // ツリー画面全体
    public Transform nodeAreaParent;            // ノードを配置する親
    public GameObject nodeSlotPrefab;           // ノード1枠のPrefab
    public GameObject lineRendererPrefab;       // ノード間接続ライン（UI Image）

    [Header("右パネル：詳細")]
    public Image detailIcon;                    // 選択ノードのアイコン
    public TextMeshProUGUI detailNodeName;      // ノード名
    public TextMeshProUGUI detailDescription;   // 解放されるレシピ一覧テキスト
    public Transform unlockCostParent;          // 解放コスト一覧の親
    public GameObject unlockCostSlotPrefab;     // コスト1行のPrefab
    public Button unlockButton;                 // UNLOCKボタン
    public TextMeshProUGUI unlockButtonText;    // ボタンテキスト
    public Button closeButton;                  // CLOSEボタン

    [Header("鍵アイコン")]
    public Sprite lockSprite;                   // 未解放ノードに表示する鍵画像

    [Header("参照")]
    public Inventory playerInventory;
    public OxygenSystem oxygenSystem;
    public VitalSystem vitalSystem;

    // 現在表示中のツリーとWB
    private CraftTreeData currentTreeData;
    private WorkbenchInteraction currentWorkbench;
    private CraftTreeNode selectedNode;

    // 生成したノードオブジェクトの管理
    private List<GameObject> nodeObjects = new List<GameObject>();
    private List<GameObject> lineObjects = new List<GameObject>();

    // ノード間隔設定
    private const float NodeSpacingX = 120f;
    private const float NodeSpacingY = 120f;

    public bool IsOpen { get; private set; }

    // -----------------------------------------------
    // 初期化
    // -----------------------------------------------

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (treePanel != null) treePanel.SetActive(false);
        if (unlockButton != null) unlockButton.onClick.AddListener(OnUnlockButtonPressed);
        if (closeButton != null) closeButton.onClick.AddListener(() => Close());
    }

    void Update()
    {
        if (!IsOpen) return;

        // Escキーで閉じる
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();

        // WBから離れたら強制終了
        if (currentWorkbench != null && !currentWorkbench.IsPlayerInRange())
            Close();
    }

    // -----------------------------------------------
    // 公開API
    // -----------------------------------------------

    /// <summary>WorkbenchInteractionから呼ぶ</summary>
    public void Open(WorkbenchInteraction workbench)
    {
        currentWorkbench = workbench;

        // WBレベルに対応するツリーデータを取得
        // 複数ツリーがある場合は最初の1本を表示（後でタブ切り替えを追加可能）
        currentTreeData = FindTreeData(workbench.wbLevel);

        if (currentTreeData == null)
        {
            Debug.LogWarning($"[CraftTreeUI] WBLevel:{workbench.wbLevel} のCraftTreeDataが見つかりません");
            return;
        }

        IsOpen = true;
        treePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        BuildTree();
        ClearDetail();
    }

    public void Close()
    {
        IsOpen = false;
        if (treePanel != null) treePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentWorkbench = null;
        currentTreeData = null;
        selectedNode = null;
        ClearNodes();
        ClearDetail();
    }

    // -----------------------------------------------
    // ツリーの描画
    // -----------------------------------------------

    void BuildTree()
    {
        ClearNodes();
        if (currentTreeData == null) return;

        // ノードをグリッド状に配置（簡易レイアウト）
        // 前提依存を深さ（列）と兄弟順（行）で配置
        var depthMap = BuildDepthMap();

        // 列ごとのノードリスト
        var columns = new Dictionary<int, List<CraftTreeNode>>();
        foreach (var kv in depthMap)
        {
            int depth = kv.Value;
            if (!columns.ContainsKey(depth))
                columns[depth] = new List<CraftTreeNode>();
            columns[depth].Add(kv.Key);
        }

        // 各ノードのUI位置を計算して生成
        var nodePositions = new Dictionary<string, Vector2>();

        foreach (var kv in columns)
        {
            int col = kv.Key;
            List<CraftTreeNode> colNodes = kv.Value;

            for (int row = 0; row < colNodes.Count; row++)
            {
                float x = col * NodeSpacingX;
                float y = -(row - (colNodes.Count - 1) / 2f) * NodeSpacingY;
                Vector2 pos = new Vector2(x, y);

                CraftTreeNode node = colNodes[row];
                nodePositions[node.nodeId] = pos;
                CreateNodeObject(node, pos);
            }
        }

        // 接続ラインを描画
        foreach (var node in currentTreeData.nodes)
        {
            foreach (var prereqId in node.prerequisites)
            {
                if (nodePositions.ContainsKey(node.nodeId) &&
                    nodePositions.ContainsKey(prereqId))
                {
                    CreateLine(nodePositions[prereqId], nodePositions[node.nodeId]);
                }
            }
        }
    }

    /// <summary>各ノードの深さ（列位置）を計算する</summary>
    Dictionary<CraftTreeNode, int> BuildDepthMap()
    {
        var depthMap = new Dictionary<CraftTreeNode, int>();

        foreach (var node in currentTreeData.nodes)
            CalculateDepth(node, depthMap);

        return depthMap;
    }

    int CalculateDepth(CraftTreeNode node, Dictionary<CraftTreeNode, int> depthMap)
    {
        if (depthMap.ContainsKey(node)) return depthMap[node];

        if (node.prerequisites.Count == 0)
        {
            depthMap[node] = 0;
            return 0;
        }

        int maxPrereqDepth = 0;
        foreach (var prereqId in node.prerequisites)
        {
            CraftTreeNode prereq = currentTreeData.GetNode(prereqId);
            if (prereq != null)
            {
                int d = CalculateDepth(prereq, depthMap);
                if (d > maxPrereqDepth) maxPrereqDepth = d;
            }
        }

        depthMap[node] = maxPrereqDepth + 1;
        return maxPrereqDepth + 1;
    }

    void CreateNodeObject(CraftTreeNode node, Vector2 position)
    {
        if (nodeSlotPrefab == null) return;

        GameObject obj = Instantiate(nodeSlotPrefab, nodeAreaParent);
        nodeObjects.Add(obj);

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = position;

        // アイコン設定
        Image icon = FindChild<Image>(obj, "NodeIcon");
        if (icon != null)
        {
            if (node.isUnlocked)
            {
                // 解放済み：アイテムアイコン表示
                icon.sprite = node.nodeIcon;
                icon.color = node.nodeIcon != null ? Color.white : Color.gray;
            }
            else
            {
                // 未解放：鍵アイコン表示
                icon.sprite = lockSprite;
                icon.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }

        // 解放済みは枠を強調（青枠）
        Image bg = obj.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = node.isUnlocked
                ? new Color(0.2f, 0.4f, 0.8f, 1f)   // 解放済み：青
                : new Color(0.2f, 0.2f, 0.2f, 1f);   // 未解放：暗い灰
        }

        // クリックで詳細表示
        CraftTreeNode capturedNode = node;
        Button btn = obj.GetComponent<Button>();
        if (btn == null) btn = obj.AddComponent<Button>();
        btn.onClick.AddListener(() => SelectNode(capturedNode));

        obj.name = $"Node_{node.nodeId}";
    }

    void CreateLine(Vector2 from, Vector2 to)
    {
        if (lineRendererPrefab == null) return;

        GameObject lineObj = Instantiate(lineRendererPrefab, nodeAreaParent);
        lineObjects.Add(lineObj);

        // lineObjをfromとtoの中点に配置し、幅をfromとtoの距離に設定
        RectTransform rt = lineObj.GetComponent<RectTransform>();
        if (rt == null) return;

        Vector2 dir = to - from;
        float dist = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = (from + to) / 2f;
        rt.sizeDelta = new Vector2(dist, 4f);  // 高さ4pxのライン
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);

        // ラインを前面に持ってくる（ノードの後ろに表示）
        lineObj.transform.SetAsFirstSibling();
    }

    void ClearNodes()
    {
        foreach (var obj in nodeObjects) Destroy(obj);
        foreach (var obj in lineObjects) Destroy(obj);
        nodeObjects.Clear();
        lineObjects.Clear();
    }

    // -----------------------------------------------
    // ノード詳細の描画
    // -----------------------------------------------

    void SelectNode(CraftTreeNode node)
    {
        selectedNode = node;
        ShowDetail(node);
    }

    void ShowDetail(CraftTreeNode node)
    {
        if (node == null) { ClearDetail(); return; }

        // アイコン
        if (detailIcon != null)
        {
            detailIcon.sprite = node.nodeIcon;
            detailIcon.color = node.nodeIcon != null ? Color.white : Color.clear;
        }

        // ノード名
        if (detailNodeName != null)
            detailNodeName.text = node.nodeName;

        // 解放されるレシピ一覧
        if (detailDescription != null)
        {
            var recipeNames = new System.Text.StringBuilder();
            recipeNames.AppendLine("解放されるレシピ：");
            foreach (var recipe in node.unlockedRecipes)
            {
                if (recipe != null)
                    recipeNames.AppendLine($"・{recipe.itemResult?.itemName}");
            }
            detailDescription.text = recipeNames.ToString();
        }

        // 解放コスト一覧
        if (unlockCostParent != null)
        {
            foreach (Transform child in unlockCostParent)
                Destroy(child.gameObject);

            foreach (var cost in node.unlockCosts)
            {
                if (unlockCostSlotPrefab == null) break;
                GameObject row = Instantiate(unlockCostSlotPrefab, unlockCostParent);

                int have = playerInventory.GetAmount(cost.item);
                bool enough = have >= cost.count;

                TextMeshProUGUI label = FindChild<TextMeshProUGUI>(row, "CostText");
                if (label != null)
                {
                    label.text = $"{cost.item.itemName}  {have} / {cost.count}";
                    label.color = enough ? Color.white : new Color(1f, 0.4f, 0.4f, 1f);
                }

                Image icon = FindChild<Image>(row, "CostIcon");
                if (icon != null)
                {
                    icon.sprite = cost.item.icon;
                    icon.color = cost.item.icon != null ? Color.white : Color.clear;
                }
            }
        }

        // UNLOCKボタンの状態更新
        UpdateUnlockButton(node);
    }

    void UpdateUnlockButton(CraftTreeNode node)
    {
        if (unlockButton == null) return;

        if (node.isUnlocked)
        {
            unlockButton.interactable = false;
            if (unlockButtonText != null) unlockButtonText.text = "解放済み";
            return;
        }

        bool prereqOk = currentTreeData.CanUnlock(node, RecipeKnowledgeManager.Instance);
        bool canAfford = node.CanAfford(playerInventory);

        unlockButton.interactable = prereqOk && canAfford;

        if (!prereqOk)
            if (unlockButtonText != null) unlockButtonText.text = "前提未解放";
            else if (!canAfford)
                if (unlockButtonText != null) unlockButtonText.text = "素材不足";
                else
                if (unlockButtonText != null) unlockButtonText.text = "UNLOCK";
    }

    void ClearDetail()
    {
        if (detailIcon != null) detailIcon.color = Color.clear;
        if (detailNodeName != null) detailNodeName.text = "";
        if (detailDescription != null) detailDescription.text = "";
        if (unlockCostParent != null)
            foreach (Transform child in unlockCostParent)
                Destroy(child.gameObject);
        if (unlockButton != null) unlockButton.interactable = false;
    }

    // -----------------------------------------------
    // UNLOCK処理
    // -----------------------------------------------

    void OnUnlockButtonPressed()
    {
        if (selectedNode == null || currentTreeData == null) return;
        if (selectedNode.isUnlocked) return;
        if (!currentTreeData.CanUnlock(selectedNode, RecipeKnowledgeManager.Instance)) return;
        if (!selectedNode.CanAfford(playerInventory)) return;

        // コスト消費
        foreach (var cost in selectedNode.unlockCosts)
        {
            var slots = playerInventory.GetSlots();
            int remaining = cost.count;
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i] == null || slots[i].item != cost.item) continue;
                int take = Mathf.Min(slots[i].amount, remaining);
                playerInventory.ReduceSlot(slots[i], take);
                remaining -= take;
            }
        }

        // ノード解放
        selectedNode.isUnlocked = true;

        // レシピを習得させる
        foreach (var recipe in selectedNode.unlockedRecipes)
        {
            if (recipe != null && RecipeKnowledgeManager.Instance != null)
                RecipeKnowledgeManager.Instance.Learn(recipe);
        }

        Debug.Log($"[CraftTreeUI] ノード解放：{selectedNode.nodeName}");

        // UI更新
        BuildTree();
        ShowDetail(selectedNode);
    }

    // -----------------------------------------------
    // ユーティリティ
    // -----------------------------------------------

    /// <summary>WBレベルに対応するCraftTreeDataをResourcesから検索</summary>
    CraftTreeData FindTreeData(WBLevel level)
    {
        // Resources/CraftTrees/ フォルダに置いたSOを全件検索
        var allTrees = Resources.LoadAll<CraftTreeData>("CraftTrees");
        foreach (var tree in allTrees)
            if (tree.wbLevel == level) return tree;
        return null;
    }

    T FindChild<T>(GameObject root, string name) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<T>();
        return null;
    }
}