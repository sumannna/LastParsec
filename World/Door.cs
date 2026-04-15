using System.Collections;
using UnityEngine;

/// <summary>
/// 横スライドドア。InteractionManager 経由で E キーによりトグル開閉する。
/// playerTransform の参照は不要になったため削除済み。
/// </summary>
public class Door : MonoBehaviour, IInteractable
{
    [Header("設定")]
    [SerializeField] private float openDuration = 0.5f;
    [SerializeField] private Vector3 openOffset = new Vector3(2f, 0f, 0f);
    [SerializeField] private bool startOpen = false;

    [Header("ハイライト")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color highlightColor = new Color(0.4f, 0.4f, 0f, 1f);

    public bool IsOpen { get; private set; } = false;
    public bool IsMoving { get; private set; } = false;

    private Vector3 closedLocalPos;
    private Vector3 openLocalPos;

    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        closedLocalPos = transform.localPosition;
        openLocalPos = transform.localPosition + openOffset;

        if (startOpen)
        {
            transform.localPosition = openLocalPos;
            IsOpen = true;
        }

        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>();
    }

    // -----------------------------------------------
    // IInteractable
    // -----------------------------------------------
    public string InteractionLabel => IsOpen ? "閉じる [E]" : "開く [E]";
    public bool CanInteract => !IsMoving;

    public void Interact() => Toggle();

    public void OnFocusEnter() => SetHighlight(highlightColor);
    public void OnFocusExit() => SetHighlight(Color.black);

    // -----------------------------------------------
    // 開閉
    // -----------------------------------------------
    void Toggle()
    {
        if (IsMoving) return;
        StartCoroutine(Slide(!IsOpen));
    }

    IEnumerator Slide(bool opening)
    {
        IsMoving = true;
        Vector3 from = transform.localPosition;
        Vector3 to = opening ? openLocalPos : closedLocalPos;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(from, to, elapsed / openDuration);
            yield return null;
        }

        transform.localPosition = to;
        IsOpen = opening;
        IsMoving = false;
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + openOffset, transform.lossyScale);
    }
#endif
}