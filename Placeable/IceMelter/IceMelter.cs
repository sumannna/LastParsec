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
    public float powerConsumption = 1f;       // kW
    public float processTime = 5f;            // 1回の処理秒数
    public float waterPerIce = 1f;            // 氷1個→水xL
    public int slotCount = 5;
    public ItemData iceItemData;              // 氷アイテム

    [Header("接続")]
    public PipeConnector outletConnector;     // パイプ出口
    public ElectricConnector electricConnector;

    [Header("液体定義")]
    public LiquidData waterLiquid;

    // IPowerConsumer
    public string ConsumerName => "IceMelter";
    public float PowerConsumption => powerConsumption;
    public bool IsRunning => isOn && isPowered;

    private bool isOn = false;
    private bool isPowered = false;
    private Coroutine processCoroutine;

    public bool IsOn => isOn;
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

    // IPowerConsumer
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
            // 氷スロットを確認
            Inventory.Slot iceSlot = FindIceSlot();
            if (iceSlot == null)
            {
                processCoroutine = null;
                yield break;
            }

            // パイプ接続確認
            if (!outletConnector.IsConnected)
            {
                OnStatusChanged?.Invoke("パイプ未接続");
                yield return new WaitForSeconds(1f);
                continue;
            }
            OnStatusChanged?.Invoke(isOn && isPowered ? "稼働中" : "停止中");

            yield return new WaitForSeconds(processTime);

            // 再確認
            iceSlot = FindIceSlot();
            if (iceSlot == null || !isOn || !isPowered) break;

            // 氷消費→水をパイプへ
            ReduceSlot(iceSlot, 1);
            // 接続先機械に液体を直接送る
            var fillingMachine = outletConnector.GetConnectedMachine<FillingMachine>();
            var electrolyzer = outletConnector.GetConnectedMachine<Electrolyzer>();

            if (fillingMachine != null)
                fillingMachine.ReceiveLiquid(waterLiquid, waterPerIce);
            else if (electrolyzer != null)
                electrolyzer.ReceiveLiquid(waterLiquid, waterPerIce);
            else
                Debug.Log("[IceMelter] 接続先機械が見つかりません");

            outletConnector.PushLiquid(waterLiquid, waterPerIce);

            OnSlotsChanged?.Invoke();
        }
        processCoroutine = null;
    }

    Inventory.Slot FindIceSlot()
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

    public event System.Action OnSlotsChanged;
    public event System.Action<string> OnStatusChanged;
}