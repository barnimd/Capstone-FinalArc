using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using MPUIKIT;

/// <summary>
/// One-click build of the "Pilih Karakter Mu" screen in the active scene.
///
///   Tools → SecMind → Build Character Select Canvas
///
/// Re-runnable: it deletes the canvas and controller it made last time before
/// building again, so tweaking the layout is edit-the-script-and-rerun rather
/// than nudging rects by hand.
///
/// All panels/pills/badges are MPImage, matching the dark MPUIKit styling used in
/// the Topic 5/6 panels (see UpdateTrashPanelBuilder). Character art stays a plain
/// Image — MPImage is for procedural shapes, not for sprites.
///
/// Every serialized field on CharacterSelectController is wired automatically.
/// The only thing left to do by hand is dropping in the two character sprites.
/// </summary>
public static class CharacterSelectSetup
{
    private const string CanvasName     = "CharacterSelect_Canvas";
    private const string ControllerName = "CharacterSelectController";

    // Accents come from the mockup rather than the shared blue, because this
    // screen's whole job is to contrast the two characters.
    private static readonly Color Ink          = new Color32(0x0A, 0x07, 0x08, 0xFF);
    private static readonly Color Cream        = new Color32(0xF4, 0xEE, 0xE4, 0xFF);
    private static readonly Color Muted        = new Color32(0x8A, 0x7E, 0x74, 0xFF);
    private static readonly Color MaleAccent   = new Color32(0xF2, 0x8C, 0x26, 0xFF);
    private static readonly Color FemaleAccent = new Color32(0xED, 0x4A, 0x99, 0xFF);
    private static readonly Color ErrorRed     = new Color32(0xE8, 0x6C, 0x60, 0xFF);
    private static readonly Color CardBg       = new Color32(0x16, 0x12, 0x12, 0xF2);

    private const float Pill = 100f;   // MPImage clamps to half-height → true pill

    [MenuItem("Tools/SecMind/Build Character Select Canvas")]
    public static void Build()
    {
        foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == CanvasName || go.name == ControllerName)
                Object.DestroyImmediate(go);

        GameObject canvasGo = new GameObject(CanvasName,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        RectTransform root = canvasGo.GetComponent<RectTransform>();

        if (Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // ── Background ───────────────────────────────────────────────────────
        Stretch(MP("Background", root, Ink, Vector4.zero).rectTransform);

        // ── Header (top-left) ────────────────────────────────────────────────
        // Anchor AND pivot both top-left. Anchoring to a corner while leaving the
        // pivot centred is what pushed the title half off-screen last time.
        MPImage shield = MP("ShieldBadge", root, new Color(0f, 0f, 0f, 0f), V4(10f));
        shield.OutlineColor = MaleAccent;
        shield.OutlineWidth = 2f;
        Place(shield.rectTransform, TopLeft, TopLeft, new Vector2(52f, -40f), new Vector2(52f, 52f));

        TextMeshProUGUI eyebrow = Text("Eyebrow", root, "SECMIND", 17f, Muted, true);
        Place(eyebrow.rectTransform, TopLeft, TopLeft, new Vector2(120f, -44f), new Vector2(560f, 22f));
        eyebrow.alignment = TextAlignmentOptions.TopLeft;
        eyebrow.characterSpacing = 10f;

        TextMeshProUGUI title = Text("Title", root, "PILIH KARAKTER MU", 42f, Cream, true);
        Place(title.rectTransform, TopLeft, TopLeft, new Vector2(118f, -66f), new Vector2(900f, 56f));
        title.alignment = TextAlignmentOptions.TopLeft;
        title.characterSpacing = 2f;

        RectTransform rule = MP("HeaderRule", root, new Color(1f, 1f, 1f, 0.07f), Vector4.zero).rectTransform;
        rule.anchorMin = new Vector2(0f, 1f);
        rule.anchorMax = new Vector2(1f, 1f);
        rule.pivot     = new Vector2(0.5f, 1f);
        rule.anchoredPosition = new Vector2(0f, -132f);
        rule.sizeDelta = new Vector2(0f, 2f);

        // ── Character sides ──────────────────────────────────────────────────
        SideRefs male   = BuildSide(root, "Male",   -470f, "BIMA", "OPERATIVE 01", MaleAccent);
        SideRefs female = BuildSide(root, "Female",  470f, "AYU",  "OPERATIVE 02", FemaleAccent);

        // ── Centre pill ──────────────────────────────────────────────────────
        MPImage pill = MP("ChooseGenderPill", root, new Color(1f, 1f, 1f, 0.03f), V4(Pill));
        pill.OutlineColor = new Color(1f, 1f, 1f, 0.16f);
        pill.OutlineWidth = 1.5f;
        Place(pill.rectTransform, Mid, Mid, new Vector2(0f, 40f), new Vector2(376f, 74f));

        TextMeshProUGUI pillLabel = Text("Label", pill.rectTransform, "Choose Gender", 21f, Cream, true);
        Stretch(pillLabel.rectTransform);
        pillLabel.alignment = TextAlignmentOptions.Center;
        pillLabel.characterSpacing = 4f;

        Button arrowLeft  = ArrowButton("ArrowLeft",  pill.rectTransform, "‹", -148f);
        Button arrowRight = ArrowButton("ArrowRight", pill.rectTransform, "›",  148f);

        // ── Confirm (bottom centre, clear of both badges) ────────────────────
        MPImage confirmBg = MP("ConfirmButton", root, MaleAccent, V4(Pill));
        Place(confirmBg.rectTransform, BottomMid, BottomMid, new Vector2(0f, 58f), new Vector2(300f, 64f));
        Button confirm = confirmBg.gameObject.AddComponent<Button>();
        confirm.targetGraphic = confirmBg;

        TextMeshProUGUI confirmLabel = Text("Label", confirmBg.rectTransform, "Pilih Karakter", 21f,
            new Color32(0x18, 0x12, 0x0E, 0xFF), true);
        Stretch(confirmLabel.rectTransform);
        confirmLabel.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI warning = Text("WarningText", root,
            "Pilihan ini tidak bisa diubah lagi nanti.", 15f, Muted, false);
        Place(warning.rectTransform, BottomMid, BottomMid, new Vector2(0f, 26f), new Vector2(760f, 26f));
        warning.alignment = TextAlignmentOptions.Center;

        TextMeshProUGUI error = Text("ErrorText", root, "", 17f, ErrorRed, true);
        Place(error.rectTransform, BottomMid, BottomMid, new Vector2(0f, 140f), new Vector2(900f, 28f));
        error.alignment = TextAlignmentOptions.Center;
        error.gameObject.SetActive(false);

        MPImage spinner = MP("LoadingSpinner", root, new Color(1f, 1f, 1f, 0.55f), V4(Pill));
        Place(spinner.rectTransform, BottomMid, BottomMid, new Vector2(0f, 140f), new Vector2(28f, 28f));
        spinner.gameObject.SetActive(false);

        // ── Controller + auto-wiring ─────────────────────────────────────────
        GameObject controllerGo = new GameObject(ControllerName);
        var controller = controllerGo.AddComponent<CharacterSelectController>();

        SerializedObject so = new SerializedObject(controller);
        Set(so, "malePortrait",      male.Portrait);
        Set(so, "maleGroup",         male.Group);
        Set(so, "maleGlow",          male.Glow);
        Set(so, "maleBadge",         male.Badge);
        Set(so, "maleClickArea",     male.Click);
        Set(so, "femalePortrait",    female.Portrait);
        Set(so, "femaleGroup",       female.Group);
        Set(so, "femaleGlow",        female.Glow);
        Set(so, "femaleBadge",       female.Badge);
        Set(so, "femaleClickArea",   female.Click);
        Set(so, "arrowLeft",         arrowLeft);
        Set(so, "arrowRight",        arrowRight);
        Set(so, "confirmButton",     confirm);
        Set(so, "confirmBackground", confirmBg);
        Set(so, "confirmLabel",      confirmLabel);
        Set(so, "warningText",       warning);
        Set(so, "errorText",         error);
        Set(so, "loadingSpinner",    spinner.gameObject);
        so.FindProperty("maleAccent").colorValue   = MaleAccent;
        so.FindProperty("femaleAccent").colorValue = FemaleAccent;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = controllerGo;

        Debug.Log("[CharacterSelectSetup] Canvas rebuilt. Assign maleSprite / femaleSprite " +
                  "on CharacterSelectController, then save the scene.");
    }

    // ── Side ─────────────────────────────────────────────────────────────────

    private struct SideRefs
    {
        public Image       Portrait;
        public CanvasGroup Group;
        public GameObject  Glow;
        public GameObject  Badge;
        public Button      Click;
    }

    private static SideRefs BuildSide(RectTransform parent, string id, float x,
                                      string name, string subtitle, Color accent)
    {
        RectTransform side = Rect(id + "Side", parent);
        Place(side, BottomMid, BottomMid, new Vector2(x, 0f), new Vector2(720f, 1080f));
        CanvasGroup group = side.gameObject.AddComponent<CanvasGroup>();

        // Light pooling at the character's feet. A radial gradient fading to zero
        // alpha, not a filled rectangle — the flat slab was what made the last
        // attempt read as two coloured blocks instead of a lit character.
        MPImage glow = MP(id + "Glow", side, Color.white, V4(Pill));
        Place(glow.rectTransform, BottomMid, BottomMid, new Vector2(0f, 60f), new Vector2(700f, 460f));
        ApplyRadialGlow(glow, accent, 0.42f);

        Image portrait = new GameObject(id + "Portrait", typeof(RectTransform), typeof(Image))
            .GetComponent<Image>();
        portrait.transform.SetParent(side, false);
        Place(portrait.rectTransform, BottomMid, BottomMid, new Vector2(0f, 150f), new Vector2(560f, 800f));
        portrait.preserveAspect = true;
        // Transparent until a sprite is assigned, so an unfilled slot reads as
        // empty space instead of a white slab. CharacterSelectController.ApplyPortrait
        // restores the alpha once the sprite is set.
        portrait.color = new Color(1f, 1f, 1f, 0f);
        portrait.raycastTarget = true;

        Button click = portrait.gameObject.AddComponent<Button>();
        click.targetGraphic = portrait;
        click.transition = Selectable.Transition.None;

        MPImage badge = MP(id + "Badge", side, CardBg, V4(10f));
        badge.OutlineColor = accent;
        badge.OutlineWidth = 2f;
        Place(badge.rectTransform, BottomMid, BottomMid, new Vector2(0f, 120f), new Vector2(230f, 84f));

        TextMeshProUGUI badgeName = Text("Name", badge.rectTransform, name, 29f, Cream, true);
        Place(badgeName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
              new Vector2(0f, -12f), new Vector2(210f, 36f));
        badgeName.alignment = TextAlignmentOptions.Top;

        TextMeshProUGUI badgeSub = Text("Subtitle", badge.rectTransform, subtitle, 13f, accent, true);
        Place(badgeSub.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
              new Vector2(0f, 14f), new Vector2(210f, 20f));
        badgeSub.alignment = TextAlignmentOptions.Bottom;
        badgeSub.characterSpacing = 6f;

        return new SideRefs { Portrait = portrait, Group = group, Glow = glow.gameObject,
                              Badge = badge.gameObject, Click = click };
    }

    private static void ApplyRadialGlow(MPImage img, Color accent, float peakAlpha)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(accent, 0f), new GradientColorKey(accent, 1f) },
            new[] { new GradientAlphaKey(peakAlpha, 0f),
                    new GradientAlphaKey(peakAlpha * 0.45f, 0.45f),
                    new GradientAlphaKey(0f, 1f) });

        GradientEffect ge = img.GradientEffect;
        ge.Enabled      = true;
        ge.GradientType = GradientType.Radial;
        ge.Gradient     = g;
        img.GradientEffect = ge;
        img.raycastTarget = false;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static readonly Vector2 TopLeft   = new Vector2(0f, 1f);
    private static readonly Vector2 Mid       = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 BottomMid = new Vector2(0.5f, 0f);

    private static Vector4 V4(float r) => new Vector4(r, r, r, r);

    private static void Set(SerializedObject so, string field, Object value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = value;
        else Debug.LogWarning("[CharacterSelectSetup] Field not found: " + field);
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static MPImage MP(string name, Transform parent, Color color, Vector4 corner)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        MPImage img = go.AddComponent<MPImage>();
        img.color     = color;
        img.DrawShape = DrawShape.Rectangle;

        Rectangle rect = img.Rectangle;
        rect.CornerRadius = corner;
        img.Rectangle = rect;
        return img;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string text,
                                        float size, Color color, bool bold)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text          = text;
        t.fontSize      = size;
        t.color         = color;
        t.fontStyle     = bold ? FontStyles.Bold : FontStyles.Normal;
        t.raycastTarget = false;
        return t;
    }

    private static Button ArrowButton(string name, Transform parent, string glyph, float x)
    {
        MPImage img = MP(name, parent, new Color(1f, 1f, 1f, 0.05f), V4(Pill));
        img.OutlineColor = MaleAccent;
        img.OutlineWidth = 1.5f;
        Place(img.rectTransform, Mid, Mid, new Vector2(x, 0f), new Vector2(46f, 46f));

        TextMeshProUGUI label = Text("Glyph", img.rectTransform, glyph, 24f, Cream, true);
        Stretch(label.rectTransform);
        label.alignment = TextAlignmentOptions.Center;

        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        return b;
    }

    /// <summary>Anchor and pivot are separate on purpose — see the header comment.</summary>
    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot     = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
