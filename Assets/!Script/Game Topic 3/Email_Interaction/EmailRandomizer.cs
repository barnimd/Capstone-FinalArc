using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmailRandomizer : MonoBehaviour
{
    // ─── Panel ────────────────────────────────────────────────────────────────
    [Header("=== Panel ===")]
    public GameObject panelInbox;
    public GameObject panelDetail;

    // ─── Data ─────────────────────────────────────────────────────────────────
    [Header("=== Data Email ===")]
    public EmailData emailData;
    public int minPhishing = 1;
    public int minNormal   = 1;

    // ─── Email Items di Hierarchy ─────────────────────────────────────────────
    [Header("=== Email Items (drag dari Hierarchy) ===")]
    public List<GameObject> emailItems;

    // ─── Email Detail UI ──────────────────────────────────────────────────────
    [Header("=== Email Detail UI ===")]
    public TMP_Text txtTopbarSubjek;
    public TMP_Text txtSenderInformation;
    public TMP_Text txtTitleHeader;
    public TMP_Text txtBodyContent;

    // ─── Runtime ──────────────────────────────────────────────────────────────
    private List<EmailEntry> _activeEmails = new();
    private EmailEntry       _emailTerbuka;
    private int              _indexTerbuka = -1;

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

        SenderTooltip tooltip = SenderTooltip.Instance ?? FindObjectOfType<SenderTooltip>(true);
        if (tooltip != null)
            tooltip.SetEmailData(_emailTerbuka);
    }

    public EmailEntry GetEmailTerbuka() => _emailTerbuka;

    /// <summary>
    /// Hilangkan email yang sedang dibuka dari list.
    /// Dipanggil setelah player sudah action (Balas/Hapus/Laporkan).
    /// Jika semua email habis → panggil GameplayManager.FinishGame().
    /// </summary>
    public void HilangkanEmailDariList()
    {
        if (_indexTerbuka < 0 || _indexTerbuka >= _activeEmails.Count) return;

        _activeEmails.RemoveAt(_indexTerbuka);
        _emailTerbuka = null;
        _indexTerbuka = -1;

        RefreshTampilanList();
        TutupDetailEmail();

        // Cek apakah semua email sudah selesai
        if (_activeEmails.Count == 0)
        {
            Debug.Log("[EmailRandomizer] Semua email selesai! Trigger FinishGame.");
            GameplayManager gm = FindObjectOfType<GameplayManager>();
            if (gm != null)
                gm.FinishGame();
            else
                Debug.LogError("[EmailRandomizer] GameplayManager tidak ditemukan di scene!");
        }
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
            Debug.LogError("[EmailRandomizer] emailItems kosong!");
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
                emailItems[i].SetActive(false);
            }
        }
    }

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

    private void IsiPanelDetail(EmailEntry entry)
    {
        if (entry == null) return;

        if (txtTopbarSubjek      != null) txtTopbarSubjek.text      = entry.subjek;
        if (txtSenderInformation != null) txtSenderInformation.text = $"{entry.namaPengirim} <{entry.emailPengirim}>";
        if (txtTitleHeader       != null) txtTitleHeader.text       = entry.teksHeader;
        if (txtBodyContent       != null) txtBodyContent.text       = entry.isiEmail;
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
