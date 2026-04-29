using UnityEngine;

/// <summary>
/// VN-style proximity NPC. Same flow as NPCBubbleInteractable
/// (player walks up, "Press E", press E to interact) but instead of a
/// speech bubble it opens a full-screen Visual Novel scene via VNDialogueManager.
///
/// Use this on the receptionist (and any other NPC you want VN-styled in Topic 1).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class VNNPCInteractable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("'Press E to interact' prompt (child UI element).")]
    public GameObject interactPrompt;

    [Tooltip("Key the player presses to start the VN dialogue.")]
    public KeyCode interactKey = KeyCode.E;

    [Header("VN Dialogue Data")]
    public VNDialogueData vnDialogue;

    [Header("Manager Reference")]
    [Tooltip("VN dialogue manager in the scene. If left empty it will use VNDialogueManager.Instance.")]
    public VNDialogueManager manager;

    [Header("Optional Events")]
    [Tooltip("GameObject to activate after this dialogue ends (e.g. enable next quest).")]
    public GameObject activateAfterDialogue;

    [Tooltip("Next NPC to enable after this dialogue ends.")]
    public VNNPCInteractable nextNPCToActivate;

    [Tooltip("Optional NPC to disable after this dialogue ends.")]
    public VNNPCInteractable npcToDisableAfterDialogue;

    [Header("Accept / Reject Side Effects")]
    [Tooltip("GameObject activated when player chooses Accept on a choice line.")]
    public GameObject activateOnAccept;

    [Header("State")]
    public bool isInteractable = true;
    public bool oneTimeOnly = false;

    // ---- runtime ----
    private bool playerNearby;
    private bool hasInteracted;
    private PlayerMovement playerMovement;

    void Start()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning("[VNNPCInteractable] Collider2D on '" + gameObject.name +
                             "' should be a trigger.");
    }

    void Update()
    {
        if (!isInteractable) return;
        if (oneTimeOnly && hasInteracted) return;

        if (playerNearby && Input.GetKeyDown(interactKey))
            OpenDialogue();
    }

    void OpenDialogue()
    {
        VNDialogueManager mgr = manager != null ? manager : VNDialogueManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[VNNPCInteractable] No VNDialogueManager assigned and no Instance found.");
            return;
        }
        if (vnDialogue == null)
        {
            Debug.LogWarning("[VNNPCInteractable] No VNDialogueData assigned on " + gameObject.name);
            return;
        }

        mgr.SetActiveInteractable(this);
        mgr.StartDialogue(vnDialogue);

        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (playerMovement != null) playerMovement.movementLocked = true;

        hasInteracted = true;
    }

    public void OnDialogueAccepted()
    {
        if (activateOnAccept != null) activateOnAccept.SetActive(true);
    }

    public void OnDialogueRejected()
    {
        // hook for later
    }

    public void OnDialogueEnd()
    {
        if (playerMovement != null) playerMovement.movementLocked = false;

        if (activateAfterDialogue != null) activateAfterDialogue.SetActive(true);

        if (nextNPCToActivate != null) nextNPCToActivate.isInteractable = true;

        if (npcToDisableAfterDialogue != null)
        {
            npcToDisableAfterDialogue.isInteractable = false;
            if (npcToDisableAfterDialogue.interactPrompt != null)
                npcToDisableAfterDialogue.interactPrompt.SetActive(false);
        }

        if (oneTimeOnly && interactPrompt != null) interactPrompt.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerNearby = true;
        playerMovement = collision.GetComponent<PlayerMovement>();

        if (!isInteractable || (oneTimeOnly && hasInteracted)) return;
        if (interactPrompt != null) interactPrompt.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerNearby = false;
        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (playerMovement != null) playerMovement.movementLocked = false;
    }
}
