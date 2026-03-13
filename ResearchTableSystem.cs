using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// リサーチテーブル（設置型）。
/// - Eキーでインベントリ＋リサーチUIを開く
/// - ブループリントをD&Dまたはダブルクリックでリサーチスロットにセット
/// - リサーチボタンで開始 → 完了時にブループリント＋追加コスト消費 → レシピ解放
/// - ウィンドウを閉じるとリサーチ強制終了（消費なし）
/// </summary>
public class ResearchTableSystem : MonoBehaviour
{
    public static ResearchTableSystem Instance { get; private set; }
    public bool IsOpen => isOpen;

    [Header("設定")]
    [SerializeField] private float interactRange = 1.5f;

    [Header("UI")]
    [SerializeField] private GameObject researchPanel;
    [SerializeField] private Button closeButton;

    [Header("リサーチスロット")]
    [SerializeField] private Image blueprintSlotIcon;
    [SerializeField] private TextMeshProUGUI blueprintSlotText;

    [Header("詳細表示")]
    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private TextMeshProUGUI researchTimeText;
    [SerializeField] private Transform costListParent;
    [SerializeField] private GameObject costSlotPrefab;

    [Header("リサーチ実行")]
    [SerializeField] private Button researchButton;
    [SerializeField] private TextMeshProUGUI researchButtonText;
    [SerializeField] private Slider progressBar;

    [Header("参照")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private InventoryUI inventoryUI;

    private bool isOpen = false;
    private BlueprintData currentBlueprint = null;
    private Coroutine researchCoroutine = null;
    private float currentProgress = 0f;

    public bool IsResearching => researchCoroutine != null;

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
        if (researchPanel != null) researchPanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (researchButton != null) researchButton.onClick.AddListener(OnResearchButtonPressed);
        if (progressBar != null) progressBar.value = 0f;
        ClearSlot();
    }

    // -----------------------------------------------
    // 毎フレーム
    // -----------------------------------------------

    void Update()
    {
        // 範囲外に出たら強制終了
        if (isOpen && !IsPlayerInRange())
        {
            ClosePanel();
            return;
        }

        // Eキーで開閉
        if (IsPlayerInRange() && Input.GetKeyDown(KeyCode.E))
        {
            if (isOpen) ClosePanel();
            else if (UIManager.Instance != null) UIManager.Instance.OpenResearchTable(this);
            else OpenPanel();
            return;
        }

        // Esc / Qキーで閉じる
        if (isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q)))
        {
            ClosePanel();
            return;
        }

        // プログレスバー更新
        if (isOpen && IsResearching && progressBar != null)
            progressBar.value = currentProgress;
    }

    // -----------------------------------------------
    // 開閉
    // -----------------------------------------------

    /// <summary>UIManager経由で外部から開く</summary>
    public void OpenPanelExternal()
    {
        OpenPanel();
    }

    void OpenPanel()
    {
        isOpen = true;
        researchPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // インベントリを同時に開く
        if (inventoryUI != null && !inventoryUI.IsOpen)
            inventoryUI.OpenInventoryExternal();

        UpdateUI();
    }

    public void ClosePanel()
    {
        if (IsResearching) CancelResearch();

        isOpen = false;
        researchPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.CloseInventory();
    }

    // -----------------------------------------------
    // ブループリントのセット
    // -----------------------------------------------

    /// <summary>
    /// インベントリからD&DまたはダブルクリックでBPをセットする際に呼ぶ。
    /// </summary>
    public bool TrySetBlueprint(Inventory.Slot slot)
    {
        Debug.Log("[ResearchTable] TrySetBlueprint 開始");

        if (slot == null || slot.item == null)
        {
            Debug.Log("[ResearchTable] slot または item が null");
            return false;
        }

        if (slot.item is not BlueprintData bp)
        {
            Debug.Log($"[ResearchTable] Blueprintではない item={slot.item.itemName}");
            return false;
        }

        if (IsResearching)
        {
            Debug.Log("[ResearchTable] 研究中のためセット不可");
            return false;
        }

        currentBlueprint = bp;
        Debug.Log($"[ResearchTable] currentBlueprint 設定={currentBlueprint.itemName}, iconNull={(currentBlueprint.icon == null)}");

        UpdateUI();
        return true;
    }

    public void ClearSlot()
    {
        if (IsResearching) return;
        currentBlueprint = null;
        UpdateUI();
    }

    // -----------------------------------------------
    // UI更新
    // -----------------------------------------------

    void UpdateUI()
    {
        bool hasBlueprint = currentBlueprint != null;

        // スロット表示
        if (blueprintSlotIcon != null)
        {
            blueprintSlotIcon.sprite = hasBlueprint ? currentBlueprint.icon : null;
            blueprintSlotIcon.color = hasBlueprint && currentBlueprint.icon != null
                ? Color.white : Color.clear;

            Debug.Log($"[ResearchTable] UpdateUI hasBlueprint={hasBlueprint}, iconNull={(hasBlueprint ? currentBlueprint.icon == null : true)}, color={blueprintSlotIcon.color}");
        }
        else
        {
            Debug.Log("[ResearchTable] blueprintSlotIcon が null");
        }

        if (blueprintSlotText != null)
            blueprintSlotText.text = hasBlueprint ? "" : "ここにBPをセット";

        if (!hasBlueprint)
        {
            ClearDetail();
            return;
        }

        // レシピ名
        if (recipeNameText != null)
            recipeNameText.text = $"解放：{currentBlueprint.targetRecipe?.itemResult?.itemName ?? "不明"}";

        // リサーチ時間
        if (researchTimeText != null)
            researchTimeText.text = $"リサーチ時間：{currentBlueprint.researchTime}秒";

        // コスト一覧を再描画
        if (costListParent != null)
        {
            foreach (Transform child in costListParent)
                Destroy(child.gameObject);

            // ブループリント自体のコスト行
            AddCostRow(currentBlueprint, 1);

            // 追加コスト
            foreach (var cost in currentBlueprint.researchCosts)
                AddCostRow(cost.item, cost.count);
        }

        UpdateResearchButton();
    }

    void AddCostRow(ItemData item, int count)
    {
        if (costSlotPrefab == null || item == null) return;

        GameObject row = Instantiate(costSlotPrefab, costListParent);
        int have = playerInventory.GetAmount(item);
        bool enough = have >= count;

        TextMeshProUGUI label = FindChild<TextMeshProUGUI>(row, "CostText");
        if (label != null)
        {
            label.text = $"{item.itemName}  {have} / {count}";
            label.color = enough ? Color.white : new Color(1f, 0.4f, 0.4f, 1f);
        }

        Image icon = FindChild<Image>(row, "CostIcon");
        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.color = item.icon != null ? Color.white : Color.clear;
        }
    }

    void UpdateResearchButton()
    {
        if (researchButton == null) return;

        if (currentBlueprint == null)
        {
            researchButton.interactable = false;
            if (researchButtonText != null) researchButtonText.text = "BPをセットしてください";
            return;
        }

        bool alreadyKnown = RecipeKnowledgeManager.Instance != null
                         && RecipeKnowledgeManager.Instance.IsKnown(currentBlueprint.targetRecipe);
        if (alreadyKnown)
        {
            researchButton.interactable = false;
            if (researchButtonText != null) researchButtonText.text = "習得済み";
            return;
        }

        bool canResearch = currentBlueprint.CanResearch(playerInventory);
        researchButton.interactable = canResearch && !IsResearching;
        if (researchButtonText != null)
            researchButtonText.text = canResearch ? "リサーチ開始" : "素材不足";
    }

    void ClearDetail()
    {
        if (recipeNameText != null) recipeNameText.text = "";
        if (researchTimeText != null) researchTimeText.text = "";
        if (costListParent != null)
            foreach (Transform child in costListParent)
                Destroy(child.gameObject);
        if (researchButton != null) researchButton.interactable = false;
        if (researchButtonText != null) researchButtonText.text = "BPをセットしてください";
        if (progressBar != null) progressBar.value = 0f;
    }

    // -----------------------------------------------
    // リサーチ処理
    // -----------------------------------------------

    void OnResearchButtonPressed()
    {
        if (currentBlueprint == null) return;
        if (IsResearching) return;
        if (!currentBlueprint.CanResearch(playerInventory)) return;

        researchCoroutine = StartCoroutine(ResearchRoutine());
    }

    IEnumerator ResearchRoutine()
    {
        float elapsed = 0f;
        float total = currentBlueprint.researchTime;

        if (researchButtonText != null) researchButtonText.text = "リサーチ中...";
        if (researchButton != null) researchButton.interactable = false;

        while (elapsed < total)
        {
            elapsed += Time.deltaTime;
            currentProgress = elapsed / total;
            yield return null;
        }

        CompleteResearch();
    }

    void CompleteResearch()
    {
        researchCoroutine = null;
        currentProgress = 0f;

        var slots = playerInventory.GetSlots();

        // ブループリント消費
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || slots[i].item != currentBlueprint) continue;
            playerInventory.ReduceSlot(slots[i], 1);
            break;
        }

        // 追加コスト消費
        foreach (var cost in currentBlueprint.researchCosts)
        {
            int remaining = cost.count;
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i] == null || slots[i].item != cost.item) continue;
                int take = Mathf.Min(slots[i].amount, remaining);
                playerInventory.ReduceSlot(slots[i], take);
                remaining -= take;
            }
        }

        // レシピ解放
        if (RecipeKnowledgeManager.Instance != null)
            RecipeKnowledgeManager.Instance.Learn(currentBlueprint.targetRecipe);

        Debug.Log($"[ResearchTable] 完了：{currentBlueprint.targetRecipe?.itemResult?.itemName}");

        currentBlueprint = null;
        if (progressBar != null) progressBar.value = 0f;
        UpdateUI();
    }

    void CancelResearch()
    {
        if (researchCoroutine != null)
        {
            StopCoroutine(researchCoroutine);
            researchCoroutine = null;
        }

        currentProgress = 0f;
        if (progressBar != null) progressBar.value = 0f;
        Debug.Log("[ResearchTable] キャンセル（消費なし）");
        UpdateUI();
    }

    // -----------------------------------------------
    // 距離判定
    // -----------------------------------------------

    bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        return Vector3.Distance(playerTransform.position, transform.position) <= interactRange;
    }

    // -----------------------------------------------
    // ユーティリティ
    // -----------------------------------------------

    T FindChild<T>(GameObject root, string name) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<T>();
        return null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}