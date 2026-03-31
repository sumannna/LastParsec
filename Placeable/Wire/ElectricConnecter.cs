using UnityEngine;

/// <summary>
/// 電気接続口。各機械・電源のGameObjectにアタッチする。
/// </summary>
public class ElectricConnector : MonoBehaviour
{
    [Header("設定")]
    public ElectricConnectorType connectorType;

    [Header("接続状態（読み取り専用）")]
    public ElectricConnector connectedTo;

    public bool IsConnected => connectedTo != null;

    public void Connect(ElectricConnector other)
    {
        connectedTo = other;
        other.connectedTo = this;
        Debug.Log($"[ElectricConnector] 接続: {gameObject.name} <-> {other.gameObject.name}");
    }

    public void Disconnect()
    {
        if (connectedTo != null)
        {
            connectedTo.connectedTo = null;
        }
        connectedTo = null;
    }
}

public enum ElectricConnectorType
{
    Supply,  // 電源側
    Consume  // 消費側
}