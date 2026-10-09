using System;
using UnityEngine;

[Serializable]
public class DialogueData
{
    public string speakerName;

    [TextArea(2, 5)]
    public string[] sentences;
}