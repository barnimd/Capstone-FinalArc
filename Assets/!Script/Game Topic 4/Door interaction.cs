using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Doorinteraction : MonoBehaviour
{
    [Header("Colliders")]
    public Collider2D doorCollider;      // collider di master (isTrigger OFF)
    public Collider2D triggerCollider;   // collider di child (isTrigger ON)

    [Header("Settings")]
    public string playerTag = "Player";

    private Animator anim;
    private bool isOpen = false;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger hit by: " + other.name + " | Tag: " + other.tag);
        if (other.CompareTag(playerTag) && !isOpen)
        {
            isOpen = true;
            anim.SetBool("isOpen", true);
            doorCollider.enabled = false; // matiin collider biar player bisa lewat
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && isOpen)
        {
            isOpen = false;
            anim.SetBool("isOpen", false);
            doorCollider.enabled = true; // nyalain lagi collider
        }
    }
}
