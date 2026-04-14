using UnityEngine;

/// <summary>
/// 船内の有酸素ゾーンを定義するTriggerCollider。
/// プレイヤーが入退出したとき EnvironmentSystem に通知する。
/// 複数設置可能。重複対応のため入場カウンタで管理する。
/// </summary>
[RequireComponent(typeof(Collider))]
public class OxygenZone : MonoBehaviour
{
    private static int zoneEnterCount = 0;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        zoneEnterCount++;
        UpdateEnvironment();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        zoneEnterCount = Mathf.Max(0, zoneEnterCount - 1);
        UpdateEnvironment();
    }

    static void UpdateEnvironment()
    {
        if (EnvironmentSystem.Instance == null) return;

        bool insideZone = zoneEnterCount > 0;
        // 船内大気がある場合のみ hasAir=true（大気枯渇時は false）
        bool hasAir = insideZone && (ShipAtmosphereSystem.Instance == null || ShipAtmosphereSystem.Instance.HasAtmosphere);
        EnvironmentSystem.Instance.SetZoneAir(hasAir);
    }

    /// <summary>ShipAtmosphereSystemの大気変化時にも呼ぶ（外部から）</summary>
    public static void RefreshEnvironment()
    {
        UpdateEnvironment();
    }
}