using UnityEngine;

/// <summary>
/// ハーフ壁（1m×0.1m×2m）。
/// </summary>
public class HalfWall : MonoBehaviour, IInteractable
{
    public Vector3Int AttachedCell { get; private set; }
    public Vector3Int WallNormal { get; private set; }

    public void Initialize(Vector3Int cell, Vector3Int normal)
    {
        AttachedCell = cell;
        WallNormal = normal;
    }

    public static bool CanPlace(Vector3Int cell, Vector3Int normal)
    {
        if (ModuleGrid.Instance == null) return false;
        return ModuleGrid.Instance.HasModule(cell)
            || ModuleGrid.Instance.HasModule(cell + normal);
    }

    public string InteractionLabel => "撤去 [長押し左クリック]";
    public bool CanInteract => false;
    public void Interact() { }
    public void OnFocusEnter() { }
    public void OnFocusExit() { }
}