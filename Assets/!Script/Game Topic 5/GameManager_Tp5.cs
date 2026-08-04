using UnityEngine;

public class GameManager_Tp5 : MonoBehaviour
{
    public static GameManager_Tp5 Instance { get; private set; }

    [Header("=== Canvas ===")]
    public GameObject desktopCanvas;
    public GameObject websiteCanvas;

    [Header("=== Controllers ===")]
    public WifiSelectorController wifiController;
    public WebsiteSecurityController websiteController;

    [Header("=== UI ===")]
    public ObjectiveUI_Tp4 objectiveUI;

    [Header("=== Evaluation ===")]
    public EvaluationManager_Tp4 evaluationManager;

    [Header("=== Crash ===")]
    public CrashOverlayController_Tp5 crashOverlay;

    [Header("=== Summary ===")]
    public GameObject summaryCanvas;

    [Header("=== Score ===")]
    public int scoreOnSuccess = 75;

    [Header("=== Backend / Neon ===")]
    [Tooltip("Stage ID untuk disimpan ke Neon. Topic 5 = wifi-security.")]
    public string stageId = "wifi-security";
    [Tooltip("Skor akhir (map + challenge) di-clamp ke [0, maxScore]. Topic 5 max 100.")]
    public int maxScore = 100;

    private bool _wifiDone;
    private bool _websiteDone;
    private bool _finished;

    private void Awake()
    {
        Instance = this;
        PlayerRunRecorder.Ensure(stageId);
    }

    private void Start()
    {
        desktopCanvas.SetActive(true);
        if (websiteCanvas != null) websiteCanvas.SetActive(false);
        if (summaryCanvas != null) summaryCanvas.SetActive(false);

        if (objectiveUI != null)
            objectiveUI.ShowObjective("Connect to a Wi-Fi network to continue.");

        if (wifiController != null)
            wifiController.StartWifiPhase(OnWifiComplete);
    }

    private void OnWifiComplete(bool success)
    {
        if (!success)
        {
            if (crashOverlay != null)
                crashOverlay.PlayCrash(() => EndGame(false));
            else
                EndGame(false);
            return;
        }

        _wifiDone = true;

        if (objectiveUI != null)
            objectiveUI.ShowObjective("Koneksi tidak aman! Aktifkan VPN atau lanjutkan dengan resiko");

        if (desktopCanvas != null) desktopCanvas.SetActive(false);
        if (websiteCanvas != null) websiteCanvas.SetActive(true);

        if (websiteController != null)
            websiteController.StartWebsitePhase(OnWebsiteComplete);
    }

    private void OnWebsiteComplete(bool success)
    {
        _websiteDone = success;

        if (!success)
        {
            if (crashOverlay != null)
                crashOverlay.PlayCrash(() => EndGame(false));
            else
                EndGame(false);
            return;
        }

        // Failsafe: ensure crash canvas is closed
        if (crashOverlay != null && crashOverlay.crashCanvas != null)
            crashOverlay.crashCanvas.SetActive(false);

        // Serahkan evaluasi ke EvaluationManager_Tp4. JANGAN wire ulang btnSelesai dari sini:
        // EvaluationPanel_Tp4.Awake() memasang listener-nya sendiri, dan panel itu baru aktif
        // (jadi Awake-nya baru jalan) setelah MulaiEvaluasi — jadi RemoveAllListeners() dari
        // luar selalu kalah cepat dan skor kuis kehitung dua kali.
        // TriggerEvaluation juga sudah punya failsafe: kalau data/panel kosong, langsung ke summary.
        if (evaluationManager != null)
            evaluationManager.TriggerEvaluation(() => EndGame(true));
        else
            EndGame(true);
    }

    private void EndGame(bool isSuccess)
    {
        if (_finished)
            return;

        _finished = true;
        if (crashOverlay != null && crashOverlay.crashCanvas != null)
            crashOverlay.crashCanvas.SetActive(false);

        if (objectiveUI != null) objectiveUI.gameObject.SetActive(false);
        if (desktopCanvas != null) desktopCanvas.SetActive(false);
        if (websiteCanvas != null) websiteCanvas.SetActive(false);

        // Gabung hasil challenge ke skor map (AddScore — JANGAN timpa biar skor map gak hilang),
        // lalu cap ke maxScore. AddScore juga me-refresh teks HUD ScoreManager.
        if (ScoreManager.instance != null)
        {
            if (isSuccess)
                ScoreManager.instance.AddScore(scoreOnSuccess);
            ScoreManager.instance.ClampScore(0, maxScore);
        }

        // Simpan skor GABUNGAN (map + challenge, sudah di-cap) ke Neon
        int finalScore = ScoreManager.instance != null ? ScoreManager.instance.score : (isSuccess ? scoreOnSuccess : 0);
        if (StageManager.Instance != null)
            StageManager.Instance.SubmitFinalScore(stageId, finalScore, maxScore, "Topic5");
        else
            Debug.LogWarning("[GameManager_Tp5] StageManager tidak ada — skor tidak tersimpan ke Neon (cuma lokal).");

        // Tampilkan panel lewat SummaryManager supaya scoreText-nya kebaca skor gabungan yang benar
        // (panel-nya objek yang sama dengan summaryCanvas). SummaryManager di scene ini stageId-nya
        // kosong, jadi dia cuma menampilkan — TIDAK double-save.
        if (SummaryManager.instance != null)
            SummaryManager.instance.TriggerSummary();
        else if (summaryCanvas != null)
            summaryCanvas.SetActive(true);
    }
}
