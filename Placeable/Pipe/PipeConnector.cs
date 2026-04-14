using UnityEngine;

/// <summary>
/// パイプ接続口。各機械のパイプ接続部GameObjectにアタッチする。
/// </summary>
public class PipeConnector : MonoBehaviour
{
    [Header("設定")]
    public PipeConnectorType connectorType; // 入口 or 出口
    public LiquidData acceptedLiquid;       // null = 種類問わず接続可

    [Header("接続状態（読み取り専用）")]
    public PipeConnector connectedTo;
    public float currentFlow;    // 現在の流量 L/s
    public LiquidData currentLiquidType; // 現在流れている液体種

    public bool IsConnected => connectedTo != null;

    /// <summary>接続先GameObjectのコンポーネントを取得</summary>
    public T GetConnectedMachine<T>() where T : Component
    {
        if (connectedTo == null) return null;
        return connectedTo.GetComponentInParent<T>();
    }

    /// <summary>液体を押し込む。種類が違えばfalseを返す</summary>
    public bool PushLiquid(LiquidData liquid, float amount)
    {
        if (IsConnected && connectedTo.acceptedLiquid != null
            && connectedTo.acceptedLiquid != liquid)
        {
            Debug.Log($"[PipeConnector] 液体種不一致: {liquid.liquidName} != {connectedTo.acceptedLiquid.liquidName}");
            return false;
        }
        currentLiquidType = liquid;
        currentFlow = amount;
        return true;
    }

    public void Connect(PipeConnector other)
    {
        connectedTo = other;
        other.connectedTo = this;
        Debug.Log($"[PipeConnector] 接続: {gameObject.name} <-> {other.gameObject.name}");
    }

    public void Disconnect()
    {
        if (connectedTo != null && connectedTo.gameObject != null)
        {
            connectedTo.connectedTo = null;
            connectedTo.currentFlow = 0f;
            connectedTo.currentLiquidType = null;
        }
        connectedTo = null;
        currentFlow = 0f;
        currentLiquidType = null;
    }
}

public enum PipeConnectorType
{
    Inlet,  // 入口
    Outlet  // 出口
}