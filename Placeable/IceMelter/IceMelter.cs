using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 解氷機。氷アイテムを消費して水をパイプに供給する。
/// IPowerConsumerを実装し、PowerGridManagerに登録される。
/// </summary>
public class IceMelter : MonoBehaviour, IPowerConsumer
{
    [Header("設定")]
    public float powerConsumption = 1f;
    public float processTime = 5f;
    public float waterPerIce = 1f;
    public int slotCount = 5;
    public ItemData iceItemData;

    [Header("接続")]
    public PipeConnector outletConnector;
    public ElectricConnector electricConnector;

    [Header("液体定義")]
    public LiquidData waterLiquid;

    // IPowerConsumer
    public string ConsumerName => "IceMelter";
    public float PowerConsumption => powerConsumption;
    public bool IsRunning => isOn && isPowered;
    public bool IsOn => isOn;
    public bool IsConsuming => isOn && isPowered && FindIceSlot() != null;
    public bool IsPowered => isPowered;
    public ElectricConnector Connector => electricConnector;

    private bool isOn = false;
    private bool isPowered = false;
    private Coroutine processCoroutine;

    public Inventory.Slot[] slots;

    void Awake()
    {
        slots = new Inventory.Slot[slotCount];
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
        else
            OnStatusChanged?.Invoke("待機中");
    }

    public void OnPowerCutOff()
    {
        isPowered = false;
        if (processCoroutine != null) { StopCoroutine(processCoroutine); processCoroutine = null; }
        OnStatusChanged?.Invoke("未接続");
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
            Inventory.Slot iceSlot = FindIceSlot();
            if (iceSlot == null)
            {
                processCoroutine = null;
                yield break;
            }

            if (!outletConnector.IsConnected)
            {
                OnStatusChanged?.Invoke("パイプ未接続");
                yield return new WaitForSeconds(1f);
                continue;
            }
            OnStatusChanged?.Invoke("稼働中");

            yield return new WaitForSeconds(processTime);

            iceSlot = FindIceSlot();
            if (iceSlot == null || !isOn || !isPowered) break;

            ReduceSlot(iceSlot, 1);

            var fillingMachine = outletConnector.GetConnectedMachine<FillingMachine>();
            var electrolyzer = outletConnector.GetConnectedMachine<Electrolyzer>();

            if (fillingMachine != null)
                fillingMachine.ReceiveLiquid(waterLiquid, waterPerIce);
            else if (electrolyzer != null)
                electrolyzer.ReceiveLiquid(waterLiquid, waterPerIce);
            else

            outletConnector.PushLiquid(waterLiquid, waterPerIce);
            OnSlotsChanged?.Invoke();
        }
        processCoroutine = null;
    }

    public Inventory.Slot FindIceSlot()
    {
        foreach (var slot in slots)
            if (slot != null && slot.item == iceItemData) return slot;
        return null;
    }

    public void ReduceSlot(Inventory.Slot slot, int amount)
    {
        slot.amount -= amount;
        if (slot.amount <= 0)
        {
            int idx = System.Array.IndexOf(slots, slot);
            if (idx >= 0) slots[idx] = null;
        }
    }

    public bool AddItem(ItemData item)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] != null && slots[i].item == item && slots[i].amount < item.maxStack)
            { slots[i].amount++; return true; }
        }
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] == null)
            { slots[i] = new Inventory.Slot(item, 1); return true; }
        }
        return false;
    }

    public void NotifySlotsChanged() => OnSlotsChanged?.Invoke();

    public event System.Action OnSlotsChanged;
    public event System.Action<string> OnStatusChanged;
}