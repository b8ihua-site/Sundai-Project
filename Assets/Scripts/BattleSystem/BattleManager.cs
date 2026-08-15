using UnityEngine;
using System.Collections;
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

            BattleContext.HasData = false;
            BattleContext.WindColor = "";
        }

        battleUI.SetupCommandButtons(OnSelectFight, OnSelectItem, OnSelectRun);

        playerHP = playerMaxHP;
        enemyHP = enemyMaxHP;
        battleUI.UpdateHP(playerHP, playerMaxHP, enemyHP, enemyMaxHP);

        if (enemyObject != null) enemyObject.SetActive(false);

        if (dodgeArena != null)
        {
            dodgeArena.OnWrongHit += HandleWrongHit;
            dodgeArena.OnTurnEnd += HandleDodgeFinished;
        }

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
        StartCoroutine(ItemPlaceholder()); // 今は未実装
    }

    IEnumerator ItemPlaceholder()
    {
        battleUI.ShowCommandPanel(false);
        battleUI.ShowMessage("もちものは まだ ない！");
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

        yield return new WaitForSeconds(resultWait);
        ReturnToMainScene();
    }

    IEnumerator LoseSequence()
    {
        State = BattleState.Lose;
        battleUI.ShowResult(false);
        // LoseSequence 内
SEManager.Instance.Play("lose");
        yield return new WaitForSeconds(resultWait);
        ReturnToMainScene(); // とりあえずMainSceneへ（負け時の扱いは要相談）
    }

    void ReturnToMainScene()
    {
        // TODO: ここでプレイヤーHPなどを持ち帰る処理（HP連動のとき）
        SceneManager.LoadScene(mainSceneName);
    }

    void SetAnswerPhaseActive(bool active)
    {
        if (answerPhaseObjects == null) return;
        foreach (var go in answerPhaseObjects)
            if (go != null) go.SetActive(active);
    }
}
