using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach script ini ke EmailDetailPanel.
/// Assign semua referensi di Inspector.
/// </summary>
public class EmailDetailButtons : MonoBehaviour
{
    [Header("=== Tombol Email Detail ===")]
    public Button btnBalas;
    public Button btnHapus;
    public Button btnLaporkan;

    [Header("=== Konfirmasi Dialog Panel ===")]
    public GameObject panelKonfirmasi;
    public TextMeshProUGUI txtKonfirmasiPesan;
    public Button btnKonfirmasiYes;
    public Button btnKonfirmasiNo;

    [Header("=== Hasil / Notifikasi Panel ===")]
    public GameObject panelHasil;
    public TextMeshProUGUI txtHasilPesan;
    public Button btnHasilOk;

    [Header("=== Panel yang perlu ditutup/dibuka ===")]
    public GameObject panelEmailDetail;   // panel detail ini sendiri
    public GameObject panelEmailList;     // panel daftar email (EmailList)

    // Menyimpan action yang akan dijalankan saat Yes diklik
    private System.Action _onYesAction;

    // ─────────────────────────────────────────────
    private void Start()
    {
        // Pastikan panel dialog tertutup di awal
        panelKonfirmasi.SetActive(false);
        panelHasil.SetActive(false);

        // Wire tombol utama
        btnBalas.onClick.AddListener(OnClickBalas);
        btnHapus.onClick.AddListener(OnClickHapus);
        btnLaporkan.onClick.AddListener(OnClickLaporkan);

        // Wire tombol konfirmasi
        btnKonfirmasiYes.onClick.AddListener(OnKonfirmasiYes);
        btnKonfirmasiNo.onClick.AddListener(OnKonfirmasiNo);

        // Wire tombol hasil OK
        btnHasilOk.onClick.AddListener(OnHasilOk);
    }

    // ─────────────────────────────────────────────
    // BALAS
    // ─────────────────────────────────────────────
    private void OnClickBalas()
    {
        TampilKonfirmasi(
            pesan: "Apakah kamu yakin ingin membalas email ini?",
            onYes: () =>
            {
                // Tampilkan peringatan phishing
                TampilHasil("Kamu telah mengklik Phishing!\nLain kali harap lebih berhati-hati.");
            }
        );
    }

    // ─────────────────────────────────────────────
    // HAPUS
    // ─────────────────────────────────────────────
    private void OnClickHapus()
    {
        TampilKonfirmasi(
            pesan: "Apakah kamu yakin ingin menghapus email ini?",
            onYes: () =>
            {
                // Langsung kembali ke email list (Yes maupun No sama-sama kembali)
                KembaliKeEmailList();
            }
        );
    }

    // ─────────────────────────────────────────────
    // LAPORKAN
    // ─────────────────────────────────────────────
    private void OnClickLaporkan()
    {
        TampilKonfirmasi(
            pesan: "Apakah kamu yakin ingin melaporkan email ini sebagai Phishing?",
            onYes: () =>
            {
                TampilHasil("Kamu berhasil melaporkan email Phishing!\nTerima kasih, kami akan menindaklanjuti laporan ini.");
            }
        );
    }

    // ─────────────────────────────────────────────
    // KONFIRMASI: YES
    // ─────────────────────────────────────────────
    private void OnKonfirmasiYes()
    {
        panelKonfirmasi.SetActive(false);
        _onYesAction?.Invoke();
        _onYesAction = null;
    }

    // ─────────────────────────────────────────────
    // KONFIRMASI: NO  →  kembali ke email list
    // ─────────────────────────────────────────────
    private void OnKonfirmasiNo()
    {
        panelKonfirmasi.SetActive(false);
        _onYesAction = null;
        KembaliKeEmailList();
    }

    // ─────────────────────────────────────────────
    // HASIL OK  →  kembali ke email list
    // ─────────────────────────────────────────────
    private void OnHasilOk()
    {
        panelHasil.SetActive(false);
        KembaliKeEmailList();
    }

    // ─────────────────────────────────────────────
    // HELPER: Tampilkan dialog konfirmasi
    // ─────────────────────────────────────────────
    private void TampilKonfirmasi(string pesan, System.Action onYes)
    {
        _onYesAction = onYes;
        txtKonfirmasiPesan.text = pesan;
        panelKonfirmasi.SetActive(true);
    }

    // ─────────────────────────────────────────────
    // HELPER: Tampilkan panel hasil/notifikasi
    // ─────────────────────────────────────────────
    private void TampilHasil(string pesan)
    {
        txtHasilPesan.text = pesan;
        panelHasil.SetActive(true);
    }

    // ─────────────────────────────────────────────
    // HELPER: Kembali ke daftar email
    // ─────────────────────────────────────────────
    private void KembaliKeEmailList()
    {
        panelEmailDetail.SetActive(false);
        panelEmailList.SetActive(true);
    }
}
