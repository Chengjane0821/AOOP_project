using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CoinCollectible : MonoBehaviour
{
    private MiniGameManager manager;
    private bool collected;

    private void Start()
    {
        manager = FindFirstObjectByType<MiniGameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        if (other.GetComponentInParent<MiniGamePlayerController>() == null)
            return;

        if (manager == null) return;

        collected = true;
        manager.CollectCoin();
        gameObject.SetActive(false);
    }
}