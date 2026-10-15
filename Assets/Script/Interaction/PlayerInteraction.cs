
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2.5f;
    [SerializeField] private LayerMask interactionLayer;

    private IInteractable currentInteractable;

    private void Update()
    {
        FindInteractable();

        // 按 O 鍵互動
        if (currentInteractable != null &&
            Keyboard.current != null &&
            Keyboard.current.oKey.wasPressedThisFrame)
        {
            currentInteractable.Interact();
        }
    }

    private void FindInteractable()
    {
        currentInteractable = null;
        float nearestDistance = float.MaxValue;

        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            interactionRadius,
            interactionLayer,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider col in colliders)
        {
            IInteractable interactable =
                col.GetComponentInParent<IInteractable>();

            if (interactable == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                col.ClosestPoint(transform.position)
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                currentInteractable = interactable;
            }
        }
    }

    private void OnGUI()
    {
        if (currentInteractable == null)
            return;

        GUI.Box(
            new Rect(
                Screen.width / 2f - 120,
                Screen.height - 100,
                240,
                45
            ),
            currentInteractable.InteractionPrompt
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            transform.position,
            interactionRadius
        );
    }
}
