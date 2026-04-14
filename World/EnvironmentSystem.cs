using UnityEngine;

public class EnvironmentSystem : MonoBehaviour
{
    [Header("環境設定")]
    [SerializeField] private bool hasCentrifugalGravity = false;

    public bool HasCentrifugalGravity => hasCentrifugalGravity;

    public static EnvironmentSystem Instance { get; private set; }

    // ゾーン判定による空気フラグ（OxygenZoneが制御）
    private bool zoneHasAir = false;

    // デバッグオーバーライド（Tキーでトグル）
    private bool debugOverride = false;

    /// <summary>現在の有酸素状態。デバッグON時は常にtrue。</summary>
    public bool HasAir => debugOverride || zoneHasAir;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Tキー：デバッグオーバーライドのトグル
        if (Input.GetKeyDown(KeyCode.T))
        {
            debugOverride = !debugOverride;
            Debug.Log($"[EnvironmentSystem] デバッグ空気オーバーライド：{(debugOverride ? "ON（強制空気あり）" : "OFF")}");
        }

        // Jキー：遠心重力トグル（テスト用）
        if (Input.GetKeyDown(KeyCode.J))
        {
            hasCentrifugalGravity = !hasCentrifugalGravity;
            Debug.Log($"[EnvironmentSystem] 重力：{(hasCentrifugalGravity ? "あり" : "なし")}");
        }
    }

    /// <summary>OxygenZoneから呼ばれる。ゾーン内外と大気量を反映する。</summary>
    public void SetZoneAir(bool hasAir)
    {
        zoneHasAir = hasAir;
    }
}