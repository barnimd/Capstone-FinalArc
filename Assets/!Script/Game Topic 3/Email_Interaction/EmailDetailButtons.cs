using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("=== Referensi ===")]
    public EmailRandomizer emailRandomizer;

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
                bool phishing = EmailSaatIniPhishing();
                if (EmailManager.Instance != null)
                    EmailManager.Instance.RecordDecision(PlayerAction.Balas, phishing);

                if (phishing)
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
            onYes: () =>
            {
                bool phishing = EmailSaatIniPhishing();
                if (EmailManager.Instance != null)
                    EmailManager.Instance.RecordDecision(PlayerAction.Hapus, phishing);

                TampilHasil("🗑️ Email telah dihapus.");
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
                bool phishing = EmailSaatIniPhishing();
                if (EmailManager.Instance != null)
                    EmailManager.Instance.RecordDecision(PlayerAction.Laporkan, phishing);

                if (phishing)
                    TampilHasil("✅ Benar! Ini adalah email Phishing.\nTerima kasih sudah melaporkannya!");
                else
                    TampilHasil("⚠️ Email ini sebenarnya normal.\nLain kali periksa lebih teliti.");
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

    // Setelah player lihat hasil → email hilang dari list
    private void OnHasilOk()
    {
        panelHasil.SetActive(false);

        if (emailRandomizer != null)
            emailRandomizer.HilangkanEmailDariList();
        else
            KembaliKeEmailList();
    }

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
