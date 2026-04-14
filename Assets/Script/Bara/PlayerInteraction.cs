using UnityEngine;

/// <summary>
/// Add to the Player GameObject. Detects nearby IInteractable objects via a
/// trigger collider and calls Interact() when the player presses E.
/// Requires a CircleCollider2D (Is Trigger = true) on this GameObject for the detection zone.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Key Binding")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private IInteractable currentInteractable;
    private InteractionPromptUI promptUI;

    private void Start()
    {
        promptUI = FindObjectOfType<InteractionPromptUI>(true);
        if (promptUI == null)
            Debug.LogWarning("[PlayerInteraction] No InteractionPromptUI found in scene.");
    }

    private void Update()
    {
        if (currentInteractable != null && Input.GetKeyDown(interactKey))
            currentInteractable.Interact(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check the hit object AND its parent for IInteractable
        IInteractable interactable = other.GetComponent<IInteractable>()
                                  ?? other.GetComponentInParent<IInteractable>();
        if (interactable == null) return;

        currentInteractable = interactable;
        promptUI?.ShowPrompt(interactable.InteractionPrompt);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>()
                                  ?? other.GetComponentInParent<IInteractable>();
        if (interactable == null || interactable != currentInteractable) return;

        currentInteractable = null;
        promptUI?.HidePrompt();
    }
}
