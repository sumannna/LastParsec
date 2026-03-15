using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// チェストのインベントリ管理。
/// マルチプレイを見越してデータを中央管理できる構造にしている。
/// </summary>
public class ChestInventory : MonoBehaviour
{
    [Header("設定")]
    public int slotCount = 10;

    private Inventory.Slot[] slots;

    void Awake()
    {
        slots = new Inventory.Slot[slotCount];
    }

    public Inventory.Slot[] GetSlots() => slots;

    public bool AddItem(ItemData item)
    {
        // スタック可能なスロットを探す
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] != null && slots[i].item == item && slots[i].amount < item.maxStack)
            {
                slots[i].amount++;
                return true;
            }
        }
        // 空きスロットを探す
        int empty = GetFirstEmptyIndex();
        if (empty < 0) return false;
        slots[empty] = new Inventory.Slot(item, 1);
        return true;
    }

    public bool AddItemAtIndex(ItemData item, int index)
    {
        if (index < 0 || index >= slotCount) return false;
        if (slots[index] != null) return false;
        slots[index] = new Inventory.Slot(item, 1);
        return true;
    }

    public void RemoveSlot(Inventory.Slot slot)
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] == slot) { slots[i] = null; return; }
    }

    public void ReduceSlot(Inventory.Slot slot, int amount)
    {
        if (slot == null) return;
        slot.amount -= amount;
        if (slot.amount <= 0) RemoveSlot(slot);
    }

    public void SwapSlots(Inventory.Slot a, Inventory.Slot b)
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

    public void MoveSlotToIndex(Inventory.Slot slot, int targetIndex)
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

    public int MergeIntoSlot(Inventory.Slot source, int dragAmount, Inventory.Slot target)
    {
        if (source == null || target == null || target.item == null) return dragAmount;
        int space = target.item.maxStack - target.amount;
        int toAdd = Mathf.Min(dragAmount, space);
        target.amount += toAdd;
        ReduceSlot(source, toAdd);
        return dragAmount - toAdd;
    }

    public bool IsFull() => GetFirstEmptyIndex() < 0;

    public int GetAmount(ItemData item)
    {
        int total = 0;
        for (int i = 0; i < slotCount; i++)
            if (slots[i] != null && slots[i].item == item)
                total += slots[i].amount;
        return total;
    }

    public int GetSlotIndex(Inventory.Slot slot)
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] == slot) return i;
        return -1;
    }

    int GetFirstEmptyIndex()
    {
        for (int i = 0; i < slotCount; i++)
            if (slots[i] == null) return i;
        return -1;
    }

    /// <summary>
    /// チェストを回収する。中身をインベントリに移し、あふれた場合はワールドドロップ。
    /// </summary>
    public void Collect(Inventory playerInventory, PickupSpawner spawner)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] == null) continue;
            ItemData item = slots[i].item;
            int amount = slots[i].amount;
            for (int j = 0; j < amount; j++)
            {
                bool added = playerInventory.AddItem(item);
                if (!added && spawner != null)
                    spawner.SpawnItem(item, 1);
            }
            slots[i] = null;
        }
    }
}