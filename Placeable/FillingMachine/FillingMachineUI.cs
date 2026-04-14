using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FillingMachineUI : MonoBehaviour
{
    public static FillingMachineUI Instance { get; private set; }

    [Header("UI")]
    public GameObject panel;
    public Transform inputSlotsParent;
    public Transform outputSlotsParent;
    public GameObject slotPrefab;
    public Button toggleButton;
    public TextMeshProUGUI toggleButtonText;
    public Button closeButton;

    [Header("参照")]
    public InventoryUI inventoryUI;
    public TextMeshProUGUI pipeInfoText;

    [Header("液体ゲージ")]
    public Image waterGaugeFill;
    public Image oxygenGaugeFill;
    public Image hydrogenGaugeFill;
    public TextMeshProUGUI waterAmountText;
    public TextMeshProUGUI oxygenAmountText;
    public TextMeshProUGUI hydrogenAmountText;
    public float maxDisplayAmount = 50f;

    [Header("液体定義")]
    public LiquidData waterLiquid;
    public LiquidData oxygenLiquid;
    public LiquidData hydrogenLiquid;

    [Header("液体ゲージ親オブジェクト")]
    public GameObject waterGaugeRoot;
    public GameObject oxygenGaugeRoot;
    public GameObject hydrogenGaugeRoot;

    private FillingMachine currentMachine;
    public FillingMachine CurrentMachine => currentMachine;

    private List<GameObject> inputSlotObjects = new List<GameObject>();
    private List<GameObject> outputSlotObjects = new List<GameObject>();

    // 入力・出力スロットの ISlotOwner（RefreshSlots で毎回生成）
    private ISlotOwner currentInputOwner;
    private ISlotOwner currentOutputOwner;

    public bool IsOpen { get; private set; }
    private bool openedThisFrame = false;
    private bool closedThisFrame = false;
    public bool ClosedThisFrame => closedThisFrame;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        panel.SetActive(false);
        toggleButton.onClick.AddListener(OnTogglePressed);
        closeButton.onClick.AddListener(Close);
    }

    void Update()
    {
        closedThisFrame = false;
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
        if (!openedThisFrame && Input.GetKeyDown(KeyCode.E)) Close();
        openedThisFrame = false;
        UpdatePipeInfo();
    }

    public void Open(FillingMachine machine)
    {
        currentMachine = machine;
        IsOpen = true;
        openedThisFrame = true;
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (inventoryUI != null && !inventoryUI.IsOpen)
            inventoryUI.OpenInventoryExternalNoEquipment();
        machine.OnSlotsChanged += () => StartCoroutine(DelayedRefreshSlots());
        RefreshAll();
    }

    public void Close()
    {
        if (!IsOpen) return;
        closedThisFrame = true;
        if (currentMachine != null)
            currentMachine.OnSlotsChanged -= RefreshSlots;
        IsOpen = false;
        panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.CloseInventory();
        inventoryUI?.RemoveMachineHandlers();
        ClearSlots();
        currentMachine = null;
        currentInputOwner = null;
        currentOutputOwner = null;
    }

    void RefreshAll()
    {
        RefreshSlots();
        UpdateToggleButton();
    }

    public void RefreshSlots()
    {
        ClearSlots();
        if (currentMachine == null) return;

        Inventory playerInventory = inventoryUI?.inventory;

        // 入力スロット用 Owner（全アイテム受け入れ、読み書き可）
        // onChanged は同期呼び出しすると dragIcon が残るバグになるため 1 フレーム遅延する。
        currentInputOwner = new ArraySlotOwner(
            currentMachine.inputSlots,
            false,
            item => currentMachine.acceptableInputItems != null
                 && System.Array.IndexOf(currentMachine.acceptableInputItems, item) >= 0,
            () => StartCoroutine(DelayedRefreshSlots())
        );

        // 出力スロット用 Owner（読み取り専用：ドロップ不可、ドラッグのみ可）
        currentOutputOwner = new ArraySlotOwner(
            currentMachine.outputSlots,
            true,
            null,
            () => StartCoroutine(DelayedRefreshSlots())
        );

        // 入力スロット
        for (int i = 0; i < currentMachine.slotCount; i++)
        {
            GameObject obj = Instantiate(slotPrefab, inputSlotsParent);
            inputSlotObjects.Add(obj);

            Inventory.Slot slot = currentMachine.inputSlots[i];
            SetSlotVisual(obj, slot);

            int capturedIndex = i;

            // ドロップハンドラ（インベントリ/ホットバー→充填機 Input）
            MachineDropHandler drop = obj.AddComponent<MachineDropHandler>();
            drop.Init(currentInputOwner, capturedIndex, playerInventory, inventoryUI);

            // ドラッグハンドラ（充填機 Input→どこへでも）
            ItemDragHandler drag = obj.AddComponent<ItemDragHandler>();
            drag.machineOwner = currentInputOwner;
            drag.machineSlotIndex = capturedIndex;
            drag.inventory = playerInventory;
            drag.inventoryUI = inventoryUI;

            // クリックハンドラ（DC：充填機 Input→インベントリ）
            MachineSlotClickHandler click = obj.AddComponent<MachineSlotClickHandler>();
            click.Init(currentInputOwner, capturedIndex, playerInventory, inventoryUI);
        }

        // 出力スロット（読み取り専用：ドロップ不可、ドラッグのみ）
        for (int i = 0; i < currentMachine.slotCount; i++)
        {
            GameObject obj = Instantiate(slotPrefab, outputSlotsParent);
            outputSlotObjects.Add(obj);

            Inventory.Slot slot = currentMachine.outputSlots[i];
            SetSlotVisual(obj, slot);

            // ゲージ表示（充填済みタンクのゲージを表示）
            if (slot != null)
            {
                if (slot.tankInstance != null)
                    SetGauge(obj.transform, "TankSlotGauge",
                        slot.tankInstance.Ratio, new Color32(0, 255, 0, 255));
                else if (slot.waterTankInstance != null)
                    SetGauge(obj.transform, "TankSlotGauge",
                        slot.waterTankInstance.Ratio, new Color32(0, 200, 255, 255));
            }

            int capturedIndex = i;

            // ドラッグハンドラ（充填機 Output→インベントリ/ホットバー、ドロップ不可）
            ItemDragHandler drag = obj.AddComponent<ItemDragHandler>();
            drag.machineOwner = currentOutputOwner;
            drag.machineSlotIndex = capturedIndex;
            drag.inventory = playerInventory;
            drag.inventoryUI = inventoryUI;

            // クリックハンドラ（DC：充填機 Output→インベントリ、インスタンス保持）
            MachineSlotClickHandler click = obj.AddComponent<MachineSlotClickHandler>();
            click.Init(currentOutputOwner, capturedIndex, playerInventory, inventoryUI);

            // MachineDropHandler は追加しない（IsReadOnly=true で拒否するが念のため省略）
        }

        // インベントリスロットにDCハンドラを追加（DC：インベントリ→充填機 Input のみ）
        if (IsOpen) inventoryUI?.AddMachineHandlers(currentInputOwner);
    }

    void SetSlotVisual(GameObject obj, Inventory.Slot slot)
    {
        Image icon = FindChild<Image>(obj, "ItemIcon");
        TextMeshProUGUI amt = FindChild<TextMeshProUGUI>(obj, "AmountText");

        if (slot != null)
        {
            if (icon != null) { icon.sprite = slot.item.icon; icon.color = Color.white; }
            if (amt != null) amt.text = slot.amount.ToString();
        }
        else
        {
            if (icon != null) icon.color = Color.clear;
            if (amt != null) amt.text = "";
        }
    }

    /// <summary>
    /// SetSlotVisual の強化版。GameObjectのSetActive(true)も確実に行う。
    /// Drag中にHideSourceVisualsで非表示化されたスロットへの新規アイテム生成時に使用。
    /// </summary>
    void SetSlotVisualFull(GameObject obj, Inventory.Slot slot)
    {
        Image icon = FindChild<Image>(obj, "ItemIcon");
        TextMeshProUGUI amt = FindChild<TextMeshProUGUI>(obj, "AmountText");

        if (slot != null)
        {
            if (icon != null)
            {
                icon.gameObject.SetActive(true);
                icon.sprite = slot.item.icon;
                icon.color = Color.white;
            }
            if (amt != null)
            {
                amt.gameObject.SetActive(true);
                amt.text = slot.amount.ToString();
            }
        }
        else
        {
            if (icon != null) { icon.gameObject.SetActive(true); icon.color = Color.clear; }
            if (amt != null) { amt.gameObject.SetActive(true); amt.text = ""; }
        }
    }

    void SetGauge(Transform root, string gaugeName, float ratio, Color32 color)
    {
        Transform gaugeT = null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == gaugeName) { gaugeT = child; break; }
        if (gaugeT == null) return;

        if (ratio <= 0f) { gaugeT.gameObject.SetActive(false); return; }
        gaugeT.gameObject.SetActive(true);

        Transform fill = gaugeT.Find("Fill");
        if (fill != null)
        {
            Image fillImg = fill.GetComponent<Image>();
            if (fillImg != null)
            {
                fillImg.color = color;
                fillImg.rectTransform.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);
            }
        }
    }

    /// <summary>指定名のゲージ子オブジェクトを非表示にする（非アクティブ含めて検索）。</summary>
    void DisableGauge(Transform root, string gaugeName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == gaugeName) { child.gameObject.SetActive(false); break; }
        }
    }

    void UpdateToggleButton()
    {
        if (currentMachine == null) return;
        toggleButtonText.text = currentMachine.IsOn ? "ON" : "OFF";
        UpdatePipeInfo();
    }

    void UpdatePipeInfo()
    {
        if (currentMachine == null) return;

        if (pipeInfoText != null)
            pipeInfoText.text = currentMachine.inletConnector.IsConnected
                ? "パイプ：接続済"
                : "パイプ：未接続（蓄積量で動作可）";

        float w = currentMachine.storedWater;
        float o = currentMachine.storedOxygen;
        float h = currentMachine.storedHydrogen;

        bool hasAny = w > 0f || o > 0f || h > 0f;
        bool showWater = hasAny && w >= o && w >= h;
        bool showOxygen = hasAny && !showWater && o >= h;
        bool showHydrogen = hasAny && !showWater && !showOxygen;

        if (waterGaugeRoot != null) waterGaugeRoot.SetActive(showWater);
        if (oxygenGaugeRoot != null) oxygenGaugeRoot.SetActive(showOxygen);
        if (hydrogenGaugeRoot != null) hydrogenGaugeRoot.SetActive(showHydrogen);

        if (showWater) UpdateGauge(waterGaugeFill, waterAmountText, w);
        if (showOxygen) UpdateGauge(oxygenGaugeFill, oxygenAmountText, o);
        if (showHydrogen) UpdateGauge(hydrogenGaugeFill, hydrogenAmountText, h);
    }

    void UpdateGauge(Image fill, TextMeshProUGUI text, float value)
    {
        if (fill != null)
            fill.rectTransform.localScale = new Vector3(
                Mathf.Clamp01(value / maxDisplayAmount), 1f, 1f);
        if (text != null)
            text.text = $"{value:F1}L";
    }

    void OnTogglePressed()
    {
        if (currentMachine == null) return;
        currentMachine.SetOn(!currentMachine.IsOn);
        UpdateToggleButton();
    }

    void ClearSlots()
    {
        foreach (var obj in inputSlotObjects) if (obj != null) Destroy(obj);
        foreach (var obj in outputSlotObjects) if (obj != null) Destroy(obj);
        inputSlotObjects.Clear();
        outputSlotObjects.Clear();
    }

    System.Collections.IEnumerator DelayedRefreshSlots()
    {
        yield return null;
        if (ItemDragHandler.AnyDragging)
            RefreshSlotsVisualOnly();
        else
            RefreshSlots();
    }

    void RefreshSlotsVisualOnly()
    {
        if (currentMachine == null) return;

        // 入力スロット
        for (int i = 0; i < currentMachine.slotCount && i < inputSlotObjects.Count; i++)
        {
            if (inputSlotObjects[i] != null)
                SetSlotVisual(inputSlotObjects[i], currentMachine.inputSlots[i]);
        }

        // 出力スロット
        for (int i = 0; i < currentMachine.slotCount && i < outputSlotObjects.Count; i++)
        {
            if (outputSlotObjects[i] == null) continue;

            var slot = currentMachine.outputSlots[i];

            // Drag中のスロットに新規アイテムが生成された場合の判定
            bool isDraggedSlot = ItemDragHandler.ActiveDragOwner == currentOutputOwner
                              && ItemDragHandler.ActiveDragSlotIndex == i;

            if (isDraggedSlot && slot != null)
            {
                // HideSourceVisuals で SetActive(false) したアイコン・テキスト・ゲージを全て復活させる。
                // 通常の SetSlotVisual は color/sprite しか変更せず SetActive(true) を呼ばないため
                // ここで明示的に再アクティブ化する。
                SetSlotVisualFull(outputSlotObjects[i], slot);
            }
            else
            {
                SetSlotVisual(outputSlotObjects[i], slot);
            }

            // ゲージ表示
            if (slot?.tankInstance != null)
                SetGauge(outputSlotObjects[i].transform, "TankSlotGauge",
                    slot.tankInstance.Ratio, new Color32(0, 255, 0, 255));
            else if (slot?.waterTankInstance != null)
                SetGauge(outputSlotObjects[i].transform, "TankSlotGauge",
                    slot.waterTankInstance.Ratio, new Color32(0, 200, 255, 255));
            else
                DisableGauge(outputSlotObjects[i].transform, "TankSlotGauge");
        }
    }

    T FindChild<T>(GameObject root, string name) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<T>();
        return null;
    }
}