using UnityEngine;

public class DamageCube : MonoBehaviour
{
    public HPBar hpBar;
    public float damage = 10f;
    public float interval = 1f;

    float timer;

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            timer += Time.deltaTime;
            if (timer >= interval)
            {
                hpBar.TakeDamage(damage);
                timer = 0f;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            timer = 0f;
    }
}