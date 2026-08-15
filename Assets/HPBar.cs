using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HPBar : MonoBehaviour
{
    [Header("参照")]
    public Image barFill;
    public Image barDelay;
    public TMP_Text hpText;

    [Header("HP")]
    public float maxHP = 300f;
    public float currentHP = 300f;

    [Header("アニメーション")]
    public float delayTime = 0.5f;
    public float delaySpeed = 2f;

    float delayHP;
    float delayTimer;

    void Start()
    {
        delayHP = currentHP;
        UpdateBar();
    }

    void Update()
    {
        // 遅延バー
        if (delayHP > currentHP)
        {
            delayTimer += Time.deltaTime;
            if (delayTimer >= delayTime)
            {
                delayHP = Mathf.Lerp(delayHP, currentHP, Time.deltaTime * delaySpeed);
                if (delayHP - currentHP < 0.1f) delayHP = currentHP;
            }
        }
        else
        {
            delayHP = currentHP;
            delayTimer = 0f;
        }

        UpdateBar();
    }

    void UpdateBar()
    {
        barFill.fillAmount = currentHP / maxHP;
        barDelay.fillAmount = delayHP / maxHP;
        hpText.text = Mathf.CeilToInt(currentHP) + " / " + Mathf.CeilToInt(maxHP);
    }

    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Clamp(currentHP - amount, 0f, maxHP);
        delayTimer = 0f;
    }

    public void Heal(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0f, maxHP);
    }
}