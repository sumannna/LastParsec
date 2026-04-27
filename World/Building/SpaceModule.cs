using UnityEngine;

/// <summary>
/// 空間モジュール本体。
/// 6面の壁を隣接モジュールの有無に応じて表示/非表示する。
/// </summary>
public class SpaceModule : MonoBehaviour
{
    [Header("壁オブジェクト（順：+X,-X,+Y,-Y,+Z,-Z）")]
    [SerializeField] private GameObject wallPosX;
    [SerializeField] private GameObject wallNegX;
    [SerializeField] private GameObject wallPosY;
    [SerializeField] private GameObject wallNegY;
    [SerializeField] private GameObject wallPosZ;
    [SerializeField] private GameObject wallNegZ;

    [Header("設定")]
    [SerializeField] private bool isDefault = false; // 原点モジュールはtrue

    public Vector3Int GridCell { get; private set; }
    public bool IsDefault => isDefault;

    // -----------------------------------------------
    // 初期化
    // -----------------------------------------------
    /// <summary>BuildingToolkitから設置時に呼ぶ</summary>
    public void Initialize(Vector3Int cell)
    {
        GridCell = cell;
        ModuleGrid.Instance.RegisterModule(cell, this);
        RefreshWalls();
        RefreshNeighborWalls();
        AddOxygenZone();
    }

    /// <summary>デフォルトモジュール（原点）用。Startで自動登録する</summary>
    void Start()
    {
        if (ModuleGrid.Instance == null)
        {
            Debug.LogError("[SpaceModule] ModuleGrid が見つかりません。実行順を確認してください。");
            return;
        }

        if (isDefault)
        {
            // SetOriginは原点モジュール（cell=(0,0,0)）のみ呼ぶ
            if (!ModuleGrid.Instance.IsOriginSet)
            {
                Debug.Log($"[SpaceModule] gridOrigin を {transform.position} に設定");
                ModuleGrid.Instance.SetOrigin(transform.position);
                GridCell = Vector3Int.zero;
            }
            else
            {
                // 2個目以降のisDefaultモジュール：原点からの相対セルを計算
                GridCell = ModuleGrid.Instance.WorldToGrid(transform.position);
            }
            ModuleGrid.Instance.RegisterModule(GridCell, this);
            RefreshWalls();
            AddOxygenZone();
        }
    }

        // -----------------------------------------------
        // OxygenZone追加
        // -----------------------------------------------
        void AddOxygenZone()
    {
        // 既にあれば追加しない
        if (GetComponent<OxygenZone>() != null) return;

        var col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one * (ModuleGrid.CellSize - 0.1f);
        gameObject.AddComponent<OxygenZone>();
    }

    // -----------------------------------------------
    // 壁の表示/非表示
    // -----------------------------------------------
    public void RefreshWalls()
    {
        if (ModuleGrid.Instance == null) return;

        SetWall(wallPosX, !ModuleGrid.Instance.HasModule(GridCell + Vector3Int.right));
        SetWall(wallNegX, !ModuleGrid.Instance.HasModule(GridCell + Vector3Int.left));
        SetWall(wallPosY, !ModuleGrid.Instance.HasModule(GridCell + Vector3Int.up));
        SetWall(wallNegY, !ModuleGrid.Instance.HasModule(GridCell + Vector3Int.down));
        SetWall(wallPosZ, !ModuleGrid.Instance.HasModule(GridCell + new Vector3Int(0, 0, 1)));
        SetWall(wallNegZ, !ModuleGrid.Instance.HasModule(GridCell + new Vector3Int(0, 0, -1)));
    }

    // -----------------------------------------------
    // 指定方向の壁Rendererを返す（ハイライト用）
    // -----------------------------------------------
    public Renderer GetWallRenderer(Vector3Int direction)
    {
        string wallName = DirectionToWallName(direction);
        Debug.Log($"[GetWallRenderer] direction={direction} wallName={wallName}");
        if (wallName == null) { Debug.Log("[GetWallRenderer] wallName null"); return null; }

        Transform wallTransform = transform.Find(wallName);
        Debug.Log($"[GetWallRenderer] wallTransform={wallTransform}");
        if (wallTransform == null) { Debug.Log($"[GetWallRenderer] {wallName} not found in {gameObject.name}"); return null; }

        Renderer r = wallTransform.GetComponent<Renderer>();
        Debug.Log($"[GetWallRenderer] Renderer={r}");
        return r;
    }

    string DirectionToWallName(Vector3Int dir)
    {
        if (dir == Vector3Int.right) return "Wall_PosX";
        if (dir == Vector3Int.left) return "Wall_NegX";
        if (dir == Vector3Int.up) return "Wall_PosY";
        if (dir == Vector3Int.down) return "Wall_NegY";
        if (dir == Vector3Int.forward) return "Wall_PosZ";
        if (dir == Vector3Int.back) return "Wall_NegZ";
        return null;
    }

    void SetWall(GameObject wall, bool visible)
    {
        if (wall != null) wall.SetActive(visible);
    }

    /// <summary>設置・撤去時に隣接モジュールの壁も更新する</summary>
    public void RefreshNeighborWalls()
    {
        if (ModuleGrid.Instance == null) return;

        var dirs = new Vector3Int[]
        {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        foreach (var dir in dirs)
        {
            var neighbor = ModuleGrid.Instance.GetModule(GridCell + dir);
            if (neighbor != null) neighbor.RefreshWalls();
        }
    }

    // -----------------------------------------------
    // 撤去
    // -----------------------------------------------
    public void Remove()
    {
        ModuleGrid.Instance.UnregisterModule(GridCell);
        RefreshNeighborWalls();
        Destroy(gameObject);
    }

    // -----------------------------------------------
    // エリア内にプレイヤーがいるか
    // -----------------------------------------------
    public bool HasPlayerInside()
    {
        var center = transform.position;
        float half = ModuleGrid.CellSize * 0.5f - 0.1f;
        var hits = Physics.OverlapBox(center, Vector3.one * half);
        foreach (var h in hits)
        {
            if (h.CompareTag("Player")) return true;
        }
        return false;
    }

    // -----------------------------------------------
    // エリア内のPlaceableを全て取得
    // -----------------------------------------------
    public GameObject[] GetPlaceablesInside()
    {
        var center = transform.position;
        float half = ModuleGrid.CellSize * 0.5f - 0.1f;
        var hits = Physics.OverlapBox(center, Vector3.one * half);
        var list = new System.Collections.Generic.List<GameObject>();
        foreach (var h in hits)
        {
            if (h.GetComponent<PlaceableData>() != null)
                list.Add(h.gameObject);
        }
        return list.ToArray();
    }
}