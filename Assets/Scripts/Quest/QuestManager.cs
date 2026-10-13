using System;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public Quest MainQuest { get; private set; }

    public event Action OnQuestUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartMainQuest()
    {
        if (MainQuest != null)
            return;

        MainQuest = new Quest(
            "The Missing Villager",
            "Investigate the village and discover what happened."
        );

        MainQuest.AddObjective("Find the Forgotten Photograph");
        MainQuest.AddObjective("Discover the Memory Potion");
        MainQuest.AddObjective("Find the Broken Emblem");
        MainQuest.AddObjective("Find the Hidden Passage Map");
        MainQuest.AddObjective("Find the Forgotten Diary");

        Debug.Log("Main quest started.");

        // Tell UI to refresh
        OnQuestUpdated?.Invoke();
    }

    public void CompleteObjective(int index)
    {
        Debug.Log("CompleteObjective called with index: " + index);

        if (MainQuest == null)
        {
            Debug.LogError("MainQuest is NULL!");
            return;
        }

        if (index < 0 || index >= MainQuest.objectives.Count)
        {
            Debug.LogError("Invalid objective index: " + index);
            return;
        }

        MainQuest.objectives[index].Complete();

        Debug.Log(
            "Objective completed: " +
            MainQuest.objectives[index].description
        );

        // IMPORTANT
        OnQuestUpdated?.Invoke();

        if (MainQuest.IsCompleted)
        {
            Debug.Log("All clues found! House 6 can now be unlocked.");
        }
    }
}