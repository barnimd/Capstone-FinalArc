using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class PlayerRunFact
{
    public string key;
    public string value;
}

[Serializable]
public class PlayerRunDecision
{
    public string eventId;
    public string choiceId;
    public string outcomeId;
    public int scoreDelta;
    public float elapsedSeconds;
    public List<PlayerRunFact> facts = new List<PlayerRunFact>();
}

[Serializable]
public class PlayerRunSnapshot
{
    public string runId;
    public string contentVersion;
    public string stageId;
    public int score;
    public int maxScore;
    public float durationSeconds;
    public List<PlayerRunDecision> decisions = new List<PlayerRunDecision>();
}

/// <summary>
/// Records a privacy-safe timeline of gameplay choices for the post-game AI coach.
/// It never records free-form credentials, email bodies, or player chat text.
/// </summary>
public class PlayerRunRecorder : MonoBehaviour
{
    private const int MaxDecisions = 100;
    private const int MaxFactsPerDecision = 12;
    private const string ContentVersion = "v2-2026-08-04";
    public static PlayerRunRecorder Instance { get; private set; }

    private PlayerRunSnapshot _run;
    private float _startedAt;
    private bool _completed;

    public PlayerRunSnapshot Current => _run;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    public static PlayerRunRecorder Ensure(string stageId = null)
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("PlayerRunRecorder");
            Instance = go.AddComponent<PlayerRunRecorder>();
        }

        string resolved = string.IsNullOrWhiteSpace(stageId) ? ResolveStageId() : stageId;
        if (!string.IsNullOrWhiteSpace(resolved))
            Instance.BeginRun(resolved);
        return Instance;
    }

    public void BeginRun(string stageId)
    {
        string normalized = NormalizeId(stageId);
        if (string.IsNullOrEmpty(normalized))
            return;
        if (_run != null && _run.stageId == normalized)
            return;

        _run = new PlayerRunSnapshot
        {
            runId = Guid.NewGuid().ToString(),
            contentVersion = ContentVersion,
            stageId = normalized,
            decisions = new List<PlayerRunDecision>()
        };
        _startedAt = Time.time;
        _completed = false;
        Debug.Log($"[RunRecorder] Started {_run.stageId} run {_run.runId}");
    }

    public static void Record(string eventId, string choiceId)
    {
        PlayerRunRecorder recorder = Ensure();
        recorder?.RecordDecision(eventId, choiceId, null, 0, null);
    }

    public static void RecordDetailed(
        string eventId,
        string choiceId,
        string outcomeId,
        int scoreDelta,
        params string[] factPairs)
    {
        PlayerRunRecorder recorder = Ensure();
        recorder?.RecordDecision(eventId, choiceId, outcomeId, scoreDelta, factPairs);
    }

    public void RecordDecision(
        string eventId,
        string choiceId,
        string outcomeId,
        int scoreDelta,
        string[] factPairs)
    {
        if (_run == null || _completed || _run.decisions.Count >= MaxDecisions)
            return;

        string safeEvent = NormalizeId(eventId);
        string safeChoice = NormalizeId(choiceId);
        string safeOutcome = NormalizeId(outcomeId);
        if (string.IsNullOrEmpty(safeEvent) || string.IsNullOrEmpty(safeChoice))
            return;

        List<PlayerRunFact> safeFacts = new List<PlayerRunFact>();
        if (factPairs != null)
        {
            int pairCount = Mathf.Min(factPairs.Length / 2, MaxFactsPerDecision);
            for (int i = 0; i < pairCount; i++)
            {
                string key = NormalizeId(factPairs[i * 2]);
                string value = NormalizeId(factPairs[i * 2 + 1]);
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    safeFacts.Add(new PlayerRunFact { key = key, value = value });
            }
        }

        _run.decisions.Add(new PlayerRunDecision
        {
            eventId = safeEvent,
            choiceId = safeChoice,
            outcomeId = safeOutcome,
            scoreDelta = Mathf.Clamp(scoreDelta, -100, 100),
            facts = safeFacts,
            elapsedSeconds = Mathf.Max(0f, Time.time - _startedAt)
        });
    }

    public PlayerRunSnapshot CompleteRun(int score, int maxScore, float durationSeconds)
    {
        if (_run == null)
            BeginRun(ResolveStageId());
        if (_run == null)
            return null;

        _run.score = Mathf.Clamp(score, 0, Mathf.Max(1, maxScore));
        _run.maxScore = Mathf.Max(1, maxScore);
        _run.durationSeconds = Mathf.Max(0f, durationSeconds);
        _completed = true;
        return _run;
    }

    public static void ResetCurrentRun()
    {
        if (Instance == null)
            return;
        Instance._run = null;
        Instance._completed = false;
        Instance._startedAt = 0f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "All_Menu" || scene.name.Contains("Dashboard"))
            ResetCurrentRun();
    }

    public static string ResolveStageId()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        string scene = activeScene.name;
        string path = (activeScene.path ?? "").Replace('\\', '/');
        if (path.Contains("/Game Topic 1/")) return "phishing";
        if (path.Contains("/Game Topic 2/")) return "2fa";
        if (path.Contains("/Game Topic 3/")) return "password-security";
        if (path.Contains("/Game Topic 4/")) return "malware-awareness";
        if (path.Contains("/Game Topic 5/")) return "wifi-security";
        if (path.Contains("/Game Topic 6/")) return "ransomware";
        switch (scene)
        {
            case "Privasi_Keamanan":
            case "Office_Level1_Prototype2": return "phishing";
            case "Office_Environment": return "2fa";
            case "Map_Topic_3":
            case "Email_Layout": return "password-security";
            case "Map_Topic4": return "malware-awareness";
            case "Map_Topic_5": return "wifi-security";
            case "Map_Topic6":
            case "computer_interaction": return "ransomware";
            default: return null;
        }
    }

    private static string NormalizeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        StringBuilder builder = new StringBuilder(80);
        foreach (char raw in value.Trim().ToLowerInvariant())
        {
            char c = char.IsLetterOrDigit(raw) || raw == '.' || raw == ':' || raw == '-' ? raw : '_';
            if (builder.Length == 0 && !char.IsLetterOrDigit(c))
                continue;
            if (builder.Length >= 80)
                break;
            if (c == '_' && builder.Length > 0 && builder[builder.Length - 1] == '_')
                continue;
            builder.Append(c);
        }
        return builder.Length > 0 ? builder.ToString() : null;
    }
}
