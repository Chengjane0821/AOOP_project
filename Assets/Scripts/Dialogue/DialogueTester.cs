using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTester : MonoBehaviour
{
    public DialogueData testDialogue;

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.tKey.wasPressedThisFrame)
        {
            DialogueManager.Instance.StartDialogue(testDialogue);
        }
    }
}