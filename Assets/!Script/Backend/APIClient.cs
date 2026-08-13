using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// HTTPS wrapper around the SecMind Vercel API.
/// - Auto-attaches Firebase ID token as Bearer auth
/// - On 401, refreshes the token via FirebaseManager.RefreshIdToken and retries once
/// - JSON via Newtonsoft.Json (handles nested objects / dicts unlike JsonUtility)
/// - Pure Coroutines (WebGL: no async/threads)
/// </summary>
public class APIClient : MonoBehaviour
{
    public static APIClient Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Only the duplicate component goes, never the GameObject. In All_Menu this
            // sits on SecMindManagers alongside SaveSystem, StageManager and
            // LeaderboardManager; LoginScene now carries its own APIClient, so by the
            // time All_Menu loads this one is the duplicate. Destroying the GameObject
            // here would take those three managers down with it.
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>GET request. Pass query string in path (e.g. "/api/leaderboard?stageId=phishing").</summary>
    public void Get<TResp>(string path, Action<bool, TResp, string> callback)
    {
        StartCoroutine(SendRequest<object, TResp>("GET", path, null, callback));
    }

    /// <summary>POST request with a JSON body.</summary>
    public void Post<TBody, TResp>(string path, TBody body, Action<bool, TResp, string> callback)
    {
        StartCoroutine(SendRequest<TBody, TResp>("POST", path, body, callback));
    }

    // ── Convenience wrappers per endpoint ───────────────────────────────────

    public void GetStages(Action<bool, StagesResponse, string> cb)
        => Get(SecMindAPI.Paths.Stages, cb);

    /// <summary>Guest login. The only endpoint that runs before a token exists.</summary>
    public void GuestLogin(string callsign, string deviceId, Action<bool, GuestLoginResponse, string> cb)
        => Post(SecMindAPI.Paths.GuestLogin, new GuestLoginRequest { callsign = callsign, deviceId = deviceId }, cb);

    public void UserSync(string displayName, Action<bool, UserSyncResponse, string> cb)
        => Post(SecMindAPI.Paths.UserSync, new UserSyncRequest { displayName = displayName }, cb);

    /// <summary>
    /// Upsert the user and record their character choice. Gender is permanent —
    /// the server ignores this field once the player has already picked.
    /// </summary>
    public void UserSyncWithGender(string displayName, string gender, Action<bool, UserSyncResponse, string> cb)
        => Post(SecMindAPI.Paths.UserSync, new UserSyncRequest { displayName = displayName, gender = gender }, cb);

    public void CheckpointSave(string stageId, object checkpointData, Action<bool, CheckpointSaveResponse, string> cb)
        => Post(SecMindAPI.Paths.CheckpointSave, new CheckpointSaveRequest { stageId = stageId, checkpointData = checkpointData }, cb);

    public void CheckpointLoad(string stageId, Action<bool, CheckpointLoadResponse, string> cb)
        => Get($"{SecMindAPI.Paths.CheckpointLoad}?stageId={UnityWebRequest.EscapeURL(stageId)}", cb);

    public void StageComplete(string stageId, int finalScore, Action<bool, StageCompleteResponse, string> cb)
        => Post(SecMindAPI.Paths.StageComplete, new StageCompleteRequest { stageId = stageId, finalScore = finalScore }, cb);

    public void StageRestart(string stageId, Action<bool, StageRestartResponse, string> cb)
        => Post(SecMindAPI.Paths.StageRestart, new StageRestartRequest { stageId = stageId }, cb);

    public void ScoreSubmit(string stageId, int score, Action<bool, ScoreSubmitResponse, string> cb)
        => Post(SecMindAPI.Paths.ScoreSubmit, new ScoreSubmitRequest { stageId = stageId, score = score }, cb);

    public void GetLeaderboard(string stageId, int limit, Action<bool, LeaderboardResponse, string> cb)
        => Get($"{SecMindAPI.Paths.Leaderboard}?stageId={UnityWebRequest.EscapeURL(stageId)}&limit={limit}", cb);

    public void GetGlobalLeaderboard(int limit, Action<bool, GlobalLeaderboardResponse, string> cb)
        => Get($"{SecMindAPI.Paths.LeaderboardGlobal}?limit={limit}", cb);

    /// <summary>Admin only — server rejects with 403 unless users.role = 'admin'.</summary>
    public void SetStageLock(string stageId, bool locked, Action<bool, StageLockResponse, string> cb)
        => Post(SecMindAPI.Paths.AdminStageLock, new StageLockRequest { stageId = stageId, locked = locked }, cb);

    /// <summary>Dashboard poster list. Public — works for guests too.</summary>
    public void GetPosters(Action<bool, PostersResponse, string> cb)
        => Get(SecMindAPI.Paths.Posters, cb);

    /// <summary>Admin only. imageBase64 must already be compressed — Vercel rejects bodies over 4.5 MB.</summary>
    public void UploadPoster(string title, string mimeType, string imageBase64, Action<bool, PosterUploadResponse, string> cb)
        => Post(SecMindAPI.Paths.PosterUpload,
                new PosterUploadRequest { title = title, mimeType = mimeType, imageBase64 = imageBase64 }, cb);

    /// <summary>Admin only.</summary>
    public void DeletePoster(int id, Action<bool, PosterDeleteResponse, string> cb)
        => Post(SecMindAPI.Paths.PosterDelete, new PosterDeleteRequest { id = id }, cb);

    /// <summary>Admin only. Send every active poster id, in the order they should appear.</summary>
    public void ReorderPosters(int[] ids, Action<bool, PosterReorderResponse, string> cb)
        => Post(SecMindAPI.Paths.PosterReorder, new PosterReorderRequest { ids = ids }, cb);

    // ── Core ────────────────────────────────────────────────────────────────

    private IEnumerator SendRequest<TBody, TResp>(string method, string path, TBody body, Action<bool, TResp, string> callback)
    {
        bool   retried     = false;
        string url         = SecMindAPI.BaseUrl + path;
        byte[] cachedBytes = null;

        if (body != null)
        {
            string json = JsonConvert.SerializeObject(body);
            cachedBytes = Encoding.UTF8.GetBytes(json);
        }

        while (true)
        {
            UnityWebRequest req = new UnityWebRequest(url, method);
            req.downloadHandler = new DownloadHandlerBuffer();

            if (cachedBytes != null)
            {
                req.uploadHandler = new UploadHandlerRaw(cachedBytes);
                req.SetRequestHeader("Content-Type", "application/json");
            }

            FirebaseManager fm = FirebaseManager.Instance;
            if (fm != null && !string.IsNullOrEmpty(fm.IdToken))
            {
                req.SetRequestHeader("Authorization", "Bearer " + fm.IdToken);
            }

            yield return req.SendWebRequest();

            long   code = req.responseCode;
            string raw  = req.downloadHandler != null ? req.downloadHandler.text : "";

            // 401 → refresh token once then retry
            if (code == 401 && !retried && fm != null && !string.IsNullOrEmpty(fm.RefreshToken))
            {
                Debug.Log("[APIClient] 401 received, attempting token refresh");
                bool refreshDone = false;
                bool refreshOk   = false;
                fm.RefreshIdToken(ok => { refreshOk = ok; refreshDone = true; });
                yield return new WaitUntil(() => refreshDone);

                req.Dispose();
                if (refreshOk)
                {
                    retried = true;
                    continue;
                }
                Debug.LogWarning("[APIClient] Token refresh failed");
                callback?.Invoke(false, default, raw);
                yield break;
            }

            bool ok = req.result == UnityWebRequest.Result.Success && code >= 200 && code < 300;
            if (!ok)
            {
                Debug.LogWarning($"[APIClient] {method} {path} failed: HTTP {code} | {raw}");
                callback?.Invoke(false, default, raw);
                req.Dispose();
                yield break;
            }

            TResp parsed = default;
            try
            {
                parsed = JsonConvert.DeserializeObject<TResp>(raw);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[APIClient] JSON parse failed: " + e.Message + " | raw: " + raw);
                callback?.Invoke(false, default, raw);
                req.Dispose();
                yield break;
            }

            callback?.Invoke(true, parsed, raw);
            req.Dispose();
            yield break;
        }
    }
}
