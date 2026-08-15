using UnityEngine;
using UnityEngine.UI;

public class ImageBreathing : MonoBehaviour
{
    [Header("対象")]
    public RectTransform targetRect;    // 背景画像のRectTransform
    public Image targetImage;           // 背景画像のImage

    [Header("露光ゆらぎ")]
    public float exposureDriftAmount = 0.15f;  // 明度の変化幅
    public float exposureDriftSpeed  = 0.08f;

    [Header("揺れ")]
    public float shakeDriftAmount = 8f;        // ズレる最大ピクセル数
    public float shakeDriftSpeed  = 0.05f;

    [Header("まれなイベント")]
    public Vector2 eventInterval        = new Vector2(8f, 22f);
    public float eventDuration          = 2.5f;
    public float eventExposurePunch     = 0.3f;
    public float eventSmooth            = 0.8f;

    private float _seedExpX, _seedExpY, _seedShakeX, _seedShakeY;
    private float _eventTimer;
    private float _expEvent, _expTarget, _expVel;
    private Vector2 _originPos;
    private Color _originColor;

    void OnEnable()
    {
        if (targetRect  == null) targetRect  = GetComponent<RectTransform>();
        if (targetImage == null) targetImage = GetComponent<Image>();

        _originPos   = targetRect.anchoredPosition;
        _originColor = targetImage.color;

        _seedExpX   = Random.value * 100f;
        _seedExpY   = Random.value * 100f;
        _seedShakeX = Random.value * 100f;
        _seedShakeY = Random.value * 100f;

        _eventTimer = Random.Range(eventInterval.x, eventInterval.y);
    }

    void OnDisable()
    {
        if (targetRect  != null) targetRect.anchoredPosition = _originPos;
        if (targetImage != null) targetImage.color = _originColor;
    }

    void Update()
    {
        float t = Time.time;

        // 露光ゆらぎ（明度をPerlinNoiseで上下）
        float expDrift = (Mathf.PerlinNoise(_seedExpX, t * exposureDriftSpeed) - 0.5f)
                         * 2f * exposureDriftAmount;

        // まれなイベント
        _eventTimer -= Time.deltaTime;
        if (_eventTimer <= 0f)
        {
            _expTarget  = Random.Range(-eventExposurePunch, eventExposurePunch);
            Invoke(nameof(EndEvent), eventDuration);
            _eventTimer = Random.Range(eventInterval.x, eventInterval.y);
        }
        _expEvent = Mathf.SmoothDamp(_expEvent, _expTarget, ref _expVel, eventSmooth);

        // 明度に適用（元の色を基準に上下）
        float brightness = Mathf.Clamp01(_originColor.r + expDrift + _expEvent);
        targetImage.color = new Color(brightness, brightness, brightness, _originColor.a);

        // 揺れ（PerlinNoiseでXY方向にゆっくりズレる）
        float shakeX = (Mathf.PerlinNoise(_seedShakeX, t * shakeDriftSpeed) - 0.5f)
                       * 2f * shakeDriftAmount;
        float shakeY = (Mathf.PerlinNoise(_seedShakeY, t * shakeDriftSpeed) - 0.5f)
                       * 2f * shakeDriftAmount;

        targetRect.anchoredPosition = _originPos + new Vector2(shakeX, shakeY);
    }

    void EndEvent() { _expTarget = 0f; }
}