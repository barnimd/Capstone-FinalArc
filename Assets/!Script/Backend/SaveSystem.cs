using System;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Hybrid checkpoint persistence: PlayerPrefs (instant) + Neon via APIClient (source of truth).
///
/// Flow:
///   SaveCheckpoint(stageId, data)
///     → write to PlayerPrefs immediately (zero latency)
///     → background POST /api/checkpoint/save (retry next save if it fails)
///
///   LoadCheckpoint(stageId, onLoaded)
///     → try GET /api/checkpoint/load (server is truth)
///     → on network failure, fall back to PlayerPrefs cache
///
/// Per-stage: one active checkpoint slot per (user, stage). Cleared on complete or restart.
/// </summary>
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private const string PlayerPrefsPrefix = "SecMind.Checkpoint.";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Save checkpoint locally (instant) and to server (background).
    /// `data` can be any serializable object — will be JSON-encoded via Newtonsoft.
    /// onServerDone fires after the network call (true = saved server-side).
    /// </summary>
    public void SaveCheckpoint(string stageId, object data, Action<bool> onServerDone = null)
    {
        if (string.IsNullOrEmpty(stageId) || data == null)
        {
            Debug.LogWarning("[SaveSystem] SaveCheckpoint: stageId or data empty");
            onServerDone?.Invoke(false);
            return;
        }

        // 1. Write to PlayerPrefs immediately
        string json = JsonConvert.SerializeObject(data);
        PlayerPrefs.SetString(KeyFor(stageId), json);
        PlayerPrefs.Save();

        // 2. Background sync to server
        if (APIClient.Instance == null || FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogWarning("[SaveSystem] SaveCheckpoint: not authenticated — kept local copy only");
            onServerDone?.Invoke(false);
            return;
        }

        APIClient.Instance.CheckpointSave(stageId, data, (ok, resp, raw) =>
        {
            if (ok && resp.success)
            {
                Debug.Log($"[SaveSystem] Server save OK — stage={stageId} id={resp.checkpointId} at={resp.savedAt}");
                onServerDone?.Invoke(true);
            }
            else
            {
                Debug.LogWarning($"[SaveSystem] Server save FAILED for stage={stageId}: {raw} (local copy retained)");
                onServerDone?.Invoke(false);
            }
        });
    }

    /// <summary>
    /// Loads checkpoint + completion status for a stage. Server is source of truth;
    /// if network fails, falls back to PlayerPrefs local cache (status = HasCheckpoint, only checkpoint populated).
    /// </summary>
    public void LoadCheckpoint(string stageId, Action<LoadResult> callback)
    {
        if (string.IsNullOrEmpty(stageId))
        {
            callback?.Invoke(LoadResult.NetworkErrorResult());
            return;
        }

        if (APIClient.Instance == null || FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            // Not logged in — offline-only fallback
            string local = PlayerPrefs.GetString(KeyFor(stageId), "");
            if (!string.IsNullOrEmpty(local))
            {
                callback?.Invoke(new LoadResult
                {
                    Status                = LoadStatus.NetworkError,
                    HasLocalCheckpoint    = true,
                    LocalCheckpointJson   = local,
                });
            }
            else
            {
                callback?.Invoke(LoadResult.NetworkErrorResult());
            }
            return;
        }

        APIClient.Instance.CheckpointLoad(stageId, (ok, resp, raw) =>
        {
            if (!ok || resp == null)
            {
                // Network failure → try local
                string local = PlayerPrefs.GetString(KeyFor(stageId), "");
                callback?.Invoke(new LoadResult
                {
                    Status                = LoadStatus.NetworkError,
                    HasLocalCheckpoint    = !string.IsNullOrEmpty(local),
                    LocalCheckpointJson   = local,
                });
                return;
            }

            LoadStatus status;
            if (resp.isCompleted && resp.hasCheckpoint)        status = LoadStatus.CompletedWithCheckpoint;
            else if (resp.isCompleted)                          status = LoadStatus.Completed;
            else if (resp.hasCheckpoint)                        status = LoadStatus.HasCheckpoint;
            else                                                status = LoadStatus.Fresh;

            // Server returned its checkpoint — refresh local cache so offline next time still works
            string serverJson = null;
            if (resp.checkpoint != null)
            {
                serverJson = resp.checkpoint is string s ? s : JsonConvert.SerializeObject(resp.checkpoint);
                PlayerPrefs.SetString(KeyFor(stageId), serverJson);
                PlayerPrefs.Save();
            }

            callback?.Invoke(new LoadResult
            {
                Status                = status,
                IsCompleted           = resp.isCompleted,
                BestScore             = resp.bestScore,
                CompletedAt           = resp.completedAt,
                HasServerCheckpoint   = resp.hasCheckpoint,
                ServerCheckpointJson  = serverJson,
                CheckpointUpdatedAt   = resp.checkpointUpdatedAt,
                HasLocalCheckpoint    = !string.IsNullOrEmpty(serverJson),
                LocalCheckpointJson   = serverJson,
            });
        });
    }

    /// <summary>Helper to deserialize the JSON returned from LoadCheckpoint into a typed object.</summary>
    public static T DeserializeCheckpoint<T>(string json)
    {
        if (string.IsNullOrEmpty(json)) return default;
        try   { return JsonConvert.DeserializeObject<T>(json); }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveSystem] DeserializeCheckpoint failed: " + e.Message);
            return default;
        }
    }

    /// <summary>Manually clear the checkpoint for a stage (both local + server).</summary>
    public void RestartStage(string stageId, Action<bool> callback = null)
    {
        if (string.IsNullOrEmpty(stageId)) { callback?.Invoke(false); return; }

        PlayerPrefs.DeleteKey(KeyFor(stageId));
        PlayerPrefs.Save();

        if (APIClient.Instance == null || FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogWarning("[SaveSystem] RestartStage: not authenticated — only cleared local cache");
            callback?.Invoke(false);
            return;
        }

        APIClient.Instance.StageRestart(stageId, (ok, resp, raw) =>
        {
            if (ok && resp.success)
            {
                Debug.Log($"[SaveSystem] Restart OK — stage={stageId} serverDeleted={resp.checkpointDeleted}");
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogWarning($"[SaveSystem] Server restart FAILED for stage={stageId}: {raw}");
                callback?.Invoke(false);
            }
        });
    }

    /// <summary>Mark stage complete with final score. Drops the active checkpoint on success.</summary>
    public void CompleteStage(string stageId, int finalScore, Action<bool> callback = null)
    {
        if (string.IsNullOrEmpty(stageId)) { callback?.Invoke(false); return; }

        if (APIClient.Instance == null || FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogWarning("[SaveSystem] CompleteStage: not authenticated");
            callback?.Invoke(false);
            return;
        }

        APIClient.Instance.StageComplete(stageId, finalScore, (ok, resp, raw) =>
        {
            if (ok && resp.success)
            {
                // Server has cleared its checkpoint; mirror locally
                PlayerPrefs.DeleteKey(KeyFor(stageId));
                PlayerPrefs.Save();
                Debug.Log($"[SaveSystem] Complete OK — stage={stageId} finalScore={resp.finalScore}");
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogWarning($"[SaveSystem] Server complete FAILED for stage={stageId}: {raw}");
                callback?.Invoke(false);
            }
        });
    }

    /// <summary>Submit a replay score (stage must already be completed).</summary>
    public void SubmitReplayScore(string stageId, int score, Action<bool> callback = null)
    {
        if (APIClient.Instance == null || FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            callback?.Invoke(false);
            return;
        }
        APIClient.Instance.ScoreSubmit(stageId, score, (ok, resp, raw) =>
        {
            if (ok && resp.success)
            {
                Debug.Log($"[SaveSystem] Replay score submitted — id={resp.scoreId}");
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogWarning($"[SaveSystem] Submit replay score FAILED: {raw}");
                callback?.Invoke(false);
            }
        });
    }

    // ── Internals ───────────────────────────────────────────────────────────

    private string KeyFor(string stageId)
    {
        string uid = FirebaseManager.Instance != null ? FirebaseManager.Instance.LocalId : "anon";
        return $"{PlayerPrefsPrefix}{uid}.{stageId}";
    }
}

// ── DTOs ────────────────────────────────────────────────────────────────────

public enum LoadStatus
{
    Fresh,                    // never played
    HasCheckpoint,            // mid-stage, no completion record
    Completed,                // stage done, no active checkpoint
    CompletedWithCheckpoint,  // stage done, mid-replay
    NetworkError,             // server unreachable; check HasLocalCheckpoint
}

[Serializable]
public class LoadResult
{
    public LoadStatus Status;
    public bool       IsCompleted;
    public int?       BestScore;
    public string     CompletedAt;
    public bool       HasServerCheckpoint;
    public string     ServerCheckpointJson;
    public string     CheckpointUpdatedAt;
    public bool       HasLocalCheckpoint;
    public string     LocalCheckpointJson;

    public string BestCheckpointJson => !string.IsNullOrEmpty(ServerCheckpointJson) ? ServerCheckpointJson : LocalCheckpointJson;

    public static LoadResult NetworkErrorResult() => new LoadResult { Status = LoadStatus.NetworkError };
}

/// <summary>
/// Example schema: stage-specific games extend this or define their own POCO.
/// All fields will be JSON-serialized to Neon's JSONB column.
/// </summary>
[Serializable]
public class CheckpointData
{
    public float    playerX;
    public float    playerY;
    public int      currentScore;
    public int      currentQuestionIndex;
    public int[]    answeredQuestions;
    public float    timeElapsed;
    public string   stageVersion;  // bump if stage layout changes incompatibly
}
