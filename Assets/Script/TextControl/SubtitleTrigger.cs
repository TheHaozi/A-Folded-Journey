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
            subtitleText.text = subtitleContent;
            subtitleText.gameObject.SetActive(true);
        }
     
    }

    void Update()
    {
        if (player == null || subtitleText == null || cameraTransform == null) return;

        // 计算距离
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float distanceToCamera = Vector3.Distance(transform.position, cameraTransform.position);

        bool wasInRange = isPlayerInRange;
        isPlayerInRange = distanceToPlayer <= triggerDistance;

        // 如果离相机太远，强制隐藏
        if (distanceToCamera > maxViewDistance)
        {
            if (subtitleText.color.a > 0.01f)
            {
                FadeToAlpha(0f);
            }
            return;
        }

   

        
        // 额外检查：如果在范围内但Alpha值很低，强制淡入
        if (isPlayerInRange && subtitleText.color.a < 0.1f && currentFadeCoroutine == null)
        {
            
            FadeToAlpha(1f);
        }
    }

    void FadeToAlpha(float targetAlpha)
    {
        // 如果目标Alpha和当前差不多，就不执行
        if (Mathf.Abs(subtitleText.color.a - targetAlpha) < 0.05f)
        {
            
            return;
        }

        // 停止之前的淡入淡出
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
            
        }

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

            // 确保文本在淡入过程中是激活的
            if (currentAlpha > 0.01f && !subtitleText.gameObject.activeInHierarchy)
                subtitleText.gameObject.SetActive(true);

            subtitleText.color = new Color(originalColor.r, originalColor.g, originalColor.b, currentAlpha);
            
            yield return null;
        }

        // 确保最终状态正确
        subtitleText.color = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha);
        
        // 如果完全透明，可以禁用对象（可选）
        if (targetAlpha < 0.01f)
            subtitleText.gameObject.SetActive(false);
        
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