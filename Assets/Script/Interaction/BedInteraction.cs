
using UnityEngine;
using UnityEngine.SceneManagement;

public class BedInteraction : MonoBehaviour, IInteractable
{
    [Header("Bed Settings")]
    [SerializeField]
    private string targetSceneName = "BedMiniGame";

    public string InteractionPrompt => "Press O to Sleep";

    private bool isLoading = false;

    public void Interact()
    {
        if (isLoading)
            return;

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError(
                "Scene not found: " + targetSceneName
            );
            return;
        }

        isLoading = true;

        Debug.Log("Entering BedMiniGame...");
        SceneManager.LoadScene(targetSceneName);
    }
}
