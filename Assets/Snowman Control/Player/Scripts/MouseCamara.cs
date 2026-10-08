using UnityEngine;
using UnityEngine.InputSystem;

public class MouseCameraController : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    [SerializeField]
    private float mouseSensitivity = 0.15f;

    [SerializeField]
    private float distance = 5f;

    [SerializeField]
    private float height = 2f;

    [SerializeField]
    private float minPitch = -20f;

    [SerializeField]
    private float maxPitch = 60f;

    private float yaw;
    private float pitch = 15f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null || Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        yaw += mouseDelta.x * mouseSensitivity;
        pitch -= mouseDelta.y * mouseSensitivity;

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );

        Quaternion rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );

        Vector3 offset =
            rotation *
            new Vector3(
                0f,
                0f,
                -distance
            );

        transform.position =
            target.position +
            Vector3.up * height +
            offset;

        transform.LookAt(
            target.position +
            Vector3.up * height
        );
    }
}