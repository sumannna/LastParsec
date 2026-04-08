using UnityEngine;

/// <summary>
/// ホットバーにPipeItemDataを持つ間、視線先のPipeConnectorにクリックで接続する。
/// 1回目クリック：始点選択　2回目クリック：終点選択→接続
/// </summary>
public class PipeItem : MonoBehaviour
{
    [Header("参照")]
    public Hotbar hotbar;
    public Camera playerCamera;
    public InventoryUI inventoryUI;
    [SerializeField] private HotbarUI hotbarUI;

    [Header("設定")]
    public float reachDistance = 5f;
    public PipeItemData pipeItemData; // ホットバー判定用

    [Header("ライン描画")]
    public Material lineMaterial;
    public float lineWidth = 0.05f;

    private PipeConnector pendingConnector; // 1回目クリックで選んだ始点
    private LineRenderer previewLine;       // プレビュー用ライン

    void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return;

        // ホットバーの選択アイテムがPipeItemDataでなければ無効
        Hotbar.Slot selected = hotbar?.GetSelected();
        if (selected?.item != pipeItemData)
        {
            CancelPending();
            return;
        }

        UpdatePreviewLine();

        if (!Input.GetMouseButtonDown(0)) return;

        PipeConnector hit = GetLookedConnector();
        if (hit == null) return;

        if (pendingConnector == null)
        {
            // 1回目：始点選択
            pendingConnector = hit;
            Debug.Log($"[PipeItem] 始点選択: {hit.gameObject.name}");
            CreatePreviewLine();
        }
        else
        {
            // 2回目：終点選択→接続
            if (pendingConnector == hit)
            {
                Debug.Log("[PipeItem] 同じConnectorは接続不可");
                return;
            }
            ConnectPipe(pendingConnector, hit);
            CancelPending();
        }
    }

    void ConnectPipe(PipeConnector a, PipeConnector b)
    {
        // 既存接続を切断
        if (a.IsConnected) a.Disconnect();
        if (b.IsConnected) b.Disconnect();

        a.Connect(b);

        // パイプアイテムを1個消費
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

        // 接続ラインを永続表示
        CreatePersistentLine(a.transform.position, b.transform.position);
        hotbarUI?.RefreshAll();
        Debug.Log($"[PipeItem] 接続完了: {a.gameObject.name} <-> {b.gameObject.name}");
        FindObjectOfType<HotbarUI>()?.RefreshAll();
    }

    PipeConnector GetLookedConnector()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, reachDistance)) return null;
        return hit.collider.GetComponent<PipeConnector>();
    }

    void CreatePreviewLine()
    {
        if (previewLine != null) Destroy(previewLine.gameObject);
        GameObject obj = new GameObject("PipePreview");
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
        PipeConnector hovered = GetLookedConnector();
        Vector3 endPos = hovered != null
            ? hovered.transform.position
            : playerCamera.transform.position + playerCamera.transform.forward * reachDistance;
        previewLine.SetPosition(1, endPos);
    }

    void CreatePersistentLine(Vector3 from, Vector3 to)
    {
        GameObject obj = new GameObject("PipeLine");
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
