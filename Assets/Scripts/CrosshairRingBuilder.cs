using UnityEngine;
using UnityEngine.UI;

// カメラ構え中の照準(クロスヘア)を、塗りつぶしではなく白い円のリング(輪っか)にする
[RequireComponent(typeof(Image))]
public class CrosshairRingBuilder : MonoBehaviour
{
    public float ringThickness = 0.15f; // リングの太さ（0〜0.5、大きいほど太い）

    void Start()
    {
        var image = GetComponent<Image>();
        image.sprite = CreateRingSprite();
        image.type = Image.Type.Simple;
    }

    Sprite CreateRingSprite(int size = 64)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float outerRadius = size * 0.5f;
        float innerRadius = outerRadius * (1f - ringThickness);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);

                float alpha;
                if (dist > outerRadius) alpha = 0f;
                else if (dist < innerRadius) alpha = 0f;
                else
                {
                    // 内外のエッジをソフトにフェードさせる
                    float edge = Mathf.Min(dist - innerRadius, outerRadius - dist);
                    alpha = Mathf.Clamp01(edge / 2f);
                }

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
