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

    [Header("=== Animasi Kena Hack (Balas email phishing) ===")]
    [Tooltip("Kosongkan untuk auto-cari di scene. Kalau tidak ada, hasil langsung tampil tanpa animasi.")]
    public PhishingHackAnimation animasiKenaHack;

    private System.Action _onYesAction;
    private bool          _gameOverSetelahHasil;

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

    private EmailEntry EmailSaatIni()
    {
        return emailRandomizer != null ? emailRandomizer.GetEmailTerbuka() : null;
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
                EmailEntry entry = EmailSaatIni();
                bool phishing = entry != null && entry.isPhishing;
                if (EmailManager.Instance != null)
                    EmailManager.Instance.RecordDecision(PlayerAction.Balas, entry);

                if (phishing)
                {
                    // Popup dulu. Animasi kena hack + game over jalan setelah player pencet OK.
                    _gameOverSetelahHasil = true;
                    TampilHasil("Kamu telah membalas email Phishing!\nLain kali harap lebih berhati-hati.");
                }
                else
                {
                    TampilHasil("Balasan terkirim.\nEmail ini adalah email normal.");
                }
            }
        );
    }

    /// <summary>
    /// Putar cutscene hacker mancing data dari PC. Kalau animasinya tidak ada di scene,
    /// langsung jalankan <paramref name="setelahnya"/> supaya alur game tidak mandek.
    /// </summary>
    private void MainkanAnimasiKenaHack(System.Action setelahnya)
    {
        if (animasiKenaHack == null)
            animasiKenaHack = PhishingHackAnimation.Cari();

        if (animasiKenaHack == null)
        {
            Debug.LogWarning("[EmailDetailButtons] PhishingHackAnimation tidak ada di scene, hasil ditampilkan langsung.");
            setelahnya?.Invoke();
            return;
        }

        animasiKenaHack.Mainkan(setelahnya);
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
                EmailEntry entry = EmailSaatIni();
                if (EmailManager.Instance != null)
                    EmailManager.Instance.RecordDecision(PlayerAction.Hapus, entry);

                TampilHasil("Email telah dihapus.");
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
                EmailEntry entry = EmailSaatIni();
                bool phishing = entry != null && entry.isPhishing;
                if (EmailManager.Instance != null)
                    EmailManager.Instance.RecordDecision(PlayerAction.Laporkan, entry);

                if (phishing)
                    TampilHasil("Benar! Ini adalah email Phishing.\nTerima kasih sudah melaporkannya!");
                else
                    TampilHasil("Email ini sebenarnya normal.\nLain kali periksa lebih teliti.");
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

    // Setelah player lihat hasil → email hilang dari list.
    // Khusus kasus balas email phishing: putar animasi kena hack, lalu game over → summary.
    private void OnHasilOk()
    {
        panelHasil.SetActive(false);

        if (_gameOverSetelahHasil)
        {
            _gameOverSetelahHasil = false;
            btnHasilOk.interactable = false;

            // Tutup UI email dulu, baru animasi. Jadi pas overlay fade out player tidak
            // sempat lihat inbox berkedip sebelum summary muncul.
            panelEmailDetail.SetActive(false);
            if (EmailManager.Instance != null)
                EmailManager.Instance.CloseEmailCanvas();

            MainkanAnimasiKenaHack(GameOverKenaPhishing);
            return;
        }

        if (emailRandomizer != null)
            emailRandomizer.HilangkanEmailDariList();
        else
            KembaliKeEmailList();
    }

    /// <summary>Game berakhir karena player membalas email phishing → langsung ke summary.</summary>
    private void GameOverKenaPhishing()
    {
        if (EvaluationManager.Instance != null)
        {
            EvaluationManager.Instance.TriggerGameOver("balas_email_phishing");
        }
        else
        {
            Debug.LogError("[EmailDetailButtons] EvaluationManager tidak ada di scene — summary tidak bisa ditampilkan!");
            btnHasilOk.interactable = true;
            KembaliKeEmailList();
        }
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
