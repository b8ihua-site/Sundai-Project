using UnityEngine;

public class CameraRangeRotator : MonoBehaviour
{
    public Transform player;

    void LateUpdate()
    {
        float playerAngle = player.eulerAngles.y;
        float camAngle = Camera.main.transform.eulerAngles.y;

        // キャラ向きを0基準としたカメラの相対角度
        float relativeAngle = camAngle - playerAngle + 180f;
transform.localRotation = Quaternion.Euler(0, 0, -relativeAngle);
    }
}