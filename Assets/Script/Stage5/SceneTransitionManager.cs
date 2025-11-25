using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("场景设置")]
    public string nextSceneName = "NextLevel";
    public float transitionDistance = 800f;
    
    [Header("渐隐效果设置")]
    public float fadeDuration = 2f;
    public Color fadeColor = Color.black;
    public bool enableFade = true;
    
    [Header("音频设置")]
    public AudioSource backgroundAudio;
    public bool fadeAudio = true;
    
    private bool isTransitioning = false;
    private GameObject fadeObject;

    void Update()
    {
        if (isTransitioning) return;
        
        if (DistanceManager.Instance == null)
        {
            Debug.LogError("❌ DistanceManager未找到");
            return;
        }

        float distance = DistanceManager.Instance.CurrentDistance;
        
        if (distance >= transitionDistance)
        {
            StartTransition();
        }
    }

    void StartTransition()
    {
        isTransitioning = true;
        Debug.Log($"🎬 达到{transitionDistance}米，开始切换到场景: {nextSceneName}");
        
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("❌ 下一场景名称未设置");
            isTransitioning = false;
            return;
        }

        StartCoroutine(TransitionToNextScene());
    }

    IEnumerator TransitionToNextScene()
    {
        if (enableFade)
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
        }

        // 加载目标场景
        SceneManager.LoadScene(nextSceneName);
    }

    GameObject CreateFadeObject()
    {
        // 创建渐隐画布
        GameObject fadeObj = new GameObject("SceneFade");
        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        CanvasScaler scaler = fadeObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
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

    [ContextMenu("强制切换场景")]
    public void ForceTransition()
    {
        if (!isTransitioning)
        {
            StartTransition();
        }
    }

    [ContextMenu("测试渐隐效果")]
    public void TestFadeEffect()
    {
        if (!isTransitioning)
        {
            StartCoroutine(TestFadeCoroutine());
        }
    }

    IEnumerator TestFadeCoroutine()
    {
        Debug.Log("🎬 测试渐隐效果");
        
        fadeObject = CreateFadeObject();
        CanvasGroup canvasGroup = fadeObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        
        // 淡出
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = elapsed / fadeDuration;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(1f);
        
        // 淡入
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        Destroy(fadeObject);
        fadeObject = null;
        
        Debug.Log("✅ 渐隐效果测试完成");
    }

    void OnDrawGizmosSelected()
    {
        if (DistanceManager.Instance == null || DistanceManager.Instance.centerPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(DistanceManager.Instance.centerPoint.position, transitionDistance);
        
        // 绘制当前玩家位置和距离信息
        if (DistanceManager.Instance.playerController != null)
        {
            Vector3 playerPos = DistanceManager.Instance.PlayerPosition;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerPos, 5f);
            
            // 绘制距离文本（在Scene视图中显示）
            #if UNITY_EDITOR
            float currentDistance = DistanceManager.Instance.CurrentDistance;
            string distanceText = $"{currentDistance:F1}/{transitionDistance}米";
            UnityEditor.Handles.Label(playerPos + Vector3.up * 10f, distanceText);
            #endif
        }
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