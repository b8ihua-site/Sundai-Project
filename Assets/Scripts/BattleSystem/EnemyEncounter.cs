using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyEncounter : MonoBehaviour
{
    [Header("設定")]
    public string subjectName = "市ヶ谷";
    public string enemyName = "なぞの敵";   // 表示名「○○が現れた！」
    public int enemyMaxHP = 100;
    public int enemyAttack = 20;
    public float encounterRange = 3f;

    [Header("シーン")]
    public string battleSceneName = "BattleScene";

    private bool battleStarted = false;
    private Transform player;

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (battleStarted || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= encounterRange)
        {
            battleStarted = true;

            BattleContext.HasData    = true;
            BattleContext.EnemyName  = enemyName;
            BattleContext.EnemyMaxHP = enemyMaxHP;
            BattleContext.EnemyAttack= enemyAttack;
            BattleContext.Subject    = subjectName;

            SceneManager.LoadScene(battleSceneName);
        }
    }
}