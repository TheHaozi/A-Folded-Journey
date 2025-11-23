using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class SubtitleTrigger : MonoBehaviour
{
    [Header("字幕设置")]
    public TextMeshProUGUI subtitleText;
    public string subtitleContent = "这里是字幕内容";
    public float fadeDuration = 1f;
    public float triggerDistance = 3f;
    public float maxViewDistance = 10f;
    public Color textColor = Color.white;

    [Header("玩家绑定")]
    public Transform player; // 直接在Inspector中绑定玩家

    private Transform cameraTransform;
    private Color originalColor;
    private Coroutine currentFadeCoroutine;

    private enum SubtitleState { Hidden, FadingIn, Visible, FadingOut }
    private SubtitleState currentState = SubtitleState.Hidden;

    void Start()
    {
        InitializeSubtitleSystem();
    }

    void InitializeSubtitleSystem()
    {
        // 查找相机
        FindCamera();
        
        // 检查玩家引用
        if (player == null)
        {
            // 尝试自动查找作为备用
            TryFindPlayerBackup();
        }
        // 确保字幕文本组件存在
        if (subtitleText == null)
        {
            CreateSubtitleUI();
        }
        else
        {
            InitializeSubtitleText();
        }
    }

    void FindCamera()
    {
        // 查找相机
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Camera cam = FindObjectOfType<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
        }
    }

    void TryFindPlayerBackup()
    {
        // 备用方案：尝试自动查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    void InitializeSubtitleText()
    {
        if (subtitleText != null)
        {
            subtitleText.text = subtitleContent;
            originalColor = textColor;
            subtitleText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            subtitleText.gameObject.SetActive(true);
            Debug.Log("字幕文本初始化完成");
        }
    }

    void Update()
    {
        // 检查必要的组件
        if (!AreComponentsValid())
        {
            return;
        }

        // 计算距离并更新字幕状态
        UpdateSubtitleState();
    }

    bool AreComponentsValid()
    {
        if (player == null)
        {
            return false;
        }
        if (subtitleText == null)
        {
            return false;
        }
        if (cameraTransform == null)
        {
            return false;
        }
        return true;
    }

    void UpdateSubtitleState()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float distanceToCamera = Vector3.Distance(transform.position, cameraTransform.position);

        // 检查是否在视野内
        bool isInView = IsInCameraView();
        bool shouldBeVisible = distanceToPlayer <= triggerDistance && 
                              distanceToCamera <= maxViewDistance &&
                              isInView;

        // 状态转换逻辑
        switch (currentState)
        {
            case SubtitleState.Hidden:
                if (shouldBeVisible)
                {
                    FadeToAlpha(1f);
                    currentState = SubtitleState.FadingIn;
                }
                break;

            case SubtitleState.FadingIn:
                if (!shouldBeVisible)
                {
                    FadeToAlpha(0f);
                    currentState = SubtitleState.FadingOut;
                }
                break;

            case SubtitleState.Visible:
                if (!shouldBeVisible)
                {
                    FadeToAlpha(0f);
                    currentState = SubtitleState.FadingOut;
                }
                break;

            case SubtitleState.FadingOut:
                if (shouldBeVisible)
                {
                    FadeToAlpha(1f);
                    currentState = SubtitleState.FadingIn;
                }
                break;
        }
    }

    bool IsInCameraView()
    {
        if (cameraTransform == null) return false;
        
        Vector3 directionToCamera = (cameraTransform.position - transform.position).normalized;
        float dotProduct = Vector3.Dot(cameraTransform.forward, -directionToCamera);
        return dotProduct > 0.3f;
    }

    void CreateSubtitleUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }

        GameObject subtitleObj = new GameObject("SubtitleText");
        subtitleObj.transform.SetParent(canvas.transform);
        subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        
        InitializeSubtitleText();
        ConfigureTextStyle(subtitleObj);
    }

    Canvas CreateCanvas()
    {
        GameObject canvasObj = new GameObject("SubtitleCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        return canvas;
    }

    void ConfigureTextStyle(GameObject textObject)
    {
        if (subtitleText == null) return;

        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontSize = 28;
        subtitleText.enableWordWrapping = true;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, -150);
        rect.sizeDelta = new Vector2(800, 60);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    void FadeToAlpha(float targetAlpha)
    {
        if (subtitleText == null) return;

        if (Mathf.Abs(subtitleText.color.a - targetAlpha) < 0.05f)
        {
            return;
        }

        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    IEnumerator FadeRoutine(float targetAlpha)
    {
        if (subtitleText == null) yield break;

        float startAlpha = subtitleText.color.a;
        float elapsed = 0f;

        if (!subtitleText.gameObject.activeInHierarchy)
            subtitleText.gameObject.SetActive(true);

        while (elapsed < fadeDuration && subtitleText != null)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            subtitleText.color = new Color(originalColor.r, originalColor.g, originalColor.b, currentAlpha);
            yield return null;
        }

        if (subtitleText != null)
        {
            subtitleText.color = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha);
            
            if (targetAlpha < 0.1f)
            {
                currentState = SubtitleState.Hidden;
            }
            else
            {
                currentState = SubtitleState.Visible;
            }
        }
        
        currentFadeCoroutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);

        Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxViewDistance);
    }
}