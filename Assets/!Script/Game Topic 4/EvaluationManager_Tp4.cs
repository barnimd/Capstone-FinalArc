using UnityEngine;

public class EvaluationManager_Tp4 : MonoBehaviour
{
    public static EvaluationManager_Tp4 Instance { get; private set; }

    [Header("=== Evaluation Data ===")]
    public EvaluationData_Tp4 evaluationData;

    [Header("=== Panel Referensi ===")]
    public EvaluationPanel_Tp4 evaluationPanel;
    public GameObject evaluationCanvas;
    public GameObject summaryCanvas;

    [Header("=== Score ===")]
    public int scorePerCorrect = 2;

    private bool _evaluationDone;
    private System.Action _onComplete;

    private void Awake()
    {
        Instance = this;
    }

    public void TriggerEvaluation(System.Action onComplete)
    {
        if (_evaluationDone) return;
        _evaluationDone = true;

        _onComplete = onComplete;

        // Failsafe: kalau quiz tidak bisa jalan (panel/data hilang atau tidak ada soal),
        // JANGAN gantung game — langsung lanjut ke summary supaya CanvasSummaryPanel
        // tetap muncul. Kalau normal, summary tampil SETELAH player selesai quiz.
        bool canRunQuiz = evaluationPanel != null
                          && evaluationData != null
                          && evaluationData.questions != null
                          && evaluationData.questions.Length > 0;

        if (!canRunQuiz)
        {
            Debug.LogWarning("[EvaluationManager_Tp4] Quiz tidak bisa dimulai " +
                             "(evaluationPanel/evaluationData/questions kosong) — " +
                             "skip langsung ke summary.");
            if (evaluationCanvas != null)
                evaluationCanvas.SetActive(false);
            _onComplete?.Invoke();
            return;
        }

        if (evaluationCanvas != null)
            evaluationCanvas.SetActive(true);

        evaluationPanel.MulaiEvaluasi(evaluationData);
    }

    public void SelesaiEvaluasi()
    {
        int correct = evaluationPanel != null ? evaluationPanel.GetCorrectCount() : 0;
        int total = evaluationPanel != null ? evaluationPanel.GetTotalQuestions() : 0;
        int evaluationScore = correct * scorePerCorrect;

        Debug.Log($"[EvaluationManager_Tp4] Evaluasi selesai! Correct={correct}/{total} | Score={evaluationScore}");

        if (ScoreManager.instance != null)
            ScoreManager.instance.AddScore(evaluationScore);

        if (evaluationCanvas != null)
            evaluationCanvas.SetActive(false);

        _onComplete?.Invoke();
    }
}
