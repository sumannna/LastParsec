using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusBarManager : MonoBehaviour
{
    [Header("設定")]
    public GameObject gaugeBarPrefab;
    public Transform gaugeParent;

    [Header("ゲージ設定")]
    public float gaugeHeight = 20f;
    public float gaugeSpacing = 8f;

    public class GaugeBar
    {
        public string id;
        public GameObject root;
        public Image fill;
        public Color color;
        public float value = 1f;
        public bool isVisible = false;
        public int order; // 表示順（0が一番下）

        public GaugeBar(string id, GameObject root, Image fill, Color color, int order)
        {
            this.id = id;
            this.root = root;
            this.fill = fill;
            this.color = color;
            this.order = order;
            fill.color = color;
        }

        public void SetValue(float ratio)
        {
            value = Mathf.Clamp01(ratio);
            fill.rectTransform.localScale = new Vector3(value, 1f, 1f);

            // 条件表示のゲージ（isVisibleで管理）が0で非表示
            // 常時表示のゲージは0になっても表示のまま
            if (!isVisible) return;
            root.SetActive(true);
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
            root.SetActive(visible);
        }
    }

    private Dictionary<string, GaugeBar> gauges = new Dictionary<string, GaugeBar>();
    private List<GaugeBar> gaugeList = new List<GaugeBar>();

    void Awake()
    {
        CreateGauge("spacesuit", new Color(0.5f, 1f, 0.5f), false, 6);
        CreateGauge("oxygenTank", new Color(0.5f, 0.8f, 1f), false, 5);
        CreateGauge("thruster", new Color(1f, 0.9f, 0f), false, 4);
        CreateGauge("oxygen", new Color(1f, 1f, 1f), true, 3);
        CreateGauge("water", new Color(0.3f, 0.7f, 1f), true, 2);
        CreateGauge("hunger", new Color(1f, 0.6f, 0f), true, 1);
        CreateGauge("hp", new Color(1f, 0.2f, 0.2f), true, 0);

        UpdatePositions();
    }

    void CreateGauge(string id, Color color, bool visibleByDefault, int order)
    {
        GameObject obj = Instantiate(gaugeBarPrefab, gaugeParent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(200, gaugeHeight);

        Image fill = obj.transform.Find("Fill").GetComponent<Image>();
        GaugeBar gauge = new GaugeBar(id, obj, fill, color, order);
        gauge.SetVisible(visibleByDefault);
        gauges[id] = gauge;
        gaugeList.Add(gauge);
    }

    void UpdatePositions()
    {
        foreach (var gauge in gaugeList)
        {
            RectTransform rt = gauge.root.GetComponent<RectTransform>();
            float yPos = gauge.order * (gaugeHeight + gaugeSpacing);
            rt.anchoredPosition = new Vector2(0, yPos);
        }
    }

    public void SetValue(string id, float ratio)
    {
        if (gauges.ContainsKey(id))
            gauges[id].SetValue(ratio);
    }

    public void SetVisible(string id, bool visible)
    {
        if (!gauges.ContainsKey(id)) return;
        gauges[id].SetVisible(visible);
    }
}
