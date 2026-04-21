using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleEmailClick : MonoBehaviour
{
    [Header("Referensi Panel")]
    public GameObject panelInbox;  // Panel daftar email (Kotak Masuk)
    public GameObject panelDetail; // Panel isi email (Detail)

    // FUNGSI 1: Dipanggil saat klik baris email di daftar
    public void BukaDetailEmail()
    {
        if (panelDetail != null)
        {
            panelDetail.SetActive(true);
        }
    }

    // FUNGSI 2: Dipanggil saat klik tombol Close/Exit di panel detail
    public void TutupDetailEmail()
    {
        if (panelDetail != null)
        {
            panelDetail.SetActive(false); // Sembunyikan detail

            if (panelInbox != null)
                panelInbox.SetActive(true);  // Munculkan kembali daftar
        }
    }
}
