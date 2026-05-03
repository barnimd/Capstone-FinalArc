using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    private FirebaseConfig config;

    private string firestoreBase => config != null ? config.FirestoreBase : "";
    private string authBase      => config != null ? config.AuthBase      : "";
    private string apiKey        => config != null ? config.apiKey        : "";

    private string measurementId = "G-5BHQNWRTMY";

    private string idToken   = "";
    private string localId   = "";
    private string _username = "";

    public bool   IsAuthenticated => !string.IsNullOrEmpty(idToken);
    public string IdToken         => idToken;
    public string LocalId         => localId;
    public string Username        => _username;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        config = Resources.Load<FirebaseConfig>("FirebaseConfig");
        if (config == null)
            Debug.LogError("[FirebaseManager] FirebaseConfig not found in Resources. Create it at Assets/Resources/FirebaseConfig.asset");
    }

    // ─── Connection Test ─────────────────────────────────────────────────────

    [ContextMenu("Test Firebase Connection")]
    public void TestConnection()
    {
        StartCoroutine(TestConnectionCoroutine());
    }

    private IEnumerator TestConnectionCoroutine()
    {
        Debug.Log("[FirebaseManager] TestConnection: starting...");

        bool signInDone = false;
        bool signInOk   = false;
        SignInAnonymous(result => { signInOk = result; signInDone = true; });
        yield return new WaitUntil(() => signInDone);

        if (!signInOk)
        {
            Debug.LogError("[FirebaseManager] TestConnection: SignInAnonymous FAILED");
            yield break;
        }
        Debug.Log("[FirebaseManager] TestConnection: SignInAnonymous OK");

        bool saveDone = false;
        bool saveOk   = false;
        SaveScore("test", "TestPlayer", 100, 1, result => { saveOk = result; saveDone = true; });
        yield return new WaitUntil(() => saveDone);

        if (!saveOk)
        {
            Debug.LogError("[FirebaseManager] TestConnection: SaveScore FAILED");
            yield break;
        }
        Debug.Log("[FirebaseManager] TestConnection: SaveScore OK");

        bool leaderboardDone = false;
        bool leaderboardOk   = false;
        GetLeaderboard(10, entries =>
        {
            if (entries == null)
            {
                leaderboardOk = false;
            }
            else
            {
                leaderboardOk = true;
                foreach (LeaderboardEntry entry in entries)
                    Debug.Log($"[FirebaseManager] Leaderboard entry — user:{entry.userId} username:{entry.username} score:{entry.score} level:{entry.level}");
            }
            leaderboardDone = true;
        });
        yield return new WaitUntil(() => leaderboardDone);

        if (!leaderboardOk)
        {
            Debug.LogError("[FirebaseManager] TestConnection: GetLeaderboard FAILED");
            yield break;
        }

        Debug.Log("[FirebaseManager] Firebase WebGL connection: ALL SYSTEMS OK");
    }

    // ─── Auth ────────────────────────────────────────────────────────────────

    public void SignInAnonymous(Action<bool> callback)
    {
        StartCoroutine(SignInAnonymousCoroutine(callback));
    }

    private IEnumerator SignInAnonymousCoroutine(Action<bool> callback)
    {
        string url  = authBase + "/accounts:signUp?key=" + apiKey;
        string body = "{\"returnSecureToken\":true}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ParseAuthResponse(request.downloadHandler.text);
                Debug.Log("[FirebaseManager] SignInAnonymous token received. localId: " + localId + " | idToken empty: " + string.IsNullOrEmpty(idToken));
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogWarning("[FirebaseManager] SignInAnonymous failed. URL: " + url + " | Response: " + request.downloadHandler.text);
                callback?.Invoke(false);
            }
        }
    }

    public void SignInWithEmail(string email, string password, Action<bool> callback)
    {
        StartCoroutine(SignInWithEmailCoroutine(email, password, callback));
    }

    private IEnumerator SignInWithEmailCoroutine(string email, string password, Action<bool> callback)
    {
        string url  = authBase + "/accounts:signInWithPassword?key=" + apiKey;
        string body = $"{{\"email\":\"{email}\",\"password\":\"{password}\",\"returnSecureToken\":true}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ParseAuthResponse(request.downloadHandler.text);
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogWarning("[FirebaseManager] SignInWithEmail failed: " + request.downloadHandler.text);
                callback?.Invoke(false);
            }
        }
    }

    private void ParseAuthResponse(string json)
    {
        idToken = ExtractJsonField(json, "idToken");
        localId = ExtractJsonField(json, "localId");
    }

    public void SignUpWithEmail(string email, string password, string username, Action<bool, string> callback)
    {
        StartCoroutine(SignUpWithEmailCoroutine(email, password, username, callback));
    }

    private IEnumerator SignUpWithEmailCoroutine(string email, string password, string username, Action<bool, string> callback)
    {
        string url  = authBase + "/accounts:signUp?key=" + apiKey;
        string body = "{\"email\":\"" + email + "\",\"password\":\"" + password + "\",\"returnSecureToken\":true}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[FirebaseManager] SignUpWithEmail failed: " + request.downloadHandler.text);
                callback?.Invoke(false, request.downloadHandler.text);
                yield break;
            }

            ParseAuthResponse(request.downloadHandler.text);
        }

        // Auth succeeded — save full user profile to Firestore
        SaveUserProfile(localId, username, email, callback);
    }

    public void SaveUserProfile(string userId, string username, string email, Action<bool, string> callback)
    {
        StartCoroutine(SaveUserProfileCoroutine(userId, username, email, callback));
    }

    private IEnumerator SaveUserProfileCoroutine(string userId, string username, string email, Action<bool, string> callback)
    {
        string url         = firestoreBase + "/players/" + userId;
        string createdAt   = DateTime.UtcNow.ToString("o");
        string createdDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string body =
            "{\"fields\":{" +
                "\"username\":{\"stringValue\":\"" + username + "\"}," +
                "\"email\":{\"stringValue\":\"" + email + "\"}," +
                "\"createdAt\":{\"stringValue\":\"" + createdAt + "\"}," +
                "\"createdDate\":{\"stringValue\":\"" + createdDate + "\"}" +
            "}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(idToken))
                request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true, null);
            }
            else
            {
                Debug.LogWarning("[FirebaseManager] SaveUserProfile failed: " + request.downloadHandler.text);
                callback?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    public void SaveUsername(string userId, string username, Action<bool> callback)
    {
        StartCoroutine(SaveUsernameCoroutine(userId, username, callback));
    }

    private IEnumerator SaveUsernameCoroutine(string userId, string username, Action<bool> callback)
    {
        string url         = firestoreBase + "/players/" + userId;
        string createdAt   = DateTime.UtcNow.ToString("o");
        string createdDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string body =
            "{\"fields\":{" +
                "\"username\":{\"stringValue\":\"" + username + "\"}," +
                "\"createdAt\":{\"stringValue\":\"" + createdAt + "\"}," +
                "\"createdDate\":{\"stringValue\":\"" + createdDate + "\"}" +
            "}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(idToken))
                request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogWarning("[FirebaseManager] SaveUsername failed: " + request.downloadHandler.text);
                callback?.Invoke(false);
            }
        }
    }

    public void GetUsername(string userId, Action<string> callback)
    {
        StartCoroutine(GetUsernameCoroutine(userId, callback));
    }

    private IEnumerator GetUsernameCoroutine(string userId, Action<string> callback)
    {
        string url = firestoreBase + "/players/" + userId;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(idToken))
                request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[FirebaseManager] GetUsername failed: " + request.downloadHandler.text);
                callback?.Invoke(null);
                yield break;
            }

            int fieldsIndex = request.downloadHandler.text.IndexOf("\"fields\"", StringComparison.Ordinal);
            string username = fieldsIndex >= 0
                ? ExtractFirestoreString(request.downloadHandler.text, fieldsIndex, "username")
                : "";

            callback?.Invoke(username);
        }
    }

    public IEnumerator SignUp(string email, string password, Action<bool, string> callback)
    {
        string url  = $"{authBase}/accounts:signUp?key={apiKey}";
        string body = $"{{\"email\":\"{email}\",\"password\":\"{password}\",\"returnSecureToken\":true}}";
        yield return SendAuthRequest(url, body, callback);
    }

    public IEnumerator SignIn(string email, string password, Action<bool, string> callback)
    {
        string url  = $"{authBase}/accounts:signInWithPassword?key={apiKey}";
        string body = $"{{\"email\":\"{email}\",\"password\":\"{password}\",\"returnSecureToken\":true}}";
        yield return SendAuthRequest(url, body, callback);
    }

    // ─── Guest Sign-In (anonymous + username) ────────────────────────────────

    /// <summary>
    /// Signs in anonymously, validates username is unique for today, then saves it.
    /// Callback: (success, errorMessage). errorMessage is null on success.
    /// </summary>
    public void SignInAsGuest(string username, Action<bool, string> callback)
    {
        StartCoroutine(SignInAsGuestCoroutine(username, callback));
    }

    private IEnumerator SignInAsGuestCoroutine(string username, Action<bool, string> callback)
    {
        // Step 1: anonymous sign-in to get a token (needed for Firestore query)
        bool signInDone = false;
        bool signInOk   = false;
        SignInAnonymous(result => { signInOk = result; signInDone = true; });
        yield return new WaitUntil(() => signInDone);

        if (!signInOk)
        {
            Debug.LogWarning("[FirebaseManager] SignInAsGuest: anonymous sign-in failed");
            callback?.Invoke(false, "Gagal terhubung ke server. Coba lagi.");
            yield break;
        }

        // Step 2: check username availability for today
        bool checkDone      = false;
        bool usernameIsFree = false;
        CheckUsernameAvailableToday(username, result => { usernameIsFree = result; checkDone = true; });
        yield return new WaitUntil(() => checkDone);

        if (!usernameIsFree)
        {
            Debug.LogWarning("[FirebaseManager] SignInAsGuest: username already taken today — " + username);
            // clear the anonymous token we just created
            idToken = "";
            localId = "";
            callback?.Invoke(false, "Username sudah dipakai hari ini. Coba username lain.");
            yield break;
        }

        // Step 3: save username to Firestore players/{localId}
        bool saveDone = false;
        bool saveOk   = false;
        SaveUsername(localId, username, result => { saveOk = result; saveDone = true; });
        yield return new WaitUntil(() => saveDone);

        if (!saveOk)
        {
            Debug.LogWarning("[FirebaseManager] SignInAsGuest: SaveUsername failed");
            idToken = "";
            localId = "";
            callback?.Invoke(false, "Gagal menyimpan username. Coba lagi.");
            yield break;
        }

        _username = username;
        Debug.Log("[FirebaseManager] SignInAsGuest OK — username: " + username + " | localId: " + localId);
        callback?.Invoke(true, null);
    }

    /// <summary>
    /// Returns true if username is NOT used today (available), false if taken.
    /// Requires a valid idToken to query Firestore.
    /// </summary>
    private void CheckUsernameAvailableToday(string username, Action<bool> callback)
    {
        StartCoroutine(CheckUsernameAvailableTodayCoroutine(username, callback));
    }

    private IEnumerator CheckUsernameAvailableTodayCoroutine(string username, Action<bool> callback)
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string url   = firestoreBase + ":runQuery";
        string body  =
            "{\"structuredQuery\":{" +
                "\"from\":[{\"collectionId\":\"players\"}]," +
                "\"where\":{\"compositeFilter\":{\"op\":\"AND\",\"filters\":[" +
                    "{\"fieldFilter\":{\"field\":{\"fieldPath\":\"username\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"" + username + "\"}}}," +
                    "{\"fieldFilter\":{\"field\":{\"fieldPath\":\"createdDate\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"" + today + "\"}}}" +
                "]}}," +
                "\"limit\":1" +
            "}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(idToken))
                request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[FirebaseManager] CheckUsernameAvailableToday query failed: " + request.downloadHandler.text);
                // On query failure, allow login (fail open — better UX than hard block)
                callback?.Invoke(true);
                yield break;
            }

            // Firestore returns [{document: {...}}] if found, or [{}] if no results
            string json    = request.downloadHandler.text;
            bool   hasDoc  = json.Contains("\"document\"");
            Debug.Log("[FirebaseManager] CheckUsername '" + username + "' today=" + today + " taken=" + hasDoc);
            callback?.Invoke(!hasDoc); // available = not taken
        }
    }

    public void SignOut()
    {
        idToken   = "";
        localId   = "";
        _username = "";
    }

    public void SignInWithUsernameAndPassword(string username, string password, Action<bool, string> callback)
    {
        StartCoroutine(SignInWithUsernameAndPasswordCoroutine(username, password, callback));
    }

    private IEnumerator SignInWithUsernameAndPasswordCoroutine(string username, string password, Action<bool, string> callback)
    {
        // Step 1: anonymous sign-in to get a token for Firestore query
        bool anonDone = false;
        bool anonOk   = false;
        SignInAnonymous(result => { anonOk = result; anonDone = true; });
        yield return new WaitUntil(() => anonDone);

        if (!anonOk)
        {
            callback?.Invoke(false, "Gagal terhubung ke server. Coba lagi.");
            yield break;
        }

        // Step 2: find email by username in Firestore
        string foundEmail = null;
        bool   queryDone  = false;
        GetEmailByUsername(username, result => { foundEmail = result; queryDone = true; });
        yield return new WaitUntil(() => queryDone);

        if (string.IsNullOrEmpty(foundEmail))
        {
            idToken = "";
            localId = "";
            callback?.Invoke(false, "Username tidak ditemukan");
            yield break;
        }

        // Step 3: sign in with email + password
        string url  = authBase + "/accounts:signInWithPassword?key=" + apiKey;
        string body = "{\"email\":\"" + foundEmail + "\",\"password\":\"" + password + "\",\"returnSecureToken\":true}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ParseAuthResponse(request.downloadHandler.text);
                _username = username;
                callback?.Invoke(true, null);
            }
            else
            {
                Debug.LogWarning("[FirebaseManager] SignInWithUsernameAndPassword failed: " + request.downloadHandler.text);
                idToken = "";
                localId = "";
                callback?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    private void GetEmailByUsername(string username, Action<string> callback)
    {
        StartCoroutine(GetEmailByUsernameCoroutine(username, callback));
    }

    private IEnumerator GetEmailByUsernameCoroutine(string username, Action<string> callback)
    {
        string url  = firestoreBase + ":runQuery";
        string body =
            "{\"structuredQuery\":{" +
                "\"from\":[{\"collectionId\":\"players\"}]," +
                "\"where\":{\"fieldFilter\":{\"field\":{\"fieldPath\":\"username\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"" + username + "\"}}}," +
                "\"limit\":1" +
            "}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(idToken))
                request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[FirebaseManager] GetEmailByUsername failed: " + request.downloadHandler.text);
                callback?.Invoke(null);
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log("[FirebaseManager] GetEmailByUsername response: " + json);

            if (!json.Contains("\"document\""))
            {
                Debug.LogWarning("[FirebaseManager] GetEmailByUsername: no document found for username=" + username);
                callback?.Invoke(null);
                yield break;
            }

            int fieldsIndex = json.IndexOf("\"fields\"", StringComparison.Ordinal);
            string email    = fieldsIndex >= 0 ? ExtractFirestoreString(json, fieldsIndex, "email") : null;
            Debug.Log("[FirebaseManager] GetEmailByUsername: username=" + username + " email=" + email);
            callback?.Invoke(string.IsNullOrEmpty(email) ? null : email);
        }
    }

    private IEnumerator SendAuthRequest(string url, string jsonBody, Action<bool, string> callback)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                idToken = ExtractJsonField(response, "idToken");
                localId = ExtractJsonField(response, "localId");
                callback?.Invoke(true, response);
            }
            else
            {
                callback?.Invoke(false, request.downloadHandler.text);
            }
        }
    }

    // ─── Firestore ───────────────────────────────────────────────────────────

    public IEnumerator GetDocument(string collection, string documentId, Action<bool, string> callback)
    {
        string url = $"{firestoreBase}/{collection}/{documentId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            SetAuthHeader(request);

            yield return request.SendWebRequest();
            HandleResponse(request, callback);
        }
    }

    public IEnumerator SetDocument(string collection, string documentId, string firestoreJson, Action<bool, string> callback)
    {
        string url = $"{firestoreBase}/{collection}/{documentId}?currentDocument.exists=false";
        yield return SendFirestoreWrite(url, "POST", firestoreJson, callback);
    }

    public IEnumerator UpdateDocument(string collection, string documentId, string firestoreJson, string updateMask, Action<bool, string> callback)
    {
        string mask = string.IsNullOrEmpty(updateMask) ? "" : $"&updateMask.fieldPaths={updateMask}";
        string url  = $"{firestoreBase}/{collection}/{documentId}?{mask}";
        yield return SendFirestoreWrite(url, "PATCH", firestoreJson, callback);
    }

    public IEnumerator DeleteDocument(string collection, string documentId, Action<bool, string> callback)
    {
        string url = $"{firestoreBase}/{collection}/{documentId}";
        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            SetAuthHeader(request);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();
            HandleResponse(request, callback);
        }
    }

    public IEnumerator QueryCollection(string collection, string firestoreQueryJson, Action<bool, string> callback)
    {
        string url = $"{firestoreBase}/{collection}:runQuery";
        yield return SendFirestoreWrite(url, "POST", firestoreQueryJson, callback);
    }

    public void SaveScore(string userId, string username, int score, int level, Action<bool> callback)
    {
        StartCoroutine(SaveScoreCoroutine(userId, username, score, level, callback));
    }

    private IEnumerator SaveScoreCoroutine(string userId, string username, int score, int level, Action<bool> callback)
    {
        string url = firestoreBase + "/leaderboard/" + userId;
        string timestamp = DateTime.UtcNow.ToString("o");
        string body =
            "{\"fields\":{" +
                "\"score\":{\"integerValue\":\"" + score + "\"}," +
                "\"username\":{\"stringValue\":\"" + username + "\"}," +
                "\"level\":{\"integerValue\":\"" + level + "\"}," +
                "\"timestamp\":{\"stringValue\":\"" + timestamp + "\"}" +
            "}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(idToken))
                request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogWarning("[FirebaseManager] SaveScore failed. URL: " + url + " | Status: " + request.responseCode + " | Response: " + request.downloadHandler.text);
                callback?.Invoke(false);
            }
        }
    }

    public void GetLeaderboard(int limit, Action<List<LeaderboardEntry>> callback)
    {
        StartCoroutine(GetLeaderboardCoroutine(limit, callback));
    }

    private IEnumerator GetLeaderboardCoroutine(int limit, Action<List<LeaderboardEntry>> callback)
    {
        string url = firestoreBase + ":runQuery";
        string body =
            "{\"structuredQuery\":{" +
                "\"from\":[{\"collectionId\":\"leaderboard\"}]," +
                "\"orderBy\":[{\"field\":{\"fieldPath\":\"score\"},\"direction\":\"DESCENDING\"}]," +
                "\"limit\":" + limit +
            "}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(idToken))
                request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[FirebaseManager] GetLeaderboard failed: " + request.downloadHandler.text);
                callback?.Invoke(null);
                yield break;
            }

            List<LeaderboardEntry> entries = ParseLeaderboardResponse(request.downloadHandler.text);
            callback?.Invoke(entries);
        }
    }

    private List<LeaderboardEntry> ParseLeaderboardResponse(string json)
    {
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        int searchFrom = 0;
        while (true)
        {
            int docStart = json.IndexOf("\"document\"", searchFrom, StringComparison.Ordinal);
            if (docStart < 0) break;

            int fieldsStart = json.IndexOf("\"fields\"", docStart, StringComparison.Ordinal);
            if (fieldsStart < 0) break;

            // find the closing brace of this document block (next top-level closing brace after fields)
            int nameStart = json.IndexOf("\"name\"", docStart, StringComparison.Ordinal);
            string docUserId = "";
            if (nameStart > 0 && nameStart < fieldsStart)
            {
                int nameValStart = json.IndexOf(':', nameStart) + 1;
                // skip whitespace and quote
                while (nameValStart < json.Length && (json[nameValStart] == ' ' || json[nameValStart] == '"')) nameValStart++;
                int nameValEnd = json.IndexOf('"', nameValStart);
                string fullName = nameValEnd > 0 ? json.Substring(nameValStart, nameValEnd - nameValStart) : "";
                int lastSlash = fullName.LastIndexOf('/');
                docUserId = lastSlash >= 0 ? fullName.Substring(lastSlash + 1) : fullName;
            }

            LeaderboardEntry entry = new LeaderboardEntry();
            entry.userId   = docUserId;
            entry.username = ExtractFirestoreString(json, fieldsStart, "username");
            entry.score    = ExtractFirestoreInt(json, fieldsStart, "score");
            entry.level    = ExtractFirestoreInt(json, fieldsStart, "level");
            entries.Add(entry);

            searchFrom = fieldsStart + 1;
        }

        return entries;
    }

    public void SaveProgress(string userId, PlayerProgress progress, Action<bool> callback)
    {
        StartCoroutine(SaveProgressCoroutine(userId, progress, callback));
    }

    private IEnumerator SaveProgressCoroutine(string userId, PlayerProgress progress, Action<bool> callback)
    {
        string url = firestoreBase + "/players/" + userId;

        // Build arrayValue of stringValues for completedScenarios
        StringBuilder scenariosBuilder = new StringBuilder();
        scenariosBuilder.Append("{\"values\":[");
        if (progress.completedScenarios != null)
        {
            for (int i = 0; i < progress.completedScenarios.Count; i++)
            {
                if (i > 0) scenariosBuilder.Append(",");
                scenariosBuilder.Append("{\"stringValue\":\"" + progress.completedScenarios[i] + "\"}");
            }
        }
        scenariosBuilder.Append("]}");

        string body =
            "{\"fields\":{" +
                "\"currentLevel\":{\"integerValue\":\"" + progress.currentLevel + "\"}," +
                "\"completedScenarios\":{\"arrayValue\":" + scenariosBuilder + "}," +
                "\"totalScore\":{\"integerValue\":\"" + progress.totalScore + "\"}," +
                "\"lastPlayed\":{\"stringValue\":\"" + progress.lastPlayed + "\"}" +
            "}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(idToken))
                request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogWarning("[FirebaseManager] SaveProgress failed: " + request.downloadHandler.text);
                callback?.Invoke(false);
            }
        }
    }

    public void LoadProgress(string userId, Action<PlayerProgress> callback)
    {
        StartCoroutine(LoadProgressCoroutine(userId, callback));
    }

    private IEnumerator LoadProgressCoroutine(string userId, Action<PlayerProgress> callback)
    {
        string url = firestoreBase + "/players/" + userId;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(idToken))
                request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[FirebaseManager] LoadProgress failed: " + request.downloadHandler.text);
                callback?.Invoke(null);
                yield break;
            }

            PlayerProgress progress = ParsePlayerProgress(request.downloadHandler.text);
            callback?.Invoke(progress);
        }
    }

    private PlayerProgress ParsePlayerProgress(string json)
    {
        PlayerProgress progress = new PlayerProgress();

        int fieldsIndex = json.IndexOf("\"fields\"", StringComparison.Ordinal);
        if (fieldsIndex < 0) return progress;

        progress.currentLevel = ExtractFirestoreInt(json, fieldsIndex, "currentLevel");
        progress.totalScore   = ExtractFirestoreInt(json, fieldsIndex, "totalScore");
        progress.lastPlayed   = ExtractFirestoreString(json, fieldsIndex, "lastPlayed");

        // Parse arrayValue of completedScenarios
        progress.completedScenarios = new List<string>();
        string arrayMarker = "\"completedScenarios\"";
        int arrayPos = json.IndexOf(arrayMarker, fieldsIndex, StringComparison.Ordinal);
        if (arrayPos >= 0)
        {
            int valuesPos = json.IndexOf("\"values\"", arrayPos, StringComparison.Ordinal);
            if (valuesPos >= 0)
            {
                int arrayStart = json.IndexOf('[', valuesPos);
                int arrayEnd   = json.IndexOf(']', arrayStart);
                if (arrayStart >= 0 && arrayEnd >= 0)
                {
                    string arraySlice = json.Substring(arrayStart, arrayEnd - arrayStart);
                    int searchFrom = 0;
                    while (true)
                    {
                        string svMarker = "\"stringValue\":\"";
                        int svPos = arraySlice.IndexOf(svMarker, searchFrom, StringComparison.Ordinal);
                        if (svPos < 0) break;
                        int valStart = svPos + svMarker.Length;
                        int valEnd   = arraySlice.IndexOf('"', valStart);
                        if (valEnd < 0) break;
                        progress.completedScenarios.Add(arraySlice.Substring(valStart, valEnd - valStart));
                        searchFrom = valEnd + 1;
                    }
                }
            }
        }

        return progress;
    }

    private string ExtractFirestoreString(string json, int fromIndex, string fieldName)
    {
        string marker = "\"" + fieldName + "\"";
        int pos = json.IndexOf(marker, fromIndex, StringComparison.Ordinal);
        if (pos < 0) return "";
        string value = ExtractJsonField(json.Substring(pos), "stringValue");
        return value;
    }

    private int ExtractFirestoreInt(string json, int fromIndex, string fieldName)
    {
        string marker = "\"" + fieldName + "\"";
        int pos = json.IndexOf(marker, fromIndex, StringComparison.Ordinal);
        if (pos < 0) return 0;
        string value = ExtractJsonField(json.Substring(pos), "integerValue");
        int.TryParse(value, out int result);
        return result;
    }

    private IEnumerator SendFirestoreWrite(string url, string method, string jsonBody, Action<bool, string> callback)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            SetAuthHeader(request);

            yield return request.SendWebRequest();
            HandleResponse(request, callback);
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SetAuthHeader(UnityWebRequest request)
    {
        if (!string.IsNullOrEmpty(idToken))
            request.SetRequestHeader("Authorization", $"Bearer {idToken}");
    }

    private void HandleResponse(UnityWebRequest request, Action<bool, string> callback)
    {
        bool success = request.result == UnityWebRequest.Result.Success;
        callback?.Invoke(success, request.downloadHandler.text);
    }

    private string ExtractJsonField(string json, string fieldName)
    {
        // Match both "field":"value" and "field": "value" (Firebase returns pretty-printed JSON)
        string search = $"\"{fieldName}\"";
        int start = json.IndexOf(search, StringComparison.Ordinal);
        if (start < 0) return "";
        start += search.Length;

        // Skip past colon and any whitespace
        while (start < json.Length && (json[start] == ':' || json[start] == ' ' || json[start] == '\t')) start++;

        // Expect opening quote
        if (start >= json.Length || json[start] != '"') return "";
        start++; // skip opening quote

        int end = json.IndexOf('"', start);
        return end < 0 ? "" : json.Substring(start, end - start);
    }
}

[Serializable]
public class PlayerProgress
{
    public int          currentLevel;
    public List<string> completedScenarios;
    public int          totalScore;
    public string       lastPlayed;
}

[Serializable]
public class LeaderboardEntry
{
    public string userId;
    public string username;
    public int    score;
    public int    level;
}
