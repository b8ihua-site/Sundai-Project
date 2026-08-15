using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [TextArea(3, 5)]
    public string[] pages;

    [Header("会話設定")]
    public bool isNPC = false;
    public string speakerName = "";
    public AudioClip[] voiceClips;

    [Header("エンカウント設定（敵Cube用）")]
    public bool isEnemy = false;        // これをONにすると戦闘選択肢が出る
    public string enemyName = "なぞの敵";
    public int enemyMaxHP = 100;
    public int enemyAttack = 20;
    public string subject = "市ヶ谷";
}