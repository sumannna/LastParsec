using UnityEngine;

/// <summary>
/// 階段。空間モジュール1個分（5m×5m）のフットプリントで設置する。
/// 高さは仕切り壁と同じ（5m）。
/// </summary>
public class Staircase : MonoBehaviour, IInteractable
{
    // 設置情報
    public Vector3Int AttachedCell { get; private set; }
    public Vector3Int FacingDirection { get; private set; } // 昇る向き

    // -----------------------------------------------
    // 初期化
    // -----------------------------------------------
    public void Initialize(Vector3Int cell, Vector3Int facing)
    {
        AttachedCell = cell;
        FacingDirection = facing;
    }

    // -----------------------------------------------
    // 設置可否チェック（static）
    // -----------------------------------------------
    /// <summary>
    /// 指定セル内に階段を設置できるか判定する。
    /// 条件：セルに空間モジュールが存在し、
    ///       階段の足元（cell）と頭（cell + up）の両方にモジュールがある。
    /// </summary>
    public static bool CanPlace(Vector3Int cell)
    {
        if (ModuleGrid.Instance == null) return false;
        return ModuleGrid.Instance.HasModule(cell);
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