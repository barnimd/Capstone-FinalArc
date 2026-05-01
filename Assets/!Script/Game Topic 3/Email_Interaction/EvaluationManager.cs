using UnityEngine;

public class EvaluationManager : MonoBehaviour
{
    public static EvaluationManager Instance { get; private set; }

    [Header("=== Evaluation Data ===")]
    public EvaluationData evaluationData;

    [Header("=== Panel Referensi ===")]
    public EvaluationPanel evaluationPanel;
    public GameObject summaryCanvas;

    [Header("=== Score ===")]
    [Tooltip("Skor per jawaban benar dalam evaluasi")]
    public int scorePerCorrect = 20;

    [Header("=== Email Stats ===")]
    [Tooltip("Referensi ke EmailManager untuk baca statistik email")]
    public EmailManager emailManager;

    private bool _evaluationDone = false;

    private void Awake()
    {
        Instance = this;
    }

    public void TriggerEvaluation()
    {
        if (_evaluationDone) return;
        _evaluationDone = true;

        if (evaluationPanel != null && evaluationData != null)
        {
            evaluationPanel.MulaiEvaluasi(evaluationData);
        }
        else
        {
            Debug.LogError("[EvaluationManager] evaluationPanel atau evaluationData belum di-assign!");
            LangsungKeSummary();
        }
    }

    public void SelesaiEvaluasi()
    {
        int correct = evaluationPanel.GetCorrectCount();
        int total = evaluationPanel.GetTotalQuestions();

        int evaluationScore = correct * scorePerCorrect;

        int emailCorrect = 0;
        int emailWrong = 0;
        int emailNeutral = 0;
        if (emailManager != null)
        {
            emailCorrect = emailManager.GetScore(DecisionOutcome.Correct);
            emailWrong   = emailManager.GetScore(DecisionOutcome.Wrong);
            emailNeutral = emailManager.GetScore(DecisionOutcome.Neutral);
        }

        int totalScore = evaluationScore + (emailCorrect * 10) - (emailWrong * 5);

        if (ScoreManager.instance != null)
            ScoreManager.instance.AddScore(totalScore);

        Debug.Log($"[EvaluationManager] Evaluasi selesai! Correct={correct}/{total} | " +
                  $"Email: C={emailCorrect} W={emailWrong} N={emailNeutral} | TotalScore={totalScore}");

        TampilkanSummary();
    }

    private void TampilkanSummary()
    {
        if (summaryCanvas != null)
            summaryCanvas.SetActive(true);
    }

    private void LangsungKeSummary()
    {
        if (summaryCanvas != null)
            summaryCanvas.SetActive(true);
    }
}
