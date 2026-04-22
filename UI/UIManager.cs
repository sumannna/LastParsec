using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private CraftUI craftUI;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void CloseAll()
    {
        if (inventoryUI != null && inventoryUI.IsOpen)
            inventoryUI.CloseInventory();
        if (craftUI != null && craftUI.IsOpen)
            craftUI.CloseCraft(cancel: true);
        if (CraftTreeUI.Instance != null && CraftTreeUI.Instance.IsOpen)
            CraftTreeUI.Instance.Close();
        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen)
            ChestUI.Instance.Close();
        if (IceMelterUI.Instance != null && IceMelterUI.Instance.IsOpen)
            IceMelterUI.Instance.Close();
        if (FillingMachineUI.Instance != null && FillingMachineUI.Instance.IsOpen)
            FillingMachineUI.Instance.Close();
        if (ElectrolyzerUI.Instance != null && ElectrolyzerUI.Instance.IsOpen)
            ElectrolyzerUI.Instance.Close();
        if (HandCrankerUI.Instance != null && HandCrankerUI.Instance.IsOpen)
            HandCrankerUI.Instance.Close();
        var tables = FindObjectsOfType<ResearchTableSystem>();
        foreach (var table in tables)
            if (table != null && table.IsOpen) table.ClosePanel();
        RadialMenuUI.Instance?.Close();
    }

    public bool IsAnyUIOpen()
    {
        if (inventoryUI != null && inventoryUI.IsOpen) return true;
        if (craftUI != null && craftUI.IsOpen) return true;
        if (CraftTreeUI.Instance != null && CraftTreeUI.Instance.IsOpen) return true;
        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen) return true;
        if (IceMelterUI.Instance != null && IceMelterUI.Instance.IsOpen) return true;
        if (FillingMachineUI.Instance != null && FillingMachineUI.Instance.IsOpen) return true;
        if (ElectrolyzerUI.Instance != null && ElectrolyzerUI.Instance.IsOpen) return true;
        if (HandCrankerUI.Instance != null && HandCrankerUI.Instance.IsOpen) return true;
        if (RadialMenuUI.Instance != null && RadialMenuUI.Instance.IsOpen) return true;
        var tables = FindObjectsOfType<ResearchTableSystem>();
        foreach (var table in tables)
            if (table != null && table.IsOpen) return true;
        return false;
    }

    public void OpenInventory()
    {
        CloseAll();
        inventoryUI?.OpenInventoryExternal();
    }

    public void OpenCraft()
    {
        CloseAll();
        craftUI?.OpenCraftExternal();
    }

    public void OpenResearchTable(ResearchTableSystem table)
    {
        CloseAll();
        table?.OpenPanelExternal();
    }

    public void OpenCraftTree(WorkbenchInteraction workbench)
    {
        CloseAll();
        CraftTreeUI.Instance?.Open(workbench);
    }

    public void OpenIceMelter(IceMelterInteraction interaction)
    {
        CloseAll();
        IceMelterUI.Instance?.Open(interaction.GetMachine());
    }

    public void OpenFillingMachine(FillingMachineInteraction interaction)
    {
        CloseAll();
        FillingMachineUI.Instance?.Open(interaction.GetMachine());
    }

    public void OpenElectrolyzer(ElectrolyzerInteraction interaction)
    {
        CloseAll();
        ElectrolyzerUI.Instance?.Open(interaction.GetMachine());
    }

    public void OpenHandCranker(HandCranker cranker)
    {
        CloseAll();
        HandCrankerUI.Instance?.Open(cranker);
    }
}