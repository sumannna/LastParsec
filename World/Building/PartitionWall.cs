using UnityEngine;

/// <summary>
/// 仕切り壁。空間モジュールの面（5m単位）にスナップして設置する。
/// 必ず既存の壁または空間モジュール境界面に接続している必要がある。
/// </summary>
public class PartitionWall : MonoBehaviour, IInteractable
{
    public static readonly float WallThickness = 0.1f;

    // 設置情報
    public Vector3Int AttachedCell { get; private set; }   // 所属する空間モジュールのセル
    public Vector3Int WallNormal { get; private set; }     // 壁の法線方向（グリッド単位）

    // -----------------------------------------------
    // 初期化
    // -----------------------------------------------
    public void Initialize(Vector3Int cell, Vector3Int normal)
    {
        AttachedCell = cell;
        WallNormal = normal;
    }

    // -----------------------------------------------
    // 設置可否チェック（static）
    // -----------------------------------------------
    /// <summary>
    /// 指定セル境界面に仕切り壁を設置できるか判定する。
    /// 条件：隣接する2セルのどちらかに空間モジュールが存在し、
    ///       壁面がモジュール境界または既存仕切り壁と接続している。
    /// </summary>
    public static bool CanPlace(Vector3Int cell, Vector3Int normal)
    {
        if (ModuleGrid.Instance == null) return false;

        // 壁面の両側のどちらかにモジュールが必要
        bool hasCellA = ModuleGrid.Instance.HasModule(cell);
        bool hasCellB = ModuleGrid.Instance.HasModule(cell + normal);

        return hasCellA || hasCellB;
    }

    // -----------------------------------------------
    // IInteractable（撤去ヒント表示用）
    // -----------------------------------------------
    public string InteractionLabel => "撤去 [長押し左クリック]";
    public bool CanInteract => false;
    public void Interact() { }
    public void OnFocusEnter() { }
    public void OnFocusExit() { }
}