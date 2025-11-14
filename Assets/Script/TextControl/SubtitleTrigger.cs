using UnityEngine;
using TMPro;
using System.Collections;

public class SubtitleTrigger : MonoBehaviour
{
    [Header("字幕设置")]
    public TextMeshProUGUI subtitleText;
    public string subtitleContent = "这里是字幕内容";
    public float fadeDuration = 1f;
    public float triggerDistance = 3f;
    public float maxViewDistance = 10f;

    private Transform player;
    private Transform cameraTransform;
    private Color originalColor;
    private bool isPlayerInRange = false;
    private Coroutine currentFadeCoroutine;

    void Start()
    {
        // 自动查找玩家和相机
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        cameraTransform = Camera.main?.transform;
        
        if (subtitleText != null)
        {
            originalColor = subtitleText.color;
            // 初始状态：完全透明
            subtitleText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            subtitleText.text = subtitleContent; // 预先设置文字
        }
        else
        {
            Debug.LogError("请将TextMeshPro文本拖拽到Subtitle Text字段！");
        }

        if (player == null)
            Debug.LogError("找不到带有Player标签的玩家对象！");
        
        
    }

    void Update()
    {
        if (player == null || subtitleText == null || cameraTransform == null) return;

        // 计算距离
        float distanceToPlayer = Vector2.Distance(new Vector2(transform.position.x, transform.position.y), 
                                                 new Vector2(player.position.x, player.position.y));
        float distanceToCamera = Vector3.Distance(transform.position, cameraTransform.position);

        bool wasInRange = isPlayerInRange;
        isPlayerInRange = distanceToPlayer <= triggerDistance;

        // 如果离相机太远，强制隐藏
        if (distanceToCamera > maxViewDistance)
        {
            if (subtitleText.color.a > 0.01f)
            {
                FadeSubtitle(0f);
            }
            return;
        }

        // 状态变化处理
        if (isPlayerInRange != wasInRange)
        {
            if (isPlayerInRange)
            {
                // 玩家进入范围：淡入
                FadeSubtitle(1f);
            }
            else
            {
                // 玩家离开范围：淡出
                FadeSubtitle(0f);
            }
        }
    }

    void FadeSubtitle(float targetAlpha)
    {
        // 停止之前的淡入淡出
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        currentFadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = subtitleText.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            subtitleText.color = new Color(originalColor.r, originalColor.g, originalColor.b, currentAlpha);
            yield return null;
        }

        // 确保最终状态正确
        subtitleText.color = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha);
        currentFadeCoroutine = null;
    }

    void OnDrawGizmosSelected()
    {
        // 绘制触发范围（黄色）
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);

        // 绘制最大可视范围（蓝色）
        Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxViewDistance);
    }
}