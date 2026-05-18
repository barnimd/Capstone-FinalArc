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

    [Header("Auto Trigger")]
    [Tooltip("Start dialogue automatically when player enters the trigger (no key press needed).")]
    public bool autoTrigger = false;

    [Header("Room Transition (fires while screen is black during VN fade-in)")]
    [Tooltip("Teleport player here when screen is fully black. Leave empty to skip.")]
    public Transform roomSpawnPoint;
    public CameraFollow roomCameraFollow;
    public Transform roomCameraSnapPoint;
    public Vector2 roomMinBounds;
    public Vector2 roomMaxBounds;

    [Header("Objective & Follow Arrow (optional)")]
    [Tooltip("Objective UI panel to display after this dialogue ends.")]
    public ObjectiveUI_Tp4 objectiveUI;

    [Tooltip("Text to show in the objective panel after this dialogue ends.")]
    public string objectiveTextOnEnd;

    [Tooltip("Follow-arrow to activate after this dialogue ends.")]
    public FollowArrow_Tp1 followArrow;

    [Tooltip("World-space target the follow-arrow should point toward (e.g. RightWallTrigger).")]
    public Transform followArrowTarget;

    [Header("State")]
    public bool isInteractable = true;
    public bool oneTimeOnly = false;

    // ---- runtime ----
    private bool playerNearby;
    private bool hasInteracted;
    private PlayerMovement playerMovement;
    private Rigidbody2D playerRb;

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
        if (playerRb != null) { playerRb.bodyType = RigidbodyType2D.Kinematic; playerRb.velocity = Vector2.zero; }

        hasInteracted = true;
    }

    // Called by VNDialogueManager when screen is fully black during fade-in.
    public void OnFadeToBlack()
    {
        if (roomSpawnPoint == null) return;

        // Use the cached rb/playerMovement references (found via GetComponentInParent)
        // so we move the correct root object, not a child collider.
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.gameObject.transform.position = roomSpawnPoint.position;
        }
        else if (playerMovement != null)
        {
            playerMovement.transform.position = roomSpawnPoint.position;
        }

        if (roomCameraFollow != null)
        {
            // Snap to explicit snap point, or fall back to player spawn so the
            // camera never drifts from the old room across the map.
            Vector3 snapPos = roomCameraSnapPoint != null
                ? roomCameraSnapPoint.position
                : roomSpawnPoint.position;

            float z = roomCameraFollow.transform.position.z;
            roomCameraFollow.transform.position = new Vector3(snapPos.x, snapPos.y, z);
            roomCameraFollow.minBounds = roomMinBounds;
            roomCameraFollow.maxBounds = roomMaxBounds;
        }
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
        if (playerRb != null) { playerRb.velocity = Vector2.zero; playerRb.bodyType = RigidbodyType2D.Dynamic; }
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

        if (objectiveUI != null && !string.IsNullOrEmpty(objectiveTextOnEnd))
            objectiveUI.ShowObjective(objectiveTextOnEnd);

        if (followArrow != null)
        {
            if (followArrowTarget != null)
                followArrow.Show(followArrowTarget);
            else
                followArrow.Show();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerNearby = true;
        playerMovement = collision.GetComponentInParent<PlayerMovement>();
        playerRb = collision.GetComponentInParent<Rigidbody2D>();

        if (!isInteractable || (oneTimeOnly && hasInteracted)) return;

        if (autoTrigger)
        {
            OpenDialogue();
            return;
        }

        if (interactPrompt != null) interactPrompt.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        playerNearby = false;
        if (interactPrompt != null) interactPrompt.SetActive(false);

        // autoTrigger: unlock is handled by VNDialogueManager.FinalizeEnd,
        // not here — otherwise the teleport in OnFadeToBlack triggers an exit
        // event that unlocks movement mid-dialogue and drops the player.
        if (!autoTrigger && playerMovement != null)
            playerMovement.movementLocked = false;
    }
}
