using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum Tp6State
{
    Organize, Recovery, Crash, Ransomware,
    BackupQuestion, RecoveryChoice, BackupSetup,
    Simulation,   // FIX R7-A: state ini sekarang ditangani
    Success, Failed
}

public class GameManager_Tp6 : MonoBehaviour
{
    public static GameManager_Tp6 Instance { get; private set; }

    [Header("=== Canvas ===")]
    public GameObject desktopCanvas;
    public bool startOnSceneStart = true;

    [Header("=== Controllers ===")]
    public ExplorerController explorer;
    public TrashController trash;
    public CrashController crash;
    public RansomwareController ransomware;
    public BackupController backup;
    public EvaluationManager_Tp6 evaluation;

    [Header("=== Panels ===")]
    public GameObject feedbackPanel;
    public GameObject summaryCanvas;

    [Header("=== UI ===")]
    public ObjectiveUI_Tp4 objectiveUI;

    [Header("=== Score ===")]
    public int scoreOrganize = 15, scoreRecovery = 15, scoreBackupYes = 20,
               scoreRestore = 20, scoreSetup = 20;

    [Header("=== Timing ===")]
    public float fbDuration = 2.5f;

    // FIX R7-A: simpan konfigurasi backup yang disubmit player untuk dipakai di Simulation
    private bool _backupConfigCorrect;

    private Tp6State _state;
    private int _score;
    private bool _started;

    // FIX R5: referensi ke tombol OK di feedbackPanel agar bisa di-disable saat auto-dismiss
    private Button _feedbackOkBtn;

    void Awake()
    {
        Instance = this;

        // Wire file explorer icon clicks
        var dc = desktopCanvas?.GetComponent<Canvas>();
        if (dc) foreach (Button b in dc.GetComponentsInChildren<Button>(true))
        {
            if (b.name.ToLower().Contains("file") && b.name.ToLower().Contains("explorer"))
            { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => explorer.ShowExplorer()); }
            if (b.name.ToLower().Contains("recycle"))
            { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => trash.ShowTrash()); }
        }

        // FIX R5: cache referensi tombol OK feedbackPanel
        if (feedbackPanel)
        {
            var okBtn = feedbackPanel.transform.Find("BtnFeedbackOk");
            if (okBtn) _feedbackOkBtn = okBtn.GetComponent<Button>();
        }
    }

    void Start()
    {
        if (desktopCanvas) desktopCanvas.SetActive(startOnSceneStart);
        if (feedbackPanel) feedbackPanel.SetActive(false);
        if (summaryCanvas) summaryCanvas.SetActive(false);

        if (startOnSceneStart)
            BeginDesktopFlow();
    }

    void Update()
    {
        // Saat startOnSceneStart = false, desktopCanvas diaktifkan dari luar
        // (InteractableTrigger tombol E). Mulai flow begitu canvas aktif.
        if (!_started && desktopCanvas && desktopCanvas.activeInHierarchy)
            BeginDesktopFlow();
    }

    public void BeginDesktopFlow()
    {
        if (_started)
            return;

        _started = true;
        if (desktopCanvas) desktopCanvas.SetActive(true);
        GoToState(Tp6State.Organize);
    }

    public void GoToState(Tp6State s)
    {
        Debug.Log($"[Tp6] GoToState({s}) — _score saat ini = {_score}");
        _state = s;
        switch (s)
        {
            // ── Round 1: Atur file ──────────────────────────────────────
            case Tp6State.Organize:
                SetObj("Buka File Explorer dan atur file ke folder yang sesuai");
                explorer.ShowExplorer();
                break;

            // ── Round 2: Recovery / cari file hilang ───────────────────
            case Tp6State.Recovery:
                Add(scoreOrganize);
                SetObj("File penting hilang! Buka Trash untuk recovery");
                trash.ShowTrash();
                break;

            // ── Round 3: System crash ───────────────────────────────────
            case Tp6State.Crash:
                Add(scoreRecovery);

                // Pastikan objek tempat script CrashController menempel aktif
                if (crash != null)
                {
                    // Jika kamu menaruh script CrashController di objek 'CrashOverlayCanvas'
                    crash.gameObject.SetActive(true);

                    // Jalankan animasinya
                    crash.PlayCrash(() => GoToState(Tp6State.Ransomware));
                }
                else
                {
                    // Jika lupa pasang di Inspector, langsung skip ke Ransomware agar tidak macet
                    GoToState(Tp6State.Ransomware);
                }
                break;

            // ── Round 4: Ransomware — semua file terkunci ───────────────
            case Tp6State.Ransomware:
                FileSystemManager.Instance.LockAllFiles();
                explorer.RefreshUI();

                // TAMBAHKAN INI: Pastikan objek ransomware aktif sebelum dipanggil
                if (ransomware != null)
                {
                    ransomware.gameObject.SetActive(true);
                }

                if (trash) trash.LockTrashAfterCrash();
                ransomware.Show(() => GoToState(Tp6State.BackupQuestion));
                break;

            // ── Round 5: Punya backup? ──────────────────────────────────
            case Tp6State.BackupQuestion:
                backup.ShowBackupQuestion();
                break;

            // ── Round 6: Pilih sumber recovery ─────────────────────────
            case Tp6State.RecoveryChoice:
                Add(scoreBackupYes);
                backup.ShowRecoveryChoice();
                break;

            // ── Round 6→7: Restore & setup backup ──────────────────────
            case Tp6State.BackupSetup:
                Add(scoreRestore);
                StartCoroutine(RestoreSetup());
                break;

            // ── Round 8: Simulasi validasi konfigurasi ──────────────────
            // FIX R7-A: state Simulation sekarang ditangani secara penuh
            case Tp6State.Simulation:
                StartCoroutine(SimulationRoutine());
                break;

            // ── Final: Success ──────────────────────────────────────────
            case Tp6State.Success:
                Debug.Log($"[Tp6] GoToState(Success) dipanggil! _score sebelum Add = {_score}");
                Add(scoreSetup);
                Debug.Log($"[Tp6] _score setelah Add = {_score}");
                StartCoroutine(FinalRoutine());
                break;

            // ── Final: Failed ───────────────────────────────────────────
            case Tp6State.Failed:
                EndGame(false);
                break;
        }
    }

    // ── FIX R3: dipanggil oleh CrashController tepat setelah animasi crash selesai ──
    public void OnCrashFinished()
    {
        if (trash) trash.LockTrashAfterCrash();
    }

    // ── Round 6: Pulihkan file lalu tampilkan setup backup ──────────────────────────
    IEnumerator RestoreSetup()
    {
        FileSystemManager.Instance.RestoreAllFiles();
        explorer.RefreshUI();
        // FIX R6: tampilkan animasi restore progress sebentar sebelum ke setup
        ShowFB("Restoring Backup...", "Memulihkan data dari backup...", autoClose: false);
        yield return new WaitForSeconds(1.5f);
        ShowFB("Backup Restored!", "Data berhasil dipulihkan.\nSekarang atur jadwal backup rutin.", autoClose: false);
        yield return new WaitForSeconds(fbDuration);
        HideFB();
        backup.ShowBackupSetup();
    }

    // ── Round 8: Simulasi validasi — jalankan "test" lalu putuskan hasil ────────────
    // FIX R7-A: coroutine baru untuk state Simulation
    IEnumerator SimulationRoutine()
    {
        ShowFB("Running Simulation", "Sistem sedang menguji konfigurasi backup kamu...", autoClose: false);
        yield return new WaitForSeconds(2f);

        // Lakukan "simulasi serangan" — kunci file lagi sebentar sebagai efek visual
        FileSystemManager.Instance.LockAllFiles();
        explorer.RefreshUI();
        yield return new WaitForSeconds(0.5f);

        if (_backupConfigCorrect)
        {
            // Config benar: restore berhasil
            FileSystemManager.Instance.RestoreAllFiles();
            explorer.RefreshUI();
            ShowFB("Simulation Passed", "Backup kamu berhasil memulihkan semua data!", autoClose: false);
            yield return new WaitForSeconds(fbDuration);
            HideFB();
            GoToState(Tp6State.Success);
        }
        else
        {
            ShowFB("Your backup is not reliable", "External Drive + Daily schedule = safe.", autoClose: false);
            yield return new WaitForSeconds(fbDuration);
            HideFB();
            GoToState(Tp6State.Failed);
        }
    }

    public void SyncScoreToManager()
    {
        if (ScoreManager.instance != null)
        {
            Debug.Log($"[Tp6] SyncScoreToManager → _score = {_score}"); 
            ScoreManager.instance.score = _score;
        }
    }


    // ── Final: berhasil ─────────────────────────────────────────────────────────────
    IEnumerator FinalRoutine()
    {
        FileSystemManager.Instance.RestoreAllFiles();
        explorer.RefreshUI();

        ShowFB("One backup can save everything", "Desktop kembali normal.", autoClose: false);
        yield return new WaitForSeconds(fbDuration);
        HideFB();

        // Sync game score (90) ke ScoreManager sebelum evaluation menambahkan +10
        SyncScoreToManager();

        if (evaluation != null)
        {
            evaluation.TriggerEvaluation(() =>
            {
                if (objectiveUI) objectiveUI.gameObject.SetActive(false);
                if (desktopCanvas) desktopCanvas.SetActive(false);
                if (summaryCanvas) summaryCanvas.SetActive(true);

                if (ScoreManager.instance != null)
                    ScoreManager.instance.score = Mathf.Clamp(ScoreManager.instance.score, 0, 100);
            });
        }
        else
        {
            if (objectiveUI) objectiveUI.gameObject.SetActive(false);
            if (desktopCanvas) desktopCanvas.SetActive(false);
            if (summaryCanvas) summaryCanvas.SetActive(true);

            if (ScoreManager.instance != null)
                ScoreManager.instance.score = Mathf.Clamp(_score, 0, 100);
        }
    }

    // ── Callbacks dari controller ────────────────────────────────────────────────────
    public void OnImportantLost()
    {
        if (_state == Tp6State.Organize) GoToState(Tp6State.Recovery);
    }

    public void OnRecoveryAttempted()
    {
        // FIX: Izinkan lanjut ke Crash dari state Organize maupun Recovery
        if (_state == Tp6State.Recovery || _state == Tp6State.Organize)
        {
            GoToState(Tp6State.Crash);
        }
    }

    // FIX R5: ShowFB sekarang punya parameter autoClose untuk mengontrol tombol OK
    public void OnBackupQuestionNo()
    {
        ShowFB("Data cannot be recovered", "Tanpa backup, semua data hilang permanen!", autoClose: true);
        Invoke(nameof(DoneFail), fbDuration);
    }

    // FIX R7-A: simpan hasil config, lanjut ke Simulation bukan langsung ke Success/Failed
    public void OnBackupSetupDone(bool correct)
    {
        _backupConfigCorrect = correct;
        // Langsung masuk Simulation — Simulation yang memutuskan Success atau Failed
        GoToState(Tp6State.Simulation);
    }

    void DoneSuccess() { HideFB(); GoToState(Tp6State.Success); }
    void DoneFail()    { HideFB(); GoToState(Tp6State.Failed); }

    void EndGame(bool success)
    {
        if (objectiveUI) objectiveUI.gameObject.SetActive(false);
        if (desktopCanvas) desktopCanvas.SetActive(false);
        if (ScoreManager.instance != null) ScoreManager.instance.score = Mathf.Max(5, _score);
        if (summaryCanvas && !success) summaryCanvas.SetActive(true);
        Debug.Log($"[Tp6] {(success ? "SUCCESS" : "FAILED")} Score={_score}");
    }

    // ── FIX R5: ShowFB kini menerima flag autoClose ──────────────────────────────────
    // autoClose = true  → tombol OK tetap aktif (player bisa dismiss sendiri)
    // autoClose = false → tombol OK di-disable (dismissal dikontrol oleh coroutine/Invoke)
    void ShowFB(string t, string d, bool autoClose = false)
    {
        if (!feedbackPanel) return;
        var tt = feedbackPanel.transform.Find("txtFeedbackTitle")?.GetComponent<TMP_Text>();
        var td = feedbackPanel.transform.Find("txtFeedbackDetail")?.GetComponent<TMP_Text>();
        if (tt) tt.text = t;
        if (td) td.text = d;

        // Caching jika belum ada
        if (_feedbackOkBtn == null)
        {
            var okBtn = feedbackPanel.transform.Find("BtnFeedbackOk");
            if (okBtn) _feedbackOkBtn = okBtn.GetComponent<Button>();
        }

        // FIX R5: nonaktifkan OK jika dismissal otomatis, aktifkan jika manual
        if (_feedbackOkBtn) _feedbackOkBtn.interactable = autoClose;

        feedbackPanel.SetActive(true);
        feedbackPanel.transform.SetAsLastSibling();
    }

    void HideFB()
    {
        if (!feedbackPanel) return;
        // Aktifkan kembali tombol OK untuk feedback berikutnya
        if (_feedbackOkBtn) _feedbackOkBtn.interactable = true;
        feedbackPanel.SetActive(false);
    }

    void Add(int v) { _score += v; }
    void SetObj(string t)
    {
        if (!objectiveUI) return;
        // ObjectiveUI bisa dalam keadaan nonaktif (mis. belum pernah dibuka /
        // dimatikan di akhir game) — aktifkan dulu agar coroutine-nya bisa jalan.
        if (!objectiveUI.gameObject.activeSelf)
            objectiveUI.gameObject.SetActive(true);
        objectiveUI.ShowObjective(t);
    }
}
