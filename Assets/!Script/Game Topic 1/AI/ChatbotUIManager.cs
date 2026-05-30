using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the in-game AI chatbot panel for Topic 1 (Privasi Keamanan).
///
/// Scene wiring required (assign in Inspector):
///   chatService        — DeepSeekChatService component
///   chatPanel          — root panel GameObject (toggled on/off)
///   scrollRect         — ScrollRect wrapping the message list
///   messageContainer   — Content transform inside the ScrollRect
///   userBubblePrefab   — Prefab with TMP_Text child (right-aligned bubble)
///   aiBubblePrefab     — Prefab with TMP_Text child (left-aligned bubble)
///   inputField         — TMP_InputField for player typing
///   sendButton         — Button that submits the message
///   typingIndicator    — GameObject shown while waiting for AI reply
///   toggleButton       — Floating button that opens/closes the panel
/// </summary>
public class ChatbotUIManager : MonoBehaviour
{
    [Header("Service")]
    [SerializeField] private DeepSeekChatService chatService;

    [Header("Panel")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform  messageContainer;

    [Header("Bubbles")]
    [SerializeField] private GameObject userBubblePrefab;
    [SerializeField] private GameObject aiBubblePrefab;

    [Header("Input")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button         sendButton;
    [SerializeField] private GameObject     typingIndicator;

    [Header("Toggle")]
    [SerializeField] private Button toggleButton;

    private bool _isOpen;

    // ── Lifecycle ───────────────────────────────────────────────────────────

    private void Start()
    {
        chatPanel.SetActive(false);
        typingIndicator.SetActive(false);

        sendButton.onClick.AddListener(OnSendClicked);
        toggleButton.onClick.AddListener(ToggleChat);
        inputField.onSubmit.AddListener(_ => OnSendClicked());
    }

    // ── Public ──────────────────────────────────────────────────────────────

    public void ToggleChat()
    {
        _isOpen = !_isOpen;
        chatPanel.SetActive(_isOpen);
        if (_isOpen)
            inputField.Select();
    }

    // ── Input handling ──────────────────────────────────────────────────────

    private void OnSendClicked()
    {
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text) || chatService.IsWaiting) return;

        inputField.text = "";
        AppendBubble(text, isUser: true);

        SetInputActive(false);
        typingIndicator.SetActive(true);

        chatService.SendMessage(text, OnReplyReceived);
    }

    private void OnReplyReceived(bool ok, string reply)
    {
        typingIndicator.SetActive(false);
        SetInputActive(true);

        string display = ok ? reply : "⚠ " + reply;
        AppendBubble(display, isUser: false);

        inputField.Select();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void AppendBubble(string text, bool isUser)
    {
        GameObject prefab = isUser ? userBubblePrefab : aiBubblePrefab;
        GameObject bubble = Instantiate(prefab, messageContainer);
        bubble.GetComponentInChildren<TMP_Text>().text = text;
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        // Wait one frame for LayoutGroup to recalculate sizes
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void SetInputActive(bool active)
    {
        inputField.interactable = active;
        sendButton.interactable = active;
    }
}
