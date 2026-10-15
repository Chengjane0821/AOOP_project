using UnityEngine;

public class ClueItem : MonoBehaviour
{
    [SerializeField] private string clueName;

    private bool collected = false;

    public void Collect()
    {
        if (collected)
            return;

        if (QuestManager.Instance == null ||
            QuestManager.Instance.CurrentQuest == null)
        {
            Debug.LogWarning("No active quest.");
            return;
        }

        collected = true;

        QuestManager.Instance.CompleteObjective(0);

        Debug.Log("Collected clue: " + clueName);

        gameObject.SetActive(false);
    }
}