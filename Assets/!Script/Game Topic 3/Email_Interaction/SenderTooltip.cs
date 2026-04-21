using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SenderTooltip : MonoBehaviour
{
    // Singleton agar bisa diakses dengan mudah oleh script lain
    public static SenderTooltip Instance;

    [Header("UI Elements")]
    public Image background;         // Tarik komponen Image panel ini ke sini
    public TextMeshProUGUI infoText;    // Tarik komponen Text (TMP) di dalam panel ke sini

    [Header("Posisi")]
    public Vector2 offset = new Vector2(10f, -10f); // Jarak dari kursor mouse

    private void Awake()
    {
        // Setup Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Sembunyikan tooltip saat game dimulai
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // Update posisi panel agar mengikuti koordinat mouse setiap frame
        if (gameObject.activeSelf)
        {
            transform.position = Input.mousePosition + (Vector3)offset;
        }
    }

    // Fungsi untuk memunculkan tooltip dengan data spesifik
    public void ShowTooltip(string message, bool isPhishing)
    {
        if (infoText != null) infoText.text = message;

        // Logika penggantian warna dihapus, hanya mengaktifkan panel
        gameObject.SetActive(true);
    }

    // Fungsi untuk menyembunyikan tooltip
    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }
}