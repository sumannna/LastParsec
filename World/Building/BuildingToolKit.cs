using System.Collections.Generic;
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
    // 状態
    private enum Mode { None, Build, Remove }
    private Mode currentMode = Mode.None;

    private BuildingData selectedBuilding;
    private float rightClickHeldTime = 0f;
    private bool radialOpened = false;

    // 建築物の回転ステップ（ラジアルメニュー切替後も保持）
    private int wallRotStep = 0;
    private int staircaseRotStep = 0;

    // モジュール設置時の壁ハイライト
    [SerializeField] private Material wallHighlightMaterial;
    private List<(Renderer r, Material mat)> highlightedWalls = new List<(Renderer, Material)>();

    // 撤去
    private float removeHeldTime = 0f;
    private SpaceModule removeTarget;
    private Vector3Int targetNormal = Vector3Int.zero;

    // Vキー（Placeable撤去）
    private bool placeableRemoveMode = false;
    private float placeableRemoveHeldTime = 0f;

    // GravityModuleのRキー競合防止用
    public bool IsBuildMode => currentMode == Mode.Build && GetSelectedToolkit() != null;

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
            // ...（既存Vキー処理）
            return;
        }

        // Rキー：壁/階段の向き変更（BuildingMode中かつツールキット装備中のみ）
        if (Input.GetKeyDown(KeyCode.R) && hasToolkit && !anyUIOpen
            && currentMode == Mode.Build && selectedBuilding != null)
        {
            switch (selectedBuilding.buildingType)
            {
                case BuildingType.PartitionWall:
                case BuildingType.HalfWall:
                    wallRotStep = (wallRotStep + 1) % 3;
                    Debug.Log($"[BuildingToolkit] 壁回転ステップ: {wallRotStep}");
                    break;
                case BuildingType.Staircase:
                    staircaseRotStep = (staircaseRotStep + 1) % 4;
                    Debug.Log($"[BuildingToolkit] 階段回転ステップ: {staircaseRotStep}");
                    break;
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
            ClearWallHighlight();
            return;
        }

        // モジュール以外の建築タイプではハイライト不要
        if (selectedBuilding.buildingType != BuildingType.SpaceModule &&
            selectedBuilding.buildingType != BuildingType.GravityModule)
        {
            ClearWallHighlight();
        }

        switch (selectedBuilding.buildingType)
        {
            case BuildingType.SpaceModule:
            case BuildingType.GravityModule:
                HandleModuleBuild();
                break;
            case BuildingType.PartitionWall:
                HandleWallBuild();
                break;
            case BuildingType.HalfWall:
                HandleHalfWallBuild();
                break;
            case BuildingType.Staircase:
                HandleStaircaseBuild();
                break;
        }
    }

    // -----------------------------------------------
    // 空間モジュール / 疑似重力モジュール
    // -----------------------------------------------
    void HandleModuleBuild()
    {
        if (!TryGetModulePlaceTarget(out Vector3Int placeCell, out Vector3 worldPos))
        {
            preview.Hide();
            ClearWallHighlight();
            return;
        }

        // z<0方向（HUD側）は非表示
        if (placeCell.z < 0)
        {
            preview.Hide();
            ClearWallHighlight();
            return;
        }

        float distToCell = Vector3.Distance(cameraTransform.position, worldPos);
        bool inRange = distToCell <= placeDistance;

        bool canPlace = ModuleGrid.Instance.CanPlace(placeCell)
                        && HasCost(selectedBuilding)
                        && inRange;

        Debug.Log($"[HandleModuleBuild] placeCell={placeCell} worldPos={worldPos} canPlace={canPlace}");

        // 設置可の時のみ壁ハイライト
        if (canPlace)
            SetWallHighlight(placeCell);
        else
            ClearWallHighlight();

        preview.Show(selectedBuilding.placePrefab, worldPos, canPlace);

        if (Input.GetMouseButtonDown(0) && canPlace)
            PlaceBuilding(placeCell, worldPos);
    }

    // -----------------------------------------------
    // モジュール設置専用ターゲット取得
    // RaycastAll全ヒットを走査し、placeCellが有効な最初の面を返す
    // 「有効」= モジュールが存在する面の外側が空セルであること
    // -----------------------------------------------
    bool TryGetModulePlaceTarget(out Vector3Int placeCell, out Vector3 worldPos)
    {
        placeCell = Vector3Int.zero;
        worldPos = Vector3.zero;

        if (ModuleGrid.Instance == null) return false;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, placementLayerMask, QueryTriggerInteraction.Collide);
        if (hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            // SpaceModule以外はスキップ
            if (h.collider.GetComponentInParent<SpaceModule>() == null) continue;

            Vector3 n = h.normal;
            Vector3Int gridN = GetClosestGridDirection(n);
            if (gridN == Vector3Int.zero) continue;

            Vector3 gridNf = new Vector3(gridN.x, gridN.y, gridN.z);
            Vector3 offsetHalf = gridNf * (ModuleGrid.CellSize * 0.5f);
            Vector3Int cellA = ModuleGrid.Instance.WorldToGrid(h.point + offsetHalf);
            Vector3Int cellB = ModuleGrid.Instance.WorldToGrid(h.point - offsetHalf);

            bool hasA = ModuleGrid.Instance.HasModule(cellA);
            bool hasB = ModuleGrid.Instance.HasModule(cellB);

            Vector3Int modCell, outNormal;
            if (hasA && !hasB) { modCell = cellA; outNormal = -gridN; }
            else if (!hasA && hasB) { modCell = cellB; outNormal = gridN; }
            else continue;

            Vector3Int candidate = modCell + outNormal;

            // placeCellにモジュールがある → スキップして次へ
            if (ModuleGrid.Instance.HasModule(candidate))
            {
                Debug.Log($"[TryGetModulePlaceTarget] placeCell occupied, skip: candidate={candidate}");
                continue;
            }

            placeCell = candidate;
            worldPos = ModuleGrid.Instance.GridToWorld(placeCell);
            return true;
        }

        Debug.Log("[TryGetModulePlaceTarget] 有効なヒットなし");
        return false;
    }

    // -----------------------------------------------
    // 壁ハイライト制御
    // -----------------------------------------------
    void SetWallHighlight(Vector3Int placeCell)
    {
        if (wallHighlightMaterial == null) return;

        // 収集する壁のRendererリストを作成
        var newRenderers = new List<Renderer>();

        // placeCell の6方向を確認し、モジュールが存在するセルの「placeCell側の壁」を収集
        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            Vector3Int.forward, Vector3Int.back
        };

        foreach (var dir in directions)
        {
            Vector3Int neighborCell = placeCell + dir;
            SpaceModule mod = ModuleGrid.Instance.GetModule(neighborCell);
            if (mod == null) continue;

            // neighborCellからplaceCellを向く方向の壁（-dir方向）
            Renderer r = mod.GetWallRenderer(-dir);
            if (r != null) newRenderers.Add(r);
        }

        // 前回と同じセットなら何もしない
        bool same = newRenderers.Count == highlightedWalls.Count;
        if (same)
        {
            for (int i = 0; i < newRenderers.Count; i++)
                if (newRenderers[i] != highlightedWalls[i].r) { same = false; break; }
        }
        if (same) return;

        ClearWallHighlight();

        foreach (var r in newRenderers)
        {
            highlightedWalls.Add((r, r.sharedMaterial));
            r.material = wallHighlightMaterial;
            Debug.Log($"[SetWallHighlight] 適用: {r.gameObject.name}");
        }
    }

    void ClearWallHighlight()
    {
        foreach (var hw in highlightedWalls)
            if (hw.r != null) hw.r.material = hw.mat;
        highlightedWalls.Clear();
    }

    // -----------------------------------------------
    // 仕切り壁
    // -----------------------------------------------
    void HandleWallBuild()
    {
        if (!TryRaycastBuildingSurface(out Vector3Int moduleCell, out Vector3Int outwardNormal,
                                       out Vector3 hitPoint, out float surfaceFixedCoord))
        {
            preview.Hide();
            return;
        }

        // 面内2Dスナップで壁の配置位置を確定（2m境界±1,±3にスナップ）
        Vector3 worldPos = SnapWallPosition(hitPoint, outwardNormal, surfaceFixedCoord);

        targetNormal = outwardNormal;

        float distToCell = Vector3.Distance(cameraTransform.position, worldPos);
        bool inRange = distToCell <= placeDistance;

        // 側壁ルール：X/Z法線の面（側壁・SubDiv）から水平壁は不可
        bool isSideWall = (outwardNormal.x != 0 || outwardNormal.z != 0);
        bool isHorizontal = (wallRotStep % 3 == 2);

        bool canPlace = PartitionWall.CanPlace(moduleCell, outwardNormal)
                        && HasCost(selectedBuilding)
                        && inRange
                        && !(isSideWall && isHorizontal);

        Quaternion wallRot = GetWallRotation(outwardNormal, wallRotStep);

        Debug.Log($"[HandleWallBuild] worldPos={worldPos} outward={outwardNormal} sideWallViolation={isSideWall && isHorizontal} canPlace={canPlace}");

        preview.Show(selectedBuilding.placePrefab, worldPos, canPlace, wallRot);

        if (Input.GetMouseButtonDown(0) && canPlace)
            PlaceBuilding(moduleCell, worldPos);
    }

    // -----------------------------------------------
    // 階段
    // -----------------------------------------------
    void HandleStaircaseBuild()
    {
        if (!TryRaycastBuildingSurface(out Vector3Int moduleCell, out Vector3Int outwardNormal,
                                       out Vector3 hitPoint, out _))
        {
            preview.Hide();
            return;
        }

        // hitPointを2mセル中心（-2,0,+2）にスナップ
        Vector3 origin = ModuleGrid.Instance.GridOrigin;
        Vector3 worldPos = new Vector3(
            Snap2mCenter(hitPoint.x, origin.x),
            Snap2mCenter(hitPoint.y, origin.y),
            Snap2mCenter(hitPoint.z, origin.z)
        );

        // スナップ後のワールド座標からモジュールセルを再確認
        Vector3Int targetCell = ModuleGrid.Instance.WorldToGrid(worldPos);

        float distToCell = Vector3.Distance(cameraTransform.position, worldPos);
        bool inRange = distToCell <= placeDistance;

        bool canPlace = Staircase.CanPlace(targetCell)
                        && HasCost(selectedBuilding)
                        && inRange;

        Debug.Log($"[HandleStaircaseBuild] hitPoint={hitPoint} worldPos={worldPos} targetCell={targetCell} canPlace={canPlace}");

        preview.Show(selectedBuilding.placePrefab, worldPos, canPlace, GetStaircaseRotation(staircaseRotStep));

        if (Input.GetMouseButtonDown(0) && canPlace)
            PlaceBuilding(targetCell, worldPos);
    }

    // -----------------------------------------------
    // ハーフ壁
    // -----------------------------------------------
    void HandleHalfWallBuild()
    {
        if (!TryRaycastHalfWallSurface(out Vector3Int moduleCell, out Vector3Int outwardNormal,
                                        out Vector3 hitPoint, out float surfaceFixedCoord, out bool isEndFace))
        {
            preview.Hide();
            return;
        }

        Vector3 worldPos = SnapHalfWallPosition(hitPoint, outwardNormal, surfaceFixedCoord);
        targetNormal = outwardNormal;

        float distToCell = Vector3.Distance(cameraTransform.position, worldPos);
        bool inRange = distToCell <= placeDistance;

        bool canPlace;
        if (isEndFace)
        {
            // 端面（WallEndFace）からの延長：CanPlace・側壁ルールをスキップ
            canPlace = HasCost(selectedBuilding) && inRange;
        }
        else
        {
            // 側壁ルール：X/Z法線の面から水平壁は不可
            bool isSideWall = (outwardNormal.x != 0 || outwardNormal.z != 0);
            bool isHorizontal = (wallRotStep % 3 == 2);

            canPlace = HalfWall.CanPlace(moduleCell, outwardNormal)
                       && HasCost(selectedBuilding)
                       && inRange
                       && !(isSideWall && isHorizontal);
        }

        Quaternion wallRot = GetWallRotation(outwardNormal, wallRotStep);

        Debug.Log($"[HandleHalfWallBuild] worldPos={worldPos} outward={outwardNormal} isEndFace={isEndFace} canPlace={canPlace}");

        preview.Show(selectedBuilding.placePrefab, worldPos, canPlace, wallRot);

        if (Input.GetMouseButtonDown(0) && canPlace)
            PlaceBuilding(moduleCell, worldPos);
    }

    // ハーフ壁用Raycast：WallEndFace端面を最優先でヒット検出
    bool TryRaycastHalfWallSurface(out Vector3Int moduleCell, out Vector3Int outwardNormal,
                                    out Vector3 hitPoint, out float surfaceFixedCoord, out bool isEndFace)
    {
        moduleCell = Vector3Int.zero;
        outwardNormal = Vector3Int.zero;
        hitPoint = Vector3.zero;
        surfaceFixedCoord = 0f;
        isEndFace = false;

        if (ModuleGrid.Instance == null) return false;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, placementLayerMask, QueryTriggerInteraction.Collide);
        if (hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            Vector3 n = h.normal;
            Vector3Int gridN = GetClosestGridDirection(n);
            if (gridN == Vector3Int.zero) continue;

            // -----------------------------------------------
            // WallEndFaceヒット（壁/ハーフ壁の端面）→ 最優先
            // -----------------------------------------------
            if (h.collider.GetComponentInParent<WallEndFace>() != null)
            {
                outwardNormal = gridN;
                hitPoint = h.point;
                isEndFace = true;

                Vector3 endPos = h.collider.transform.position;
                if (gridN.x != 0) surfaceFixedCoord = endPos.x;
                else if (gridN.y != 0) surfaceFixedCoord = endPos.y;
                else surfaceFixedCoord = endPos.z;

                moduleCell = ModuleGrid.Instance.WorldToGrid(h.point);

                Debug.Log($"[TryRaycastHalfWallSurface] WallEndFace HIT: outward={outwardNormal} surfaceFixed={surfaceFixedCoord}");
                return true;
            }

            // PartitionWall/HalfWall本体（端面でない部分）はスキップ
            if (h.collider.GetComponentInParent<PartitionWall>() != null ||
                h.collider.GetComponentInParent<HalfWall>() != null)
            {
                Debug.Log($"[TryRaycastHalfWallSurface] 壁本体スキップ: {h.collider.gameObject.name}");
                continue;
            }

            if (h.collider.GetComponentInParent<Staircase>() != null) continue;

            // -----------------------------------------------
            // SpaceModule外壁ヒット
            // -----------------------------------------------
            if (h.collider.GetComponentInParent<SpaceModule>() == null) continue;

            Vector3 gridNf = new Vector3(gridN.x, gridN.y, gridN.z);
            Vector3 offsetHalf = gridNf * (ModuleGrid.CellSize * 0.5f);
            Vector3Int cellA = ModuleGrid.Instance.WorldToGrid(h.point + offsetHalf);
            Vector3Int cellB = ModuleGrid.Instance.WorldToGrid(h.point - offsetHalf);
            bool hasA = ModuleGrid.Instance.HasModule(cellA);
            bool hasB = ModuleGrid.Instance.HasModule(cellB);

            if (hasA && !hasB) { moduleCell = cellA; outwardNormal = -gridN; }
            else if (!hasA && hasB) { moduleCell = cellB; outwardNormal = gridN; }
            else if (hasA && hasB)
            {
                Vector3 wA = ModuleGrid.Instance.GridToWorld(cellA);
                Vector3 wB = ModuleGrid.Instance.GridToWorld(cellB);
                float dA = Vector3.Dot(ray.direction, wA - ray.origin);
                float dB = Vector3.Dot(ray.direction, wB - ray.origin);
                if (dA > dB) { moduleCell = cellA; outwardNormal = -gridN; }
                else { moduleCell = cellB; outwardNormal = gridN; }
            }
            else continue;

            hitPoint = h.point;
            Vector3 modCenter = ModuleGrid.Instance.GridToWorld(moduleCell);
            Vector3 outNf = new Vector3(outwardNormal.x, outwardNormal.y, outwardNormal.z);
            Vector3 wc = modCenter + outNf * (ModuleGrid.CellSize * 0.5f);
            if (outwardNormal.x != 0) surfaceFixedCoord = wc.x;
            else if (outwardNormal.y != 0) surfaceFixedCoord = wc.y;
            else surfaceFixedCoord = wc.z;

            Debug.Log($"[TryRaycastHalfWallSurface] 外壁 HIT: moduleCell={moduleCell} outward={outwardNormal}");
            return true;
        }

        return false;
    }

    // 1mHalfスナップ（ハーフ壁用）：-2.5,-1.5,-0.5,+0.5,+1.5,+2.5 にスナップ
    float Snap1mHalf(float worldCoord, float originAxis)
    {
        float local = worldCoord - originAxis;
        return Mathf.Round(local - 0.5f) + 0.5f + originAxis;
    }

    // 法線方向は surfaceFixedCoord 固定、残り2軸を1mHalfスナップ
    Vector3 SnapHalfWallPosition(Vector3 hitPoint, Vector3Int outwardNormal, float surfaceFixedCoord)
    {
        Vector3 origin = ModuleGrid.Instance.GridOrigin;
        if (outwardNormal.x != 0)
            return new Vector3(surfaceFixedCoord,
                               Snap1mHalf(hitPoint.y, origin.y),
                               Snap1mHalf(hitPoint.z, origin.z));
        if (outwardNormal.y != 0)
            return new Vector3(Snap1mHalf(hitPoint.x, origin.x),
                               surfaceFixedCoord,
                               Snap1mHalf(hitPoint.z, origin.z));
        return new Vector3(Snap1mHalf(hitPoint.x, origin.x),
                           Snap1mHalf(hitPoint.y, origin.y),
                           surfaceFixedCoord);
    }

    // -----------------------------------------------
    // 共通Raycast：モジュール面に当たるまでRayを進める
    // moduleCell     : ヒット面が属するモジュールのセル
    // outwardNormal  : そのモジュールの面の外向き法線（グリッド単位）
    // hitPoint       : Rayのヒット点（面内スナップ計算に使用）
    // surfaceFixedCoord : 法線方向の固定ワールド座標（壁の面位置）
    // -----------------------------------------------
    bool TryRaycastBuildingSurface(out Vector3Int moduleCell, out Vector3Int outwardNormal,
                                   out Vector3 hitPoint, out float surfaceFixedCoord)
    {
        moduleCell = Vector3Int.zero;
        outwardNormal = Vector3Int.zero;
        hitPoint = Vector3.zero;
        surfaceFixedCoord = 0f;

        if (ModuleGrid.Instance == null) return false;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, placementLayerMask, QueryTriggerInteraction.Collide);
        if (hits.Length == 0)
        {
            Debug.Log("[TryRaycastBuildingSurface] Rayヒットなし");
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider.GetComponentInParent<PartitionWall>() != null)
            {
                Debug.Log($"[TryRaycastBuildingSurface] 既設PartitionWallスキップ: {h.collider.gameObject.name}");
                continue;
            }
            if (h.collider.GetComponentInParent<Staircase>() != null)
            {
                Debug.Log($"[TryRaycastBuildingSurface] 既設Staircaseスキップ: {h.collider.gameObject.name}");
                continue;
            }

            Vector3 n = h.normal;
            Vector3Int gridN = GetClosestGridDirection(n);
            if (gridN == Vector3Int.zero) continue;

            // -----------------------------------------------
            // SpaceModule外壁ヒット
            // -----------------------------------------------
            if (h.collider.GetComponentInParent<SpaceModule>() == null)
            {
                Debug.Log($"[TryRaycastBuildingSurface] SpaceModule外ヒットスキップ: {h.collider.gameObject.name}");
                continue;
            }

            Vector3 gridNf = new Vector3(gridN.x, gridN.y, gridN.z);
            Vector3 offsetHalf = gridNf * (ModuleGrid.CellSize * 0.5f);
            Vector3Int cellA = ModuleGrid.Instance.WorldToGrid(h.point + offsetHalf);
            Vector3Int cellB = ModuleGrid.Instance.WorldToGrid(h.point - offsetHalf);

            bool hasA = ModuleGrid.Instance.HasModule(cellA);
            bool hasB = ModuleGrid.Instance.HasModule(cellB);

            if (hasA && !hasB) { moduleCell = cellA; outwardNormal = -gridN; }
            else if (!hasA && hasB) { moduleCell = cellB; outwardNormal = gridN; }
            else if (hasA && hasB)
            {
                Vector3 worldA = ModuleGrid.Instance.GridToWorld(cellA);
                Vector3 worldB = ModuleGrid.Instance.GridToWorld(cellB);
                float dA = Vector3.Dot(ray.direction, worldA - ray.origin);
                float dB = Vector3.Dot(ray.direction, worldB - ray.origin);
                if (dA > dB) { moduleCell = cellA; outwardNormal = -gridN; }
                else { moduleCell = cellB; outwardNormal = gridN; }
            }
            else
            {
                Debug.Log($"[TryRaycastBuildingSurface] 空中ヒット透過: {h.collider.gameObject.name}");
                continue;
            }

            hitPoint = h.point;

            // 外壁の固定座標：モジュール中心 ± CellSize/2
            Vector3 modCenter = ModuleGrid.Instance.GridToWorld(moduleCell);
            Vector3 outNf = new Vector3(outwardNormal.x, outwardNormal.y, outwardNormal.z);
            Vector3 wallCenter = modCenter + outNf * (ModuleGrid.CellSize * 0.5f);
            if (outwardNormal.x != 0) surfaceFixedCoord = wallCenter.x;
            else if (outwardNormal.y != 0) surfaceFixedCoord = wallCenter.y;
            else surfaceFixedCoord = wallCenter.z;

            // outerFaceOnly=true（モジュール設置用）：内壁面ヒットをスキップし外壁面のみ使用
            Debug.Log($"[TryRaycastBuildingSurface] 外壁 HIT: {h.collider.gameObject.name} moduleCell={moduleCell} outward={outwardNormal} surfaceFixed={surfaceFixedCoord}");
            return true;
        }

        Debug.Log("[TryRaycastBuildingSurface] 有効なヒットなし");
        return false;
    }

    // -----------------------------------------------
    // 2mサブグリッドスナップ（モジュール中心起点、±1,±3にスナップ）
    // -----------------------------------------------
    float Snap2m(float worldCoord, float originAxis)
    {
        float local = worldCoord - originAxis;
        return Mathf.Round((local + 1f) / 2f) * 2f + originAxis - 1f;
    }

    // 2mサブグリッドセル中心スナップ（階段用）
    // セル中心位置：モジュール中心起点で -2, 0, +2
    float Snap2mCenter(float worldCoord, float originAxis)
    {
        float local = worldCoord - originAxis;
        return Mathf.Round(local / 2f) * 2f + originAxis;
    }

    // 法線方向は surfaceFixedCoord 固定、残り2軸を2mスナップ
    Vector3 SnapWallPosition(Vector3 hitPoint, Vector3Int outwardNormal, float surfaceFixedCoord)
    {
        Vector3 origin = ModuleGrid.Instance.GridOrigin;
        if (outwardNormal.x != 0)
            return new Vector3(surfaceFixedCoord,
                               Snap2m(hitPoint.y, origin.y),
                               Snap2m(hitPoint.z, origin.z));
        if (outwardNormal.y != 0)
            return new Vector3(Snap2m(hitPoint.x, origin.x),
                               surfaceFixedCoord,
                               Snap2m(hitPoint.z, origin.z));
        return new Vector3(Snap2m(hitPoint.x, origin.x),
                           Snap2m(hitPoint.y, origin.y),
                           surfaceFixedCoord);
    }

    // -----------------------------------------------
    // 仕切り壁のRotation計算
    // Prefabデフォルト：Scale(5,0.1,5)＝XZ水平板、法線=±Y
    // outwardNormalに応じて縦壁になるよう回転させる
    // -----------------------------------------------
    Quaternion GetWallRotation(Vector3Int outwardNormal, int step = 0)
    {
        // step 0: outwardNormalに垂直な縦壁（デフォルト）
        // step 1: outwardNormalに平行な縦壁（90度Y回転）
        // step 2: 水平板（床/天井）
        if (outwardNormal.x != 0)
        {
            switch (step % 3)
            {
                case 0: return Quaternion.Euler(0f, 0f, 90f);   // Z方向縦壁
                case 1: return Quaternion.Euler(0f, 90f, 90f);  // X方向縦壁
                case 2: return Quaternion.Euler(0f, 0f, 0f);    // 水平板
            }
        }
        if (outwardNormal.z != 0)
        {
            switch (step % 3)
            {
                case 0: return Quaternion.Euler(90f, 0f, 0f);   // X方向縦壁
                case 1: return Quaternion.Euler(90f, 0f, 90f);  // Z方向縦壁
                case 2: return Quaternion.Euler(0f, 0f, 0f);    // 水平板
            }
        }
        // outwardNormal.y != 0（床/天井面）
        switch (step % 3)
        {
            case 0: return Quaternion.Euler(0f, 0f, 0f);    // 水平板
            case 1: return Quaternion.Euler(0f, 0f, 90f);   // Z方向縦壁
            case 2: return Quaternion.Euler(90f, 0f, 0f);   // X方向縦壁
        }
        return Quaternion.identity;
    }

    Quaternion GetStaircaseRotation(int step)
    {
        return Quaternion.Euler(0f, step * 90f, 0f);
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

        Quaternion placeRotation;
        switch (selectedBuilding.buildingType)
        {
            case BuildingType.PartitionWall:
            case BuildingType.HalfWall:
                placeRotation = GetWallRotation(targetNormal, wallRotStep);
                break;
            case BuildingType.Staircase:
                placeRotation = GetStaircaseRotation(staircaseRotStep);
                break;
            default:
                placeRotation = Quaternion.identity;
                break;
        }

        GameObject obj = Instantiate(selectedBuilding.placePrefab, worldPos, placeRotation);

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

            case BuildingType.HalfWall:
                var hw = obj.GetComponent<HalfWall>();
                if (hw != null) hw.Initialize(cell, targetNormal);
                break;

            case BuildingType.Staircase:
                var stair = obj.GetComponent<Staircase>();
                var staircaseFacings = new Vector3Int[]
                {
                    new Vector3Int(0, 0, 1),   // step 0: forward
                    new Vector3Int(1, 0, 0),   // step 1: right
                    new Vector3Int(0, 0, -1),  // step 2: back
                    new Vector3Int(-1, 0, 0),  // step 3: left
                };
                if (stair != null) stair.Initialize(cell, staircaseFacings[staircaseRotStep % 4]);
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