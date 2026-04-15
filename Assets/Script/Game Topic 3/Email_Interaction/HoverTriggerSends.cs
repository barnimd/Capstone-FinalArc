using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Panel yang ingin dimunculkan")]
    public GameObject panelClue;

    // Saat mouse masuk ke area teks
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (panelClue != null)
        {
            panelClue.SetActive(true);
        }
    }

    // Saat mouse keluar dari area teks
    public void OnPointerExit(PointerEventData eventData)
    {
        if (panelClue != null)
        {
            panelClue.SetActive(false);
        }
    }
}