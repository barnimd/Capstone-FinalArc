using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallMagazineInteraction : MonoBehaviour
{
    public GameObject hintUI;
    public GameObject rulebookPanel;

    private bool playerInRange = false;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            rulebookPanel.SetActive(true);
            hintUI.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            hintUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            hintUI.SetActive(false);
        }
    }

    public void CloseRulebook()
    {
        rulebookPanel.SetActive(false);
    }
}
