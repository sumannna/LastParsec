using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Qキーで開くクラフト画面。
/// - 習得済みレシピ一覧を表示
/// - 素材十分：不透明 / 素材不足：半透明
/// - クラフト中は採掘不可（IsCraftingをToolUserが参照）
/// - 他ウィンドウを開いた時・Q/Escキーで強制終了
/// </summary>
public class CraftUI : MonoBehaviour
{
    public static CraftUI Instance { get; private set; }

    [Header("UI")]
    public GameObject craftPanel;               // クラフト画面全体
    public Transform recipeListParent;          // レシピアイコンを並べる親
    public GameObject recipeSlotPrefab;         // レシピ1枠のPrefab

    [Header("詳細パネル")]
    public Image detailIcon;                    // 選択レシピのアイコン
    public TextMeshProUGUI detailName;          // アイテム名
    public TextMeshProUGUI detailDescription;   // 説明文
    public Transform ingredientListParent;      // 素材リスト親
    public GameObject ingredientSlotPrefab;     // 素材1行Prefab
    public TextMeshProUGUI craftTimeText;       // クラフト時間表示
    public TextMeshProUGUI wbLevelText;         // 必要ワークベンチ

    [Header("クラフト実行")]
    public Image progressBarImage;          // FillAmount方式のImage
    public TextMeshProUGUI remainingTimeText; // 残り秒数テキスト
    public GameObject progressBarRoot;      // ゲージ全体の親（表示切替用）
    public Button craftButton;
    public Button cancelButton;
    public TMP_InputField countInput;

    [Header("参照")]
    public Inventory playerInventory;
    public InventoryUI inventoryUI;
    public OxygenSystem oxygenSystem;
    public VitalSystem vitalSystem;

    [Header("個数操作")]
    public Button incrementButton;   // 右矢印（+1）
    public Button decrementButton;   // 左矢印（-1）
    public Button maxButton;         // MAX

    private bool isOpen = false;
    private RecipeData selectedRecipe = null;
    private List<GameObject> recipeSlotObjects = new List<GameObject>();
    private int craftCount = 1;

    public bool IsOpen => isOpen;

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
        craftPanel.SetActive(false);

        if (incrementButton != null)
            incrementButton.onClick.AddListener(OnIncrementPressed);
        if (decrementButton != null)
            decrementButton.onClick.AddListener(OnDecrementPressed);
        if (maxButton != null)
            maxButton.onClick.AddListener(OnMaxPressed);

        if (craftButton != null)
            craftButton.onClick.AddListener(OnCraftButtonPressed);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonPressed);
        if (countInput != null)
        {
            countInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            countInput.text = "1";
            countInput.onEndEdit.AddListener(OnCountInputChanged);
        }
        SetProgressVisible(false);
    }

    // -----------------------------------------------
    // 毎フレーム
    // -----------------------------------------------

    void Update()
    {
        // 死亡中はクラフト不可
        bool isDead = (oxygenSystem != null && oxygenSystem.IsGameOver)
                   || (vitalSystem != null && vitalSystem.IsDead);
        if (isDead)
        {
            if (isOpen) CloseCraft(cancel: true);
            return;
        }

        // Qキーで開閉
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (isOpen) CloseCraft(cancel: true);
            else if (UIManager.Instance != null) UIManager.Instance.OpenCraft();
            else OpenCraft();
            return;
        }

        // Escキーで強制終了
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCraft(cancel: true);
            return;
        }

        // Tabキー → クラフト画面を閉じてインベントリへ
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isOpen) CloseCraft(cancel: true);
            return;
        }

        // クラフト中のプログレスバー更新
        if (isOpen && CraftSystem.Instance != null && CraftSystem.Instance.IsCrafting)
        {
            float progress = CraftSystem.Instance.Progress;
            if (progressBarImage != null)
                progressBarImage.rectTransform.localScale = new Vector3(progress, 1f, 1f);
            if (remainingTimeText != null && CraftSystem.Instance.CurrentRecipe != null)
            {
                float remaining = CraftSystem.Instance.CurrentRecipe.craftTime * (1f - progress);
                remainingTimeText.text = $"{remaining:F1}";
            }
        }

        // WB距離が変わる可能性があるため毎フレームボタン状態を更新
        if (isOpen && !CraftSystem.Instance.IsCrafting)
            UpdateCraftButton();
    }

    // -----------------------------------------------
    // 開閉
    // -----------------------------------------------

    /// <summary>UIManager経由で外部から開く</summary>
    public void OpenCraftExternal()
    {
        OpenCraft();
    }

    void OpenCraft()
    {
        // インベントリが開いていれば閉じる
        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.CloseInventory();

        isOpen = true;
        craftPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshRecipeList();
    }

    /// <summary>
    /// 外部から呼び出し可能なクローズ。
    /// cancel=true のとき進行中クラフトを強制終了する。
    /// </summary>
    public void CloseCraft(bool cancel = true)
    {
        if (cancel && CraftSystem.Instance != null)
            CraftSystem.Instance.CancelCraft();

        isOpen = false;
        craftPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        selectedRecipe = null;
        ResetCraftCount();
        ClearDetail();
    }

    // -----------------------------------------------
    // レシピ一覧の描画
    // -----------------------------------------------

    public void RefreshRecipeList()
    {
        foreach (Transform child in recipeListParent)
        {
            Destroy(child.gameObject);
        }

        recipeSlotObjects.Clear();

        Debug.Log($"[CraftUI] 削除予約後 childCount={recipeListParent.childCount}, tracked={recipeSlotObjects.Count}");

        if (RecipeKnowledgeManager.Instance == null) return;

        var recipeList = RecipeKnowledgeManager.Instance
            .GetKnownRecipesWithCraftability(playerInventory);

        Debug.Log($"[CraftUI] recipeList件数={recipeList.Count}");

        foreach (var (recipe, canCraft) in recipeList)
        {
            Debug.Log($"[CraftUI] 生成 recipe={recipe.name}");

            GameObject slotObj = Instantiate(recipeSlotPrefab, recipeListParent);
            recipeSlotObjects.Add(slotObj);

            RectTransform rt = slotObj.GetComponent<RectTransform>();
            Debug.Log($"[CraftUI] 生成後 childCount={recipeListParent.childCount}, tracked={recipeSlotObjects.Count}, name={slotObj.name}, anchoredPos={rt.anchoredPosition}, localPos={rt.localPosition}");

            // アイコン
            Image icon = FindChild<Image>(slotObj, "ItemIcon");
            if (icon != null && recipe.itemResult != null)
            {
                icon.sprite = recipe.itemResult.icon;
                icon.color = canCraft ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            }

            // クリックで詳細表示
            RecipeData capturedRecipe = recipe;
            Button btn = slotObj.GetComponent<Button>();
            if (btn == null) btn = slotObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectRecipe(capturedRecipe));
        }
    }

    // -----------------------------------------------
    // レシピ詳細の描画
    // -----------------------------------------------

    void SelectRecipe(RecipeData recipe)
    {
        selectedRecipe = recipe;
        ResetCraftCount();
        ShowDetail(recipe);
    }

    void ShowDetail(RecipeData recipe)
    {
        if (recipe == null) { ClearDetail(); return; }

        if (detailIcon != null)
        {
            detailIcon.sprite = recipe.itemResult?.icon;
            detailIcon.color = recipe.itemResult?.icon != null ? Color.white : Color.clear;
        }
        if (detailName != null)
            detailName.text = recipe.itemResult?.itemName ?? "";
        if (detailDescription != null)
            detailDescription.text = recipe.description;
        if (craftTimeText != null)
            craftTimeText.text = $"クラフト時間：{recipe.craftTime}秒";

        // 素材リスト
        if (ingredientListParent != null)
        {
            foreach (Transform child in ingredientListParent)
                Destroy(child.gameObject);

            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredientSlotPrefab == null) break;
                GameObject row = Instantiate(ingredientSlotPrefab, ingredientListParent);

                // 所持数 / 必要数
                int have = playerInventory.GetAmount(ingredient.item);
                bool enough = have >= ingredient.count;

                TextMeshProUGUI label = FindChild<TextMeshProUGUI>(row, "IngredientText");
                if (label != null)
                {
                    label.text = $"{ingredient.item.itemName}  {have} / {ingredient.count}";
                    label.color = enough ? Color.white : new Color(1f, 0.4f, 0.4f, 1f);
                }

                Image icon = FindChild<Image>(row, "IngredientIcon");
                if (icon != null)
                {
                    icon.sprite = ingredient.item.icon;
                    icon.color = ingredient.item.icon != null ? Color.white : Color.clear;
                }
            }
        }

        // WB必須表示
        if (wbLevelText != null)
        {
            if (recipe.requiredWBLevel != WBLevel.None)
                wbLevelText.text = $"必要ワークベンチ：Lv{(int)recipe.requiredWBLevel}";
            else
                wbLevelText.text = "";
        }

        UpdateCraftButton();
    }

    void ClearDetail()
    {
        if (detailIcon != null) detailIcon.color = Color.clear;
        if (detailName != null) detailName.text = "";
        if (detailDescription != null) detailDescription.text = "";
        if (craftTimeText != null) craftTimeText.text = "";
        if (wbLevelText != null) wbLevelText.text = "";
        if (ingredientListParent != null)
            foreach (Transform child in ingredientListParent)
                Destroy(child.gameObject);
        UpdateCraftButton();
    }

    void UpdateCraftButton()
    {
        if (craftButton == null) return;

        bool wbOk = true;
        if (selectedRecipe != null && selectedRecipe.requiredWBLevel != WBLevel.None)
        {
            wbOk = false;
            var allWB = FindObjectsOfType<WorkbenchInteraction>();
            Debug.Log($"[CraftUI] WBチェック: requiredLevel={selectedRecipe.requiredWBLevel} WB数={allWB.Length}");
            foreach (var wb in allWB)
            {
                bool inRange = wb.IsPlayerInRange();
                Debug.Log($"[CraftUI] WB={wb.gameObject.name} level={wb.wbLevel} inRange={inRange}");
                if (wb != null && wb.wbLevel == selectedRecipe.requiredWBLevel && inRange)
                {
                    wbOk = true;
                    break;
                }
            }
            Debug.Log($"[CraftUI] wbOk={wbOk}");
        }

        bool recipeCraft = selectedRecipe != null && selectedRecipe.CanCraft(playerInventory);
        bool notCrafting = !CraftSystem.Instance.IsCrafting;
        Debug.Log($"[CraftUI] canCraft判定: selectedRecipe={selectedRecipe != null} recipeCraft={recipeCraft} notCrafting={notCrafting} wbOk={wbOk}");

        bool canCraft = selectedRecipe != null
                     && recipeCraft
                     && notCrafting
                     && wbOk;

        craftButton.interactable = canCraft;
        Debug.Log($"[CraftUI] craftButton.interactable={craftButton.interactable} canCraft={canCraft}");
        if (cancelButton != null)
            cancelButton.interactable = CraftSystem.Instance != null
                                     && CraftSystem.Instance.IsCrafting;
    }

    // -----------------------------------------------
    // ボタン処理
    // -----------------------------------------------

    void OnCraftButtonPressed()
    {
        if (selectedRecipe == null) return;
        if (CraftSystem.Instance == null) return;

        Debug.Log($"[CraftUI] OnCraftButtonPressed: recipe={selectedRecipe.name} craftCount={craftCount} requiredWB={selectedRecipe.requiredWBLevel}");

        WorkbenchInteraction nearbyWB = null;
        if (selectedRecipe.requiredWBLevel != WBLevel.None)
        {
            foreach (var wb in FindObjectsOfType<WorkbenchInteraction>())
            {
                if (wb != null && wb.wbLevel == selectedRecipe.requiredWBLevel && wb.IsPlayerInRange())
                {
                    nearbyWB = wb;
                    break;
                }
            }
        }

        bool started = CraftSystem.Instance.StartCraft(selectedRecipe, craftCount, nearbyWB);

        Debug.Log($"[CraftUI] StartCraft result={started}");
        if (started)
        {
            SetProgressVisible(true);
            UpdateCraftButton();

            CraftSystem.Instance.OnCraftFinished += OnCraftFinished;
            CraftSystem.Instance.OnCraftCancelled += OnCraftCancelled;
            CraftSystem.Instance.OnCraftCompleted += OnCraftUnitCompleted;
        }
    }

    void OnCraftUnitCompleted(RecipeData recipe)
    {
        // 素材の所持数表示を更新
        if (selectedRecipe != null) ShowDetail(selectedRecipe);
    }

    void OnCancelButtonPressed()
    {
        if (CraftSystem.Instance != null)
            CraftSystem.Instance.CancelCraft();
    }

    void OnCountInputChanged(string value)
    {
        if (!int.TryParse(value, out int parsed) || parsed < 1)
            parsed = 1;
        craftCount = Mathf.Clamp(parsed, 1, GetMaxCraftCount());
        if (countInput != null) countInput.text = craftCount.ToString();
    }

    void OnCraftFinished()
    {
        SetProgressVisible(false);
        UnsubscribeCraftEvents();
        RefreshRecipeList();
        if (selectedRecipe != null) ShowDetail(selectedRecipe);
    }

    void OnCraftCancelled()
    {
        SetProgressVisible(false);
        UnsubscribeCraftEvents();
        UpdateCraftButton();
    }

    void UnsubscribeCraftEvents()
    {
        if (CraftSystem.Instance == null) return;
        CraftSystem.Instance.OnCraftFinished -= OnCraftFinished;
        CraftSystem.Instance.OnCraftCancelled -= OnCraftCancelled;
        CraftSystem.Instance.OnCraftCompleted -= OnCraftUnitCompleted;
    }

    void OnIncrementPressed()
    {
        int max = GetMaxCraftCount();
        craftCount = Mathf.Min(craftCount + 1, max);
        UpdateCountInput();
    }

    int GetMaxCraftCount()
    {
        if (selectedRecipe == null) return 1;

        int max = int.MaxValue;
        foreach (var ingredient in selectedRecipe.ingredients)
        {
            if (ingredient.count <= 0) continue;
            int canMake = playerInventory.GetAmount(ingredient.item) / ingredient.count;
            max = Mathf.Min(max, canMake);
        }
        return Mathf.Max(1, max == int.MaxValue ? 1 : max);
    }

    void OnDecrementPressed()
    {
        craftCount = Mathf.Max(1, craftCount - 1);
        UpdateCountInput();
    }

    void OnMaxPressed()
    {
        craftCount = GetMaxCraftCount();
        UpdateCountInput();
    }

    void UpdateCountInput()
    {
        if (countInput != null)
            countInput.text = craftCount.ToString();
    }

    void ResetCraftCount()
    {
        craftCount = 1;
        UpdateCountInput();
    }

    void SetProgressVisible(bool visible)
    {
        if (progressBarRoot != null)
            progressBarRoot.SetActive(visible);
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
}