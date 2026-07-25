using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Server-owned lock state for the class levels.
///
/// The admin toggles a level in Menu Admin > Manage Class, which writes to Neon via
/// POST /api/admin/stage-lock. Every player reads it back from GET /api/stages when the
/// menu loads and when the Class page opens, so a lock applies to everyone.
///
/// LessonData.isUnlocked is only the shipped default — it's used before the first
/// successful fetch. PlayerPrefs keeps the last known server answer so a failed request
/// shows the previous state instead of silently unlocking everything.
/// </summary>
public static class LessonLocks
{
    private const string LockPrefPrefix = "SecMind.stageLocked.";

    // A lesson's sceneName decides which stage id it belongs to. Stage ids must match
    // STAGES in Docs/lib/stages.js.
    private static readonly Dictionary<string, string> SceneToStage = new Dictionary<string, string>
    {
        { "Privasi_Keamanan",   "phishing" },
        { "Office_Environment", "2fa" },
        { "Map_Topic_3",        "password-security" },
        { "Map_Topic4",         "malware-awareness" },
        { "Map_Topic_5",        "wifi-security" },
        { "Map_Topic6",         "ransomware" },
    };

    private static readonly Dictionary<string, bool> LockedByStage = new Dictionary<string, bool>();

    public static string StageIdForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        return SceneToStage.TryGetValue(sceneName, out string stageId) ? stageId : null;
    }

    public static string StageIdFor(LessonData data)
        => data == null ? null : StageIdForScene(data.sceneName);

    /// <summary>Pulls lock state for every stage from the server. cb(true) once applied.</summary>
    public static void Refresh(Action<bool> cb = null)
    {
        if (APIClient.Instance == null)
        {
            cb?.Invoke(false);
            return;
        }

        APIClient.Instance.GetStages((ok, resp, raw) =>
        {
            if (!ok || resp == null || resp.stages == null)
            {
                Debug.LogWarning("[LessonLocks] GetStages failed, keeping cached locks: " + raw);
                cb?.Invoke(false);
                return;
            }

            foreach (StageInfo s in resp.stages)
            {
                if (s == null || string.IsNullOrEmpty(s.stageId)) continue;
                LockedByStage[s.stageId] = s.locked;
                PlayerPrefs.SetInt(LockPrefPrefix + s.stageId, s.locked ? 1 : 0);
            }
            PlayerPrefs.Save();
            cb?.Invoke(true);
        });
    }

    /// <summary>True when the level belongs in the Unlocked grid.</summary>
    public static bool IsUnlocked(LessonData data)
    {
        if (data == null) return false;

        string stageId = StageIdFor(data);
        if (stageId == null) return data.isUnlocked;   // lesson with no stage mapped

        if (LockedByStage.TryGetValue(stageId, out bool locked)) return !locked;

        string key = LockPrefPrefix + stageId;
        if (PlayerPrefs.HasKey(key)) return PlayerPrefs.GetInt(key) == 0;

        return data.isUnlocked;
    }

    /// <summary>
    /// Admin only. Writes the new state to the server first; the local cache is updated
    /// only after the server confirms, so a rejected call can't desync the UI.
    /// </summary>
    public static void SetLocked(LessonData data, bool locked, Action<bool> cb = null)
    {
        string stageId = StageIdFor(data);
        if (stageId == null || APIClient.Instance == null)
        {
            Debug.LogWarning($"[LessonLocks] Cannot toggle '{(data != null ? data.title : "null")}' — no stage id for scene '{(data != null ? data.sceneName : "")}'");
            cb?.Invoke(false);
            return;
        }

        APIClient.Instance.SetStageLock(stageId, locked, (ok, resp, raw) =>
        {
            if (!ok || resp == null || !resp.success)
            {
                Debug.LogWarning($"[LessonLocks] Lock update rejected for '{stageId}': " + raw);
                cb?.Invoke(false);
                return;
            }

            LockedByStage[stageId] = resp.locked;
            PlayerPrefs.SetInt(LockPrefPrefix + stageId, resp.locked ? 1 : 0);
            PlayerPrefs.Save();
            cb?.Invoke(true);
        });
    }
}
