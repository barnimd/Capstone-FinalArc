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
    public int scoreOnSuccess = 90;

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

        if (evaluationManager != null)
            evaluationManager.TriggerEvaluation(() => EndGame(true));
        else
            EndGame(true);
    }

    private void EndGame(bool isSuccess)
    {
        if (crashOverlay != null && crashOverlay.crashCanvas != null)
            crashOverlay.crashCanvas.SetActive(false);

        if (objectiveUI != null) objectiveUI.gameObject.SetActive(false);
        if (desktopCanvas != null) desktopCanvas.SetActive(false);
        if (websiteCanvas != null) websiteCanvas.SetActive(false);

        if (isSuccess && ScoreManager.instance != null)
            ScoreManager.instance.AddScore(scoreOnSuccess);

        if (summaryCanvas != null)
            summaryCanvas.SetActive(true);
    }
}
