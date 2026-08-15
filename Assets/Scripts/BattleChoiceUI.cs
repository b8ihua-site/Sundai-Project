using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class BattleChoiceUI : MonoBehaviour
{
    public static BattleChoiceUI Instance;

    [Header("パネル")]
    public GameObject panel;            // 選択肢全体の親

    [Header("ボタン")]
    public Button fightButton;
    public Button cancelButton;

    [Header("メッセージ")]
    public TextMeshProUGUI messageText; // 「○○が現れた！」などを表示

    [Header("シーン")]
    public string battleSceneName = "BattleScene";

    private InteractableObject currentEnemy;
    private Action onCancel;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        panel.SetActive(false);

        fightButton.onClick.AddListener(OnFight);
        cancelButton.onClick.AddListener(OnCancel);
    }

    public void Show(InteractableObject enemy, Action onCancelCallback)
    {
        currentEnemy = enemy;
        onCancel = onCancelCallback;

        messageText.text = $"{enemy.enemyName} に話しかけた。\nどうする？";
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
        currentEnemy = null;
    }

    void OnFight()
    {
        if (currentEnemy == null) return;

        BattleContext.HasData     = true;
        BattleContext.EnemyName   = currentEnemy.enemyName;
        BattleContext.EnemyMaxHP  = currentEnemy.enemyMaxHP;
        BattleContext.EnemyAttack = currentEnemy.enemyAttack;
        BattleContext.Subject     = currentEnemy.subject;
        BattleContext.WindColor   = "";

        Hide();
        SceneManager.LoadScene(battleSceneName);
    }

    void OnCancel()
    {
        Hide();
        onCancel?.Invoke();  // InteractSystem側でプレイヤーを解放する
    }
}