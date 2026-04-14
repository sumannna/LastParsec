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
    // 機械スロット用フィールド
    // -----------------------------------------------
    [HideInInspector] public ISlotOwner machineOwner;
    [HideInInspector] public int machineSlotIndex = -1;

    // OnDrop → OnEndDrag の順で呼ばれるため、OnDrop時点でhotbarSlotがnullになる。
    private bool wasHotbarDrag = false;
    private bool wasMachineDrag = false;

    [HideInInspector] public int dragAmount;
    [HideInInspector] public bool useCustomDragAmount = false;

    private GameObject dragIcon;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Image sourceIcon;
    private RectTransform dragIconRt;
    private GameObject sourceGaugeObject;
    private TMP_Text sourceAmountText;

    public static bool AnyDragging { get; private set; }
    public static ISlotOwner ActiveDragOwner { get; private set; }
    public static int ActiveDragSlotIndex { get; private set; } = -1;
    public static int ActiveDragAmount { get; private set; } = 0;
    private static ItemDragHandler activeDragHandler;

    // -----------------------------------------------
    // Drag開始時の元スロット情報（マルチプレイ対応）
    // -----------------------------------------------

    /// <summary>インベントリDrag開始時の元配列インデックス。外部参照用（EquipmentDropHandler等）。</summary>
    public int InventorySourceIndex => inventorySourceIndex;
    private int inventorySourceIndex = -1;

    /// <summary>ホットバーDrag時のスナップショット（スロットクリア後もデータ保持）。DropHandlerからもアクセス可。</summary>
    [HideInInspector] public Hotbar.Slot hotbarSlotSnapshot;

    /// <summary>機械スロットDrag時のスナップショット（全種対応）。</summary>
    private Inventory.Slot draggedSlotSnapshot;

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
    // 右クリック・ホイールクリック検出
    // -----------------------------------------------

    void Update()
    {
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
                BeginSpecialDrag(dragDivisor: 3);
            }
        }

        if (Input.GetMouseButtonUp(2))
        {
            middleButtonHeldOnThis = false;
            if (isMiddleClickDragging) EndSpecialDrag(ref isMiddleClickDragging);
        }

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
    // ソース判定
    // -----------------------------------------------

    bool IsInventoryDrag() => inventorySlot != null;
    bool IsHotbarDrag() => hotbarSlot != null;
    public bool IsMachineDrag() => machineOwner != null && machineSlotIndex >= 0;

    // -----------------------------------------------
    // ドラッグ元スロット取得（スナップショット対応）
    // -----------------------------------------------

    /// <summary>機械スロットのドラッグデータ。スナップショットがあればそれを、なければ実データを返す。</summary>
    public Inventory.Slot GetDraggedMachineSlot()
        => draggedSlotSnapshot ?? machineOwner?.GetSlot(machineSlotIndex);

    /// <summary>ホットバーのドラッグデータ。スナップショットがあればそれを、なければ実スロットを返す。</summary>
    public Hotbar.Slot GetDraggedHotbarSlot()
        => hotbarSlotSnapshot ?? hotbarSlot;

    // -----------------------------------------------
    // 右クリック・ホイールクリック共通ドラッグ
    // -----------------------------------------------

    void BeginSpecialDrag(int dragDivisor)
    {
        ItemData item = null;
        int amount = 0;

        if (IsMachineDrag())
        {
            if (machineOwner.IsReadOnly) return;
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
        wasHotbarDrag = IsHotbarDrag();
        wasMachineDrag = IsMachineDrag();
        ActiveDragOwner = IsMachineDrag() ? machineOwner : null;
        ActiveDragSlotIndex = IsMachineDrag() ? machineSlotIndex : -1;
        ActiveDragAmount = dragAmount;

        // ── Drag開始時に元スロットからデータを除去（マルチプレイ対応）
        ApplyDragSourceRemoval();

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
        ActiveDragOwner = null;
        ActiveDragSlotIndex = -1;
        ActiveDragAmount = 0;

        if (dragIcon != null) { Destroy(dragIcon); dragIcon = null; }

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

        // ── Drop後の後処理
        if (wasMachineDrag)
        {
            wasMachineDrag = false;
            if (machineOwner != null)
                ShowSourceVisuals();
            draggedSlotSnapshot = null;
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
    // Drag開始時の元スロットデータ除去（マルチプレイ対応の核心）
    // -----------------------------------------------

    /// <summary>
    /// Drag開始時に元スロットからdragAmount分を除去する。
    /// インベントリ：フルDragのみ配列から除去（部分Dragはデータ変更なし）。
    /// ホットバー：常にスナップショット保存＋スロットクリア。
    /// 機械（全種）：常にスナップショット保存＋除去。
    /// </summary>
    void ApplyDragSourceRemoval()
    {
        if (IsInventoryDrag() && inventorySlot != null && inventory != null)
        {
            // インベントリ元インデックスを記録
            var slots = inventory.GetSlots();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == inventorySlot) { inventorySourceIndex = i; break; }
            }

            if (inventorySourceIndex >= 0 && dragAmount >= inventorySlot.amount)
            {
                // フルDrag：配列から除去（amountは変更しない）
                slots[inventorySourceIndex] = null;
            }
            // 部分Drag：データ変更なし（HideSourceVisualsで残数表示を更新）
        }
        else if (IsHotbarDrag() && hotbarSlot != null && hotbar != null)
        {
            // ホットバーは常にフルDrag → スナップショット保存＋クリア
            hotbarSlotSnapshot = new Hotbar.Slot();
            hotbarSlotSnapshot.item = hotbarSlot.item;
            hotbarSlotSnapshot.amount = hotbarSlot.amount;
            hotbarSlotSnapshot.toolInstance = hotbarSlot.toolInstance;
            hotbarSlotSnapshot.tankInstance = hotbarSlot.tankInstance;
            hotbarSlotSnapshot.thrusterInstance = hotbarSlot.thrusterInstance;
            hotbarSlotSnapshot.waterTankInstance = hotbarSlot.waterTankInstance;
            hotbar.ClearSlot(hotbarIndex);
        }
        else if (IsMachineDrag() && machineOwner != null)
        {
            // 機械スロット（全種）：スナップショット保存＋除去
            var srcSlot = machineOwner.GetSlot(machineSlotIndex);
            if (srcSlot != null)
            {
                draggedSlotSnapshot = new Inventory.Slot(srcSlot.item, dragAmount);
                draggedSlotSnapshot.tankInstance = srcSlot.tankInstance;
                draggedSlotSnapshot.thrusterInstance = srcSlot.thrusterInstance;
                draggedSlotSnapshot.waterTankInstance = srcSlot.waterTankInstance;

                if (srcSlot.amount <= dragAmount)
                    machineOwner.SetSlot(machineSlotIndex, null);
                else
                    srcSlot.amount -= dragAmount;

                // ここでは NotifyChanged() を呼ばない。
                // 理由：ChestUI等のonChangedがRefreshAll→ClearSlots→Destroyを1フレーム後に
                // 実行し、ドラッグ元GameObjectを破棄してOnEndDragが呼ばれなくなるため。
                // 実データは既に配列から除去済み。UIへの通知はDrop成功・失敗後に各DropHandlerが行う。
            }
        }
    }

    // -----------------------------------------------
    // Drop失敗時の元スロット復元
    // -----------------------------------------------

    /// <summary>インベントリスロットをDrag前の状態に復元する。</summary>
    void RestoreInventorySlot()
    {
        if (inventorySlot == null || inventory == null || inventorySourceIndex < 0) return;

        var slots = inventory.GetSlots();
        if (inventorySourceIndex >= slots.Length) return;

        if (slots[inventorySourceIndex] == null)
        {
            // フルDrag：配列から除去されていた → 元のインデックスへ戻す
            slots[inventorySourceIndex] = inventorySlot;
        }
        else if (slots[inventorySourceIndex] != inventorySlot)
        {
            // 元インデックスが別アイテムで埋まっている（マルチプレイ競合）→ 空き探して配置
            inventory.PlaceSlotInInventory(inventorySlot, -1);
        }
        // slots[inventorySourceIndex] == inventorySlot: 部分Drag。
        // ApplyDragSourceRemovalでamountを減算していないため、復元は不要（no-op）。

        inventorySourceIndex = -1;
        inventoryUI?.RefreshAll();
    }

    /// <summary>ホットバースロットをDrag前の状態に復元する。</summary>
    void RestoreHotbarSlot()
    {
        if (hotbarSlotSnapshot == null || hotbar == null) return;

        Hotbar.Slot actual = hotbar.GetSlot(hotbarIndex);
        if (actual == null) { hotbarSlotSnapshot = null; return; }

        actual.item = hotbarSlotSnapshot.item;
        actual.amount = hotbarSlotSnapshot.amount;
        actual.toolInstance = hotbarSlotSnapshot.toolInstance;
        actual.tankInstance = hotbarSlotSnapshot.tankInstance;
        actual.thrusterInstance = hotbarSlotSnapshot.thrusterInstance;
        actual.waterTankInstance = hotbarSlotSnapshot.waterTankInstance;

        hotbarSlotSnapshot = null;
        hotbarUI?.RefreshAll();
    }

    /// <summary>
    /// 機械スロットをDrag前の状態に復元する。
    /// preferredIndex：-1 なら空きスロットを探す（出力スロット用）、
    ///                 >= 0 なら元インデックスを優先（入力スロット用）。
    /// </summary>
    void RestoreDraggedSlot(int preferredIndex = -1)
    {
        if (draggedSlotSnapshot == null || draggedSlotSnapshot.amount <= 0 || machineOwner == null)
        {
            draggedSlotSnapshot = null;
            return;
        }

        var existingAtPreferred = (preferredIndex >= 0)
            ? machineOwner.GetSlot(preferredIndex) : null;

        if (existingAtPreferred == null && preferredIndex >= 0)
        {
            // 元インデックスが空き → 直接戻す
            machineOwner.SetSlot(preferredIndex, draggedSlotSnapshot);
        }
        else if (existingAtPreferred != null
                 && existingAtPreferred.item == draggedSlotSnapshot.item
                 && existingAtPreferred.amount + draggedSlotSnapshot.amount <= existingAtPreferred.item.maxStack)
        {
            // 部分Drag復元：同種アイテムが残っている → マージ
            existingAtPreferred.amount += draggedSlotSnapshot.amount;
        }
        else
        {
            // 元インデックスが別アイテムで埋まっている or 出力スロット → 空き探して配置
            int remaining = draggedSlotSnapshot.amount;
            ItemData item = draggedSlotSnapshot.item;

            for (int i = 0; i < machineOwner.SlotCount && remaining > 0; i++)
            {
                var s = machineOwner.GetSlot(i);
                if (s != null && s.item == item && s.amount < item.maxStack)
                {
                    int add = Mathf.Min(item.maxStack - s.amount, remaining);
                    s.amount += add;
                    remaining -= add;
                }
            }
            for (int i = 0; i < machineOwner.SlotCount && remaining > 0; i++)
            {
                if (machineOwner.GetSlot(i) == null)
                {
                    int add = Mathf.Min(item.maxStack, remaining);
                    var newSlot = new Inventory.Slot(item, add);
                    newSlot.tankInstance = draggedSlotSnapshot.tankInstance;
                    newSlot.thrusterInstance = draggedSlotSnapshot.thrusterInstance;
                    newSlot.waterTankInstance = draggedSlotSnapshot.waterTankInstance;
                    machineOwner.SetSlot(i, newSlot);
                    remaining -= add;
                }
            }
        }

        draggedSlotSnapshot = null;
        machineOwner.NotifyChanged();
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
        if (IsMachineDrag()) return GetDraggedMachineSlot()?.item;
        if (IsInventoryDrag()) return inventorySlot.item;
        if (IsHotbarDrag()) return GetDraggedHotbarSlot()?.item;
        if (equipmentSystem != null && equipmentSlotData != null)
            return equipmentSystem.GetEquipped(equipmentSlotData);
        return null;
    }

    float GetGaugeRatio(ItemData item)
    {
        if (item == null) return -1f;

        if (IsMachineDrag())
        {
            var mSlot = GetDraggedMachineSlot();
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
            var hSlot = GetDraggedHotbarSlot();
            if (item is ToolData && hSlot?.toolInstance != null)
                return hSlot.toolInstance.Ratio;
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
        if (IsMachineDrag())
        {
            // ApplyDragSourceRemoval で実スロットの amount を削減済みのため、
            // スナップショット（dragAmount 分）＋実スロット残量 = 元の総量 を返す。
            // HideSourceVisuals で再度 dragAmount を引くことで正しい残数が表示される。
            if (draggedSlotSnapshot != null)
            {
                int remaining = machineOwner.GetSlot(machineSlotIndex)?.amount ?? 0;
                return draggedSlotSnapshot.amount + remaining;
            }
            return machineOwner.GetSlot(machineSlotIndex)?.amount ?? 0;
        }
        if (IsInventoryDrag()) return inventorySlot.amount;
        if (IsHotbarDrag()) return hotbarSlot.amount;
        return 1;
    }

    // -----------------------------------------------
    // ビジュアル制御
    // -----------------------------------------------

    public void HideSourceVisuals()
    {
        int totalAmount = GetDraggedAmount();
        bool isPartialDrag = dragAmount > 0 && dragAmount < totalAmount;

        if (isPartialDrag)
        {
            if (sourceAmountText != null)
            {
                sourceAmountText.gameObject.SetActive(true);
                sourceAmountText.text = (totalAmount - dragAmount).ToString();
            }
            return;
        }

        if (sourceIcon != null) sourceIcon.gameObject.SetActive(false);
        if (sourceGaugeObject != null) sourceGaugeObject.SetActive(false);
        if ((IsInventoryDrag() || IsMachineDrag()) && sourceAmountText != null)
            sourceAmountText.gameObject.SetActive(false);
    }

    void ShowSourceVisuals()
    {
        Debug.Log($"[ItemDragHandler] ShowSourceVisuals");

        // ── ホットバー：スナップショットから復元
        if (wasHotbarDrag)
        {
            RestoreHotbarSlot();
            return;
        }

        // ── 機械スロット（全種）：スナップショットから復元
        if (IsMachineDrag() && machineOwner != null && draggedSlotSnapshot != null)
        {
            int preferred = machineOwner.IsReadOnly ? -1 : machineSlotIndex;
            RestoreDraggedSlot(preferred);
            return; // UIはNotifyChangedで更新
        }

        // ── インベントリ：配列へ戻す
        if (IsInventoryDrag() && inventorySlot != null && inventorySourceIndex >= 0)
        {
            RestoreInventorySlot();
            return; // UIはRefreshAllで更新
        }

        // ── 装備スロット等：ビジュアルのみ復元
        if (sourceIcon != null) sourceIcon.gameObject.SetActive(true);

        if (sourceGaugeObject != null)
        {
            bool shouldShowGauge;
            if (IsInventoryDrag())
                shouldShowGauge = IsGaugeSlot(inventorySlot);
            else if (equipmentSlotData != null)
                shouldShowGauge = true;
            else
                shouldShowGauge = false;
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
            ? new Color32(255, 165, 0, 255)
            : new Color32(0, 255, 0, 255);

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
        ActiveDragOwner = IsMachineDrag() ? machineOwner : null;
        ActiveDragSlotIndex = IsMachineDrag() ? machineSlotIndex : -1;
        ActiveDragAmount = dragAmount;

        // ── Drag開始時に元スロットからデータを除去（マルチプレイ対応）
        ApplyDragSourceRemoval();

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
        ActiveDragOwner = null;
        ActiveDragSlotIndex = -1;
        ActiveDragAmount = 0;

        if (dragIcon != null) { Destroy(dragIcon); dragIcon = null; }
        else Debug.LogWarning("[ItemDragHandler] OnEndDrag: dragIcon は既にnull");

        // ── 機械スロット
        if (wasMachineDrag)
        {
            wasMachineDrag = false;
            if (machineOwner != null)
                ShowSourceVisuals(); // Drop失敗 → 復元
            draggedSlotSnapshot = null;
            return;
        }

        // ── インベントリ・装備・ホットバー
        Debug.Log($"[ItemDragHandler] OnEndDrag: inventorySlot={inventorySlot?.item?.itemName ?? "null"}, wasHotbarDrag={wasHotbarDrag}");

        // インベントリ・装備のビジュアル復元（Drop失敗時）
        bool willShowSource = (inventorySlot != null || equipmentSlotData != null) && !wasHotbarDrag;
        if (willShowSource)
            ShowSourceVisuals();

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            Debug.Log($"[ItemDragHandler] hit={result.gameObject.name}");
        }

        // ── ホットバーDragの後処理（成功・失敗問わず実行）
        if (wasHotbarDrag)
        {
            wasHotbarDrag = false;
            if (IsHotbarDrag())
            {
                // Drop失敗（hotbarSlot != null）: スナップショットから復元
                if (hotbarSlotSnapshot != null)
                    RestoreHotbarSlot();
            }
            // 成功・失敗問わず常にクリア
            hotbarSlotSnapshot = null;
            if (hotbarUI != null) hotbarUI.RefreshAll();
            return;
        }
        wasHotbarDrag = false;

        // ── インベントリDragの後処理
        if (IsInventoryDrag())
        {
            if (inventorySlot == null) return;

            bool overUI = IsPointerOverInventoryOrEquipment(eventData);
            Debug.Log($"[ItemDragHandler] OnEndDrag overUI={overUI}");

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

        // フルDragの場合スロットは配列外なのでReduceSlotは不要（amountを直接減じるだけ）
        inventorySlot.amount -= spawnAmount;
        if (inventorySlot.amount <= 0)
            inventorySlot = null; // GCに任せる

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

        // 全スロット種別の復元
        ShowSourceVisuals(); // 機械・インベントリの復元はここで実行

        // ホットバーは ShowSourceVisuals の wasHotbarDrag 分岐で処理されるが、
        // ForceCancelDrag 時は wasHotbarDrag が false のまま呼ばれる場合があるため明示的に復元
        if (hotbarSlotSnapshot != null)
            RestoreHotbarSlot();

        AnyDragging = false;
        activeDragHandler = null;

        machineOwner = null;
        machineSlotIndex = -1;
        wasMachineDrag = false;
        ActiveDragOwner = null;
        ActiveDragSlotIndex = -1;
        ActiveDragAmount = 0;
        draggedSlotSnapshot = null;

        inventorySourceIndex = -1;
        hotbarSlotSnapshot = null;
    }
}