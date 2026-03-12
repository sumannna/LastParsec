using System.Collections;
using UnityEngine;

/// <summary>
/// クラフト進行を管理するシングルトン。
/// - 1ユニット完成ごとに素材消費
/// - 指定数を順次処理（キュー）
/// - 強制終了時は素材消費しない
/// </summary>
public class CraftSystem : MonoBehaviour
{
    public static CraftSystem Instance { get; private set; }

    [Header("参照")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private PickupSpawner pickupSpawner;
    [SerializeField] private Transform playerTransform;

    // 現在進行中のクラフト情報
    private RecipeData currentRecipe;
    private int remainingCount;
    private Coroutine craftCoroutine;

    // WB必須レシピ用：現在使用中のWB
    private WorkbenchInteraction currentWorkbench;

    // クラフト中かどうか
    public bool IsCrafting => craftCoroutine != null;
    public RecipeData CurrentRecipe => currentRecipe;
    public int RemainingCount => remainingCount;

    // 進捗（0〜1）
    public float Progress { get; private set; }

    // UIへの通知用イベント
    public event System.Action OnCraftStarted;
    public event System.Action<RecipeData, int> OnCraftProgress;   // recipe, remaining
    public event System.Action<RecipeData> OnCraftCompleted;       // 1ユニット完成
    public event System.Action OnCraftFinished;                    // 全ユニット完成
    public event System.Action OnCraftCancelled;

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
    }

    // -----------------------------------------------
    // 公開API
    // -----------------------------------------------

    /// <summary>クラフト開始</summary>
    public bool StartCraft(RecipeData recipe, int count, WorkbenchInteraction workbench = null)
    {
        if (recipe == null || count <= 0) return false;
        if (IsCrafting) return false;

        // 素材チェック（1ユニット分）
        if (!recipe.CanCraft(playerInventory))
        {
            Debug.Log("[CraftSystem] 素材不足");
            return false;
        }

        // WB必須チェック
        if (recipe.requiredWBLevel != WBLevel.None)
        {
            if (workbench == null || !workbench.IsPlayerInRange())
            {
                Debug.Log("[CraftSystem] ワークベンチが必要です");
                return false;
            }
            currentWorkbench = workbench;
        }
        else
        {
            currentWorkbench = null;
        }

        currentRecipe = recipe;
        remainingCount = count;
        Progress = 0f;

        craftCoroutine = StartCoroutine(CraftRoutine());
        OnCraftStarted?.Invoke();
        return true;
    }

    /// <summary>クラフト強制終了（素材返却不要・消費前なので）</summary>
    public void CancelCraft()
    {
        if (!IsCrafting) return;

        StopCoroutine(craftCoroutine);
        craftCoroutine = null;
        Progress = 0f;
        currentRecipe = null;
        currentWorkbench = null;
        remainingCount = 0;

        OnCraftCancelled?.Invoke();
        Debug.Log("[CraftSystem] クラフトキャンセル");
    }

    // -----------------------------------------------
    // クラフトコルーチン
    // -----------------------------------------------

    private IEnumerator CraftRoutine()
    {
        while (remainingCount > 0)
        {
            // 素材チェック（このユニット開始前）
            if (!currentRecipe.CanCraft(playerInventory))
            {
                Debug.Log("[CraftSystem] 素材不足のためキャンセル");
                CancelCraft();
                yield break;
            }

            // WB距離チェック（WB必須レシピのみ）
            if (currentWorkbench != null && !currentWorkbench.IsPlayerInRange())
            {
                Debug.Log("[CraftSystem] WBから離れたためキャンセル");
                CancelCraft();
                yield break;
            }

            // タイマー
            float elapsed = 0f;
            float craftTime = currentRecipe.craftTime;

            while (elapsed < craftTime)
            {
                // WB距離を毎フレームチェック
                if (currentWorkbench != null && !currentWorkbench.IsPlayerInRange())
                {
                    Debug.Log("[CraftSystem] クラフト中にWBから離れました");
                    CancelCraft();
                    yield break;
                }

                elapsed += Time.deltaTime;
                Progress = elapsed / craftTime;
                yield return null;
            }

            Progress = 1f;

            // 1ユニット完成：素材消費 → 完成品格納
            ConsumeIngredients();
            GiveResult();

            remainingCount--;
            OnCraftCompleted?.Invoke(currentRecipe);
            OnCraftProgress?.Invoke(currentRecipe, remainingCount);

            Debug.Log($"[CraftSystem] {currentRecipe.itemResult.itemName} 完成（残り{remainingCount}）");
        }

        // 全ユニット完成
        craftCoroutine = null;
        Progress = 0f;
        currentRecipe = null;
        currentWorkbench = null;

        OnCraftFinished?.Invoke();
        Debug.Log("[CraftSystem] クラフト完了");
    }

    // -----------------------------------------------
    // 内部処理
    // -----------------------------------------------

    /// <summary>素材をインベントリから消費する</summary>
    private void ConsumeIngredients()
    {
        foreach (var ingredient in currentRecipe.ingredients)
        {
            int remaining = ingredient.count;

            for (int i = 0; i < playerInventory.GetSlots().Length && remaining > 0; i++)
            {
                var slots = playerInventory.GetSlots();
                if (slots[i] == null || slots[i].item != ingredient.item) continue;

                int take = Mathf.Min(slots[i].amount, remaining);
                playerInventory.ReduceSlot(slots[i], take);
                remaining -= take;
            }
        }
    }

    /// <summary>完成品をインベントリに追加、満杯ならワールドドロップ</summary>
    private void GiveResult()
    {
        ItemData result = currentRecipe.itemResult;
        int count = currentRecipe.resultCount;

        for (int i = 0; i < count; i++)
        {
            bool added = playerInventory.AddItem(result);
            if (!added)
            {
                // インベントリ満杯 → ワールドドロップ
                if (PickupSpawner.Instance != null)
                    PickupSpawner.Instance.SpawnItem(result, 1);
                else
                    Debug.LogWarning("[CraftSystem] PickupSpawnerが見つかりません");
            }
        }
    }
}