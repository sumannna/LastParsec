using UnityEngine;

/// <summary>
/// UIウィンドウの排他制御を一元管理するシングルトン。
/// 新しいウィンドウを開く前に必ずCloseAllを呼ぶことで重なりを防ぐ。
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("参照")]
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private CraftUI craftUI;

    // ResearchTableSystemはシングルトンで直接参照
    // CraftTreeUIはシングルトンで直接参照

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -----------------------------------------------
    // 公開API
    // -----------------------------------------------

    /// <summary>現在開いている全UIを閉じる</summary>
    public void CloseAll()
    {
        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.CloseInventory();

        if (craftUI != null && craftUI.IsOpen)
            craftUI.CloseCraft(cancel: true);

        if (ResearchTableSystem.Instance != null && ResearchTableSystem.Instance.IsOpen)
            ResearchTableSystem.Instance.ClosePanel();

        if (CraftTreeUI.Instance != null && CraftTreeUI.Instance.IsOpen)
            CraftTreeUI.Instance.Close();
    }

    /// <summary>インベントリを開く（他を閉じてから）</summary>
    public void OpenInventory()
    {
        CloseAll();
        inventoryUI?.OpenInventoryExternal();
    }

    /// <summary>クラフト画面を開く（他を閉じてから）</summary>
    public void OpenCraft()
    {
        CloseAll();
        craftUI?.OpenCraftExternal();
    }

    /// <summary>リサーチテーブルを開く（他を閉じてから）</summary>
    public void OpenResearchTable(ResearchTableSystem table)
    {
        CloseAll();
        table?.OpenPanelExternal();
    }

    /// <summary>クラフトツリーを開く（他を閉じてから）</summary>
    public void OpenCraftTree(WorkbenchInteraction workbench)
    {
        CloseAll();
        CraftTreeUI.Instance?.Open(workbench);
    }
}