using UnityEngine;

public class PopupSpawner : MonoBehaviour
{
    public static PopupSpawner Instance;

    [Header("Prefab")]
    public GameObject popupPrefab;
    public RectTransform canvasRect;

    [Header("自分へのダメージ")]
    public Color playerDamageColor = new Color(1f, 0.35f, 0.35f);
    public float playerDamageFontSize = 40f;

    [Header("敵へのダメージ")]
    public Color enemyDamageColor = new Color(1f, 0.85f, 0.3f);
    public float enemyDamageFontSize = 40f;

    [Header("コンボ")]
    public Color comboColor = new Color(0.4f, 0.9f, 1f);
    public float comboFontSize = 28f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Spawn(string text, Color color, Vector2 screenPos, float fontSize = 36f)
    {
        var go = Instantiate(popupPrefab, canvasRect);
        var rect = go.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, null, out Vector2 localPos);
        rect.anchoredPosition = localPos;

        go.GetComponent<DamagePopup>().Setup(text, color, fontSize);
    }

    public void SpawnDamage(int damage, Vector2 screenPos)
        => Spawn($"-{damage}", playerDamageColor, screenPos, playerDamageFontSize);

    public void SpawnEnemyDamage(int damage, Vector2 screenPos)
        => Spawn($"-{damage}", enemyDamageColor, screenPos, enemyDamageFontSize);

    public void SpawnCombo(int combo, Vector2 screenPos)
        => Spawn($"COMBO {combo}!", comboColor, screenPos, comboFontSize);
}