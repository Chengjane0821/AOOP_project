using UnityEngine;

public class NPC : MonoBehaviour
{
    [SerializeField] private DialogueData dialogue;

    [Header("Quest")]
    [SerializeField] private bool startsQuestSequence;

    public void Talk()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogue);
        }

        if (startsQuestSequence &&
            QuestManager.Instance != null)
        {
            QuestManager.Instance.StartQuestSequence();
        }
    }
}