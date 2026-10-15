
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteraction : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField]
    private string targetSceneName = "House000";

    private bool isLoading = false;

    public string InteractionPrompt => "Press O to Enter";

    public void Interact()
    {
        if (isLoading)
            return;

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError(
                "Scene not found in Build Profiles: " +
                targetSceneName
            );
            return;
        }

        isLoading = true;

        Debug.Log("Entering " + targetSceneName);

        SceneManager.LoadScene(targetSceneName);
    }
}
