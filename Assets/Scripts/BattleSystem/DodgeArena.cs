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
    public float maxBulletLifetime = 6f; // この秒数を過ぎたら位置に関係なく強制的に消す（詰まり防止の保険）
    public float fontSize = 30f;
    public TMP_FontAsset japaneseFont;

    [Header("おとり文字プール")]
    public string hiraganaPool = "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん";

    [Header("回復弾")]
    [Range(0f, 1f)] public float healChance = 0.05f; // 正解の文字が回復弾になる確率
    public Color healColor = new Color(0.6f, 0.9f, 0.25f); // 黄緑

    // ターン終了時に (コンボ数, 文字数) を渡して呼ばれる
    public Action<int, int> OnTurnEnd;

    // 回復弾に当たった時に呼ばれる（BattleManager側でHPを回復する）
    public Action OnHeal;

    // ---------- 攻撃パターン ----------
    private enum BulletPattern
    {
        RowWave,        // 一斉横列
        CornerConverge, // 四隅から収束
        SingleEdgeBurst,// 一点集中スポーン
        StraightDrop,   // 静止ドロップ（上から真下）
        SineWave,       // 蛇行（サインカーブ）
        Telegraph,      // 溜め→高速発射
        Split,          // 分裂弾
    }

    private static readonly BulletPattern[] AllPatterns =
        (BulletPattern[])Enum.GetValues(typeof(BulletPattern));

    private BulletPattern currentPattern;
    private int fixedEdge; // RowWave/SingleEdgeBurstで使う、このターン固定の辺（0上 1下 2左 3右）

    private string answer;
    private int combo;
    private Queue<int> spawnQueue;
    private readonly List<HiraganaBullet> activeBullets = new List<HiraganaBullet>();
    private bool spawning;
    private Coroutine spawnCoroutine;

    public void StartRound(string answerText)
    {
        answer = answerText ?? "";
        combo = 0;

        ClearBullets();

        spawnQueue = new Queue<int>();
        for (int i = 0; i < answer.Length; i++)
            spawnQueue.Enqueue(i);

        currentPattern = AllPatterns[UnityEngine.Random.Range(0, AllPatterns.Length)];
        fixedEdge = UnityEngine.Random.Range(0, 4);

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

    // ---------- 弾の生成（パターンごとに出現位置・初速・動きが変わる） ----------

    void SpawnBullet(int index, char letter)
    {
        if (arenaRect == null) return;

        switch (currentPattern)
        {
            case BulletPattern.RowWave: SpawnRowWave(index, letter); break;
            case BulletPattern.CornerConverge: SpawnCornerConverge(index, letter); break;
            case BulletPattern.SingleEdgeBurst: SpawnSingleEdgeBurst(index, letter); break;
            case BulletPattern.StraightDrop: SpawnStraightDrop(index, letter); break;
            case BulletPattern.SineWave: SpawnSineWave(index, letter); break;
            case BulletPattern.Telegraph: SpawnTelegraph(index, letter); break;
            case BulletPattern.Split: SpawnSplitCarrier(index, letter); break;
        }
    }

    // 1: 一斉横列 ―― 固定した1辺から、本命の左右にもおとりを添えて横並びに出す
    void SpawnRowWave(int index, char letter)
    {
        float t = UnityEngine.Random.Range(-0.8f, 0.8f);
        SpawnStraightFromEdge(fixedEdge, t, index, letter);

        foreach (float dt in new[] { -0.35f, 0.35f })
        {
            float ct = Mathf.Clamp(t + dt, -1f, 1f);
            SpawnStraightFromEdge(fixedEdge, ct, -1, RandomDecoyChar());
        }
    }

    // 2: 四隅から収束 ―― ランダムな角から本命、別の角からおとりが中央へ向かって飛ぶ
    void SpawnCornerConverge(int index, char letter)
    {
        Vector2 half = HalfSize();
        Vector2[] cornerSigns = { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) };

        int c1 = UnityEngine.Random.Range(0, 4);
        Vector2 pos1 = Vector2.Scale(cornerSigns[c1], half + Vector2.one * spawnMargin);
        CreateBullet(pos1, (-pos1).normalized * bulletSpeed, index, letter);

        int c2;
        do { c2 = UnityEngine.Random.Range(0, 4); } while (c2 == c1);
        Vector2 pos2 = Vector2.Scale(cornerSigns[c2], half + Vector2.one * spawnMargin);
        CreateBullet(pos2, (-pos2).normalized * bulletSpeed, -1, RandomDecoyChar());
    }

    // 4: 一点集中スポーン ―― このターン固定の辺の、ほぼ同じ場所から連続して出現
    void SpawnSingleEdgeBurst(int index, char letter)
    {
        float t = UnityEngine.Random.Range(-0.15f, 0.15f);
        SpawnStraightFromEdge(fixedEdge, t, index, letter, speedMultiplier: 1.15f);
    }

    // 5: 静止ドロップ ―― 上から真下へまっすぐ落ちてくる
    void SpawnStraightDrop(int index, char letter)
    {
        Vector2 half = HalfSize();
        float x = UnityEngine.Random.Range(-half.x * 0.9f, half.x * 0.9f);
        Vector2 pos = new Vector2(x, half.y + spawnMargin);
        CreateBullet(pos, Vector2.down * (bulletSpeed * 0.8f), index, letter);
    }

    // 8: サインカーブ（蛇行） ―― 直進しつつ左右にS字で揺れる
    void SpawnSineWave(int index, char letter)
    {
        var (pos, vel) = RandomEdgeSpawn();
        var b = CreateBullet(pos, vel, index, letter);
        b.SetSine(amplitude: 40f, frequency: 3f);
    }

    // 11: 溜め→高速発射 ―― 枠のふちで一瞬パルスして予告したあと、勢いよく飛んでくる
    void SpawnTelegraph(int index, char letter)
    {
        int edge = UnityEngine.Random.Range(0, 4);
        float t = UnityEngine.Random.Range(-0.7f, 0.7f);
        Vector2 pos = EdgePoint(edge, t, 0f);
        Vector2 vel = InwardDir(edge) * (bulletSpeed * 1.6f);
        var b = CreateBullet(pos, vel, index, letter);
        b.SetTelegraph(0.5f);
    }

    // 13: 分裂弾 ―― 飛んでいる途中で2つに分かれる（本命は文字を引き継ぐ）
    void SpawnSplitCarrier(int index, char letter)
    {
        var (pos, vel) = RandomEdgeSpawn();
        var b = CreateBullet(pos, vel, index, letter);
        b.SetSplit(0.45f);
    }

    void HandleSplit(HiraganaBullet parent)
    {
        Vector2 pos = parent.rt.anchoredPosition;
        Vector2 baseDir = parent.Velocity.sqrMagnitude > 0.001f ? parent.Velocity.normalized : Vector2.up;
        float speed = Mathf.Max(parent.Velocity.magnitude, bulletSpeed);

        CreateBullet(pos, RotateDir(baseDir, 35f) * speed, parent.answerIndex, parent.Letter);
        CreateBullet(pos, RotateDir(baseDir, -35f) * speed, -1, RandomDecoyChar());
    }

    // ---------- 出現ジオメトリの共通ヘルパー ----------

    Vector2 HalfSize()
    {
        Vector2 size = arenaRect.rect.size;
        return new Vector2(size.x * 0.5f, size.y * 0.5f);
    }

    // t: -1〜1（辺に沿った位置）、margin: 辺の外にどれだけ離すか
    Vector2 EdgePoint(int edge, float t, float margin)
    {
        Vector2 half = HalfSize();
        float lerp = (Mathf.Clamp(t, -1f, 1f) + 1f) * 0.5f;
        switch (edge)
        {
            case 0: return new Vector2(Mathf.Lerp(-half.x, half.x, lerp), half.y + margin);  // 上
            case 1: return new Vector2(Mathf.Lerp(-half.x, half.x, lerp), -half.y - margin); // 下
            case 2: return new Vector2(-half.x - margin, Mathf.Lerp(-half.y, half.y, lerp)); // 左
            default: return new Vector2(half.x + margin, Mathf.Lerp(-half.y, half.y, lerp)); // 右
        }
    }

    Vector2 InwardDir(int edge)
    {
        switch (edge)
        {
            case 0: return Vector2.down;
            case 1: return Vector2.up;
            case 2: return Vector2.right;
            default: return Vector2.left;
        }
    }

    void SpawnStraightFromEdge(int edge, float t, int index, char letter, float speedMultiplier = 1f)
    {
        Vector2 pos = EdgePoint(edge, t, spawnMargin);
        Vector2 vel = InwardDir(edge) * (bulletSpeed * speedMultiplier);
        CreateBullet(pos, vel, index, letter);
    }

    // 元々のデフォルト挙動：ランダムな辺から、枠内のランダムな地点へ向かって飛ぶ
    (Vector2 pos, Vector2 vel) RandomEdgeSpawn()
    {
        Vector2 half = HalfSize();
        int edge = UnityEngine.Random.Range(0, 4);
        Vector2 spawnPos;
        switch (edge)
        {
            case 0: spawnPos = new Vector2(UnityEngine.Random.Range(-half.x, half.x), half.y + spawnMargin); break;
            case 1: spawnPos = new Vector2(UnityEngine.Random.Range(-half.x, half.x), -half.y - spawnMargin); break;
            case 2: spawnPos = new Vector2(-half.x - spawnMargin, UnityEngine.Random.Range(-half.y, half.y)); break;
            default: spawnPos = new Vector2(half.x + spawnMargin, UnityEngine.Random.Range(-half.y, half.y)); break;
        }

        Vector2 targetPos = new Vector2(
            UnityEngine.Random.Range(-half.x * 0.6f, half.x * 0.6f),
            UnityEngine.Random.Range(-half.y * 0.6f, half.y * 0.6f));

        Vector2 dir = (targetPos - spawnPos).normalized;
        return (spawnPos, dir * bulletSpeed);
    }

    static Vector2 RotateDir(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    HiraganaBullet CreateBullet(Vector2 spawnPos, Vector2 velocity, int index, char letter)
    {
        var go = new GameObject("Bullet");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(arenaRect, false);
        rt.sizeDelta = new Vector2(50f, 50f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = spawnPos;

        var bullet = go.AddComponent<HiraganaBullet>();
        bullet.Init(letter, index, velocity, japaneseFont, fontSize);

        // 正解の文字だけ、ごく低確率で回復弾にする
        if (index >= 0 && UnityEngine.Random.value < healChance)
            bullet.SetHeal(healColor);

        activeBullets.Add(bullet);
        return bullet;
    }

    // ---------- 毎フレーム更新 ----------

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

            if (b.ConsumeSplitRequest())
            {
                HandleSplit(b);
                activeBullets.RemoveAt(i);
                Destroy(b.gameObject);
                continue;
            }

            if (b.IsOutOfBounds(arenaSize, despawnMargin) || b.Age > maxBulletLifetime)
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
        // 順番は問わない。答えの文字（おとりでない）なら正解扱い
        bool correct = b.answerIndex >= 0;

        if (correct)
        {
            combo++;
            if (SEManager.Instance != null) SEManager.Instance.PlayScaleNote(combo);
            if (PopupSpawner.Instance != null)
                PopupSpawner.Instance.SpawnCombo(combo, RectTransformToScreenPoint(b.rt));

            if (b.IsHeal) OnHeal?.Invoke();
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
