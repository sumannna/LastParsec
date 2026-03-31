using System.Collections.Generic;
using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    [Header("装備スロット定義")]
    public List<EquipmentSlotData> slotDefinitions;

    [Header("初期装備")]
    public ItemData initialSpacesuit;
    public ItemData initialOxygenTank;
    public ItemData initialThrusterTank;

    [Header("参照")]
    public OxygenSystem oxygenSystem;
    public Inventory inventory;
    public FuelSystem fuelSystem;
    public StatusBarManager statusBarManager;
    public VitalSystem vitalSystem;

    private Dictionary<EquipmentSlotData, ItemData> equipped
        = new Dictionary<EquipmentSlotData, ItemData>();
    private Dictionary<EquipmentSlotData, OxygenTankInstance> tankInstances
        = new Dictionary<EquipmentSlotData, OxygenTankInstance>();
    private Dictionary<EquipmentSlotData, ThrusterTankInstance> thrusterInstances
        = new Dictionary<EquipmentSlotData, ThrusterTankInstance>();
    private Dictionary<EquipmentSlotData, SpacesuitInstance> spacesuitInstances
        = new Dictionary<EquipmentSlotData, SpacesuitInstance>();

    void Start()
    {
        foreach (var slot in slotDefinitions)
        {
            equipped[slot] = null;
            tankInstances[slot] = null;
            thrusterInstances[slot] = null;
            spacesuitInstances[slot] = null;
        }

        // 初期装備
        if (initialSpacesuit != null)
        {
            var slot = GetSlotByType(ItemType.Spacesuit);
            if (slot != null)
            {
                SpacesuitInstance suit = new SpacesuitInstance(
                    initialSpacesuit as SpacesuitData);
                Equip(slot, initialSpacesuit, null, null, suit);
            }
        }

        if (initialOxygenTank != null)
        {
            var slot = GetSlotByType(ItemType.OxygenTank);
            if (slot != null)
            {
                OxygenTankInstance tank = new OxygenTankInstance(
                    initialOxygenTank as OxygenTankData);
                Equip(slot, initialOxygenTank, tank);
            }
        }

        if (initialThrusterTank != null)
        {
            var slot = GetSlotByType(ItemType.ThrusterTank);
            if (slot != null)
            {
                ThrusterTankInstance tank = new ThrusterTankInstance(
                    initialThrusterTank as ThrusterTankData);
                Equip(slot, initialThrusterTank, null, tank);
            }
        }
    }

    void Update()
    {
        // Uキーでダメージ（テスト用）
        if (Input.GetKeyDown(KeyCode.U))
            ApplyDamage(20f);

        CheckBreakage();
    }

    public void ApplyDamage(float damage)
    {
        var suitSlot = GetSlotByType(ItemType.Spacesuit);
        SpacesuitInstance suit = suitSlot != null ? spacesuitInstances[suitSlot] : null;

        float actualDamage = damage;

        if (suit != null && !suit.IsBroken)
        {
            // 宇宙服でダメージ軽減・耐久減
            actualDamage = damage * (1f - suit.data.damageReduction);
            suit.currentDurability -= damage * suit.data.damageReduction;
            suit.currentDurability = Mathf.Clamp(suit.currentDurability, 0f,
                suit.data.maxDurability);

            if (statusBarManager != null)
                statusBarManager.SetValue("spacesuit", suit.Ratio);

            Debug.Log($"宇宙服耐久：{suit.currentDurability} / 実ダメージ：{actualDamage}");
        }

        // HPにダメージ
        if (vitalSystem != null)
            vitalSystem.TakeDamage(actualDamage);
    }

    void CheckBreakage()
    {
        // 宇宙服破損チェック（最優先）
        var suitSlot = GetSlotByType(ItemType.Spacesuit);
        if (suitSlot != null && spacesuitInstances.ContainsKey(suitSlot) && spacesuitInstances[suitSlot] != null)
        {
            if (spacesuitInstances[suitSlot].IsBroken)
            {
                BreakItem(suitSlot);
                return; // 1フレームに1回だけ処理
            }
        }

        // 酸素タンク破損チェック
        var oxySlot = GetSlotByType(ItemType.OxygenTank);
        if (oxySlot != null && tankInstances.ContainsKey(oxySlot) && tankInstances[oxySlot] != null)
        {
            if (!tankInstances[oxySlot].HasOxygen)
            {
                BreakItem(oxySlot);
                return;
            }
        }

        // スラスタータンク破損チェック
        var thrusterSlot = GetSlotByType(ItemType.ThrusterTank);
        if (thrusterSlot != null && thrusterInstances.ContainsKey(thrusterSlot) && thrusterInstances[thrusterSlot] != null)
        {
            if (!thrusterInstances[thrusterSlot].HasFuel)
            {
                BreakItem(thrusterSlot);
                return;
            }
        }
    }

    void BreakItem(EquipmentSlotData slot)
    {
        ItemData item = equipped[slot];
        if (item == null) return;

        Debug.Log($"{item.itemName} が破損・消滅");

        // 宇宙服破損時は酸素タンクをインベントリに戻す
        if (item.itemType == ItemType.Spacesuit)
            UnequipOxygenTankAuto();

        // 装備データをクリア
        equipped[slot] = null;
        tankInstances[slot] = null;
        thrusterInstances[slot] = null;
        spacesuitInstances[slot] = null;

        // 各システムの参照をクリア
        if (item.itemType == ItemType.OxygenTank && oxygenSystem != null)
            oxygenSystem.equippedTank = null;

        if (item.itemType == ItemType.ThrusterTank)
        {
            if (fuelSystem != null) fuelSystem.equippedTank = null;
            if (statusBarManager != null) statusBarManager.SetVisible("thruster", false);
        }

        if (item.itemType == ItemType.Spacesuit && statusBarManager != null)
            statusBarManager.SetVisible("spacesuit", false);
    }

    void UnequipOxygenTankAuto()
    {
        var tankSlot = GetSlotByType(ItemType.OxygenTank);

        if (tankSlot == null || equipped[tankSlot] == null) return;

        ItemData tankItem = equipped[tankSlot];
        OxygenTankInstance tankInst = tankInstances[tankSlot];

        equipped[tankSlot] = null;
        tankInstances[tankSlot] = null;

        if (oxygenSystem != null)
            oxygenSystem.equippedTank = null;

        bool added = inventory.AddItemWithTank(tankItem, tankInst);
        if (!added)
            DropItem(tankItem, tankInst);
    }

    // 仮実装：無重力空間に排出
    void DropItem(ItemData item, OxygenTankInstance tankInst = null)
    {
        Debug.Log($"{item.itemName} をドロップ（仮実装：無重力ワールドに排出）");
    }

    public EquipmentSlotData GetSlotByType(ItemType type)
    {
        foreach (var slot in slotDefinitions)
            if (slot.acceptedType == type) return slot;
        return null;
    }

    public bool HasSpacesuit()
    {
        var slot = GetSlotByType(ItemType.Spacesuit);
        return slot != null && equipped[slot] != null;
    }

    public bool Equip(EquipmentSlotData slot, ItemData item,
        OxygenTankInstance oxyTank = null,
        ThrusterTankInstance thrusterTank = null,
        SpacesuitInstance spacesuitInst = null)
    {
        if (item.itemType != slot.acceptedType)
        {
            Debug.Log($"{item.itemName} はこのスロットに装備できない");
            return false;
        }

        if (item.itemType == ItemType.OxygenTank && !HasSpacesuit())
        {
            Debug.Log("宇宙服を先に装備してください");
            return false;
        }

        equipped[slot] = item;

        if (item is OxygenTankData)
        {
            if (oxyTank == null)
                oxyTank = new OxygenTankInstance(item as OxygenTankData);
            tankInstances[slot] = oxyTank;
            if (oxygenSystem != null)
                oxygenSystem.equippedTank = oxyTank;
        }

        if (item is ThrusterTankData)
        {
            if (thrusterTank == null)
                thrusterTank = new ThrusterTankInstance(item as ThrusterTankData);
            thrusterInstances[slot] = thrusterTank;
            if (fuelSystem != null)
                fuelSystem.equippedTank = thrusterTank;
            if (statusBarManager != null)
            {
                statusBarManager.SetVisible("thruster", true);
                statusBarManager.SetValue("thruster", thrusterTank.Ratio);
            }
        }

        if (item is SpacesuitData)
        {
            if (spacesuitInst == null)
                spacesuitInst = new SpacesuitInstance(item as SpacesuitData);
            spacesuitInstances[slot] = spacesuitInst;
            if (statusBarManager != null)
            {
                statusBarManager.SetVisible("spacesuit", true);
                statusBarManager.SetValue("spacesuit", spacesuitInst.Ratio);
            }
        }

        Debug.Log($"{slot.slotName} に {item.itemName} を装備");
        return true;
    }

    public (ItemData item, OxygenTankInstance oxyTank,
        ThrusterTankInstance thrusterTank, SpacesuitInstance spacesuitInst)
        Unequip(EquipmentSlotData slot)
    {
        ItemData item = equipped[slot];
        OxygenTankInstance oxyTank = tankInstances[slot];
        ThrusterTankInstance thrusterTank = thrusterInstances[slot];
        SpacesuitInstance spacesuitInst = spacesuitInstances[slot];

        equipped[slot] = null;
        tankInstances[slot] = null;
        thrusterInstances[slot] = null;
        spacesuitInstances[slot] = null;

        if (item != null && item.itemType == ItemType.Spacesuit)
            UnequipOxygenTankAuto();

        if (item != null && item.itemType == ItemType.OxygenTank)
            if (oxygenSystem != null)
                oxygenSystem.equippedTank = null;

        if (item != null && item.itemType == ItemType.ThrusterTank)
        {
            if (fuelSystem != null) fuelSystem.equippedTank = null;
            if (statusBarManager != null) statusBarManager.SetVisible("thruster", false);
        }

        if (item != null && item.itemType == ItemType.Spacesuit)
            if (statusBarManager != null) statusBarManager.SetVisible("spacesuit", false);

        return (item, oxyTank, thrusterTank, spacesuitInst);
    }

    public void RespawnEquip()
    {
        // 全装備を新品で再装備
        var suitSlot = GetSlotByType(ItemType.Spacesuit);
        if (suitSlot != null && initialSpacesuit != null)
        {
            equipped[suitSlot] = initialSpacesuit;
            SpacesuitInstance newSuit = new SpacesuitInstance(
                initialSpacesuit as SpacesuitData);
            spacesuitInstances[suitSlot] = newSuit;
            if (statusBarManager != null)
            {
                statusBarManager.SetVisible("spacesuit", true);
                statusBarManager.SetValue("spacesuit", 1f);
            }
        }

        var oxySlot = GetSlotByType(ItemType.OxygenTank);
        if (oxySlot != null && initialOxygenTank != null)
        {
            equipped[oxySlot] = initialOxygenTank;
            OxygenTankInstance newOxy = new OxygenTankInstance(
                initialOxygenTank as OxygenTankData);
            tankInstances[oxySlot] = newOxy;
            if (oxygenSystem != null)
                oxygenSystem.equippedTank = newOxy;
        }

        var thrusterSlot = GetSlotByType(ItemType.ThrusterTank);
        if (thrusterSlot != null && initialThrusterTank != null)
        {
            equipped[thrusterSlot] = initialThrusterTank;
            ThrusterTankInstance newThruster = new ThrusterTankInstance(
                initialThrusterTank as ThrusterTankData);
            thrusterInstances[thrusterSlot] = newThruster;
            if (fuelSystem != null)
                fuelSystem.equippedTank = newThruster;
            if (statusBarManager != null)
            {
                statusBarManager.SetVisible("thruster", true);
                statusBarManager.SetValue("thruster", 1f);
            }
        }
    }

    public (ItemData item, OxygenTankInstance oxyTank,
        ThrusterTankInstance thrusterTank, SpacesuitInstance spacesuitInst)
        UnequipSpacesuitOnly(EquipmentSlotData slot)
    {
        ItemData item = equipped[slot];
        OxygenTankInstance oxyTank = tankInstances[slot];
        ThrusterTankInstance thrusterTank = thrusterInstances[slot];
        SpacesuitInstance spacesuitInst = spacesuitInstances[slot];

        equipped[slot] = null;
        spacesuitInstances[slot] = null;

        // タンクは外さない（tankInstances・thrusterInstancesはそのまま）
        if (statusBarManager != null)
            statusBarManager.SetVisible("spacesuit", false);

        return (item, oxyTank, thrusterTank, spacesuitInst);
    }

    public ItemData GetEquipped(EquipmentSlotData slot)
        => equipped.ContainsKey(slot) ? equipped[slot] : null;

    public OxygenTankInstance GetTankInstance(EquipmentSlotData slot)
        => tankInstances.ContainsKey(slot) ? tankInstances[slot] : null;

    public ThrusterTankInstance GetThrusterInstance(EquipmentSlotData slot)
        => thrusterInstances.ContainsKey(slot) ? thrusterInstances[slot] : null;

    public SpacesuitInstance GetSpacesuitInstance(EquipmentSlotData slot)
        => spacesuitInstances.ContainsKey(slot) ? spacesuitInstances[slot] : null;

    public List<(EquipmentSlotData slot, ItemData item, OxygenTankInstance tank)>
        GetAllSlots()
    {
        var result = new List<(EquipmentSlotData, ItemData, OxygenTankInstance)>();
        foreach (var slot in slotDefinitions)
            result.Add((slot, equipped[slot], tankInstances[slot]));
        return result;
    }
}
