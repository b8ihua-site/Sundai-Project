using UnityEngine;
using TMPro;

public class MinimapIcon : MonoBehaviour
{
    public Transform target;
    public Transform player;
    public RectTransform mapRect;
    public float mapScale = 48.62f;

    RectTransform rt;
    TMP_Text[] labels; // 配列に変更

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        labels = GetComponentsInChildren<TMP_Text>(); // 複数取得
    }

    void LateUpdate()
    {
        Vector3 offset = target.position - player.position;

        float angle = player.eulerAngles.y * Mathf.Deg2Rad;
        float rotX = offset.x * Mathf.Cos(angle) - offset.z * Mathf.Sin(angle);
        float rotZ = offset.x * Mathf.Sin(angle) + offset.z * Mathf.Cos(angle);

        float mapSize = mapRect.sizeDelta.x;
        float x = (rotX / mapScale) * (mapSize * 0.5f);
        float y = (rotZ / mapScale) * (mapSize * 0.5f);

        rt.anchoredPosition = new Vector2(x, y);
        rt.rotation = Quaternion.identity;

        float radius = mapSize * 0.5f;
        bool inRange = new Vector2(x, y).magnitude < radius;

        // 全部のTMPに適用
        foreach (var label in labels)
            label.enabled = inRange;
    }
}