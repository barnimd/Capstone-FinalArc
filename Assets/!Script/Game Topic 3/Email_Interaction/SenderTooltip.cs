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
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (gameObject.activeSelf)
        {
            transform.position = Input.mousePosition + (Vector3)offset;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Dipanggil oleh EmailRandomizer setiap kali email baru dibuka.
    /// Menyimpan data email — tooltip akan pakai data ini saat ShowTooltip dipanggil.
    /// </summary>
    public void SetEmailData(EmailEntry entry)
    {
        _emailAktif = entry;
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Tampilkan tooltip. Isi teks otomatis dari data email aktif.
    /// Parameter message dan isPhishing tetap ada untuk kompatibilitas,
    /// tapi isi teks sekarang diambil dari _emailAktif.
    /// </summary>
    public void ShowTooltip(string message = "", bool isPhishing = false)
    {
        if (infoText != null)
        {
            if (_emailAktif != null)
            {
                // Format otomatis dari data email yang sedang dibuka
                infoText.text = $"dikirim oleh: {_emailAktif.emailPengirim}\n" +
                                $"waktu: {_emailAktif.waktu}\n" +
                                $"subject: {_emailAktif.subjek}";
            }
            else
            {
                // Fallback jika belum ada data
                infoText.text = message;
            }
        }

        gameObject.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }
}
