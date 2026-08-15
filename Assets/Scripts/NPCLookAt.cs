using UnityEngine;

public class NPCLookAt : MonoBehaviour
{
    public float lookRange = 5f;        // この距離以内でこっちを向く
    public float rotateSpeed = 3f;      // 振り向く速さ
    public Transform npcBody;          // 向きを変えるTransform（NPC本体）

    private Transform player;

    void Start()
    {
        // Playerタグからプレイヤーを自動取得
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= lookRange)
        {
            // Y軸のみ回転（上下は向かない）
            Vector3 direction = player.position - npcBody.position;
            direction.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            npcBody.rotation = Quaternion.Slerp(npcBody.rotation, targetRotation, Time.deltaTime * rotateSpeed);
        }
    }
}