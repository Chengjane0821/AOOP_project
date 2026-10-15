
using UnityEngine;

public class SnowballProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float lifetime = 5f;

    private void Start()
    {
        // 五秒後自動刪除雪球
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 碰到物體後消失
        Destroy(gameObject);
    }
}
