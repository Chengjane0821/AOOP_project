using UnityEngine;

public class PersistentGameSystems : MonoBehaviour
{
    private static PersistentGameSystems instance;

    private void Awake()
    {

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);
    }
}