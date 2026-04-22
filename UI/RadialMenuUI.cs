using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 右クリック長押しで表示されるラジアルメニュー。
/// マウス位置によってセグメントを選択する。
/// </summary>
public class RadialMenuUI : MonoBehaviour
{
    public static RadialMenuUI Instance { get; private set; }

    [Header("参照")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Transform segmentContainer;
    [SerializeField] private GameObject segmentPrefab;  // Image + TextMeshPro
    [SerializeField] private TextMeshProUGUI centerLabel;

    [Header("設定")]
    [SerializeField] private float innerRadius = 60f;
    [SerializeField] private float outerRadius = 160f;

    private BuildingData[] currentBuildings;
    private int selectedIndex = -1;
    private RadialSegment[] segments;

    public int SelectedIndex => selectedIndex;
    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        menuRoot.SetActive(false);
    }

    // -----------------------------------------------
    // 表示
    // -----------------------------------------------
    public void Open(BuildingData[] buildings)
    {
        currentBuildings = buildings;
        selectedIndex = -1;
        IsOpen = true;
        menuRoot.SetActive(true);
        BuildSegments();
    }

    public void Close()
    {
        IsOpen = false;
        menuRoot.SetActive(false);
    }

    // -----------------------------------------------
    // セグメント生成
    // -----------------------------------------------
    void BuildSegments()
    {
        // 既存クリア
        foreach (Transform child in segmentContainer)
            Destroy(child.gameObject);

        int count = currentBuildings.Length;
        segments = new RadialSegment[count];

        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep;
            var obj = Instantiate(segmentPrefab, segmentContainer);
            var seg = obj.GetComponent<RadialSegment>();
            if (seg == null) seg = obj.AddComponent<RadialSegment>();

            float rad = (angle - 90f) * Mathf.Deg2Rad;
            float midRadius = (innerRadius + outerRadius) * 0.5f;
            var rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(
                Mathf.Cos(rad) * midRadius,
                Mathf.Sin(rad) * midRadius
            );

            seg.Setup(currentBuildings[i].buildingName, angle, angleStep);
            segments[i] = seg;
        }
    }

    // -----------------------------------------------
    // 毎フレーム更新
    // -----------------------------------------------
    void Update()
    {
        if (!IsOpen) return;

        Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        // 各セグメントのアンカー位置（スクリーン座標）と距離を比較
        float minDist = float.MaxValue;
        int closestIndex = -1;

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float distFromCenter = Vector2.Distance(mousePos, screenCenter);

        if (distFromCenter < innerRadius)
        {
            selectedIndex = -1;
            if (centerLabel != null) centerLabel.text = "キャンセル";
            HighlightSegment(-1);
            return;
        }

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == null) continue;

            // セグメントのスクリーン座標を取得
            RectTransform rt = segments[i].GetComponent<RectTransform>();
            Vector3 worldPos = rt.position;
            float dist = Vector2.Distance(mousePos, new Vector2(worldPos.x, worldPos.y));

            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }

        selectedIndex = closestIndex;
        if (closestIndex >= 0 && centerLabel != null)
            centerLabel.text = currentBuildings[closestIndex].buildingName;
        HighlightSegment(closestIndex);
    }

    void HighlightSegment(int index)
    {
        if (segments == null) return;
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] != null)
                segments[i].SetHighlight(i == index);
        }
    }
}