using System;
using UnityEngine;

/// <summary>
/// Coordinates stage entry/completion/restart. Wraps SaveSystem and translates server
/// state into a single StageEntryDecision so stage UI can show the right buttons.
///
/// Notion mapping:
///   • isCompleted=false, hasCheckpoint=false → Start
///   • isCompleted=false, hasCheckpoint=true  → Continue (+ Restart)
///   • isCompleted=true,  hasCheckpoint=false → Replay (shows best score)
///   • isCompleted=true,  hasCheckpoint=true  → ReplayContinue (+ Restart)
///   • network down with local cache          → OfflineContinue (best-effort)
///   • network down without cache             → Blocked
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Call this when the player opens the stage selector / lobby screen for a stage.
    /// The decision tells the UI which buttons to show.
    /// </summary>
    public void EnterStage(string stageId, Action<StageEntryDecision> callback)
    {
        if (string.IsNullOrEmpty(stageId)) { callback?.Invoke(StageEntryDecision.Blocked(stageId, "stageId empty")); return; }
        if (SaveSystem.Instance == null)   { callback?.Invoke(StageEntryDecision.Blocked(stageId, "SaveSystem missing")); return; }

        SaveSystem.Instance.LoadCheckpoint(stageId, result =>
        {
            StageEntryDecision decision = new StageEntryDecision
            {
                StageId           = stageId,
                BestScore         = result.BestScore,
                CompletedAt       = result.CompletedAt,
                CheckpointJson    = result.BestCheckpointJson,
                FromNetwork       = result.Status != LoadStatus.NetworkError,
            };

            switch (result.Status)
            {
                case LoadStatus.Fresh:
                    decision.Mode = StageEntryMode.Start;
                    break;
                case LoadStatus.HasCheckpoint:
                    decision.Mode = StageEntryMode.Continue;
                    break;
                case LoadStatus.Completed:
                    decision.Mode = StageEntryMode.Replay;
                    break;
                case LoadStatus.CompletedWithCheckpoint:
                    decision.Mode = StageEntryMode.ReplayContinue;
                    break;
                case LoadStatus.NetworkError:
                    decision.Mode = result.HasLocalCheckpoint
                        ? StageEntryMode.OfflineContinue
                        : StageEntryMode.Blocked;
                    decision.Error = "Network unreachable";
                    break;
            }

            callback?.Invoke(decision);
        });
    }

    /// <summary>Save mid-stage checkpoint. Convenience wrapper.</summary>
    public void SaveCheckpoint(string stageId, object data, Action<bool> onServerDone = null)
    {
        if (SaveSystem.Instance == null) { onServerDone?.Invoke(false); return; }
        SaveSystem.Instance.SaveCheckpoint(stageId, data, onServerDone);
    }

    /// <summary>Manual restart: clears active checkpoint (local + server). UI: "Restart from beginning".</summary>
    public void Restart(string stageId, Action<bool> callback = null)
    {
        if (SaveSystem.Instance == null) { callback?.Invoke(false); return; }
        SaveSystem.Instance.RestartStage(stageId, callback);
    }

    /// <summary>
    /// Player finished the stage. On success, server keeps highest of (existing best, new score)
    /// and the active checkpoint is dropped.
    /// </summary>
    public void Complete(string stageId, int finalScore, Action<bool> callback = null)
    {
        if (SaveSystem.Instance == null) { callback?.Invoke(false); return; }
        SaveSystem.Instance.CompleteStage(stageId, finalScore, callback);
    }

    /// <summary>
    /// Submit a score from a replay session. Only valid if stage already completed once.
    /// Note: Complete() already keeps highest — use this only if you want individual score history.
    /// </summary>
    public void SubmitReplayScore(string stageId, int score, Action<bool> callback = null)
    {
        if (SaveSystem.Instance == null) { callback?.Invoke(false); return; }
        SaveSystem.Instance.SubmitReplayScore(stageId, score, callback);
    }
}

// ── DTOs ────────────────────────────────────────────────────────────────────

public enum StageEntryMode
{
    Start,             // never played, no checkpoint → start fresh
    Continue,          // mid-stage, no completion → resume from checkpoint
    Replay,            // stage done, no active checkpoint → replay from start
    ReplayContinue,    // stage done + mid-replay → resume replay (or restart)
    OfflineContinue,   // server unreachable but local cache exists
    Blocked,           // can't proceed (no auth, no network, no cache)
}

[Serializable]
public class StageEntryDecision
{
    public string         StageId;
    public StageEntryMode Mode;
    public int?           BestScore;
    public string         CompletedAt;
    public string         CheckpointJson;  // null if no checkpoint
    public bool           FromNetwork;     // false if served from local cache
    public string         Error;

    public bool HasCheckpoint => !string.IsNullOrEmpty(CheckpointJson);
    public bool IsCompleted   => Mode == StageEntryMode.Replay || Mode == StageEntryMode.ReplayContinue;

    public static StageEntryDecision Blocked(string stageId, string error)
        => new StageEntryDecision { StageId = stageId, Mode = StageEntryMode.Blocked, Error = error };
}
