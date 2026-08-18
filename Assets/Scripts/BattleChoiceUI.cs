using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
    public TextMeshProUGUI cancelLabel; // 「やめる」/「みのがす」など状況で切り替える

    [Header("メッセージ")]
    public TextMeshProUGUI messageText; // 「○○が現れた！」などを表示

    [Header("シーン")]
    public string battleSceneName = "BattleScene";

    public bool IsShowing { get; private set; }

    private InteractableObject currentEnemy;
    private Action onFightOverride; // 設定されていれば、こちらを優先して呼ぶ（知識の風など汎用用途）
    private Action onCancel;
    private const string DefaultCancelLabel = "やめる";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        panel.SetActive(false);

        fightButton.onClick.AddListener(OnFight);
        cancelButton.onClick.AddListener(OnCancel);
    }

    // NPCとの遭遇用（既存）
    public void Show(InteractableObject enemy, Action onCancelCallback)
    {
        currentEnemy = enemy;
        onFightOverride = null;
        onCancel = onCancelCallback;

        if (cancelLabel != null) cancelLabel.text = DefaultCancelLabel;
        messageText.text = $"{enemy.enemyName} に話しかけた。\nどうする？";
        ShowPanel();
    }

    // 汎用用途（知識の風など）。メッセージとコールバックを直接渡す
    public void Show(string message, string cancelLabelText, Action onFightCallback, Action onCancelCallback)
    {
        currentEnemy = null;
        onFightOverride = onFightCallback;
        onCancel = onCancelCallback;

        if (cancelLabel != null) cancelLabel.text = string.IsNullOrEmpty(cancelLabelText) ? DefaultCancelLabel : cancelLabelText;
        messageText.text = message;
        ShowPanel();
    }

    void ShowPanel()
    {
        panel.SetActive(true);
        IsShowing = true;

        if (EventSystem.current != null && fightButton != null)
            EventSystem.current.SetSelectedGameObject(fightButton.gameObject);
    }

    public void Hide()
    {
        panel.SetActive(false);
        IsShowing = false;
        currentEnemy = null;
    }

    void OnFight()
    {
        if (onFightOverride != null)
        {
            var callback = onFightOverride;
            Hide();
            callback.Invoke();
            return;
        }

        if (currentEnemy == null) return;

        BattleContext.HasData     = true;
        BattleContext.EnemyName   = currentEnemy.enemyName;
        BattleContext.EnemyMaxHP  = currentEnemy.enemyMaxHP;
        BattleContext.EnemyAttack = currentEnemy.enemyAttack;
        BattleContext.Subject     = currentEnemy.subject;
        BattleContext.WindColor   = "";
        BattleContext.MoneyMin    = currentEnemy.moneyRewardMin;
        BattleContext.MoneyMax    = currentEnemy.moneyRewardMax;
        BattleContext.EnemyLevel  = PlayerAbilities.Instance != null ? PlayerAbilities.Instance.level : 1;

        Hide();
        PlayerAbilities.CaptureFieldPosition();
        SceneManager.LoadScene(battleSceneName);
    }

    void OnCancel()
    {
        var callback = onCancel;
        Hide();
        callback?.Invoke();  // InteractSystem側でプレイヤーを解放する 等
    }
}
