using System.Collections;
using UnityEngine;

/// <summary>
/// 漂流物を管理するSpawner。
/// 起動時に initialDebrisCount 個を球殻内にランダム配置する。
/// 以降は spawnInterval 秒ごとに通常スポーン（PQ直線飛翔）を行う。
/// </summary>
public class DebrisSpawner : MonoBehaviour
{
    [System.Serializable]
    public class DebrisEntry
    {
        public DebrisData data;
        [Range(0.1f, 10f)] public float weight = 1f;
    }

    [Header("スポーン設定")]
    [SerializeField] private DebrisEntry[] entries;
    [SerializeField] private float spawnInterval = 15f;
    [SerializeField] private int maxDebrisCount = 15;
    [SerializeField] private int initialDebrisCount = 10;

    [Header("球設定")]
    [SerializeField] private float largeSphereRadius = 40f;
    [SerializeField] private float middleSphereRadius = 25f;
    [SerializeField] private float smallSphereRadius = 15f;

    [Header("参照")]
    [SerializeField] private Transform shipTransform;
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private InventoryUI playerInventoryUI;

    private int currentCount = 0;

    void Start()
    {
        if (shipTransform == null)
            Debug.LogWarning("[DebrisSpawner] shipTransform が未アサイン");

        SpawnInitial();
        StartCoroutine(SpawnLoop());
    }

    // -----------------------------------------------
    // 初期スポーン
    // -----------------------------------------------
    void SpawnInitial()
    {
        int count = Mathf.Min(initialDebrisCount, maxDebrisCount);
        for (int i = 0; i < count; i++)
            SpawnOneInitial();
    }

    void SpawnOneInitial()
    {
        DebrisData debrisData = PickRandom();
        if (debrisData == null || debrisData.worldPrefab == null)
        {
            Debug.LogWarning("[DebrisSpawner] DebrisData または worldPrefab が未設定");
            return;
        }

        Vector3 center = shipTransform != null ? shipTransform.position : Vector3.zero;

        // 小球～大球の球殻内のランダム点
        float randomRadius = Random.Range(smallSphereRadius, largeSphereRadius);
        Vector3 spawnPos = center + Random.onUnitSphere * randomRadius;

        GameObject obj = Instantiate(debrisData.worldPrefab, spawnPos, Random.rotation);

        DebrisObject debris = obj.GetComponent<DebrisObject>();
        if (debris == null)
        {
            Debug.LogError($"[DebrisSpawner] {debrisData.worldPrefab.name} に DebrisObject がアタッチされていません");
            Destroy(obj);
            return;
        }

        debris.InitializeRandom(debrisData, center, largeSphereRadius, playerInventory, playerInventoryUI);

        DebrisDestroyTracker tracker = obj.AddComponent<DebrisDestroyTracker>();
        tracker.Init(this);

        currentCount++;
    }

    // -----------------------------------------------
    // 通常スポーンループ
    // -----------------------------------------------
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            if (currentCount < maxDebrisCount)
                SpawnOne();
        }
    }

    void SpawnOne()
    {
        DebrisData debrisData = PickRandom();
        if (debrisData == null || debrisData.worldPrefab == null)
        {
            Debug.LogWarning("[DebrisSpawner] DebrisData または worldPrefab が未設定");
            return;
        }

        Vector3 center = shipTransform != null ? shipTransform.position : Vector3.zero;

        // ① 大球面上のランダム点P
        Vector3 P = center + Random.onUnitSphere * largeSphereRadius;

        // ② PからOへの直線L（地軸）
        Vector3 L = (center - P).normalized;

        // ③ Lに垂直なベクトルを求める
        Vector3 perp = Vector3.Cross(L, Vector3.up).normalized;
        if (perp.magnitude < 0.001f)
            perp = Vector3.Cross(L, Vector3.right).normalized;

        // ④ 小球～中球のドーナツエリア上のランダム点Q
        float randomAngle = Random.Range(0f, 360f);
        float randomRadius = Random.Range(smallSphereRadius, middleSphereRadius);
        Vector3 Q = center + Quaternion.AngleAxis(randomAngle, L) * perp * randomRadius;

        GameObject obj = Instantiate(debrisData.worldPrefab, P, Random.rotation);

        DebrisObject debris = obj.GetComponent<DebrisObject>();
        if (debris == null)
        {
            Debug.LogError($"[DebrisSpawner] {debrisData.worldPrefab.name} に DebrisObject がアタッチされていません");
            Destroy(obj);
            return;
        }

        debris.Initialize(debrisData, P, Q, center, smallSphereRadius, playerInventory, playerInventoryUI);

        DebrisDestroyTracker tracker = obj.AddComponent<DebrisDestroyTracker>();
        tracker.Init(this);

        currentCount++;
        Debug.Log($"[DebrisSpawner] {debrisData.debrisName} をスポーン（現在 {currentCount}/{maxDebrisCount}）");
    }

    // -----------------------------------------------
    // カウント管理
    // -----------------------------------------------
    public void OnDebrisDestroyed()
    {
        currentCount = Mathf.Max(0, currentCount - 1);
    }

    // -----------------------------------------------
    // ランダム選択
    // -----------------------------------------------
    DebrisData PickRandom()
    {
        if (entries == null || entries.Length == 0) return null;

        float totalWeight = 0f;
        foreach (var e in entries)
            if (e.data != null) totalWeight += e.weight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var e in entries)
        {
            if (e.data == null) continue;
            cumulative += e.weight;
            if (roll <= cumulative) return e.data;
        }
        return entries[entries.Length - 1].data;
    }

    // -----------------------------------------------
    // Gizmo
    // -----------------------------------------------
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 center = shipTransform != null ? shipTransform.position : transform.position;
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(center, largeSphereRadius);
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(center, middleSphereRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(center, smallSphereRadius);
    }
#endif
}

/// <summary>
/// DebrisObject が破棄されたとき DebrisSpawner にカウント減算を通知する。
/// </summary>
public class DebrisDestroyTracker : MonoBehaviour
{
    private DebrisSpawner spawner;
    public void Init(DebrisSpawner s) => spawner = s;
    void OnDestroy() => spawner?.OnDebrisDestroyed();
}