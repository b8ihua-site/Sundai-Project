using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneFadeIn : MonoBehaviour
{
    [Header("白パネル")]
    public Image fadePanel;
    public float fadeOutDuration = 0.8f;

    void Start()
    {
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        SetAlpha(fadePanel, 1f);  // 最初は真っ白
        yield return null;        // 1フレーム待って描画させる

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            SetAlpha(fadePanel, 1f - t);
            yield return null;
        }

        SetAlpha(fadePanel, 0f);
        fadePanel.gameObject.SetActive(false);
    }

    void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        var c = img.color;
        img.color = new Color(c.r, c.g, c.b, alpha);
    }
}