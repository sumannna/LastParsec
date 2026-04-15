using UnityEngine;

/// <summary>
/// 充填機インタラクト。
/// 開く：InteractionManager 経由の E キー。
/// 閉じる：E キーまたは Tab キー（自分の UI が開いているとき）。
/// </summary>
public class FillingMachineInteraction : MonoBehaviour, IInteractable
{
    [Header("参照")]
    public FillingMachine fillingMachine;

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
        if (FillingMachineUI.Instance == null) return;

        if (FillingMachineUI.Instance.IsOpen &&
            FillingMachineUI.Instance.CurrentMachine == fillingMachine)
            FillingMachineUI.Instance.Close();
    }

    // -----------------------------------------------
    // IInteractable
    // -----------------------------------------------
    public string InteractionLabel => "操作 [E]";
    public bool CanInteract =>
        FillingMachineUI.Instance == null ||
        (!FillingMachineUI.Instance.IsOpen && !FillingMachineUI.Instance.ClosedThisFrame);

    public void Interact()
    {
        if (UIManager.Instance == null || UIManager.Instance.IsAnyUIOpen()) return;
        UIManager.Instance.OpenFillingMachine(this);
    }

    public void OnFocusEnter() => SetHighlight(highlightColor);
    public void OnFocusExit() => SetHighlight(Color.black);

    public FillingMachine GetMachine() => fillingMachine;

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
}