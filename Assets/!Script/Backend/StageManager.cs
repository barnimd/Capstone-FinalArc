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

    // ── Shared "stage finished → save to Neon" helper ─────────────────────────

    /// <summary>
    /// One-call stage completion save used by every topic's end-of-game point.
    /// Clamps the score to [0, maxScore], ensures the Neon users row exists (UserSync),
    /// then marks the stage complete (server keeps the highest score per stage).
    /// Logs every step with the given tag. Safe no-op (logs why) if stageId is empty,
    /// backend managers are missing, or the player isn't authenticated.
    /// </summary>
    public void SubmitFinalScore(string stageId, int rawScore, int maxScore = 100, string tag = "Neon")
    {
        if (string.IsNullOrEmpty(stageId)) return;

        int finalScore = Mathf.Clamp(rawScore, 0, maxScore);
        Debug.Log($"[{tag}] 🏁 Stage '{stageId}' selesai — final score siap (raw={rawScore}, dikirim={finalScore}). Mulai simpan...");

        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogWarning($"[{tag}] ⚠️ Save DIBATALKAN untuk '{stageId}' — belum login / FirebaseManager hilang. Skor {finalScore} cuma lokal.");
            return;
        }

        if (APIClient.Instance != null)
        {
            Debug.Log($"[{tag}] 📤 Sedang menyimpan ke Vercel — langkah 1/2: sinkronisasi data user...");
            APIClient.Instance.UserSync(FirebaseManager.Instance.Username, (ok, resp, raw) =>
            {
                if (ok) Debug.Log($"[{tag}] ✅ Langkah 1/2 OK — user tersinkron. Lanjut simpan hasil stage...");
                else    Debug.LogWarning($"[{tag}] ⚠️ Langkah 1/2 (UserSync) gagal, lanjut tetap coba simpan: {raw}");
                CompleteWithLog(stageId, finalScore, tag);
            });
        }
        else
        {
            CompleteWithLog(stageId, finalScore, tag);
        }
    }

    private void CompleteWithLog(string stageId, int finalScore, string tag)
    {
        Debug.Log($"[{tag}] 📤 Sedang menyimpan ke Vercel — langkah 2/2: kirim hasil stage '{stageId}' (score={finalScore})...");

        if (APIClient.Instance != null)
        {
            APIClient.Instance.StageComplete(stageId, finalScore, (ok, resp, raw) =>
            {
                if (ok && resp != null && resp.success)
                    Debug.Log($"[{tag}] ✅ SAVE BERHASIL MASUK DB — stage '{stageId}', finalScore={finalScore}. (Neon menyimpan skor tertinggi)");
                else
                    Debug.LogError($"[{tag}] ❌ SAVE GAGAL ke DB untuk stage '{stageId}' (score {finalScore}). Respon server: {raw}");
            });
        }
        else if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.CompleteStage(stageId, finalScore, success =>
            {
                if (success) Debug.Log($"[{tag}] ✅ SAVE BERHASIL MASUK DB — stage '{stageId}', finalScore={finalScore}.");
                else         Debug.LogError($"[{tag}] ❌ SAVE GAGAL ke DB untuk stage '{stageId}' — score {finalScore} cuma lokal.");
            });
        }
        else
        {
            Debug.LogError($"[{tag}] ❌ SAVE GAGAL — APIClient & SaveSystem dua-duanya hilang. Stage '{stageId}'.");
        }
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
