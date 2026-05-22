using UnityEngine;

public class EvaluationManager_Tp6 : MonoBehaviour
{
    public static EvaluationManager_Tp6 Instance { get; private set; }

    public GameObject evaluationCanvas;
    public EvaluationPanel_Tp4 evaluationPanel;
    public EvaluationData_Tp4 evaluationData;

    private System.Action _onComplete;
    private bool _done;

    void Awake()
    {
        Instance = this;
        if (evaluationCanvas) evaluationCanvas.SetActive(false);
    }

    void Start()
    {
        if (evaluationPanel != null && evaluationPanel.btnSelesai != null)
        {
            evaluationPanel.btnSelesai.onClick.RemoveAllListeners();
            evaluationPanel.btnSelesai.onClick.AddListener(SelesaiEvaluasi);
        }
    }

    public void TriggerEvaluation(System.Action onComplete)
    {
        if (_done) return; _done = true;
        _onComplete = onComplete;
        if (evaluationCanvas) evaluationCanvas.SetActive(true);
        if (evaluationPanel != null && evaluationData != null)
            evaluationPanel.MulaiEvaluasi(evaluationData);
        else
            Debug.LogError("[EvaluationManager_Tp6] Missing evaluationData or evaluationPanel!");
    }

    public void SelesaiEvaluasi()
    {
        int correct = evaluationPanel != null ? evaluationPanel.GetCorrectCount() : 0;
        Debug.Log($"[EvaluationManager_Tp6] Eval done! Correct={correct}");

        if (evaluationCanvas) evaluationCanvas.SetActive(false);

        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.AddScore(correct * 2);

            ScoreManager.instance.score =
                Mathf.Clamp(ScoreManager.instance.score, 0, 100);
        }

        _onComplete?.Invoke();
    }
}
