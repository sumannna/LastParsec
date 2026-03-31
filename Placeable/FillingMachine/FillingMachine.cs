using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 充填機。上段タンクにパイプから液体を充填し、下段に完成タンクを生成する。
/// </summary>
public class FillingMachine : MonoBehaviour, IPowerConsumer
{
    [Header("設定")]
    public float powerConsumption = 1.5f;
    public float processTime = 5f;
    public float fillAmountPerProcess = 5f; // 1回の処理で充填するL数
    public int slotCount = 5;               // 上段・下段それぞれ5スロット

    [Header("接続")]
    public PipeConnector inletConnector;
    public ElectricConnector electricConnector;

    // IPowerConsumer
    public string ConsumerName => "FillingMachine";
    public float PowerConsumption => powerConsumption;
    public bool IsRunning => isOn && isPowered;

    private bool isOn = false;
    private bool isPowered = false;
    private Coroutine processCoroutine;

    public bool IsOn => isOn;
    public Inventory.Slot[] inputSlots;   // 上段：空タンク
    public Inventory.Slot[] outputSlots;  // 下段：充填済みタンク

    
    public void NotifySlotsChanged() => OnSlotsChanged?.Invoke();
    public event System.Action OnSlotsChanged;

    // 液体蓄積
    public float storedWater = 0f;
    public float storedOxygen = 0f;
    public float storedHydrogen = 0f;
    public LiquidData waterLiquid;
    public LiquidData oxygenLiquid;
    public LiquidData hydrogenLiquid;

    public void ReceiveLiquid(LiquidData liquid, float amount)
    {
        if (liquid == waterLiquid) storedWater += amount;
        else if (liquid == oxygenLiquid) storedOxygen += amount;
        else if (liquid == hydrogenLiquid) storedHydrogen += amount;
        Debug.Log($"[FillingMachine] 受信: {liquid.liquidName} {amount}L / 蓄積: W={storedWater} O={storedOxygen} H={storedHydrogen}");
        OnSlotsChanged?.Invoke();
    }

    void Awake()
    {
        inputSlots = new Inventory.Slot[slotCount];
        outputSlots = new Inventory.Slot[slotCount];
    }

    void Start()
    {
        PowerGridManager.Instance?.RegisterConsumer(this);
    }

    void OnDestroy()
    {
        PowerGridManager.Instance?.UnregisterConsumer(this);
    }

    public void OnPowerSupplied()
    {
        isPowered = true;
        if (isOn && processCoroutine == null)
            processCoroutine = StartCoroutine(ProcessRoutine());
    }

    public void OnPowerCutOff()
    {
        isPowered = false;
        if (processCoroutine != null)
        {
            StopCoroutine(processCoroutine);
            processCoroutine = null;
        }
    }

    public void SetOn(bool on)
    {
        isOn = on;
        if (!isOn && processCoroutine != null)
        {
            StopCoroutine(processCoroutine);
            processCoroutine = null;
        }
    }

    IEnumerator ProcessRoutine()
    {
        while (isOn && isPowered)
        {
            // 上段に空タンクがあるか
            Inventory.Slot inputSlot = FindInputSlot();
            if (inputSlot == null) { yield return new WaitForSeconds(1f); continue; }

            // パイプ接続または蓄積量があれば処理可能
            if (!HasStoredLiquid())
            { yield return new WaitForSeconds(1f); continue; }

            // 下段に空きがあるか
            if (!HasOutputSpace()) { yield return new WaitForSeconds(1f); continue; }

            yield return new WaitForSeconds(processTime);

            // 再確認
            inputSlot = FindInputSlot();
            if (inputSlot == null || !isOn || !isPowered) break;
            if (!inletConnector.IsConnected || inletConnector.currentLiquidType == null) continue;
            if (!HasOutputSpace()) continue;

            LiquidData liquid = GetAvailableLiquid();
            ItemData tankItem = inputSlot.item;

            // 蓄積量を消費
            ConsumeLiquid(liquid, fillAmountPerProcess);
            ReduceSlot(inputSlots, inputSlot, 1);
            AddToOutput(tankItem, liquid);

            OnSlotsChanged?.Invoke();
        }
        processCoroutine = null;
    }

    Inventory.Slot FindInputSlot()
    {
        foreach (var slot in inputSlots)
            if (slot != null && slot.item != null) return slot;
        return null;
    }

    bool HasOutputSpace()
    {
        foreach (var slot in outputSlots)
            if (slot == null) return true;
        return false;
    }

    void AddToOutput(ItemData tankItem, LiquidData liquid)
    {
        // タンク種類と液体種に応じてインスタンスを生成
        for (int i = 0; i < slotCount; i++)
        {
            if (outputSlots[i] != null) continue;
            var slot = new Inventory.Slot(tankItem, 1);

            if (tankItem is OxygenTankData oxyData)
            {
                var inst = new OxygenTankInstance(oxyData);
                inst.currentOxygen = fillAmountPerProcess;
                slot.tankInstance = inst;
            }
            else if (tankItem is ThrusterTankData thrusterData)
            {
                var inst = new ThrusterTankInstance(thrusterData);
                inst.currentFuel = fillAmountPerProcess;
                slot.thrusterInstance = inst;
            }
            else if (tankItem is WaterTankData waterData)
            {
                var inst = new WaterTankInstance(waterData);
                inst.currentWater = fillAmountPerProcess;
                slot.waterTankInstance = inst;
            }

            outputSlots[i] = slot;
            Debug.Log($"[FillingMachine] 充填完了: {tankItem.itemName} with {liquid.liquidName}");
            return;
        }
    }

    void ReduceSlot(Inventory.Slot[] slotArray, Inventory.Slot slot, int amount)
    {
        slot.amount -= amount;
        if (slot.amount <= 0)
        {
            int idx = System.Array.IndexOf(slotArray, slot);
            if (idx >= 0) slotArray[idx] = null;
        }
    }

    public bool AddToInput(ItemData item)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (inputSlots[i] != null && inputSlots[i].item == item
                && inputSlots[i].amount < item.maxStack)
            { inputSlots[i].amount++; return true; }
        }
        for (int i = 0; i < slotCount; i++)
        {
            if (inputSlots[i] == null)
            { inputSlots[i] = new Inventory.Slot(item, 1); return true; }
        }
        return false;
    }

    bool HasStoredLiquid()
    {
        return storedWater >= fillAmountPerProcess
            || storedOxygen >= fillAmountPerProcess
            || storedHydrogen >= fillAmountPerProcess;
    }

    LiquidData GetAvailableLiquid()
    {
        if (storedWater >= fillAmountPerProcess) return waterLiquid;
        if (storedOxygen >= fillAmountPerProcess) return oxygenLiquid;
        if (storedHydrogen >= fillAmountPerProcess) return hydrogenLiquid;
        return null;
    }

    void ConsumeLiquid(LiquidData liquid, float amount)
    {
        if (liquid == waterLiquid) storedWater -= amount;
        else if (liquid == oxygenLiquid) storedOxygen -= amount;
        else if (liquid == hydrogenLiquid) storedHydrogen -= amount;
        storedWater = Mathf.Max(0f, storedWater);
        storedOxygen = Mathf.Max(0f, storedOxygen);
        storedHydrogen = Mathf.Max(0f, storedHydrogen);
    }
}