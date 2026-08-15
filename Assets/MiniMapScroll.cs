using UnityEngine;

public class MiniMapScroll : MonoBehaviour
{
    public Transform player;
    public RectTransform mapRect;

    public Vector2 worldMin;
    public Vector2 worldMax;

    void Update()
    {
        float x = Mathf.InverseLerp(
            worldMin.x,
            worldMax.x,
            player.position.x);

        float y = Mathf.InverseLerp(
            worldMin.y,
            worldMax.y,
            player.position.z);

        float mapX =
            -(x - 0.5f) * mapRect.rect.width;

        float mapY =
            -(y - 0.5f) * mapRect.rect.height;

        mapRect.anchoredPosition =
            new Vector2(mapX, mapY);
            
            mapRect.localEulerAngles =
    new Vector3(
        0,
        0,
        player.eulerAngles.y);
    }
}