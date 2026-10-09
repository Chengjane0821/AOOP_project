using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public Quest MainQuest { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
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
    }

    public void CompleteObjective(int index)
    {
        if (MainQuest == null)
            return;

        if (index < 0 ||
            index >= MainQuest.objectives.Count)
            return;

        MainQuest.objectives[index].Complete();

        Debug.Log(
            "Objective completed: " +
            MainQuest.objectives[index].description
        );

        if (MainQuest.IsCompleted)
        {
            Debug.Log(
                "All clues found! House 6 can now be unlocked."
            );
        }
    }

    public bool IsObjectiveCompleted(int index)
    {
        if (MainQuest == null)
            return false;

        if (index < 0 ||
            index >= MainQuest.objectives.Count)
            return false;

        return MainQuest.objectives[index].isCompleted;
    }
}