using UnityEngine;

/// <summary>
/// 漂流物本体。
/// Normal：スポーン点PからQを通る直線を飛翔、累積移動距離でDestroyする。
/// Initial：球殻内ランダム配置、ランダム方向へ飛翔、大球面到達でDestroyする。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class DebrisObject : MonoBehaviour, IInteractable
{
    // -----------------------------------------------
    // Inspector
    // -----------------------------------------------
    [Header("参照 (Spawner が自動設定。手動配置時はアサイン)")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private InventoryUI playerInventoryUI;

    [Header("ハイライト")]
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color highlightColor = new Color(0.4f, 0.4f, 0f, 1f);

    // -----------------------------------------------
    // 内部状態
    // -----------------------------------------------
    private enum SpawnType { Normal, Initial }
    private SpawnType spawnType;

    private DebrisData data;
    private int remainingYield;

    private Vector3 flightDir;
    private float flightSpeed;

    // Normal用
    private float traveledDistance;

    // Initial用
    private Vector3 shipCenter;
    private float largeSphereRadius;

    // 共通
    private Vector3 rotationAxis;
    private float rotationSpeed;

    private Rigidbody rb;

    private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

    // -----------------------------------------------
    // Awake
    // -----------------------------------------------
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // -----------------------------------------------
    // 初期化：通常スポーン
    // -----------------------------------------------
    public void Initialize(
        DebrisData debrisData,
        Vector3 spawnPos,
        Vector3 targetPos,
        Vector3 center,
        float smallRadius,
        Inventory inventory,
        InventoryUI inventoryUI)
    {
        data = debrisData;
        playerInventory = inventory;
        playerInventoryUI = inventoryUI;
        spawnType = SpawnType.Normal;

        remainingYield = debrisData.requiresPickaxe ? debrisData.maxYield : 0;
        traveledDistance = 0f;

        flightDir = (targetPos - spawnPos).normalized;
        flightSpeed = Random.Range(debrisData.approachSpeedMin, debrisData.approachSpeedMax);

        rotationAxis = Random.onUnitSphere;
        rotationSpeed = Random.Range(debrisData.rotationSpeedMin, debrisData.rotationSpeedMax);

        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>();
    }

    // -----------------------------------------------
    // 初期化：初期スポーン
    // -----------------------------------------------
    public void InitializeRandom(
        DebrisData debrisData,
        Vector3 center,
        float largeRadius,
        Inventory inventory,
        InventoryUI inventoryUI)
    {
        data = debrisData;
        playerInventory = inventory;
        playerInventoryUI = inventoryUI;
        spawnType = SpawnType.Initial;

        remainingYield = debrisData.requiresPickaxe ? debrisData.maxYield : 0;
        shipCenter = center;
        largeSphereRadius = largeRadius;

        flightDir = Random.onUnitSphere;
        flightSpeed = Random.Range(debrisData.approachSpeedMin, debrisData.approachSpeedMax);

        rotationAxis = Random.onUnitSphere;
        rotationSpeed = Random.Range(debrisData.rotationSpeedMin, debrisData.rotationSpeedMax);

        if (highlightRenderers == null || highlightRenderers.Length == 0)
            highlightRenderers = GetComponentsInChildren<Renderer>();
    }

    // -----------------------------------------------
    // FixedUpdate
    // -----------------------------------------------
    void FixedUpdate()
    {
        if (data == null) return;

        float step = flightSpeed * Time.fixedDeltaTime;
        Vector3 nextPos = rb.position + flightDir * step;
        rb.MovePosition(nextPos);

        // 自転
        Quaternion deltaRot = Quaternion.AngleAxis(rotationSpeed * Time.fixedDeltaTime, rotationAxis);
        rb.MoveRotation(rb.rotation * deltaRot);

        // 消滅判定
        if (spawnType == SpawnType.Normal)
        {
            traveledDistance += step;
            if (traveledDistance >= data.despawnDistance)
                Destroy(gameObject);
        }
        else
        {
            if (Vector3.Distance(nextPos, shipCenter) >= largeSphereRadius)
                Destroy(gameObject);
        }
    }

    // -----------------------------------------------
    // 母船衝突 → 反射
    // -----------------------------------------------
    void OnCollisionEnter(Collision collision)
    {
        flightDir = Vector3.Reflect(flightDir, collision.contacts[0].normal);
    }

    // -----------------------------------------------
    // ToolUser から呼ばれる（ツルハシ系）
    // -----------------------------------------------
    public void TakeMiningHit(Inventory inventory, InventoryUI inventoryUI)
    {
        if (data == null || !data.requiresPickaxe) return;

        bool isLastHit = remainingYield <= data.yieldPerHit;
        int take = isLastHit ? remainingYield : data.yieldPerHit;

        if (inventory == null || !inventory.HasSpaceFor(data.dropItem, take)) return;

        inventory.AddItemAmount(data.dropItem, take);
        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.RefreshAll();

        if (isLastHit)
        {
            Destroy(gameObject);
            return;
        }

        remainingYield -= data.yieldPerHit;
    }

    // -----------------------------------------------
    // IInteractable
    // -----------------------------------------------
    public string InteractionLabel =>
        data != null && data.requiresPickaxe ? "採掘 [左クリック]" : "拾う [E]";

    public bool CanInteract =>
        data != null && !data.requiresPickaxe;

    public void Interact()
    {
        if (data == null || data.requiresPickaxe) return;
        if (playerInventory == null) return;
        if (!playerInventory.HasSpaceFor(data.dropItem, data.yieldPerPickup)) return;

        playerInventory.AddItemAmount(data.dropItem, data.yieldPerPickup);
        if (playerInventoryUI != null && playerInventoryUI.IsOpen)
            playerInventoryUI.RefreshAll();

        Destroy(gameObject);
    }

    public void OnFocusEnter() => SetHighlight(highlightColor);
    public void OnFocusExit() => SetHighlight(Color.black);

    // -----------------------------------------------
    // ハイライト
    // -----------------------------------------------
    void SetHighlight(Color color)
    {
        foreach (var r in highlightRenderers)
        {
            if (r == null) continue;
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionColorProp, color);
            r.SetPropertyBlock(mpb);
        }
    }
}