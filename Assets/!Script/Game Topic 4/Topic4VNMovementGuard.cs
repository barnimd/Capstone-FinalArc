using UnityEngine;

[DisallowMultipleComponent]
public class Topic4VNMovementGuard : MonoBehaviour
{
    [SerializeField] private VNDialogueManager dialogueManager;

    private TopDownPlayerMovement playerMovement;
    private bool previousCanMove;
    private bool movementLocked;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = GetComponent<VNDialogueManager>();

        playerMovement = FindObjectOfType<TopDownPlayerMovement>();
    }

    private void Update()
    {
        bool dialogueVisible = dialogueManager != null
            && dialogueManager.vnRoot != null
            && dialogueManager.vnRoot.activeInHierarchy;

        if (dialogueVisible && !movementLocked)
        {
            if (playerMovement == null)
                playerMovement = FindObjectOfType<TopDownPlayerMovement>();

            if (playerMovement == null)
                return;

            previousCanMove = playerMovement.CanMove;
            playerMovement.CanMove = false;
            movementLocked = true;
        }
        else if (!dialogueVisible && movementLocked)
        {
            RestoreMovement();
        }
    }

    private void OnDisable()
    {
        RestoreMovement();
    }

    private void RestoreMovement()
    {
        if (playerMovement != null)
            playerMovement.CanMove = previousCanMove;

        movementLocked = false;
    }
}
