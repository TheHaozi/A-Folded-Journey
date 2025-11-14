using UnityEngine;
using TMPro;
using System.Collections;

public class WorldSubtitle : MonoBehaviour
{
    [Header("字幕设置")]
    public float triggerDistance = 3f;
    public float fadeDuration = 1f;
    
    private Transform player;
    private TextMeshPro tmpText;
    private Color originalColor;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        tmpText = GetComponent<TextMeshPro>();
        
        if (tmpText != null)
        {
            originalColor = tmpText.color;
            tmpText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }
    }
    
    void Update()
    {
        if (player == null || tmpText == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        float currentAlpha = tmpText.color.a;
        
        if (distance <= triggerDistance && currentAlpha < 1f)
        {
            // 淡入
            float targetAlpha = 1f - (distance / triggerDistance); // 越近越清晰
            StartCoroutine(FadeTo(targetAlpha));
        }
        else if (distance > triggerDistance && currentAlpha > 0f)
        {
            // 淡出
            StartCoroutine(FadeTo(0f));
        }
    }
    
    IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = tmpText.color.a;
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            
            tmpText.color = new Color(originalColor.r, originalColor.g, originalColor.b, currentAlpha);
            yield return null;
        }
    }
}
