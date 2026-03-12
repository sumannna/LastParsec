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
        public ToolInstance toolInstance;          // ← 追加：道具の耐久インスタンス

        public Slot(ItemData item, int amount)
        {
            this.item = item;
            this.amount = amount;

            if (item == null) return;

            if (item is OxygenTankData tankData)
                tankInstance = new OxygenTankInstance(tankData);
            if (item is ThrusterTankData thrusterData)
                thrusterInstance = new ThrusterTankInstance(thrusterData);
            if (item is SpacesuitData spacesuitData)
                spacesuitInstance = new SpacesuitInstance(spacesuitData);
            if (item is ToolData toolData)
                toolInstance = new ToolInstance(toolData);
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

    public void RemoveSlot(Slot slot)
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] == slot) { slots[i] = null; return; }
    }

    public void SwapSlots(Slot a, Slot b)
    {
        int indexA = -1, indexB = -1;
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] == a) indexA = i;
            if (slots[i] == b) indexB = i;
        }
        if (indexA < 0 || indexB < 0) return;
        slots[indexA] = b;
        slots[indexB] = a;
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

    // -----------------------------------------------
    // スタック操作
    // -----------------------------------------------

    public void ReduceSlot(Slot slot, int reduction)
    {
        if (slot == null) return;
        slot.amount -= reduction;
        if (slot.amount <= 0)
            RemoveSlot(slot);
    }

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

    public bool IsFull()
    {
        return GetFirstEmptyIndex() < 0;
    }

    public int GetAmount(ItemData item)
    {
        int total = 0;
        for (int i = 0; i < slotCount; i++)
            if (slots[i] != null && slots[i].item == item)
                total += slots[i].amount;
        return total;
    }
}