using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public int slotCount = 20;
    private Slot[] slots;

    void Awake()
    {
        slots = new Slot[slotCount];
    }

    public class Slot
    {
        public ItemData item;
        public int amount;
        public OxygenTankInstance tankInstance;
        public ThrusterTankInstance thrusterInstance;
        public SpacesuitInstance spacesuitInstance;
        public ToolInstance toolInstance;
        public WaterTankInstance waterTankInstance;

        public Slot(ItemData item, int amount)
        {
            this.item = item;
            this.amount = amount;

            if (item == null) return;

            if (item is OxygenTankData tankData) tankInstance = new OxygenTankInstance(tankData);
            if (item is ThrusterTankData thrusterData) thrusterInstance = new ThrusterTankInstance(thrusterData);
            if (item is SpacesuitData spacesuitData) spacesuitInstance = new SpacesuitInstance(spacesuitData);
            if (item is ToolData toolData) toolInstance = new ToolInstance(toolData);
            if (item is WaterTankData waterTankData) waterTankInstance = new WaterTankInstance(waterTankData);
        }
    }

    int GetFirstEmptyIndex()
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] == null) return i;
        return -1;
    }

    public bool AddItem(ItemData item)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] != null && slots[i].item == item
                && slots[i].amount < item.maxStack)
            {
                slots[i].amount++;
                return true;
            }
        }

        int emptyIndex = GetFirstEmptyIndex();
        if (emptyIndex < 0) return false;
        slots[emptyIndex] = new Slot(item, 1);
        return true;
    }

    public bool AddItemWithTank(ItemData item, OxygenTankInstance tankInstance)
    {
        int emptyIndex = GetFirstEmptyIndex();
        if (emptyIndex < 0) return false;
        var slot = new Slot(item, 1);
        slot.tankInstance = tankInstance;
        slots[emptyIndex] = slot;
        return true;
    }

    public bool AddItemWithThruster(ItemData item, ThrusterTankInstance thrusterInstance)
    {
        int emptyIndex = GetFirstEmptyIndex();
        if (emptyIndex < 0) return false;
        var slot = new Slot(item, 1);
        slot.thrusterInstance = thrusterInstance;
        slots[emptyIndex] = slot;
        return true;
    }

    public bool AddItemWithSpacesuit(ItemData item, SpacesuitInstance spacesuitInstance)
    {
        int emptyIndex = GetFirstEmptyIndex();
        if (emptyIndex < 0) return false;
        var slot = new Slot(item, 1);
        slot.spacesuitInstance = spacesuitInstance;
        slots[emptyIndex] = slot;
        return true;
    }

    public bool AddItemWithWaterTank(ItemData item, WaterTankInstance waterTank)
    {
        int emptyIndex = GetFirstEmptyIndex();
        if (emptyIndex < 0) return false;
        var slot = new Slot(item, 1);
        slot.waterTankInstance = waterTank;
        slots[emptyIndex] = slot;
        return true;
    }

    public void RemoveSlot(Slot slot)
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] == slot) { slots[i] = null; return; }
    }

    /// <summary>
    /// スロットをインベントリに配置する。
    /// preferredIndex が空いていればそこへ、なければスタック→空きスロットの順で配置。
    /// </summary>
    public void PlaceSlotInInventory(Slot slot, int preferredIndex)
    {
        if (slot == null || slot.item == null || slot.amount <= 0) return;

        if (preferredIndex >= 0 && preferredIndex < slotCount && slots[preferredIndex] == null)
        {
            slots[preferredIndex] = slot;
            return;
        }

        bool hasInstance = slot.tankInstance != null || slot.thrusterInstance != null
                        || slot.spacesuitInstance != null || slot.waterTankInstance != null
                        || slot.toolInstance != null;

        if (!hasInstance)
        {
            for (int i = 0; i < slotCount && slot.amount > 0; i++)
            {
                if (slots[i] != null && slots[i].item == slot.item
                    && slots[i].amount < slot.item.maxStack
                    && slots[i].tankInstance == null)
                {
                    int add = Mathf.Min(slot.item.maxStack - slots[i].amount, slot.amount);
                    slots[i].amount += add;
                    slot.amount -= add;
                }
            }
        }

        if (slot.amount > 0)
        {
            int emptyIdx = GetFirstEmptyIndex();
            if (emptyIdx >= 0)
                slots[emptyIdx] = slot;
        }
    }

    public void SwapSlots(Slot a, Slot b)
    {
        int indexA = -1, indexB = -1;
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] == a) indexA = i;
            if (slots[i] == b) indexB = i;
        }

        if (indexA >= 0 && indexB >= 0)
        {
            slots[indexA] = b;
            slots[indexB] = a;
        }
        else if (indexA < 0 && indexB >= 0)
        {
            slots[indexB] = a;
            PlaceSlotInInventory(b, -1);
        }
    }

    public void MoveSlotToIndex(Slot slot, int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= slotCount) return;
        if (slots[targetIndex] != null) return;

        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] == slot)
            {
                slots[i] = null;
                slots[targetIndex] = slot;
                return;
            }
        }

        slots[targetIndex] = slot;
    }

    public void ClearAll()
    {
        for (int i = 0; i < slotCount; i++)
            slots[i] = null;
    }

    public void ReserveIndex(int index)
    {
        if (index >= 0 && index < slotCount && slots[index] == null)
            slots[index] = new Slot(null, 0);
    }

    public void UnreserveIndex(int index)
    {
        if (index >= 0 && index < slotCount && slots[index]?.item == null)
            slots[index] = null;
    }

    public bool AddItemAtIndex(ItemData item, int index)
    {
        if (index < 0 || index >= slotCount) return false;
        if (slots[index] != null) return false;
        slots[index] = new Slot(item, 1);
        return true;
    }

    public bool AddItemWithTankAtIndex(ItemData item, OxygenTankInstance tank, int index)
    {
        if (index < 0 || index >= slotCount) return false;
        if (slots[index] != null) return false;
        var slot = new Slot(item, 1);
        slot.tankInstance = tank;
        slots[index] = slot;
        return true;
    }

    public bool AddItemWithThrusterAtIndex(ItemData item, ThrusterTankInstance thruster, int index)
    {
        if (index < 0 || index >= slotCount) return false;
        if (slots[index] != null) return false;
        var slot = new Slot(item, 1);
        slot.thrusterInstance = thruster;
        slots[index] = slot;
        return true;
    }

    public bool AddItemWithSpacesuitAtIndex(ItemData item, SpacesuitInstance suit, int index)
    {
        if (index < 0 || index >= slotCount) return false;
        if (slots[index] != null) return false;
        var slot = new Slot(item, 1);
        slot.spacesuitInstance = suit;
        slots[index] = slot;
        return true;
    }

    public void ReduceSlot(Slot slot, int reduction)
    {
        if (slot == null) return;
        slot.amount -= reduction;
        if (slot.amount <= 0)
            RemoveSlot(slot);
    }

    /// <summary>
    /// source の dragAmount 個を target へマージ。
    /// 戻り値：移動できなかった残余個数。
    /// </summary>
    public int MergeIntoSlot(Slot source, int dragAmount, Slot target)
    {
        if (source == null || target == null || target.item == null) return dragAmount;
        int space = target.item.maxStack - target.amount;
        int toAdd = Mathf.Min(dragAmount, space);
        target.amount += toAdd;
        ReduceSlot(source, toAdd);
        return dragAmount - toAdd;
    }

    public void AddItemAmount(ItemData item, int amount)
    {
        int remaining = amount;

        for (int i = 0; i < slotCount && remaining > 0; i++)
        {
            if (slots[i] != null && slots[i].item == item && slots[i].amount < item.maxStack)
            {
                int space = item.maxStack - slots[i].amount;
                int add = Mathf.Min(space, remaining);
                slots[i].amount += add;
                remaining -= add;
            }
        }

        while (remaining > 0)
        {
            int idx = GetFirstEmptyIndex();
            if (idx < 0) break;
            int add = Mathf.Min(item.maxStack, remaining);
            slots[idx] = new Slot(item, add);
            remaining -= add;
        }
    }

    /// <summary>
    /// 指定アイテムを指定個数追加できるか事前に確認する。
    /// 既存スタックの空きと空スロットを合算して判定する。
    /// </summary>
    public bool HasSpaceFor(ItemData item, int amount)
    {
        int remaining = amount;

        // 既存スタックの空きを先に消費
        for (int i = 0; i < slotCount && remaining > 0; i++)
        {
            if (slots[i] != null && slots[i].item == item && slots[i].amount < item.maxStack)
                remaining -= (item.maxStack - slots[i].amount);
        }
        if (remaining <= 0) return true;

        // 空スロットで補う
        for (int i = 0; i < slotCount && remaining > 0; i++)
        {
            if (slots[i] == null)
                remaining -= item.maxStack;
        }
        return remaining <= 0;
    }

    public bool SplitSlotToIndex(Slot source, int splitAmount, int targetIndex)
    {
        if (source == null || splitAmount <= 0 || splitAmount > source.amount) return false;
        if (targetIndex < 0 || targetIndex >= slotCount) return false;
        if (slots[targetIndex] != null) return false;

        if (splitAmount == source.amount)
        {
            MoveSlotToIndex(source, targetIndex);
            return true;
        }

        source.amount -= splitAmount;
        slots[targetIndex] = new Slot(source.item, splitAmount);
        return true;
    }

    public bool SplitSlotToEmpty(Slot source, int splitAmount)
    {
        int idx = GetFirstEmptyIndex();
        if (idx < 0) return false;
        return SplitSlotToIndex(source, splitAmount, idx);
    }

    public Slot[] GetSlots() => slots;

    public bool IsFull() => GetFirstEmptyIndex() < 0;

    public int GetAmount(ItemData item)
    {
        int total = 0;
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] != null && slots[i].item == item)
                total += slots[i].amount;
        }
        return total;
    }

    public void RemoveItemAmount(ItemData item, int amount)
    {
        int remaining = amount;
        for (int i = 0; i < slotCount && remaining > 0; i++)
        {
            if (slots[i] == null || slots[i].item != item) continue;
            int take = Mathf.Min(slots[i].amount, remaining);
            slots[i].amount -= take;
            remaining -= take;
            if (slots[i].amount <= 0) slots[i] = null;
        }
    }
}