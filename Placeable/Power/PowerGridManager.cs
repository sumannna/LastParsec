using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 電力グリッド全体を管理するSingleton。
/// IBatterySource（電源）とIPowerConsumer（消費機器）を登録し、
/// 毎フレーム供給可否を判定して各機器に通知する。
/// </summary>
public class PowerGridManager : MonoBehaviour
{
    public static PowerGridManager Instance { get; private set; }

    private readonly List<IBatterySource> sources = new List<IBatterySource>();
    private readonly List<IPowerConsumer> consumers = new List<IPowerConsumer>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -----------------------------------------------
    // 登録 / 解除
    // -----------------------------------------------
    public void RegisterSource(IBatterySource s) { if (!sources.Contains(s)) sources.Add(s); }
    public void UnregisterSource(IBatterySource s) { sources.Remove(s); }
    public void RegisterConsumer(IPowerConsumer c) { if (!consumers.Contains(c)) consumers.Add(c); }
    public void UnregisterConsumer(IPowerConsumer c) { consumers.Remove(c); }

    // -----------------------------------------------
    // 状態取得
    // -----------------------------------------------
    public float TotalCapacity => sources.Sum(s => s.MaxCapacity);
    public float TotalCharge => sources.Sum(s => s.CurrentCharge);
    public float TotalConsumption => consumers.Where(c => c.IsRunning).Sum(c => c.PowerConsumption);
    public float ChargeRatio => TotalCapacity > 0f ? TotalCharge / TotalCapacity : 0f;

    // -----------------------------------------------
    // 毎フレーム：放電 → 供給判定
    // -----------------------------------------------
    void Update()
    {
        float needed = TotalConsumption * Time.deltaTime; // kWh消費
        float available = TotalCharge;

        if (available >= needed)
        {
            // 全Consumer稼働可能：消費量を各Sourceから均等放電
            DischargeAll(needed);
            foreach (var c in consumers)
                c.OnPowerSupplied();
        }
        else
        {
            // 電力不足：消費電力の小さい順に優先供給、残りは停止
            float remaining = available;
            DischargeAll(available);

            var ordered = consumers.OrderBy(c => c.PowerConsumption).ToList();
            foreach (var c in ordered)
            {
                float cost = c.PowerConsumption * Time.deltaTime;
                if (remaining >= cost)
                {
                    remaining -= cost;
                    c.OnPowerSupplied();
                }
                else
                {
                    c.OnPowerCutOff();
                }
            }
        }
    }

    // -----------------------------------------------
    // 内部処理
    // -----------------------------------------------
    void DischargeAll(float totalKWh)
    {
        if (sources.Count == 0 || totalKWh <= 0f) return;
        float perSource = totalKWh / sources.Count;
        foreach (var s in sources)
            s.Discharge(perSource);
    }
}