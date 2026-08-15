using UnityEngine;
using TMPro;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    [Header("設定")]
    public float floatSpeed = 80f;    // 上に流れる速さ
    public float duration = 1.2f;     // 表示時間
    public float fadeStart = 0.6f;    // この時間を過ぎたらフェードアウト開始

    private TextMeshProUGUI tmp;
    private RectTransform rect;
    private float elapsed = 0f;
    private Color originalColor;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();
    }

    public void Setup(string text, Color color, float fontSize = 36f)
    {
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize;
        originalColor = color;
        elapsed = 0f;
        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // 上に流れる
            rect.anchoredPosition += Vector2.up * floatSpeed * Time.deltaTime;

            // フェードアウト
            if (elapsed >= fadeStart)
            {
                float alpha = 1f - ((elapsed - fadeStart) / (duration - fadeStart));
                tmp.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}