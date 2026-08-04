using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashboardPopupController : MonoBehaviour
{
    [Header("=== Root Panel ===")]
    public GameObject dashboardPanelRoot;

    [Header("=== Popup UI ===")]
    public GameObject popupPanel;
    public TMP_Text txtPopupTitle;
    public TMP_Text txtPopupMessage;
    public Button btnActionA;
    public Button btnActionB;
    public TMP_Text txtActionA;
    public TMP_Text txtActionB;

    [Header("=== Popup Feedback ===")]
    public GameObject feedbackPanel;
    public TMP_Text txtFeedbackTitle;
    public TMP_Text txtFeedbackDetail;
    public Button btnFeedbackNext;

    [Header("=== Result Panel ===")]
    public GameObject resultPanel;
    public TMP_Text txtResultScore;
    public TMP_Text txtResultDetail;
    public Button btnResultSelesai;

    [Header("=== Data ===")]
    public PopupData_Tp4 popupData;
    [Tooltip("Jumlah popup phishing yang muncul")]
    public int phishingPopupCount = 2;
    [Tooltip("Jumlah popup legitimate yang muncul")]
    public int legitimatePopupCount = 2;

    private List<PopupEntry> _popupQueue;
    private int _currentPopupIndex = 0;
    private int _correctCount = 0;
    private bool[] _playerAnswers;
    private System.Action _onAllPopupsDone;

    private void Awake()
    {
        if (btnActionA != null) btnActionA.onClick.AddListener(() => OnActionClicked(false));
        if (btnActionB != null) btnActionB.onClick.AddListener(() => OnActionClicked(true));
        if (btnFeedbackNext != null) btnFeedbackNext.onClick.AddListener(OnFeedbackNextClicked);
        if (btnResultSelesai != null) btnResultSelesai.onClick.AddListener(OnResultSelesaiClicked);

        if (popupPanel != null) popupPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    public void StartDashboardSession(System.Action onComplete)
    {
        _onAllPopupsDone = onComplete;
        _correctCount = 0;

        _popupQueue = GeneratePopupQueue();
        _playerAnswers = new bool[_popupQueue.Count];
        _currentPopupIndex = 0;

        dashboardPanelRoot.SetActive(true);
        popupPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        resultPanel.SetActive(false);

        TampilkanPopup(_currentPopupIndex);
    }

    private List<PopupEntry> GeneratePopupQueue()
    {
        var phishingPool = new List<PopupEntry>(popupData.phishingPopups);
        var legitPool = new List<PopupEntry>(popupData.legitimatePopups);

        Acak(phishingPool);
        Acak(legitPool);

        var queue = new List<PopupEntry>();

        int ambilPhish = Mathf.Min(phishingPopupCount, phishingPool.Count);
        int ambilLegit = Mathf.Min(legitimatePopupCount, legitPool.Count);

        for (int i = 0; i < ambilPhish; i++) queue.Add(phishingPool[i]);
        for (int i = 0; i < ambilLegit; i++) queue.Add(legitPool[i]);

        Acak(queue);
        return queue;
    }

    private void TampilkanPopup(int index)
    {
        if (index >= _popupQueue.Count) return;

        PopupEntry entry = _popupQueue[index];

        popupPanel.SetActive(true);
        txtPopupTitle.text = entry.title;
        txtPopupMessage.text = entry.message;

        bool isPhishing = entry.isPhishing;
        if (isPhishing)
        {
            txtActionA.text = entry.wrongActionLabel;
            txtActionB.text = entry.correctActionLabel;
        }
        else
        {
            txtActionA.text = entry.wrongActionLabel;
            txtActionB.text = entry.correctActionLabel;
        }
    }

    private void OnActionClicked(bool isCorrectButton)
    {
        PopupEntry entry = _popupQueue[_currentPopupIndex];
        bool answeredCorrectly = isCorrectButton;
        PlayerRunRecorder.Record(
            "popup.round_" + (_currentPopupIndex + 1) + (entry.isPhishing ? ".phishing" : ".legitimate"),
            answeredCorrectly ? "correct" : "incorrect");

        _playerAnswers[_currentPopupIndex] = answeredCorrectly;
        if (answeredCorrectly) _correctCount++;

        string resultTitle = answeredCorrectly ? "BENAR!" : "SALAH!";
        string resultDetail = answeredCorrectly
            ? $"Kamu memilih aksi yang tepat.\n\n{entry.explanation}"
            : $"Kamu memilih aksi yang salah.\n\n{entry.explanation}";

        popupPanel.SetActive(false);
        feedbackPanel.SetActive(true);
        txtFeedbackTitle.text = resultTitle;
        txtFeedbackDetail.text = resultDetail;
    }

    private void OnFeedbackNextClicked()
    {
        feedbackPanel.SetActive(false);

        _currentPopupIndex++;

        if (_currentPopupIndex >= _popupQueue.Count)
        {
            TampilkanHasil();
        }
        else
        {
            TampilkanPopup(_currentPopupIndex);
        }
    }

    private void TampilkanHasil()
    {
        popupPanel.SetActive(false);
        feedbackPanel.SetActive(false);
        resultPanel.SetActive(true);

        int total = _popupQueue.Count;
        txtResultScore.text = $"Skor: {_correctCount} / {total}";

        string detail = "";
        for (int i = 0; i < _popupQueue.Count; i++)
        {
            string icon = _playerAnswers[i] ? "[OK]" : "[X]";
            detail += $"{icon} {_popupQueue[i].title}\n";
        }
        txtResultDetail.text = detail;
    }

    private void OnResultSelesaiClicked()
    {
        resultPanel.SetActive(false);
        dashboardPanelRoot.SetActive(false);
        _onAllPopupsDone?.Invoke();
    }

    public int GetCorrectCount() => _correctCount;
    public int GetTotalPopups() => _popupQueue.Count;

    private static void Acak<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
