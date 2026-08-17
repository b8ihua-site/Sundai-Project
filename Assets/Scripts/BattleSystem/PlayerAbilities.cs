using UnityEngine;

// 知識の風を倒すごとに色ごとの能力値を蓄積し、すべてが一定値に達したらレベルアップする
public class PlayerAbilities : MonoBehaviour
{
    public static PlayerAbilities Instance;

    [Header("能力値（青=数学 / 緑=理科 / 赤=市ヶ谷）")]
    public int mathPower = 0;
    public int sciencePower = 0;
    public int ichigayaPower = 0;

    [Header("レベルアップ設定")]
    public int abilityThreshold = 5; // 3色すべてがこの値に達したらレベルアップ
    public int level = 1;

    [Header("HP（フィールドとバトルで共有。レベルアップ時はここを増やす想定）")]
    public int maxHP = 100;
    public int currentHP = 100;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddAbility(string windColor)
    {
        switch (windColor)
        {
            case "Blue":  mathPower++;    break;
            case "Green": sciencePower++; break;
            case "Red":   ichigayaPower++; break;
        }

        CheckLevelUp();
    }

    void CheckLevelUp()
    {
        if (mathPower >= abilityThreshold && sciencePower >= abilityThreshold && ichigayaPower >= abilityThreshold)
        {
            mathPower -= abilityThreshold;
            sciencePower -= abilityThreshold;
            ichigayaPower -= abilityThreshold;
            level++;
            // TODO: レベルアップ演出・通知UIは今後
            Debug.Log("レベルアップ！ Lv." + level);
        }
    }
}
