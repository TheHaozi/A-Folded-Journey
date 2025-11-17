using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Buttom : MonoBehaviour
{
    [Header("渐入渐出设置")]
    public float fadeDuration = 1.0f; // 渐变持续时间
    [Header("音频设置")]
    public AudioSource backgroundAudio; // 背景音乐AudioSource
    
    private Image fadeImage; // 动态创建的遮罩
    private bool isTransitioning = false; // 防止重复触发
    private GameObject fadeCanvas; // 遮罩画布
    
    void Start()
    {
        // 开始时的渐入效果
        StartCoroutine(FadeIn());
    }
    
    // 动态创建遮罩
    private void CreateFadeMask()
    {
        // 如果已经存在，先销毁
        if (fadeCanvas != null)
        {
            Destroy(fadeCanvas);
        }
        
        // 创建画布
        fadeCanvas = new GameObject("FadeCanvas");
        Canvas canvas = fadeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // 确保在最顶层
        
        // 添加Canvas Scaler
        CanvasScaler scaler = fadeCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // 创建Image组件
        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(fadeCanvas.transform);
        
        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = Color.black;
        
        // 设置全屏尺寸
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // 初始隐藏
        fadeImage.gameObject.SetActive(false);
    }
    
    public void LoadNextScene()
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToNextScene());
        }
    }
    
    public void EndGame()
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToQuit());
        }
    }
    
    // 切换到下一个场景的协程
    private IEnumerator TransitionToNextScene()
    {
        isTransitioning = true;
        
        // 确保遮罩存在
        if (fadeImage == null)
        {
            CreateFadeMask();
        }
        
        // 同步渐出画面和音乐
        yield return StartCoroutine(FadeOutWithAudio());
        
        // 获取当前场景的索引
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        
        // 计算下一个场景的索引
        int nextSceneIndex = currentSceneIndex + 1;
        
        // 检查下一个场景是否存在
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            // 加载下一个场景
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            // 如果已经是最后一个场景，回到第一个场景
            Debug.Log("已经是最后一个场景，回到第一个场景");
            SceneManager.LoadScene(0);
        }
    }
    
    // 退出游戏的协程
    private IEnumerator TransitionToQuit()
    {
        isTransitioning = true;
        
        // 确保遮罩存在
        if (fadeImage == null)
        {
            CreateFadeMask();
        }
        
        // 同步渐出画面和音乐
        yield return StartCoroutine(FadeOutWithAudio());
        
        Debug.Log("退出游戏");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    // 同步渐出画面和音乐
    private IEnumerator FadeOutWithAudio()
    {
        fadeImage.gameObject.SetActive(true);
        float timer = 0f;
        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;
        
        float startVolume = backgroundAudio != null ? backgroundAudio.volume : 0f;
        bool hasAudio = backgroundAudio != null && backgroundAudio.isPlaying;
        
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fadeDuration);
            
            // 同步更新画面透明度
            color.a = progress;
            fadeImage.color = color;
            
            // 同步更新音乐音量
            if (hasAudio)
            {
                backgroundAudio.volume = Mathf.Lerp(startVolume, 0f, progress);
            }
            
            yield return null;
        }
        
        // 最终状态
        color.a = 1f;
        fadeImage.color = color;
        
        if (hasAudio)
        {
            backgroundAudio.volume = 0f;
            backgroundAudio.Stop();
        }
    }
    
    // 渐入效果：从黑屏到透明（同步音乐）
    private IEnumerator FadeIn()
    {
        // 确保遮罩存在
        if (fadeImage == null)
        {
            CreateFadeMask();
        }
        
        fadeImage.gameObject.SetActive(true);
        Color color = fadeImage.color;
        color.a = 1f;
        fadeImage.color = color;
        
        // 准备音乐
        bool hasAudio = backgroundAudio != null;
        if (hasAudio)
        {
            backgroundAudio.volume = 0f;
            if (!backgroundAudio.isPlaying)
            {
                backgroundAudio.Play();
            }
        }
        
        float timer = 0f;
        float targetVolume = hasAudio ? 1.0f : 0f;
        
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fadeDuration);
            float alpha = 1f - progress;
            
            // 同步更新画面透明度
            color.a = alpha;
            fadeImage.color = color;
            
            // 同步更新音乐音量
            if (hasAudio)
            {
                backgroundAudio.volume = Mathf.Lerp(0f, targetVolume, progress);
            }
            
            yield return null;
        }
        
        // 最终状态
        color.a = 0f;
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false);
        
        if (hasAudio)
        {
            backgroundAudio.volume = targetVolume;
        }
    }
    
    // 清理资源
    private void OnDestroy()
    {
        if (fadeCanvas != null)
        {
            Destroy(fadeCanvas);
        }
    }
    
    // 如果你想要通过键盘按键来触发（可选）
    /*void Update()
    {
        // 例如：按下空格键切换到下一个场景
        if (Input.GetKeyDown(KeyCode.Space) && !isTransitioning)
        {
            LoadNextScene();
        }
        
        // 例如：按下Escape键退出游戏
        if (Input.GetKeyDown(KeyCode.Escape) && !isTransitioning)
        {
            EndGame();
        }
    }*/
}