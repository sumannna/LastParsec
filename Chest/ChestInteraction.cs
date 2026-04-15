using UnityEngine;

/// <summary>
/// チェストへのインタラクト処理。
/// 開く：InteractionManager 経由の E キー。
/// 閉じる：E キー（isOpen 時）または距離外に出たとき自動で閉じる。
/// </summary>
public class ChestInteraction : MonoBehaviour, IInteractable
{
    [Header("設定")]
    public float interactRange = 2f;

    [Header("参照")]
    public Transform playerTransform;
    public ChestInventory chestInventory;
    public Inventory playerInventory;
    public InventoryUI inventoryUI;
    public PickupSpawner pickupSpawner;

    [Header("ハイライト")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color highlightColor = new Color(0.4f, 0.4f, 0f, 1f);

    private bool isOpen = false;
    public bool IsOpen => isOpen;

    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        // 距離外に出たら自動で閉じる
        if (isOpen && !IsPlayerInRange())
        {
            CloseChest();
            return;
        }

        // E キーで閉じる（開いているときのみ）
        if (isOpen && Input.GetKeyDown(KeyCode.E))
            CloseChest();
    }

    // -----------------------------------------------
    // IInteractable
    // -----------------------------------------------
    public string InteractionLabel => "開く [E]";
    public bool CanInteract => !isOpen;

    public void Interact()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return;
        OpenChest();
    }

    public void OnFocusEnter() => SetHighlight(highlightColor);
    public void OnFocusExit() => SetHighlight(Color.black);

    // -----------------------------------------------
    // 開閉
    // -----------------------------------------------
    void OpenChest()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return;
        isOpen = true;
        ChestUI.Instance?.Open(this);
    }

    public void CloseChest()
    {
        if (!isOpen) return;
        isOpen = false;
        if (ChestUI.Instance != null && ChestUI.Instance.CurrentChest == this)
            ChestUI.Instance.Close();
    }

    public bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        return Vector3.Distance(playerTransform.position, transform.position) <= interactRange;
    }

    /// <summary>チェストを回収して GameObject を削除する</summary>
    public void Collect()
    {
        chestInventory.Collect(playerInventory, pickupSpawner);
        Destroy(gameObject);
    }

    // -----------------------------------------------
    // ハイライト
    // -----------------------------------------------
    void SetHighlight(Color color)
    {
        foreach (var r in highlightRenderers)
        {
            if (r == null) continue;
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionColorProp, color);
            r.SetPropertyBlock(mpb);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}