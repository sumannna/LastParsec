using UnityEngine;

public class OxygenCanister : MonoBehaviour
{
    [Header("アイテム設定")]
    public ItemData itemData;
    public int amount = 1;
    public float oxygenAmount = 0f;
    public float pickupDistance = 2f;

    [Header("インスタンス")]
    public OxygenTankInstance oxyTankInstance;
    public ThrusterTankInstance thrusterTankInstance;
    public SpacesuitInstance spacesuitInstance;

    [Header("参照")]
    public OxygenSystem oxygenSystem;
    public Inventory inventory;
    public InventoryUI inventoryUI;

    void Update()
    {
        if (inventoryUI != null && inventoryUI.IsOpen) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist <= pickupDistance && Input.GetKeyDown(KeyCode.E))
            Pickup();
    }

    void Pickup()
    {
        if (itemData == null) return;

        bool added = false;

        if (itemData is OxygenTankData oxyTankData)
        {
            OxygenTankInstance inst = oxyTankInstance ?? new OxygenTankInstance(oxyTankData);
            added = inventory.AddItemWithTank(itemData, inst);
        }
        else if (itemData is ThrusterTankData thrusterData)
        {
            ThrusterTankInstance inst = thrusterTankInstance ?? new ThrusterTankInstance(thrusterData);
            added = inventory.AddItemWithThruster(itemData, inst);
        }
        else if (itemData is SpacesuitData spacesuitData)
        {
            SpacesuitInstance inst = spacesuitInstance ?? new SpacesuitInstance(spacesuitData);
            added = inventory.AddItemWithSpacesuit(itemData, inst);
        }
        else
        {
            // amount分まとめて追加
            if (!inventory.IsFull())
            {
                inventory.AddItemAmount(itemData, amount);
                added = true;
            }
        }

        if (!added)
        {
            Debug.Log("インベントリ満杯");
            return;
        }

        if (oxygenAmount > 0f && oxygenSystem != null)
            oxygenSystem.AddOxygen(oxygenAmount);

        Destroy(gameObject);
    }
}