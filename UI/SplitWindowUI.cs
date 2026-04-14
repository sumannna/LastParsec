using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// インベントリ上に常時表示される分割ウィンドウ。
/// スロットをクリックすると選択状態になり、スライダーで個数を指定して
/// アイコンをD&Dすることでアイテムを分割移動できる。
///
/// ■ Unity Editor設定
/// 1. Canvas配下にPanelを作成（名前：SplitWindowUI）
///    → インベントリパネルの上部に配置
///    → SplitWindowUIコンポーネントをアタッチ
/// 2. 子オブジェクトを作成：
///    - ItemIcon     (Image)          → displayIcon にアサイン
///    - AmountText   (TMP_Text)        → amountText  にアサイン
///    - AmountSlider (Slider)          → amountSlider にアサイン
///    - DragIconObj  (Image)           → dragIconObj  にアサイン
///      └ DragIconObj の子に "ItemIcon" という名前のImage を作成
/// 3. InventoryUI の splitWindowUI フィールドにこのオブジェクトをアサイン
/// </summary>
public class SplitWindowUI : MonoBehaviour
{
    [Header("UI部品")]
    public Image displayIcon;           // 選択中アイテムの表示アイコン（操作不可）
    public TextMeshProUGUI amountText;  // 選択中スタック数テキスト
    public Slider amountSlider;         // 分割個数スライダー
    public TMP_InputField amountInput;  // 分割個数入力フィールド
    public GameObject dragIconObj;      // D&D用アイコン（ItemDragHandlerをAddComponent）

    // 内部状態
    private Inventory.Slot currentSlot;
    private Inventory currentInventory;
    private InventoryUI currentInventoryUI;
    private ItemDragHandler dragHandler;
    private int splitAmount = 1;

    void Awake()
    {
        if (amountSlider != null)
            amountSlider.onValueChanged.AddListener(OnSliderChanged);

        if (amountInput != null)
        {
            amountInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            amountInput.onEndEdit.AddListener(OnInputEndEdit);
        }

        Deselect();
    }

    // -----------------------------------------------
    // スロット選択（SlotClickHandlerから呼ぶ）
    // -----------------------------------------------

    /// <summary>
    /// スロットをクリックした時に呼ぶ。ウィンドウにアイテム情報をセットする。
    /// </summary>
    public void Select(Inventory.Slot slot, Inventory inventory, InventoryUI inventoryUI)
    {
        // ゲージ付き・nullは選択不可
        if (slot == null || slot.item == null || IsGaugeSlot(slot))
        {
            Deselect();
            return;
        }

        currentSlot = slot;
        currentInventory = inventory;
        currentInventoryUI = inventoryUI;
        splitAmount = 1;

        // アイコン表示
        if (displayIcon != null)
        {
            displayIcon.sprite = slot.item.icon;
            displayIcon.color = slot.item.icon != null ? Color.white : Color.clear;
        }

        // スタック数テキスト
        UpdateAmountText();

        // スライダー設定
        if (amountSlider != null)
        {
            amountSlider.minValue = 1;
            amountSlider.maxValue = Mathf.Max(1, slot.amount - 1);
            amountSlider.wholeNumbers = true;
            amountSlider.value = 1;
            amountSlider.interactable = slot.amount >= 2;
        }

        // 入力フィールド設定
        if (amountInput != null)
        {
            amountInput.text = "1";
            amountInput.interactable = slot.amount >= 2;
        }

        // ドラッグハンドラ更新
        SetupDragHandler();
    }

    /// <summary>
    /// 選択解除（インベントリを閉じた時・スロットが消えた時）
    /// </summary>
    public void Deselect()
    {
        currentSlot = null;
        currentInventory = null;
        currentInventoryUI = null;

        if (displayIcon != null)
        {
            displayIcon.sprite = null;
            displayIcon.color = Color.clear;
        }

        if (amountText != null)
            amountText.text = "-";

        if (amountSlider != null)
        {
            amountSlider.value = 1;
            amountSlider.interactable = false;
        }

        if (amountInput != null)
        {
            amountInput.text = "";
            amountInput.interactable = false;
        }

        if (dragIconObj != null)
        {
            // ドラッグアイコンのImageをクリア
            Image icon = GetDragIcon();
            if (icon != null) { icon.sprite = null; icon.color = Color.clear; }
        }

        if (dragHandler != null)
        {
            DestroyImmediate(dragHandler);
            dragHandler = null;
        }
    }

    /// <summary>
    /// RefreshAll後に呼ぶ。選択中スロットが消えていたら選択解除。
    /// </summary>
    public void Validate()
    {
        if (currentSlot == null) return;

        // スロットがインベントリから消えていないか確認
        if (currentInventory == null)
        {
            Deselect();
            return;
        }

        bool found = false;
        foreach (var s in currentInventory.GetSlots())
        {
            if (s == currentSlot) { found = true; break; }
        }

        if (!found || currentSlot.amount < 1)
        {
            Deselect();
            return;
        }

        // スライダー上限を更新
        if (amountSlider != null)
        {
            amountSlider.maxValue = Mathf.Max(1, currentSlot.amount - 1);
            if (amountSlider.value > amountSlider.maxValue)
                amountSlider.value = amountSlider.maxValue;
            amountSlider.interactable = currentSlot.amount >= 2;
        }

        if (amountInput != null)
        {
            int clamped = Mathf.Clamp(splitAmount, 1, currentSlot.amount - 1);
            amountInput.text = clamped.ToString();
            amountInput.interactable = currentSlot.amount >= 2;
        }

        // D&D後にamountが減った場合、splitAmountをclampして全体に反映
        int maxAmount = Mathf.Max(1, currentSlot.amount - 1);
        splitAmount = Mathf.Clamp(splitAmount, 1, maxAmount);

        if (amountSlider != null)
            amountSlider.SetValueWithoutNotify(splitAmount);
        if (amountInput != null)
            amountInput.SetTextWithoutNotify(splitAmount.ToString());

        UpdateAmountText();
        SetupDragHandler(); // D&D後は常に再生成して確実にリセット
    }

    // -----------------------------------------------
    // スライダー連動
    // -----------------------------------------------

    void OnSliderChanged(float value)
    {
        splitAmount = Mathf.RoundToInt(value);
        UpdateAmountText();

        // 入力欄と同期（リスナーの無限ループ防止）
        if (amountInput != null && amountInput.text != splitAmount.ToString())
            amountInput.SetTextWithoutNotify(splitAmount.ToString());

        if (dragHandler != null)
            dragHandler.dragAmount = splitAmount;
    }

    void OnInputEndEdit(string value)
    {
        if (currentSlot == null) return;

        if (!int.TryParse(value, out int parsed))
            parsed = 1;

        int max = currentSlot.amount - 1;
        splitAmount = Mathf.Clamp(parsed, 1, max);

        // 入力欄を補正値で上書き
        amountInput.SetTextWithoutNotify(splitAmount.ToString());

        // スライダーと同期
        if (amountSlider != null)
            amountSlider.SetValueWithoutNotify(splitAmount);

        UpdateAmountText();

        if (dragHandler != null)
            dragHandler.dragAmount = splitAmount;
    }

    void UpdateAmountText()
    {
        if (amountText == null || currentSlot == null) return;
        amountText.text = $"{splitAmount} / {currentSlot.amount}";
    }

    // -----------------------------------------------
    // ドラッグハンドラのセットアップ
    // -----------------------------------------------

    void SetupDragHandler()
    {
        if (dragIconObj == null || currentSlot == null) return;

        Image icon = GetDragIcon();
        if (icon != null)
        {
            icon.sprite = currentSlot.item.icon;
            icon.color = currentSlot.item.icon != null ? Color.white : Color.clear;
            icon.enabled = true;
        }
        else
        {
            Debug.LogWarning("[SplitWindow] DragIconObj配下にItemIconという名前のImageが見つからない。");
        }

        // 既存のItemDragHandlerを破棄して再生成
        if (dragHandler != null)
        {
            DestroyImmediate(dragHandler);
            dragHandler = null;
        }

        dragHandler = dragIconObj.AddComponent<ItemDragHandler>();
        dragHandler.inventorySlot = currentSlot;
        dragHandler.inventory = currentInventory;
        dragHandler.inventoryUI = currentInventoryUI;
        dragHandler.dragAmount = splitAmount;
        dragHandler.useCustomDragAmount = true;
    }

    // dragIconObj配下の "ItemIcon" Imageを取得
    Image GetDragIcon()
    {
        if (dragIconObj == null) return null;
        foreach (Transform child in dragIconObj.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "ItemIcon")
            {
                child.gameObject.SetActive(true); // 非アクティブでも強制表示
                return child.GetComponent<Image>();
            }
        }
        return null;
    }

    // -----------------------------------------------
    // ユーティリティ
    // -----------------------------------------------

    bool IsGaugeSlot(Inventory.Slot slot)
    {
        return slot?.item is OxygenTankData
            || slot?.item is ThrusterTankData
            || slot?.item is SpacesuitData;
    }
}