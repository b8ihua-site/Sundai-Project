using UnityEngine;
using System.Collections;

public class Shaker : MonoBehaviour
{
    public static Shaker Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // 指定したTransformを揺らす
    public void Shake(Transform target, float duration = 0.25f, float strength = 0.3f)
    {
        if (target != null)
            StartCoroutine(ShakeRoutine(target, duration, strength));
    }

    IEnumerator ShakeRoutine(Transform target, float duration, float strength)
    {
        Vector3 origin = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damper = 1f - (elapsed / duration);      // だんだん弱まる
            Vector3 offset = Random.insideUnitSphere * strength * damper;
            offset.z = 0f;                                  // 奥行きは揺らさない
            target.localPosition = origin + offset;
            yield return null;
        }

        target.localPosition = origin; // 元に戻す
    }
}