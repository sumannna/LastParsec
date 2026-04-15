using UnityEngine;

/// <summary>
/// ワークベンチインタラクト。
/// 開く：InteractionManager 経由の E キー（範囲内のみ）。
/// 閉じる：E キー（CraftTreeUI が開いているとき・範囲内のみ）。
/// </summary>
[RequireComponent(typeof(Collider))]
public class WorkbenchInteraction : MonoBehaviour, IInteractable
{
    [Header("WB レベル")]
    public WBLevel wbLevel = WBLevel.Lv1;

    [Header("インタラクト設定")]
    [SerializeField] private float interactRange = 1f;

    [Header("参照")]
    [SerializeField] public Transform playerTransform;

    [Header("ハイライト")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color highlightColor = new Color(0.4f, 0.4f, 0f, 1f);

    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        if (!IsPlayerInRange()) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (CraftTreeUI.Instance != null && CraftTreeUI.Instance.IsOpen)
            CraftTreeUI.Instance.Close();
    }

    // -----------------------------------------------
    // IInteractable
    // -----------------------------------------------
    public string InteractionLabel => "クラフト [E]";
    public bool CanInteract => CraftTreeUI.Instance == null || !CraftTreeUI.Instance.IsOpen;

    public void Interact()
    {
        if (UIManager.Instance == null || UIManager.Instance.IsAnyUIOpen()) return;
        UIManager.Instance.OpenCraftTree(this);
    }

    public void OnFocusEnter() => SetHighlight(highlightColor);
    public void OnFocusExit() => SetHighlight(Color.black);

    // -----------------------------------------------
    // 範囲判定（CraftSystem から毎フレーム呼ばれる）
    // -----------------------------------------------
    public bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        return Vector3.Distance(playerTransform.position, transform.position) <= interactRange;
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