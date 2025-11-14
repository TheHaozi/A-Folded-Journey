using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Buttom : MonoBehaviour
{
    [Header("渐入渐出设置")]
    public float fadeDuration = 1.0f; // 渐变持续时间
    
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
        
        // 渐出到黑屏
        yield return StartCoroutine(FadeOut());
        
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
        
        // 渐出到黑屏
        yield return StartCoroutine(FadeOut());
        
        Debug.Log("退出游戏");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    // 渐出效果：从透明到黑屏
    private IEnumerator FadeOut()
    {
        fadeImage.gameObject.SetActive(true);
        float timer = 0f;
        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;
        
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / fadeDuration);
            color.a = alpha;
            fadeImage.color = color;
            yield return null;
        }
        
        color.a = 1f;
        fadeImage.color = color;
    }
    
    // 渐入效果：从黑屏到透明
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
        
        float timer = 0f;
        
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
            color.a = alpha;
            fadeImage.color = color;
            yield return null;
        }
        
        color.a = 0f;
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false);
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