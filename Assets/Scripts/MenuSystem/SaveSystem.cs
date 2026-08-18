using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int level;
    public int mathPower, sciencePower, ichigayaPower;
    public int maxHP, currentHP;
    public int money;
    public int mathDefeats, scienceDefeats, ichigayaDefeats;
    public List<string> itemIds = new List<string>();
    public List<int> itemCounts = new List<int>();
    public bool hasPosition;
    public float posX, posY, posZ;
    public float bodyYaw;   // プレイヤー本体の向き
    public float camYaw, camPitch; // カメラ（視点）の向き
}

// PlayerAbilitiesの中身とプレイヤーの座標・向きをPlayerPrefsへ保存/復元する
public static class SaveSystem
{
    const string SaveKey = "SundaiSaveData";

    public static bool HasSaveData() => PlayerPrefs.HasKey(SaveKey);

    public static void Save(PlayerAbilities pa, Transform playerTransform)
    {
        if (pa == null) return;

        var data = new SaveData
        {
            level = pa.level,
            mathPower = pa.mathPower,
            sciencePower = pa.sciencePower,
            ichigayaPower = pa.ichigayaPower,
            maxHP = pa.maxHP,
            currentHP = pa.currentHP,
            money = pa.money,
            mathDefeats = pa.mathDefeats,
            scienceDefeats = pa.scienceDefeats,
            ichigayaDefeats = pa.ichigayaDefeats,
        };

        foreach (var entry in pa.inventory)
        {
            data.itemIds.Add(entry.itemId);
            data.itemCounts.Add(entry.count);
        }

        if (playerTransform != null)
        {
            data.hasPosition = true;
            data.posX = playerTransform.position.x;
            data.posY = playerTransform.position.y;
            data.posZ = playerTransform.position.z;
            data.bodyYaw = playerTransform.eulerAngles.y;

            var controller = playerTransform.GetComponentInChildren<StarterAssets.ThirdPersonController>();
            if (controller != null && controller.CinemachineCameraTarget != null)
            {
                var camAngles = controller.CinemachineCameraTarget.transform.eulerAngles;
                data.camYaw = camAngles.y;
                // eulerAnglesは0〜360で返るので、上下を向く角度(-30〜70想定)に戻すため符号付きに変換する
                float pitch = camAngles.x;
                if (pitch > 180f) pitch -= 360f;
                data.camPitch = pitch;
            }
        }

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    // PlayerAbilities.Awake()から呼ばれる。セーブデータがあれば読み込んで反映する
    public static void LoadInto(PlayerAbilities pa)
    {
        if (!HasSaveData()) return;

        var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
        if (data == null) return;

        pa.level = data.level;
        pa.mathPower = data.mathPower;
        pa.sciencePower = data.sciencePower;
        pa.ichigayaPower = data.ichigayaPower;
        pa.maxHP = data.maxHP;
        pa.currentHP = data.currentHP;
        pa.money = data.money;
        pa.mathDefeats = data.mathDefeats;
        pa.scienceDefeats = data.scienceDefeats;
        pa.ichigayaDefeats = data.ichigayaDefeats;

        pa.inventory.Clear();
        for (int i = 0; i < data.itemIds.Count; i++)
            pa.inventory.Add(new PlayerAbilities.InventoryEntry { itemId = data.itemIds[i], count = data.itemCounts[i] });

        if (data.hasPosition)
        {
            PlayerAbilities.PendingPositionLoad = true;
            PlayerAbilities.LoadedPosition = new Vector3(data.posX, data.posY, data.posZ);
            PlayerAbilities.LoadedBodyYaw = data.bodyYaw;
            PlayerAbilities.LoadedCamYaw = data.camYaw;
            PlayerAbilities.LoadedCamPitch = data.camPitch;
        }
    }
}
