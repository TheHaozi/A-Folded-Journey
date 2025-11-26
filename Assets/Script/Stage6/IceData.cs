using UnityEngine;

[CreateAssetMenu(fileName = "New Ice Data", menuName = "Ice Breaking/Ice Data")]
public class IceData : ScriptableObject
{
    [Header("碎冰贴图序列")]
    public Sprite[] iceSprites; // 碎冰Sprite序列，从完整到破碎
    
    [Header("点击音效")]
    public AudioClip clickSound; // 点击音效
    
    [Header("损毁音效")]
    public AudioClip destroySound; // 完全损毁音效
    
    [Header("点击效果设置")]
    public float clickEffectScale = 1.05f; // 点击时的缩放倍数
    public float clickEffectDuration = 0.1f; // 点击效果持续时间
    
    [Header("其他设置")]
    public bool enableClickEffect = true; // 是否启用点击效果
}