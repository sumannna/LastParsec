using UnityEngine;
using TMPro;

/// <summary>
/// 画面中央下部に「開く [E]」などのインタラクションプロンプトを表示する。
/// Canvas 上の Panel + TextMeshPro をアサインして使用。
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI label;

    void Awake() => Hide();

    public void Show(string text)
    {
        Debug.Log($"[PromptUI] Show: {text} / panel={panel != null} / label={label != null}");
        if (panel != null) panel.SetActive(true);
        if (label != null) label.text = text;
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}