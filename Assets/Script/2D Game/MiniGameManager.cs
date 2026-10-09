using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Rigidbody2D player;

    [Header("Coin System")]
    [SerializeField] private int requiredCoins = 6;
    [SerializeField] private GameObject chestObject;

    [Header("Goal")]
    [SerializeField] private string returnScene = "House01";

    private int collectedCoins;
    private bool memoryCollected;
    private bool completed;

    private Vector2 checkpointPosition;
    private float startTime;
    private int deathCount;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("MiniGameManager: Player not assigned!");
            enabled = false;
            return;
        }

        checkpointPosition = player.position;
        startTime = Time.time;

        if (chestObject != null)
        {
            chestObject.SetActive(false);
        }
    }

    public void CollectCoin()
    {
        if (completed) return;

        collectedCoins++;
        Debug.Log("Coins: " + collectedCoins + "/" + requiredCoins);

        if (collectedCoins >= requiredCoins && chestObject != null)
        {
            chestObject.SetActive(true);
            Debug.Log("Chest appeared!");
        }
    }

    public int GetCollectedCoins()
    {
        return collectedCoins;
    }

    public int GetRequiredCoins()
    {
        return requiredCoins;
    }

    public void CollectMemoryFragment()
    {
        if (completed) return;

        memoryCollected = true;
        Debug.Log("Memory Fragment collected!");
    }

    public bool HasMemoryFragment()
    {
        return memoryCollected;
    }

    public void SetCheckpoint(Vector2 position)
    {
        checkpointPosition = position;
        Debug.Log("Checkpoint saved!");
    }

    public void Respawn()
    {
        if (completed || player == null) return;

        deathCount++;
        player.linearVelocity = Vector2.zero;
        player.angularVelocity = 0f;
        player.position = checkpointPosition;

        Debug.Log("Respawn! Deaths: " + deathCount);
    }

    public void TryFinish()
    {
        if (completed) return;

        if (!memoryCollected)
        {
            Debug.Log("You must collect the Forgotten Memory Fragment first!");
            return;
        }

        completed = true;
        Debug.Log("Mini-game completed!");
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 260, 110), "The Forgotten Dream");

        GUI.Label(
            new Rect(20, 35, 220, 25),
            "Coins: " + collectedCoins + "/" + requiredCoins
        );

        GUI.Label(
            new Rect(20, 60, 220, 25),
            "Memory: " + (memoryCollected ? "Collected" : "Not Yet")
        );

        GUI.Label(
            new Rect(20, 85, 220, 25),
            "Deaths: " + deathCount +
            "   Time: " + Mathf.FloorToInt(Time.time - startTime) + "s"
        );

        if (completed)
        {
            float x = Screen.width / 2f - 120f;
            float y = Screen.height / 2f - 70f;

            GUI.Box(new Rect(x, y, 240, 140), "Dream Completed!");

            if (GUI.Button(
                new Rect(x + 25, y + 70, 190, 40),
                "Return to House01"
            ))
            {
                SceneManager.LoadScene(returnScene);
            }
        }
    }
}