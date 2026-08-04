using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds and controls the two-column post-game summary at runtime so every scene
/// that uses CanvasSummaryPanel receives the same AI coach without scene rewiring.
/// </summary>
public class PostGameAICoachController : MonoBehaviour
{
    private static readonly Color BgDark = new Color(0.106f, 0.082f, 0.071f, 1f);
    private static readonly Color CardDark = new Color(0.165f, 0.129f, 0.114f, 1f);
    private static readonly Color Orange = new Color(0.910f, 0.510f, 0.180f, 1f);
    private static readonly Color Cream = new Color(0.961f, 0.945f, 0.918f, 1f);
    private static readonly Color Muted = new Color(0.725f, 0.698f, 0.659f, 1f);

    private DeepSeekChatService _service;
    private ScrollRect _scrollRect;
    private RectTransform _messageContainer;
    private TMP_InputField _input;
    private Button _sendButton;
    private TMP_Text _statusText;
    private TMP_Text _remainingText;
    private PlayerRunSnapshot _snapshot;
    private bool _configured;
    private bool _sessionStarted;

    public static void Configure(GameObject panelRoot, int score, int maxScore, float elapsedSeconds)
    {
        if (panelRoot == null) return;
        PostGameAICoachController coach = panelRoot.GetComponent<PostGameAICoachController>();
        if (coach == null) coach = panelRoot.AddComponent<PostGameAICoachController>();
        coach.Prepare(score, maxScore, elapsedSeconds);
    }

    private void Prepare(int score, int maxScore, float elapsedSeconds)
    {
        BuildLayout();
        PlayerRunRecorder recorder = PlayerRunRecorder.Ensure();
        _snapshot = recorder != null ? recorder.CompleteRun(score, maxScore, elapsedSeconds) : null;
        Transform topicName = FindDeepChild(transform, "TopicNameText");
        TMP_Text topicLabel = topicName != null ? topicName.GetComponent<TMP_Text>() : null;
        if (topicLabel != null && _snapshot != null)
            topicLabel.text = GetTopicDisplayName(_snapshot.stageId);
        _configured = true;
        if (isActiveAndEnabled) BeginSession();
    }

    private void OnEnable()
    {
        if (_configured) BeginSession();
    }

    private void BuildLayout()
    {
        if (_messageContainer != null) return;
        Transform panel = transform.Find("SummaryPannel");
        if (panel == null) panel = transform;
        RectTransform panelRect = panel as RectTransform;
        if (panelRect != null) panelRect.sizeDelta = new Vector2(1180f, 620f);

        RectTransform left = GetOrCreateRect("LeftSummaryColumn", panel);
        SetRect(left, new Vector2(390f, 572f), new Vector2(-371f, 0f));
        RectTransform right = GetOrCreateRect("AICoachColumn", panel);
        SetRect(right, new Vector2(718f, 572f), new Vector2(207f, 0f));
        Image rightBg = GetOrAdd<Image>(right.gameObject);
        rightBg.color = CardDark;

        MoveAndPlace(panel, left, "UsernameBadge", new Vector2(350f, 32f), new Vector2(0f, 252f));
        MoveAndPlace(panel, left, "LevelCompleteText", new Vector2(350f, 44f), new Vector2(0f, 207f));
        MoveAndPlace(panel, left, "TopicNameText", new Vector2(350f, 34f), new Vector2(0f, 166f));
        MoveAndPlace(panel, left, "ScoreTile", new Vector2(350f, 82f), new Vector2(0f, 76f));
        MoveAndPlace(panel, left, "TimeTile", new Vector2(350f, 82f), new Vector2(0f, -18f));
        MoveAndPlace(panel, left, "AccuracyTile", new Vector2(350f, 82f), new Vector2(0f, -112f));
        MoveAndPlace(panel, left, "BackToMenuButton", new Vector2(166f, 52f), new Vector2(-92f, -244f));
        MoveAndPlace(panel, left, "ReplayButton", new Vector2(166f, 52f), new Vector2(92f, -244f));

        Transform next = FindDeepChild(panel, "NextLevelButton");
        if (next != null) next.gameObject.SetActive(false);
        Transform replay = FindDeepChild(left, "ReplayButton");
        if (replay != null)
        {
            TMP_Text label = replay.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "↻  Retry";
            Button button = replay.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(RetryCurrentTopic);
            }
        }

        BuildChatColumn(right);
    }

    private void BuildChatColumn(RectTransform right)
    {
        TMP_Text title = CreateText("AICoachTitle", right, "AI Learning Coach", 24f, Cream, TextAlignmentOptions.Left, FontStyles.Bold);
        Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(430f, 36f), new Vector2(24f, -20f));
        _remainingText = CreateText("RemainingQuestionsText", right, "3 questions remaining", 12f, Orange, TextAlignmentOptions.Right, FontStyles.Bold);
        Anchor(_remainingText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(210f, 28f), new Vector2(-24f, -25f));
        _statusText = CreateText("AIStatusText", right, "", 12f, Muted, TextAlignmentOptions.Left);
        Anchor(_statusText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(640f, 22f), new Vector2(24f, -58f));

        RectTransform scrollRoot = CreateRect("ChatScroll", right);
        scrollRoot.anchorMin = Vector2.zero;
        scrollRoot.anchorMax = Vector2.one;
        scrollRoot.pivot = new Vector2(0.5f, 0.5f);
        scrollRoot.offsetMin = new Vector2(20f, 78f);
        scrollRoot.offsetMax = new Vector2(-20f, -88f);
        Image scrollBg = scrollRoot.gameObject.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.14f);
        _scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;

        RectTransform viewport = CreateRect("Viewport", scrollRoot);
        Stretch(viewport, 8f);
        viewport.gameObject.AddComponent<RectMask2D>();
        _scrollRect.viewport = viewport;

        _messageContainer = CreateRect("Messages", viewport);
        _messageContainer.anchorMin = new Vector2(0f, 1f);
        _messageContainer.anchorMax = new Vector2(1f, 1f);
        _messageContainer.pivot = new Vector2(0.5f, 1f);
        _messageContainer.anchoredPosition = Vector2.zero;
        _messageContainer.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = _messageContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 10f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = _messageContainer.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _scrollRect.content = _messageContainer;

        RectTransform inputRoot = CreateRect("ChatInput", right);
        Anchor(inputRoot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(610f, 50f), new Vector2(20f, 16f));
        Image inputBg = inputRoot.gameObject.AddComponent<Image>();
        inputBg.color = BgDark;
        _input = inputRoot.gameObject.AddComponent<TMP_InputField>();
        _input.characterLimit = 400;
        _input.lineType = TMP_InputField.LineType.SingleLine;

        TMP_Text placeholder = CreateText("Placeholder", inputRoot, "Ask about your decisions...", 14f, Muted, TextAlignmentOptions.MidlineLeft);
        Stretch(placeholder.rectTransform, 14f, 80f, 10f, 10f);
        TMP_Text inputText = CreateText("Text", inputRoot, "", 14f, Cream, TextAlignmentOptions.MidlineLeft);
        Stretch(inputText.rectTransform, 14f, 80f, 10f, 10f);
        _input.textComponent = inputText;
        _input.placeholder = placeholder;
        _input.onSubmit.AddListener(_ => SendQuestion());

        RectTransform sendRoot = CreateRect("SendButton", inputRoot);
        Anchor(sendRoot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(64f, 38f), new Vector2(-6f, 0f));
        Image sendBg = sendRoot.gameObject.AddComponent<Image>();
        sendBg.color = Orange;
        _sendButton = sendRoot.gameObject.AddComponent<Button>();
        _sendButton.targetGraphic = sendBg;
        TMP_Text sendText = CreateText("SendText", sendRoot, "Send", 13f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(sendText.rectTransform, 0f);
        _sendButton.onClick.AddListener(SendQuestion);

        _service = GetComponent<DeepSeekChatService>();
        if (_service == null) _service = gameObject.AddComponent<DeepSeekChatService>();
    }

    private void BeginSession()
    {
        if (_sessionStarted || _snapshot == null || _service == null) return;
        _sessionStarted = true;
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            _statusText.text = "AI coach requires an authenticated account.";
            SetInputActive(false);
            return;
        }

        _statusText.text = "Analyzing your run...";
        SetInputActive(false);
        _service.StartSession(_snapshot, (ok, response, remaining, error) =>
        {
            _statusText.text = ok ? "Debrief ready" : "AI coach is temporarily unavailable";
            if (ok && _service.RestoredMessages != null && _service.RestoredMessages.Length > 0)
            {
                foreach (AIStoredMessage message in _service.RestoredMessages)
                {
                    if (message == null) continue;
                    string restored = message.content_json != null
                        ? message.content_json.ToDisplayText()
                        : message.content_text;
                    if (!string.IsNullOrWhiteSpace(restored))
                        AppendBubble(restored, message.role == "user");
                }
            }
            else if (ok && response != null) AppendBubble(response.ToDisplayText(), false);
            else AppendBubble("⚠ " + (error ?? "Please try again later."), false);
            UpdateRemaining(remaining);
            SetInputActive(ok && remaining > 0);
        });
    }

    private void SendQuestion()
    {
        if (_service == null || _service.IsWaiting || _service.RemainingQuestions <= 0) return;
        string message = _input != null ? _input.text.Trim() : "";
        if (string.IsNullOrEmpty(message)) return;
        _input.text = "";
        RectTransform pendingBubble = AppendBubble(message, true);
        _statusText.text = "Thinking...";
        SetInputActive(false);
        _service.SendQuestion(message, (ok, response, remaining, error) =>
        {
            bool canRetry = !ok && _service.LastFailureRetryable && remaining > 0;
            if (canRetry)
            {
                if (pendingBubble != null)
                    Destroy(pendingBubble.gameObject);
                if (_input != null)
                    _input.text = message;
            }

            _statusText.text = ok ? "" : canRetry
                ? "Coba kirim lagi - kuota tidak berkurang"
                : "Message could not be answered";
            AppendBubble(ok && response != null ? response.ToDisplayText() : "⚠ " + (error ?? "Please try again."), false);
            UpdateRemaining(remaining);
            SetInputActive((ok || canRetry) && remaining > 0);
        });
    }

    private RectTransform AppendBubble(string value, bool isUser)
    {
        if (_messageContainer == null) return null;
        RectTransform bubble = CreateRect(isUser ? "PlayerMessage" : "AIMessage", _messageContainer);
        Image background = bubble.gameObject.AddComponent<Image>();
        background.color = isUser ? new Color(0.34f, 0.20f, 0.10f, 1f) : BgDark;
        TMP_Text text = CreateText("MessageText", bubble, value, 14f, Cream, isUser ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft);
        text.enableWordWrapping = true;
        Stretch(text.rectTransform, 14f);
        float height = Mathf.Clamp(text.GetPreferredValues(value, 610f, 0f).y + 28f, 48f, 240f);
        LayoutElement element = bubble.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        StartCoroutine(ScrollBottom());
        return bubble;
    }

    private IEnumerator ScrollBottom()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 0f;
    }

    private void UpdateRemaining(int count)
    {
        if (_remainingText != null) _remainingText.text = count + (count == 1 ? " question remaining" : " questions remaining");
    }

    private void SetInputActive(bool active)
    {
        if (_input != null) _input.interactable = active;
        if (_sendButton != null) _sendButton.interactable = active;
    }

    private static void RetryCurrentTopic()
    {
        Time.timeScale = 1f;
        PlayerRunRecorder.ResetCurrentRun();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private static string GetTopicDisplayName(string stageId)
    {
        switch (stageId)
        {
            case "phishing": return "Phishing & Social Engineering";
            case "2fa": return "Password Security & MFA";
            case "password-security": return "Email & Password Security";
            case "malware-awareness": return "Malware & Website Awareness";
            case "wifi-security": return "Wi-Fi & Website Security";
            case "ransomware": return "Ransomware & Backup";
            default: return "Cybersecurity Training";
        }
    }

    private static void MoveAndPlace(Transform searchRoot, RectTransform parent, string name, Vector2 size, Vector2 position)
    {
        Transform found = FindDeepChild(searchRoot, name);
        if (found == null) return;
        found.SetParent(parent, false);
        RectTransform rect = found as RectTransform;
        if (rect != null) SetRect(rect, size, position);
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    private static RectTransform GetOrCreateRect(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        return existing != null ? (RectTransform)existing : CreateRect(name, parent);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.raycastTarget = false;
        return text;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
