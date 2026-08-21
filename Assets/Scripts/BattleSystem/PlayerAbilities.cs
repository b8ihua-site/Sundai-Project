using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

// 知識の風を倒すごとに色ごとの能力値を蓄積し、すべてが一定値に達したらレベルアップする
// フィールド・バトル・メニューで共有する永続プレイヤーデータ（HP・お金・アイテム）もここにまとめる
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
    public int hpPerLevel = 10; // レベルアップ時にmaxHPへ加算する量

    [Header("HP（フィールドとバトルで共有）")]
    public int maxHP = 100;
    public int currentHP = 100;

    [Header("お金")]
    public int money = 1000; // 初期所持金

    [Header("図鑑（倒した知識の風の数）")]
    public int mathDefeats = 0;
    public int scienceDefeats = 0;
    public int ichigayaDefeats = 0;

    [System.Serializable]
    public class InventoryEntry
    {
        public string itemId;
        public int count;
    }

    [Header("アイテム（ショップは未実装。今は初期所持のみ）")]
    public List<InventoryEntry> inventory = new List<InventoryEntry>
    {
        new InventoryEntry { itemId = "heal_small", count = 3 },
    };

    // ロード直後、フィールドに配置すべき保存位置・向き（あれば）
    public static bool PendingPositionLoad = false;
    public static Vector3 LoadedPosition;
    public static float LoadedBodyYaw;
    public static float LoadedCamYaw;
    public static float LoadedCamPitch;

    // 戦闘に入る直前のフィールド上の位置・向き（戦闘から戻ったときに復元する用）
    static bool hasFieldPosition;
    static Vector3 fieldPosition;
    static float fieldBodyYaw;
    static float fieldCamYaw;
    static float fieldCamPitch;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SaveSystem.LoadInto(this);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

#if UNITY_EDITOR
    // デバッグ用：F9で3科目それぞれ知識の風を1体倒した判定にする。動作確認が終わったら削除すること
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            AddAbility("Blue");
            AddAbility("Green");
            AddAbility("Red");
            Debug.Log("[Debug] 3科目をそれぞれ1体分加算しました");
        }
    }
#endif

    // PlayerAbilitiesはDontDestroyOnLoadでStart()が最初の1回しか呼ばれないため、
    // 戦闘から戻るたびの位置復元にはsceneLoadedを使う
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!PendingPositionLoad) return;
        PendingPositionLoad = false;
        StartCoroutine(ApplyLoadedTransform());
    }

    // 戦闘シーンへ移る直前に呼ぶ。現在のプレイヤー位置・向きを控えておく
    public static void CaptureFieldPosition()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) { hasFieldPosition = false; return; }

        hasFieldPosition = true;
        fieldPosition = player.transform.position;
        fieldBodyYaw = player.transform.eulerAngles.y;

        var controller = player.GetComponentInChildren<StarterAssets.ThirdPersonController>();
        if (controller != null && controller.CinemachineCameraTarget != null)
        {
            var camAngles = controller.CinemachineCameraTarget.transform.eulerAngles;
            fieldCamYaw = camAngles.y;
            // eulerAnglesは0〜360で返るので、上下を向く角度に戻すため符号付きに変換する
            float pitch = camAngles.x;
            if (pitch > 180f) pitch -= 360f;
            fieldCamPitch = pitch;
        }
    }

    // 戦闘から戻る直前に呼ぶ。CaptureFieldPositionで控えた位置をmainscene読み込み後に復元する
    public static void RestoreFieldPositionOnReturn()
    {
        if (!hasFieldPosition) return;

        PendingPositionLoad = true;
        LoadedPosition = fieldPosition;
        LoadedBodyYaw = fieldBodyYaw;
        LoadedCamYaw = fieldCamYaw;
        LoadedCamPitch = fieldCamPitch;
    }

    // ThirdPersonController.Start()がカメラのyawを自分の初期値で上書きしてしまうため、
    // 全スクリプトのStart()が終わった後（1フレーム後）に反映する
    IEnumerator ApplyLoadedTransform()
    {
        yield return null;

        var player = GameObject.FindWithTag("Player");
        if (player == null) yield break;

        player.transform.position = LoadedPosition;
        player.transform.rotation = Quaternion.Euler(0f, LoadedBodyYaw, 0f);

        var controller = player.GetComponentInChildren<StarterAssets.ThirdPersonController>();
        if (controller != null) controller.SetLookAngles(LoadedCamYaw, LoadedCamPitch);
    }

    public void AddAbility(string windColor)
    {
        switch (windColor)
        {
            case "Blue":  mathPower++;     mathDefeats++;     break;
            case "Green": sciencePower++;  scienceDefeats++;  break;
            case "Red":   ichigayaPower++; ichigayaDefeats++; break;
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

            int previousLevel = level;
            level++;

            maxHP += hpPerLevel;
            currentHP = Mathf.Min(maxHP, currentHP + hpPerLevel);

            Debug.Log("レベルアップ！ Lv." + level);
            MenuController.Instance?.QueueLevelUp(previousLevel, level);
        }
    }

    public void AddMoney(int amount)
    {
        money = Mathf.Max(0, money + amount);
    }

    public bool SpendMoney(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        return true;
    }

    public void AddItem(string itemId, int count = 1)
    {
        var entry = inventory.Find(e => e.itemId == itemId);
        if (entry != null) entry.count += count;
        else inventory.Add(new InventoryEntry { itemId = itemId, count = count });
    }

    public int GetItemCount(string itemId)
    {
        var entry = inventory.Find(e => e.itemId == itemId);
        return entry != null ? entry.count : 0;
    }

    // アイテムを1個消費する。持っていなければfalse
    public bool ConsumeItem(string itemId)
    {
        var entry = inventory.Find(e => e.itemId == itemId);
        if (entry == null || entry.count <= 0) return false;

        entry.count--;
        if (entry.count <= 0) inventory.Remove(entry);
        return true;
    }
}
