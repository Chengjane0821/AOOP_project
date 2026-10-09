using System;

[Serializable]
public class QuestObjective
{
    public string description;
    public bool isCompleted;

    public QuestObjective(string description)
    {
        this.description = description;
        isCompleted = false;
    }

    public void Complete()
    {
        isCompleted = true;
    }
}