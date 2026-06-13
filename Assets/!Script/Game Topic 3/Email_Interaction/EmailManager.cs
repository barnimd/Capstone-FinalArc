using System.Collections;
using UnityEngine;

public enum PlayerAction    { None, Balas, Hapus, Laporkan }
public enum DecisionOutcome { Correct, Wrong, Neutral }

public class EmailManager : MonoBehaviour
{
    public static EmailManager Instance { get; private set; }

    [Header("Email Canvas")]
    [SerializeField] private GameObject emailCanvas;
    [SerializeField] private float fadeInDuration = 0.3f;

    [Header("Player Decisions (Debug)")]
    [SerializeField] private int scoreCorrect = 0;
    [SerializeField] private int scoreWrong   = 0;
    [SerializeField] private int scoreNeutral = 0;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        Instance = this;

        if (emailCanvas != null)
        {
            _canvasGroup = emailCanvas.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = emailCanvas.AddComponent<CanvasGroup>();
        }
    }

    public void RecordDecision(PlayerAction action, bool isPhishing)
    {
        DecisionOutcome outcome = EvaluateOutcome(action, isPhishing);

        switch (outcome)
        {
            case DecisionOutcome.Correct: scoreCorrect++; break;
            case DecisionOutcome.Wrong:   scoreWrong++;   break;
            case DecisionOutcome.Neutral: scoreNeutral++; break;
        }

        Debug.Log($"[EmailManager] {action} on {(isPhishing ? "PHISHING" : "NORMAL")} → {outcome} | Correct={scoreCorrect}, Wrong={scoreWrong}, Neutral={scoreNeutral}");
    }

    private static DecisionOutcome EvaluateOutcome(PlayerAction action, bool isPhishing)
    {
        if (isPhishing)
        {
            return action switch
            {
                PlayerAction.Laporkan => DecisionOutcome.Correct,
                PlayerAction.Hapus    => DecisionOutcome.Neutral,
                _                     => DecisionOutcome.Wrong    // Balas
            };
        }
        else
        {
            return action switch
            {
                PlayerAction.Balas    => DecisionOutcome.Correct,
                PlayerAction.Laporkan => DecisionOutcome.Neutral,
                _                     => DecisionOutcome.Wrong    // Hapus
            };
        }
    }

    public int GetScore(DecisionOutcome outcome) => outcome switch
    {
        DecisionOutcome.Correct => scoreCorrect,
        DecisionOutcome.Wrong   => scoreWrong,
        DecisionOutcome.Neutral => scoreNeutral,
        _                       => 0
    };

    public int GetTotalDecisions()
    {
        return scoreCorrect + scoreWrong + scoreNeutral;
    }

    public void OnEmailIconClicked()
    {
        if (emailCanvas == null) return;
        StartCoroutine(FadeInEmailCanvas());
    }

    public void CloseEmailCanvas()
    {
        if (emailCanvas != null)
            emailCanvas.SetActive(false);
    }

    private IEnumerator FadeInEmailCanvas()
    {
        _canvasGroup.alpha = 0f;
        emailCanvas.SetActive(true);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
    }
}
