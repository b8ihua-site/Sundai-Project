using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ClockSystem : MonoBehaviour
{
    public TextMeshProUGUI clockText; // ← フィールド名を合わせる

    const float RingSize = 44f;
    const float RingGap = 62f; // リング同士の縦間隔
    const float RingInsetFromRight = 52f;

    Image mathFill, scienceFill, ichigayaFill;

    void Awake()
    {
        // clockTextの箱はpivotが中央のまま画面右端に配置されているため、
        // 日付表記を足して文字が長くなると右端からはみ出していた。
        // pivotを右端に合わせ、文字も右詰めにして、常に画面内に収まるようにする
        var rt = clockText.rectTransform;
        rt.pivot = new Vector2(1f, rt.pivot.y);
        clockText.alignment = TextAlignmentOptions.MidlineRight;

        BuildAbilityRings();
    }

    void Update()
    {
        var pa = PlayerAbilities.Instance;
        string dateText = MenuController.FormatRoninDate(pa);
        string timeText = DateTime.Now.ToString("HH:mm");
        clockText.text = string.IsNullOrEmpty(dateText) ? timeText : $"{dateText} {timeText}";

        if (pa == null) return;
        mathFill.fillAmount     = Mathf.Clamp01((float)pa.mathPower / pa.abilityThreshold);
        scienceFill.fillAmount  = Mathf.Clamp01((float)pa.sciencePower / pa.abilityThreshold);
        ichigayaFill.fillAmount = Mathf.Clamp01((float)pa.ichigayaPower / pa.abilityThreshold);
    }

    // 画面右端の中央あたりに、各科目の達成度を円状の棒グラフで縦に並べて常時表示する
    void BuildAbilityRings()
    {
        var parent = transform.parent; // ClockSystemと同じ常在UI Canvas
        var ringSprite = MenuController.CreateRingSprite();
        var circleSprite = MenuController.CreateCircleSprite();

        mathFill     = BuildRing(parent, ringSprite, circleSprite, "数学",   new Color(0.4f, 0.6f, 1f),   new Vector2(0f, RingGap));
        scienceFill  = BuildRing(parent, ringSprite, circleSprite, "理科",   new Color(0.4f, 0.9f, 0.5f), Vector2.zero);
        ichigayaFill = BuildRing(parent, ringSprite, circleSprite, "市ヶ谷", new Color(1f, 0.5f, 0.5f),   new Vector2(0f, -RingGap));
    }

    Image BuildRing(Transform parent, Sprite ringSprite, Sprite circleSprite, string label, Color color, Vector2 yOffset)
    {
        var anchoredPos = new Vector2(-RingInsetFromRight, 0f) + yOffset;

        var root = new GameObject("AbilityRing_" + label, typeof(RectTransform));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(1f, 0.5f); // 画面右端の中央を基準に縦並び
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = anchoredPos;
        rootRect.sizeDelta = new Vector2(RingSize, RingSize);

        // 背景（暗い円）：ゲーム画面の上でも視認しやすいようにする
        var backing = root.AddComponent<Image>();
        backing.sprite = circleSprite;
        backing.color = new Color(0f, 0f, 0f, 0.4f);

        var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.SetParent(root.transform, false);
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bg.GetComponent<Image>();
        bgImage.sprite = ringSprite;
        bgImage.color = new Color(1f, 1f, 1f, 0.2f);

        // 進捗リング（達成分）：リング形のスプライトにRadial360フィルをかけて円状の棒グラフにする
        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.SetParent(root.transform, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fillGo.GetComponent<Image>();
        fillImage.sprite = ringSprite;
        fillImage.color = color;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Radial360;
        fillImage.fillOrigin = (int)Image.Origin360.Top;
        fillImage.fillClockwise = true;
        fillImage.fillAmount = 0f;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.SetParent(root.transform, false);
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(RingSize, RingSize);
        var labelText = labelGo.AddComponent<TextMeshProUGUI>();
        if (clockText.font != null) labelText.font = clockText.font;
        labelText.text = label;
        labelText.fontSize = 10f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(1f, 1f, 1f, 0.85f);
        labelText.raycastTarget = false;
        labelText.enableWordWrapping = false;

        return fillImage;
    }
}
