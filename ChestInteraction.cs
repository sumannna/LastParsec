using UnityEngine;

/// <summary>
/// チェストへのインタラクト処理。
/// EキーでUIを開閉、距離外に出たら自動で閉じる。
/// </summary>
public class ChestInteraction : MonoBehaviour
{
    [Header("設定")]
    public float interactRange = 2f;

    [Header("参照")]
    public Transform playerTransform;
    public ChestInventory chestInventory;
    public Inventory playerInventory;
    public InventoryUI inventoryUI;
    public PickupSpawner pickupSpawner;

    private bool isOpen = false;
    public bool IsOpen => isOpen;

    void Update()
    {
        if (isOpen && !IsPlayerInRange())
        {
            CloseChest();
            return;
        }

        if (!IsPlayerInRange())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (ChestUI.Instance != null && ChestUI.Instance.ClosedThisFrame) return;
            if (ChestUI.Instance != null && ChestUI.Instance.IsOpen) return;
            if (isOpen)
            {
                CloseChest();
                return;
            }
            if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return;
            OpenChest();
        }
    }

    void OpenChest()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return;

        isOpen = true;

        if (ChestUI.Instance != null)
        {
            ChestUI.Instance.Open(this);
        }
    }

    public void CloseChest()
    {
        if (!isOpen) return;
        isOpen = false;
        if (ChestUI.Instance != null && ChestUI.Instance.CurrentChest == this)
            ChestUI.Instance.Close();
    }

    public bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        return Vector3.Distance(playerTransform.position, transform.position) <= interactRange;
    }

    /// <summary>チェストを回収してGameObjectを削除する</summary>
    public void Collect()
    {
        chestInventory.Collect(playerInventory, pickupSpawner);
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}