using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CompassBar : MonoBehaviour
{
    [Header("参照")]
    public RectTransform ticker;

    [Header("サイズ")]
    public float barWidth = 600f;
    public int majorTickCount = 20;
    public float majorTickSpacing = 60f;
    public int minorTicksPerMajor = 4;

    [Header("大目盛り")]
    public float majorTickWidth = 1.5f;
    public float majorTickHeight = 10f;
    public Color majorTickColor = new Color(1f, 1f, 1f, 0.6f);

    [Header("小目盛り")]
    public float minorTickWidth = 1f;
    public float minorTickHeight = 5f;
    public Color minorTickColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("ラベル")]
    public TMP_FontAsset font;
    public float fontSize = 14f;
    public Color cardinalColor = new Color(1f, 0.85f, 0.3f, 1f);

    GameObject[] majorTicks;
    GameObject[] minorTicks;
    TMP_Text[] labels;

    string[] cardinals = { "N", "E", "S", "W" };
    float[] cardinalAngles = { 0f, 90f, 180f, 270f };

    void Start()
    {
        CreateMajorTicks();
        CreateMinorTicks();
        CreateCardinalLabels();
    }

    void CreateMajorTicks()
    {
        majorTicks = new GameObject[majorTickCount];
        for (int i = 0; i < majorTickCount; i++)
        {
            GameObject tick = new GameObject("MajorTick_" + i);
            tick.transform.SetParent(ticker, false);
            RectTransform rt = tick.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(majorTickWidth, majorTickHeight);
            Image img = tick.AddComponent<Image>();
            img.color = majorTickColor;
            majorTicks[i] = tick;
        }
    }

    void CreateMinorTicks()
    {
        int totalMinor = majorTickCount * minorTicksPerMajor;
        minorTicks = new GameObject[totalMinor];
        for (int i = 0; i < totalMinor; i++)
        {
            GameObject tick = new GameObject("MinorTick_" + i);
            tick.transform.SetParent(ticker, false);
            RectTransform rt = tick.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(minorTickWidth, minorTickHeight);
            Image img = tick.AddComponent<Image>();
            img.color = minorTickColor;
            minorTicks[i] = tick;
        }
    }

    void CreateCardinalLabels()
    {
        labels = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject obj = new GameObject("Label_" + cardinals[i]);
            obj.transform.SetParent(ticker, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(30f, 20f);
            TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = cardinals[i];
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = cardinalColor;
            if (font != null) tmp.font = font;
            labels[i] = tmp;
        }
    }

    void Update()
    {
        float camAngle = Camera.main.transform.eulerAngles.y;
        float totalWidth = majorTickSpacing * majorTickCount;
        float offset = -((camAngle + 180f) / 360f) * totalWidth;

        for (int i = 0; i < majorTickCount; i++)
        {
            float x = (i * majorTickSpacing + offset) % totalWidth;
            if (x < -totalWidth * 0.5f) x += totalWidth;
            if (x >  totalWidth * 0.5f) x -= totalWidth;

            RectTransform rt = majorTicks[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(majorTickWidth, majorTickHeight);
            majorTicks[i].GetComponent<Image>().color = majorTickColor;
            majorTicks[i].SetActive(Mathf.Abs(x) < barWidth * 0.5f);
        }

        float minorSpacing = majorTickSpacing / (minorTicksPerMajor + 1);
        int totalMinor = majorTickCount * minorTicksPerMajor;
        for (int i = 0; i < totalMinor; i++)
        {
            int major = i / minorTicksPerMajor;
            int minor = i % minorTicksPerMajor + 1;
            float baseX = major * majorTickSpacing + minor * minorSpacing;

            float x = (baseX + offset) % totalWidth;
            if (x < -totalWidth * 0.5f) x += totalWidth;
            if (x >  totalWidth * 0.5f) x -= totalWidth;

            RectTransform rt = minorTicks[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(minorTickWidth, minorTickHeight);
            minorTicks[i].GetComponent<Image>().color = minorTickColor;
            minorTicks[i].SetActive(Mathf.Abs(x) < barWidth * 0.5f);
        }

        for (int i = 0; i < 4; i++)
        {
            float x = ((-camAngle - 180f + cardinalAngles[i]) / 360f) * totalWidth;
            x = x % totalWidth;
            if (x < -totalWidth * 0.5f) x += totalWidth;
            if (x >  totalWidth * 0.5f) x -= totalWidth;

            RectTransform rt = labels[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, -16f);
            labels[i].fontSize = fontSize;
            labels[i].color = cardinalColor;
            if (font != null) labels[i].font = font;
            labels[i].gameObject.SetActive(Mathf.Abs(x) < barWidth * 0.5f);
        }
    }
}