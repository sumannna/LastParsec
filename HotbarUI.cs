using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarUI : MonoBehaviour
{
    [Header("UI")]
    public Transform contentParent;
    public GameObject slotPrefab;

    [Header("参照")]
    public Hotbar hotbar;
    public Inventory inventory;
    public InventoryUI inventoryUI;

    [Header("見た目")]
    public Color normalColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    public Color highlightColor = new Color(1f, 0.85f, 0f, 1f);

    private GameObject[] slotObjects = new GameObject[Hotbar.SlotCount];

    private static readonly KeyCode[] hotkeys = new KeyCode[]
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
    };

    void Start()
    {
        BuildSlots();
        RefreshAll();
    }

    void Update()
    {
        for (int i = 0; i < hotkeys.Length; i++)
        {
            if (Input.GetKeyDown(hotkeys[i]))
            {
                hotbar.SetSelected(i);
                UpdateHighlight();
                break;
            }
        }

        // ホイールスクロールで選択切り替え
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            int current = hotbar.SelectedIndex;
            int next = scroll > 0f ? current - 1 : current + 1;
            next = (next + Hotbar.SlotCount) % Hotbar.SlotCount;
            hotbar.SetSelected(next);
            UpdateHighlight();
        }

        // ゲージのリアルタイム更新（ドラッグ中は除く）
        if (!ItemDragHandler.AnyDragging)
            UpdateGauges();
    }

    void UpdateGauges()
    {
        Hotbar.Slot[] slots = hotbar.GetSlots();
        for (int i = 0; i < Hotbar.SlotCount; i++)
        {
            GameObject slotObj = slotObjects[i];
            if (slotObj == null) continue;

            Hotbar.Slot slot = slots[i];
            if (slot.item == null) continue;

            if (slot.item is ToolData && slot.toolInstance != null)
                SetGauge(slotObj, "TankSlotGauge", slot.toolInstance.Ratio, new Color32(255, 165, 0, 255));
        }
    }

    void BuildSlots()
    {
        foreach (var obj in slotObjects)
            if (obj != null) Destroy(obj);

        for (int i = 0; i < Hotbar.SlotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, contentParent);
            slotObjects[i] = slotObj;

            // 番号テキスト
            GameObject numObj = new GameObject("HotkeyText");
            numObj.transform.SetParent(slotObj.transform, false);
            RectTransform numRt = numObj.AddComponent<RectTransform>();
            numRt.anchorMin = new Vector2(0, 1);
            numRt.anchorMax = new Vector2(0, 1);
            numRt.pivot = new Vector2(0, 1);
            numRt.anchoredPosition = new Vector2(4, -4);
            numRt.sizeDelta = new Vector2(20, 20);
            TextMeshProUGUI numText = numObj.AddComponent<TextMeshProUGUI>();
            numText.text = i == 9 ? "0" : (i + 1).ToString();
            numText.fontSize = 12;
            numText.color = Color.white;
            numText.raycastTarget = false;

            // ドロップハンドラ
            HotbarDropHandler dropHandler = slotObj.AddComponent<HotbarDropHandler>();
            dropHandler.hotbar = hotbar;
            dropHandler.hotbarIndex = i;
            dropHandler.inventory = inventory;
            dropHandler.inventoryUI = inventoryUI;
            dropHandler.hotbarUI = this;

            // ドラッグハンドラ（ホットバー→インベントリのD&D用）
            ItemDragHandler dragHandler = slotObj.AddComponent<ItemDragHandler>();
            dragHandler.hotbar = hotbar;
            dragHandler.hotbarIndex = i;
            dragHandler.hotbarSlot = hotbar.GetSlot(i);
            dragHandler.hotbarUI = this;
            dragHandler.inventory = inventory;
            dragHandler.inventoryUI = inventoryUI;

            int index = i;
            slotObj.name = $"HotbarSlot_{index}";
        }
    }

    public void RefreshAll()
    {
        Hotbar.Slot[] slots = hotbar.GetSlots();
        for (int i = 0; i < Hotbar.SlotCount; i++)
        {
            GameObject slotObj = slotObjects[i];
            if (slotObj == null) continue;

            Hotbar.Slot slot = slots[i];

            // ItemDragHandlerのhotbarSlot参照を最新に更新
            ItemDragHandler dragHandler = slotObj.GetComponent<ItemDragHandler>();
            if (dragHandler != null)
                dragHandler.hotbarSlot = slot;

            Image icon = FindChild<Image>(slotObj, "ItemIcon");
            if (icon != null)
            {
                icon.gameObject.SetActive(true); // HideSourceVisualsで非表示になった場合に復元
                if (slot.item != null && slot.item.icon != null)
                { icon.sprite = slot.item.icon; icon.color = Color.white; }
                else
                { icon.sprite = null; icon.color = Color.clear; }
            }

            TextMeshProUGUI amount = FindChild<TextMeshProUGUI>(slotObj, "AmountText");
            if (amount != null)
                amount.text = (slot.item != null && slot.amount > 1) ? slot.amount.ToString() : "";

            // 耐久ゲージ（ToolInstance）
            if (slot.item is ToolData && slot.toolInstance != null)
                SetGauge(slotObj, "TankSlotGauge", slot.toolInstance.Ratio, new Color32(255, 165, 0, 255));
            else
            {
                Transform gauge = FindChildTransform(slotObj, "TankSlotGauge");
                if (gauge != null) gauge.gameObject.SetActive(false);
            }
        }

        UpdateHighlight();
    }

    void UpdateHighlight()
    {
        int selected = hotbar.SelectedIndex;
        for (int i = 0; i < Hotbar.SlotCount; i++)
        {
            if (slotObjects[i] == null) continue;
            Image bg = slotObjects[i].GetComponent<Image>();
            if (bg != null)
                bg.color = (i == selected) ? highlightColor : normalColor;
        }
    }

    T FindChild<T>(GameObject root, string name) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<T>();
        return null;
    }

    Transform FindChildTransform(GameObject root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    void SetGauge(GameObject root, string gaugeName, float ratio, Color32 color)
    {
        Transform gaugeTransform = FindChildTransform(root, gaugeName);
        if (gaugeTransform == null) return;

        if (ratio <= 0f) { gaugeTransform.gameObject.SetActive(false); return; }

        gaugeTransform.gameObject.SetActive(true);
        Transform fill = gaugeTransform.Find("Fill");
        if (fill != null)
        {
            Image fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = color;
                fillImage.rectTransform.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);
            }
        }
    }
}