using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RadialSegment : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI label;

    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color highlightColor = new Color(0.4f, 0.7f, 1f, 0.9f);

    public void Setup(string name, float angle, float angleStep)
    {
        if (label != null) label.text = name;
        if (background != null) background.color = normalColor;
    }

    public void SetHighlight(bool on)
    {
        if (background != null)
            background.color = on ? highlightColor : normalColor;
    }
}