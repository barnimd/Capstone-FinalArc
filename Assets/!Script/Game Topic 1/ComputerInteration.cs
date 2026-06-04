using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractOpenDesktop : MonoBehaviour
{
    public event System.Action DesktopOpened;
    public event System.Action DesktopClosed;

    public GameObject desktopCanvas;
    public GameObject interactText;
    public MonoBehaviour playerMovement; // Legacy field kept for backward compat

    [Tooltip("If true, player cannot open this desktop by pressing E — must be unlocked via NPC dialogue (activateOnAccept)")]
    public bool requiresNPCUnlock = false;

    private bool canInteract = false;
    private bool desktopOpen = false;
    private PlayerMovement playerMov;

    public bool IsUnlocked => !requiresNPCUnlock;

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
            OpenDesktop();
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

        DesktopClosed?.Invoke();
        RefreshPrompt();
    }

    public void SetUnlocked(bool unlocked)
    {
        requiresNPCUnlock = !unlocked;
        if (!unlocked && desktopOpen)
            CloseDesktop();
        RefreshPrompt();
    }

    public void RefreshPrompt()
    {
        if (interactText != null)
            interactText.SetActive(canInteract && !requiresNPCUnlock && !desktopOpen);
    }

    private void OpenDesktop()
    {
        if (desktopCanvas != null)
            desktopCanvas.SetActive(true);

        desktopOpen = true;
        if (playerMov != null)
            playerMov.movementLocked = true;

        RefreshPrompt();
        DesktopOpened?.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            playerMov = other.GetComponent<PlayerMovement>();

            RefreshPrompt();
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

            RefreshPrompt();
        }
    }
}

