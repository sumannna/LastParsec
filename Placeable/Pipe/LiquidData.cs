using UnityEngine;

[CreateAssetMenu(fileName = "NewLiquid", menuName = "LastParsec/LiquidData")]
public class LiquidData : ScriptableObject
{
    [Header("基本情報")]
    public string liquidName;
    public Sprite icon;
    public Color pipeColor = Color.white; // パイプの色分け用
}