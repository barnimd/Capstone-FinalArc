using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Topic6ComputerInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Canvas canvasToOpen;
    [SerializeField] private TopDownPlayerMovement playerMovement;
    [SerializeField] private GameManager_Tp6 gameManager;
    [SerializeField] private Topic6ProgressionController progression;
    [SerializeField] private bool unlocked;

    public string InteractionPrompt => unlocked
        ? "Tekan E untuk berinteraksi"
        : "Selesaikan dialog backup dulu";

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    public void SetUnlocked(bool value)
    {
        unlocked = value;
    }

    public void Interact(GameObject interactor)
    {
        if (!unlocked)
            return;

        if (playerMovement == null && interactor != null)
            playerMovement = interactor.GetComponentInParent<TopDownPlayerMovement>();

        if (progression == null)
            progression = FindObjectOfType<Topic6ProgressionController>();

        progression?.BeginComputerInteraction();

        if (gameManager != null)
            gameManager.BeginDesktopFlow();
        else if (canvasToOpen != null)
            canvasToOpen.gameObject.SetActive(true);

        if (playerMovement != null)
            playerMovement.CanMove = false;

        // Sembunyikan prompt "Tekan E" selama desktop terbuka
        var prompt = FindObjectOfType<InteractionPromptUI>();
        if (prompt != null)
            prompt.HidePrompt();
    }

    public void CloseInteraction()
    {
        if (canvasToOpen != null)
            canvasToOpen.gameObject.SetActive(false);

        if (playerMovement != null)
            playerMovement.CanMove = true;
    }
}
