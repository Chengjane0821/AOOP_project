using System.Text;
using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TMP_Text questTitleText;
    [SerializeField] private TMP_Text questObjectivesText;

    private void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestUpdated += RefreshUI;
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
        if (QuestManager.Instance == null ||
            QuestManager.Instance.CurrentQuest == null)
        {
            questPanel.SetActive(false);
            return;
        }

        Quest quest = QuestManager.Instance.CurrentQuest;

        questPanel.SetActive(true);

        questTitleText.text = quest.questName;

        StringBuilder text = new StringBuilder();

        foreach (QuestObjective objective in quest.objectives)
        {
            if (objective.isCompleted)
            {
                text.AppendLine("[DONE] " + objective.description);
            }
            else
            {
                text.AppendLine("[ ] " + objective.description);
            }
        }

        questObjectivesText.text = text.ToString();
    }
}