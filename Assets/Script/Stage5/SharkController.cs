using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SharkController : MonoBehaviour
{
    [Header("移动设置")]
    public SharkMovementType movementType = SharkMovementType.AutoDetect;
    public float wanderSpeed = 1f;
    public float rotationSpeed = 2f;

    [Header("音频设置")]
    public AudioClip attackSound;

    [Header("死亡效果")]
    public float fadeOutDuration = 2f; // 渐出持续时间
    public float restartDelay = 3f; // 重新开始延迟

    [Header("运行时状态")]
    public bool isWandering = true;
    public float currentSpeed = 0f;
    public Vector2Int? HomeChunk { get; private set; } // 所在区块

    public enum SharkMovementType
    {
        AutoDetect,
        Horizontal,
        Vertical,
        Free
    }

    private Transform player;
    private Vector3 wanderDirection;
    private float wanderChangeTime;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Vector3 initialForward;
    private Collider2D sharkCollider;
    private Camera targetCamera;
    private bool isInitialized = false;
    private bool playerDead = false; // 防止重复触发

    // 死亡效果相关
    private Image fadeOverlay;
    private bool isFading = false;

    public void Initialize(Vector2Int homeChunk)
    {
        HomeChunk = homeChunk;
        StartCoroutine(DelayedInitialization());
    }

    IEnumerator DelayedInitialization()
    {
        // 获取组件引用
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        sharkCollider = GetComponent<Collider2D>();
        targetCamera = Camera.main;
        
        EnsureComponents();

        // 创建渐出遮罩
        CreateFadeOverlay();

        // 等待一帧确保场景加载完成
        yield return null;
        
        FindPlayer();

        if (player != null)
        {
            InitializeMovementType();
            InitializeWander();
            isInitialized = true;
            currentSpeed = wanderSpeed;
            Debug.Log($"✅ {gameObject.name} 在区块 {HomeChunk} 初始化完成");
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name} 无法找到玩家");
        }
    }

    void CreateFadeOverlay()
    {
        // 检查是否已经存在死亡画布
        GameObject existingCanvas = GameObject.Find("DeathCanvas");
        if (existingCanvas != null)
        {
            fadeOverlay = existingCanvas.GetComponentInChildren<Image>();
            return;
        }

        // 创建Canvas
        GameObject canvasObject = new GameObject("DeathCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 修复：使用 ScreenSpaceOverlay
        canvas.sortingOrder = 999; // 最高层级

        // 添加 CanvasScaler 和 GraphicRaycaster 以确保正确显示
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObject.AddComponent<GraphicRaycaster>();

        // 创建遮罩
        GameObject overlayObject = new GameObject("FadeOverlay");
        overlayObject.transform.SetParent(canvas.transform);
        fadeOverlay = overlayObject.AddComponent<Image>();
        fadeOverlay.color = new Color(0, 0, 0, 0); // 初始透明
        fadeOverlay.rectTransform.anchorMin = Vector2.zero;
        fadeOverlay.rectTransform.anchorMax = Vector2.one;
        fadeOverlay.rectTransform.offsetMin = Vector2.zero;
        fadeOverlay.rectTransform.offsetMax = Vector2.zero;

        // 初始隐藏
        canvasObject.SetActive(false);
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void EnsureComponents()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.spatialBlend = 1f;

        if (sharkCollider == null)
        {
            sharkCollider = GetComponentInChildren<Collider2D>();
        }
    }

    void InitializeMovementType()
    {
        if (movementType == SharkMovementType.AutoDetect)
        {
            movementType = DetectMovementTypeByContext();
        }

        initialForward = transform.up;
    }

    SharkMovementType DetectMovementTypeByContext()
    {
        string objectName = gameObject.name.ToLower();
        if (objectName.Contains("horizontal") || objectName.Contains("side"))
        {
            return SharkMovementType.Horizontal;
        }
        else if (objectName.Contains("vertical") || objectName.Contains("updown"))
        {
            return SharkMovementType.Vertical;
        }

        if (sharkCollider != null)
        {
            Bounds bounds = sharkCollider.bounds;
            float aspectRatio = bounds.size.x / bounds.size.y;
            
            if (aspectRatio > 1.5f) return SharkMovementType.Horizontal;
            if (aspectRatio < 0.67f) return SharkMovementType.Vertical;
        }

        return SharkMovementType.Free;
    }

    void InitializeWander()
    {
        switch (movementType)
        {
            case SharkMovementType.Horizontal:
                wanderDirection = Random.value > 0.5f ? Vector3.right : Vector3.left;
                break;
            case SharkMovementType.Vertical:
                wanderDirection = Random.value > 0.5f ? Vector3.up : Vector3.down;
                break;
            case SharkMovementType.Free:
                wanderDirection = Random.insideUnitCircle.normalized;
                break;
        }
        
        wanderChangeTime = Time.time + Random.Range(2f, 5f);
    }

    void Update()
    {
        if (!isInitialized) return;

        // 只进行闲逛，不追逐玩家
        Wander();
        UpdateVisuals();
    }

    void Wander()
    {
        if (Time.time >= wanderChangeTime)
        {
            switch (movementType)
            {
                case SharkMovementType.Horizontal:
                    wanderDirection = Random.value > 0.5f ? Vector3.right : Vector3.left;
                    break;
                case SharkMovementType.Vertical:
                    wanderDirection = Random.value > 0.5f ? Vector3.up : Vector3.down;
                    break;
                case SharkMovementType.Free:
                    wanderDirection = Random.insideUnitCircle.normalized;
                    break;
            }
            wanderChangeTime = Time.time + Random.Range(2f, 5f);
        }

        transform.position += wanderDirection * wanderSpeed * Time.deltaTime;

        // 更新朝向
        if (wanderDirection != Vector3.zero)
        {
            float wanderAngle = Mathf.Atan2(wanderDirection.y, wanderDirection.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, wanderAngle - 90f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 0.5f * Time.deltaTime);
        }
    }

    void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            // 鲨鱼保持正常颜色，不显示追逐状态
            spriteRenderer.color = Color.white;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !playerDead)
        {
            Debug.Log($"💀 玩家撞到鲨鱼 {gameObject.name}！");
            playerDead = true;
            
            // 播放攻击音效
            if (attackSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
            
            // 触发玩家死亡和屏幕渐出
            StartCoroutine(PlayerDeathSequence(collision.gameObject));
        }
    }

    IEnumerator PlayerDeathSequence(GameObject playerObject)
    {
        // 禁用玩家控制
        RipplePushEffect playerController = playerObject.GetComponent<RipplePushEffect>();
        if (playerController != null)
        {
            playerController.PlayerDieByShark();
        }

        // 开始屏幕渐出
        yield return StartCoroutine(FadeOutScreen());

        // 等待额外时间
        yield return new WaitForSeconds(restartDelay - fadeOutDuration);

        // 重新开始游戏
        RestartGame();
    }

    IEnumerator FadeOutScreen()
    {
        if (fadeOverlay == null) yield break;

        // 激活遮罩
        fadeOverlay.transform.parent.gameObject.SetActive(true);
        isFading = true;

        float timer = 0f;
        Color startColor = new Color(0, 0, 0, 0);
        Color endColor = new Color(0, 0, 0, 1);

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeOutDuration;
            fadeOverlay.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }

        fadeOverlay.color = endColor;
        isFading = false;
    }

    void RestartGame()
    {
        // 重新加载当前场景
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    [ContextMenu("重新初始化")]
    public void Reinitialize()
    {
        if (HomeChunk.HasValue)
        {
            Initialize(HomeChunk.Value);
        }
    }

    [ContextMenu("显示状态信息")]
    public void ShowStatusInfo()
    {
        Debug.Log($"🦈 {gameObject.name} 状态:");
        Debug.Log($"   移动类型: {movementType}");
        Debug.Log($"   所在区块: {HomeChunk}");
        Debug.Log($"   当前速度: {currentSpeed:F1}");
        Debug.Log($"   初始化状态: {(isInitialized ? "完成" : "未完成")}");
    }

    [ContextMenu("测试死亡效果")]
    public void TestDeathEffect()
    {
        if (!playerDead)
        {
            StartCoroutine(PlayerDeathSequence(GameObject.FindGameObjectWithTag("Player")));
        }
    }

   //=============================

    
}