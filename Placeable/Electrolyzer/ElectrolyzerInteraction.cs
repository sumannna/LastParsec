using UnityEngine;

/// <summary>
/// 電気分解機インタラクト。
/// 開く：InteractionManager 経由の E キー。
/// 閉じる：E キーまたは Tab キー（UI が開いているとき）。
/// </summary>
public class ElectrolyzerInteraction : MonoBehaviour, IInteractable
{
    [Header("参照")]
    public Electrolyzer electrolyzer;

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
        if (ElectrolyzerUI.Instance == null) return;

        if (ElectrolyzerUI.Instance.IsOpen)
            ElectrolyzerUI.Instance.Close();
    }

    // -----------------------------------------------
    // IInteractable
    // -----------------------------------------------
    public string InteractionLabel => "操作 [E]";
    public bool CanInteract => ElectrolyzerUI.Instance == null || !ElectrolyzerUI.Instance.IsOpen;

    public void Interact()
    {
        if (UIManager.Instance == null || UIManager.Instance.IsAnyUIOpen()) return;
        UIManager.Instance.OpenElectrolyzer(this);
    }

    public void OnFocusEnter() => SetHighlight(highlightColor);
    public void OnFocusExit() => SetHighlight(Color.black);

    public Electrolyzer GetMachine() => electrolyzer;

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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
#endif
}