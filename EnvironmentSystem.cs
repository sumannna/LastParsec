using UnityEngine;

public class EnvironmentSystem : MonoBehaviour
{
    [Header("環境設定")]
    public bool hasAir = true;
    public bool hasCentrifugalGravity = false;

    public bool HasAir => hasAir;
    public bool HasCentrifugalGravity => hasCentrifugalGravity;

    // シングルトン（どこからでもアクセスできる）
    public static EnvironmentSystem Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Tキーで空気あり/なし切り替え（テスト用）
        if (Input.GetKeyDown(KeyCode.T))
        {
            hasAir = !hasAir;
            Debug.Log($"環境切り替え：空気{(hasAir ? "あり" : "なし")}");
        }

        // Uキーで無重力/遠心重力切り替え（テスト用）
        if (Input.GetKeyDown(KeyCode.J))
        {
            hasCentrifugalGravity = !hasCentrifugalGravity;
            Debug.Log($"環境切り替え：重力{(hasCentrifugalGravity ? "あり" : "なし")}");
        }
    }
}
