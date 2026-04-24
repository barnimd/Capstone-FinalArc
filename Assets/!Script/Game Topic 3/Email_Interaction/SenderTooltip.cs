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
    public Image background;
    public TextMeshProUGUI infoText;

    [Header("Posisi")]
    public Vector2 offset = new Vector2(10f, -10f);

    // ─── Data email aktif ─────────────────────────────────────────────────────
    private EmailEntry _emailAktif;

    private void Awake()
    {
        // Setup singleton — dipanggil meskipun GameObject tidak aktif
        // karena Awake hanya dipanggil saat pertama kali diaktifkan
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // OnEnable dipanggil setiap kali GameObject diaktifkan
    private void OnEnable()
    {
        // Pastikan Instance selalu ter-set ulang saat panel diaktifkan
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (gameObject.activeSelf)
        {
            transform.position = Input.mousePosition + (Vector3)offset;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void SetEmailData(EmailEntry entry)
    {
        _emailAktif = entry;

        // Jika tooltip sedang aktif saat email berganti, langsung update teksnya
        if (gameObject.activeSelf)
            UpdateTeks();
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void ShowTooltip(string message = "", bool isPhishing = false)
    {
        UpdateTeks();

        // Logika penggantian warna dihapus, hanya mengaktifkan panel
        gameObject.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void UpdateTeks()
    {
        if (infoText == null) return;

        if (_emailAktif != null)
        {
            infoText.text = $"dikirim oleh: {_emailAktif.emailPengirim}\n" +
                            $"waktu: {_emailAktif.waktu}\n" +
                            $"subject: {_emailAktif.subjek}";
        }
        else
        {
            infoText.text = "Tidak ada data email.";
        }
    }
}
