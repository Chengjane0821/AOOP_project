
using UnityEngine;
using UnityEngine.InputSystem;

public class SnowballThrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject snowballPrefab;
    [SerializeField] private Transform throwPoint;

    [Header("Throw Settings")]
    [SerializeField] private float throwForce = 12f;
    [SerializeField] private float upwardForce = 2f;
    [SerializeField] private float cooldown = 0.4f;

    private float nextThrowTime;

    private void Update()
    {
        // 按 I 鍵丟雪球
        if (Keyboard.current != null &&
            Keyboard.current.iKey.wasPressedThisFrame)
        {
            ThrowSnowball();
        }
    }

    private void ThrowSnowball()
    {
        if (Time.time < nextThrowTime)
            return;

        if (snowballPrefab == null || throwPoint == null)
        {
            Debug.LogWarning(
                "Snowball Prefab or Throw Point is missing!"
            );
            return;
        }

        GameObject snowball = Instantiate(
            snowballPrefab,
            throwPoint.position,
            Quaternion.identity
        );

        Rigidbody rb = snowball.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Snowball needs a Rigidbody!");
            Destroy(snowball);
            return;
        }

        Vector3 throwDirection =
            transform.forward * throwForce +
            Vector3.up * upwardForce;

        rb.AddForce(throwDirection, ForceMode.Impulse);

        nextThrowTime = Time.time + cooldown;
    }
}
