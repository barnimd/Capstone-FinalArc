using UnityEngine;

/// <summary>
/// Temporary diagnostic component to verify Unity → Vercel → Neon.
/// Attach to any GameObject in a scene where FirebaseManager + APIClient exist.
///
/// Context-menu actions (right-click on the component in Inspector):
///   • Test 1: GET /api/stages          — no auth required
///   • Test 2: POST /api/user/sync      — auth required, upserts user in Neon
///   • Test 3: GET /api/leaderboard     — no auth required
///   • Test 4: Login admintest + sync   — full end-to-end smoke test
///
/// DELETE THIS FILE before shipping.
/// </summary>
public class APITester : MonoBehaviour
{
    [Header("Login credentials for Test 4")]
    public string testUsername = "admintest";
    public string testPassword = "admintest";

    [ContextMenu("Test 1: GET /api/stages")]
    public void TestStages()
    {
        if (APIClient.Instance == null) { Debug.LogError("[APITester] APIClient.Instance missing — add APIClient to scene"); return; }

        APIClient.Instance.GetStages((ok, resp, raw) =>
        {
            if (!ok) { Debug.LogError("[APITester] GetStages FAILED: " + raw); return; }
            Debug.Log($"[APITester] GetStages OK — {resp.stages.Length} stages");
            foreach (StageInfo s in resp.stages)
                Debug.Log($"   • {s.stageId} — {s.name} (max {s.maxScore})");
        });
    }

    [ContextMenu("Test 2: POST /api/user/sync")]
    public void TestUserSync()
    {
        if (APIClient.Instance == null) { Debug.LogError("[APITester] APIClient.Instance missing"); return; }
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[APITester] Not authenticated. Run Test 4 first, or login via the game.");
            return;
        }

        string displayName = FirebaseManager.Instance.Username;
        APIClient.Instance.UserSync(displayName, (ok, resp, raw) =>
        {
            if (!ok) { Debug.LogError("[APITester] UserSync FAILED: " + raw); return; }
            Debug.Log($"[APITester] UserSync OK — user_id={resp.user.user_id} display_name={resp.user.display_name} email={resp.user.email}");
        });
    }

    [ContextMenu("Test 3: GET /api/leaderboard?stageId=phishing")]
    public void TestLeaderboard()
    {
        if (APIClient.Instance == null) { Debug.LogError("[APITester] APIClient.Instance missing"); return; }

        APIClient.Instance.GetLeaderboard("phishing", 10, (ok, resp, raw) =>
        {
            if (!ok) { Debug.LogError("[APITester] GetLeaderboard FAILED: " + raw); return; }
            Debug.Log($"[APITester] GetLeaderboard OK — {resp.leaderboard.Length} entries");
            foreach (LeaderboardEntryDTO e in resp.leaderboard)
                Debug.Log($"   #{e.rank}: {e.displayName} ({e.userId}) — {e.score}");
        });
    }

    [ContextMenu("Test 4: Login admintest + sync (FULL E2E)")]
    public void TestFullFlow()
    {
        if (FirebaseManager.Instance == null) { Debug.LogError("[APITester] FirebaseManager.Instance missing"); return; }
        if (APIClient.Instance == null)       { Debug.LogError("[APITester] APIClient.Instance missing");       return; }

        Debug.Log("[APITester] Step 1: Login as " + testUsername);
        FirebaseManager.Instance.SignInWithUsernameAndPassword(testUsername, testPassword, (ok, err) =>
        {
            if (!ok) { Debug.LogError("[APITester] Login FAILED: " + err); return; }
            Debug.Log("[APITester] Login OK — uid=" + FirebaseManager.Instance.LocalId);

            Debug.Log("[APITester] Step 2: POST /api/user/sync");
            APIClient.Instance.UserSync(testUsername, (syncOk, syncResp, syncRaw) =>
            {
                if (!syncOk) { Debug.LogError("[APITester] UserSync FAILED: " + syncRaw); return; }
                Debug.Log($"[APITester] UserSync OK — Neon user row: {syncResp.user.user_id} / {syncResp.user.display_name}");
                Debug.Log("[APITester] ✅ FULL E2E PASS — Unity → Vercel → Neon working");
            });
        });
    }

    [Header("SaveSystem test (run after Test 4)")]
    public string saveTestStageId = "phishing";

    [ContextMenu("Test 5: SaveCheckpoint roundtrip")]
    public void TestSaveCheckpoint()
    {
        if (SaveSystem.Instance == null) { Debug.LogError("[APITester] SaveSystem.Instance missing — add SaveSystem to scene"); return; }
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[APITester] Not authenticated. Run Test 4 first.");
            return;
        }

        CheckpointData sample = new CheckpointData
        {
            playerX              = 12.5f,
            playerY              = -3.0f,
            currentScore         = 70,
            currentQuestionIndex = 3,
            answeredQuestions    = new int[] { 0, 1, 2 },
            timeElapsed          = 124.5f,
            stageVersion         = "v1",
        };

        Debug.Log("[APITester] Step 1: SaveCheckpoint stage=" + saveTestStageId);
        SaveSystem.Instance.SaveCheckpoint(saveTestStageId, sample, serverOk =>
        {
            if (!serverOk) { Debug.LogError("[APITester] SaveCheckpoint server save FAILED"); return; }

            Debug.Log("[APITester] Step 2: LoadCheckpoint roundtrip");
            SaveSystem.Instance.LoadCheckpoint(saveTestStageId, result =>
            {
                Debug.Log($"[APITester] LoadCheckpoint status={result.Status} hasServer={result.HasServerCheckpoint} hasLocal={result.HasLocalCheckpoint}");
                if (string.IsNullOrEmpty(result.BestCheckpointJson))
                {
                    Debug.LogError("[APITester] LoadCheckpoint returned no data");
                    return;
                }
                CheckpointData loaded = SaveSystem.DeserializeCheckpoint<CheckpointData>(result.BestCheckpointJson);
                bool match = loaded != null
                          && loaded.currentScore         == sample.currentScore
                          && loaded.currentQuestionIndex == sample.currentQuestionIndex
                          && Mathf.Approximately(loaded.playerX, sample.playerX);
                if (match) Debug.Log("[APITester] ✅ SaveCheckpoint roundtrip PASS — data matches");
                else       Debug.LogError("[APITester] ❌ Data mismatch — loaded=" + result.BestCheckpointJson);
            });
        });
    }

    [ContextMenu("Test 6: RestartStage (clear checkpoint)")]
    public void TestRestartStage()
    {
        if (SaveSystem.Instance == null) { Debug.LogError("[APITester] SaveSystem.Instance missing"); return; }
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[APITester] Not authenticated. Run Test 4 first.");
            return;
        }

        SaveSystem.Instance.RestartStage(saveTestStageId, ok =>
        {
            if (!ok) { Debug.LogError("[APITester] RestartStage FAILED"); return; }
            Debug.Log("[APITester] RestartStage OK — now verify with Test 7 (should return Fresh)");
        });
    }

    [ContextMenu("Test 7: LoadCheckpoint (expect Fresh after restart)")]
    public void TestLoadAfterRestart()
    {
        if (SaveSystem.Instance == null) { Debug.LogError("[APITester] SaveSystem.Instance missing"); return; }
        SaveSystem.Instance.LoadCheckpoint(saveTestStageId, result =>
        {
            Debug.Log($"[APITester] After restart: status={result.Status} hasServer={result.HasServerCheckpoint} hasLocal={result.HasLocalCheckpoint}");
            if (result.Status == LoadStatus.Fresh) Debug.Log("[APITester] ✅ Restart cleared checkpoint correctly");
            else                                    Debug.LogWarning("[APITester] ⚠️ Expected Fresh but got " + result.Status);
        });
    }

    [ContextMenu("Test 8: StageManager.EnterStage (decision tree)")]
    public void TestStageManagerEnter()
    {
        if (StageManager.Instance == null) { Debug.LogError("[APITester] StageManager.Instance missing — add StageManager to scene"); return; }
        StageManager.Instance.EnterStage(saveTestStageId, decision =>
        {
            Debug.Log($"[APITester] EnterStage({decision.StageId}) → Mode={decision.Mode} bestScore={decision.BestScore} hasCheckpoint={decision.HasCheckpoint} fromNetwork={decision.FromNetwork}");
            switch (decision.Mode)
            {
                case StageEntryMode.Start:           Debug.Log("  → UI should show: [Start]");                          break;
                case StageEntryMode.Continue:        Debug.Log("  → UI should show: [Continue] [Restart]");             break;
                case StageEntryMode.Replay:          Debug.Log("  → UI should show: [Replay] (with best score badge)"); break;
                case StageEntryMode.ReplayContinue:  Debug.Log("  → UI should show: [Continue Replay] [Restart]");      break;
                case StageEntryMode.OfflineContinue: Debug.Log("  → UI should show: [Continue (offline)]");             break;
                case StageEntryMode.Blocked:         Debug.LogWarning("  → BLOCKED: " + decision.Error);                break;
            }
        });
    }

    [ContextMenu("Test 9: StageManager.Complete (mark stage done)")]
    public void TestStageComplete()
    {
        if (StageManager.Instance == null) { Debug.LogError("[APITester] StageManager.Instance missing"); return; }
        int dummyScore = 850;
        StageManager.Instance.Complete(saveTestStageId, dummyScore, ok =>
        {
            if (ok) Debug.Log($"[APITester] ✅ Stage {saveTestStageId} marked complete with score {dummyScore} (highest wins on replay)");
            else    Debug.LogError("[APITester] ❌ Stage complete FAILED");
        });
    }

    [ContextMenu("Test 10: LeaderboardManager.Fetch")]
    public void TestLeaderboardManager()
    {
        if (LeaderboardManager.Instance == null) { Debug.LogError("[APITester] LeaderboardManager.Instance missing — add LeaderboardManager to scene"); return; }
        LeaderboardManager.Instance.Fetch(saveTestStageId, 10, (ok, entries) =>
        {
            if (!ok) { Debug.LogError("[APITester] Leaderboard fetch FAILED"); return; }
            Debug.Log($"[APITester] ✅ Leaderboard for stage={saveTestStageId} — {entries.Length} entries:");
            foreach (LeaderboardEntryDTO e in entries)
                Debug.Log($"   #{e.rank}: {e.displayName} ({e.userId}) — {e.score}");
        });
    }
}
