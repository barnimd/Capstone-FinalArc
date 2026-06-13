#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MPUIKIT;

/// <summary>
/// DESIGN-ONLY builder for the redesigned Objective UI (Topic 6).
/// Creates TWO NEW GameObjects under "ObjectiveCanvas" without touching the
/// existing "ObjectivePanel" (teammate-owned):
///   1. ObjectivePopup_Center  -> referensi foto 1 (diamond + OBJECTIVE card, solid)
///   2. ObjectiveDetail_Side    -> referensi foto 2 (TARGET DETAIL card, transparan, pojok kanan atas)
///
/// Visuals only. Fade/slide animation script dibuat nanti (kemungkinan di GameManager_Tp6).
/// Idempotent: jalankan ulang akan rebuild kedua panel.
/// Canvas: ScreenSpaceOverlay, CanvasScaler ref 800x600 (match width) -> ukuran di-tune untuk 800-wide.
/// </summary>
public static class ObjectiveRedesignSetup
{
    // ---- Palette (dari 2 foto referensi) ----
    static readonly Color Orange    = new Color(0.910f, 0.510f, 0.180f, 1f); // #E8822E
    static readonly Color Cream     = new Color(0.937f, 0.914f, 0.855f, 1f); // #EFE9DA
    static readonly Color DarkText  = new Color(0.118f, 0.106f, 0.094f, 1f); // near-black
    static readonly Color LightText = new Color(0.961f, 0.945f, 0.918f, 1f);
    static readonly Color MutedText = new Color(0.725f, 0.698f, 0.659f, 1f);
    static readonly Color Dim       = new Color(0.055f, 0.043f, 0.035f, 0.55f); // popup backdrop (solid OK)
    static readonly Color SideBG    = new Color(0.070f, 0.055f, 0.045f, 0.55f); // transparan, tembus belakang

    const string PopupName = "ObjectivePopup_Center";
    const string SideName  = "ObjectiveDetail_Side";

    [MenuItem("Game/Setup Topic 6 - Objective Redesign (New Panels)")]
    public static void Build()
    {
        var canvasGo = GameObject.Find("ObjectiveCanvas");
        if (canvasGo == null)
        {
            EditorUtility.DisplayDialog("Objective Redesign",
                "ObjectiveCanvas tidak ditemukan di scene aktif.", "OK");
            return;
        }
        var canvas = canvasGo.transform;

        Remove(canvas, PopupName);
        Remove(canvas, SideName);

        BuildPopup(canvas);
        BuildSide(canvas);

        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Debug.Log("[ObjectiveRedesign] Popup + Side panel dibuat di ObjectiveCanvas.");
    }

    // =====================================================================
    // PANEL 1 — CENTER POPUP (foto 1): diamond glow + cream card + OBJECTIVE
    // =====================================================================
    static void BuildPopup(Transform canvas)
    {
        var root = NewRect(PopupName, canvas);
        Stretch(root, 0, 0, 0, 0);                 // full screen
        var cg = root.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;                 // jangan blokir input game (preview)

        // Backdrop dim (solid OK utk popup)
        var bd = NewRect("Backdrop", root);
        Stretch(bd, 0, 0, 0, 0);
        Shape(bd, DrawShape.Rectangle, Dim);

        // Diamond rings (di belakang card) — kecil, benar2 di tengah
        Diamond(root, "DiamondGlow",  210f, 2f, new Color(Orange.r, Orange.g, Orange.b, 0.25f), 4f);
        Diamond(root, "DiamondMain",  190f, 2f, Orange, 1.1f);
        Diamond(root, "DiamondInner", 120f, 1.5f, new Color(Orange.r, Orange.g, Orange.b, 0.45f), 1.1f);

        // Cream card (compact, tepat di tengah)
        var card = NewRect("Card", root);
        Anchor(card, Center, Center, new Vector2(265f, 60f), Vector2.zero);
        var cardImg = Shape(card, DrawShape.Rectangle, Cream);
        cardImg.OutlineColor = Orange;
        cardImg.OutlineWidth = 2f;
        Rounded(cardImg, 10f);

        var txt = Label("txtObjective", card, "Sapa rekan kerja di pagi hari",
                        17f, DarkText, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(txt.rectTransform, 16f, 6f, 16f, 6f);
        txt.enableAutoSizing = true; txt.fontSizeMin = 11f; txt.fontSizeMax = 18f;

        // Icon bulat + label OBJECTIVE
        var icon = NewRect("IconCircle", root);
        Anchor(icon, Center, Center, new Vector2(26f, 26f), new Vector2(0f, -50f));
        var iconImg = Shape(icon, DrawShape.Circle, Orange);
        iconImg.StrokeWidth = 2f;
        var glyph = Label("txtIcon", icon, "❐", 12f, Orange, TextAlignmentOptions.Center);
        Stretch(glyph.rectTransform, 0, 0, 0, 0);

        var lbl = Label("txtLabel", root, "OBJECTIVE", 11f, Orange,
                        TextAlignmentOptions.Center, FontStyles.Bold);
        Anchor(lbl.rectTransform, Center, Center, new Vector2(220f, 18f), new Vector2(0f, -70f));
        lbl.characterSpacing = 8f;
    }

    static void Diamond(Transform parent, string name, float size, float stroke, Color color, float falloff)
    {
        var rt = NewRect(name, parent);
        Anchor(rt, Center, Center, new Vector2(size, size), Vector2.zero);
        rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var img = Shape(rt, DrawShape.Rectangle, color);
        img.StrokeWidth = stroke;
        img.FalloffDistance = falloff;
        Rounded(img, 6f);
    }

    // =====================================================================
    // PANEL 2 — SIDE DETAIL (foto 2): TARGET DETAIL, transparan, pojok kanan atas
    // =====================================================================
    static void BuildSide(Transform canvas)
    {
        var root = NewRect(SideName, canvas);
        Anchor(root, TopRight, TopRight, new Vector2(250f, 118f), new Vector2(-14f, -14f)); // resting pos
        var cg = root.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        // Card background — TRANSPARAN (tembus ke belakang)
        var bgImg = Shape(root, DrawShape.Rectangle, SideBG);
        bgImg.OutlineColor = new Color(Orange.r, Orange.g, Orange.b, 0.45f);
        bgImg.OutlineWidth = 1.5f;
        Rounded(bgImg, 11f);

        // Accent bar oranye (kiri)
        var bar = NewRect("AccentBar", root);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(0f, 1f);
        bar.pivot = new Vector2(0f, 0.5f);
        bar.offsetMin = new Vector2(7f, 11f);   // x=7 dari kiri, bawah inset 11
        bar.offsetMax = new Vector2(11f, -11f); // width 4 (11-7), atas inset 11
        var barImg = Shape(bar, DrawShape.Rectangle, Orange);
        Rounded(barImg, 2f);

        // Header
        var hdr = Label("txtDetailHeader", root, "TARGET DETAIL", 11f, Orange,
                        TextAlignmentOptions.TopLeft, FontStyles.Bold);
        Anchor(hdr.rectTransform, TopLeft, TopLeft, new Vector2(180f, 16f), new Vector2(18f, -13f));
        hdr.characterSpacing = 4f;

        // Badge "!" (kanan atas)
        var badge = NewRect("Badge", root);
        Anchor(badge, TopRight, TopRight, new Vector2(22f, 22f), new Vector2(-11f, -11f));
        var badgeImg = Shape(badge, DrawShape.Rectangle, Orange);
        Rounded(badgeImg, 5f);
        var bang = Label("txtBang", badge, "!", 14f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(bang.rectTransform, 0, 0, 0, 0);

        // Divider tipis
        var div = NewRect("Divider", root);
        div.anchorMin = new Vector2(0f, 1f);
        div.anchorMax = new Vector2(1f, 1f);
        div.pivot = new Vector2(0.5f, 1f);
        div.offsetMin = new Vector2(18f, -34f);
        div.offsetMax = new Vector2(-14f, -32.5f);
        var dImg = div.gameObject.AddComponent<Image>();
        dImg.color = new Color(1f, 1f, 1f, 0.15f);
        dImg.raycastTarget = false;

        // Body (isi objective)
        var body = Label("txtDetailBody", root,
                         "Gunakan komputer di Ruang IT untuk melaporkan email phising ke tim keamanan.",
                         13f, LightText, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        body.rectTransform.anchorMin = new Vector2(0f, 1f);
        body.rectTransform.anchorMax = new Vector2(1f, 1f);
        body.rectTransform.pivot = new Vector2(0.5f, 1f);
        body.rectTransform.offsetMin = new Vector2(18f, -98f);
        body.rectTransform.offsetMax = new Vector2(-14f, -40f);
        body.enableAutoSizing = true; body.fontSizeMin = 10f; body.fontSizeMax = 13f;

        // Location (opsional — boleh dikosongkan nanti)
        var loc = Label("txtDetailLocation", root, "◎  RUANG IT · LANTAI 2",
                        10f, MutedText, TextAlignmentOptions.Left);
        Anchor(loc.rectTransform, BottomLeft, BottomLeft, new Vector2(220f, 14f), new Vector2(18f, 10f));
    }

    // =====================================================================
    // Helpers
    // =====================================================================
    static readonly Vector2 Center    = new Vector2(0.5f, 0.5f);
    static readonly Vector2 TopRight   = new Vector2(1f, 1f);
    static readonly Vector2 TopLeft    = new Vector2(0f, 1f);
    static readonly Vector2 BottomLeft = new Vector2(0f, 0f);

    static void Remove(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    // Full-stretch with padding (left, top, right, bottom)
    static void Stretch(RectTransform rt, float l, float t, float r, float b)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = Center;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    static MPImage Shape(RectTransform rt, DrawShape shape, Color fill)
    {
        var img = rt.gameObject.AddComponent<MPImage>();
        img.raycastTarget = false;
        img.type = Image.Type.Simple;
        img.DrawShape = shape;
        img.color = fill;
        img.Init();
        return img;
    }

    static void Rounded(MPImage img, float radius)
    {
        var r = img.Rectangle;
        r.CornerRadius = new Vector4(radius, radius, radius, radius);
        img.Rectangle = r;
    }

    static TextMeshProUGUI Label(string name, Transform parent, string text, float size,
                                 Color color, TextAlignmentOptions align,
                                 FontStyles style = FontStyles.Normal)
    {
        var rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.raycastTarget = false;
        t.enableWordWrapping = true;
        return t;
    }
}
#endif
