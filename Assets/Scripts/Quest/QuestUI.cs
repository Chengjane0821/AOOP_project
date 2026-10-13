using System.Text;
using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    [Header("Quest UI")]
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TMP_Text questTitleText;
    [SerializeField] private TMP_Text questObjectivesText;

    private void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestUpdated += RefreshUI;

            Debug.Log("QuestUI subscribed to QuestManager.");
        }
        else
        {
            Debug.LogError("QuestUI cannot find QuestManager!");
        }

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestUpdated -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        Debug.Log("QuestUI RefreshUI called.");

        if (QuestManager.Instance == null ||
            QuestManager.Instance.MainQuest == null)
        {
            questPanel.SetActive(false);
            return;
        }

        Quest quest = QuestManager.Instance.MainQuest;

        questPanel.SetActive(true);

        questTitleText.text = quest.questName;

        StringBuilder text = new StringBuilder();

        foreach (QuestObjective objective in quest.objectives)
        {
            if (objective.isCompleted)
            {
                // 暫時用 [DONE]，避免 TMP 字型不支援 ✓
                text.AppendLine(
                    "[DONE] " + objective.description
                );
            }
            else
            {
                text.AppendLine(
                    "[ ] " + objective.description
                );
            }
        }

        questObjectivesText.text = text.ToString();

        Debug.Log("Quest UI text updated:\n" + text);
    }
}