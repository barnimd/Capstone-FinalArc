using UnityEngine;

/// <summary>
/// Interface for any object that can be interacted with by the player.
/// Implement this on any MonoBehaviour to make it interactable.
/// </summary>
public interface IInteractable
{
    /// <summary>Text shown in the interaction prompt (e.g. "Press E to use Computer")</summary>
    string InteractionPrompt { get; }

    /// <summary>Called when the player presses the interact key while in range</summary>
    void Interact(GameObject interactor);
}
