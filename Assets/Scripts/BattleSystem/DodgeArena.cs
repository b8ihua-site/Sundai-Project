using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// アンダーテール風の回避ミニゲーム。白枠の中を自機が動き、
// 答えのひらがなが正しい順番で飛んでくるのに当たるとコンボ（＝攻撃）、
// 順番違い／おとりに当たると被弾する
public class DodgeArena : MonoBehaviour
{
    [Header("枠・自機")]
    public RectTransform arenaRect;   // 白枠。このサイズの中だけ自機が動ける
    public RectTransform player;
    public float playerSpeed = 260f;
    public float playerRadius = 10f;
    public float bulletRadius = 16f;

    [Header("弾")]
    public float spawnInterval = 0.45f;
    public float bulletSpeed = 160f;
    [Range(0f, 1f)] public float decoyChance = 0.5f;
    public float spawnMargin = 60f;   // 枠の外、どれだけ離れた所から出現するか
    public float despawnMargin = 100f; // 枠の外、どれだけ離れたら消滅扱いにするか
    public float fontSize = 30f;
    public TMP_FontAsset japaneseFont;

    [Header("おとり文字プール")]
    public string hiraganaPool = "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん";

    // ターン終了時に (コンボ数, 文字数) を渡して呼ばれる
    public Action<int, int> OnTurnEnd;

    private string answer;
    private int expectedIndex;
    private int combo;
    private Queue<int> spawnQueue;
    private readonly List<HiraganaBullet> activeBullets = new List<HiraganaBullet>();
    private bool spawning;
    private Coroutine spawnCoroutine;

    public void StartRound(string answerText)
    {
        answer = answerText ?? "";
        expectedIndex = 0;
        combo = 0;

        ClearBullets();

        spawnQueue = new Queue<int>();
        for (int i = 0; i < answer.Length; i++)
            spawnQueue.Enqueue(i);

        if (player != null)
            player.anchoredPosition = Vector2.zero;

        gameObject.SetActive(true);

        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void EndRoundImmediately()
    {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawning = false;
        ClearBullets();
        gameObject.SetActive(false);
    }

    void ClearBullets()
    {
        foreach (var b in activeBullets)
            if (b != null) Destroy(b.gameObject);
        activeBullets.Clear();
    }

    IEnumerator SpawnLoop()
    {
        spawning = true;

        while (spawnQueue.Count > 0)
        {
            yield return new WaitForSeconds(spawnInterval);

            bool spawnDecoy = spawnQueue.Count > 0 && UnityEngine.Random.value < decoyChance;

            if (spawnDecoy)
            {
                SpawnBullet(-1, RandomDecoyChar());
            }
            else
            {
                int idx = spawnQueue.Dequeue();
                SpawnBullet(idx, answer[idx]);
            }
        }

        spawning = false;
    }

    char RandomDecoyChar()
    {
        // 答えの文字と被らないおとり文字を選ぶ
        for (int tries = 0; tries < 10; tries++)
        {
            char c = hiraganaPool[UnityEngine.Random.Range(0, hiraganaPool.Length)];
            if (answer.IndexOf(c) < 0) return c;
        }
        return hiraganaPool[UnityEngine.Random.Range(0, hiraganaPool.Length)];
    }

    void SpawnBullet(int index, char letter)
    {
        if (arenaRect == null) return;

        var go = new GameObject("Bullet");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(arenaRect, false);
        rt.sizeDelta = new Vector2(50f, 50f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Vector2 size = arenaRect.rect.size;
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        int edge = UnityEngine.Random.Range(0, 4); // 0=上 1=下 2=左 3=右
        Vector2 spawnPos;
        switch (edge)
        {
            case 0: spawnPos = new Vector2(UnityEngine.Random.Range(-halfW, halfW), halfH + spawnMargin); break;
            case 1: spawnPos = new Vector2(UnityEngine.Random.Range(-halfW, halfW), -halfH - spawnMargin); break;
            case 2: spawnPos = new Vector2(-halfW - spawnMargin, UnityEngine.Random.Range(-halfH, halfH)); break;
            default: spawnPos = new Vector2(halfW + spawnMargin, UnityEngine.Random.Range(-halfH, halfH)); break;
        }

        // 枠内のランダムな地点へ向かって飛ばす（多方向感を出す）
        Vector2 targetPos = new Vector2(
            UnityEngine.Random.Range(-halfW * 0.6f, halfW * 0.6f),
            UnityEngine.Random.Range(-halfH * 0.6f, halfH * 0.6f));

        rt.anchoredPosition = spawnPos;
        Vector2 dir = (targetPos - spawnPos).normalized;

        var bullet = go.AddComponent<HiraganaBullet>();
        bullet.Init(letter, index, dir * bulletSpeed, japaneseFont, fontSize);

        activeBullets.Add(bullet);
    }

    void Update()
    {
        HandleMovement();
        UpdateBullets();

        if (!spawning && activeBullets.Count == 0 && spawnQueue != null)
            FinishRound();
    }

    void HandleMovement()
    {
        if (player == null || arenaRect == null) return;

        float h = 0f, v = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;

        Vector2 move = new Vector2(h, v);
        if (move.sqrMagnitude > 1f) move.Normalize();

        Vector2 pos = player.anchoredPosition + move * playerSpeed * Time.deltaTime;

        Vector2 size = arenaRect.rect.size;
        float halfW = Mathf.Max(0f, size.x * 0.5f - playerRadius);
        float halfH = Mathf.Max(0f, size.y * 0.5f - playerRadius);
        pos.x = Mathf.Clamp(pos.x, -halfW, halfW);
        pos.y = Mathf.Clamp(pos.y, -halfH, halfH);

        player.anchoredPosition = pos;
    }

    void UpdateBullets()
    {
        if (arenaRect == null) return;
        Vector2 arenaSize = arenaRect.rect.size;

        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            var b = activeBullets[i];
            if (b == null) { activeBullets.RemoveAt(i); continue; }

            if (b.IsOutOfBounds(arenaSize, despawnMargin))
            {
                activeBullets.RemoveAt(i);
                Destroy(b.gameObject);
                continue;
            }

            if (player != null)
            {
                float dist = Vector2.Distance(b.rt.anchoredPosition, player.anchoredPosition);
                if (dist <= playerRadius + bulletRadius)
                {
                    ResolveHit(b);
                    activeBullets.RemoveAt(i);
                    Destroy(b.gameObject);
                }
            }
        }
    }

    void ResolveHit(HiraganaBullet b)
    {
        bool correct = b.answerIndex == expectedIndex;

        if (correct)
        {
            combo++;
            expectedIndex++;
            if (SEManager.Instance != null) SEManager.Instance.PlayScaleNote(combo);
            if (PopupSpawner.Instance != null)
                PopupSpawner.Instance.SpawnCombo(combo, RectTransformToScreenPoint(b.rt));
        }
        else
        {
            if (SEManager.Instance != null) SEManager.Instance.Play("miss");
            OnWrongHit?.Invoke();
        }
    }

    // 被弾を知らせる（BattleManager側でプレイヤーHPを減らす）
    public Action OnWrongHit;

    Vector2 RectTransformToScreenPoint(RectTransform rt)
    {
        return RectTransformUtility.WorldToScreenPoint(null, rt.position);
    }

    void FinishRound()
    {
        spawnQueue = null; // FinishRoundの多重呼び出し防止
        int totalChars = answer.Length;
        int finalCombo = combo;

        gameObject.SetActive(false);
        OnTurnEnd?.Invoke(finalCombo, totalChars);
    }
}
