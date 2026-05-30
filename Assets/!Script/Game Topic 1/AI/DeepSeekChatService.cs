using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

// ── DTOs ────────────────────────────────────────────────────────────────────

[Serializable]
public class AIChatMessage
{
    public string role;
    public string content;
}

[Serializable]
public class AIChatRequest
{
    public List<AIChatMessage> messages;
}

[Serializable]
public class AIChatResponse
{
    public bool   success;
    public string reply;
    public string error;
}

// ── Service ─────────────────────────────────────────────────────────────────

/// <summary>
/// Sends player messages to /api/chat (Vercel → DeepSeek proxy).
/// Maintains conversation history for multi-turn context.
/// Pure coroutines — safe for Unity WebGL.
/// </summary>
public class DeepSeekChatService : MonoBehaviour
{
    private const string ENDPOINT    = SecMindAPI.Paths.Chat;
    private const int    MAX_HISTORY = 20; // trim oldest pairs to avoid token overflow

    private readonly List<AIChatMessage> _history = new();

    public bool IsWaiting { get; private set; }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Send a user message. callback(ok, replyText).
    /// Does nothing if a request is already in flight.
    /// </summary>
    public void SendMessage(string userMessage, Action<bool, string> callback)
    {
        if (IsWaiting) return;

        _history.Add(new AIChatMessage { role = "user", content = userMessage });

        // Keep history bounded so we don't blow the context window
        while (_history.Count > MAX_HISTORY)
            _history.RemoveAt(0);

        StartCoroutine(PostChat(callback));
    }

    public void ClearHistory() => _history.Clear();

    // ── Internal ────────────────────────────────────────────────────────────

    private IEnumerator PostChat(Action<bool, string> callback)
    {
        IsWaiting = true;

        string url       = SecMindAPI.BaseUrl + ENDPOINT;
        string json      = JsonConvert.SerializeObject(new AIChatRequest { messages = _history });
        byte[] bodyBytes = Encoding.UTF8.GetBytes(json);

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        FirebaseManager fm = FirebaseManager.Instance;
        if (fm != null && !string.IsNullOrEmpty(fm.IdToken))
            req.SetRequestHeader("Authorization", "Bearer " + fm.IdToken);

        yield return req.SendWebRequest();

        IsWaiting = false;

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("[DeepSeekChat] Network error: " + req.error);
            RemoveLastUserMessage();
            callback?.Invoke(false, "Connection error. Please try again.");
            req.Dispose();
            yield break;
        }

        string raw = req.downloadHandler.text;
        req.Dispose();

        AIChatResponse resp;
        try
        {
            resp = JsonConvert.DeserializeObject<AIChatResponse>(raw);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[DeepSeekChat] JSON parse failed: " + e.Message + " | " + raw);
            RemoveLastUserMessage();
            callback?.Invoke(false, "Response error. Please try again.");
            yield break;
        }

        if (!resp.success)
        {
            Debug.LogWarning("[DeepSeekChat] API error: " + resp.error);
            RemoveLastUserMessage();
            callback?.Invoke(false, resp.error ?? "Unknown error.");
            yield break;
        }

        _history.Add(new AIChatMessage { role = "assistant", content = resp.reply });
        callback?.Invoke(true, resp.reply);
    }

    private void RemoveLastUserMessage()
    {
        if (_history.Count > 0 && _history[_history.Count - 1].role == "user")
            _history.RemoveAt(_history.Count - 1);
    }
}
