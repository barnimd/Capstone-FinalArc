using UnityEngine;

public class Interactable : MonoBehaviour
{
    public GameObject interactPrompt;

    [Header("Data Dialog NPC Ini")]
    public DialogueData dataDialogAwal;

    [Header("Referensi ke Manager")]
    public DialogueManager manager;

    private bool playerNearby = false;
    private PlayerMovement playerMovement;

    void Start()
    {
        interactPrompt.SetActive(false);
    }

    void Update()
    {
        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Interaction triggered");
            OpenDialogue();
        }
    }

        void OpenDialogue()
    {
        if (manager != null && dataDialogAwal != null)
        {
            manager.StartDialogue(dataDialogAwal);
        }
        else
        {
            Debug.LogWarning("Manager atau Data Dialog belum dimasukkan ke Inspector NPC!");
        }

        interactPrompt.SetActive(false);

        if (playerMovement != null)
            playerMovement.movementLocked = true;
    }

    public void CloseDialogue()
    {
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