using UnityEngine;
using UnityEngine.InputSystem;

public class QuestTester : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // Q: Start quest
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            Debug.Log("Q pressed - starting quest");

            QuestManager.Instance?.StartMainQuest();
        }

        // 1
        if (Keyboard.current.digit1Key.wasPressedThisFrame ||
            Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            Debug.Log("Completing objective 0");
            QuestManager.Instance?.CompleteObjective(0);
        }

        // 2
        if (Keyboard.current.digit2Key.wasPressedThisFrame ||
            Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            Debug.Log("Completing objective 1");
            QuestManager.Instance?.CompleteObjective(1);
        }

        // 3
        if (Keyboard.current.digit3Key.wasPressedThisFrame ||
            Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            Debug.Log("Completing objective 2");
            QuestManager.Instance?.CompleteObjective(2);
        }

        // 4
        if (Keyboard.current.digit4Key.wasPressedThisFrame ||
            Keyboard.current.numpad4Key.wasPressedThisFrame)
        {
            Debug.Log("Completing objective 3");
            QuestManager.Instance?.CompleteObjective(3);
        }

        // 5
        if (Keyboard.current.digit5Key.wasPressedThisFrame ||
            Keyboard.current.numpad5Key.wasPressedThisFrame)
        {
            Debug.Log("Completing objective 4");
            QuestManager.Instance?.CompleteObjective(4);
        }
    }
}