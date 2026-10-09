using UnityEngine;

public class NPC : MonoBehaviour
{
    [SerializeField]
    private DialogueData dialogue;

    [SerializeField]
    private bool startsMainQuest;

    public void Talk()
    {
        DialogueManager.Instance.StartDialogue(dialogue);

        if (startsMainQuest &&
            QuestManager.Instance != null)
        {
            QuestManager.Instance.StartMainQuest();
        }
    }
}