using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField]
    private Transform target;

    [SerializeField]
    private float mouseSensitivity = 0.15f;

    [SerializeField]
    private float height = 1.6f;

    [SerializeField]
    private float minPitch = -80f;

    [SerializeField]
    private float maxPitch = 80f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null)
            yaw = target.eulerAngles.y;
    }

    private void Update()
    {
        if (target == null || Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        yaw += mouseDelta.x * mouseSensitivity;
        pitch -= mouseDelta.y * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        transform.position =
            target.position + Vector3.up * height;

        transform.rotation =
            Quaternion.Euler(pitch, yaw, 0f);
    }
}