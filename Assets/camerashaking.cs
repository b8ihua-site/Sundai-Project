using UnityEngine;

public class HandheldShake : MonoBehaviour
{
    [Header("位置のブレ")]
    public float posAmplitude = 0.02f;   // 揺れ幅（大きいほど派手）
    public float posFrequency = 0.8f;    // 揺れの速さ

    [Header("回転のブレ")]
    public float rotAmplitude = 0.15f;   // 傾きの揺れ幅（度）
    public float rotFrequency = 0.6f;

    Vector3 basePos;
    Quaternion baseRot;

    // 各軸ごとにノイズの開始位置をずらす（同じ動きにならないように）
    float seedX, seedY, seedZ;

    void Start()
    {
        basePos = transform.localPosition;
        baseRot = transform.localRotation;

        seedX = Random.value * 100f;
        seedY = Random.value * 100f;
        seedZ = Random.value * 100f;
    }

    void LateUpdate()
    {
        float tp = Time.time * posFrequency;
        float tr = Time.time * rotFrequency;

        // PerlinNoiseは0〜1を返すので -0.5 して中心を0にする
        Vector3 offset = new Vector3(
            (Mathf.PerlinNoise(seedX, tp) - 0.5f),
            (Mathf.PerlinNoise(seedY, tp) - 0.5f),
            (Mathf.PerlinNoise(seedZ, tp) - 0.5f) * 0.5f  // 前後は控えめ
        ) * posAmplitude * 2f;

        Vector3 rotOffset = new Vector3(
            (Mathf.PerlinNoise(seedX + 10f, tr) - 0.5f),
            (Mathf.PerlinNoise(seedY + 10f, tr) - 0.5f),
            (Mathf.PerlinNoise(seedZ + 10f, tr) - 0.5f)
        ) * rotAmplitude * 2f;

        transform.localPosition = basePos + offset;
        transform.localRotation = baseRot * Quaternion.Euler(rotOffset);
    }
}