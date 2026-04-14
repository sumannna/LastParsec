using UnityEngine;

/// <summary>
/// エアロック内の酸素ゾーン。
/// Airlock の状態（与圧/減圧）に連動して有酸素/無酸素を切り替える。
/// OxygenZone とは独立して動作する。
/// </summary>
[RequireComponent(typeof(Collider))]
public class AirlockZone : MonoBehaviour
{
    [SerializeField] private Airlock airlock;

    private bool playerInside = false;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        Refresh();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        // エアロックを出たら EnvironmentSystem の制御を OxygenZone に戻す
        // （OxygenZone 側の OnTriggerEnter/Exit が自動で処理する）
    }

    /// <summary>
    /// Airlock の状態変化時に呼ぶ。
    /// プレイヤーがエアロック内にいる場合のみ EnvironmentSystem を更新する。
    /// </summary>
    public void Refresh()
    {
        if (!playerInside) return;
        if (airlock == null) return;
        if (EnvironmentSystem.Instance == null) return;

        bool hasAir = airlock.CurrentState == Airlock.State.Pressurized;
        EnvironmentSystem.Instance.SetZoneAir(hasAir);
    }
}