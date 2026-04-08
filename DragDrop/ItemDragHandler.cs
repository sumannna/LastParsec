using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ItemDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("参照")]
    public Inventory inventory;
    public InventoryUI inventoryUI;
    public EquipmentSystem equipmentSystem;
    public EquipmentUI equipmentUI;

    [Header("どちらからドラッグされたか")]
    public Inventory.Slot inventorySlot;
    public EquipmentSlotData equipmentSlotData;
    public Hotbar.Slot hotbarSlot;
    public int hotbarIndex = -1;
    public Hotbar hotbar;
    public HotbarUI hotbarUI;

    // -----------------------------------------------
    // 機械スロット用フィールド（IceMelter/FillingMachine/Chest等）
    // -----------------------------------------------
    [HideInInspector] public ISlotOwner machineOwner;
    [HideInInspector] public int machineSlotIndex = -1;

    // OnDrop → OnEndDrag の順で呼ばれるため、OnDrop時点でhotbarSlotがnullになる。
    // ホットバードラッグだったかをフラグで記録する。
    private bool wasHotbarDrag = false;
    // 機械スロットドラッグだったかをフラグで記録する。
    private bool wasMachineDrag = false;

    /// <summary>このドラッグで運ぶ個数。</summary>
    [HideInInspector] public int dragAmount;
    /// <summary>trueの場合、OnBeginDragでdragAmountを上書きしない（SplitWindow用）。</summary>
    [HideInInspector] public bool useCustomDragAmount = false;

    private GameObject dragIcon;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Image sourceIcon;
    private RectTransform dragIconRt;
    private GameObject sourceGaugeObject;
    private TMP_Text sourceAmountText;

    public static bool AnyDragging { get; private set; }
    private static ItemDragHandler activeDragHandler;

    // 右クリックドラッグ用
    private bool isRightClickDragging = false;
    private bool rightButtonHeldOnThis = false;
    private Vector2 rightMouseDownPos;
    private const float rightDragThreshold = 5f;

    // ホイールクリックドラッグ用
    private bool isMiddleClickDragging = false;
    private bool middleButtonHeldOnThis = false;
    private Vector2 middleMouseDownPos;
    private const float middleDragThreshold = 5f;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();
        CacheSourceReferences();
    }

    // -----------------------------------------------
    // 右クリック・ホイールクリック検出（Update直接取得）
    // -----------------------------------------------

    void Update()
    {
        // ── 右クリック押下 ──
        if (Input.GetMouseButtonDown(1))
        {
            if (!AnyDragging && !isRightClickDragging && !isMiddleClickDragging)
            {
                if (IsMouseOverThisObject())
                {
                    rightButtonHeldOnThis = true;
                    rightMouseDownPos = Input.mousePosition;
                }
            }
        }

        if (rightButtonHeldOnThis && !isRightClickDragging && !AnyDragging)
        {
            if (Vector2.Distance(Input.mousePosition, rightMouseDownPos) > rightDragThreshold)
            {
                rightButtonHeldOnThis = false;
                BeginSpecialDrag(dragDivisor: 0);
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            rightButtonHeldOnThis = false;
            if (isRightClickDragging) EndSpecialDrag(ref isRightClickDragging);
        }

        // ── ホイールクリック押下（Shift必須）──
        if (Input.GetMouseButtonDown(2) && !AnyDragging && !isRightClickDragging && !isMiddleClickDragging)
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (shift && IsMouseOverThisObject())
            {
                middleButtonHeldOnThis = true;
                middleMouseDownPos = Input.mousePosition;
            }
        }

        if (middleButtonHeldOnThis && !isMiddleClickDragging && !AnyDragging)
        {
            if (Vector2.Distance(Input.mousePosition, middleMouseDownPos) > middleDragThreshold)
            {
                middleButtonHeldOnThis = false;
                BeginSpecialDrag(dragDivisor: 3); // 3 = 1/3
            }
        }

        if (Input.GetMouseButtonUp(2))
        {
            middleButtonHeldOnThis = false;
            if (isMiddleClickDragging) EndSpecialDrag(ref isMiddleClickDragging);
        }

        // ── ドラッグ中の位置更新 ──
        if (isRightClickDragging || isMiddleClickDragging)
            UpdateSpecialDragIconPosition();
    }

    bool IsMouseOverThisObject()
    {
        var pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        foreach (var r in results)
        {
            if (r.gameObject == gameObject || r.gameObject.transform.IsChildOf(transform))
                return true;
        }
        return false;
    }

    // -----------------------------------------------
    // ソース判定ヘルパー
    // -----------------------------------------------

    bool IsInventoryDrag() => inventorySlot != null;
    bool IsHotbarDrag() => hotbarSlot != null;

    /// <summary>機械・チェストスロットからのドラッグか。</summary>
    public bool IsMachineDrag() => machineOwner != null && machineSlotIndex >= 0;

    // -----------------------------------------------
    // 右クリック・ホイールクリック共通ドラッグ
    // -----------------------------------------------

    /// <param name="dragDivisor">0=1個固定、2=半分、3=1/3</param>
    void BeginSpecialDrag(int dragDivisor)
    {
        // ── ソースアイテム取得 ──
        ItemData item = null;
        int amount = 0;

        if (IsMachineDrag())
        {
            if (machineOwner.IsReadOnly) return; // Outputスロットは分割移動禁止
            var mSlot = machineOwner.GetSlot(machineSlotIndex);
            if (mSlot == null || mSlot.item == null || mSlot.amount <= 0) return;
            item = mSlot.item;
            amount = mSlot.amount;
        }
        else if (inventorySlot != null)
        {
            if (inventorySlot.item == null || inventorySlot.amount <= 0) return;
            if (IsGaugeSlot(inventorySlot)) return;
            item = inventorySlot.item;
            amount = inventorySlot.amount;
        }
        else return;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        canvasRect = canvas.GetComponent<RectTransform>();

        CacheSourceReferences();
        if (sourceIcon == null) return;

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        dragAmount = dragDivisor == 0
            ? (shift ? 2 : 1)
            : Mathf.Max(1, amount / dragDivisor);

        AnyDragging = true;
        activeDragHandler = this;
        wasMachineDrag = IsMachineDrag();

        if (dragDivisor == 0) isRightClickDragging = true;
        else isMiddleClickDragging = true;

        RectTransform sourceRt = sourceIcon.GetComponent<RectTransform>();

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        RectTransform rt = dragIcon.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = sourceRt != null ? sourceRt.pivot : new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sourceRt != null ? sourceRt.sizeDelta : new Vector2(60f, 60f);
        rt.localScale = Vector3.one;
        dragIconRt = rt;

        Image img = dragIcon.AddComponent<Image>();
        img.sprite = item.icon;
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;

        CreateAmountText(dragAmount);
        CreateGauge(GetGaugeRatio(item));
        HideSourceVisuals();
        UpdateSpecialDragIconPosition();
    }

    void UpdateSpecialDragIconPosition()
    {
        if (dragIconRt == null || canvasRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, Input.mousePosition, null, out Vector2 localPoint);
        dragIconRt.localPosition = localPoint;
    }

    void EndSpecialDrag(ref bool draggingFlag)
    {
        draggingFlag = false;
        isRightClickDragging = false;
        isMiddleClickDragging = false;
        rightButtonHeldOnThis = false;
        middleButtonHeldOnThis = false;
        activeDragHandler = null;
        dragIconRt = null;
        AnyDragging = false;

        if (dragIcon != null) { Destroy(dragIcon); dragIcon = null; }

        // カーソル下のDropHandlerへ（MachineDropHandler / HotbarDropHandler / DropHandler の順で検索）
        var pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            MachineDropHandler mDh = result.gameObject.GetComponent<MachineDropHandler>();
            if (mDh != null) { mDh.ReceiveDrop(this); break; }

            HotbarDropHandler hDh = result.gameObject.GetComponent<HotbarDropHandler>();
            if (hDh != null) { hDh.ReceiveDrop(this); break; }

            DropHandler dh = result.gameObject.GetComponent<DropHandler>();
            if (dh != null) { dh.ReceiveDrop(this); break; }
        }

        // ── ドロップ後の後処理 ──
        if (wasMachineDrag)
        {
            wasMachineDrag = false;
            if (machineOwner != null)
                ShowSourceVisuals();
        }
        else if (inventorySlot != null)
        {
            if (!IsPointerOverInventoryOrEquipment(results))
                DropToWorld();
            else
                ShowSourceVisuals();
        }
    }

    // -----------------------------------------------
    // キャッシュ・ユーティリティ
    // -----------------------------------------------

    void CacheSourceReferences()
    {
        sourceIcon = null;
        sourceGaugeObject = null;
        sourceAmountText = null;

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (sourceIcon == null && child.name == "ItemIcon")
                sourceIcon = child.GetComponent<Image>();
            if (sourceGaugeObject == null && child.name == GetGaugeObjectName())
                sourceGaugeObject = child.gameObject;
            if (sourceAmountText == null && child.name == "AmountText")
                sourceAmountText = child.GetComponent<TMP_Text>();
        }
    }

    public static bool IsGaugeSlot(Inventory.Slot slot)
    {
        return slot?.item is OxygenTankData
            || slot?.item is ThrusterTankData
            || slot?.item is SpacesuitData;
    }

    ItemData GetDraggedItem()
    {
        if (IsMachineDrag()) return machineOwner.GetSlot(machineSlotIndex)?.item;
        if (IsInventoryDrag()) return inventorySlot.item;
        if (IsHotbarDrag()) return hotbarSlot.item;
        if (equipmentSystem != null && equipmentSlotData != null)
            return equipmentSystem.GetEquipped(equipmentSlotData);
        return null;
    }

    float GetGaugeRatio(ItemData item)
    {
        if (item == null) return -1f;

        if (IsMachineDrag())
        {
            var mSlot = machineOwner.GetSlot(machineSlotIndex);
            if (mSlot == null) return -1f;
            if (item is OxygenTankData && mSlot.tankInstance != null) return mSlot.tankInstance.Ratio;
            if (item is ThrusterTankData && mSlot.thrusterInstance != null) return mSlot.thrusterInstance.Ratio;
            if (item is WaterTankData && mSlot.waterTankInstance != null) return mSlot.waterTankInstance.Ratio;
            if (item is ToolData && mSlot.toolInstance != null) return mSlot.toolInstance.Ratio;
            return -1f;
        }

        if (IsInventoryDrag())
        {
            if (item is OxygenTankData && inventorySlot.tankInstance != null) return inventorySlot.tankInstance.Ratio;
            if (item is ThrusterTankData && inventorySlot.thrusterInstance != null) return inventorySlot.thrusterInstance.Ratio;
            if (item is SpacesuitData && inventorySlot.spacesuitInstance != null) return inventorySlot.spacesuitInstance.Ratio;
            if (item is ToolData && inventorySlot.toolInstance != null) return inventorySlot.toolInstance.Ratio;
        }
        else if (IsHotbarDrag())
        {
            if (item is ToolData && hotbarSlot.toolInstance != null)
                return hotbarSlot.toolInstance.Ratio;
        }
        else if (equipmentSystem != null)
        {
            if (item is OxygenTankData) { var t = equipmentSystem.GetTankInstance(equipmentSlotData); if (t != null) return t.Ratio; }
            if (item is ThrusterTankData) { var t = equipmentSystem.GetThrusterInstance(equipmentSlotData); if (t != null) return t.Ratio; }
            if (item is SpacesuitData) { var s = equipmentSystem.GetSpacesuitInstance(equipmentSlotData); if (s != null) return s.Ratio; }
        }
        return -1f;
    }

    string GetGaugeObjectName() =>
        (IsInventoryDrag() || IsHotbarDrag() || IsMachineDrag()) ? "TankSlotGauge" : "TankGauge";

    int GetDraggedAmount()
    {
        if (IsMachineDrag()) return machineOwner.GetSlot(machineSlotIndex)?.amount ?? 0;
        if (IsInventoryDrag()) return inventorySlot.amount;
        if (IsHotbarDrag()) return hotbarSlot.amount;
        return 1;
    }

    // -----------------------------------------------
    // ビジュアル制御
    // -----------------------------------------------

    public void HideSourceVisuals()
    {
        // 部分移動（右クリックDD=1個、Shiftドラッグ=半分）の場合は
        // 移動元スロットにアイコン・ゲージを残し、残数だけ更新する。
        int totalAmount = GetDraggedAmount();
        bool isPartialDrag = dragAmount > 0 && dragAmount < totalAmount;

        if (isPartialDrag)
        {
            // アイコンとゲージはそのまま表示、AmountTextだけ残数に書き換える
            if (sourceAmountText != null)
            {
                sourceAmountText.gameObject.SetActive(true);
                sourceAmountText.text = (totalAmount - dragAmount).ToString();
            }
            return;
        }

        // 全量移動の場合は従来通り非表示
        if (sourceIcon != null) sourceIcon.gameObject.SetActive(false);
        if (sourceGaugeObject != null) sourceGaugeObject.SetActive(false);
        if ((IsInventoryDrag() || IsMachineDrag()) && sourceAmountText != null)
            sourceAmountText.gameObject.SetActive(false);
    }

    void ShowSourceVisuals()
    {
        Debug.Log($"[ItemDragHandler] ShowSourceVisuals: sourceIcon={sourceIcon?.gameObject.name ?? "null"}, active前={sourceIcon?.gameObject.activeSelf}");
        if (wasHotbarDrag) return;
        if (sourceIcon != null) sourceIcon.gameObject.SetActive(true);

        if (sourceGaugeObject != null)
        {
            bool shouldShowGauge = IsInventoryDrag()
                ? IsGaugeSlot(inventorySlot)
                : (equipmentSlotData != null);
            sourceGaugeObject.SetActive(shouldShowGauge);
        }

        if ((IsInventoryDrag() || IsMachineDrag()) && sourceAmountText != null)
            sourceAmountText.gameObject.SetActive(true);
    }

    void CreateAmountText(int amount)
    {
        if (sourceAmountText != null)
        {
            GameObject cloned = Instantiate(sourceAmountText.gameObject, dragIcon.transform);
            cloned.name = "AmountText";
            cloned.SetActive(true);
            TMP_Text t = cloned.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.text = amount.ToString();
                t.raycastTarget = false;
                t.enabled = true;
                t.alpha = 1f;
                t.ForceMeshUpdate();
            }
            RectTransform rt = cloned.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                rt.anchoredPosition = new Vector2(-4f, 2f);
            }
            return;
        }

        GameObject amountObj = new GameObject("AmountText");
        amountObj.transform.SetParent(dragIcon.transform, false);
        RectTransform amountRt = amountObj.AddComponent<RectTransform>();
        amountRt.anchorMin = new Vector2(1, 0);
        amountRt.anchorMax = new Vector2(1, 0);
        amountRt.pivot = new Vector2(1, 0);
        amountRt.anchoredPosition = new Vector2(-4f, 2f);
        amountRt.sizeDelta = new Vector2(40f, 20f);
        TextMeshProUGUI amountText = amountObj.AddComponent<TextMeshProUGUI>();
        amountText.text = amount.ToString();
        if (TMP_Settings.defaultFontAsset != null)
            amountText.font = TMP_Settings.defaultFontAsset;
        amountText.fontSize = 18;
        amountText.color = Color.white;
        amountText.alignment = TextAlignmentOptions.BottomRight;
        amountText.raycastTarget = false;
        amountText.enableWordWrapping = false;
        amountText.overflowMode = TextOverflowModes.Overflow;
        amountText.enabled = true;
        amountText.alpha = 1f;
        amountText.ForceMeshUpdate();
    }

    void CreateGauge(float ratio)
    {
        if (ratio < 0f) return;

        ItemData item = GetDraggedItem();
        Color32 fillColor = item is ToolData
            ? new Color32(255, 165, 0, 255)   // オレンジ（耐久）
            : new Color32(0, 255, 0, 255);    // 緑（タンク系）

        GameObject gaugeBg = new GameObject(GetGaugeObjectName());
        gaugeBg.transform.SetParent(dragIcon.transform, false);
        Image bgImg = gaugeBg.AddComponent<Image>();
        bgImg.color = new Color32(60, 60, 60, 255);
        bgImg.raycastTarget = false;
        RectTransform bgRt = gaugeBg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0);
        bgRt.anchorMax = new Vector2(1, 0);
        bgRt.pivot = new Vector2(0, 0);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(0, 10);

        GameObject gaugeFill = new GameObject("Fill");
        gaugeFill.transform.SetParent(gaugeBg.transform, false);
        Image fillImg = gaugeFill.AddComponent<Image>();
        fillImg.color = fillColor;
        fillImg.raycastTarget = false;
        RectTransform fillRt = gaugeFill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(1, 1);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);
    }

    // -----------------------------------------------
    // 左クリックドラッグ（Unity標準EventSystem）
    // -----------------------------------------------

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (isRightClickDragging || isMiddleClickDragging) return;

        ItemData item = GetDraggedItem();
        if (item == null) return;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        canvasRect = canvas.GetComponent<RectTransform>();
        CacheSourceReferences();
        if (sourceIcon == null) return;

        RectTransform sourceRt = sourceIcon.GetComponent<RectTransform>();
        if (sourceRt == null) return;

        if (!useCustomDragAmount)
        {
            dragAmount = GetDraggedAmount();

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (IsMachineDrag() && !machineOwner.IsReadOnly)
            {
                if (shift)
                {
                    var mSlot = machineOwner.GetSlot(machineSlotIndex);
                    bool isGauge = mSlot?.item is OxygenTankData || mSlot?.item is ThrusterTankData;
                    if (!isGauge && mSlot != null)
                        dragAmount = Mathf.Max(1, mSlot.amount / 2);
                }
            }
            else if (IsInventoryDrag() && inventorySlot != null && !IsGaugeSlot(inventorySlot))
            {
                if (shift)
                    dragAmount = Mathf.Max(1, inventorySlot.amount / 2);
            }
        }

        AnyDragging = true;
        activeDragHandler = this;
        wasHotbarDrag = IsHotbarDrag();
        wasMachineDrag = IsMachineDrag();

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        RectTransform rt = dragIcon.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = sourceRt.pivot;
        rt.sizeDelta = sourceRt.sizeDelta;
        rt.localScale = Vector3.one;
        dragIconRt = rt;

        Image img = dragIcon.AddComponent<Image>();
        img.sprite = item.icon;
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = true;

        CreateAmountText(dragAmount);
        CreateGauge(GetGaugeRatio(item));
        HideSourceVisuals();
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIconRt == null || canvasRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        dragIconRt.localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        activeDragHandler = null;
        dragIconRt = null;
        AnyDragging = false;

        if (dragIcon != null) { Destroy(dragIcon); dragIcon = null; }
        else Debug.LogWarning("[ItemDragHandler] OnEndDrag: dragIcon は既にnull");

        if (wasMachineDrag)
        {
            wasMachineDrag = false;
            if (machineOwner != null)
            {
                // ドロップ拒否 → ビジュアル復元。machineOwner/machineSlotIndexは保持する
                ShowSourceVisuals();
            }
            return;
        }

        // ── インベントリ・装備・ホットバードラッグの後処理（既存） ──
        Debug.Log($"[ItemDragHandler] OnEndDrag: inventorySlot={inventorySlot?.item?.itemName ?? "null"}, wasHotbarDrag={wasHotbarDrag}");

        bool willShowSource = (inventorySlot != null || equipmentSlotData != null) && !wasHotbarDrag;
        Debug.Log($"[ItemDragHandler] ShowSourceVisuals実行予定={willShowSource}");

        if (willShowSource)
            ShowSourceVisuals();

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            Debug.Log(
                $"[ItemDragHandler] hit={result.gameObject.name}, " +
                $"drop={result.gameObject.GetComponent<DropHandler>() != null}, " +
                $"equipDrop={result.gameObject.GetComponent<EquipmentDropHandler>() != null}, " +
                $"researchDrop={result.gameObject.GetComponent<ResearchBlueprintHandler>() != null}"
            );
        }

        wasHotbarDrag = false;

        if (IsHotbarDrag())
        {
            if (hotbarUI != null) hotbarUI.RefreshAll();
            return;
        }

        if (IsInventoryDrag())
        {
            if (inventorySlot == null) return;

            bool overUI = IsPointerOverInventoryOrEquipment(eventData);
            Debug.Log($"[ItemDragHandler] OnEndDrag overUI={overUI}, item={(inventorySlot.item != null ? inventorySlot.item.itemName : "null")}, amount={inventorySlot.amount}");

            if (!overUI)
            {
                Debug.Log("[ItemDragHandler] DropToWorld 実行");
                DropToWorld();
            }
        }
    }

    // -----------------------------------------------
    // ユーティリティ
    // -----------------------------------------------

    bool IsPointerOverInventoryOrEquipment(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return IsPointerOverInventoryOrEquipment(results);
    }

    bool IsPointerOverInventoryOrEquipment(List<RaycastResult> results)
    {
        foreach (var result in results)
        {
            string n = result.gameObject.name;

            if (n == "InventoryPanel" || n == "Content" || n == "SlotPrefab(Clone)" ||
                n == "Viewport" || n == "SlotGrid" || n == "EquipmentPanel" ||
                n == "EquipmentSlotPrefab(Clone)" || n == "ResearchPanel" ||
                n == "BlueprintSlot")
                return true;

            if (result.gameObject.GetComponent<DropHandler>() != null) return true;
            if (result.gameObject.GetComponent<EquipmentDropHandler>() != null) return true;
            if (result.gameObject.GetComponent<ResearchBlueprintHandler>() != null) return true;
            // 機械・チェストパネル上もワールドドロップ不可
            if (result.gameObject.GetComponent<MachineDropHandler>() != null) return true;
        }
        return false;
    }

    void DropToWorld()
    {
        if (inventorySlot == null || inventorySlot.item == null) return;

        int spawnAmount = Mathf.Min(dragAmount, inventorySlot.amount);
        PickupSpawner.Instance.SpawnItem(
            inventorySlot.item, spawnAmount,
            inventorySlot.tankInstance,
            inventorySlot.thrusterInstance,
            inventorySlot.spacesuitInstance);

        inventory.ReduceSlot(inventorySlot, spawnAmount);
        inventoryUI.RefreshAll();
    }

    // -----------------------------------------------
    // 強制キャンセル（CloseInventory時）
    // -----------------------------------------------

    public static void CancelDrag()
    {
        if (activeDragHandler == null) return;
        activeDragHandler.ForceCancelDrag();
    }

    void ForceCancelDrag()
    {
        if (dragIcon != null) { Destroy(dragIcon); dragIcon = null; }
        dragIconRt = null;
        isRightClickDragging = false;
        isMiddleClickDragging = false;
        rightButtonHeldOnThis = false;
        middleButtonHeldOnThis = false;
        ShowSourceVisuals();
        AnyDragging = false;
        activeDragHandler = null;
        machineOwner = null;
        machineSlotIndex = -1;
        wasMachineDrag = false;
    }
}