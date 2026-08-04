using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WebsiteLoginController : MonoBehaviour
{
    [Header("=== Root Panel ===")]
    public GameObject websitePanelRoot;

    [Header("=== URL Display ===")]
    public TMP_Text txtURL;
    public Image urlBarBackground;

    [Header("=== Login Form ===")]
    public TMP_InputField inputUsername;
    public TMP_InputField inputPassword;
    public Button btnLogin;
    public Button btnCancel;

    [Header("=== Feedback Panel ===")]
    public GameObject feedbackPanel;
    public TMP_Text txtFeedbackTitle;
    public TMP_Text txtFeedbackDetail;
    public Button btnFeedbackOk;

    [Header("=== Round Info ===")]
    public TMP_Text txtRoundInfo;

    [Header("=== URL Colors ===")]
    public Color phishingURLColor = new Color(0.9f, 0.2f, 0.2f);
    public Color legitimateURLColor = new Color(0.2f, 0.7f, 0.2f);

    private List<URLEntry> _roundURLs;
    private int _currentRound = 0;
    private int _totalRounds = 3;
    private System.Action<bool, string> _onRoundComplete;

    private void Awake()
    {
        if (btnLogin != null) btnLogin.onClick.AddListener(OnLoginClicked);
        if (btnCancel != null) btnCancel.onClick.AddListener(OnCancelClicked);
        if (btnFeedbackOk != null) btnFeedbackOk.onClick.AddListener(OnFeedbackOkClicked);

        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }

    public void StartWebsiteSession(List<URLEntry> urls, System.Action<bool, string> onComplete)
    {
        _roundURLs = urls;
        _currentRound = 0;
        _totalRounds = urls.Count;
        _onRoundComplete = onComplete;

        websitePanelRoot.SetActive(true);
        feedbackPanel.SetActive(false);
        TampilkanRound(_currentRound);
    }

    private void TampilkanRound(int index)
    {
        if (_roundURLs == null || index >= _roundURLs.Count) return;

        URLEntry entry = _roundURLs[index];

        txtURL.text = entry.url;
        txtRoundInfo.text = $"Round {index + 1} dari {_totalRounds}";

        if (urlBarBackground != null)
            urlBarBackground.color = Color.white;

        inputUsername.text = "";
        inputPassword.text = "";

        btnLogin.interactable = true;
        btnCancel.interactable = true;
    }

    private void OnLoginClicked()
    {
        if (_roundURLs == null || _roundURLs.Count == 0) return;

        URLEntry entry = _roundURLs[_currentRound];
        PlayerRunRecorder.Record(
            "website.round_" + (_currentRound + 1) + (entry.isPhishing ? ".phishing" : ".legitimate"),
            "login");

        if (entry.isPhishing)
        {
            TampilkanFeedback(
                "GAME OVER!",
                $"Kamu login ke website phishing!\n\nURL: {entry.url}\n\n{entry.explanation}"
            );
        }
        else
        {
            TampilkanFeedback(
                "LOGIN BERHASIL!",
                $"Kamu login ke website yang benar!\n\nURL: {entry.url}\n\n{entry.explanation}"
            );
        }
    }

    private void OnCancelClicked()
    {
        if (_roundURLs == null || _roundURLs.Count == 0) return;

        URLEntry entry = _roundURLs[_currentRound];
        PlayerRunRecorder.Record(
            "website.round_" + (_currentRound + 1) + (entry.isPhishing ? ".phishing" : ".legitimate"),
            "cancel");

        _currentRound++;

        if (_currentRound >= _totalRounds)
        {
            TampilkanFeedback(
                "GAME OVER!",
                "Kamu tidak login ke satupun website.\nKamu harus bisa mengidentifikasi website yang legitimate."
            );
        }
        else
        {
            if (entry.isPhishing)
            {
                TampilkanFeedbackInstant(
                    "DIBATALKAN",
                    $"Kamu membatalkan login.\nURL: {entry.url}\n\n{entry.explanation}",
                    () => TampilkanRound(_currentRound)
                );
            }
            else
            {
                TampilkanFeedbackInstant(
                    "DIBATALKAN",
                    $"Kamu membatalkan login.\nURL: {entry.url}\n\nIni adalah website LEGITIMATE! Kamu melewatkan yang benar.",
                    () => TampilkanRound(_currentRound)
                );
            }
        }
    }

    private void TampilkanFeedback(string title, string detail)
    {
        btnLogin.interactable = false;
        btnCancel.interactable = false;

        feedbackPanel.SetActive(true);
        txtFeedbackTitle.text = title;
        txtFeedbackDetail.text = detail;

        bool isFail = title.Contains("GAME OVER");
        _feedbackResultSuccess = !isFail;
        _feedbackDetailMsg = detail;
    }

    private void TampilkanFeedbackInstant(string title, string detail, System.Action onOk)
    {
        feedbackPanel.SetActive(true);
        txtFeedbackTitle.text = title;
        txtFeedbackDetail.text = detail;

        btnFeedbackOk.onClick.RemoveAllListeners();
        btnFeedbackOk.onClick.AddListener(() =>
        {
            feedbackPanel.SetActive(false);
            onOk?.Invoke();
            btnFeedbackOk.onClick.RemoveAllListeners();
            btnFeedbackOk.onClick.AddListener(OnFeedbackOkClicked);
        });
    }

    private bool _feedbackResultSuccess;
    private string _feedbackDetailMsg;

    private void OnFeedbackOkClicked()
    {
        feedbackPanel.SetActive(false);
        websitePanelRoot.SetActive(false);
        _onRoundComplete?.Invoke(_feedbackResultSuccess, _feedbackDetailMsg);
    }

    public void HidePanel()
    {
        websitePanelRoot.SetActive(false);
        feedbackPanel.SetActive(false);
    }
}
