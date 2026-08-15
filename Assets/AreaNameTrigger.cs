using UnityEngine;
using TMPro;
using System.Collections;

public class AreaNameTrigger : MonoBehaviour
{
    [Header("表示する地名")]
    public string areaName;

    [Header("UI")]
    public TextMeshProUGUI placeText;

    [Header("時間設定")]
    public float fadeInDuration = 0.5f;
    public float holdDuration = 2.0f;
    public float fadeOutDuration = 0.5f;

    private Coroutine fadeCoroutine;
    private Coroutine exitCoroutine;

    private void Start()
    {
        if (placeText != null)
        {
            placeText.text = "";
            SetAlpha(0);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        placeText.text = areaName;

        // 消える予約キャンセル（連続エリア対策）
        if (exitCoroutine != null)
        {
            StopCoroutine(exitCoroutine);
            exitCoroutine = null;
        }

        Fade(1, fadeInDuration);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        exitCoroutine = StartCoroutine(HoldThenFade());
    }

    IEnumerator HoldThenFade()
    {
        yield return new WaitForSeconds(holdDuration);
        Fade(0, fadeOutDuration);
    }

    void Fade(float targetAlpha, float duration)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = placeText.color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    void SetAlpha(float a)
    {
        if (placeText == null) return;

        var c = placeText.color;
        c.a = a;
        placeText.color = c;
    }
}