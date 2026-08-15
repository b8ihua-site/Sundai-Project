using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class LetterGrid : MonoBehaviour
{
    [Header("設定")]
    public float timePerChar = 5f;
    public float animDuration = 0.3f;    // フェード/スライドの時間
    public float slideDistance = 200f;   // 左にズレる距離

    [Header("参照")]
    public GameObject cellPrefab;
    public Transform gridParent;
    public Slider timerSlider;
    public TextMeshProUGUI comboText;

    [Header("コンボポップアップ位置")]
    public Vector2 comboPopupPos = new Vector2(660f, 350f);

    private string currentAnswer;
    private int currentCharIndex = 0;
    private int combo = 0;
    private Coroutine timerCoroutine;
    private bool isAnimating = false;   // アニメ中は入力を受け付けない
    private bool isActive = false;      // グリッドがアクティブか

    // 現在表示中のセル9個
    private List<GameObject> currentCells = new List<GameObject>();

    // テンキー配置（左下1、右上9）
    // テンキー位置 → グリッドインデックス（0=左上）
// テンキー 1〜9 を順番に並べる
private readonly KeyCode[] numpadKeys = {
    KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3,  // 1,2,3
    KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6,  // 4,5,6
    KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9   // 7,8,9
};

// 1〜9それぞれが何番インデックスか（左下1=index6、右上9=index2）
private readonly int[] numpadToIndex = {
    6, 7, 8,   // 1,2,3 → 下段（index 6,7,8）
    3, 4, 5,   // 4,5,6 → 中段（index 3,4,5）
    0, 1, 2    // 7,8,9 → 上段（index 0,1,2）
};



    private readonly string hiraganaPool =
        "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん";

    public void SetupGrid(string answer)
    {
        currentAnswer = answer;
        currentCharIndex = 0;
        combo = 0;
        isActive = true;
        UpdateComboText();
        ShowNextGrid(false); // 最初は即表示（アニメなし）
        StartTimer();
    }

    public void ClearGrid()
    {
        isActive = false;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        foreach (var cell in currentCells)
            if (cell != null) Destroy(cell);
        currentCells.Clear();
        combo = 0;
        UpdateComboText();
        if (timerSlider != null) timerSlider.value = 0f;
    }

    void Update()
    {
        if (!isActive || isAnimating) return;

        // テンキー入力
        for (int i = 0; i < numpadKeys.Length; i++)
        {
            if (Input.GetKeyDown(numpadKeys[i]))
            {
                int gridIndex = numpadToIndex[i];
                OnCellClicked(gridIndex);
                break;
            }
        }
    }

    // 次の文字のグリッドを生成（アニメあり/なし）
    void ShowNextGrid(bool animate)
    {
        if (currentCharIndex >= currentAnswer.Length) return;

        char target = currentAnswer[currentCharIndex];
        List<char> letters = BuildLetterList(target);

        // 新しいセルを生成（最初は透明・少し下にオフセット）
        var newCells = new List<GameObject>();
        for (int i = 0; i < 9; i++)
        {
            var cell = Instantiate(cellPrefab, gridParent);
            cell.GetComponentInChildren<TextMeshProUGUI>().text = letters[i].ToString();

            var img = cell.GetComponent<Image>();
            SetAlpha(img, animate ? 0f : 1f);

            int idx = i;
            cell.GetComponent<Button>().onClick.AddListener(() => OnCellClicked(idx));

            newCells.Add(cell);
        }

        if (animate)
        {
            // 古いグリッドと新しいグリッドを同時にアニメ
            StartCoroutine(CrossFadeGrids(currentCells, newCells));
        }
        else
        {
            currentCells = newCells;
        }
    }

    IEnumerator CrossFadeGrids(List<GameObject> oldCells, List<GameObject> newCells)
    {
        isAnimating = true;

        // 新グリッドを少し下にオフセット
        var newParentRect = gridParent.GetComponent<RectTransform>();
        Vector2 originPos = newParentRect.anchoredPosition;
        float offsetY = -80f;

        // 新セルの初期位置を下にずらす
        foreach (var cell in newCells)
        {
            var r = cell.GetComponent<RectTransform>();
            r.anchoredPosition += new Vector2(0, offsetY);
        }

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float ease = Mathf.SmoothStep(0f, 1f, t);

            // 古いグリッド：左にスライド＋フェードアウト
            foreach (var cell in oldCells)
            {
                if (cell == null) continue;
                var r = cell.GetComponent<RectTransform>();
                r.anchoredPosition += new Vector2(-slideDistance * Time.deltaTime / animDuration, 0);
                SetAlpha(cell.GetComponent<Image>(), 1f - ease);

                // テキストもフェード
                var tmp = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    var c = tmp.color;
                    tmp.color = new Color(c.r, c.g, c.b, 1f - ease);
                }
            }

            // 新しいグリッド：下から上にスライド＋フェードイン
            foreach (var cell in newCells)
            {
                if (cell == null) continue;
                var r = cell.GetComponent<RectTransform>();
                // Y位置をoffsetY → 0 に補間
                float currentOffsetY = Mathf.Lerp(offsetY, 0f, ease);
                // X位置はそのまま（GridLayoutGroupが管理）
                var pos = r.anchoredPosition;
                r.anchoredPosition = new Vector2(pos.x, currentOffsetY);

                SetAlpha(cell.GetComponent<Image>(), ease);

                var tmp = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    var c = tmp.color;
                    tmp.color = new Color(c.r, c.g, c.b, ease);
                }
            }

            yield return null;
        }

        // 古いセルを破棄
        foreach (var cell in oldCells)
            if (cell != null) Destroy(cell);

        // 新セルのY位置をリセット
        foreach (var cell in newCells)
        {
            if (cell == null) continue;
            var r = cell.GetComponent<RectTransform>();
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, 0f);
            SetAlpha(cell.GetComponent<Image>(), 1f);
            var tmp = cell.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                var c = tmp.color;
                tmp.color = new Color(c.r, c.g, c.b, 1f);
            }
        }

        currentCells = newCells;
        isAnimating = false;
    }

    // 不正解・時間切れ：グリッド全体を下にフェードアウトして終了
    IEnumerator FadeOutAndFinish()
    {
        isAnimating = true;
        isActive = false;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float ease = Mathf.SmoothStep(0f, 1f, t);

            foreach (var cell in currentCells)
            {
                if (cell == null) continue;
                var r = cell.GetComponent<RectTransform>();
                r.anchoredPosition += new Vector2(0, -80f * Time.deltaTime / animDuration);
                SetAlpha(cell.GetComponent<Image>(), 1f - ease);
                var tmp = cell.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    var c = tmp.color;
                    tmp.color = new Color(c.r, c.g, c.b, 1f - ease);
                }
            }

            yield return null;
        }

        foreach (var cell in currentCells)
            if (cell != null) Destroy(cell);
        currentCells.Clear();

        isAnimating = false;
        // LetterGridはDodgeArenaに置き換えられ、BattleManagerからは使われていない
    }

    void OnCellClicked(int index)
    {
        if (!isActive || isAnimating) return;
        if (index < 0 || index >= currentCells.Count) return;

        char selected = currentCells[index]
            .GetComponentInChildren<TextMeshProUGUI>().text[0];
        char expected = currentAnswer[currentCharIndex];

        if (selected == expected)
        {
            SEManager.Instance.PlayScaleNote(combo);
            combo++;
            currentCharIndex++;
            UpdateComboText();

            if (combo > 0)
                PopupSpawner.Instance.SpawnCombo(combo, comboPopupPos);

            if (currentCharIndex >= currentAnswer.Length)
            {
                // 全文字正解 → フェードアウトして終了
                if (timerCoroutine != null) StopCoroutine(timerCoroutine);
                StartCoroutine(FadeOutAndFinish());
            }
            else
            {
                // 次の文字へ（クロスフェード）
                if (timerCoroutine != null) StopCoroutine(timerCoroutine);
                ShowNextGrid(true);
                StartTimer();
            }
        }
        else
        {
            // 不正解
            SEManager.Instance.Play("miss");
            StartCoroutine(FadeOutAndFinish());
        }
    }

    void StartTimer()
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    IEnumerator TimerCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < timePerChar)
        {
            elapsed += Time.deltaTime;
            if (timerSlider != null)
                timerSlider.value = 1f - (elapsed / timePerChar);
            yield return null;
        }

        // 時間切れ
        StartCoroutine(FadeOutAndFinish());
    }

    List<char> BuildLetterList(char target)
    {
        List<char> letters = new List<char> { target };
        while (letters.Count < 9)
        {
            char r = hiraganaPool[Random.Range(0, hiraganaPool.Length)];
            if (!letters.Contains(r))
                letters.Add(r);
        }

        // シャッフル
        for (int i = letters.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (letters[i], letters[j]) = (letters[j], letters[i]);
        }
        return letters;
    }

    void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        var c = img.color;
        img.color = new Color(c.r, c.g, c.b, alpha);
    }

    void UpdateComboText()
    {
        if (comboText != null)
            comboText.text = $"COMBO: {combo}";
    }
}