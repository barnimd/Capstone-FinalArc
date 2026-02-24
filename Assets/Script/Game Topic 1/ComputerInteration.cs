using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractOpenDesktop : MonoBehaviour
{
    public GameObject desktopCanvas;
    public GameObject interactText;
    public MonoBehaviour playerMovement;

    private bool canInteract = false;
    private bool desktopOpen = false;

    void Start()
    {
        if (interactText != null)
            interactText.SetActive(false);

        if (desktopCanvas != null)
            desktopCanvas.SetActive(false);
    }

    void Update()
    {
        if (canInteract && Input.GetKeyDown(KeyCode.E) && !desktopOpen)
        {
            desktopCanvas.SetActive(true);
            playerMovement.enabled = false; // MATIKAN GERAK
            desktopOpen = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;

            if (interactText != null)
                interactText.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;

            if (interactText != null)
                interactText.SetActive(false);
        }
    }
}
