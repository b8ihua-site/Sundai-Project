using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BattleUI : MonoBehaviour
{
    [Header("HPBar")]
    public HPBar playerHPBar;
    public HPBar enemyHPBar;

    [Header("バトル情報")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI resultText;

    [Header("コマンド")]
    public GameObject commandPanel;
    public Button fightButton;
    public Button itemButton;
    public Button runButton;

    public void SetupCommandButtons(Action onFight, Action onItem, Action onRun)
    {
        if (fightButton  != null) { fightButton.onClick.RemoveAllListeners();  fightButton.onClick.AddListener(() => onFight()); }
        if (itemButton   != null) { itemButton.onClick.RemoveAllListeners();   itemButton.onClick.AddListener(() => onItem());  }
        if (runButton    != null) { runButton.onClick.RemoveAllListeners();    runButton.onClick.AddListener(() => onRun());   }
    }

    public void ShowCommandPanel(bool show)
    {
        if (commandPanel != null) commandPanel.SetActive(show);
    }

    public void ShowMessage(string msg)
    {
        if (questionText != null) questionText.text = msg;
        if (resultText   != null) resultText.text = "";
    }

    public void ClearDamage()
    {
        if (damageText != null) damageText.text = "";
    }

    // HPBar に値を渡す（BattleManager から呼ばれる）
    public void UpdateHP(int playerHP, int playerMax, int enemyHP, int enemyMax)
    {
        if (playerHPBar != null)
        {
            playerHPBar.maxHP     = playerMax;
            playerHPBar.currentHP = playerHP;
        }
        if (enemyHPBar != null)
        {
            enemyHPBar.maxHP     = enemyMax;
            enemyHPBar.currentHP = enemyHP;
        }
    }

    public void ShowQuestion(string q)
    {
        if (questionText != null) questionText.text = q;
        if (damageText   != null) damageText.text = "";
        if (resultText   != null) resultText.text = "";
    }

    public void ShowDamage(int damage)      { if (damageText != null) damageText.text = $"{damage} ダメージ！"; }
    public void ShowEnemyAttack(int damage) { if (damageText != null) damageText.text = $"敵の攻撃！ {damage} ダメージ"; }
    public void ShowResult(bool win)        { if (resultText != null) resultText.text = win ? "勝利！" : "敗北..."; }
}