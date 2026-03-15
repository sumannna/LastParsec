using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// WBのEキーで開くクラフトツリー画面。
/// - ノードをアイコンで表示（解放済み：通常表示、未解放：暗く表示）
/// - ノード間をラインで接続
/// - 右パネル：選択ノードの詳細・解放コスト・UNLOCKボタン
/// </summary>
public class CraftTreeUI : MonoBehaviour
{
    public static CraftTreeUI Instance { get; private set; }

    [Header("UI")]
    public GameObject treePanel;
    public Transform nodeAreaParent;
    public GameObject nodeSlotPrefab;
    public GameObject lineRendererPrefab;

    [Header("右パネル：詳細")]
    public Image detailIcon;
    public TextMeshProUGUI detailNodeName;
    public TextMeshProUGUI detailDescription;
    public Transform unlockCostParent;
    public GameObject unlockCostSlotPrefab;
    public Button unlockButton;
    public TextMeshProUGUI unlockButtonText;
    public Button closeButton;

    [Header("鍵アイコン")]
    public Sprite lockSprite;

    [Header("参照")]
    public Inventory playerInventory;
    public OxygenSystem oxygenSystem;
    public VitalSystem vitalSystem;

    private CraftTreeData currentTreeData;
    private WorkbenchInteraction currentWorkbench;
    private CraftTreeNode selectedNode;

    private readonly List<GameObject> nodeObjects = new List<GameObject>();
    private readonly List<GameObject> lineObjects = new List<GameObject>();

    private const float NodeSpacingX = 120f;
    private const float NodeSpacingY = 120f;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (treePanel != null)
            treePanel.SetActive(false);

        if (unlockButton != null)
            unlockButton.onClick.AddListener(OnUnlockButtonPressed);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    void Update()
    {
        if (!IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Close();

        if (currentWorkbench != null && !currentWorkbench.IsPlayerInRange())
            Close();

        if (Input.GetMouseButtonDown(0))
        {
            var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            eventData.position = Input.mousePosition;

            var results = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

            Debug.Log($"[CraftTreeUI] UI Raycast 件数: {results.Count}");

            for (int i = 0; i < results.Count; i++)
            {
                Debug.Log($"[CraftTreeUI] Hit[{i}] = {results[i].gameObject.name}");
            }
        }
    }

    /// <summary>WorkbenchInteraction から呼ばれる</summary>
    public void Open(WorkbenchInteraction workbench)
    {
        currentWorkbench = workbench;
        currentTreeData = FindTreeData(workbench.wbLevel);

        if (currentTreeData == null)
        {
            Debug.LogWarning($"[CraftTreeUI] WBLevel:{workbench.wbLevel} のCraftTreeDataが見つかりません");
            return;
        }

        IsOpen = true;

        if (treePanel != null)
            treePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        BuildTree();
        ClearDetail();
    }

    public void Close()
    {
        IsOpen = false;

        if (treePanel != null)
            treePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentWorkbench = null;
        currentTreeData = null;
        selectedNode = null;

        ClearNodes();
        ClearDetail();
    }

    void BuildTree()
    {
        ClearNodes();

        if (currentTreeData == null)
            return;

        var depthMap = BuildDepthMap();

        var columns = new Dictionary<int, List<CraftTreeNode>>();
        foreach (var kv in depthMap)
        {
            int depth = kv.Value;
            if (!columns.ContainsKey(depth))
                columns[depth] = new List<CraftTreeNode>();

            columns[depth].Add(kv.Key);
        }

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

        foreach (var node in currentTreeData.nodes)
        {
            if (node.prerequisites == null)
                continue;

            foreach (var prereqId in node.prerequisites)
            {
                if (nodePositions.ContainsKey(node.nodeId) && nodePositions.ContainsKey(prereqId))
                {
                    CreateLine(nodePositions[prereqId], nodePositions[node.nodeId]);
                }
            }
        }
    }

    Dictionary<CraftTreeNode, int> BuildDepthMap()
    {
        var depthMap = new Dictionary<CraftTreeNode, int>();

        foreach (var node in currentTreeData.nodes)
            CalculateDepth(node, depthMap);

        return depthMap;
    }

    int CalculateDepth(CraftTreeNode node, Dictionary<CraftTreeNode, int> depthMap)
    {
        if (depthMap.ContainsKey(node))
            return depthMap[node];

        if (node.prerequisites == null || node.prerequisites.Count == 0)
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
                if (d > maxPrereqDepth)
                    maxPrereqDepth = d;
            }
        }

        depthMap[node] = maxPrereqDepth + 1;
        return maxPrereqDepth + 1;
    }

    void CreateNodeObject(CraftTreeNode node, Vector2 position)
    {
        if (nodeSlotPrefab == null || nodeAreaParent == null)
            return;

        GameObject obj = Instantiate(nodeSlotPrefab, nodeAreaParent);
        obj.name = $"Node_{node.nodeId}";
        nodeObjects.Add(obj);

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = position;

        // ルートにraycastを受けるImageを必ず持たせる
        Image rootImage = obj.GetComponent<Image>();
        if (rootImage == null)
            rootImage = obj.AddComponent<Image>();

        rootImage.color = new Color(1f, 1f, 1f, 0.001f);
        rootImage.raycastTarget = true;

        // 子のGraphicは基本raycastを切る
        Graphic[] graphics = obj.GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            if (g.gameObject != obj)
                g.raycastTarget = false;
        }

        Image icon = FindChild<Image>(obj, "NodeIcon");
        Image lockOverlay = FindChild<Image>(obj, "LockOverlay");

        if (icon != null)
        {
            icon.sprite = node.nodeIcon;
            icon.color = node.nodeIcon != null ? Color.white : Color.clear;
        }

        bool isUnlocked = RecipeKnowledgeManager.Instance != null &&
          (RecipeKnowledgeManager.Instance.IsNodeUnlocked(node.nodeId)
           || IsNodeUnlockedByRecipeKnowledge(node));

        if (isUnlocked)
        {
            if (icon != null)
                icon.color = Color.white;

            if (lockOverlay != null)
                lockOverlay.gameObject.SetActive(false);
        }
        else if (ArePrerequisitesMet(node))
        {
            if (icon != null)
                icon.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            if (lockOverlay != null)
                lockOverlay.gameObject.SetActive(false);
        }
        else
        {
            if (icon != null)
                icon.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(true);
                if (lockSprite != null)
                    lockOverlay.sprite = lockSprite;
            }
        }

        Button btn = obj.GetComponent<Button>();
        if (btn == null)
            btn = obj.AddComponent<Button>();

        btn.targetGraphic = rootImage;
        btn.transition = Selectable.Transition.ColorTint;

        CraftTreeNode capturedNode = node;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            SelectNode(capturedNode);
        });
    }

    bool IsNodeUnlockedByRecipeKnowledge(CraftTreeNode node)
    {
        if (node.unlockedRecipes == null || node.unlockedRecipes.Count == 0)
        {
            Debug.Log($"[CraftTreeUI] node={node.nodeId} unlockedRecipesが空");
            return false;
        }
        if (RecipeKnowledgeManager.Instance == null) return false;

        foreach (var recipe in node.unlockedRecipes)
        {
            if (recipe == null) continue;
            bool isKnown = RecipeKnowledgeManager.Instance.IsKnown(recipe);
            Debug.Log($"[CraftTreeUI] node={node.nodeId} recipe={recipe.name} learnSource={recipe.learnSource} isKnown={isKnown}");

            if ((recipe.learnSource & LearnSource.Initial) != 0) continue;
            if (!isKnown) return false;
        }

        foreach (var recipe in node.unlockedRecipes)
        {
            if (recipe == null) continue;
            if ((recipe.learnSource & LearnSource.Initial) == 0)
            {
                Debug.Log($"[CraftTreeUI] node={node.nodeId} → true (CraftTree/ResearchTable経由レシピあり)");
                return true;
            }
        }

        Debug.Log($"[CraftTreeUI] node={node.nodeId} → false (Initial以外のレシピなし)");
        return false;
    }

    bool ArePrerequisitesMet(CraftTreeNode node)
    {
        if (node == null) return false;
        if (node.prerequisites == null || node.prerequisites.Count == 0) return true;

        foreach (var prereqId in node.prerequisites)
        {
            CraftTreeNode prereqNode = currentTreeData?.GetNode(prereqId);
            if (!IsNodeFullyUnlocked(prereqNode)) return false;
        }
        return true;
    }

    /// <summary>
    /// ノードが解放済みかつ、そのノードの前提も全て根本から繋がっているか再帰チェック
    /// </summary>
    bool IsNodeFullyUnlocked(CraftTreeNode node)
    {
        if (node == null) return false;
        if (RecipeKnowledgeManager.Instance == null) return false;

        // このノード自体が解放されているか
        bool unlocked = RecipeKnowledgeManager.Instance.IsNodeUnlocked(node.nodeId)
                     || IsNodeUnlockedByRecipeKnowledge(node);
        if (!unlocked) return false;

        // このノードの前提も全て解放されているか再帰チェック
        if (node.prerequisites == null || node.prerequisites.Count == 0) return true;

        foreach (var prereqId in node.prerequisites)
        {
            CraftTreeNode prereqNode = currentTreeData?.GetNode(prereqId);
            if (!IsNodeFullyUnlocked(prereqNode)) return false;
        }
        return true;
    }

    void CreateLine(Vector2 from, Vector2 to)
    {
        if (lineRendererPrefab == null || nodeAreaParent == null)
            return;

        GameObject lineObj = Instantiate(lineRendererPrefab, nodeAreaParent);
        lineObjects.Add(lineObj);

        RectTransform rt = lineObj.GetComponent<RectTransform>();
        if (rt == null)
            return;

        Vector2 dir = to - from;
        float dist = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = (from + to) / 2f;
        rt.sizeDelta = new Vector2(dist, 4f);
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);

        lineObj.transform.SetAsFirstSibling();
    }

    void ClearNodes()
    {
        foreach (var obj in nodeObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        foreach (var obj in lineObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        nodeObjects.Clear();
        lineObjects.Clear();
    }

    void SelectNode(CraftTreeNode node)
    {
        Debug.Log($"[CraftTreeUI] Node選択: {node.nodeId}");

        selectedNode = node;
        ShowDetail(node);
    }

    void ShowDetail(CraftTreeNode node)
    {
        if (node == null)
        {
            ClearDetail();
            return;
        }

        Debug.Log($"[CraftTreeUI] ShowDetail開始: {node.nodeName}");

        if (detailIcon != null)
        {
            detailIcon.sprite = node.nodeIcon;
            detailIcon.color = node.nodeIcon != null ? Color.white : Color.clear;
        }

        if (detailNodeName != null)
            detailNodeName.text = string.IsNullOrEmpty(node.nodeName) ? "(No Name)" : node.nodeName;

        if (detailDescription != null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("解放されるレシピ:");

            if (node.unlockedRecipes != null && node.unlockedRecipes.Count > 0)
            {
                foreach (var recipe in node.unlockedRecipes)
                {
                    if (recipe == null)
                    {
                        sb.AppendLine("・(null recipe)");
                        continue;
                    }

                    string recipeName = recipe.itemResult != null ? recipe.itemResult.itemName : recipe.name;
                    sb.AppendLine($"・{recipeName}");
                }
            }
            else
            {
                sb.AppendLine("・なし");
            }

            detailDescription.text = sb.ToString();
        }

        if (unlockCostParent != null)
        {
            foreach (Transform child in unlockCostParent)
                Destroy(child.gameObject);

            if (node.unlockCosts != null)
            {
                foreach (var cost in node.unlockCosts)
                {
                    if (unlockCostSlotPrefab == null)
                        break;

                    GameObject row = Instantiate(unlockCostSlotPrefab, unlockCostParent);

                    int have = 0;
                    bool enough = false;
                    string itemName = "(null item)";
                    Sprite itemIcon = null;

                    if (cost != null && cost.item != null)
                    {
                        itemName = cost.item.itemName;
                        itemIcon = cost.item.icon;

                        if (playerInventory != null)
                        {
                            have = playerInventory.GetAmount(cost.item);
                            enough = have >= cost.count;
                        }
                    }

                    TextMeshProUGUI label = FindChild<TextMeshProUGUI>(row, "CostText");
                    if (label != null)
                    {
                        int need = cost != null ? cost.count : 0;
                        label.text = $"{itemName}  {have} / {need}";
                        label.color = enough ? Color.white : new Color(1f, 0.4f, 0.4f, 1f);
                    }

                    Image costIcon = FindChild<Image>(row, "CostIcon");
                    if (costIcon != null)
                    {
                        costIcon.sprite = itemIcon;
                        costIcon.color = itemIcon != null ? Color.white : Color.clear;
                    }
                }
            }
        }

        UpdateUnlockButton(node);
    }

    void UpdateUnlockButton(CraftTreeNode node)
    {
        if (unlockButton == null)
            return;

        if (node == null)
        {
            unlockButton.interactable = false;
            if (unlockButtonText != null)
                unlockButtonText.text = "UNLOCK";
            return;
        }

        bool isUnlocked = RecipeKnowledgeManager.Instance != null &&
          (RecipeKnowledgeManager.Instance.IsNodeUnlocked(node.nodeId)
           || IsNodeUnlockedByRecipeKnowledge(node));

        if (isUnlocked)
        {
            unlockButton.interactable = false;
            if (unlockButtonText != null)
                unlockButtonText.text = "解放済み";
            return;
        }

        bool prereqOk = ArePrerequisitesMet(node);

        bool canAfford = playerInventory != null && node.CanAfford(playerInventory);

        unlockButton.interactable = prereqOk && canAfford;

        if (!prereqOk)
        {
            if (unlockButtonText != null)
                unlockButtonText.text = "前提未解放";
        }
        else if (!canAfford)
        {
            if (unlockButtonText != null)
                unlockButtonText.text = "素材不足";
        }
        else
        {
            if (unlockButtonText != null)
                unlockButtonText.text = "UNLOCK";
        }
    }

    void ClearDetail()
    {
        if (detailIcon != null)
        {
            detailIcon.sprite = null;
            detailIcon.color = Color.clear;
        }

        if (detailNodeName != null)
            detailNodeName.text = "";

        if (detailDescription != null)
            detailDescription.text = "";

        if (unlockCostParent != null)
        {
            foreach (Transform child in unlockCostParent)
                Destroy(child.gameObject);
        }

        if (unlockButton != null)
            unlockButton.interactable = false;

        if (unlockButtonText != null)
            unlockButtonText.text = "UNLOCK";
    }

    void OnUnlockButtonPressed()
    {
        if (selectedNode == null || currentTreeData == null)
            return;

        if (RecipeKnowledgeManager.Instance != null &&
        (RecipeKnowledgeManager.Instance.IsNodeUnlocked(selectedNode.nodeId)
        || IsNodeUnlockedByRecipeKnowledge(selectedNode)))
            return;

        if (!ArePrerequisitesMet(selectedNode))
            return;

        if (playerInventory == null || !selectedNode.CanAfford(playerInventory))
            return;

        foreach (var cost in selectedNode.unlockCosts)
        {
            if (cost == null || cost.item == null)
                continue;

            var slots = playerInventory.GetSlots();
            int remaining = cost.count;

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i] == null || slots[i].item != cost.item)
                    continue;

                int take = Mathf.Min(slots[i].amount, remaining);
                playerInventory.ReduceSlot(slots[i], take);
                remaining -= take;
            }
        }

        if (RecipeKnowledgeManager.Instance != null)
        {
            RecipeKnowledgeManager.Instance.UnlockNode(selectedNode.nodeId);
        }

        foreach (var recipe in selectedNode.unlockedRecipes)
        {
            if (recipe != null && RecipeKnowledgeManager.Instance != null)
                RecipeKnowledgeManager.Instance.Learn(recipe);
        }

        Debug.Log($"[CraftTreeUI] ノード解放: {selectedNode.nodeName}");

        BuildTree();
        ShowDetail(selectedNode);
    }

    CraftTreeData FindTreeData(WBLevel level)
    {
        var allTrees = Resources.LoadAll<CraftTreeData>("CraftTrees");
        foreach (var tree in allTrees)
        {
            if (tree.wbLevel == level)
                return tree;
        }

        return null;
    }

    T FindChild<T>(GameObject root, string name) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child.GetComponent<T>();
        }

        return null;
    }
}