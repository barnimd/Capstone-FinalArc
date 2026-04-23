using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach ke GameObject manapun di scene (misal EmailManager).
/// Tidak butuh prefab, tidak butuh ScrollView.
/// Langsung referensikan EmailItem dan field detail dari Hierarchy.
/// </summary>
public class EmailRandomizer : MonoBehaviour
{
    // ─── Panel ────────────────────────────────────────────────────────────────
    [Header("=== Panel ===")]
    public GameObject panelInbox;    // EmailList
    public GameObject panelDetail;   // EmailDetailPanel

    // ─── Data ─────────────────────────────────────────────────────────────────
    [Header("=== Data Email ===")]
    [Tooltip("Assign EmailData ScriptableObject asset di sini.")]
    public EmailData emailData;
    public int minPhishing = 1;
    public int minNormal   = 1;

    // ─── Email Items di Hierarchy ─────────────────────────────────────────────
    [Header("=== Email Items (drag dari Hierarchy) ===")]
    [Tooltip("Drag EmailItem (1) s/d (6) dari Hierarchy ke sini, urut dari atas.")]
    public List<GameObject> emailItems;

    // ─── Email Detail UI ──────────────────────────────────────────────────────
    [Header("=== Email Detail UI ===")]
    [Tooltip("Topbar → Text (TMP)")]
    public TMP_Text txtTopbarSubjek;

    [Tooltip("Sender → SenderInformation")]
    public TMP_Text txtSenderInformation;

    [Tooltip("Messages → Title → Text (TMP)")]
    public TMP_Text txtTitleHeader;

    [Tooltip("Messages → BodyContent → Text (TMP)")]
    public TMP_Text txtBodyContent;

    // ─── Runtime ──────────────────────────────────────────────────────────────
    private List<EmailEntry> _activeEmails = new();
    private EmailEntry       _emailTerbuka;
    private int              _indexTerbuka = -1;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        GenerateEmailList();
    }

    // =========================================================================
    // PUBLIC
    // =========================================================================

    public void TutupDetailEmail()
    {
        if (panelDetail != null) panelDetail.SetActive(false);
        if (panelInbox  != null) panelInbox.SetActive(true);
    }

    public void BukaDetailEmailByIndex(int index)
    {
        if (index < 0 || index >= _activeEmails.Count) return;

        _indexTerbuka = index;
        _emailTerbuka = _activeEmails[index];
        IsiPanelDetail(_emailTerbuka);

        if (panelDetail != null) panelDetail.SetActive(true);
        if (panelInbox  != null) panelInbox.SetActive(false);
    }

    /// <summary>Dipakai oleh EmailDetailButtons untuk cek isPhishing.</summary>
    public EmailEntry GetEmailTerbuka() => _emailTerbuka;

    /// <summary>
    /// Hapus email yang sedang dibuka dari daftar aktif,
    /// sembunyikan EmailItem-nya, lalu kembali ke inbox.
    /// </summary>
    public void HapusEmailTerbuka()
    {
        if (_indexTerbuka < 0 || _indexTerbuka >= _activeEmails.Count) return;

        // Sembunyikan EmailItem di Hierarchy
        // Cari EmailItem mana yang menampilkan email di index ini
        // dengan cara re-render ulang list tanpa email tersebut
        _activeEmails.RemoveAt(_indexTerbuka);
        _emailTerbuka = null;
        _indexTerbuka = -1;

        RefreshTampilanList();
        TutupDetailEmail();
    }

    // =========================================================================
    // RANDOMIZER
    // =========================================================================

    public void GenerateEmailList()
    {
        if (emailData == null || emailData.semuaEmail == null || emailData.semuaEmail.Length == 0)
        {
            Debug.LogError("[EmailRandomizer] EmailData belum di-assign atau kosong!");
            return;
        }

        if (emailItems == null || emailItems.Count == 0)
        {
            Debug.LogError("[EmailRandomizer] emailItems kosong! Drag EmailItem dari Hierarchy.");
            return;
        }

        _activeEmails = PilihEmailSeimbang(emailData.semuaEmail, emailItems.Count, minPhishing, minNormal);
        Acak(_activeEmails);

        for (int i = 0; i < emailItems.Count; i++)
        {
            if (emailItems[i] == null) continue;

            if (i < _activeEmails.Count)
            {
                emailItems[i].SetActive(true);
                IsiBarisList(emailItems[i], _activeEmails[i]);
                SetupTombolBaris(emailItems[i], i);
            }
            else
            {
                emailItems[i].SetActive(false);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Refresh tampilan list setelah ada email yang dihapus
    // ─────────────────────────────────────────────────────────────────────────
    private void RefreshTampilanList()
    {
        for (int i = 0; i < emailItems.Count; i++)
        {
            if (emailItems[i] == null) continue;

            if (i < _activeEmails.Count)
            {
                emailItems[i].SetActive(true);
                IsiBarisList(emailItems[i], _activeEmails[i]);
                SetupTombolBaris(emailItems[i], i);
            }
            else
            {
                // Tidak ada data lagi untuk slot ini — sembunyikan
                emailItems[i].SetActive(false);
            }
        }
    }


    // ─────────────────────────────────────────────────────────────────────────
    private void IsiBarisList(GameObject item, EmailEntry entry)
    {
        foreach (TMP_Text tmp in item.GetComponentsInChildren<TMP_Text>())
        {
            switch (tmp.gameObject.name)
            {
                case "SenderText":  tmp.text = entry.namaPengirim; break;
                case "SubjectText": tmp.text = entry.subjek;       break;
                case "TimeText":    tmp.text = entry.waktu;        break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pasang listener klik ke Button di EmailItem
    // ─────────────────────────────────────────────────────────────────────────
    private void SetupTombolBaris(GameObject item, int index)
    {
        Button btn = item.GetComponent<Button>();
        if (btn == null) btn = item.GetComponentInChildren<Button>();

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => BukaDetailEmailByIndex(index));
        }
        else
        {
            Debug.LogWarning($"[EmailRandomizer] Tidak ada Button di EmailItem index {index}!");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Isi panel detail sesuai struktur Hierarchy
    // ─────────────────────────────────────────────────────────────────────────
    private void IsiPanelDetail(EmailEntry entry)
    {
        if (entry == null) return;

        // Topbar → Text (TMP) = subjek email
        if (txtTopbarSubjek != null)
            txtTopbarSubjek.text = entry.subjek;

        // Sender → SenderInformation = "Nama <email>"
        if (txtSenderInformation != null)
            txtSenderInformation.text = $"{entry.namaPengirim} <{entry.emailPengirim}>";

        // Messages → Title → Text (TMP) = teks header
        if (txtTitleHeader != null)
            txtTitleHeader.text = entry.teksHeader;

        // Messages → BodyContent → Text (TMP) = isi email
        if (txtBodyContent != null)
            txtBodyContent.text = entry.isiEmail;
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private List<EmailEntry> PilihEmailSeimbang(EmailEntry[] pool, int total, int minPhish, int minNorm)
    {
        var phishing = new List<EmailEntry>();
        var normal   = new List<EmailEntry>();

        foreach (var e in pool)
        {
            if (e.isPhishing) phishing.Add(e);
            else              normal.Add(e);
        }

        Acak(phishing);
        Acak(normal);

        var hasil = new List<EmailEntry>();

        int ambilPhish = Mathf.Min(minPhish, phishing.Count);
        int ambilNorm  = Mathf.Min(minNorm,  normal.Count);

        for (int i = 0; i < ambilPhish; i++) hasil.Add(phishing[i]);
        for (int i = 0; i < ambilNorm;  i++) hasil.Add(normal[i]);

        var sisa = new List<EmailEntry>();
        for (int i = ambilPhish; i < phishing.Count; i++) sisa.Add(phishing[i]);
        for (int i = ambilNorm;  i < normal.Count;   i++) sisa.Add(normal[i]);
        Acak(sisa);

        int butuh = total - hasil.Count;
        for (int i = 0; i < butuh && i < sisa.Count; i++)
            hasil.Add(sisa[i]);

        return hasil;
    }

    private static void Acak<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
