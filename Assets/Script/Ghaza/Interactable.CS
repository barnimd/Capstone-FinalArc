using UnityEngine;

public class Interactable : MonoBehaviour
{
    public GameObject interactPrompt;
    public GameObject dialoguePanel;

    private bool playerNearby = false;
    private PlayerMovement playerMovement;

    void Start()
    {
        interactPrompt.SetActive(false);
        dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Interaction triggered");
            OpenDialogue();
        }

        // ⭐ ESC CLOSE SUPPORT
        if (dialoguePanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDialogue();
        }
    }

    void OpenDialogue()
    {
        dialoguePanel.SetActive(true);
        interactPrompt.SetActive(false);

        if (playerMovement != null)
            playerMovement.movementLocked = true;
    }

    void CloseDialogue()
    {
        dialoguePanel.SetActive(false);

        if (playerMovement != null)
            playerMovement.movementLocked = false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = true;
            interactPrompt.SetActive(true);

            playerMovement = collision.GetComponent<PlayerMovement>();
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerNearby = false;
            interactPrompt.SetActive(false);
            CloseDialogue();
        }
    }
}