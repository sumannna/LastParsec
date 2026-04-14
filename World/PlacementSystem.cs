using UnityEngine;

/// <summary>
/// ホットバーにPlaceableDataがある状態で左クリック → 設置プレビュー → 確定で設置。
/// </summary>
public class PlacementSystem : MonoBehaviour
{
    [Header("参照")]
    public Hotbar hotbar;
    public Inventory inventory;
    public InventoryUI inventoryUI;
    public HotbarUI hotbarUI;
    public Camera playerCamera;
    public VitalSystem vitalSystem;
    public OxygenSystem oxygenSystem;

    [Header("プレビュー設定")]
    public Material previewMaterial;    // 半透明マテリアル
    public Color validColor = new Color(0f, 1f, 0f, 0.4f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.4f);

    private GameObject previewObject;
    private PlaceableData currentPlaceable;
    private bool canPlace = false;

    void Update()
    {
        bool isDead = (vitalSystem != null && vitalSystem.IsDead)
                   || (oxygenSystem != null && oxygenSystem.IsGameOver);
        bool uiOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();
        if (isDead || uiOpen)
        {
            CancelPlacement();
            return;
        }

        // ホットバー選択アイテムを監視
        Hotbar.Slot selected = hotbar?.GetSelected();
        PlaceableData placeable = selected?.item as PlaceableData;

        if (placeable != currentPlaceable)
        {
            CancelPlacement();
            currentPlaceable = placeable;
        }

        if (currentPlaceable == null) return;

        // プレビュー生成
        if (previewObject == null)
            CreatePreview(currentPlaceable);

        UpdatePreview();

        // 左クリックで設置確定
        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            Place();
            return;
        }

        // Escで設置キャンセル
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    void CreatePreview(PlaceableData data)
    {
        if (data.placePrefab == null) return;

        previewObject = Instantiate(data.placePrefab);

        // コライダー無効化
        foreach (var col in previewObject.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // WorkbenchInteraction無効化
        foreach (var wb in previewObject.GetComponentsInChildren<WorkbenchInteraction>())
            wb.enabled = false;

        // ChestInteraction無効化
        foreach (var chest in previewObject.GetComponentsInChildren<ChestInteraction>())
            chest.enabled = false;

        // プレビューマテリアル適用
        if (previewMaterial != null)
        {
            foreach (var r in previewObject.GetComponentsInChildren<Renderer>())
                r.material = previewMaterial;
        }
    }

    void UpdatePreview()
    {
        if (previewObject == null || playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        bool hasGravity = EnvironmentSystem.Instance != null && EnvironmentSystem.Instance.HasCentrifugalGravity;
        LayerMask activeLayer = hasGravity ? currentPlaceable.placeLayerGravity : currentPlaceable.placeLayerZeroG;

        if (Physics.Raycast(ray, out RaycastHit hit, currentPlaceable.placeDistance, activeLayer))
        {
            previewObject.SetActive(true);
            // オブジェクトのバウンズ高さの半分だけ法線方向にオフセット
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
            float halfHeight = 0f;
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers)
                    bounds.Encapsulate(r.bounds);
                halfHeight = bounds.extents.y;
            }
            previewObject.transform.position = hit.point + hit.normal * halfHeight;
            previewObject.transform.up = hit.normal;
            if (hasGravity && hit.normal.y < 0.7f)
                canPlace = false;
            else if (hasGravity && hit.collider.CompareTag("Equipment"))
                canPlace = false;
            else
                canPlace = true;

            // めり込みチェック
            if (canPlace)
            {
                Bounds bounds = new Bounds(previewObject.transform.position, Vector3.zero);
                foreach (var r in previewObject.GetComponentsInChildren<Renderer>())
                    bounds.Encapsulate(r.bounds);

                Collider[] overlaps = Physics.OverlapBox(
                    bounds.center,
                    bounds.extents * 0.9f,
                    previewObject.transform.rotation);

                foreach (var col in overlaps)
                {
                    if (col.gameObject == previewObject) continue;
                    if (col.transform.IsChildOf(previewObject.transform)) continue;
                    canPlace = false;
                    break;
                }
            }

            if (previewMaterial != null)
                previewMaterial.color = canPlace ? validColor : invalidColor;
        }
        else
        {
            canPlace = false;
            previewObject.SetActive(false);

            if (previewMaterial != null)
                previewMaterial.color = invalidColor;
        }
    }

    void Place()
    {
        if (previewObject == null || currentPlaceable == null) return;

        // Prefabを設置
        GameObject placed = Instantiate(
            currentPlaceable.placePrefab,
            previewObject.transform.position,
            previewObject.transform.rotation);

        // WorkbenchInteractionにplayerTransformをセット
        foreach (var wb in placed.GetComponentsInChildren<WorkbenchInteraction>())
        {
            wb.playerTransform = transform;
        }

        foreach (var chest in placed.GetComponentsInChildren<ChestInteraction>())
        {
            chest.playerTransform = transform;
            chest.playerInventory = inventory;
            chest.pickupSpawner = FindObjectOfType<PickupSpawner>();
            chest.inventoryUI = inventoryUI;
        }

        // Hotbar only: currentPlaceable comes from GetSelected(), scanning Inventory would double-consume
        Hotbar.Slot hotbarSlot = hotbar.GetSelected();
        if (hotbarSlot != null && hotbarSlot.item == currentPlaceable)
        {
            hotbarSlot.amount--;
            if (hotbarSlot.amount <= 0)
            {
                hotbarSlot.item = null;
                hotbarSlot.amount = 0;
            }
        }

        if (hotbarUI != null) hotbarUI.RefreshAll();

        CancelPlacement();
    }

    void CancelPlacement()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
        canPlace = false;
        currentPlaceable = null;
    }
}