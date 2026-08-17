using UnityEngine;
using TMPro;

// DodgeArena内を飛ぶひらがな1文字分の弾
// 攻撃パターンごとの動き（直進・蛇行・溜め・分裂）を持つ
[RequireComponent(typeof(RectTransform))]
public class HiraganaBullet : MonoBehaviour
{
    public enum Motion { Linear, Sine, Telegraph, Split }

    public int answerIndex = -1; // -1ならおとり。0以上なら答えの文字列中の位置
    public RectTransform rt { get; private set; }
    public char Letter { get; private set; }
    public Vector2 Velocity => velocity;
    public bool IsHeal { get; private set; }
    public float Age => age;

    private Vector2 velocity;
    private Motion motion = Motion.Linear;
    private float age;
    private TextMeshProUGUI text;

    // Sine用：進行方向に垂直にsinで揺れる
    private Vector2 sineForward;
    private Vector2 sinePerp;
    private float sineSpeed;
    private float sineAmplitude;
    private float sineFrequency;

    // Telegraph用：一瞬パルスして静止したあと勢いよく発射する
    private float telegraphDelay;
    private bool telegraphReady;
    private Vector3 baseScale;

    // Split用：一定時間で2体に分裂する（分裂処理自体はDodgeArena側で行う）
    private float splitDelay;
    private bool splitPending;
    private bool hasSplit;

    public void Init(char letter, int index, Vector2 vel, TMP_FontAsset font, float fontSize)
    {
        answerIndex = index;
        Letter = letter;
        velocity = vel;
        motion = Motion.Linear;
        rt = GetComponent<RectTransform>();

        text = gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = letter.ToString();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white; // 正解・おとりとも同じ見た目にする（読んで見分ける部分がゲーム性）
        text.raycastTarget = false;

        baseScale = rt.localScale;
    }

    // ごく低確率で出現する回復弾（正解の文字にのみ使う）。色を変えて見た目で分かるようにする
    public void SetHeal(Color color)
    {
        IsHeal = true;
        if (text != null) text.color = color;
    }

    public void SetSine(float amplitude, float frequency)
    {
        motion = Motion.Sine;
        sineForward = velocity.normalized;
        sinePerp = new Vector2(-sineForward.y, sineForward.x);
        sineSpeed = velocity.magnitude;
        sineAmplitude = amplitude;
        sineFrequency = frequency;
    }

    public void SetTelegraph(float delaySeconds)
    {
        motion = Motion.Telegraph;
        telegraphDelay = delaySeconds;
        telegraphReady = false;
    }

    public void SetSplit(float delaySeconds)
    {
        motion = Motion.Split;
        splitDelay = delaySeconds;
    }

    // DodgeArenaが1フレームに1回だけ「分裂の合図が来たか」を確認する
    public bool ConsumeSplitRequest()
    {
        if (!splitPending) return false;
        splitPending = false;
        return true;
    }

    void Update()
    {
        age += Time.deltaTime;

        switch (motion)
        {
            case Motion.Sine:
                {
                    float perpVel = sineAmplitude * sineFrequency * Mathf.Cos(age * sineFrequency);
                    rt.anchoredPosition += sineForward * sineSpeed * Time.deltaTime
                                          + sinePerp * perpVel * Time.deltaTime;
                    break;
                }

            case Motion.Telegraph:
                if (!telegraphReady)
                {
                    if (age >= telegraphDelay)
                    {
                        telegraphReady = true;
                        rt.localScale = baseScale;
                    }
                    else
                    {
                        // 溜め演出：発射前に軽くパルスさせて予告する
                        float pulse = 1f + Mathf.PingPong(age * 8f, 0.15f);
                        rt.localScale = baseScale * pulse;
                        return; // 発射前は移動しない
                    }
                }
                rt.anchoredPosition += velocity * Time.deltaTime;
                break;

            case Motion.Split:
                rt.anchoredPosition += velocity * Time.deltaTime;
                if (!hasSplit && age >= splitDelay)
                {
                    hasSplit = true;
                    splitPending = true;
                }
                break;

            default:
                rt.anchoredPosition += velocity * Time.deltaTime;
                break;
        }
    }

    public bool IsOutOfBounds(Vector2 arenaSize, float margin)
    {
        Vector2 p = rt.anchoredPosition;
        float halfW = arenaSize.x * 0.5f + margin;
        float halfH = arenaSize.y * 0.5f + margin;
        return p.x < -halfW || p.x > halfW || p.y < -halfH || p.y > halfH;
    }
}
