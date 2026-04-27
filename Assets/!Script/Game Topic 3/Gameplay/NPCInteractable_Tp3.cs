using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCInteractable_Tp3 : MonoBehaviour
{
    [Header("Dialog")]
    public DialogData_Tp3 dialogData;

    [Header("Interaction")]
    public float interactRange = 2f;
    public GameObject interactPrompt;

    private Transform player;
    private bool playerInRange = false;
    private bool hasInteracted = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (interactPrompt) interactPrompt.SetActive(false);
    }

    void Update()
    {
        if (hasInteracted) return;

        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactRange;

        if (interactPrompt)
            interactPrompt.SetActive(playerInRange && !DialogManager_Tp3.instance.IsActive());

        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !DialogManager_Tp3.instance.IsActive())
            Interact();
    }

    void Interact()
    {
        hasInteracted = true;
        if (interactPrompt) interactPrompt.SetActive(false);

        GameManager_Tp3.instance.OnPlayerApproachNPC();
        DialogManager_Tp3.instance.StartDialog(dialogData);
    }

    public void ResetInteraction()
    {
        hasInteracted = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}