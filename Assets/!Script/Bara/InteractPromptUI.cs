using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton HUD prompt. Auto-creates a bottom-center "[E] Enter" box
/// when an InteractableTrigger calls Show(). No scene setup needed.
/// </summary>
public class InteractPromptUI : MonoBehaviour
{
    private static InteractPromptUI instance;
    private static InteractableTrigger activeTarget;

    private GameObject panel;
    private Text keyText;
    private Text labelText;

    public static InteractPromptUI Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("InteractPromptUI");
                instance = go.AddComponent<InteractPromptUI>();
                instance.Build();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public static void Show(InteractableTrigger target, string label, KeyCode key)
    {
        var inst = Instance;
        activeTarget = target;
        inst.keyText.text = key.ToString();
        inst.labelText.text = label;
        inst.panel.SetActive(true);
    }

    public static void Hide(InteractableTrigger target)
    {
        if (instance == null) return;
        if (activeTarget != target) return;
        activeTarget = null;
        instance.panel.SetActive(false);
    }

    private void Build()
    {
        // Root canvas
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        var font = (Font)Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf");

        // Panel anchored bottom-center
        panel = new GameObject("Prompt", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = new Vector2(0.5f, 0f);
        prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0, 80);
        prt.sizeDelta = new Vector2(220, 80);
        var bg = panel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);

        var hlg = panel.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(18, 22, 12, 12);
        hlg.spacing = 14;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // KeyBox (white outline trick: white square with smaller black square inside)
        var keyBox = new GameObject("KeyBox", typeof(RectTransform), typeof(Image));
        keyBox.transform.SetParent(panel.transform, false);
        keyBox.GetComponent<Image>().color = Color.white;
        var kbLe = keyBox.AddComponent<LayoutElement>();
        kbLe.preferredWidth = 50; kbLe.preferredHeight = 50;

        var keyInner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        keyInner.transform.SetParent(keyBox.transform, false);
        var kirt = (RectTransform)keyInner.transform;
        kirt.anchorMin = Vector2.zero; kirt.anchorMax = Vector2.one;
        kirt.offsetMin = new Vector2(3, 3);
        kirt.offsetMax = new Vector2(-3, -3);
        keyInner.GetComponent<Image>().color = Color.black;

        var keyTextGO = new GameObject("KeyText", typeof(RectTransform));
        keyTextGO.transform.SetParent(keyInner.transform, false);
        var ktrt = (RectTransform)keyTextGO.transform;
        ktrt.anchorMin = Vector2.zero; ktrt.anchorMax = Vector2.one;
        ktrt.offsetMin = Vector2.zero; ktrt.offsetMax = Vector2.zero;
        keyText = keyTextGO.AddComponent<Text>();
        keyText.font = font;
        keyText.fontSize = 32;
        keyText.color = Color.white;
        keyText.alignment = TextAnchor.MiddleCenter;
        keyText.horizontalOverflow = HorizontalWrapMode.Overflow;
        keyText.verticalOverflow = VerticalWrapMode.Overflow;
        keyText.text = "E";

        // Label "Enter"
        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(panel.transform, false);
        labelText = labelGO.AddComponent<Text>();
        labelText.font = font;
        labelText.fontSize = 32;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;
        labelText.text = "Enter";
        var labelLe = labelGO.AddComponent<LayoutElement>();
        labelLe.preferredHeight = 50;

        panel.SetActive(false);
    }
}
