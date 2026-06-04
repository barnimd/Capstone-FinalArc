using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Topic5LaptopInteractable : MonoBehaviour
{
    [SerializeField] private Topic5ProgressionController progression;
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private bool playerNearby;

    private void Start()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerNearby || progression == null || !progression.CanUseLaptop)
            return;

        if (Input.GetKeyDown(interactKey))
            progression.BeginLaptopInteraction();
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
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    public void RefreshPrompt()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(playerNearby && progression != null && progression.CanUseLaptop);
    }
}
