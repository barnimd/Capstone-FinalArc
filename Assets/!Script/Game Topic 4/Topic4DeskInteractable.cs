using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Topic4DeskInteractable : MonoBehaviour
{
    [SerializeField] private Topic4ProgressionController progression;
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private bool playerNearby;
    private bool unlocked;

    private void Start()
    {
        RefreshPrompt();
    }

    private void Update()
    {
        if (!playerNearby || !unlocked || progression == null)
            return;

        if (Input.GetKeyDown(interactKey))
            progression.BeginComputerInteraction();
    }

    public void SetUnlocked(bool value)
    {
        unlocked = value;
        RefreshPrompt();
    }

    public void RefreshPrompt()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(playerNearby && unlocked);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = true;
        RefreshPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = false;
        RefreshPrompt();
    }
}
