using UnityEngine;

/// <summary>
/// ホットバー選択中のアイテムを左クリックで使用するシステム。
/// ToolInstance は Hotbar.Slot に保持されるため、スロット移動でも耐久値が引き継がれる。
/// </summary>
public class ToolUser : MonoBehaviour
{
    [Header("参照")]
    public Hotbar hotbar;
    public Inventory inventory;
    public InventoryUI inventoryUI;
    public HotbarUI hotbarUI;
    public VitalSystem vitalSystem;
    public Transform cameraTransform;

    void Update()
    {
        bool isDead = vitalSystem != null && vitalSystem.IsDead;
        bool anyUIOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();
        if (isDead || anyUIOpen) return;

        if (!Input.GetMouseButtonDown(0)) return;

        if (hotbar == null) { Debug.LogError("[ToolUser] hotbar が未アサイン"); return; }
        if (inventory == null) { Debug.LogError("[ToolUser] inventory が未アサイン"); return; }

        Hotbar.Slot[] slots = hotbar.GetSlots();
        int selectedIndex = hotbar.SelectedIndex;

        if (slots == null || selectedIndex < 0 || selectedIndex >= slots.Length) return;

        Hotbar.Slot hotbarSlot = slots[selectedIndex];
        if (hotbarSlot == null || hotbarSlot.item == null) return;

        if (hotbarSlot.item is PickaxeData pickaxe)
            UsePick(pickaxe, selectedIndex, hotbarSlot);
        else if (hotbarSlot.item is MedicineData medicine)
            UseMedicine(medicine);
        else if (hotbarSlot.item is WaterTankData)
            UseWaterTank(hotbarSlot);
        else if (hotbarSlot.item is PlaceableData)
            return;
    }

    // -----------------------------------------------
    // ピッケル使用
    // -----------------------------------------------

    void UsePick(PickaxeData pickaxe, int slotIndex, Hotbar.Slot hotbarSlot)
    {
        if (hotbarSlot.toolInstance == null)
        {
            hotbarSlot.toolInstance = new ToolInstance(pickaxe);
            Debug.Log("[ToolUser] ToolInstance を新規生成");
        }

        ToolInstance inst = hotbarSlot.toolInstance;

        Debug.Log($"[ToolUser] ピッケル耐久: {inst.currentDurability}/{pickaxe.maxDurability}");

        if (inst.IsBroken)
        {
            Debug.Log("[ToolUser] ピッケルが破損しているため使用不可");
            return;
        }

        if (cameraTransform == null)
        {
            Debug.LogError("[ToolUser] cameraTransform が未アサイン");
            return;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        Debug.Log($"[ToolUser] Raycast発射 range={pickaxe.miningRange} layer={pickaxe.miningLayer.value}");

        if (!Physics.Raycast(ray, out RaycastHit hit, pickaxe.miningRange, pickaxe.miningLayer))
        {
            Debug.Log("[ToolUser] Raycastヒットなし（採掘対象が範囲外 or レイヤー設定ミスの可能性）");
            return;
        }

        Debug.Log($"[ToolUser] ヒット: {hit.collider.gameObject.name} / layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");

        MiningTarget target = hit.collider.GetComponent<MiningTarget>();
        if (target != null)
        {
            target.TakeMiningDamage(pickaxe.miningDamage);
        }
        else
        {
            DebrisObject debris = hit.collider.GetComponent<DebrisObject>();
            if (debris == null)
            {
                Debug.LogWarning($"[ToolUser] {hit.collider.gameObject.name} に MiningTarget も DebrisObject もない");
                return;
            }
            debris.TakeMiningHit(inventory, inventoryUI);
        }

        bool broken = inst.ConsumeOnUse();
        Debug.Log($"[ToolUser] 耐久消費後: {inst.currentDurability}/{pickaxe.maxDurability} / broken={broken}");

        if (broken)
        {
            hotbar.ClearSlot(slotIndex);
            Debug.Log("[ToolUser] ピッケルが破損して消滅");
            if (hotbarUI != null) hotbarUI.RefreshAll();
            else if (inventoryUI != null) inventoryUI.RefreshAll();
        }
    }

    // -----------------------------------------------
    // 回復薬使用
    // -----------------------------------------------

    void UseMedicine(MedicineData medicine)
    {
        if (vitalSystem == null)
        {
            Debug.LogError("[ToolUser] vitalSystem が未アサイン");
            return;
        }

        Inventory.Slot invSlot = FindItemSlot(medicine);
        if (invSlot == null)
        {
            Debug.LogWarning("[ToolUser] インベントリに回復薬が見つからない");
            return;
        }

        vitalSystem.HealHP(medicine.healAmount);
        inventory.ReduceSlot(invSlot, 1);
        Debug.Log($"[ToolUser] 回復薬使用：HP +{medicine.healAmount}");

        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.RefreshAll();
    }

    // -----------------------------------------------
    // 水タンク使用
    // -----------------------------------------------

    void UseWaterTank(Hotbar.Slot hotbarSlot)
    {
        if (vitalSystem == null)
        {
            Debug.LogError("[ToolUser] vitalSystem null");
            return;
        }

        WaterTankData tankData = hotbarSlot.item as WaterTankData;
        if (tankData == null) return;

        Inventory.Slot invSlot = FindItemSlot(tankData);
        if (invSlot == null || invSlot.waterTankInstance == null)
        {
            Debug.LogWarning("[ToolUser] WaterTankInstance not found");
            return;
        }

        WaterTankInstance inst = invSlot.waterTankInstance;
        if (inst.IsEmpty)
        {
            Debug.Log("[ToolUser] 水タンクが空");
            return;
        }

        vitalSystem.AddWater(tankData.waterPerClick);
        bool empty = inst.Consume(tankData.waterPerClick);
        Debug.Log($"[ToolUser] 水分補給 +{tankData.waterPerClick} / 残量:{inst.currentWater}");

        if (empty)
        {
            inventory.RemoveSlot(invSlot);
            Debug.Log("[ToolUser] 水タンク空→破棄");
        }

        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.RefreshAll();
        if (hotbarUI != null)
            hotbarUI.RefreshAll();
    }

    // -----------------------------------------------
    // ユーティリティ
    // -----------------------------------------------

    Inventory.Slot FindItemSlot(ItemData item)
    {
        foreach (var slot in inventory.GetSlots())
        {
            if (slot == null || slot.item != item) continue;
            return slot;
        }
        return null;
    }
}