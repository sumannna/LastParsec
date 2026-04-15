using UnityEngine;

/// <summary>
/// 解氷機インタラクト。
/// 開く：InteractionManager 経由の E キー。
/// 閉じる：E キーまたは Tab キー（UI が開いているとき）。
/// </summary>
public class IceMelterInteraction : MonoBehaviour, IInteractable
{
    [Header("参照")]
    public IceMelter iceMelter;

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
        bool ePressed = Input.GetKeyDown(KeyCode.E);
        bool tabPressed = Input.GetKeyDown(KeyCode.Tab);
        if (!ePressed && !tabPressed) return;

        if (IceMelterUI.Instance != null && IceMelterUI.Instance.IsOpen)
            IceMelterUI.Instance.Close();
    }

    // -----------------------------------------------
    // IInteractable
    // -----------------------------------------------
    public string InteractionLabel => "操作 [E]";
    public bool CanInteract => IceMelterUI.Instance == null || !IceMelterUI.Instance.IsOpen;

    public void Interact()
    {
        if (UIManager.Instance == null || UIManager.Instance.IsAnyUIOpen()) return;
        UIManager.Instance.OpenIceMelter(this);
    }

    public void OnFocusEnter() => SetHighlight(highlightColor);
    public void OnFocusExit() => SetHighlight(Color.black);

    public IceMelter GetMachine() => iceMelter;

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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
#endif
}