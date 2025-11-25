using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SubtitleManager : MonoBehaviour
{
    [Header("字幕设置")]
    public TextMeshProUGUI subtitleText;
    public float subtitleDuration = 3f;
    public float fadeDuration = 1f;

    [System.Serializable]
    public class DistanceMessage
    {
        public float distance;
        public string message;
        public Color color = Color.white;
    }

    [Header("提示信息")]
    public List<DistanceMessage> messages = new List<DistanceMessage>()
    {
        new DistanceMessage { distance = 50f, message = "Warning: Shark infested waters ahead!", color = Color.red },
        new DistanceMessage { distance = 200f, message = "Keep going! You're doing great!", color = Color.yellow },
        new DistanceMessage { distance = 400f, message = "Halfway there! Stay strong!", color = Color.green },
        new DistanceMessage { distance = 700f, message = "Almost there! Final push!", color = Color.cyan }
    };

    private HashSet<float> triggeredMessages = new HashSet<float>();
    private Coroutine currentSubtitleCoroutine;

    void Start()
    {
        if (subtitleText == null)
            subtitleText = FindObjectOfType<TextMeshProUGUI>();
    }

    void Update()
    {
        if (DistanceManager.Instance == null) return;

        float distance = DistanceManager.Instance.CurrentDistance;
        CheckMessages(distance);
    }

    void CheckMessages(float currentDistance)
    {
        foreach (var message in messages)
        {
            if (!triggeredMessages.Contains(message.distance) && 
                currentDistance >= message.distance && 
                currentDistance < message.distance + 10f)
            {
                ShowSubtitle(message.message, message.color);
                triggeredMessages.Add(message.distance);
                break;
            }
        }
    }

    void ShowSubtitle(string message, Color color)
    {
        if (subtitleText == null)
        {
            Debug.Log($"💬 {message}");
            return;
        }

        if (currentSubtitleCoroutine != null)
            StopCoroutine(currentSubtitleCoroutine);

        currentSubtitleCoroutine = StartCoroutine(ShowSubtitleCoroutine(message, color));
    }

    System.Collections.IEnumerator ShowSubtitleCoroutine(string message, Color color)
    {
        subtitleText.text = message;
        subtitleText.color = color;

        // 淡入
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = timer / fadeDuration;
            subtitleText.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        subtitleText.color = color;
        yield return new WaitForSeconds(subtitleDuration);

        // 淡出
        timer = 0f;
        Color startColor = subtitleText.color;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1f - (timer / fadeDuration);
            subtitleText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        subtitleText.text = "";
        currentSubtitleCoroutine = null;
    }

    // 重置所有已触发的消息
    public void ResetMessages()
    {
        triggeredMessages.Clear();
    }

    void OnDrawGizmosSelected()
    {
        if (DistanceManager.Instance == null || DistanceManager.Instance.centerPoint == null) return;

        foreach (var message in messages)
        {
            Gizmos.color = message.color;
            Gizmos.DrawWireSphere(DistanceManager.Instance.centerPoint.position, message.distance);
        }
    }
}
