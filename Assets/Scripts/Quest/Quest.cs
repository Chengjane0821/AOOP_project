using System.Collections.Generic;

public class Quest
{
    public string questName;
    public string description;

    public List<QuestObjective> objectives;

    public bool IsCompleted
    {
        get
        {
            foreach (QuestObjective objective in objectives)
            {
                if (!objective.isCompleted)
                    return false;
            }

            return true;
        }
    }

    public Quest(string questName, string description)
    {
        this.questName = questName;
        this.description = description;

        objectives = new List<QuestObjective>();
    }

    public void AddObjective(string description)
    {
        objectives.Add(
            new QuestObjective(description)
        );
    }
}using UnityEngine;

public class Quest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
