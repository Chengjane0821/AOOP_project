using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public Quest CurrentQuest { get; private set; }

    public event Action OnQuestUpdated;

    private List<Quest> questSequence;
    private int currentQuestIndex = -1;

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

        CreateQuestSequence();
    }

    private void CreateQuestSequence()
    {
        questSequence = new List<Quest>();

        // Quest 1
        Quest quest1 = new Quest(
            "The Forgotten Photograph",
            "Investigate the Elder's House."
        );
        quest1.AddObjective("Find the Forgotten Photograph");
        questSequence.Add(quest1);

        // Quest 2
        Quest quest2 = new Quest(
            "The Memory Potion",
            "Investigate the Alchemist's House."
        );
        quest2.AddObjective("Discover the Memory Potion");
        questSequence.Add(quest2);

        // Quest 3
        Quest quest3 = new Quest(
            "The Broken Emblem",
            "Investigate the Carpenter's House."
        );
        quest3.AddObjective("Find the Broken Emblem");
        questSequence.Add(quest3);

        // Quest 4
        Quest quest4 = new Quest(
            "The Hidden Passage",
            "Follow the Hunter's clues."
        );
        quest4.AddObjective("Find the Hidden Passage Map");
        questSequence.Add(quest4);

        // Quest 5
        Quest quest5 = new Quest(
            "The Forgotten Diary",
            "Investigate the Scholar's House."
        );
        quest5.AddObjective("Find the Forgotten Diary");
        questSequence.Add(quest5);

        // Final Quest
        Quest quest6 = new Quest(
            "The Final Mystery",
            "All five clues point to the abandoned house."
        );
        quest6.AddObjective("Enter House 6 and uncover the truth");
        questSequence.Add(quest6);
    }

    public void StartQuestSequence()
    {
        if (currentQuestIndex >= 0)
            return;

        currentQuestIndex = 0;
        CurrentQuest = questSequence[currentQuestIndex];

        Debug.Log("Quest started: " + CurrentQuest.questName);

        OnQuestUpdated?.Invoke();
    }

    public void CompleteObjective(int objectiveIndex)
    {
        if (CurrentQuest == null)
            return;

        if (objectiveIndex < 0 ||
            objectiveIndex >= CurrentQuest.objectives.Count)
            return;

        CurrentQuest.objectives[objectiveIndex].Complete();

        Debug.Log(
            "Objective completed: " +
            CurrentQuest.objectives[objectiveIndex].description
        );

        OnQuestUpdated?.Invoke();

        if (CurrentQuest.IsCompleted)
        {
            AdvanceToNextQuest();
        }
    }

    private void AdvanceToNextQuest()
    {
        Debug.Log("Quest completed: " + CurrentQuest.questName);

        currentQuestIndex++;

        if (currentQuestIndex >= questSequence.Count)
        {
            CurrentQuest = null;

            Debug.Log("All quests completed!");

            OnQuestUpdated?.Invoke();
            return;
        }

        CurrentQuest = questSequence[currentQuestIndex];

        Debug.Log("New quest: " + CurrentQuest.questName);

        OnQuestUpdated?.Invoke();
    }
}