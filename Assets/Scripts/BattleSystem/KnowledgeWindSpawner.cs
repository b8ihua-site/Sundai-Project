using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// マップ内のエリアに「知識の風」をランダムな色・位置で配置する
public class KnowledgeWindSpawner : MonoBehaviour
{
    [Header("スポーン数・範囲")]
    public int spawnCount = 8;
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(120f, 0f, 120f);
    public float minSpacing = 25f;       // 風同士の最低距離（近くにいても1〜2体になるように）
    public int maxPlacementTries = 30;   // 間隔条件を満たす位置を探す試行回数
    public float spawnHeight = 2f;      // 地面から浮かせる高さ
    public float groundRayHeight = 100f; // 地面検出レイの開始オフセット（areaCenter.yから上に）
    public LayerMask groundLayer = ~0;   // 地面判定に使うレイヤー
    public float maxHeightDeviation = 6f; // areaCenter.yからこれ以上離れた高さの地面（建物の屋上等）は無効にする

    private readonly List<Vector3> placedPositions = new List<Vector3>();

    [Header("科目マッピング（QuizDatabaseのsubjectNameと一致させる）")]
    public string mathSubject = "数学";      // 青
    public string scienceSubject = "理科";   // 緑
    public string ichigayaSubject = "市ヶ谷"; // 赤

    [Header("敵ステータス")]
    public int enemyMaxHP = 80;
    public int enemyAttack = 15;

    [Header("名前表示")]
    public TMP_FontAsset japaneseFont; // 日本語グリフを含むフォント（KnowledgeWindの名前ラベルに渡す）

    [Header("再スポーン（プレイヤー追従）")]
    public bool followPlayer = true;   // 再スポーン時にプレイヤーの現在地を中心にする
    public float respawnInterval = 60f; // この秒数ごとに一式作り直す（0以下で無効）
    public string playerTag = "Player";

    private Transform player;
    private readonly List<GameObject> activeWinds = new List<GameObject>();

    void Start()
    {
        if (followPlayer)
        {
            var p = GameObject.FindWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        SpawnAll();

        if (respawnInterval > 0f)
            StartCoroutine(RespawnLoop());
    }

    IEnumerator RespawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(respawnInterval);
            RespawnAll();
        }
    }

    void RespawnAll()
    {
        foreach (var w in activeWinds)
            if (w != null) Destroy(w);
        activeWinds.Clear();

        SpawnAll();
    }

    void SpawnAll()
    {
        if (player != null)
            areaCenter = player.position;

        placedPositions.Clear();

        for (int i = 0; i < spawnCount; i++)
            SpawnOne();
    }

    void SpawnOne()
    {
        Vector3 pos = FindSpawnPosition();
        placedPositions.Add(pos);

        var obj = new GameObject("KnowledgeWind");
        activeWinds.Add(obj);
        obj.transform.position = pos;
        var wind = obj.AddComponent<KnowledgeWind>();

        wind.color = (KnowledgeWind.WindColor)Random.Range(0, 3);
        wind.enemyMaxHP = enemyMaxHP;
        wind.enemyAttack = enemyAttack;
        wind.subjectName = wind.color switch
        {
            KnowledgeWind.WindColor.Blue => mathSubject,
            KnowledgeWind.WindColor.Green => scienceSubject,
            _ => ichigayaSubject,
        };
        wind.enemyName = wind.subjectName + "の風"; // 例: 数学の風／理科の風／市ヶ谷の風
        wind.japaneseFont = japaneseFont;
    }

    // 地形の起伏に埋まらないよう、上から地面へレイを飛ばして高さを合わせる。
    // ・建物の屋上など、プレイヤーの立っている高さと大きくズレた場所は候補から除外
    // ・他の風とminSpacing以上離れた位置になるまで（最大maxPlacementTries回まで）やり直す
    Vector3 FindSpawnPosition()
    {
        Vector3 fallback = new Vector3(areaCenter.x, areaCenter.y + spawnHeight, areaCenter.z);
        Vector3 bestPlausible = fallback;
        bool foundPlausible = false;

        for (int i = 0; i < maxPlacementTries; i++)
        {
            float x = areaCenter.x + Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f);
            float z = areaCenter.z + Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f);
            float rayStartY = areaCenter.y + groundRayHeight;

            if (!Physics.Raycast(new Vector3(x, rayStartY, z), Vector3.down, out RaycastHit hit, groundRayHeight * 2f, groundLayer))
                continue; // 地面が見つからない場所はスキップ

            Vector3 candidate = hit.point + Vector3.up * spawnHeight;

            // プレイヤーが立っている高さと大きく違う（屋上・地下等）場所は避ける
            if (Mathf.Abs(candidate.y - (areaCenter.y + spawnHeight)) > maxHeightDeviation)
                continue;

            bestPlausible = candidate;
            foundPlausible = true;

            if (IsFarEnough(candidate))
                return candidate;
        }

        return foundPlausible ? bestPlausible : fallback;
    }

    bool IsFarEnough(Vector3 candidate)
    {
        foreach (var p in placedPositions)
        {
            if (Vector3.Distance(p, candidate) < minSpacing)
                return false;
        }
        return true;
    }
}
