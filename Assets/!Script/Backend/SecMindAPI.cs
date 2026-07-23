using System;
using System.Collections.Generic;

/// <summary>
/// Constants and DTOs for the SecMind Vercel API.
/// Endpoints live at https://secmind.vercel.app/api/...
/// </summary>
public static class SecMindAPI
{
    public const string BaseUrl = "https://secmind.vercel.app";

    public static class Paths
    {
        public const string Stages           = "/api/stages";
        public const string UserSync         = "/api/user/sync";
        public const string CheckpointSave   = "/api/checkpoint/save";
        public const string CheckpointLoad   = "/api/checkpoint/load";
        public const string StageComplete    = "/api/stage/complete";
        public const string StageRestart     = "/api/stage/restart";
        public const string ScoreSubmit      = "/api/score/submit";
        public const string Leaderboard       = "/api/leaderboard";
        public const string LeaderboardGlobal = "/api/leaderboard/global";
        public const string Chat              = "/api/chat";
    }
}

// ── DTOs ────────────────────────────────────────────────────────────────────

[Serializable]
public class StageInfo
{
    public string stageId;
    public string name;
    public int    maxScore;
}

[Serializable]
public class StagesResponse
{
    public bool        success;
    public StageInfo[] stages;
}

[Serializable]
public class UserRecord
{
    public string user_id;
    public string display_name;
    public string email;
    public string role;          // "user" (default) or "admin" — gates the Menu Admin sidebar
    public string created_at;
    public string last_login;
}

[Serializable]
public class UserSyncRequest
{
    public string displayName;
}

[Serializable]
public class UserSyncResponse
{
    public bool       success;
    public UserRecord user;
}

[Serializable]
public class CheckpointSaveRequest
{
    public string stageId;
    public object checkpointData; // any serializable struct/dict
}

[Serializable]
public class CheckpointSaveResponse
{
    public bool   success;
    public int    checkpointId;
    public string savedAt;
}

[Serializable]
public class CheckpointLoadResponse
{
    public bool   success;
    public string stageId;
    public bool   isCompleted;
    public int?   bestScore;
    public string completedAt;
    public bool   hasCheckpoint;
    public object checkpoint;            // JSONB blob — caller deserializes to its own type
    public string checkpointUpdatedAt;
}

[Serializable]
public class StageCompleteRequest
{
    public string stageId;
    public int    finalScore;
}

[Serializable]
public class StageCompleteResponse
{
    public bool   success;
    public string stageId;
    public int    finalScore;
}

[Serializable]
public class StageRestartRequest
{
    public string stageId;
}

[Serializable]
public class StageRestartResponse
{
    public bool success;
    public bool checkpointDeleted;
}

[Serializable]
public class ScoreSubmitRequest
{
    public string stageId;
    public int    score;
}

[Serializable]
public class ScoreSubmitResponse
{
    public bool success;
    public int  scoreId;
}

[Serializable]
public class LeaderboardEntryDTO
{
    public int    rank;
    public string userId;
    public string displayName;
    public int    score;
}

[Serializable]
public class LeaderboardResponse
{
    public bool                  success;
    public string                stageId;
    public LeaderboardEntryDTO[] leaderboard;
}

[Serializable]
public class GlobalLeaderboardEntryDTO
{
    public int    rank;
    public string userId;
    public string displayName;
    public string bestStageId;
    public string bestStageName;
    public int    score;
}

[Serializable]
public class GlobalLeaderboardResponse
{
    public bool                        success;
    public GlobalLeaderboardEntryDTO[] leaderboard;
}

[Serializable]
public class APIErrorResponse
{
    public string error;
    public string detail;
}
