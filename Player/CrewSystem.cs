using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrewSystem : MonoBehaviour
{
    [Header("乗員設定")]
    public int initialCrew = 5;

    [Header("UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI crewText;
    public TextMeshProUGUI gameOverText;
    public Button respawnButton;

    [Header("参照")]
    public OxygenSystem oxygenSystem;
    public PlayerController playerController;
    public FuelSystem fuelSystem;
    public VitalSystem vitalSystem;
    public EquipmentSystem equipmentSystem;
    public Inventory inventory;
    public InventoryUI inventoryUI;

    private int currentCrew;
    private bool isDeathHandled = false;

    void Start()
    {
        currentCrew = initialCrew;
        gameOverPanel.SetActive(false);
        gameOverText.gameObject.SetActive(false);
        UpdateCrewText();
    }

    public void OnPlayerDeath()
    {
        if (isDeathHandled) return;
        isDeathHandled = true;

        gameOverPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (currentCrew <= 0)
        {
            respawnButton.gameObject.SetActive(false);
            gameOverText.gameObject.SetActive(true);
        }
        else
        {
            respawnButton.gameObject.SetActive(true);
            gameOverText.gameObject.SetActive(false);
        }
    }

    public void Respawn()
    {
        isDeathHandled = false;
        currentCrew--;
        UpdateCrewText();
        gameOverPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;

        // インベントリを全消去
        inventory.ClearAll();

        // 全装備を新品で再装備
        equipmentSystem.RespawnEquip();

        // 各システム回復
        oxygenSystem.Revive();
        fuelSystem.AddFuel(100f);
        playerController.ResetVelocity();
        vitalSystem.Revive();

        // インベントリUIを更新
        inventoryUI.CloseInventory();
    }

    void UpdateCrewText()
    {
        crewText.text = $"乗員数：{currentCrew}名";
    }
}
