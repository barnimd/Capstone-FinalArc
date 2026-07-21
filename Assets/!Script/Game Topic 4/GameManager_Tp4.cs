using System.Collections.Generic;
using UnityEngine;

public enum Tp4State { Desktop, WebsiteLogin, Dashboard, Failed, Completed }

public class GameManager_Tp4 : MonoBehaviour
{
    public static GameManager_Tp4 Instance { get; private set; }

    [Header("=== Canvas ===")]
    public GameObject desktopCanvas;
    public GameObject websiteCanvas;

    [Header("=== Controllers ===")]
    public WebsiteLoginController websiteController;
    public DashboardPopupController dashboardController;

    [Header("=== Data ===")]
    public URLData_Tp4 urlData;
    public PopupData_Tp4 popupData;

    [Header("=== Score ===")]
    public int scorePerCorrectLogin = 30;
    public int scorePerCorrectPopup = 15;
    public int penaltyPerWrongPopup = 0;

    [Header("=== Backend / Neon ===")]
    public string stageId = "malware-awareness";
    public int maxScore = 100;

    [Header("=== UI ===")]
    public ObjectiveUI_Tp4 objectiveUI;

    [Header("=== Evaluation ===")]
    public EvaluationManager_Tp4 evaluationManager;

    [Header("=== Crash ===")]
    public CrashOverlayController_Tp4 crashOverlay;

    [Header("=== Summary ===")]
    public GameObject summaryCanvas;

    private Tp4State _currentState = Tp4State.Desktop;
    private int _totalScore = 0;
    private List<URLEntry> _currentRoundURLs;
    private string _gameResultMessage;
    private bool _finished;

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
            objectiveUI.ShowObjective("Buka aplikasi chrome");
    }

    public void OnBrowserIconClicked()
    {
        if (_currentState != Tp4State.Desktop) return;

        _currentState = Tp4State.WebsiteLogin;
        _currentRoundURLs = GenerateURLRound();

        if (desktopCanvas != null) desktopCanvas.SetActive(false);
        if (websiteCanvas != null) websiteCanvas.SetActive(true);

        if (objectiveUI != null)
            objectiveUI.ShowObjective("Round 1 Pilih: lanjut login ke website ini atau hindari");

        websiteController.StartWebsiteSession(_currentRoundURLs, OnWebsiteRoundComplete);
    }

    private List<URLEntry> GenerateURLRound()
    {
        var phishingPool = new List<URLEntry>(urlData.phishingURLs);
        var legitPool = new List<URLEntry>(urlData.legitimateURLs);

        Acak(phishingPool);
        Acak(legitPool);

        int ambilPhish = Mathf.Min(2, phishingPool.Count);
        int ambilLegit = Mathf.Min(1, legitPool.Count);

        var urls = new List<URLEntry>();
        for (int i = 0; i < ambilPhish; i++) urls.Add(phishingPool[i]);
        for (int i = 0; i < ambilLegit; i++) urls.Add(legitPool[i]);

        Acak(urls);
        return urls;
    }

    private void OnWebsiteRoundComplete(bool success, string message)
    {
        if (success)
        {
            _totalScore += scorePerCorrectLogin;
            _gameResultMessage = message;

            if (websiteCanvas != null) websiteCanvas.SetActive(false);

            _currentState = Tp4State.Dashboard;

            if (objectiveUI != null)
                objectiveUI.ShowObjective("Round 2 Pilih: popup aman atau jebakan?");

            dashboardController.StartDashboardSession(OnDashboardComplete);
        }
        else
        {
            _gameResultMessage = message;
            EndGame(false);
        }
    }

    private void OnDashboardComplete()
    {
        int popupCorrect = dashboardController.GetCorrectCount();
        int popupTotal = dashboardController.GetTotalPopups();

        _totalScore += popupCorrect * scorePerCorrectPopup;
        _totalScore += (popupTotal - popupCorrect) * penaltyPerWrongPopup;

        if (objectiveUI != null)
            objectiveUI.gameObject.SetActive(false);

        if (evaluationManager != null)
            evaluationManager.TriggerEvaluation(OnEvaluationComplete);
        else
            EndGame(true);
    }

    private void OnEvaluationComplete()
    {
        EndGame(true);
    }

    private void EndGame(bool isSuccess)
    {
        if (_finished)
            return;

        _finished = true;
        _currentState = isSuccess ? Tp4State.Completed : Tp4State.Failed;

        int finalScore = Mathf.Clamp(
            (ScoreManager.instance != null ? ScoreManager.instance.score : 0) + _totalScore,
            0,
            maxScore);

        if (ScoreManager.instance != null)
            ScoreManager.instance.SetScore(finalScore);

        StageManager.Instance?.SubmitFinalScore(stageId, finalScore, maxScore, "Topic4");

        string status = isSuccess ? "COMPLETED" : "FAILED";
        Debug.Log($"[GameManager_Tp4] {status} | Score: {finalScore} | {_gameResultMessage}");

        if (desktopCanvas != null) desktopCanvas.SetActive(false);
        if (websiteCanvas != null) websiteCanvas.SetActive(false);
        if (objectiveUI != null) objectiveUI.gameObject.SetActive(false);

        if (!isSuccess && crashOverlay != null)
        {
            crashOverlay.PlayCrash(ShowSummary);
        }
        else
        {
            ShowSummary();
        }
    }

    // Shows this station's summary panel with Name / Score / Time / Accuracy filled and the
    // player frozen. Populates directly (each station owns its summaryCanvas) instead of going
    // through the SummaryManager singleton, which isn't safe with 3 stations in one scene.
    private void ShowSummary()
    {
        if (summaryCanvas == null)
            return;

        int finalScore = ScoreManager.instance != null ? ScoreManager.instance.score : 0;
        int accuracy = maxScore > 0 ? Mathf.Clamp(Mathf.RoundToInt(100f * finalScore / maxScore), 0, 100) : 0;
        // Total time from when the player entered Topic 4 (scene load) until the summary shows —
        // not per-station, so it reflects the whole run start-to-finish.
        float elapsed = Time.timeSinceLevelLoad;

        PlayerFreezer.FreezeAll();
        SummaryManager.PopulateSummaryTexts(summaryCanvas, finalScore, elapsed, accuracy, null, "");
        summaryCanvas.SetActive(true);

        // Make sure the inner panel is on (matches SummaryManager's own reveal logic).
        Transform pannel = summaryCanvas.transform.Find("SummaryPannel");
        if (pannel != null) pannel.gameObject.SetActive(true);
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
