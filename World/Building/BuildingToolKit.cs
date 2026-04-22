using UnityEngine;

/// <summary>
/// ツールキット選択中の建築・撤去を制御するコンポーネント。
/// プレイヤーにアタッチする。
/// </summary>
public class BuildingToolkit : MonoBehaviour
{
    public static BuildingToolkit Instance { get; private set; }

    [Header("参照")]
    [SerializeField] private Hotbar hotbar;
    [SerializeField] private Inventory inventory;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private BuildingPreview preview;
    [SerializeField] private VitalSystem vitalSystem;

    [Header("設定")]
    [SerializeField] private float placeDistance = 8f;
    [SerializeField] private float holdTime = 0.3f;       // 右クリック長押し判定秒数
    [SerializeField] private float removeHoldTime = 1f;   // 撤去長押し秒数
    [SerializeField] private LayerMask placementLayerMask;

    // 状態
    private enum Mode { None, Build, Remove }
    private Mode currentMode = Mode.None;

    private BuildingData selectedBuilding;
    private float rightClickHeldTime = 0f;
    private bool radialOpened = false;

    // 撤去
    private float removeHeldTime = 0f;
    private SpaceModule removeTarget;
    private Vector3Int targetNormal = Vector3Int.zero;

    // Vキー（Placeable撤去）
    private bool placeableRemoveMode = false;
    private float placeableRemoveHeldTime = 0f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        bool isDead = vitalSystem != null && vitalSystem.IsDead;
        if (isDead) return;

        bool anyUIOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();
        bool radialOpen = RadialMenuUI.Instance != null && RadialMenuUI.Instance.IsOpen;

        // -----------------------------------
        // ツールキットが選択中か判定
        // -----------------------------------
        ToolkitData toolkit = GetSelectedToolkit();
        bool hasToolkit = toolkit != null;

        // -----------------------------------
        // Vキー：モード切替
        // -----------------------------------
        if (Input.GetKeyDown(KeyCode.V) && !anyUIOpen)
        {
            if (hasToolkit)
            {
                // ツールキットあり：建築物撤去モード
                if (currentMode == Mode.Remove)
                    ExitRemoveMode();
                else
                    EnterRemoveMode();
            }
            else
            {
                // ツールキットなし：Placeable撤去モード
                placeableRemoveMode = !placeableRemoveMode;
                Debug.Log($"[BuildingToolkit] Placeable撤去モード: {placeableRemoveMode}");
            }
            return;
        }

        // -----------------------------------
        // Escなどでモード解除
        // -----------------------------------
        // ラジアルメニュー中はEsc以外の割り込みを無視
        if (radialOpen)
        {
            HandleRightClickRadial(toolkit);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || anyUIOpen)
        {
            ExitAllModes();
            return;
        }

        // ホイール・スロット変更で撤去モードのみ解除（建築モードは維持）
        if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f)
        {
            if (currentMode == Mode.Remove)
                ExitRemoveMode();
            placeableRemoveMode = false;
            return;
        }

        // -----------------------------------
        // ツールキット選択中の処理
        // -----------------------------------
        if (hasToolkit && !anyUIOpen)
        {
            // 初回またはselectedBuildingがnullなら最初のBuildingDataを選択
            if (selectedBuilding == null && toolkit.buildings != null && toolkit.buildings.Length > 0)
            {
                selectedBuilding = toolkit.buildings[0];
                Debug.Log($"[BuildingToolkit] 初期選択: {selectedBuilding.buildingName}");
            }

            // 建築モードでない場合は建築モードに戻す（撤去モード中は維持）
            if (currentMode == Mode.None && selectedBuilding != null)
                currentMode = Mode.Build;

            HandleRightClickRadial(toolkit);

            if (currentMode == Mode.Build && selectedBuilding != null)
                HandleBuildMode();
            else if (currentMode == Mode.Remove)
                HandleRemoveMode();
            else
                preview.Hide();
        }
        else
        {
            preview?.Hide();
        }

        // -----------------------------------
        // Placeable撤去モード
        // -----------------------------------
        if (placeableRemoveMode && !hasToolkit && !anyUIOpen)
            HandlePlaceableRemove();
    }

    // -----------------------------------------------
    // 右クリック長押し → ラジアルメニュー
    // -----------------------------------------------
    void HandleRightClickRadial(ToolkitData toolkit)
    {
        if (Input.GetMouseButton(1))
        {
            rightClickHeldTime += Time.deltaTime;
            if (!radialOpened && rightClickHeldTime >= holdTime)
            {
                radialOpened = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                RadialMenuUI.Instance?.Open(toolkit.buildings);
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (radialOpened && RadialMenuUI.Instance != null)
            {
                int idx = RadialMenuUI.Instance.SelectedIndex;
                if (idx >= 0 && idx < toolkit.buildings.Length)
                {
                    selectedBuilding = toolkit.buildings[idx];
                    currentMode = Mode.Build;
                    Debug.Log($"[BuildingToolkit] 選択: {selectedBuilding.buildingName}");
                }
                RadialMenuUI.Instance.Close();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            rightClickHeldTime = 0f;
            radialOpened = false;
        }
    }

    // -----------------------------------------------
    // 建築モード
    // -----------------------------------------------
    void HandleBuildMode()
    {
        if (selectedBuilding == null || selectedBuilding.placePrefab == null)
        {
            preview.Hide();
            return;
        }

        Vector3Int? targetCell = GetTargetCell(out Vector3 worldPos);

        Debug.Log($"[BuildingToolkit] targetCell={targetCell} worldPos={worldPos}");

        if (targetCell == null)
        {
            preview.Hide();
            return;
        }

        float dist = Vector3.Distance(cameraTransform.position, worldPos);
        bool canPlace_raw = ModuleGrid.Instance.CanPlace(targetCell.Value);
        bool hasCost = HasCost(selectedBuilding);
        Debug.Log($"[BuildingToolkit] dist={dist} canPlace={canPlace_raw} hasCost={hasCost}");

        float distToCell = Vector3.Distance(cameraTransform.position, worldPos);
        bool inRange = distToCell <= placeDistance;

        bool canPlace = ModuleGrid.Instance.CanPlace(targetCell.Value)
                        && HasCost(selectedBuilding)
                        && inRange;

        preview.Show(selectedBuilding.placePrefab, worldPos, canPlace);

        if (Input.GetMouseButtonDown(0) && canPlace)
            PlaceBuilding(targetCell.Value, worldPos);
    }

    Vector3Int? GetTargetCell(out Vector3 worldPos)
    {
        worldPos = Vector3.zero;
        targetNormal = Vector3Int.zero;
        if (ModuleGrid.Instance == null)
        {
            Debug.Log("[GetTargetCell] ModuleGrid.Instance is null");
            return null;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        Debug.Log($"[GetTargetCell] Ray origin={ray.origin} dir={ray.direction} mask={placementLayerMask.value}");

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementLayerMask))
        {
            Debug.Log("[GetTargetCell] Raycast hit nothing");
            return null;
        }

        Debug.Log($"[GetTargetCell] HIT name={hit.collider.gameObject.name} layer={LayerMask.LayerToName(hit.collider.gameObject.layer)} point={hit.point} normal={hit.normal}");

        // ヒットした壁の属する既存モジュールのセル（ヒット点から法線と逆に少し入ったところ）
        Vector3 n = hit.normal;
        // ヒット法線から先にグリッド軸方向を確定させる
        Vector3Int gridNormalOuter = GetClosestGridDirection(n);

        // ヒット面の内側（モジュール中心方向）にオフセットして既存セルを特定
        Vector3 offsetInward = -new Vector3(gridNormalOuter.x, gridNormalOuter.y, gridNormalOuter.z) * (ModuleGrid.CellSize * 0.5f);
        Vector3Int hitCell = ModuleGrid.Instance.WorldToGrid(hit.point + offsetInward);

        Vector3Int placeCell;
        if (ModuleGrid.Instance.HasModule(hitCell))
        {
            targetNormal = gridNormalOuter;
            placeCell = hitCell + gridNormalOuter;
        }
        else
        {
            // 内面ヒット：法線の逆方向が外向き
            Vector3Int gridNormalInner = GetClosestGridDirection(-n);
            targetNormal = gridNormalInner;
            Vector3 offsetInwardInner = -new Vector3(gridNormalInner.x, gridNormalInner.y, gridNormalInner.z) * (ModuleGrid.CellSize * 0.5f);
            hitCell = ModuleGrid.Instance.WorldToGrid(hit.point + offsetInwardInner);
            placeCell = hitCell + gridNormalInner;
        }

        worldPos = ModuleGrid.Instance.GridToWorld(placeCell);

        Debug.Log($"[GetTargetCell] hitCell={hitCell} placeCell={placeCell} worldPos={worldPos}");
        return placeCell;
    }

    Vector3Int GetClosestGridDirection(Vector3 n)
    {
        Vector3Int result = Vector3Int.zero;
        float maxDot = 0f;
        foreach (var dir in new Vector3Int[]
        {
        Vector3Int.right, Vector3Int.left,
        Vector3Int.up, Vector3Int.down,
        new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
        })
        {
            float d = Vector3.Dot(n, new Vector3(dir.x, dir.y, dir.z));
            if (d > maxDot) { maxDot = d; result = dir; }
        }
        return result;
    }

    void PlaceBuilding(Vector3Int cell, Vector3 worldPos)
    {
        ConsumeCost(selectedBuilding);

        GameObject obj = Instantiate(selectedBuilding.placePrefab, worldPos, Quaternion.identity);

        switch (selectedBuilding.buildingType)
        {
            case BuildingType.SpaceModule:
                var module = obj.GetComponent<SpaceModule>();
                if (module != null) module.Initialize(cell);
                break;

            case BuildingType.GravityModule:
                var gravity = obj.GetComponent<GravityModule>();
                if (gravity != null) gravity.Initialize(cell);
                break;

            case BuildingType.PartitionWall:
                if (targetNormal != Vector3Int.zero)
                {
                    var wall = obj.GetComponent<PartitionWall>();
                    if (wall != null) wall.Initialize(cell, targetNormal);
                }
                break;

            case BuildingType.Staircase:
                var stair = obj.GetComponent<Staircase>();
                if (stair != null) stair.Initialize(cell, Vector3Int.forward);
                break;
        }

        Debug.Log($"[BuildingToolkit] 設置: {selectedBuilding.buildingName} at {cell}");
    }

    // -----------------------------------------------
    // 撤去モード（建築物）
    // -----------------------------------------------
    void EnterRemoveMode()
    {
        currentMode = Mode.Remove;
        Debug.Log("[BuildingToolkit] 撤去モード ON");
    }

    void ExitRemoveMode()
    {
        currentMode = Mode.None;
        removeHeldTime = 0f;
        removeTarget = null;
        preview.Hide();
        Debug.Log("[BuildingToolkit] 撤去モード OFF");
    }

    void HandleRemoveMode()
    {
        SpaceModule module = GetTargetModule();

        if (module == null || module.IsDefault)
        {
            removeHeldTime = 0f;
            removeTarget = null;
            preview.Hide();
            return;
        }

        // プレビュー表示（赤）
        preview.Show(selectedBuilding?.placePrefab ?? module.gameObject,
             module.transform.position, false);

        if (Input.GetMouseButton(0))
        {
            if (removeTarget != module)
            {
                removeTarget = module;
                removeHeldTime = 0f;
            }

            removeHeldTime += Time.deltaTime;

            if (removeHeldTime >= removeHoldTime)
            {
                TryRemoveModule(module);
                removeHeldTime = 0f;
                removeTarget = null;
            }
        }
        else
        {
            removeHeldTime = 0f;
            removeTarget = null;
        }
    }

    void TryRemoveModule(SpaceModule module)
    {
        if (!ModuleGrid.Instance.CanRemove(module.GridCell))
        {
            Debug.Log("[BuildingToolkit] 撤去不可：孤立するか原点");
            return;
        }

        if (module.HasPlayerInside())
        {
            Debug.Log("[BuildingToolkit] 撤去不可：プレイヤーがエリア内にいる");
            return;
        }

        // 内部のPlaceableを全て消去
        foreach (var placeable in module.GetPlaceablesInside())
            Destroy(placeable);

        module.Remove();
        Debug.Log($"[BuildingToolkit] 撤去完了: {module.GridCell}");
    }

    SpaceModule GetTargetModule()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, placeDistance)) return null;
        return hit.collider.GetComponentInParent<SpaceModule>();
    }

    // -----------------------------------------------
    // Placeable撤去モード（ツールキットなし）
    // -----------------------------------------------
    void HandlePlaceableRemove()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, placeDistance)) return;

        // InteractionManagerのハイライトを流用したいが、
        // ここではPlaceableLayerのオブジェクトを対象にする
        var placeable = hit.collider.GetComponent<PlaceableData>();
        if (placeable == null) return;

        if (Input.GetMouseButton(0))
        {
            placeableRemoveHeldTime += Time.deltaTime;
            if (placeableRemoveHeldTime >= removeHoldTime)
            {
                Destroy(hit.collider.gameObject);
                placeableRemoveHeldTime = 0f;
                Debug.Log($"[BuildingToolkit] Placeable撤去: {hit.collider.gameObject.name}");
            }
        }
        else
        {
            placeableRemoveHeldTime = 0f;
        }
    }

    // -----------------------------------------------
    // コスト判定・消費
    // -----------------------------------------------
    bool HasCost(BuildingData data)
    {
        if (data.costs == null) return true;
        foreach (var cost in data.costs)
        {
            if (inventory.GetAmount(cost.item) < cost.amount) return false;
        }
        return true;
    }

    void ConsumeCost(BuildingData data)
    {
        if (data.costs == null) return;
        foreach (var cost in data.costs)
            inventory.RemoveItemAmount(cost.item, cost.amount);
    }

    // -----------------------------------------------
    // ユーティリティ
    // -----------------------------------------------
    void ExitAllModes()
    {
        currentMode = Mode.None;
        placeableRemoveMode = false;
        removeHeldTime = 0f;
        removeTarget = null;
        preview?.Hide();

        if (RadialMenuUI.Instance != null && RadialMenuUI.Instance.IsOpen)
        {
            RadialMenuUI.Instance.Close();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    ToolkitData GetSelectedToolkit()
    {
        if (hotbar == null) return null;
        var slots = hotbar.GetSlots();
        int idx = hotbar.SelectedIndex;
        if (slots == null || idx < 0 || idx >= slots.Length) return null;
        return slots[idx]?.item as ToolkitData;
    }
}