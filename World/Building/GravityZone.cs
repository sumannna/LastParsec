using UnityEngine;

/// <summary>
/// 疑似重力ゾーン。プレイヤーが入ったらPlayerControllerに重力方向を通知する。
/// GravityModuleが生成・管理する。
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class GravityZone : MonoBehaviour
{
    private Vector3 gravityDirection = Vector3.down;
    [SerializeField] private float gravityStrength = 9.8f;

    public Vector3 GravityDirection => gravityDirection;
    public float GravityStrength => gravityStrength;

    private int playerCount = 0;

    public void Setup(Vector3 direction, float strength)
    {
        gravityDirection = direction.normalized;
        gravityStrength = strength;

        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one * (ModuleGrid.CellSize - 0.1f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerCount++;
        NotifyPlayer(other, true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerCount = Mathf.Max(0, playerCount - 1);
        NotifyPlayer(other, false);
    }

    void NotifyPlayer(Collider other, bool entered)
    {
        var pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        if (entered)
            pc.EnterGravityZone(this);
        else
            pc.ExitGravityZone(this);
    }

    public bool HasPlayer => playerCount > 0;
}