using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    private DialogueData currentDialogue;
    private int currentSentenceIndex;

    // 防止開啟對話的那一次 E
    // 同時把第一句跳過
    private bool waitingForERelease = false;

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

    private void Update()
    {
        if (!IsDialogueActive)
            return;

        if (Keyboard.current == null)
            return;

        // 開啟 Dialogue 後，
        // 先等待玩家放開第一次按下的 E
        if (waitingForERelease)
        {
            if (!Keyboard.current.eKey.isPressed)
            {
                waitingForERelease = false;
            }

            return;
        }

        // 之後每按一次 E → 下一句
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            NextSentence();
        }
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

        // 防止開始對話的 E 直接跳到第二句
        waitingForERelease = true;

        dialoguePanel.SetActive(true);

        speakerNameText.text = currentDialogue.speakerName;

        ShowCurrentSentence();
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

        ShowCurrentSentence();
    }

    private void ShowCurrentSentence()
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