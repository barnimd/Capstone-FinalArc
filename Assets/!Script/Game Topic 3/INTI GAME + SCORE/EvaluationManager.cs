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
    [Tooltip("Maximum score contributed by the evaluation questions.")]
    public int evaluationMaxScore = 50;
    [Tooltip("Maximum score contributed by email decisions.")]
    public int emailMaxScore = 50;

    [Header("=== Backend / Neon ===")]
    [Tooltip("Stage ID untuk disimpan ke Neon. Topic 3 = password-security.")]
    public string stageId = "password-security";
    [Tooltip("Skor akhir di-cap ke nilai ini (semua topik max 100).")]
    public int maxScore = 100;

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

        int evaluationScore = total > 0
            ? Mathf.RoundToInt((float)correct / total * evaluationMaxScore)
            : 0;

        int emailCorrect = 0;
        int emailWrong = 0;
        int emailNeutral = 0;
        if (emailManager != null)
        {
            emailCorrect = emailManager.GetScore(DecisionOutcome.Correct);
            emailWrong   = emailManager.GetScore(DecisionOutcome.Wrong);
            emailNeutral = emailManager.GetScore(DecisionOutcome.Neutral);
        }

        int emailTotal = emailManager != null ? emailManager.GetTotalDecisions() : 0;
        int emailScore = emailTotal > 0
            ? Mathf.RoundToInt((float)emailCorrect / emailTotal * emailMaxScore)
            : 0;
        int totalScore = Mathf.Clamp(evaluationScore + emailScore, 0, maxScore);

        if (ScoreManager.instance != null)
            ScoreManager.instance.SetScore(totalScore);

        int finalScore = ScoreManager.instance != null
            ? ScoreManager.instance.score
            : Mathf.Clamp(totalScore, 0, maxScore);

        Debug.Log($"[EvaluationManager] Evaluasi selesai! Questions={correct}/{total} ({evaluationScore}) | " +
                  $"Email: C={emailCorrect} W={emailWrong} N={emailNeutral} ({emailScore}) | FinalScore={finalScore}");

        // Simpan hasil ke Neon
        if (StageManager.Instance != null)
            StageManager.Instance.SubmitFinalScore(stageId, finalScore, maxScore, "Topic3");
        else
            Debug.LogWarning("[EvaluationManager] StageManager tidak ada — skor tidak tersimpan ke Neon (cuma lokal).");

        TampilkanSummary();
    }

    private void TampilkanSummary()
    {
        // Route through the shared SummaryManager so the panel shows Name / Score / Time /
        // Accuracy and the player is frozen — consistent with every other topic. The score
        // was already written to ScoreManager above, and Neon was already saved via
        // StageManager, so SummaryManager (stageId empty here) won't double-save.
        if (SummaryManager.instance != null)
            SummaryManager.instance.TriggerSummary();
        else if (summaryCanvas != null)
            summaryCanvas.SetActive(true);
    }

    private void LangsungKeSummary()
    {
        if (SummaryManager.instance != null)
            SummaryManager.instance.TriggerSummary();
        else if (summaryCanvas != null)
            summaryCanvas.SetActive(true);
    }
}
