using UnityEngine;

/// <summary>
/// エアロック操作ボタン。InteractionManager 経由で E キーにより操作する。
/// </summary>
public class AirlockButton : MonoBehaviour, IInteractable
{
    public enum ActionType { Pressurize, Depressurize }

    [Header("設定")]
    [SerializeField] private ActionType actionType;
    [SerializeField] private Airlock airlock;

    [Header("ハイライト")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color highlightColor = new Color(0.4f, 0.4f, 0f, 1f);

    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>();
    }

    // -----------------------------------------------
    // IInteractable
    // -----------------------------------------------
    public string InteractionLabel =>
        actionType == ActionType.Pressurize ? "与圧 [E]" : "減圧 [E]";

    public bool CanInteract => airlock != null;

    public void Interact()
    {
        if (airlock == null) return;
        if (actionType == ActionType.Pressurize) airlock.RequestPressurize();
        else airlock.RequestDepressurize();
    }

    public void OnFocusEnter() => SetHighlight(highlightColor);
    public void OnFocusExit() => SetHighlight(Color.black);

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