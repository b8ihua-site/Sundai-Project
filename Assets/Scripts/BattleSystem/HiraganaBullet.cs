using UnityEngine;
using TMPro;

// DodgeArena内を飛ぶひらがな1文字分の弾
[RequireComponent(typeof(RectTransform))]
public class HiraganaBullet : MonoBehaviour
{
    public int answerIndex = -1; // -1ならおとり。0以上なら答えの文字列中の位置
    public RectTransform rt { get; private set; }

    private Vector2 velocity;

    public void Init(char letter, int index, Vector2 vel, TMP_FontAsset font, float fontSize)
    {
        answerIndex = index;
        velocity = vel;
        rt = GetComponent<RectTransform>();

        var text = gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = letter.ToString();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white; // 正解・おとりとも同じ見た目にする（読んで見分ける部分がゲーム性）
        text.raycastTarget = false;
    }

    void Update()
    {
        rt.anchoredPosition += velocity * Time.deltaTime;
    }

    public bool IsOutOfBounds(Vector2 arenaSize, float margin)
    {
        Vector2 p = rt.anchoredPosition;
        float halfW = arenaSize.x * 0.5f + margin;
        float halfH = arenaSize.y * 0.5f + margin;
        return p.x < -halfW || p.x > halfW || p.y < -halfH || p.y > halfH;
    }
}
