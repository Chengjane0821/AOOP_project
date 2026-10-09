using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    private DialogueData currentDialogue;
    private int currentSentenceIndex = 0;

    public bool IsDialogueActive { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        dialoguePanel.SetActive(false);
    }

    public void StartDialogue(DialogueData dialogue)
    {
        if (dialogue == null ||
            dialogue.sentences == null ||
            dialogue.sentences.Length == 0)
        {
            return;
        }

        currentDialogue = dialogue;
        currentSentenceIndex = 0;
        IsDialogueActive = true;

        dialoguePanel.SetActive(true);

        speakerNameText.text = currentDialogue.speakerName;

        ShowSentence();
    }

    public void NextSentence()
    {
        if (!IsDialogueActive)
            return;

        currentSentenceIndex++;

        if (currentSentenceIndex >= currentDialogue.sentences.Length)
        {
            EndDialogue();
            return;
        }

        ShowSentence();
    }

    private void ShowSentence()
    {
        dialogueText.text =
            currentDialogue.sentences[currentSentenceIndex];
    }

    public void EndDialogue()
    {
        IsDialogueActive = false;

        currentDialogue = null;
        currentSentenceIndex = 0;

        dialoguePanel.SetActive(false);
    }
}