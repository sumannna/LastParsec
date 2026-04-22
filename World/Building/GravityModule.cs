using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 疑似重力空間モジュール。
/// 設置された面を重力面として、同一行の全モジュールにGravityZoneを付与する。
/// </summary>
public class GravityModule : SpaceModule
{
    [Header("疑似重力設定")]
    [SerializeField] private float gravityStrength = 9.8f;

    // 重力方向（重力面の法線の逆方向）
    // デフォルトは-Y（床が重力面）
    private Vector3Int gravityAxis = new Vector3Int(0, -1, 0);
    private List<GravityZone> managedZones = new List<GravityZone>();

    // Rキーで重力面を切り替えるための面の順序
    private static readonly Vector3Int[] FaceAxes = new Vector3Int[]
    {
        new Vector3Int(0, -1, 0),  // 床（下向き重力）
        new Vector3Int(0,  1, 0),  // 天井（上向き重力）
        new Vector3Int(-1, 0, 0),  // 左壁
        new Vector3Int( 1, 0, 0),  // 右壁
        new Vector3Int(0,  0, -1), // 後壁
        new Vector3Int(0,  0,  1), // 前壁
    };
    private int faceIndex = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            CycleFace();
    }

    // -----------------------------------------------
    // 重力面の切り替え
    // -----------------------------------------------
    void CycleFace()
    {
        faceIndex = (faceIndex + 1) % FaceAxes.Length;
        gravityAxis = FaceAxes[faceIndex];
        RefreshGravityZones();
        Debug.Log($"[GravityModule] 重力方向: {gravityAxis}");
    }

    // -----------------------------------------------
    // GravityZone生成・更新
    // -----------------------------------------------
    public void RefreshGravityZones()
    {
        ClearZones();

        if (ModuleGrid.Instance == null) return;

        // 重力軸に垂直な2軸を求める
        Vector3Int axis = gravityAxis;

        // 重力軸と同一行のモジュール群を取得
        // 行の方向は重力軸と垂直な任意の2軸
        var rowModules = GetRowModules();

        Vector3 gravDir = new Vector3(gravityAxis.x, gravityAxis.y, gravityAxis.z);

        foreach (var module in rowModules)
        {
            if (module == null) continue;

            // 既存GravityZoneがあれば再利用しない（新規追加）
            var zone = module.gameObject.AddComponent<GravityZone>();
            zone.Setup(gravDir, gravityStrength);
            managedZones.Add(zone);
        }
    }

    /// <summary>
    /// 重力軸と垂直な面（行）の全モジュールを取得する。
    /// 重力軸がYなら XZ平面の全モジュール、Xなら YZ平面、Zなら XY平面。
    /// </summary>
    List<SpaceModule> GetRowModules()
    {
        if (ModuleGrid.Instance == null) return new List<SpaceModule>();

        var result = new List<SpaceModule>();
        var visited = new HashSet<Vector3Int>();
        var queue = new Queue<Vector3Int>();

        queue.Enqueue(GridCell);
        visited.Add(GridCell);

        // 重力軸以外の方向に伝播
        var spreadDirs = GetSpreadDirections();

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            var module = ModuleGrid.Instance.GetModule(cell);
            if (module != null) result.Add(module);

            foreach (var dir in spreadDirs)
            {
                var next = cell + dir;
                if (!visited.Contains(next) && ModuleGrid.Instance.HasModule(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        return result;
    }

    Vector3Int[] GetSpreadDirections()
    {
        // 重力軸（とその逆）以外の4方向
        var all = new Vector3Int[]
        {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            new Vector3Int(0,0,1), new Vector3Int(0,0,-1)
        };

        var result = new List<Vector3Int>();
        foreach (var d in all)
        {
            if (d != gravityAxis && d != -gravityAxis)
                result.Add(d);
        }
        return result.ToArray();
    }

    void ClearZones()
    {
        foreach (var zone in managedZones)
        {
            if (zone != null) Destroy(zone);
        }
        managedZones.Clear();
    }

    // -----------------------------------------------
    // Initialize オーバーライド
    // -----------------------------------------------
    public new void Initialize(Vector3Int cell)
    {
        base.Initialize(cell);
        RefreshGravityZones();
    }

    void OnDestroy()
    {
        ClearZones();
    }
}