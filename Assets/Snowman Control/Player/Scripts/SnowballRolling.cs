
using UnityEngine;

public class SnowballRolling : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private Transform playerRoot;

    [Header("Rolling Settings")]
    [SerializeField] private float ballRadius = 1f;
    [SerializeField] private float rollingSpeed = 1f;

    private Vector3 lastPosition;

    private void Start()
    {
        if (playerRoot == null)
        {
            Debug.LogError("Please assign Player Root!");
            enabled = false;
            return;
        }

        lastPosition = playerRoot.position;
    }

    private void LateUpdate()
    {
        // 計算 Player 這一幀移動了多少
        Vector3 movement = playerRoot.position - lastPosition;

        // 忽略上下移動，只計算地面方向
        movement.y = 0f;

        // 只要 Player 有移動，雪球就持續旋轉
        if (movement.sqrMagnitude > 0.000001f)
        {
            Vector3 axis = Vector3.Cross(
                Vector3.up,
                movement.normalized
            );

            float angle =
                movement.magnitude /
                Mathf.Max(ballRadius, 0.01f) *
                Mathf.Rad2Deg *
                rollingSpeed;

            transform.Rotate(axis, angle, Space.World);
        }

        lastPosition = playerRoot.position;
    }
}
