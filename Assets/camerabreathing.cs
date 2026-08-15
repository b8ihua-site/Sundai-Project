using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // HDRPなら HighDefinition

[RequireComponent(typeof(Volume))]
public class CinematicBreathing : MonoBehaviour
{
    [Header("参照")]
    public Volume volume;
    public VolumeProfile sourceProfile; // ← ここに Global Volume Profile アセットをドラッグ（最優先で使う）

    [Header("露光ゆらぎ")]
    public float exposureDriftAmount = 0.2f;
    public float exposureDriftSpeed  = 0.08f;

    [Header("ピンボケゆらぎ")]
    public float focusDriftAmount = 1.5f;
    public float focusDriftSpeed  = 0.05f;

    [Header("まれなイベント")]
    public Vector2 eventInterval = new Vector2(8f, 22f);
    public float eventDuration      = 2.5f;
    public float eventExposurePunch = 0.4f;
    public float eventFocusPunch    = 5f;
    public float eventSmooth        = 0.8f;

    ColorAdjustments _color;
    DepthOfField _dof;
    float _baseExp, _baseFocus;
    float _seedExp, _seedFocus, _eventTimer;
    float _expEvent, _focusEvent, _expTarget, _focusTarget, _expVel, _focusVel;

    void OnEnable()
    {
        if (volume == null) volume = GetComponent<Volume>();

        // 優先順位: 手で指定したアセット → sharedProfile → profile
        VolumeProfile p = sourceProfile;
        if (p == null) p = volume.sharedProfile;
        if (p == null) p = volume.profile;

        if (p == null) { Debug.LogError("[Breathing] プロファイルが取得できません"); enabled = false; return; }
        Debug.Log($"[Breathing] 使用プロファイル: {p.name} / オーバーライド数 {p.components.Count}");

        if (!p.TryGet(out _color)) Debug.LogWarning("[Breathing] このプロファイルに Color Adjustments が無い");
        if (!p.TryGet(out _dof))   Debug.LogWarning("[Breathing] このプロファイルに Depth of Field が無い");

        if (_color != null) { _color.postExposure.overrideState = true;  _baseExp   = _color.postExposure.value; }
        if (_dof   != null) { _dof.focusDistance.overrideState  = true;  _baseFocus = _dof.focusDistance.value; }

        Debug.Log($"[Breathing] 開始: ColorAdjustments={_color!=null}, DepthOfField={_dof!=null}");

        _seedExp = Random.value * 100f;
        _seedFocus = Random.value * 100f;
        _eventTimer = Random.Range(eventInterval.x, eventInterval.y);
    }

    void OnDisable()
    {
        if (_color != null) _color.postExposure.value = _baseExp;
        if (_dof   != null) _dof.focusDistance.value  = _baseFocus;
    }

    void Update()
    {
        float t = Time.time;
        float expDrift   = (Mathf.PerlinNoise(_seedExp,   t * exposureDriftSpeed) - 0.5f) * 2f * exposureDriftAmount;
        float focusDrift = (Mathf.PerlinNoise(_seedFocus, t * focusDriftSpeed)    - 0.5f) * 2f * focusDriftAmount;

        _eventTimer -= Time.deltaTime;
        if (_eventTimer <= 0f)
        {
            _expTarget   = Random.Range(-eventExposurePunch, eventExposurePunch);
            _focusTarget = Random.Range(-eventFocusPunch, eventFocusPunch);
            Invoke(nameof(EndEvent), eventDuration);
            _eventTimer = Random.Range(eventInterval.x, eventInterval.y);
        }
        _expEvent   = Mathf.SmoothDamp(_expEvent,   _expTarget,   ref _expVel,   eventSmooth);
        _focusEvent = Mathf.SmoothDamp(_focusEvent, _focusTarget, ref _focusVel, eventSmooth);

        if (_color != null) _color.postExposure.value = _baseExp   + expDrift   + _expEvent;
        if (_dof   != null) _dof.focusDistance.value  = Mathf.Max(0.1f, _baseFocus + focusDrift + _focusEvent);
    }

    void EndEvent() { _expTarget = 0f; _focusTarget = 0f; }
}