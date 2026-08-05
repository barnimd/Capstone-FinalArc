#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MPUIKIT;

/// <summary>
/// Rebuilds "SummaryPannel" inside Assets/!Prefab/Canvas/CanvasSummaryPanel.prefab
/// to match the "Level Complete!" reference design (badge, title, subtitle,
/// Score/Time stat tiles, and a Back to Menu / Replay / Next Level
/// button row) using MPUIKit shapes. Visual pass only — button click behavior
/// (Replay, Next Level) is wired up separately later.
///
/// Keeps the GameObject names SummaryManager.cs already looks up by name
/// ("SummaryPannel", "LevelCompleteText", "ScoreText", "TimeText") so the
/// existing summary flow keeps working without rewiring.
/// Idempotent: destroys and rebuilds "SummaryPannel" each run.
/// </summary>
public static class SummaryPanelRedesignSetup
{
    const string PrefabPath = "Assets/!Prefab/Canvas/CanvasSummaryPanel.prefab";

    // Harus sama dengan palet di PostGameAICoachController (versi kontras tinggi).
    static readonly Color BgDark    = new Color(0.106f, 0.082f, 0.071f, 1f);
    static readonly Color CardDark  = new Color(0.239f, 0.196f, 0.173f, 1f);
    static readonly Color Orange    = new Color(1f, 0.647f, 0.220f, 1f);
    static readonly Color Cream     = new Color(1f, 0.988f, 0.973f, 1f);
    static readonly Color MutedText = new Color(0.859f, 0.835f, 0.804f, 1f);
    static readonly Color Outline   = new Color(1f, 1f, 1f, 0.28f);

    static readonly Vector2 Center     = new Vector2(0.5f, 0.5f);
    static readonly Vector2 TopCenter  = new Vector2(0.5f, 1f);
    static readonly Vector2 TopLeft    = new Vector2(0f, 1f);

    [MenuItem("Game/Rebuild Summary Panel (CanvasSummaryPanel Prefab)")]
    public static void Rebuild()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError("[SummaryPanelRedesign] Could not load prefab at " + PrefabPath);
            return;
        }

        var canvasT = root.transform;
        var old = canvasT.Find("SummaryPannel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var panel = NewRect("SummaryPannel", canvasT);
        Anchor(panel, Center, Center, new Vector2(1180f, 620f), Vector2.zero);
        var panelImg = Shape(panel, DrawShape.Rectangle, BgDark);
        Rounded(panelImg, 28f);

        // ── Username badge ────────────────────────────────────
        var pill = NewRect("UsernameBadge", panel);
        Anchor(pill, TopCenter, TopCenter, new Vector2(300f, 32f), new Vector2(0f, -26f));
        var pillImg = Shape(pill, DrawShape.Rectangle, new Color(0f, 0f, 0f, 0f));
        pillImg.OutlineColor = Orange;
        pillImg.OutlineWidth = 1.5f;
        Rounded(pillImg, 16f);
        var pillTxt = Label("UsernameBadgeText", pill, "Username of player active", 12f, Orange,
            TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(pillTxt.rectTransform, 8f, 4f, 8f, 4f);

        // ── Title ──────────────────────────────────────────────
        var title = Label("LevelCompleteText", panel, "Level Complete!", 34f, Cream,
            TextAlignmentOptions.Center, FontStyles.Bold);
        Anchor(title.rectTransform, TopCenter, TopCenter, new Vector2(620f, 50f), new Vector2(0f, -80f));

        // ── Subtitle (topic name placeholder) ─────────────────
        var subtitle = Label("TopicNameText", panel, "This Should be the name of the topic player played",
            14f, MutedText, TextAlignmentOptions.Center);
        Anchor(subtitle.rectTransform, TopCenter, TopCenter, new Vector2(580f, 24f), new Vector2(0f, -124f));

        // ── Stat tiles (Score + Time saja, Accuracy dihapus) ───
        BuildStatTile(panel, "ScoreTile", new Vector2(-120f, -20f), "SCORE", "ScoreText", "67", null);
        BuildStatTile(panel, "TimeTile", new Vector2(120f, -20f), "TIME", "TimeText", "2:14", null);

        // ── Buttons ────────────────────────────────────────────
        // Tanpa glyph unicode: ↺/↻ (U+21BA/U+21BB) tidak ada di atlas LiberationSans SDF
        // dan kerender jadi kotak kosong. "‹" (U+2039) ada, jadi aman dipakai.
        BuildOutlineButton(panel, "BackToMenuButton", new Vector2(-100f, -195f), 190f, "‹  Back to Menu", true);
        BuildOutlineButton(panel, "ReplayButton", new Vector2(100f, -195f), 190f, "Retry", false);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[SummaryPanelRedesign] SummaryPannel rebuilt in " + PrefabPath);
    }

    // =====================================================================
    static void BuildStatTile(Transform parent, string name, Vector2 pos, string label,
        string valueName, string valueText, string subText)
    {
        var tile = NewRect(name, parent);
        Anchor(tile, Center, Center, new Vector2(220f, 130f), pos);
        var bg = Shape(tile, DrawShape.Rectangle, CardDark);
        Rounded(bg, 16f);

        var dot = NewRect(name + "Icon", tile);
        Anchor(dot, TopLeft, TopLeft, new Vector2(10f, 10f), new Vector2(18f, -18f));
        var dotImg = Shape(dot, DrawShape.Circle, Orange);

        var labelLbl = Label(name + "Label", tile, label, 11f, MutedText, TextAlignmentOptions.Left, FontStyles.Bold);
        Anchor(labelLbl.rectTransform, TopLeft, TopLeft, new Vector2(160f, 20f), new Vector2(36f, -14f));
        labelLbl.characterSpacing = 2f;

        var value = Label(valueName, tile, valueText, 30f, Cream, TextAlignmentOptions.Left, FontStyles.Bold);
        Anchor(value.rectTransform, TopLeft, TopLeft, new Vector2(188f, 40f), new Vector2(16f, -48f));

        if (!string.IsNullOrEmpty(subText))
        {
            var sub = Label(valueName + "SubText", tile, subText, 11f, MutedText, TextAlignmentOptions.Left);
            Anchor(sub.rectTransform, TopLeft, TopLeft, new Vector2(188f, 18f), new Vector2(16f, -94f));
        }
    }

    static void BuildOutlineButton(Transform parent, string name, Vector2 pos, float width, string text, bool addReturnToMenu)
    {
        var btnRoot = NewRect(name, parent);
        Anchor(btnRoot, Center, Center, new Vector2(width, 52f), pos);
        var bg = Shape(btnRoot, DrawShape.Rectangle, new Color(1f, 1f, 1f, 0.04f));
        bg.OutlineColor = Outline;
        bg.OutlineWidth = 1.5f;
        Rounded(bg, 12f);
        bg.raycastTarget = true;
        var btn = btnRoot.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        var lbl = Label(name + "Text", btnRoot, text, 16f, Cream, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(lbl.rectTransform, 0f, 0f, 0f, 0f);
        if (addReturnToMenu) btnRoot.gameObject.AddComponent<SummaryReturnToMenu>();
    }

    static void BuildFilledButton(Transform parent, string name, Vector2 pos, float width, string text)
    {
        var btnRoot = NewRect(name, parent);
        Anchor(btnRoot, Center, Center, new Vector2(width, 52f), pos);
        var bg = Shape(btnRoot, DrawShape.Rectangle, Orange);
        Rounded(bg, 12f);
        bg.raycastTarget = true;
        var btn = btnRoot.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        var lbl = Label(name + "Text", btnRoot, text, 16f, Cream, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(lbl.rectTransform, 0f, 0f, 0f, 0f);
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

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
        Color color, TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
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
