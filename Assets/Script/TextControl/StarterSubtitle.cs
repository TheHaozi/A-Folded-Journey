using UnityEngine;
using TMPro;
using System.Collections;

public class StarterSubtitle : MonoBehaviour
{
    [Header("字幕内容")]
    [TextArea(3, 5)]
    public string subtitleText = "欢迎来到游戏世界！";
    
    [Header("文本样式设置")]
    public TMP_FontAsset fontAsset;          // 字体资源
    public int fontSize = 32;                // 字号
    public Color textColor = Color.white;    // 文字颜色
    public FontStyles fontStyle = FontStyles.Normal; // 字体样式
    public TextAlignmentOptions alignment = TextAlignmentOptions.Center; // 对齐方式
    
    [Header("布局设置")]
    public Vector2 screenPosition = new Vector2(0, -150); // 屏幕位置
    public Vector2 size = new Vector2(800, 60);          // 文本框大小
    public bool autoSize = false;            // 是否自动调整大小
    
    [Header("显示设置")]
    public float displayDuration = 3f;       // 显示时间
    public float fadeInDuration = 1f;        // 淡入时间
    public float fadeOutDuration = 1f;       // 淡出时间
    
    [Header("触发设置")]
    public float destroyDistance = 8f;       // 销毁距离
    public bool destroyOnLeave = true;       // 离开时销毁
    
    private GameObject subtitleObject;
    private Transform player;
    private bool hasBeenDestroyed = false;
    private Coroutine currentFadeCoroutine;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        CreateSubtitle();
    }
    
    void Update()
    {
        if (hasBeenDestroyed || subtitleObject == null || player == null) return;
        
        // 检测距离
        float distance = Vector2.Distance(transform.position, player.position);
        
        if (destroyOnLeave && distance > destroyDistance)
        {
            DestroySubtitleWithFade();
        }
    }
    
    void CreateSubtitle()
    {
        // 查找或创建Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }
        
        // 创建字幕对象
        subtitleObject = new GameObject("StarterSubtitle");
        subtitleObject.transform.SetParent(canvas.transform);
        
        // 添加TMP组件并配置样式
        ConfigureTextMeshPro(subtitleObject);
        
        // 开始显示动画
        StartCoroutine(ShowSubtitle());
    }
    
    void ConfigureTextMeshPro(GameObject textObject)
    {
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        
        // 基础文本设置
        tmp.text = subtitleText;
        tmp.color = new Color(textColor.r, textColor.g, textColor.b, 0f); // 初始透明
        
        // 字体设置
        if (fontAsset != null)
        {
            tmp.font = fontAsset;
        }
        
        // 字号和样式
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.alignment = alignment;
        
        // 布局设置
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchoredPosition = screenPosition;
        
        if (autoSize)
        {
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = fontSize * 0.5f;
            tmp.fontSizeMax = fontSize;
        }
        else
        {
            rect.sizeDelta = size;
        }
        
        // 其他文本效果
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableWordWrapping = true;
    }
    
    IEnumerator ShowSubtitle()
    {
        TextMeshProUGUI tmp = subtitleObject.GetComponent<TextMeshProUGUI>();
        
        // 淡入
        yield return StartCoroutine(FadeText(0f, 1f, fadeInDuration));
        
        // 保持显示
        yield return new WaitForSeconds(displayDuration);
        
        // 淡出后销毁
        yield return StartCoroutine(FadeText(1f, 0f, fadeOutDuration));
        
        // 销毁字幕
        DestroySubtitleImmediate();
    }
    
    void DestroySubtitleWithFade()
    {
        if (subtitleObject != null && !hasBeenDestroyed)
        {
            // 停止所有协程
            if (currentFadeCoroutine != null)
                StopCoroutine(currentFadeCoroutine);
            
            // 开始淡出并销毁
            currentFadeCoroutine = StartCoroutine(FadeAndDestroy());
        }
    }
    
    IEnumerator FadeAndDestroy()
    {
        TextMeshProUGUI tmp = subtitleObject.GetComponent<TextMeshProUGUI>();
        float currentAlpha = tmp.color.a;
        
        // 淡出动画
        yield return StartCoroutine(FadeText(currentAlpha, 0f, fadeOutDuration));
        
        // 淡出完成后销毁
        DestroySubtitleImmediate();
    }
    
    IEnumerator FadeText(float fromAlpha, float toAlpha, float duration)
    {
        TextMeshProUGUI tmp = subtitleObject.GetComponent<TextMeshProUGUI>();
        float elapsed = 0f;
        Color originalColor = tmp.color;
        
        while (elapsed < duration && tmp != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
            
            tmp.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        // 确保最终状态正确
        if (tmp != null)
        {
            tmp.color = new Color(originalColor.r, originalColor.g, originalColor.b, toAlpha);
        }
    }
    
    void DestroySubtitleImmediate()
    {
        if (subtitleObject != null)
        {
            Destroy(subtitleObject);
            subtitleObject = null;
            hasBeenDestroyed = true;
            Debug.Log("字幕已销毁");
        }
    }
    
    Canvas CreateCanvas()
    {
        GameObject canvasObj = new GameObject("SubtitleCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        return canvas;
    }
    
    // 在Inspector中提供快速颜色预设
    [ContextMenu("设置为白色")]
    void SetColorWhite() { textColor = Color.white; }
    
    [ContextMenu("设置为黄色")]
    void SetColorYellow() { textColor = Color.yellow; }
    
    [ContextMenu("设置为绿色")]
    void SetColorGreen() { textColor = Color.green; }
    
    [ContextMenu("设置为红色")]
    void SetColorRed() { textColor = Color.red; }
    
    [ContextMenu("设置为蓝色")]
    void SetColorBlue() { textColor = Color.blue; }
    
    [ContextMenu("设置为黑色")]
    void SetColorBlack() { textColor = Color.black; }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, destroyDistance);
    }
}