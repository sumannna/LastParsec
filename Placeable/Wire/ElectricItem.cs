using UnityEngine;

/// <summary>
/// ホットバーにElectricItemDataを持つ間、視線先のElectricConnectorにクリックで接続する。
/// </summary>
public class ElectricItem : MonoBehaviour
{
    [Header("参照")]
    public Hotbar hotbar;
    public Camera playerCamera;
    public InventoryUI inventoryUI;

    [Header("設定")]
    public float reachDistance = 5f;
    public ElectricItemData electricItemData;

    [Header("ライン描画")]
    public Material lineMaterial;
    public float lineWidth = 0.05f;

    private ElectricConnector pendingConnector;
    private LineRenderer previewLine;

    void Update()
    {
        if (inventoryUI != null && inventoryUI.IsOpen) return;

        Hotbar.Slot selected = hotbar?.GetSelected();
        if (selected?.item != electricItemData)
        {
            CancelPending();
            return;
        }

        UpdatePreviewLine();

        if (!Input.GetMouseButtonDown(0)) return;

        ElectricConnector hit = GetLookedConnector();
        if (hit == null) return;

        if (pendingConnector == null)
        {
            pendingConnector = hit;
            Debug.Log($"[ElectricItem] 始点選択: {hit.gameObject.name}");
            CreatePreviewLine();
        }
        else
        {
            if (pendingConnector == hit) return;
            ConnectWire(pendingConnector, hit);
            CancelPending();
        }
    }

    void ConnectWire(ElectricConnector a, ElectricConnector b)
    {
        if (a.IsConnected) a.Disconnect();
        if (b.IsConnected) b.Disconnect();

        a.Connect(b);

        Hotbar.Slot selected = hotbar.GetSelected();
        if (selected != null)
        {
            selected.amount--;
            if (selected.amount <= 0)
            {
                selected.item = null;
                selected.amount = 0;
            }
        }

        CreatePersistentLine(a.transform.position, b.transform.position);
        Debug.Log($"[ElectricItem] 接続完了: {a.gameObject.name} <-> {b.gameObject.name}");
    }

    ElectricConnector GetLookedConnector()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, reachDistance)) return null;
        return hit.collider.GetComponent<ElectricConnector>();
    }

    void CreatePreviewLine()
    {
        if (previewLine != null) Destroy(previewLine.gameObject);
        GameObject obj = new GameObject("WirePreview");
        previewLine = obj.AddComponent<LineRenderer>();
        previewLine.material = lineMaterial;
        previewLine.startWidth = lineWidth;
        previewLine.endWidth = lineWidth;
        previewLine.positionCount = 2;
    }

    void UpdatePreviewLine()
    {
        if (previewLine == null || pendingConnector == null) return;
        previewLine.SetPosition(0, pendingConnector.transform.position);
        ElectricConnector hovered = GetLookedConnector();
        Vector3 endPos = hovered != null
            ? hovered.transform.position
            : playerCamera.transform.position + playerCamera.transform.forward * reachDistance;
        previewLine.SetPosition(1, endPos);
    }

    void CreatePersistentLine(Vector3 from, Vector3 to)
    {
        GameObject obj = new GameObject("WireLine");
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
    }

    void CancelPending()
    {
        pendingConnector = null;
        if (previewLine != null)
        {
            Destroy(previewLine.gameObject);
            previewLine = null;
        }
    }
}