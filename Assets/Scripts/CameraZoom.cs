using UnityEngine;
using Unity.Cinemachine;

public class CameraZoom : MonoBehaviour
{
    public GameObject cmObject;
    public float zoomSpeed = 2f;
    public float minDistance = 1f;
    public float maxDistance = 20f;

    private Cinemachine3rdPersonFollow thirdPersonFollow;

    void Start()
    {
        thirdPersonFollow = cmObject.GetComponent<Cinemachine3rdPersonFollow>();
        Debug.Log("ThirdPersonFollow: " + thirdPersonFollow);
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f || thirdPersonFollow == null) return;

        thirdPersonFollow.CameraDistance =
            Mathf.Clamp(thirdPersonFollow.CameraDistance - scroll * zoomSpeed, minDistance, maxDistance);
    }
}