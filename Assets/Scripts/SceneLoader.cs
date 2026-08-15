using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("Press Any Key")]
    public TextMeshProUGUI pressAnyKeyText;
    public float blinkInterval = 0.6f;

    [Header("BGM")]
    public AudioSource bgmSource;

    [Header("ローディングUI")]
    public GameObject loadingPanel;
    public Image fadePanel;         // 白いパネル
    public Image loadingBar;        // 黒いバー

    [Header("設定")]
    public float fadeInDuration = 0.8f;
    public string mainSceneName = "mainscene";

    private bool isLoading = false;

    void Start()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        StartCoroutine(BlinkText());
    }

    void Update()
    {
        if (!isLoading && Input.anyKeyDown)
            StartCoroutine(LoadSequence());
    }

    IEnumerator BlinkText()
    {
        while (true)
        {
            if (pressAnyKeyText != null) pressAnyKeyText.gameObject.SetActive(true);
            yield return new WaitForSeconds(blinkInterval);
            if (pressAnyKeyText != null) pressAnyKeyText.gameObject.SetActive(false);
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    IEnumerator LoadSequence()
    {
        isLoading = true;
        if (pressAnyKeyText != null) pressAnyKeyText.gameObject.SetActive(false);

        // ① パネル表示・初期化
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (loadingBar   != null) loadingBar.fillAmount = 0f;
        SetAlpha(fadePanel, 0f);

        // ② 非同期ロード開始（切り替えはまだしない）
        AsyncOperation op = SceneManager.LoadSceneAsync(mainSceneName);
        op.allowSceneActivation = false;

        // ③ BGMフェードアウト＋白パネルフェードイン（同時）
        float elapsed = 0f;
        float bgmStartVolume = bgmSource != null ? bgmSource.volume : 1f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);

            if (bgmSource != null) bgmSource.volume = Mathf.Lerp(bgmStartVolume, 0f, t);
            SetAlpha(fadePanel, t);

            // バーをロード進捗に合わせて伸ばす
            if (loadingBar != null)
                loadingBar.fillAmount = Mathf.Clamp01(op.progress / 0.9f);

            yield return null;
        }

        if (bgmSource != null) bgmSource.Stop();
        SetAlpha(fadePanel, 1f);

        // ④ ロード完了を待つ（完了していれば即通過）
        while (op.progress < 0.9f)
        {
            if (loadingBar != null)
                loadingBar.fillAmount = Mathf.Clamp01(op.progress / 0.9f);
            yield return null;
        }

        if (loadingBar != null) loadingBar.fillAmount = 1f;
        yield return new WaitForSeconds(0.3f);

        // ⑤ 白いまま切り替え（MainScene側でフェードアウト）
        op.allowSceneActivation = true;
    }

    void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        var c = img.color;
        img.color = new Color(c.r, c.g, c.b, alpha);
    }

    public void StartGame() { }
}