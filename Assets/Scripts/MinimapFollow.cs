using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform player;
    public float fixedY = -450f;

    void LateUpdate()
    {
        transform.position = new Vector3(
            player.position.x,
            fixedY,
            player.position.z
        );

        transform.rotation = Quaternion.Euler(
            90f,
            player.eulerAngles.y,
            0f
        );
    }
}