using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    [Header("场景设置")]
    public string targetSceneName;           // 目标场景名称
    public float triggerDistance = 1f;       // 触发距离
    
    [Header("渐隐效果设置")]
    public float fadeDuration = 1f;          // 渐隐持续时间
    public Color fadeColor = Color.black;    // 渐隐颜色
    
    [Header("音频设置")]
    public AudioSource backgroundAudio;      // 背景音乐AudioSource
    public bool fadeAudio = true;            // 是否启用音频渐隐
    
    private Transform player;
    private bool hasTriggered = false;
    private GameObject fadeObject;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    
    void Update()
    {
        if (hasTriggered || player == null) return;
        
        float distance = Vector2.Distance(transform.position, player.position);
        
        if (distance <= triggerDistance)
        {
            TriggerSceneTransition();
        }
    }
    
    void TriggerSceneTransition()
    {
        hasTriggered = true;
        Debug.Log($"触发场景切换: {targetSceneName}");
        
        // 执行场景切换
        StartCoroutine(TransitionToScene());
    }
    
    IEnumerator TransitionToScene()
    {
        // 创建渐隐效果
        fadeObject = CreateFadeObject();
        CanvasGroup canvasGroup = fadeObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        
        // 获取音频初始音量
        float initialAudioVolume = 0f;
        bool hasAudio = fadeAudio && backgroundAudio != null && backgroundAudio.isPlaying;
        
        if (hasAudio)
        {
            initialAudioVolume = backgroundAudio.volume;
        }
        
        // 同步渐隐画面和音频
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;
            
            // 更新画面透明度
            canvasGroup.alpha = progress;
            
            // 同步更新音频音量
            if (hasAudio)
            {
                backgroundAudio.volume = Mathf.Lerp(initialAudioVolume, 0f, progress);
            }
            
            yield return null;
        }
        
        // 确保完全黑屏和静音
        canvasGroup.alpha = 1f;
        
        if (hasAudio)
        {
            backgroundAudio.volume = 0f;
            backgroundAudio.Stop();
        }
        
        // 等待一帧确保效果完成
        yield return null;
        
        // 加载目标场景
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("目标场景名称未设置！");
        }
    }
    
    GameObject CreateFadeObject()
    {
        // 创建渐隐画布
        GameObject fadeObj = new GameObject("SceneFade");
        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // 确保在最顶层
        
        // 添加Canvas Scaler
        CanvasScaler scaler = fadeObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // 添加Graphic Raycaster（可选）
        fadeObj.AddComponent<GraphicRaycaster>();
        
        // 创建全屏Image
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(fadeObj.transform);
        
        Image image = imageObj.AddComponent<Image>();
        image.color = fadeColor;
        image.raycastTarget = false;
        
        // 设置全屏尺寸
        RectTransform rect = imageObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // 添加CanvasGroup控制透明度
        CanvasGroup canvasGroup = fadeObj.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        
        return fadeObj;
    }
    
    // 可选：在Inspector中添加快速测试方法
    [ContextMenu("测试渐隐效果")]
    void TestFadeEffect()
    {
        if (!hasTriggered)
        {
            TriggerSceneTransition();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
    
    void OnDestroy()
    {
        // 清理资源
        if (fadeObject != null)
        {
            Destroy(fadeObject);
        }
    }
}