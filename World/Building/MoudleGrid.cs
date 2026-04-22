using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 空間モジュールのグリッドを一元管理するSingleton。
/// グリッド原点はデフォルト空間モジュールのTransformで外部からセットする。
/// </summary>
public class ModuleGrid : MonoBehaviour
{
    public static ModuleGrid Instance { get; private set; }

    public const float CellSize = 5f;

    // グリッド座標 -> 空間モジュール
    private Dictionary<Vector3Int, SpaceModule> grid = new Dictionary<Vector3Int, SpaceModule>();

    // グリッド原点（デフォルト空間モジュールのワールド位置）
    private Vector3 gridOrigin;

    // HUD方向（Zマイナス）は建築禁止
    // 原点セル(0,0,0)はデフォルトモジュールなので建築禁止のZマイナス側とは Z < 0
    private static readonly Vector3Int[] Neighbors = new Vector3Int[]
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.forward,
        Vector3Int.back,
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -----------------------------------------------
    // 初期化
    // -----------------------------------------------
    public void SetOrigin(Vector3 origin)
    {
        gridOrigin = origin;
    }

    // -----------------------------------------------
    // 座標変換
    // -----------------------------------------------
    public Vector3Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = worldPos - gridOrigin;
        return new Vector3Int(
            Mathf.RoundToInt(local.x / CellSize),
            Mathf.RoundToInt(local.y / CellSize),
            Mathf.RoundToInt(local.z / CellSize)
        );
    }

    public Vector3 GridToWorld(Vector3Int cell)
    {
        return gridOrigin + new Vector3(cell.x * CellSize, cell.y * CellSize, cell.z * CellSize);
    }

    // -----------------------------------------------
    // 登録・解除
    // -----------------------------------------------
    public void RegisterModule(Vector3Int cell, SpaceModule module)
    {
        grid[cell] = module;
    }

    public void UnregisterModule(Vector3Int cell)
    {
        grid.Remove(cell);
    }

    public SpaceModule GetModule(Vector3Int cell)
    {
        grid.TryGetValue(cell, out var m);
        return m;
    }

    public bool HasModule(Vector3Int cell) => grid.ContainsKey(cell);

    // -----------------------------------------------
    // 設置可否チェック
    // -----------------------------------------------
    public bool CanPlace(Vector3Int cell)
    {
        if (cell.z < 0)
        {
            Debug.Log($"[CanPlace] NG cell={cell} z<0");
            return false;
        }
        if (HasModule(cell))
        {
            Debug.Log($"[CanPlace] NG cell={cell} already has module");
            return false;
        }
        bool adj = HasAdjacentModule(cell);
        Debug.Log($"[CanPlace] cell={cell} hasAdjacent={adj}");
        return adj;
    }

    bool HasAdjacentModule(Vector3Int cell)
    {
        foreach (var dir in Neighbors)
        {
            if (HasModule(cell + dir)) return true;
        }
        return false;
    }

    // -----------------------------------------------
    // 撤去可否チェック（孤立判定）
    // -----------------------------------------------
    public bool CanRemove(Vector3Int cell)
    {
        if (!HasModule(cell)) return false;

        // 原点セルは撤去不可
        if (cell == Vector3Int.zero) return false;

        // 仮に除去した状態で、他の全モジュールが原点から到達可能か確認
        var removed = grid[cell];
        grid.Remove(cell);

        bool connected = AllModulesConnected();

        grid[cell] = removed;
        return connected;
    }

    bool AllModulesConnected()
    {
        if (!HasModule(Vector3Int.zero)) return false;

        var visited = new HashSet<Vector3Int>();
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(Vector3Int.zero);
        visited.Add(Vector3Int.zero);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var dir in Neighbors)
            {
                var neighbor = current + dir;
                if (!visited.Contains(neighbor) && HasModule(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        // 全グリッドセルが到達可能か
        foreach (var cell in grid.Keys)
        {
            if (!visited.Contains(cell)) return false;
        }
        return true;
    }

    // -----------------------------------------------
    // 隣接モジュール取得（疑似重力の伝播に使用）
    // -----------------------------------------------
    public List<SpaceModule> GetModulesInRow(Vector3Int origin, Vector3Int axis)
    {
        var result = new List<SpaceModule>();
        var cell = origin;
        while (HasModule(cell))
        {
            result.Add(grid[cell]);
            cell += axis;
        }
        cell = origin - axis;
        while (HasModule(cell))
        {
            result.Add(grid[cell]);
            cell -= axis;
        }
        return result;
    }
}