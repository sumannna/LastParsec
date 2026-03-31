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
    [SerializeField] private Image progressBar;
    [SerializeField] private GameObject progressBarBG;
    [SerializeField] private TextMeshProUGUI remainingTimeText;

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
    }

    void Start()
    {
        if (researchPanel != null) researchPanel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (researchButton != null) researchButton.onClick.AddListener(OnResearchButtonPressed);
        if (progressBarBG != null) progressBarBG.SetActive(false);
        if (remainingTimeText != null) remainingTimeText.gameObject.SetActive(false);
        currentBlueprint = null;
        UpdateUI();
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
            if (ChestUI.Instance != null && (ChestUI.Instance.IsOpen || ChestUI.Instance.ClosedThisFrame)) return;
            if (isOpen) ClosePanel();
            else if (UIManager.Instance == null || !UIManager.Instance.IsAnyUIOpen())
            {
                if (UIManager.Instance != null) UIManager.Instance.OpenResearchTable(this);
                else OpenPanel();
            }
            return;
        }

        // Esc / Qキーで閉じる
        if (isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q)))
        {
            ClosePanel();
            return;
        }

        if (isOpen && IsResearching)
        {
            if (progressBar != null)
                progressBar.rectTransform.localScale = new Vector3(currentProgress, 1f, 1f);

            if (remainingTimeText != null && currentBlueprint != null)
            {
                float remaining = currentBlueprint.researchTime * (1f - currentProgress);
                remainingTimeText.text = $"{remaining:F1}";
            }
        }
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
        Debug.Log($"[ResearchTable] OpenPanel obj={gameObject.name} id={GetInstanceID()} current={(currentBlueprint != null ? currentBlueprint.itemName : "null")}");

        var dropHandler = blueprintSlotIcon != null
            ? blueprintSlotIcon.GetComponentInParent<ResearchBlueprintHandler>()
            : null;

        if (dropHandler != null)
            dropHandler.SetResearchTable(this);

        // ブループリントスロットにD&D・ダブルクリック用ハンドラを付与
        SetupBlueprintSlotHandlers();

        isOpen = true;
        researchPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (inventoryUI != null && !inventoryUI.IsOpen)
            inventoryUI.OpenInventoryExternal();

        UpdateUI();
    }

    void SetupBlueprintSlotHandlers()
    {
        if (researchPanel == null) return;

        // ResearchPanel以下を広く検索
        ResearchBlueprintHandler rbh = researchPanel.GetComponentInChildren<ResearchBlueprintHandler>(true);
        if (rbh != null)
        {
            rbh.SetResearchTable(this);
            Debug.Log($"[ResearchTable] SetResearchTable 完了: {rbh.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[ResearchTable] ResearchBlueprintHandler が見つかりません");
            return;
        }

        // ダブルクリックのみ追加（BlueprintSlotDragHandlerは削除）
        // ダブルクリックはResearchBlueprintHandlerのGameObjectに追加
        GameObject slotObj = rbh.gameObject;
        if (slotObj.GetComponent<BlueprintSlotClickHandler>() == null)
        {
            var clickHandler = slotObj.AddComponent<BlueprintSlotClickHandler>();
            clickHandler.researchTable = this;
            clickHandler.inventoryUI = inventoryUI;
        }

        // D&D送り出しはblueprintSlotIcon（子）に追加（DropHandlerと分離）
        if (blueprintSlotIcon != null)
        {
            GameObject iconObj = blueprintSlotIcon.gameObject;
            if (iconObj.GetComponent<BlueprintSlotDragHandler>() == null)
            {
                var dragHandler = iconObj.AddComponent<BlueprintSlotDragHandler>();
                dragHandler.researchTable = this;
                dragHandler.inventory = playerInventory;
                dragHandler.inventoryUI = inventoryUI;
            }
        }
    }

    public void ClosePanel()
    {
        Debug.Log($"[ResearchTable] ClosePanel obj={gameObject.name} id={GetInstanceID()} before current={(currentBlueprint != null ? currentBlueprint.itemName : "null")}");

        if (IsResearching) CancelResearch();

        isOpen = false;
        researchPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.CloseInventory();

        Debug.Log($"[ResearchTable] ClosePanel obj={gameObject.name} id={GetInstanceID()} after current={(currentBlueprint != null ? currentBlueprint.itemName : "null")}");
    }

    // -----------------------------------------------
    // ブループリントのセット
    // -----------------------------------------------

    /// <summary>
    /// インベントリからD&DまたはダブルクリックでBPをセットする際に呼ぶ。
    /// </summary>
    public bool TrySetBlueprint(Inventory.Slot slot)
    {
        Debug.Log($"[ResearchTable] TrySetBlueprint obj={gameObject.name} id={GetInstanceID()} current={(currentBlueprint != null ? currentBlueprint.itemName : "null")}");

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

        Debug.Log($"[ResearchTable] セット候補={bp.itemName}, slotAmountBefore={slot.amount}");

        if (IsResearching)
        {
            Debug.Log("[ResearchTable] 研究中のためセット不可");
            return false;
        }

        if (currentBlueprint != null)
        {
            Debug.Log($"[ResearchTable] 既存BP返却開始 old={currentBlueprint.itemName}");
            bool returned = playerInventory.AddItem(currentBlueprint);
            Debug.Log($"[ResearchTable] 既存BP返却結果 returned={returned}");

            if (!returned)
            {
                Debug.Log("[ResearchTable] 返却失敗で中断");
                return false;
            }
        }

        currentBlueprint = bp;
        playerInventory.ReduceSlot(slot, 1);

        Debug.Log($"[ResearchTable] 新規セット完了 obj={gameObject.name} id={GetInstanceID()} current={currentBlueprint.itemName}, slotAmountAfter={slot.amount}");

        StartCoroutine(DelayedRefresh());

        UpdateUI();
        return true;
    }

    private System.Collections.IEnumerator DelayedRefresh()
    {
        yield return null;
        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.RefreshAll();
    }


    // -----------------------------------------------
    // UI更新
    // -----------------------------------------------

    void UpdateUI()
    {
        bool hasBlueprint = currentBlueprint != null;
        Debug.Log($"[ResearchTable] UpdateUI hasBlueprint={hasBlueprint}, current={(currentBlueprint != null ? currentBlueprint.itemName : "null")}");
        // スロット表示
        if (blueprintSlotIcon != null)
        {
            blueprintSlotIcon.sprite = hasBlueprint ? currentBlueprint.icon : null;
            blueprintSlotIcon.color = hasBlueprint && currentBlueprint.icon != null
                ? Color.white : Color.clear;
        }

        if (blueprintSlotText != null)
        {
            blueprintSlotText.text = hasBlueprint ? currentBlueprint.itemName : "ここにBPをセット";

        }

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

            // 追加コストのみ表示
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

        bool canResearch = CanResearchWithInsertedBlueprint();
        Debug.Log($"[ResearchTable] UpdateResearchButton: canResearch={canResearch} IsResearching={IsResearching}");

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
        if (progressBar != null)
            progressBar.rectTransform.sizeDelta = new Vector2(0f, progressBar.rectTransform.sizeDelta.y);
    }

    // -----------------------------------------------
    // リサーチ処理
    // -----------------------------------------------

    void OnResearchButtonPressed()
    {
        if (currentBlueprint == null) return;
        if (IsResearching) return;
        if (!CanResearchWithInsertedBlueprint()) return;

        researchCoroutine = StartCoroutine(ResearchRoutine());
    }

    IEnumerator ResearchRoutine()
    {
        float elapsed = 0f;
        float total = currentBlueprint.researchTime;

        if (researchButtonText != null) researchButtonText.text = "リサーチ中...";
        if (researchButton != null) researchButton.interactable = false;
        if (progressBarBG != null) progressBarBG.SetActive(true);
        if (remainingTimeText != null) remainingTimeText.gameObject.SetActive(true);

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

        if (progressBar != null)
            progressBar.rectTransform.localScale = new Vector3(0f, 1f, 1f);

        if (progressBar != null)
            progressBar.rectTransform.localScale = new Vector3(0f, 1f, 1f);
        if (progressBarBG != null)
            progressBarBG.SetActive(false);
        if (remainingTimeText != null)
            remainingTimeText.gameObject.SetActive(false);
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

        if (progressBar != null)
            progressBar.rectTransform.localScale = new Vector3(0f, 1f, 1f);

        if (progressBar != null)
            progressBar.rectTransform.localScale = new Vector3(0f, 1f, 1f);
        if (progressBarBG != null)
            progressBarBG.SetActive(false);
        if (remainingTimeText != null)
            remainingTimeText.gameObject.SetActive(false);
        UpdateUI();
    }

    bool CanResearchWithInsertedBlueprint()
    {
        if (currentBlueprint == null)
        {
            Debug.Log("[ResearchTable] CanResearch: blueprint null");
            return false;
        }

        foreach (var cost in currentBlueprint.researchCosts)
        {
            int have = playerInventory.GetAmount(cost.item);
            Debug.Log($"[ResearchTable] CanResearch: item={cost.item?.itemName} need={cost.count} have={have}");
            if (have < cost.count)
            {
                Debug.Log($"[ResearchTable] CanResearch: 不足 → false");
                return false;
            }
        }

        Debug.Log("[ResearchTable] CanResearch: → true");
        return true;
    }

    public bool TryTakeBlueprint()
    {
        if (currentBlueprint == null)
        {
            Debug.Log("[ResearchTable] 回収対象なし");
            return false;
        }

        if (IsResearching)
        {
            Debug.Log("[ResearchTable] 研究中のため回収不可");
            return false;
        }

        bool added = playerInventory.AddItem(currentBlueprint);
        if (!added)
        {
            Debug.Log("[ResearchTable] インベントリ満杯で回収不可");
            return false;
        }

        Debug.Log($"[ResearchTable] ブループリント回収={currentBlueprint.itemName}");

        currentBlueprint = null;

        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.RefreshAll();

        UpdateUI();
        return true;
    }

    public bool TryGetCurrentBlueprintName(out string name)
    {
        if (currentBlueprint == null)
        {
            name = null;
            return false;
        }

        name = currentBlueprint.itemName;
        return true;
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