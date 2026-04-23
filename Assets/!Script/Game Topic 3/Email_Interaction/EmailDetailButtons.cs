using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach ke EmailDetailPanel.
/// Assign semua referensi di Inspector.
/// </summary>
public class EmailDetailButtons : MonoBehaviour
{
    [Header("=== Tombol Email Detail ===")]
    public Button btnBalas;
    public Button btnHapus;
    public Button btnLaporkan;

    [Header("=== Konfirmasi Dialog Panel ===")]
    public GameObject      panelKonfirmasi;
    public TextMeshProUGUI txtKonfirmasiPesan;
    public Button          btnKonfirmasiYes;
    public Button          btnKonfirmasiNo;

    [Header("=== Hasil / Notifikasi Panel ===")]
    public GameObject      panelHasil;
    public TextMeshProUGUI txtHasilPesan;
    public Button          btnHasilOk;

    [Header("=== Panel ===")]
    public GameObject panelEmailDetail;
    public GameObject panelEmailList;

    [Header("=== Referensi Randomizer ===")]
    [Tooltip("Assign GameObject yang punya EmailRandomizer component.")]
    public EmailRandomizer emailRandomizer;

    // ─────────────────────────────────────────────
    private System.Action _onYesAction;

    private void Start()
    {
        panelKonfirmasi.SetActive(false);
        panelHasil.SetActive(false);

        btnBalas.onClick.AddListener(OnClickBalas);
        btnHapus.onClick.AddListener(OnClickHapus);
        btnLaporkan.onClick.AddListener(OnClickLaporkan);

        btnKonfirmasiYes.onClick.AddListener(OnKonfirmasiYes);
        btnKonfirmasiNo.onClick.AddListener(OnKonfirmasiNo);

        btnHasilOk.onClick.AddListener(OnHasilOk);
    }

    // ─────────────────────────────────────────────
    private bool EmailSaatIniPhishing()
    {
        if (emailRandomizer == null) return false;
        var entry = emailRandomizer.GetEmailTerbuka();
        return entry != null && entry.isPhishing;
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
                if (EmailSaatIniPhishing())
                    TampilHasil("⚠️ Kamu telah membalas email Phishing!\nLain kali harap lebih berhati-hati.");
                else
                    TampilHasil("✅ Balasan terkirim.\nEmail ini adalah email normal.");
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
            onYes: () => KembaliKeEmailList()
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
                if (EmailSaatIniPhishing())
                    TampilHasil("✅ Benar! Ini adalah email Phishing.\nTerima kasih sudah melaporkannya!");
                else
                    TampilHasil("❌ Email ini bukan Phishing.\nHarap periksa lebih teliti sebelum melapor.");
            }
        );
    }

    // ─────────────────────────────────────────────
    private void OnKonfirmasiYes()
    {
        panelKonfirmasi.SetActive(false);
        _onYesAction?.Invoke();
        _onYesAction = null;
    }

    private void OnKonfirmasiNo()
    {
        panelKonfirmasi.SetActive(false);
        _onYesAction = null;
        KembaliKeEmailList();
    }

    private void OnHasilOk()
    {
        panelHasil.SetActive(false);
        KembaliKeEmailList();
    }

    // ─────────────────────────────────────────────
    private void TampilKonfirmasi(string pesan, System.Action onYes)
    {
        _onYesAction = onYes;
        txtKonfirmasiPesan.text = pesan;
        panelKonfirmasi.SetActive(true);
    }

    private void TampilHasil(string pesan)
    {
        txtHasilPesan.text = pesan;
        panelHasil.SetActive(true);
    }

    private void KembaliKeEmailList()
    {
        panelEmailDetail.SetActive(false);
        panelEmailList.SetActive(true);
    }
}
