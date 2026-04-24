using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Panel yang ingin dimunculkan")]
    public GameObject panelClue;

    // Saat mouse masuk ke area
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (panelClue != null)
        {
            // Panggil ShowTooltip agar teks terisi otomatis dari data email aktif
            if (SenderTooltip.Instance != null)
                SenderTooltip.Instance.ShowTooltip();
            else
                panelClue.SetActive(true); // fallback jika SenderTooltip tidak ada
        }
    }

    // Saat mouse keluar dari area
    public void OnPointerExit(PointerEventData eventData)
    {
        if (panelClue != null)
        {
            if (SenderTooltip.Instance != null)
                SenderTooltip.Instance.HideTooltip();
            else
                panelClue.SetActive(false); // fallback
        }
    }
}