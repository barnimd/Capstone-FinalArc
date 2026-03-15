using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseAnalytics : MonoBehaviour
{
    public static FirebaseAnalytics Instance { get; private set; }

    private string firestoreBase = "https://firestore.googleapis.com/v1/projects/capstone-69592/databases/(default)/documents";

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

    // ─── Core ─────────────────────────────────────────────────────────────────

    public void LogEvent(string eventName, Dictionary<string, string> parameters, Action<bool> callback = null)
    {
        StartCoroutine(LogEventCoroutine(eventName, parameters, callback));
    }

    private IEnumerator LogEventCoroutine(string eventName, Dictionary<string, string> parameters, Action<bool> callback)
    {
        string userId    = FirebaseManager.Instance != null ? FirebaseManager.Instance.LocalId : "";
        string timestamp = DateTime.UtcNow.ToString("o");
        string docId     = Guid.NewGuid().ToString();
        string url       = firestoreBase + "/analytics/" + docId;

        StringBuilder fields = new StringBuilder();
        fields.Append("\"eventName\":{\"stringValue\":\"" + eventName + "\"},");
        fields.Append("\"userId\":{\"stringValue\":\"" + userId + "\"},");
        fields.Append("\"timestamp\":{\"stringValue\":\"" + timestamp + "\"}");

        if (parameters != null)
        {
            foreach (KeyValuePair<string, string> kvp in parameters)
            {
                fields.Append(",\"" + kvp.Key + "\":{\"stringValue\":\"" + kvp.Value + "\"}");
            }
        }

        string body = "{\"fields\":{" + fields + "}}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            string idToken = FirebaseManager.Instance != null ? FirebaseManager.Instance.IdToken : "";
            if (!string.IsNullOrEmpty(idToken))
                request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogWarning("[FirebaseAnalytics] LogEvent '" + eventName + "' failed: " + request.downloadHandler.text);
                callback?.Invoke(false);
            }
        }
    }

    // ─── Shortcuts ────────────────────────────────────────────────────────────

    public void LogScenarioComplete(string scenarioId, int score, float timeSeconds)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "scenarioId",   scenarioId },
            { "score",        score.ToString() },
            { "timeSeconds",  Mathf.RoundToInt(timeSeconds).ToString() }
        };
        LogEvent("scenario_complete", parameters);
    }

    public void LogLevelStart(int level)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "level", level.ToString() }
        };
        LogEvent("level_start", parameters);
    }

    public void LogPhishingDetected(bool correct, string emailId)
    {
        Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "correct", correct.ToString().ToLower() },
            { "emailId", emailId }
        };
        LogEvent("phishing_detected", parameters);
    }
}
