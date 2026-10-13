using UnityEngine;

public class ClueItem : MonoBehaviour
{
    [Header("Clue Settings")]
    [SerializeField] private int objectiveIndex;
    [SerializeField] private string clueName;

    private bool collected = false;

    public void Collect()
    {
        if (collected)
            return;

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("QuestManager not found.");
            return;
        }

        if (QuestManager.Instance.MainQuest == null)
        {
            Debug.LogWarning("Main quest has not started.");
            return;
        }

        collected = true;

        QuestManager.Instance.CompleteObjective(objectiveIndex);

        Debug.Log("Collected clue: " + clueName);

        gameObject.SetActive(false);
    }
}