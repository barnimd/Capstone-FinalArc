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
    // Palet dinaikkan kontrasnya: teks sekunder dulu 0.72 abu-abu (susah dibaca di
    // background gelap), sekarang 0.86. Orange dinaikkan ke amber menyala biar
    // badge/aksen kebaca jelas.
    private static readonly Color BgDark = new Color(0.106f, 0.082f, 0.071f, 1f);
    private static readonly Color CardDark = new Color(0.180f, 0.145f, 0.129f, 1f);
    private static readonly Color TileCard = new Color(0.239f, 0.196f, 0.173f, 1f);
    private static readonly Color Orange = new Color(1f, 0.647f, 0.220f, 1f);
    private static readonly Color Cream = new Color(1f, 0.988f, 0.973f, 1f);
    private static readonly Color Muted = new Color(0.859f, 0.835f, 0.804f, 1f);
    private static readonly Color OnOrange = new Color(0.106f, 0.082f, 0.071f, 1f);

    /// <summary>Margin kiri/kanan yang dipakai SEMUA elemen kolom AI (judul, chat, input).</summary>
    private const float Pad = 20f;
    private static readonly Vector2 TileSize = new Vector2(350f, 88f);

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

        MoveAndPlace(panel, left, "UsernameBadge", new Vector2(300f, 32f), new Vector2(0f, 236f));
        MoveAndPlace(panel, left, "LevelCompleteText", new Vector2(350f, 46f), new Vector2(0f, 190f));
        MoveAndPlace(panel, left, "TopicNameText", new Vector2(350f, 34f), new Vector2(0f, 150f));
        // Cuma Score + Time. Dua tile ditaruh sebagai satu grup di tengah kolom,
        // digeser sedikit ke atas supaya jarak ke tombol bawah tidak sempit.
        MoveAndPlace(panel, left, "ScoreTile", TileSize, new Vector2(0f, 32f));
        MoveAndPlace(panel, left, "TimeTile", TileSize, new Vector2(0f, -72f));
        MoveAndPlace(panel, left, "BackToMenuButton", new Vector2(166f, 52f), new Vector2(-92f, -244f));
        MoveAndPlace(panel, left, "ReplayButton", new Vector2(166f, 52f), new Vector2(92f, -244f));

        HideChild(panel, "AccuracyTile");
        HideChild(panel, "NextLevelButton");

        // Isi tile masih memakai anchor dari prefab (tile aslinya 220x130, portrait).
        // Setelah di-resize jadi 350x88 teksnya jadi melorot keluar tile, jadi
        // internalnya ditata ulang di sini.
        RelayoutStatTile(left, "ScoreTile", "ScoreText");
        RelayoutStatTile(left, "TimeTile", "TimeText");

        Transform replay = FindDeepChild(left, "ReplayButton");
        if (replay != null)
        {
            TMP_Text label = replay.GetComponentInChildren<TMP_Text>(true);
            // Dulu "↻ Retry" / "↺ Retry" — glyph U+21BB & U+21BA tidak ada di atlas
            // LiberationSans SDF, jadi kerender jadi kotak kosong. Pakai teks polos.
            if (label != null) label.text = "Retry";
            Button button = replay.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(RetryCurrentTopic);
            }
        }

        ApplyLeftColumnContrast(left);
        BuildChatColumn(right);
    }

    private void BuildChatColumn(RectTransform right)
    {
        TMP_Text title = CreateText("AICoachTitle", right, "AI Learning Coach", 24f, Cream, TextAlignmentOptions.Left, FontStyles.Bold);
        Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(430f, 36f), new Vector2(Pad, -20f));
        _remainingText = CreateText("RemainingQuestionsText", right, "3 questions remaining", 12f, Orange, TextAlignmentOptions.Right, FontStyles.Bold);
        Anchor(_remainingText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(210f, 28f), new Vector2(-Pad, -25f));
        _statusText = CreateText("AIStatusText", right, "", 12f, Muted, TextAlignmentOptions.Left);
        Anchor(_statusText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(640f, 22f), new Vector2(Pad, -58f));

        RectTransform scrollRoot = CreateRect("ChatScroll", right);
        scrollRoot.anchorMin = Vector2.zero;
        scrollRoot.anchorMax = Vector2.one;
        scrollRoot.pivot = new Vector2(0.5f, 0.5f);
        scrollRoot.offsetMin = new Vector2(Pad, 78f);
        scrollRoot.offsetMax = new Vector2(-Pad, -88f);
        Image scrollBg = scrollRoot.gameObject.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.22f);
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

        // Input di-stretch pakai margin yang sama dengan ChatScroll (Pad kiri/kanan),
        // dulu lebarnya fix 610 jadi tepi kanannya tidak segaris dengan kotak chat.
        RectTransform inputRoot = CreateRect("ChatInput", right);
        inputRoot.anchorMin = new Vector2(0f, 0f);
        inputRoot.anchorMax = new Vector2(1f, 0f);
        inputRoot.pivot = new Vector2(0.5f, 0f);
        inputRoot.offsetMin = new Vector2(Pad, 16f);
        inputRoot.offsetMax = new Vector2(-Pad, 66f);
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
        // Teks gelap di atas oranye menyala = kontras jauh lebih tinggi dari putih.
        TMP_Text sendText = CreateText("SendText", sendRoot, "Send", 13f, OnOrange, TextAlignmentOptions.Center, FontStyles.Bold);
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
        background.color = isUser ? new Color(0.451f, 0.259f, 0.110f, 1f) : new Color(0.145f, 0.118f, 0.106f, 1f);
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
            case "phishing": return "Privasi Data & Social Engineering";
            case "2fa": return "Keamanan Password & MFA";
            case "password-security": return "Phishing Email";
            case "malware-awareness": return "Keamanan Browsing & Malware";
            case "wifi-security": return "Keamanan Wi-Fi Publik & Website";
            case "ransomware": return "Ransomware & Backup";
            default: return "Cybersecurity Training";
        }
    }

    private static void HideChild(Transform root, string name)
    {
        Transform found = FindDeepChild(root, name);
        if (found != null) found.gameObject.SetActive(false);
    }

    /// <summary>
    /// Menata ulang isi satu stat tile untuk bentuk landscape 350x88: titik aksen di
    /// kiri (rata tengah vertikal), label kecil di atas, angka besar di bawahnya —
    /// keduanya rata tengah. Sub-text ("4 / 5 correct") dimatikan karena sudah tidak dipakai.
    /// </summary>
    private static void RelayoutStatTile(Transform root, string tileName, string valueName)
    {
        Transform tile = FindDeepChild(root, tileName);
        if (tile == null) return;

        Image tileBg = tile.GetComponent<Image>();
        if (tileBg != null) tileBg.color = TileCard;

        RectTransform icon = FindDeepChild(tile, tileName + "Icon") as RectTransform;
        if (icon != null)
            Anchor(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 10f), new Vector2(18f, 0f));

        RectTransform label = FindDeepChild(tile, tileName + "Label") as RectTransform;
        if (label != null)
        {
            Anchor(label, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(300f, 18f), new Vector2(0f, -12f));
            TMP_Text labelText = label.GetComponent<TMP_Text>();
            if (labelText != null)
            {
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.fontSize = 12f;
                labelText.color = Muted;
            }
        }

        RectTransform value = FindDeepChild(tile, valueName) as RectTransform;
        if (value != null)
        {
            Anchor(value, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(300f, 44f), new Vector2(0f, -32f));
            TMP_Text valueText = value.GetComponent<TMP_Text>();
            if (valueText != null)
            {
                valueText.alignment = TextAlignmentOptions.Center;
                valueText.fontSize = 32f;
                valueText.color = Cream;
            }
        }

        HideChild(tile, valueName + "SubText");
    }

    /// <summary>
    /// Menaikkan kontras teks kolom kiri. Warnanya ikut prefab (dibuat editor script),
    /// jadi dinaikkan runtime supaya semua scene kebagian tanpa rebuild prefab.
    /// </summary>
    private static void ApplyLeftColumnContrast(Transform left)
    {
        foreach (TMP_Text text in left.GetComponentsInChildren<TMP_Text>(true))
        {
            switch (text.gameObject.name)
            {
                case "UsernameBadgeText":
                    text.color = Orange;
                    break;
                case "TopicNameText":
                    text.color = Muted;
                    break;
                case "LevelCompleteText":
                case "BackToMenuButtonText":
                case "ReplayButtonText":
                    text.color = Cream;
                    break;
            }
        }

        // Isian tombol aslinya putih 4% — nyaris menyatu dengan background. Dinaikkan ke 12%.
        foreach (string buttonName in new[] { "BackToMenuButton", "ReplayButton" })
        {
            Transform button = FindDeepChild(left, buttonName);
            if (button == null) continue;
            Image buttonBg = button.GetComponent<Image>();
            if (buttonBg != null) buttonBg.color = new Color(1f, 1f, 1f, 0.12f);
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
