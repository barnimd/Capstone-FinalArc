using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Add this to any GameObject to make it interactable.
/// Set the prompt text and hook up onInteract UnityEvent in the Inspector.
/// Requires a Collider2D set to Is Trigger = true on the same or child GameObject.
/// </summary>
public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string interactionPrompt = "Press E";

    [Header("Events")]
    [Tooltip("Fired when player presses E while in range. Wire up your logic here.")]
    [SerializeField] private UnityEvent onInteract;

    public string InteractionPrompt => interactionPrompt;

    public void Interact(GameObject interactor)
    {
        onInteract?.Invoke();
    }
}
