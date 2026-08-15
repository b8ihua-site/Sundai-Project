using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class LogoSequence : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    IEnumerator Start()
    {
        // 最初は透明
        canvasGroup.alpha = 0;
        // フェードイン
        yield return Fade(0, 1, 1f);
        // 表示維持
        yield return new WaitForSeconds(2f);
        // フェードアウト
        yield return Fade(1, 0, 1f);

    }
    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}