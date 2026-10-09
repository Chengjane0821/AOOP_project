using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyTouchDamage : MonoBehaviour
{
    private MiniGameManager manager;

    private void Start()
    {
        manager = FindFirstObjectByType<MiniGameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<MiniGamePlayerController>() == null)
            return;

        if (manager != null)
        {
            manager.Respawn();
        }
    }
}