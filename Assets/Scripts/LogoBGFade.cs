using UnityEngine;
using System.Collections;
public class WhiteBackgroundFade : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    IEnumerator Start()
    {
        // 最初は真っ白
        canvasGroup.alpha = 1f;
        // ロゴのフェードイン1秒 + 表示維持2秒
        yield return new WaitForSeconds(3f);
        // ロゴのフェードアウトと同時に消える
        float duration = 1f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}