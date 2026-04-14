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
    public float liquidPerProcess = 1f;  // 1処理で消費する液体量
    public int slotCount = 5;

    [Header("精製設定")]
    public LiquidData waterLiquid;
    public ItemData waterOutputItem;
    public LiquidData oxygenLiquid;
    public ItemData oxygenOutputItem;
    public LiquidData hydrogenLiquid;
    public ItemData hydrogenOutputItem;

    [Header("接続")]
    public PipeConnector inletConnector;
    public ElectricConnector electricConnector;

    [Header("入力受け入れアイテム")]
    public ItemData[] acceptableInputItems;

    [Header("O2大気放出設定")]
    public PipeConnector oxygenOutletConnector; // 酸素出力パイプ（未接続なら大気放出）
    public float atmosphereReleasePerProcess = 2f; // 大気放出量（m^3/process）


    // IPowerConsumer
    public string ConsumerName => "FillingMachine";
    public float PowerConsumption => powerConsumption;
    public bool IsRunning => isOn && isPowered;
    public bool IsOn => isOn;
    public bool IsConsuming => isOn && isPowered && FindInputSlot() != null && HasStoredLiquid() && HasOutputSpace();
    private bool isOn = false;
    private bool isPowered = false;
    private Coroutine processCoroutine;
    public ElectricConnector Connector => electricConnector;
    public Inventory.Slot[] inputSlots;   // 上段：空タンク
    public Inventory.Slot[] outputSlots;  // 下段：充填済みタンク

    
    public void NotifySlotsChanged() => OnSlotsChanged?.Invoke();
    public event System.Action OnSlotsChanged;

    // 液体蓄積
    public float storedWater = 0f;
    public float storedOxygen = 0f;
    public float storedHydrogen = 0f;

    public void ReceiveLiquid(LiquidData liquid, float amount)
    {
        if (liquid == waterLiquid) storedWater += amount;
        else if (liquid == oxygenLiquid) storedOxygen += amount;
        else if (liquid == hydrogenLiquid) storedHydrogen += amount;
        else return;
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
            Inventory.Slot inputSlot = FindInputSlot();
            if (inputSlot == null) { yield return new WaitForSeconds(1f); continue; }
            if (!HasStoredLiquid()) { yield return new WaitForSeconds(1f); continue; }
            if (!HasOutputSpace()) { yield return new WaitForSeconds(1f); continue; }

            yield return new WaitForSeconds(processTime);

            if (!isOn || !isPowered) break;

            inputSlot = FindInputSlot();
            if (inputSlot == null || !HasStoredLiquid() || !HasOutputSpace()) continue;

            LiquidData liquid = GetAvailableLiquid();
            if (liquid == null) continue;

            ReduceSlot(inputSlots, inputSlot, 1);
            ConsumeLiquid(liquid);
            AddToOutput(liquid);

            OnSlotsChanged?.Invoke();
        }
        processCoroutine = null;
    }

    bool HasOutputSpace()
    {
        foreach (var slot in outputSlots)
            if (slot == null) return true;
        return false;
    }

    LiquidData GetAvailableLiquid()
    {
        if (storedWater >= liquidPerProcess) return waterLiquid;
        if (storedOxygen >= liquidPerProcess) return oxygenLiquid;
        if (storedHydrogen >= liquidPerProcess) return hydrogenLiquid;
        return null;
    }

    void ConsumeLiquid(LiquidData liquid)
    {
        if (liquid == waterLiquid) storedWater -= liquidPerProcess;
        else if (liquid == oxygenLiquid) storedOxygen -= liquidPerProcess;
        else if (liquid == hydrogenLiquid) storedHydrogen -= liquidPerProcess;
        storedWater = Mathf.Max(0f, storedWater);
        storedOxygen = Mathf.Max(0f, storedOxygen);
        storedHydrogen = Mathf.Max(0f, storedHydrogen);
    }

    void AddToOutput(LiquidData liquid)
    {
        ItemData outputItem = null;
        if (liquid == waterLiquid) outputItem = waterOutputItem;
        else if (liquid == oxygenLiquid) outputItem = oxygenOutputItem;
        else if (liquid == hydrogenLiquid) outputItem = hydrogenOutputItem;
        if (outputItem == null) return;

        // O2かつ 出力パイプ未接続 → 大気放出
        if (liquid == oxygenLiquid
            && (oxygenOutletConnector == null || !oxygenOutletConnector.IsConnected))
        {
            ShipAtmosphereSystem.Instance?.AddAtmosphere(atmosphereReleasePerProcess);
            Debug.Log($"[FillingMachine] O₂大気放出: {atmosphereReleasePerProcess} m³");
            return;
        }

        // スタック可能なら既存スロットに積む
        if (!(outputItem is OxygenTankData) && !(outputItem is WaterTankData))
        {
            for (int i = 0; i < slotCount; i++)
            {
                if (outputSlots[i] == null || outputSlots[i].item != outputItem) continue;
                if (outputSlots[i].amount >= outputItem.maxStack) continue;
                outputSlots[i].amount++;
                Debug.Log($"[FillingMachine] 精製完了(スタック): {outputItem.itemName} x{outputSlots[i].amount}");
                return;
            }
        }

        for (int i = 0; i < slotCount; i++)
        {
            if (outputSlots[i] != null) continue;
            var slot = new Inventory.Slot(outputItem, 1);

            if (outputItem is OxygenTankData oxyData)
            {
                var inst = new OxygenTankInstance(oxyData);
                slot.tankInstance = inst;
            }
            else if (outputItem is WaterTankData waterData)
            {
                var inst = new WaterTankInstance(waterData);
                inst.currentWater = liquidPerProcess;
                slot.waterTankInstance = inst;
            }

            outputSlots[i] = slot;
            Debug.Log($"[FillingMachine] 精製完了: {outputItem.itemName}");
            return;
        }
    }

    bool HasStoredLiquid() => GetAvailableLiquid() != null;


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

    Inventory.Slot FindInputSlot()
    {
        foreach (var slot in inputSlots)
            if (slot != null && slot.item != null) return slot;
        return null;
    }

    
}