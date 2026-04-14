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

    public void RegisterSource(IBatterySource s) { if (!sources.Contains(s)) sources.Add(s); }
    public void UnregisterSource(IBatterySource s) { sources.Remove(s); }
    public void RegisterConsumer(IPowerConsumer c) { if (!consumers.Contains(c)) consumers.Add(c); }
    public void UnregisterConsumer(IPowerConsumer c) { consumers.Remove(c); }

    public float TotalCapacity => sources.Sum(s => s.MaxCapacity);
    public float TotalCharge => sources.Sum(s => s.CurrentCharge);
    public float TotalConsumption => consumers.Where(c => c.IsOn).Sum(c => c.PowerConsumption);
    public float ChargeRatio => TotalCapacity > 0f ? TotalCharge / TotalCapacity : 0f;

    void Update()
    {
        foreach (var consumer in consumers)
        {
            IBatterySource source = FindConnectedSource(consumer);
            if (source == null)
            {
                consumer.OnPowerCutOff();
                continue;
            }
            if (!consumer.IsOn)
            {
                consumer.OnPowerCutOff();
                continue;
            }
            float cost = consumer.IsConsuming
                ? consumer.PowerConsumption * Time.deltaTime
                : 0f;
            if (cost <= 0f || source.CurrentCharge >= cost)
            {
                if (cost > 0f) source.Discharge(cost);
                consumer.OnPowerSupplied();
            }
            else
            {
                consumer.OnPowerCutOff();
            }
        }
    }

    IBatterySource FindConnectedSource(IPowerConsumer consumer)
    {
        if (consumer.Connector == null || !consumer.Connector.IsConnected) return null;
        ElectricConnector other = consumer.Connector.connectedTo;
        foreach (var source in sources)
            if (source.Connector == other) return source;
        return null;
    }
}