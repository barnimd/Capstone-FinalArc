using UnityEngine;

/// <summary>
/// Attach to any object with a trigger Collider2D. When the player enters,
/// shows a bottom-center "[E] Enter" prompt; pressing the key opens the
/// referenced canvas and freezes player movement.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class InteractableTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Canvas to enable when the player presses the interact key.")]
    [SerializeField] private Canvas canvasToOpen;

    [Tooltip("Player movement to freeze. If null, auto-found on the player who entered.")]
    [SerializeField] private TopDownPlayerMovement playerMovement;

    [Header("Input")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string promptLabel = "Enter";

    private bool inRange;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        inRange = true;
        if (playerMovement == null)
            playerMovement = other.GetComponent<TopDownPlayerMovement>();
        InteractPromptUI.Show(this, promptLabel, interactKey);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        inRange = false;
        InteractPromptUI.Hide(this);
    }

    private void Update()
    {
        if (!inRange) return;
        if (Input.GetKeyDown(interactKey)) Interact();
    }

    private void Interact()
    {
        if (canvasToOpen != null) canvasToOpen.gameObject.SetActive(true);
        if (playerMovement != null) playerMovement.CanMove = false;
        InteractPromptUI.Hide(this);
    }

    /// <summary>Hook this to a Close button on the opened canvas to restore movement.</summary>
    public void CloseInteraction()
    {
        if (canvasToOpen != null) canvasToOpen.gameObject.SetActive(false);
        if (playerMovement != null) playerMovement.CanMove = true;
    }

    private void OnDisable()
    {
        InteractPromptUI.Hide(this);
    }
}
