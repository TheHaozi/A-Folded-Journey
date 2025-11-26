using UnityEngine;
using System.Collections;

public class IceBehavior : MonoBehaviour
{
    [Header("碎冰数据")]
    public IceData iceData;
    
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private int currentSpriteIndex = 0;
    private bool isDestroying = false; // 防止重复销毁
    
    void Start()
    {
        InitializeIce();
    }
    
    /// <summary>
    /// 初始化碎冰
    /// </summary>
    public void InitializeIce()
    {
        // 获取SpriteRenderer组件
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("未找到SpriteRenderer组件！");
            return;
        }
        
        // 添加或获取AudioSource组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && iceData != null && (iceData.clickSound != null || iceData.destroySound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // 设置初始Sprite
        if (iceData != null && iceData.iceSprites.Length > 0)
        {
            spriteRenderer.sprite = iceData.iceSprites[0];
            currentSpriteIndex = 0;
        }
        else
        {
            Debug.LogWarning("未设置碎冰数据或Sprite序列！");
        }
    }
    
    void Update()
    {
        // 如果正在销毁过程中，不接收新的点击
        if (isDestroying) return;
        
        // 检测鼠标点击（2D版本）
        if (Input.GetMouseButtonDown(0))
        {
            // 将鼠标位置转换为世界坐标
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 mousePos2D = new Vector2(mousePos.x, mousePos.y);
            
            // 2D射线检测
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            
            // 检测是否点击到这个物体
            if (hit.collider != null && hit.collider.gameObject == this.gameObject)
            {
                BreakIce();
            }
        }
    }
    
    /// <summary>
    /// 碎冰效果处理
    /// </summary>
    public void BreakIce()
    {
        if (iceData == null || isDestroying) return;
        
        // 检查是否还有下一个Sprite
        if (currentSpriteIndex < iceData.iceSprites.Length - 1)
        {
            // 播放点击音效
            PlaySound(iceData.clickSound);
            
            // 切换到下一个Sprite
            currentSpriteIndex++;
            spriteRenderer.sprite = iceData.iceSprites[currentSpriteIndex];
            
            // 添加点击反馈效果
            if (iceData.enableClickEffect)
            {
                StartCoroutine(ClickEffect());
            }
        }
        else
        {
            // 最后一级破碎，播放损毁音效并销毁
            StartCoroutine(DestroyWithSound());
        }
    }
    
    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// 带音效的销毁协程
    /// </summary>
    private System.Collections.IEnumerator DestroyWithSound()
    {
        isDestroying = true;
        
        // 播放损毁音效
        PlaySound(iceData.destroySound);
        
        // 可选：在销毁前隐藏物体
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // 禁用碰撞体，防止再次被点击
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // 等待音效播放完成（如果正在播放）
        if (iceData.destroySound != null && audioSource != null)
        {
            // 等待音效长度的时间，确保音效播放完毕
            yield return new WaitForSeconds(iceData.destroySound.length);
        }
        else
        {
            // 如果没有音效，等待一帧
            yield return null;
        }
        
        // 现在安全销毁物体
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 点击反馈效果协程
    /// </summary>
    System.Collections.IEnumerator ClickEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * iceData.clickEffectScale;
        
        float halfDuration = iceData.clickEffectDuration / 2f;
        
        // 快速放大
        float timer = 0f;
        while (timer < halfDuration)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, timer / halfDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        
        // 快速缩小回原尺寸
        timer = 0f;
        while (timer < halfDuration)
        {
            transform.localScale = Vector3.Lerp(targetScale, originalScale, timer / halfDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 设置碎冰数据
    /// </summary>
    public void SetIceData(IceData newIceData)
    {
        iceData = newIceData;
        InitializeIce();
    }
    
    /// <summary>
    /// 重置碎冰状态
    /// </summary>
    public void ResetIce()
    {
        currentSpriteIndex = 0;
        isDestroying = false;
        
        if (iceData != null && iceData.iceSprites.Length > 0)
        {
            spriteRenderer.sprite = iceData.iceSprites[0];
        }
        
        // 重新启用组件
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }
    }
    
    /// <summary>
    /// 获取当前破碎进度（0-1）
    /// </summary>
    public float GetBreakProgress()
    {
        if (iceData == null || iceData.iceSprites.Length <= 1) return 0f;
        return (float)currentSpriteIndex / (iceData.iceSprites.Length - 1);
    }
    
    /// <summary>
    /// 获取当前破碎阶段
    /// </summary>
    public int GetCurrentStage()
    {
        return currentSpriteIndex;
    }
    
    /// <summary>
    /// 获取总阶段数
    /// </summary>
    public int GetTotalStages()
    {
        return iceData != null ? iceData.iceSprites.Length : 0;
    }
}