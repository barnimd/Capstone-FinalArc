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
    private EvaluationPanel_Tp4 _evalPanel;
    private GameObject _evalCanvas;

    [Header("=== Crash ===")]
    public CrashOverlayController_Tp5 crashOverlay;

    [Header("=== Summary ===")]
    public GameObject summaryCanvas;

    [Header("=== Score ===")]
    public int scoreOnSuccess = 90;

    [Header("=== Backend / Neon ===")]
    [Tooltip("Stage ID untuk disimpan ke Neon. Topic 5 = wifi-security.")]
    public string stageId = "wifi-security";
    [Tooltip("Skor akhir di-clamp ke [0, maxScore] sebelum dikirim ke server.")]
    public int maxScore = 1000;

    private bool _wifiDone;
    private bool _websiteDone;

    private void Awake()
    {
        Instance = this;
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

        // Wire evaluation Selesai button directly
        if (evaluationManager != null)
        {
            _evalCanvas = evaluationManager.evaluationCanvas;
            _evalPanel = evaluationManager.evaluationPanel;
            if (_evalPanel != null && _evalPanel.btnSelesai != null)
            {
                _evalPanel.btnSelesai.onClick.RemoveAllListeners();
                _evalPanel.btnSelesai.onClick.AddListener(OnEvalSelesai);
            }
        }
    }

    private void OnEvalSelesai()
    {
        _evalPanel.evaluationPanelRoot.SetActive(false);
        if (_evalCanvas != null) _evalCanvas.SetActive(false);
        EndGame(true);
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

        // Show evaluation directly, btnSelesai already wired in Start()
        if (_evalCanvas != null && _evalPanel != null && evaluationManager.evaluationData != null)
        {
            _evalCanvas.SetActive(true);
            _evalPanel.MulaiEvaluasi(evaluationManager.evaluationData);
        }
        else
        {
            EndGame(true);
        }
    }

    private void EndGame(bool isSuccess)
    {
        if (crashOverlay != null && crashOverlay.crashCanvas != null)
            crashOverlay.crashCanvas.SetActive(false);

        if (objectiveUI != null) objectiveUI.gameObject.SetActive(false);
        if (desktopCanvas != null) desktopCanvas.SetActive(false);
        if (websiteCanvas != null) websiteCanvas.SetActive(false);

        if (isSuccess && ScoreManager.instance != null)
            ScoreManager.instance.score = scoreOnSuccess;
        else if (ScoreManager.instance != null)
            ScoreManager.instance.score = 0;

        // Simpan skor akhir ke Neon
        int finalScore = ScoreManager.instance != null ? ScoreManager.instance.score : (isSuccess ? scoreOnSuccess : 0);
        if (StageManager.Instance != null)
            StageManager.Instance.SubmitFinalScore(stageId, finalScore, maxScore, "Topic5");
        else
            Debug.LogWarning("[GameManager_Tp5] StageManager tidak ada — skor tidak tersimpan ke Neon (cuma lokal).");

        if (summaryCanvas != null)
            summaryCanvas.SetActive(true);
    }
}
