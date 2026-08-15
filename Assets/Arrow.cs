using UnityEngine;

public class Arrow : MonoBehaviour
{
    public Transform player;
    public Transform directionArrow;
    public Transform cameraRange;

    void LateUpdate()
    {
        float playerAngle = player.eulerAngles.y;
        directionArrow.localRotation = 
            Quaternion.Euler(0, 0, -playerAngle);

        float camAngle = Camera.main.transform.eulerAngles.y;
        cameraRange.localRotation = 
            Quaternion.Euler(0, 0, -camAngle);
    }
}