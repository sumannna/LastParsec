using System.Collections;
using UnityEngine;

/// <summary>
/// 電気分解機。パイプから水を受け取り、酸素と水素をそれぞれ別パイプに供給する。
/// </summary>
public class Electrolyzer : MonoBehaviour, IPowerConsumer
{
    [Header("設定")]
    public float powerConsumption = 2f;
    public float processTime = 5f;
    public float waterPerProcess = 1f;    // 消費水量 L
    public float outputPerProcess = 1f;   // 酸素・水素それぞれの生成量 L
    public int slotCount = 5;

    [Header("接続")]
    public PipeConnector inletConnector;     // 入口（水）
    public PipeConnector oxygenOutlet;       // 出口1（酸素）
    public PipeConnector hydrogenOutlet;     // 出口2（水素）
    public ElectricConnector electricConnector;

    [Header("液体定義")]
    public LiquidData waterLiquid;
    public LiquidData oxygenLiquid;
    public LiquidData hydrogenLiquid;

    // IPowerConsumer
    public string ConsumerName => "Electrolyzer";
    public float PowerConsumption => powerConsumption;
    public bool IsRunning => isOn && isPowered;

    private bool isOn = false;
    private bool isPowered = false;
    private Coroutine processCoroutine;

    public bool IsOn => isOn;
    public Inventory.Slot[] slots;

    public event System.Action OnSlotsChanged;
    public float storedWater = 0f;

    public void ReceiveLiquid(LiquidData liquid, float amount)
    {
        if (liquid == waterLiquid)
        {
            storedWater += amount;
            Debug.Log($"[Electrolyzer] 受信: 水 {amount}L / 蓄積: {storedWater}L");
            OnSlotsChanged?.Invoke();
        }
        else
        {
            Debug.Log($"[Electrolyzer] 液体種不一致: {liquid.liquidName}");
        }
    }

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
            if (!CanProcess())
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            yield return new WaitForSeconds(processTime);

            if (!isOn || !isPowered) break;
            if (!CanProcess()) continue;

            // 蓄積水を消費
            storedWater -= waterPerProcess;
            storedWater = Mathf.Max(0f, storedWater);

            // 酸素・水素をパイプへ供給
            bool oxyOk = oxygenOutlet.IsConnected &&
                         oxygenOutlet.PushLiquid(oxygenLiquid, outputPerProcess);
            bool hydOk = hydrogenOutlet.IsConnected &&
                         hydrogenOutlet.PushLiquid(hydrogenLiquid, outputPerProcess);

            if (!oxyOk) Debug.Log("[Electrolyzer] 酸素パイプへの供給失敗");
            if (!hydOk) Debug.Log("[Electrolyzer] 水素パイプへの供給失敗");

            Debug.Log($"[Electrolyzer] 処理完了 O2:{outputPerProcess}L H2:{outputPerProcess}L");
            OnSlotsChanged?.Invoke();
        }
        processCoroutine = null;
    }

    bool CanProcess()
    {
        return storedWater >= waterPerProcess;
    }

    public bool AddItem(ItemData item)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] != null && slots[i].item == item
                && slots[i].amount < item.maxStack)
            { slots[i].amount++; return true; }
        }
        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i] == null)
            { slots[i] = new Inventory.Slot(item, 1); return true; }
        }
        return false;
    }
}