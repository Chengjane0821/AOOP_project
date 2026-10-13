
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BedMiniGameEntrance : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform bed;

    [Header("Settings")]
    [SerializeField] private float interactDistance = 2.5f;
    [SerializeField] private string miniGameScene = "BedMiniGame";

    private void Update()
    {
        if (bed == null || Keyboard.current == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            bed.position
        );

        if (distance > interactDistance)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (Application.CanStreamedLevelBeLoaded(miniGameScene))
            {
                SceneManager.LoadScene(miniGameScene);
            }
            else
            {
                Debug.LogError(
                    "Scene not found: " + miniGameScene
                );
            }
        }
    }
}
