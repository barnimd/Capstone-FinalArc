using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Topic1EvidenceChecklistController : MonoBehaviour
{
    private GameObject canvasRoot;
    private GameObject decisionRoot;
    private GameObject feedbackRoot;
    private TMP_Text titleText;
    private TMP_Text scenarioText;
    private TMP_Text feedbackText;
    private readonly List<Toggle> evidenceToggles = new List<Toggle>();
    private Button safeButton;
    private Button unsafeButton;
    private Button continueButton;

    private Topic1EvidenceCase currentCase;
    private Action<bool> completion;
    private bool? lockedDecision;
    private bool selectedSafe;
    private PlayerMovement playerMovement;

    public bool IsOpen => canvasRoot != null && canvasRoot.activeInHierarchy;

    private void Awake()
    {
        BuildUi();
        canvasRoot.SetActive(false);
    }

    public void ShowDecision(Topic1EvidenceCase evidenceCase, Action<bool> onComplete)
    {
        Show(evidenceCase, null, onComplete);
    }

    public void ShowReview(Topic1EvidenceCase evidenceCase, bool wasSafe, Action<bool> onComplete)
    {
        Show(evidenceCase, wasSafe, onComplete);
    }

    private void Show(Topic1EvidenceCase evidenceCase, bool? fixedDecision, Action<bool> onComplete)
    {
        if (evidenceCase == null)
        {
            onComplete?.Invoke(fixedDecision ?? true);
            return;
        }

        currentCase = evidenceCase;
        lockedDecision = fixedDecision;
        completion = onComplete;
        titleText.text = evidenceCase.title;
        scenarioText.text = evidenceCase.scenario;
        feedbackRoot.SetActive(false);
        decisionRoot.SetActive(true);
        RebuildEvidenceRows(evidenceCase);

        safeButton.gameObject.SetActive(!fixedDecision.HasValue);
        unsafeButton.gameObject.SetActive(!fixedDecision.HasValue);
        continueButton.gameObject.SetActive(fixedDecision.HasValue);
        SetButtonLabel(continueButton, "Periksa bukti");

        canvasRoot.SetActive(true);
        SetPlayerLocked(true);
    }

    private void Submit(bool safe)
    {
        selectedSafe = safe;
        int found = 0;
        int selected = 0;
        for (int i = 0; i < evidenceToggles.Count; i++)
        {
            if (!evidenceToggles[i].isOn)
                continue;

            selected++;
            if (currentCase.IsRedFlag(i))
                found++;
        }

        int totalFlags = currentCase.redFlagIndices != null ? currentCase.redFlagIndices.Length : 0;
        string evidenceResult = selected == 0
            ? "Kamu belum menandai bukti apa pun."
            : $"Kamu menemukan {found} dari {totalFlags} red flag.";
        string outcome = safe ? currentCase.safeFeedback : currentCase.unsafeFeedback;

        feedbackText.text = evidenceResult + "\n\n" + outcome;
        decisionRoot.SetActive(false);
        feedbackRoot.SetActive(true);
    }

    private void Complete()
    {
        canvasRoot.SetActive(false);
        SetPlayerLocked(false);
        Action<bool> callback = completion;
        completion = null;
        callback?.Invoke(selectedSafe);
    }

    private void RebuildEvidenceRows(Topic1EvidenceCase evidenceCase)
    {
        for (int i = 0; i < evidenceToggles.Count; i++)
        {
            Toggle toggle = evidenceToggles[i];
            toggle.isOn = false;
            bool enabled = evidenceCase.evidenceOptions != null && i < evidenceCase.evidenceOptions.Length;
            toggle.gameObject.SetActive(enabled);
            if (enabled)
                toggle.GetComponentInChildren<TMP_Text>().text = evidenceCase.evidenceOptions[i];
        }

        SetButtonLabel(safeButton, evidenceCase.safeActionText);
        SetButtonLabel(unsafeButton, evidenceCase.unsafeActionText);
    }

    private void SetPlayerLocked(bool locked)
    {
        if (playerMovement == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                playerMovement = playerObject.GetComponentInParent<PlayerMovement>();
        }

        if (playerMovement != null)
            playerMovement.movementLocked = locked;
    }

    private void BuildUi()
    {
        canvasRoot = new GameObject("Topic1EvidenceCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(transform, false);
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image dim = CreateImage("Dim", canvasRoot.transform, new Color(0f, 0f, 0f, 0.72f));
        Stretch(dim.rectTransform);

        Image panel = CreateImage("EvidencePanel", dim.transform, new Color(0.07f, 0.09f, 0.12f, 0.98f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(920f, 700f);
        panelRect.anchoredPosition = Vector2.zero;

        titleText = CreateText("Title", panel.transform, 36f, FontStyles.Bold, TextAlignmentOptions.Left);
        SetRect(titleText.rectTransform, new Vector2(48f, -42f), new Vector2(824f, 56f), new Vector2(0f, 1f));

        scenarioText = CreateText("Scenario", panel.transform, 23f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        SetRect(scenarioText.rectTransform, new Vector2(48f, -112f), new Vector2(824f, 92f), new Vector2(0f, 1f));

        decisionRoot = new GameObject("Decision", typeof(RectTransform));
        decisionRoot.transform.SetParent(panel.transform, false);
        Stretch(decisionRoot.GetComponent<RectTransform>());

        for (int i = 0; i < 4; i++)
            evidenceToggles.Add(CreateEvidenceToggle(decisionRoot.transform, i));

        safeButton = CreateButton("SafeAction", decisionRoot.transform, new Color(0.12f, 0.42f, 0.28f, 1f), () => Submit(true));
        SetRect(safeButton.GetComponent<RectTransform>(), new Vector2(48f, 42f), new Vector2(390f, 72f), new Vector2(0f, 0f));

        unsafeButton = CreateButton("UnsafeAction", decisionRoot.transform, new Color(0.55f, 0.18f, 0.18f, 1f), () => Submit(false));
        SetRect(unsafeButton.GetComponent<RectTransform>(), new Vector2(-48f, 42f), new Vector2(390f, 72f), new Vector2(1f, 0f));

        continueButton = CreateButton("ReviewAction", decisionRoot.transform, new Color(0.2f, 0.4f, 0.65f, 1f),
            () => Submit(lockedDecision ?? true));
        SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0f, 42f), new Vector2(390f, 72f), new Vector2(0.5f, 0f));

        feedbackRoot = new GameObject("Feedback", typeof(RectTransform));
        feedbackRoot.transform.SetParent(panel.transform, false);
        Stretch(feedbackRoot.GetComponent<RectTransform>());

        feedbackText = CreateText("FeedbackText", feedbackRoot.transform, 26f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        SetRect(feedbackText.rectTransform, new Vector2(48f, -240f), new Vector2(824f, 310f), new Vector2(0f, 1f));

        Button feedbackContinue = CreateButton("Continue", feedbackRoot.transform, new Color(0.2f, 0.4f, 0.65f, 1f), Complete);
        SetButtonLabel(feedbackContinue, "Lanjutkan");
        SetRect(feedbackContinue.GetComponent<RectTransform>(), new Vector2(0f, 42f), new Vector2(320f, 72f), new Vector2(0.5f, 0f));
    }

    private Toggle CreateEvidenceToggle(Transform parent, int index)
    {
        GameObject row = new GameObject("Evidence_" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Toggle));
        row.transform.SetParent(parent, false);
        row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
        SetRect(row.GetComponent<RectTransform>(), new Vector2(48f, -230f - index * 78f), new Vector2(824f, 62f), new Vector2(0f, 1f));

        Image check = CreateImage("Checkmark", row.transform, new Color(0.95f, 0.75f, 0.18f, 1f));
        SetRect(check.rectTransform, new Vector2(18f, -15f), new Vector2(32f, 32f), new Vector2(0f, 1f));
        check.raycastTarget = false;

        TMP_Text label = CreateText("Label", row.transform, 22f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        SetRect(label.rectTransform, new Vector2(66f, -8f), new Vector2(730f, 46f), new Vector2(0f, 1f));
        label.raycastTarget = false;

        Toggle toggle = row.GetComponent<Toggle>();
        toggle.targetGraphic = row.GetComponent<Image>();
        toggle.graphic = check;
        toggle.isOn = false;
        return toggle;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = true;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, Color color, UnityEngine.Events.UnityAction action)
    {
        Image image = CreateImage(name, parent, color);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        TMP_Text text = CreateText("Label", image.transform, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    private static void SetButtonLabel(Button button, string label)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = label;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
