using System;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class AICoachResponse
{
    public string answer;
    public string[] evidence;
    public string nextAction;
    public bool outOfScope;

    public string ToDisplayText()
    {
        string text = answer ?? "";
        if (evidence != null && evidence.Length > 0)
        {
            text += "\n\nEvidence:";
            foreach (string item in evidence)
                if (!string.IsNullOrWhiteSpace(item)) text += "\n• " + item;
        }
        if (!string.IsNullOrWhiteSpace(nextAction))
            text += "\n\nNext step: " + nextAction;
        return text.Trim();
    }
}

[Serializable]
public class AISessionStartRequest
{
    public string runId;
    public string contentVersion;
    public string stageId;
    public int score;
    public int maxScore;
    public float durationSeconds;
    public System.Collections.Generic.List<PlayerRunDecision> decisions;
}

[Serializable]
public class AIStoredMessage
{
    public string role;
    public string content_text;
    public AICoachResponse content_json;
}

[Serializable]
public class AISessionStartResponse
{
    public bool success;
    public string sessionId;
    public AICoachResponse opening;
    public AIStoredMessage[] messages;
    public int remainingQuestions;
    public string error;
}

[Serializable]
public class AIQuestionRequest
{
    public string sessionId;
    public string message;
}

[Serializable]
public class AIQuestionResponse
{
    public bool success;
    public AICoachResponse response;
    public int remainingQuestions;
    public string error;
    public bool retryable;
}

/// <summary>
/// WebGL-safe client for the authenticated post-game AI endpoints.
/// System prompts and conversation history remain server-side.
/// </summary>
public class DeepSeekChatService : MonoBehaviour
{
    public bool IsWaiting { get; private set; }
    public string SessionId { get; private set; }
    public int RemainingQuestions { get; private set; }
    public AIStoredMessage[] RestoredMessages { get; private set; }
    public bool LastFailureRetryable { get; private set; }

    public void StartSession(PlayerRunSnapshot run, Action<bool, AICoachResponse, int, string> callback)
    {
        if (IsWaiting || run == null)
            return;
        if (APIClient.Instance == null)
        {
            callback?.Invoke(false, null, 0, "API client is unavailable.");
            return;
        }

        IsWaiting = true;
        AISessionStartRequest request = new AISessionStartRequest
        {
            runId = run.runId,
            contentVersion = run.contentVersion,
            stageId = run.stageId,
            score = run.score,
            maxScore = run.maxScore,
            durationSeconds = run.durationSeconds,
            decisions = run.decisions
        };

        APIClient.Instance.Post<AISessionStartRequest, AISessionStartResponse>(
            SecMindAPI.Paths.AISessionStart, request, (ok, response, raw) =>
            {
                IsWaiting = false;
                if (!ok || response == null || !response.success)
                {
                    callback?.Invoke(false, null, 0, response?.error ?? ExtractError(raw));
                    return;
                }

                SessionId = response.sessionId;
                RemainingQuestions = response.remainingQuestions;
                RestoredMessages = response.messages;
                AICoachResponse opening = response.opening ?? FindLastAssistant(response.messages);
                callback?.Invoke(true, opening, RemainingQuestions, null);
            });
    }

    public void SendQuestion(string message, Action<bool, AICoachResponse, int, string> callback)
    {
        LastFailureRetryable = false;
        string trimmed = message?.Trim();
        if (IsWaiting || string.IsNullOrEmpty(trimmed))
            return;
        if (string.IsNullOrEmpty(SessionId) || APIClient.Instance == null)
        {
            callback?.Invoke(false, null, RemainingQuestions, "AI session is unavailable.");
            return;
        }
        if (trimmed.Length > 400)
        {
            callback?.Invoke(false, null, RemainingQuestions, "Message is limited to 400 characters.");
            return;
        }

        IsWaiting = true;
        AIQuestionRequest request = new AIQuestionRequest { sessionId = SessionId, message = trimmed };
        APIClient.Instance.Post<AIQuestionRequest, AIQuestionResponse>(SecMindAPI.Paths.AIChat, request,
            (ok, response, raw) =>
            {
                IsWaiting = false;
                if (!ok || response == null || !response.success)
                {
                    if (response != null)
                    {
                        RemainingQuestions = response.remainingQuestions;
                        LastFailureRetryable = response.retryable;
                    }
                    callback?.Invoke(false, null, RemainingQuestions, response?.error ?? ExtractError(raw));
                    return;
                }
                LastFailureRetryable = false;
                RemainingQuestions = response.remainingQuestions;
                callback?.Invoke(true, response.response, RemainingQuestions, null);
            });
    }

    // Compatibility for the old Topic 1 ChatbotUIManager. It only works after a session starts.
    public void SendMessage(string userMessage, Action<bool, string> callback)
    {
        SendQuestion(userMessage, (ok, response, _, error) =>
            callback?.Invoke(ok, ok ? response?.ToDisplayText() : error));
    }

    public void ClearHistory()
    {
        SessionId = null;
        RemainingQuestions = 0;
        RestoredMessages = null;
        LastFailureRetryable = false;
    }

    private static AICoachResponse FindLastAssistant(AIStoredMessage[] messages)
    {
        if (messages == null) return null;
        for (int i = messages.Length - 1; i >= 0; i--)
        {
            if (messages[i] == null || messages[i].role != "assistant") continue;
            if (messages[i].content_json != null) return messages[i].content_json;
            if (!string.IsNullOrEmpty(messages[i].content_text))
                return new AICoachResponse { answer = messages[i].content_text, evidence = Array.Empty<string>(), nextAction = "Tanyakan keputusan yang ingin kamu pahami." };
        }
        return null;
    }

    private static string ExtractError(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Connection error. Please try again.";
        try
        {
            APIErrorResponse error = JsonConvert.DeserializeObject<APIErrorResponse>(raw);
            return error?.error ?? "Response error. Please try again.";
        }
        catch { return "Response error. Please try again."; }
    }
}
