using UnityEngine;
using TMPro;

/// <summary>
/// 船内大気状況をリアルタイム表示するHUD（左上）。
/// ShipAtmosphereSystem の値を毎フレーム取得して表示する。
/// </summary>
public class ShipAtmosphereHUD : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI atmosphereText;   // "247 m³ / 500 m³"
    [SerializeField] private TextMeshProUGUI consumptionText;  // "消費 15 m³/h"
    [SerializeField] private TextMeshProUGUI generationText;   // "生成 20 m³/h"

    [Header("警告設定")]
    [SerializeField] private float warningRatio = 0.2f;        // 残量20%以下で赤点滅
    [SerializeField] private float blinkInterval = 0.5f;

    // 点滅用
    private float blinkTimer = 0f;
    private bool blinkVisible = true;

    // 色定数
    private static readonly Color32 ColorNormal = new Color32(255, 255, 255, 255);
    private static readonly Color32 ColorWarning = new Color32(255, 60, 60, 255);
    private static readonly Color32 ColorPositive = new Color32(100, 255, 100, 255);
    private static readonly Color32 ColorNegative = new Color32(255, 60, 60, 255);

    void Update()
    {
        if (ShipAtmosphereSystem.Instance == null) return;

        var sys = ShipAtmosphereSystem.Instance;
        float current = sys.CurrentAtmosphere;
        float max = sys.MaxAtmosphere;
        float consume = sys.ConsumptionRate;
        float generate = sys.GenerationRate;

        // 残量表示
        if (atmosphereText != null)
        {
            atmosphereText.text = $"{current:F0} m³ / {max:F0} m³";
            bool warning = max > 0f && (current / max) <= warningRatio;
            if (warning)
            {
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= blinkInterval)
                {
                    blinkTimer = 0f;
                    blinkVisible = !blinkVisible;
                }
                atmosphereText.color = blinkVisible ? ColorWarning : ColorNormal;
            }
            else
            {
                blinkTimer = 0f;
                blinkVisible = true;
                atmosphereText.color = ColorNormal;
            }
        }

        if (consumptionText != null)
            consumptionText.text = $"消費 {consume:F1} m³/h";

        if (generationText != null)
            generationText.text = $"生成 {generate:F1} m³/h";
    }
}