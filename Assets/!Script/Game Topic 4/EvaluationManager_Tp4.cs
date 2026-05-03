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

        if (evaluationCanvas != null)
            evaluationCanvas.SetActive(true);

        if (evaluationPanel != null && evaluationData != null)
            evaluationPanel.MulaiEvaluasi(evaluationData);
        else
            Debug.LogError("[EvaluationManager_Tp4] evaluationPanel atau evaluationData belum di-assign!");
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
