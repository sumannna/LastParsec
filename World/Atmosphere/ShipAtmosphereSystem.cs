using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 船内大気を一元管理するSingleton。
/// 空間モジュール（容量）・供給源・消費者を登録し、
/// 毎フレーム大気量を更新してIOxygenConsumerに通知する。
/// </summary>
public class ShipAtmosphereSystem : MonoBehaviour
{
    public static ShipAtmosphereSystem Instance { get; private set; }

    [Header("初期設定")]
    [SerializeField] private float initialMaxAtmosphere = 100f;
    [SerializeField] private float crewConsumptionPerPerson = 5f; // m³/h

    // 登録リスト
    private readonly List<float> moduleVolumes = new List<float>();
    private readonly List<IOxygenConsumer> consumers = new List<IOxygenConsumer>();
    private readonly List<IOxygenSupplier> suppliers = new List<IOxygenSupplier>();

    // 乗組員数（CrewSystemから設定）
    private int crewCount = 1;

    // 現在の大気量
    private float currentAtmosphere;

    // 大気供給中かどうか（前フレームの状態比較用）
    private bool wasOxygenAvailable = true;

    // 公開プロパティ
    public float CurrentAtmosphere => currentAtmosphere;
    public float MaxAtmosphere => initialMaxAtmosphere + moduleVolumes.Sum();
    public float GenerationRate => suppliers.Sum(s => s.IsSupplying ? s.OxygenGeneration : 0f);
    public float ConsumptionRate
    {
        get
        {
            float crew = crewCount * crewConsumptionPerPerson;
            float placeable = consumers.Where(c => c.IsOxygenConsuming).Sum(c => c.OxygenConsumption);
            return crew + placeable;
        }
    }
    public float NetRate => GenerationRate - ConsumptionRate;
    public bool HasAtmosphere => currentAtmosphere > 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        currentAtmosphere = initialMaxAtmosphere;
    }

    void Update()
    {
        float netPerSecond = NetRate / 3600f;
        currentAtmosphere += netPerSecond * Time.deltaTime;
        currentAtmosphere = Mathf.Clamp(currentAtmosphere, 0f, MaxAtmosphere);

        bool oxygenAvailable = HasAtmosphere;

        if (oxygenAvailable != wasOxygenAvailable)
        {
            wasOxygenAvailable = oxygenAvailable;
            NotifyConsumers(oxygenAvailable);
        }

        // EnvironmentSystem への通知は OxygenZone が担当するため、
        // ここでは大気有無の状態のみ管理する
    }

    void NotifyConsumers(bool hasOxygen)
    {
        foreach (var consumer in consumers)
        {
            if (hasOxygen)
                consumer.OnOxygenSupplied();
            else
                consumer.OnOxygenCutOff();
        }
    }

    // -----------------------------------------------
    // 登録・解除
    // -----------------------------------------------

    /// <summary>空間モジュールの体積を登録（容量が増える）</summary>
    public void RegisterModule(float volume)
    {
        moduleVolumes.Add(volume);
    }

    public void UnregisterModule(float volume)
    {
        moduleVolumes.Remove(volume);
    }

    public void RegisterConsumer(IOxygenConsumer c)
    {
        if (!consumers.Contains(c)) consumers.Add(c);
    }

    public void UnregisterConsumer(IOxygenConsumer c)
    {
        consumers.Remove(c);
    }

    public void RegisterSupplier(IOxygenSupplier s)
    {
        if (!suppliers.Contains(s)) suppliers.Add(s);
    }

    public void UnregisterSupplier(IOxygenSupplier s)
    {
        suppliers.Remove(s);
    }

    public void SetCrewCount(int count)
    {
        crewCount = Mathf.Max(0, count);
    }

    // -----------------------------------------------
    // エアロック用：直接大気量を操作
    // -----------------------------------------------

    /// <summary>エアロック充填時に船内酸素を消費する</summary>
    public bool ConsumeForAirlock(float amount)
    {
        if (currentAtmosphere < amount) return false;
        currentAtmosphere -= amount;
        return true;
    }

    /// <summary>酸素を直接追加（FillingMachine大気放出など）</summary>
    public void AddAtmosphere(float amount)
    {
        currentAtmosphere = Mathf.Clamp(currentAtmosphere + amount, 0f, MaxAtmosphere);
    }
}