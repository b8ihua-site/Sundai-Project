using UnityEngine;

public class SimpleFPS : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public Transform playerBody;

    float xRotation = 0f;
    Rigidbody rb;

    void Start()
    {
        rb = playerBody.GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // マウス視点
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);

        // 入力
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move =
            (playerBody.right * x + playerBody.forward * z).normalized;

        Vector3 velocity = move * moveSpeed;
        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;
    }
}