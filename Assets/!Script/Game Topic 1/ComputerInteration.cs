using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractOpenDesktop : MonoBehaviour
{
    public GameObject desktopCanvas;
    public GameObject interactText;
    public MonoBehaviour playerMovement; // Legacy field kept for backward compat

    [Tooltip("If true, player cannot open this desktop by pressing E — must be unlocked via NPC dialogue (activateOnAccept)")]
    public bool requiresNPCUnlock = false;

    private bool canInteract = false;
    private bool desktopOpen = false;
    private PlayerMovement playerMov;

    void Start()
    {
        if (interactText != null)
            interactText.SetActive(false);

        if (desktopCanvas != null)
            desktopCanvas.SetActive(false);
    }

    void Update()
    {
        if (canInteract && Input.GetKeyDown(KeyCode.E) && !desktopOpen && !requiresNPCUnlock)
        {
            desktopCanvas.SetActive(true);
            desktopOpen = true;

            // Freeze player movement while desktop is open
            if (playerMov != null)
                playerMov.movementLocked = true;
        }

        // Allow closing desktop with Escape key
        if (desktopOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDesktop();
        }
    }

    /// <summary>
    /// Public method to close desktop (can be called from UI buttons too)
    /// </summary>
    public void CloseDesktop()
    {
        if (desktopCanvas != null)
            desktopCanvas.SetActive(false);

        desktopOpen = false;

        if (playerMov != null)
            playerMov.movementLocked = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            playerMov = other.GetComponent<PlayerMovement>();

            if (interactText != null)
                interactText.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;

            // Auto-close desktop if player walks away
            if (desktopOpen)
                CloseDesktop();

            if (interactText != null)
                interactText.SetActive(false);
        }
    }
}

