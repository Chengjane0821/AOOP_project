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
                text.AppendLine(
                    "✓ " + objective.description
                );
            }
            else
            {
                text.AppendLine(
                    "□ " + objective.description
                );
            }
        }

        questObjectivesText.text = text.ToString();
    }
}