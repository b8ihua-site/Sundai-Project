using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// マップ内をただよう「知識の風」。カメラで中心に捉えて撮影されると具現化し、強制戦闘になる
public class KnowledgeWind : MonoBehaviour, IPhotographable
{
    public enum WindColor { Blue, Green, Red }

    [Header("色と科目")]
    public WindColor color = WindColor.Red;
    public string subjectName = "市ヶ谷"; // 青=数学 緑=理科 赤=市ヶ谷 を想定
    public string enemyName = "知識の風"; // 「〇〇の風」の形を想定（構え中の名前表示にも使う）
    public int enemyMaxHP = 80;
    public int enemyAttack = 15;
    public int moneyRewardMin = 10;
    public int moneyRewardMax = 20;

    [Header("見た目（パーティクルのみ）")]
    public float visualScale = 0.8f;
    public float pulseAmount = 0.15f;
    public float pulseSpeed = 1.5f;
    public float particleEmissionRate = 100f;
    public float particleGravity = 0.4f; // 下に落ちる強さ
    [Range(0f, 1f)] public float particleAlpha = 0.7f; // 透明度（大きいほど濃い）

    [Header("ただよう動き")]
    public float driftRadius = 3f;
    public float driftSpeed = 0.3f;
    public float bobHeight = 0.3f;
    public float bobSpeed = 1f;

    [Header("名前表示（構えて狙っている時だけ表示）")]
    public float nameLabelHeight = 1.2f;
    public float nameLabelFontSize = 3f;
    public TMP_FontAsset japaneseFont; // 日本語グリフを含むフォント（未設定だと豆腐になる）

    [Header("シーン")]
    public string battleSceneName = "BattleScene";

    private Vector3 homePosition;
    private float noiseOffsetX;
    private float noiseOffsetZ;
    private float pulseOffset;
    private bool materialized = false;
    private bool found = false; // 発見演出～選択待ちの間、漂うのを止める
    private TextMeshPro nameLabel;
    private bool aimHighlighted = false;

    public string DisplayName => enemyName;

    void Start()
    {
        // スポナーがcolor/subjectNameを設定し終えてから見た目を作る
        // （Awakeで作るとAddComponent直後の色反映前に見た目が確定してしまうため）
        // 1つの構築処理が失敗しても他が止まらないよう個別にtry-catchする
        try { BuildCollider(); } catch (System.Exception e) { Debug.LogError("KnowledgeWind BuildCollider failed: " + e); }
        try { BuildParticles(); } catch (System.Exception e) { Debug.LogError("KnowledgeWind BuildParticles failed: " + e); }
        try { BuildNameLabel(); } catch (System.Exception e) { Debug.LogError("KnowledgeWind BuildNameLabel failed: " + e); }

        homePosition = transform.position;
        noiseOffsetX = Random.Range(0f, 1000f);
        noiseOffsetZ = Random.Range(0f, 1000f);
        pulseOffset = Random.Range(0f, 10f);
    }

    void Update()
    {
        if (materialized || found) return;

        float t = Time.time * driftSpeed;
        float x = (Mathf.PerlinNoise(noiseOffsetX, t) - 0.5f) * 2f * driftRadius;
        float z = (Mathf.PerlinNoise(noiseOffsetZ, t) - 0.5f) * 2f * driftRadius;
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = homePosition + new Vector3(x, y, z);

        float pulse = 1f + Mathf.Sin((Time.time + pulseOffset) * pulseSpeed) * pulseAmount;
        transform.localScale = Vector3.one * visualScale * pulse;

        if (aimHighlighted && nameLabel != null)
        {
            var cam = Camera.main;
            if (cam != null)
                nameLabel.transform.rotation = cam.transform.rotation; // 常にカメラの方を向ける
        }
    }

    // カメラのIPhotographable経由で呼ばれる（撮影時に画面中心に捉えられていた場合）
    // ここではまだ戦闘には入らず、発見演出（漂うのを止める）だけ行う
    public void OnPhotographed()
    {
        found = true;
    }

    // 「たたかう」が選ばれた
    public void Capture()
    {
        if (materialized) return;
        materialized = true;
        Materialize();
    }

    // 「みのがす」が選ばれた
    public void Release()
    {
        found = false; // 再び漂い始める
    }

    // カメラのIPhotographable経由で呼ばれる（構え中、今まさに狙われているかどうか）
    public void SetAimHighlight(bool highlighted)
    {
        aimHighlighted = highlighted;
        if (nameLabel != null)
            nameLabel.gameObject.SetActive(highlighted);
    }

    void Materialize()
    {
        BattleContext.HasData    = true;
        BattleContext.EnemyName  = enemyName;
        BattleContext.EnemyMaxHP = enemyMaxHP;
        BattleContext.EnemyAttack= enemyAttack;
        BattleContext.Subject    = subjectName;
        BattleContext.WindColor  = color.ToString();
        BattleContext.MoneyMin   = moneyRewardMin;
        BattleContext.MoneyMax   = moneyRewardMax;
        BattleContext.EnemyLevel = PlayerAbilities.Instance != null ? PlayerAbilities.Instance.level : 1;

        PlayerAbilities.CaptureFieldPosition();
        SceneManager.LoadScene(battleSceneName);
    }

    void BuildCollider()
    {
        // 見た目はパーティクルのみにし、撮影判定用の当たり判定だけ球で持たせる
        var col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true; // プレイヤーの移動を物理的に妨げないように

        transform.localScale = Vector3.one * visualScale;
    }

    void BuildNameLabel()
    {
        var go = new GameObject("NameLabel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * nameLabelHeight;

        nameLabel = go.AddComponent<TextMeshPro>();
        if (japaneseFont != null)
            nameLabel.font = japaneseFont;
        nameLabel.text = enemyName;
        nameLabel.fontSize = nameLabelFontSize;
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.color = Color.white;

        go.SetActive(false); // 狙われている時だけSetAimHighlightで表示する
    }

    void BuildParticles()
    {
        Color c = GetDisplayColor();

        var ps = gameObject.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startColor = new Color(c.r, c.g, c.b, particleAlpha);
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.45f);
        main.gravityModifier = particleGravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 500;

        var emission = ps.emission;
        emission.rateOverTime = particleEmissionRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.45f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(particleAlpha, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // Sprites/Defaultは透過ブレンドが標準で効くシェーダーなので優先して使う
        // （URPのLit/Unlit系はSurfaceTypeをTransparentに設定しないと不透明のまま描画されてしまう）
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
        if (shader == null) return; // 対応シェーダーが見つからない場合は見た目の追加をあきらめる（コライダーは既にできている）
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);

        var circleTex = CreateSoftCircleTexture();
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", circleTex);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", circleTex);

        renderer.material = mat;
    }

    // 中心が濃く外側が透明になる、丸いパーティクル用のテクスチャをその場で作る
    Texture2D CreateSoftCircleTexture(int size = 32)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(1f - dist / maxDist);
                alpha = alpha * alpha; // 端をソフトにフェードさせる
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    Color GetDisplayColor()
    {
        switch (color)
        {
            case WindColor.Blue:  return new Color(0.55f, 0.68f, 0.92f, 0.85f);
            case WindColor.Green: return new Color(0.58f, 0.82f, 0.62f, 0.85f);
            default:               return new Color(0.92f, 0.58f, 0.55f, 0.85f);
        }
    }
}
