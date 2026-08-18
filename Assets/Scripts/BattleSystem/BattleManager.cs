using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    public enum BattleState { Intro, Command, Question, Dodging, PlayerAttack, Win, Lose }
    public BattleState State { get; private set; }

    [Header("プレイヤー設定")]
    public int playerMaxHP = 100;
    public int wrongHitDamage = 10; // 回避フェイズで被弾した時、1回あたりのダメージ

    [Header("敵設定")]
    public string enemyName = "なぞの敵";
    public int enemyMaxHP = 100;
    public int enemyAttack = 20;
    public float damageVariance = 0.05f;
    public int moneyMin = 10;
    public int moneyMax = 20;
    private int enemyLevel = 1;

    [Header("敵オブジェクト（背景の3D。Cube等。任意）")]
    public GameObject enemyObject;

    [Header("ポップアップ表示位置（スクリーン座標）")]
public Vector2 enemyPopupPos = new Vector2(900f, 400f);
public Vector2 playerPopupPos = new Vector2(400f, 350f);

    [Header("揺らす対象")]
    public Transform cameraTransform;   // BackgroundCamera を入れる
    // enemyObject は既存のものを使う

    [Header("演出の待ち時間（秒）")]
    public float introWait = 1.2f;      // 「○○が現れた！」を見せる時間
    public float questionWait = 1.0f;   // 問題だけ見せる時間
    public float afterAttackWait = 1.2f;
    public float resultWait = 1.5f;

    [Header("シーン")]
    public string mainSceneName = "MainScene";

    [Header("BGM")]
    public AudioClip battleBGM;

    [Header("参照")]
    public QuizDatabase quizDatabase;
    public BattleUI battleUI;
    public DodgeArena dodgeArena;

    [Header("回避フェイズで表示するUI（DodgeArenaのパネル等を入れる）")]
    public GameObject[] answerPhaseObjects;

    public string subject = "市ヶ谷";

    private int playerHP;
    private int enemyHP;
    private QuizQuestion currentQuestion;
    private string windColor = ""; // BattleContext由来。知識の風との戦闘でなければ空文字
    private bool playerDied = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // mainscene側（EnemyEncounter/BattleChoiceUI/KnowledgeWind）から渡された敵情報を反映
        if (BattleContext.HasData)
        {
            enemyName   = BattleContext.EnemyName;
            enemyMaxHP  = BattleContext.EnemyMaxHP;
            enemyAttack = BattleContext.EnemyAttack;
            subject     = BattleContext.Subject;
            windColor   = BattleContext.WindColor;
            moneyMin    = BattleContext.MoneyMin;
            moneyMax    = BattleContext.MoneyMax;
            enemyLevel  = BattleContext.EnemyLevel;

            BattleContext.HasData = false;
            BattleContext.WindColor = "";
        }

        battleUI.SetupCommandButtons(OnSelectFight, OnSelectItem, OnSelectRun);

        // maxHPはPlayerAbilities（フィールドと共通）が本体。レベルアップ等での増加もここに反映される
        if (PlayerAbilities.Instance != null)
        {
            playerMaxHP = PlayerAbilities.Instance.maxHP;
            playerHP = Mathf.Clamp(PlayerAbilities.Instance.currentHP, 0, playerMaxHP);
        }
        else
        {
            playerHP = playerMaxHP;
        }

        enemyHP = enemyMaxHP;
        battleUI.UpdateHP(playerHP, playerMaxHP, enemyHP, enemyMaxHP);

        if (enemyObject != null) enemyObject.SetActive(false);

        if (dodgeArena != null)
        {
            dodgeArena.OnWrongHit += HandleWrongHit;
            dodgeArena.OnTurnEnd += HandleDodgeFinished;
            dodgeArena.OnHeal += HandleHeal;
        }

        if (battleBGM != null) MusicPlayer.Instance?.PlayOverride(battleBGM);

        StartCoroutine(IntroSequence());
    }

    // ---------- 敵出現 ----------
    IEnumerator IntroSequence()
    {
        State = BattleState.Intro;
        battleUI.ShowCommandPanel(false);
        SetAnswerPhaseActive(false);

        if (enemyObject != null) enemyObject.SetActive(true); // TODO: 出現アニメ
        battleUI.ShowMessage($"{enemyName} が あらわれた！");
        // IntroSequence 内
SEManager.Instance.Play("encounter");

        yield return new WaitForSeconds(introWait);
        EnterCommand();
    }

    // ---------- コマンド選択 ----------
    void EnterCommand()
    {
        State = BattleState.Command;
        SetAnswerPhaseActive(false);
        battleUI.ClearDamage();
        battleUI.ShowMessage("どうする？");
        battleUI.ShowCommandPanel(true);
    }

    void OnSelectFight()
    {
        if (State != BattleState.Command) return;
        // OnSelectFight 内
SEManager.Instance.Play("select");
        battleUI.ShowCommandPanel(false);
        StartCoroutine(QuestionSequence());
    }

    void OnSelectItem()
    {
        if (State != BattleState.Command) return;
        StartCoroutine(ItemSequence());
    }

    IEnumerator ItemSequence()
    {
        battleUI.ShowCommandPanel(false);

        var pa = PlayerAbilities.Instance;
        var db = ItemDatabase.Instance;

        var usable = new List<PlayerAbilities.InventoryEntry>();
        if (pa != null && db != null)
        {
            foreach (var entry in pa.inventory)
            {
                var def = db.Find(entry.itemId);
                if (def != null && def.healAmount > 0 && entry.count > 0)
                    usable.Add(entry);
            }
        }

        if (usable.Count == 0)
        {
            battleUI.ShowMessage("つかえる どうぐが ない！");
            yield return new WaitForSeconds(1.0f);
            EnterCommand();
            yield break;
        }

        string list = "どうぐを えらんで数字キー（もどるのはBackspace）\n";
        for (int i = 0; i < usable.Count && i < 9; i++)
        {
            var def = db.Find(usable[i].itemId);
            list += $"{i + 1}: {def.displayName} HP+{def.healAmount} (のこり{usable[i].count})\n";
        }
        battleUI.ShowMessage(list);

        int chosen = -1;
        while (chosen < 0)
        {
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
            {
                EnterCommand();
                yield break;
            }

            for (int i = 0; i < usable.Count && i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                {
                    chosen = i;
                    break;
                }
            }

            yield return null;
        }

        var chosenEntry = usable[chosen];
        var chosenDef = db.Find(chosenEntry.itemId);
        pa.ConsumeItem(chosenEntry.itemId);

        playerHP = Mathf.Clamp(playerHP + chosenDef.healAmount, 0, playerMaxHP);
        battleUI.UpdateHP(playerHP, playerMaxHP, enemyHP, enemyMaxHP);
        battleUI.ShowMessage($"{chosenDef.displayName}を つかった！");

        if (SEManager.Instance != null) SEManager.Instance.Play("select");
        if (PopupSpawner.Instance != null)
            PopupSpawner.Instance.SpawnHeal(chosenDef.healAmount, playerPopupPos);

        yield return new WaitForSeconds(1.0f);
        EnterCommand();
    }

    void OnSelectRun()
    {
        if (State != BattleState.Command) return;
        StartCoroutine(RunSequence()); // 今は必ず逃げられる仮実装
    }

    IEnumerator RunSequence()
    {
        battleUI.ShowCommandPanel(false);
        battleUI.ShowMessage("うまく にげきれた！");
        yield return new WaitForSeconds(1.0f);
        ReturnToMainScene();
    }

    // ---------- たたかう（問題→回避フェイズ） ----------
    IEnumerator QuestionSequence()
    {
        State = BattleState.Question;
        currentQuestion = quizDatabase.GetRandomQuestion(subject);
        if (currentQuestion == null) { EnterCommand(); yield break; }

        battleUI.ShowQuestion(currentQuestion.question); // まず問題だけ
        // QuestionSequence 内
SEManager.Instance.Play("question");
        yield return new WaitForSeconds(questionWait);

        State = BattleState.Dodging;
        SetAnswerPhaseActive(true);
        playerDied = false;
        dodgeArena.StartRound(currentQuestion.answer);
    }

    // DodgeArenaから呼ばれる：回復弾（正解の文字の中に低確率で出る）に当たった
    void HandleHeal()
    {
        int healAmount = Mathf.Max(1, Mathf.RoundToInt(playerMaxHP * 0.1f));
        playerHP = Mathf.Min(playerMaxHP, playerHP + healAmount);
        battleUI.UpdateHP(playerHP, playerMaxHP, enemyHP, enemyMaxHP);

        if (PopupSpawner.Instance != null)
            PopupSpawner.Instance.SpawnHeal(healAmount, playerPopupPos);
    }

    // DodgeArenaから呼ばれる：順番違い／おとりに被弾した
    void HandleWrongHit()
    {
        if (State != BattleState.Dodging || playerDied) return;

        int damage = wrongHitDamage;
        playerHP = Mathf.Max(0, playerHP - damage);
        PopupSpawner.Instance.SpawnDamage(damage, playerPopupPos);
        battleUI.UpdateHP(playerHP, playerMaxHP, enemyHP, enemyMaxHP);

        SEManager.Instance.Play("enemyAttack");
        if (cameraTransform != null)
            Shaker.Instance.Shake(cameraTransform, 0.2f, 0.25f);

        if (playerHP <= 0)
        {
            playerDied = true;
            dodgeArena.EndRoundImmediately();
            SetAnswerPhaseActive(false);
            StartCoroutine(LoseSequence());
        }
    }

    // DodgeArenaから呼ばれる：全部の弾を捌き終わった（コンボ数, 文字数）
    void HandleDodgeFinished(int combo, int totalChars)
    {
        if (playerDied) return; // 既に敗北処理に入っている場合は何もしない
        SetAnswerPhaseActive(false);
        StartCoroutine(PlayerAttackSequence(combo, totalChars));
    }

    IEnumerator PlayerAttackSequence(int combo, int totalChars)
    {
        State = BattleState.PlayerAttack;

        float ratio = totalChars > 0 ? (float)combo / totalChars : 0f;
        float variance = Random.Range(-damageVariance, damageVariance);
        int damage = Mathf.Max(0, Mathf.RoundToInt(50 * (ratio + variance)));

enemyHP = Mathf.Max(0, enemyHP - damage);
PopupSpawner.Instance.SpawnEnemyDamage(damage, enemyPopupPos);  // ★追加
battleUI.UpdateHP(playerHP, playerMaxHP, enemyHP, enemyMaxHP);

// PlayerAttackSequence 内
SEManager.Instance.Play(combo > 0 ? "attack" : "miss");

if (enemyObject != null)
            Shaker.Instance.Shake(enemyObject.transform, 0.25f, 0.3f);   // ★敵を揺らす

        yield return new WaitForSeconds(afterAttackWait);

        if (enemyHP <= 0) { StartCoroutine(WinSequence()); yield break; }
        EnterCommand(); // 敵ターンは無し。コンボ分の攻撃を与えたら、また自分のコマンドへ
    }

    // ---------- 決着 ----------
    IEnumerator WinSequence()
    {
        State = BattleState.Win;
        battleUI.ShowResult(true);
        // WinSequence 内
SEManager.Instance.Play("win");

        if (!string.IsNullOrEmpty(windColor) && PlayerAbilities.Instance != null)
            PlayerAbilities.Instance.AddAbility(windColor);

        int reward = Random.Range(moneyMin, moneyMax + 1) * Mathf.Max(1, enemyLevel);
        if (PlayerAbilities.Instance != null) PlayerAbilities.Instance.AddMoney(reward);
        if (PopupSpawner.Instance != null) PopupSpawner.Instance.SpawnMoney(reward, enemyPopupPos);

        yield return new WaitForSeconds(resultWait);
        ReturnToMainScene();
    }

    IEnumerator LoseSequence()
    {
        State = BattleState.Lose;
        battleUI.ShowResult(false);
        // LoseSequence 内
SEManager.Instance.Play("lose");

        // 敗北してもゲームオーバーにはせず、HPを半分回復して復活させる
        playerHP = Mathf.Max(1, playerMaxHP / 2);
        battleUI.UpdateHP(playerHP, playerMaxHP, enemyHP, enemyMaxHP);

        yield return new WaitForSeconds(resultWait);
        ReturnToMainScene();
    }

    void ReturnToMainScene()
    {
        // maxHPは共通なので、バトルのHPをそのままフィールド側へ書き戻す
        if (PlayerAbilities.Instance != null)
            PlayerAbilities.Instance.currentHP = Mathf.Clamp(playerHP, 0, PlayerAbilities.Instance.maxHP);

        if (battleBGM != null) MusicPlayer.Instance?.StopOverride();
        PlayerAbilities.RestoreFieldPositionOnReturn();
        SceneManager.LoadScene(mainSceneName);
    }

    void SetAnswerPhaseActive(bool active)
    {
        if (answerPhaseObjects == null) return;
        foreach (var go in answerPhaseObjects)
            if (go != null) go.SetActive(active);
    }
}
