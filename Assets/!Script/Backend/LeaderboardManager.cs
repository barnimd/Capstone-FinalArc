using System;
using UnityEngine;

/// <summary>
/// Thin coordinator over APIClient for leaderboard fetches.
/// UI components subscribe via OnLeaderboardLoaded; logic stays decoupled from any scene.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    public event Action<string, LeaderboardEntryDTO[]> OnLeaderboardLoaded; // (stageId, entries)
    public event Action<string, string>                 OnLeaderboardFailed; // (stageId, error)

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Fetch top scores for a stage. Default limit = 10, max enforced server-side (100).
    /// Callback (optional) fires alongside the event so callers can use either pattern.
    /// </summary>
    public void Fetch(string stageId, int limit = 10, Action<bool, LeaderboardEntryDTO[]> callback = null)
    {
        if (string.IsNullOrEmpty(stageId))
        {
            callback?.Invoke(false, null);
            OnLeaderboardFailed?.Invoke(stageId, "stageId empty");
            return;
        }
        if (APIClient.Instance == null)
        {
            callback?.Invoke(false, null);
            OnLeaderboardFailed?.Invoke(stageId, "APIClient not in scene");
            return;
        }

        APIClient.Instance.GetLeaderboard(stageId, limit, (ok, resp, raw) =>
        {
            if (!ok || resp == null || !resp.success)
            {
                Debug.LogWarning($"[LeaderboardManager] Fetch failed for stage={stageId}: {raw}");
                callback?.Invoke(false, null);
                OnLeaderboardFailed?.Invoke(stageId, raw);
                return;
            }

            LeaderboardEntryDTO[] entries = resp.leaderboard ?? Array.Empty<LeaderboardEntryDTO>();
            callback?.Invoke(true, entries);
            OnLeaderboardLoaded?.Invoke(stageId, entries);
        });
    }
}
