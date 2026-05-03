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
    public int scorePerCorrectLogin = 50;
    public int scorePerCorrectPopup = 20;
    public int penaltyPerWrongPopup = -10;

    [Header("=== Summary ===")]
    public GameObject summaryCanvas;

    private Tp4State _currentState = Tp4State.Desktop;
    private int _totalScore = 0;
    private List<URLEntry> _currentRoundURLs;
    private string _gameResultMessage;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        desktopCanvas.SetActive(true);
        if (websiteCanvas != null) websiteCanvas.SetActive(false);
        if (summaryCanvas != null) summaryCanvas.SetActive(false);
    }

    public void OnBrowserIconClicked()
    {
        if (_currentState != Tp4State.Desktop) return;

        _currentState = Tp4State.WebsiteLogin;
        _currentRoundURLs = GenerateURLRound();

        if (desktopCanvas != null) desktopCanvas.SetActive(false);
        if (websiteCanvas != null) websiteCanvas.SetActive(true);

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

        EndGame(true);
    }

    private void EndGame(bool isSuccess)
    {
        _currentState = isSuccess ? Tp4State.Completed : Tp4State.Failed;

        if (ScoreManager.instance != null)
            ScoreManager.instance.AddScore(_totalScore);

        string status = isSuccess ? "COMPLETED" : "FAILED";
        Debug.Log($"[GameManager_Tp4] {status} | Score: {_totalScore} | {_gameResultMessage}");

        if (desktopCanvas != null) desktopCanvas.SetActive(false);
        if (websiteCanvas != null) websiteCanvas.SetActive(false);

        if (summaryCanvas != null)
            summaryCanvas.SetActive(true);
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
