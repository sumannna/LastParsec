using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject equipmentPanel;
    public Transform slotsParent;
    public GameObject equipmentSlotPrefab;

    [Header("参照")]
    public EquipmentSystem equipmentSystem;
    public Inventory inventory;
    public InventoryUI inventoryUI;

    private List<GameObject> slotObjects = new List<GameObject>();

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

    void SetGauge(Transform root, string gaugeName, float ratio)
    {
        Transform gaugeTransform = null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == gaugeName) { gaugeTransform = child; break; }

        if (gaugeTransform == null) return;

        if (ratio <= 0f)
        {
            gaugeTransform.gameObject.SetActive(false);
            return;
        }

        gaugeTransform.gameObject.SetActive(true);

        Transform fill = gaugeTransform.Find("Fill");
        if (fill != null)
        {
            Image fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
                fillImage.rectTransform.localScale = new Vector3(ratio, 1f, 1f);
        }
    }

    public void RefreshUI()
    {
        foreach (var obj in slotObjects)
            Destroy(obj);
        slotObjects.Clear();

        foreach (var (slot, item, tank) in equipmentSystem.GetAllSlots())
        {
            GameObject slotObj = Instantiate(equipmentSlotPrefab, slotsParent);
            slotObjects.Add(slotObj);

            TextMeshProUGUI slotName = FindText(slotObj.transform, "SlotName");
            if (slotName != null) slotName.text = slot.slotName;

            Image icon = FindImage(slotObj.transform, "ItemIcon");
            TextMeshProUGUI itemName = FindText(slotObj.transform, "ItemName");

            if (item != null)
            {
                if (itemName != null) itemName.text = item.itemName;
                if (icon != null)
                {
                    if (item.icon != null)
                    {
                        icon.sprite = item.icon;
                        icon.color = Color.white;
                    }
                    else icon.color = Color.clear;
                }

                if (item is OxygenTankData && tank != null)
                    SetGauge(slotObj.transform, "TankGauge", tank.Ratio);
                else if (item is ThrusterTankData)
                {
                    var thrusterSlot = equipmentSystem.GetSlotByType(ItemType.ThrusterTank);
                    var thrusterTank = equipmentSystem.GetThrusterInstance(thrusterSlot);
                    SetGauge(slotObj.transform, "TankGauge",
                        thrusterTank != null ? thrusterTank.Ratio : 0f);
                }
                else if (item is SpacesuitData)
                {
                    var suitSlot = equipmentSystem.GetSlotByType(ItemType.Spacesuit);
                    var suitInst = equipmentSystem.GetSpacesuitInstance(suitSlot);
                    SetGauge(slotObj.transform, "TankGauge",
                        suitInst != null ? suitInst.Ratio : 0f);
                }
                else
                {
                    Transform gaugeTransform = null;
                    foreach (Transform child in slotObj.GetComponentsInChildren<Transform>(true))
                        if (child.name == "TankGauge") { gaugeTransform = child; break; }
                    if (gaugeTransform != null)
                        gaugeTransform.gameObject.SetActive(false);
                }
            }
            else
            {
                if (itemName != null) itemName.text = "未装備";
                if (icon != null)
                {
                    if (slot.slotIcon != null)
                    {
                        icon.sprite = slot.slotIcon;
                        icon.color = new Color(1, 1, 1, 0.3f);
                    }
                    else icon.color = Color.clear;
                }

                Transform gaugeTransform = null;
                foreach (Transform child in slotObj.GetComponentsInChildren<Transform>(true))
                    if (child.name == "TankGauge") { gaugeTransform = child; break; }
                if (gaugeTransform != null)
                    gaugeTransform.gameObject.SetActive(false);
            }

            // クリックハンドラ
            SlotClickHandler handler = slotObj.AddComponent<SlotClickHandler>();
            handler.equipmentSlotData = slot;
            handler.equipmentUI = this;
            handler.equipmentSystem = equipmentSystem;
            handler.inventory = inventory;
            handler.inventoryUI = inventoryUI;

            // ドラッグハンドラ
            ItemDragHandler dragHandler = slotObj.AddComponent<ItemDragHandler>();
            dragHandler.equipmentSlotData = slot;
            dragHandler.equipmentSystem = equipmentSystem;
            dragHandler.inventory = inventory;
            dragHandler.inventoryUI = inventoryUI;
            dragHandler.equipmentUI = this;

            // ドロップハンドラ
            EquipmentDropHandler equipDropHandler = slotObj.AddComponent<EquipmentDropHandler>();
            equipDropHandler.slotData = slot;
            equipDropHandler.equipmentSystem = equipmentSystem;
            equipDropHandler.inventory = inventory;
            equipDropHandler.inventoryUI = inventoryUI;
        }
    }

    public void UpdateValues()
    {
        if (ItemDragHandler.AnyDragging) return;

        var allSlots = equipmentSystem.GetAllSlots();

        if (slotObjects.Count != allSlots.Count)
        {
            RefreshUI();
            return;
        }

        for (int i = 0; i < slotObjects.Count; i++)
        {
            var (slot, item, tank) = allSlots[i];
            GameObject slotObj = slotObjects[i];

            TextMeshProUGUI itemName = FindText(slotObj.transform, "ItemName");
            if (item == null && itemName != null && itemName.text != "未装備")
            {
                RefreshUI();
                return;
            }

            if (item is OxygenTankData && tank != null)
                SetGauge(slotObj.transform, "TankGauge", tank.Ratio);
            else if (item is ThrusterTankData)
            {
                var thrusterSlot = equipmentSystem.GetSlotByType(ItemType.ThrusterTank);
                var thrusterTank = equipmentSystem.GetThrusterInstance(thrusterSlot);
                if (thrusterTank != null)
                    SetGauge(slotObj.transform, "TankGauge", thrusterTank.Ratio);
            }
            else if (item is SpacesuitData)
            {
                var suitSlot = equipmentSystem.GetSlotByType(ItemType.Spacesuit);
                var suitInst = equipmentSystem.GetSpacesuitInstance(suitSlot);
                if (suitInst != null)
                    SetGauge(slotObj.transform, "TankGauge", suitInst.Ratio);
            }
        }
    }
}
