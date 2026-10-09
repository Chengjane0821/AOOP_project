using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogue;

    public void Talk()
    {
        DialogueManager.Instance.StartDialogue(dialogue);
    }
}