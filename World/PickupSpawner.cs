using UnityEngine;

public class PickupSpawner : MonoBehaviour
{
    public static PickupSpawner Instance { get; private set; }

    [Header("設定")]
    public GameObject pickupPrefab;
    public float spawnDistance = 1.5f;

    [Header("参照")]
    public Camera playerCamera;
    public OxygenSystem oxygenSystem;
    public Inventory inventory;
    public InventoryUI inventoryUI;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnItem(ItemData item, int amount,
        OxygenTankInstance oxyTank = null,
        ThrusterTankInstance thrusterTank = null,
        SpacesuitInstance spacesuitInst = null)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        // プレイヤーの前方に排出
        Vector3 spawnPos = player.transform.position +
            player.transform.forward * spawnDistance;

        GameObject obj = Instantiate(pickupPrefab, spawnPos, Quaternion.identity);
        OxygenCanister pickup = obj.GetComponent<OxygenCanister>();

        if (pickup != null)
        {
            pickup.itemData = item;
            pickup.amount = amount;
            pickup.oxyTankInstance = oxyTank;
            pickup.thrusterTankInstance = thrusterTank;
            pickup.spacesuitInstance = spacesuitInst;
            pickup.oxygenSystem = oxygenSystem;
            pickup.inventory = inventory;
            pickup.inventoryUI = inventoryUI;
        }

        // Rigidbodyで浮遊させる
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        // カメラの向きで排出（未設定時はプレイヤーのforward）
        Vector3 throwDir = (playerCamera != null)
            ? playerCamera.transform.forward
            : player.transform.forward;

        rb.useGravity = false;
        rb.velocity = throwDir * 0.5f;

        // プレイヤーとの衝突のみ無視
        Collider playerCol = player.GetComponent<Collider>();
        if (playerCol != null)
            foreach (Collider col in obj.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(col, playerCol);
    }
}