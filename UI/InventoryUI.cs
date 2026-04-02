using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject inventoryPanel;
    public Transform contentParent;
    public GameObject slotPrefab;

    [Header("参照")]
    public Inventory inventory;
    public EquipmentUI equipmentUI;
    public OxygenSystem oxygenSystem;
    public VitalSystem vitalSystem;
    public EquipmentSystem equipmentSystem;
    public SplitWindowUI splitWindowUI;
    public int slotCount = 20;

    private bool isOpen = false;
    private List<GameObject> slotObjects = new List<GameObject>();
    private bool blockUpdate = false;

    public bool IsOpen => isOpen;
    private int lastSlotCount = 0;

    void Update()
    {
        bool isGameOver = (oxygenSystem != null && oxygenSystem.IsGameOver)
                       || (vitalSystem != null && vitalSystem.IsDead);
        if (isGameOver)
        {
            if (isOpen) CloseInventory();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isOpen)
            {
                CloseInventory();
            }
            else
            {
                // 他UIが開いていれば全部閉じてからインベントリを開く
                if (UIManager.Instance != null) UIManager.Instance.OpenInventory();
                else OpenInventory();
            }
        }

        if (isOpen)
        {
            int currentSlotCount = inventory.GetSlots().Length;
            if (currentSlotCount != lastSlotCount)
            {
                lastSlotCount = currentSlotCount;
                RefreshAll();
            }
            else
            {
                UpdateValues();
            }
        }
    }

    void OpenInventory()
    {
        isOpen = true;
        lastSlotCount = inventory.GetSlots().Length;
        inventoryPanel.SetActive(true);
        equipmentUI.equipmentPanel.SetActive(true);
        if (splitWindowUI != null) splitWindowUI.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshAll();
    }

    public void OpenInventoryExternal()
    {
        if (isOpen) return;
        OpenInventory();
    }

    public void CloseInventory()
    {
        ItemDragHandler.CancelDrag();
        splitWindowUI?.Deselect();
        if (splitWindowUI != null) splitWindowUI.gameObject.SetActive(false);
        isOpen = false;
        inventoryPanel.SetActive(false);
        equipmentUI.equipmentPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var tables = FindObjectsOfType<ResearchTableSystem>();
        foreach (var table in tables)
        {
            if (table != null && table.IsOpen)
                table.ClosePanel();
        }
    }

    public void RefreshAll()
    {
        StartCoroutine(BlockUpdateNextFrame());
        RefreshInventory();
        equipmentUI.RefreshUI();
        splitWindowUI?.Validate();

        System.Collections.IEnumerator BlockUpdateNextFrame()
        {
            blockUpdate = true;
            yield return null;
            yield return null;
            blockUpdate = false;
        }
    }

    public void UpdateValues()
    {
        if (blockUpdate || ItemDragHandler.AnyDragging) return;
        Inventory.Slot[] slots = inventory.GetSlots();

        if (slotObjects.Count != slots.Length)
        {
            RefreshAll();
            return;
        }

        for (int i = 0; i < slotObjects.Count; i++)
        {
            Inventory.Slot slot = slots[i];
            if (slot == null) continue;

            GameObject slotObj = slotObjects[i];

            if (slot.item is OxygenTankData && slot.tankInstance != null)
                SetGauge(slotObj.transform, "TankSlotGauge",
                    slot.tankInstance.Ratio, new Color32(0, 255, 0, 255));
            else if (slot.item is ThrusterTankData && slot.thrusterInstance != null)
                SetGauge(slotObj.transform, "TankSlotGauge",
                    slot.thrusterInstance.Ratio, new Color32(0, 255, 0, 255));
            else if (slot.item is SpacesuitData && slot.spacesuitInstance != null)
                SetGauge(slotObj.transform, "TankSlotGauge",
                    slot.spacesuitInstance.Ratio, new Color32(0, 255, 0, 255));
            else if (slot.item is ToolData && slot.toolInstance != null)
                SetGauge(slotObj.transform, "TankSlotGauge",
                    slot.toolInstance.Ratio, new Color32(255, 165, 0, 255));
        }

        equipmentUI.UpdateValues();
    }

    Image FindImage(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<Image>();
        return null;
    }

    TextMeshProUGUI FindText(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<TextMeshProUGUI>();
        return null;
    }

    void RefreshInventory()
    {
        foreach (var obj in slotObjects)
            Destroy(obj);
        slotObjects.Clear();

        Inventory.Slot[] slots = inventory.GetSlots();

        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, contentParent);
            slotObjects.Add(slotObj);

            Inventory.Slot slot = slots[i];

            if (slot != null)
            {
                Image icon = FindImage(slotObj.transform, "ItemIcon");
                if (icon != null)
                {
                    if (slot.item.icon != null) { icon.sprite = slot.item.icon; icon.color = Color.white; }
                    else icon.color = Color.clear;
                }

                TextMeshProUGUI amount = FindText(slotObj.transform, "AmountText");
                if (amount != null) amount.text = slot.amount.ToString();

                if (slot.item is OxygenTankData && slot.tankInstance != null)
                    SetGauge(slotObj.transform, "TankSlotGauge",
                        slot.tankInstance.Ratio, new Color32(0, 255, 0, 255));
                else if (slot.item is ThrusterTankData && slot.thrusterInstance != null)
                    SetGauge(slotObj.transform, "TankSlotGauge",
                        slot.thrusterInstance.Ratio, new Color32(0, 255, 0, 255));
                else if (slot.item is SpacesuitData && slot.spacesuitInstance != null)
                    SetGauge(slotObj.transform, "TankSlotGauge",
                        slot.spacesuitInstance.Ratio, new Color32(0, 255, 0, 255));
                else if (slot.item is ToolData && slot.toolInstance != null)
                    SetGauge(slotObj.transform, "TankSlotGauge",
                        slot.toolInstance.Ratio, new Color32(255, 165, 0, 255));
                else
                {
                    foreach (Transform child in slotObj.GetComponentsInChildren<Transform>(true))
                        if (child.name == "TankSlotGauge") { child.gameObject.SetActive(false); break; }
                }

                // クリックハンドラ
                SlotClickHandler handler = slotObj.AddComponent<SlotClickHandler>();
                handler.inventorySlot = slot;
                handler.inventoryUI = this;
                handler.equipmentSystem = equipmentSystem;
                handler.inventory = inventory;
                handler.splitWindowUI = splitWindowUI;

                // ドラッグハンドラ
                ItemDragHandler dragHandler = slotObj.AddComponent<ItemDragHandler>();
                dragHandler.inventorySlot = slot;
                dragHandler.inventory = inventory;
                dragHandler.inventoryUI = this;
                dragHandler.equipmentSystem = equipmentSystem;
                dragHandler.equipmentUI = equipmentUI;

                // ドロップハンドラ（アイテムあり）
                DropHandler dropHandler = slotObj.AddComponent<DropHandler>();
                dropHandler.targetSlot = slot;
                dropHandler.targetIndex = i;
                dropHandler.inventory = inventory;
                dropHandler.inventoryUI = this;
            }
            else
            {
                Image icon = FindImage(slotObj.transform, "ItemIcon");
                if (icon != null) icon.color = Color.clear;

                TextMeshProUGUI amount = FindText(slotObj.transform, "AmountText");
                if (amount != null) amount.text = "";

                foreach (Transform child in slotObj.GetComponentsInChildren<Transform>(true))
                    if (child.name == "TankSlotGauge") { child.gameObject.SetActive(false); break; }

                Image slotImage = slotObj.GetComponent<Image>();
                if (slotImage != null) slotImage.raycastTarget = true;

                // ドロップハンドラ（空スロット）
                DropHandler dropHandler = slotObj.AddComponent<DropHandler>();
                dropHandler.targetSlot = null;
                dropHandler.targetIndex = i;
                dropHandler.inventory = inventory;
                dropHandler.inventoryUI = this;
            }
        }
    }

    void SetGauge(Transform root, string gaugeName, float ratio, Color color)
    {
        Transform gaugeTransform = null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == gaugeName) { gaugeTransform = child; break; }

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
                fillImage.rectTransform.localScale = new Vector3(ratio, 1f, 1f);
            }
        }
    }

    public void AddIceMelterHandlers(IceMelter machine, IceMelterUI ui)
    {
        var slots = inventory.GetSlots();
        for (int i = 0; i < slotObjects.Count && i < slots.Length; i++)
        {
            if (slotObjects[i] == null) continue;
            var handler = slotObjects[i].AddComponent<IceMelterInventorySlotClickHandler>();
            handler.Init(machine, inventory, i, ui);
        }
    }

    public void RemoveIceMelterHandlers()
    {
        foreach (var obj in slotObjects)
        {
            if (obj == null) continue;
            var h = obj.GetComponent<IceMelterInventorySlotClickHandler>();
            if (h != null) Destroy(h);
        }
    }

    public void OpenInventoryExternalNoEquipment()
    {
        if (isOpen) return;
        isOpen = true;
        lastSlotCount = inventory.GetSlots().Length;
        inventoryPanel.SetActive(true);
        // equipmentUIは開かない
        if (splitWindowUI != null) splitWindowUI.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshAll();
    }
}