using UnityEngine;

/// <summary>
/// 建築プレビュー。視線先のグリッドセルに半透明メッシュを表示し、
/// 設置可否に応じて緑/赤マテリアルを切り替える。
/// </summary>
public class BuildingPreview : MonoBehaviour
{
    [SerializeField] private Material previewMaterial;
    [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.35f);
    [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.35f);

    private GameObject previewInstance;
    private Renderer[] previewRenderers;
    private bool isValid;
    private GameObject currentPrefab;

    public bool IsValid => isValid;

    // -----------------------------------------------
    // プレビュー表示
    // -----------------------------------------------
    public void Show(GameObject prefab, Vector3 worldPos, bool valid, Quaternion rotation = default)
    {
        // prefabが未生成、または選択建築物が切り替わった場合は再生成
        if (previewInstance == null || currentPrefab != prefab)
            CreatePreview(prefab);

        previewInstance.SetActive(true);
        previewInstance.transform.position = worldPos;
        previewInstance.transform.rotation = rotation;

        if (valid != isValid)
        {
            isValid = valid;
            ApplyColor(valid ? validColor : invalidColor);
        }
    }

    public void Hide()
    {
        if (previewInstance != null)
            previewInstance.SetActive(false);
    }

    // -----------------------------------------------
    // プレビュー生成
    // -----------------------------------------------
    void CreatePreview(GameObject prefab)
    {
        if (previewInstance != null)
            Destroy(previewInstance);

        previewInstance = Instantiate(prefab);
        currentPrefab = prefab;   // 現在のprefabを記録

        // Collider・スクリプトを無効化(プレビュー専用にする)
        foreach (var col in previewInstance.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var mb in previewInstance.GetComponentsInChildren<MonoBehaviour>())
            mb.enabled = false;

        previewRenderers = previewInstance.GetComponentsInChildren<Renderer>();
        foreach (var r in previewRenderers)
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = previewMaterial;
            r.sharedMaterials = mats;
        }
        ApplyColor(invalidColor);
        isValid = false;
    }

    void ApplyColor(Color color)
    {
        foreach (var r in previewRenderers)
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            r.SetPropertyBlock(mpb);
        }
    }

    void OnDestroy()
    {
        if (previewInstance != null)
            Destroy(previewInstance);
    }
}