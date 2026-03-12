using System.Collections;
using UnityEngine;

/// <summary>
/// 床に固定配置する素材アイテム用ピックアップ。
/// 拾得後に一定時間で復活する。
/// </summary>
public class RespawnPickup : MonoBehaviour
{
    [Header("アイテム設定")]
    public ItemData itemData;
    public int amount = 1;
    public float pickupDistance = 2f;
    public float respawnTime = 1f;

    [Header("参照")]
    public Inventory inventory;
    public InventoryUI inventoryUI;

    private bool isPickedUp = false;

    // 同フレームで複数オブジェクトが同時に拾われるのを防ぐ
    private static int lastPickupFrame = -1;

    // 見た目を制御するコンポーネントをキャッシュ
    private Renderer[] renderers;
    private Collider[] colliders;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
    }

    void Update()
    {
        if (isPickedUp) return;
        if (inventoryUI != null && inventoryUI.IsOpen) return;
        if (lastPickupFrame == Time.frameCount) return; // 同フレームで既に拾い済み

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist <= pickupDistance && Input.GetKeyDown(KeyCode.E))
            TryPickup();
    }

    void TryPickup()
    {
        if (itemData == null) return;
        lastPickupFrame = Time.frameCount; // 同フレームの他オブジェクトをブロック

        // スタック数分追加（スタック可能なのでループで積む）
        int remaining = amount;
        while (remaining > 0)
        {
            if (inventory.IsFull())
            {
                Debug.Log("インベントリ満杯：拾えなかった");
                return;
            }
            inventory.AddItem(itemData);
            remaining--;
        }

        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.RefreshAll();

        StartCoroutine(RespawnCoroutine());
    }

    IEnumerator RespawnCoroutine()
    {
        isPickedUp = true;
        SetVisible(false);

        yield return new WaitForSeconds(respawnTime);

        isPickedUp = false;
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        foreach (var r in renderers) r.enabled = visible;
        foreach (var c in colliders) c.enabled = visible;
    }
}