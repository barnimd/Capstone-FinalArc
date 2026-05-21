using UnityEngine;
using TMPro;

/// <summary>
/// Enhanced NPC Interactable with proximity detection.
/// - Detects player proximity via BoxCollider2D trigger
/// - Shows "Press E to interact" prompt
/// - Triggers DialogueManager dialogue on E press
/// - Freezes player movement during conversation
/// </summary>
public class NPCBubbleInteractable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("The 'Press E to interact' prompt (child TextMeshPro or UI element)")]
    public GameObject interactPrompt;

    [Header("Dialogue Data")]
    [Tooltip("The dialogue data to start when player presses E")]
    public DialogueData dataDialogAwal;

    [Header("Manager References")]
    [Tooltip("Reference to the DialogueManager in the scene")]
    public DialogueManager manager;

    [Header("Optional Events")]
    [Tooltip("Optional: GameObject to activate after dialogue ends")]
    public GameObject activateAfterDialogue;

    [Tooltip("Optional: NPC interaction to enable after this dialogue ends")]
    public NPCBubbleInteractable nextNPCToActivate;

    [Tooltip("Optional: NPC to disable after this dialogue ends (e.g., paired NPC)")]
    public NPCBubbleInteractable npcToDisableAfterDialogue;

    [Header("Accept / Reject Actions")]
    [Tooltip("Optional: GameObject to activate when player chooses Accept/Yes in dialogue")]
    public GameObject activateOnAccept;

    [Header("State")]
    [Tooltip("If false, NPC cannot be interacted with yet")]
    public bool isInteractable = true;

    [Tooltip("If true, NPC can only be interacted with once")]
    public bool oneTimeOnly = false;

    [Header("Objective")]
    public ObjectiveUI_Tp4 objectiveUI;
    public string objectiveTextOnEnd;

    // Internal state
    private bool playerNearby = false;
    private bool hasInteracted = false;
    private PlayerMovement playerMovement;

    void Awake()
    {
        Debug.Log("[NPCBubbleInteractable] Awake on: " + gameObject.name);
    }

    void Start()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        Debug.Log("[NPCBubbleInteractable] Start on: " + gameObject.name
            + " | Has Collider2D: " + (GetComponent<Collider2D>() != null)
            + " | Is Trigger: " + (GetComponent<Collider2D>() != null && GetComponent<Collider2D>().isTrigger)
            + " | Manager assigned: " + (manager != null)
            + " | DialogueData assigned: " + (dataDialogAwal != null));
    }

    void Update()
    {
        if (!isInteractable) return;
        if (oneTimeOnly && hasInteracted) return;

        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[NPCBubbleInteractable] E pressed! Opening dialogue with: " + gameObject.name);
            OpenDialogue();
        }
    }

    void OpenDialogue()
    {
        if (manager != null && dataDialogAwal != null)
        {
            manager.SetActiveInteractable(this);
            manager.StartDialogue(dataDialogAwal);
        }
        else
        {
            Debug.LogWarning("[NPCBubbleInteractable] Manager or DialogueData not assigned on: " + gameObject.name);
        }

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        if (playerMovement != null)
            playerMovement.movementLocked = true;

        hasInteracted = true;
    }

    /// <summary>
    /// Called by DialogueManager immediately when player clicks Accept/Yes.
    /// </summary>
    public void OnDialogueAccepted()
    {
        if (activateOnAccept != null)
            activateOnAccept.SetActive(true);
    }

    /// <summary>
    /// Called by DialogueManager immediately when player clicks Reject/No.
    /// </summary>
    public void OnDialogueRejected()
    {
        // activateOnAccept is NOT activated — desktop stays closed
    }

    /// <summary>
    /// Called by DialogueManager when dialogue ends.
    /// </summary>
    public void OnDialogueEnd()
    {
        if (playerMovement != null)
            playerMovement.movementLocked = false;

        if (activateAfterDialogue != null)
            activateAfterDialogue.SetActive(true);

        if (nextNPCToActivate != null)
            nextNPCToActivate.isInteractable = true;

        // Disable paired NPC (e.g., Talking 1 disables Talking 2)
        if (npcToDisableAfterDialogue != null)
        {
            npcToDisableAfterDialogue.isInteractable = false;
            if (npcToDisableAfterDialogue.interactPrompt != null)
                npcToDisableAfterDialogue.interactPrompt.SetActive(false);
        }

        if (oneTimeOnly && interactPrompt != null)
            interactPrompt.SetActive(false);

        if (objectiveUI != null && !string.IsNullOrEmpty(objectiveTextOnEnd))
            objectiveUI.ShowObjective(objectiveTextOnEnd);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("[NPCBubbleInteractable] OnTriggerEnter2D on " + gameObject.name
            + " | collider: " + collision.gameObject.name
            + " | tag: " + collision.gameObject.tag);

        if (collision.CompareTag("Player"))
        {
            playerNearby = true;
            playerMovement = collision.GetComponent<PlayerMovement>();

            if (!isInteractable || (oneTimeOnly && hasInteracted)) return;

            if (interactPrompt != null)
                interactPrompt.SetActive(true);

            Debug.Log("[NPCBubbleInteractable] Player entered range of: " + gameObject.name);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("[NPCBubbleInteractable] Player left range of: " + gameObject.name);

            playerNearby = false;

            if (interactPrompt != null)
                interactPrompt.SetActive(false);

            if (playerMovement != null)
                playerMovement.movementLocked = false;
        }
    }
}

