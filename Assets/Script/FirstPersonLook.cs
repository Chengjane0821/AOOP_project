using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField] private Transform playerBody;
    [SerializeField] private float mouseSensitivity = 0.08f;

    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private float pitch;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        pitch = 0f;

        transform.localPosition = new Vector3(0f, 3f, 0f);
        transform.localRotation = Quaternion.identity;
    }

    private void Update()
    {
        if (playerBody == null || Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        // 上下旋轉 Camera
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localRotation =
            Quaternion.Euler(pitch, 0f, 0f);

        // 左右旋轉 Player
        playerBody.Rotate(Vector3.up * mouseX);
    }
}