#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MPUIKIT;

/// <summary>
/// Builder for the redesigned Objective UI (2 panels: center popup + side detail card).
///
/// Two entry points:
///  - "Game/Apply Objective Redesign (current scene)"  -> GENERIC. Finds every
///    IObjectivePanel in the open scene, builds the redesign under its Canvas
///    (sizes auto-scaled to that canvas' reference resolution), adds an
///    ObjectiveRedesignUI controller, and wires the old panel's `redesign` field.
///    Use this for Topics 1–5.
///  - "Game/Setup Topic 6 - Objective Redesign (New Panels)" -> the original
///    Topic-6-only build under a Canvas literally named "ObjectiveCanvas".
///
/// Authored at an 800-wide reference (Topic 6). On other canvases sizes are
/// multiplied by (referenceResolution.x / 800). Idempotent.
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

    const string PopupName       = "ObjectivePopup_Center";
    const string SideName        = "ObjectiveDetail_Side";
    const string ControllerName  = "ObjectiveRedesignUI";
    const string QuestButtonName = "ObjectiveQuestButton";
    const float  BaselineWidth   = 800f; // Topic 6 reference width the design was authored at

    // Scale factor applied to all authored sizes/positions/fonts (set per canvas).
    static float S = 1f;
    static float F(float v) => v * S;
    static Vector2 V(float x, float y) => new Vector2(x * S, y * S);

    // =====================================================================
    // GENERIC ENTRY — apply to every objective panel in the open scene
    // =====================================================================
    [MenuItem("Game/Apply Objective Redesign (current scene)")]
    public static void ApplyToCurrentScene()
    {
        // Collect old objective panels (anything IObjectivePanel that isn't our own controller).
        var panels = new List<MonoBehaviour>();
        foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>(true))
            if (mb is IObjectivePanel && !(mb is ObjectiveRedesignUI))
                panels.Add(mb);

        if (panels.Count == 0)
        {
            Debug.LogWarning("[ObjectiveRedesign] Tidak ada IObjectivePanel di scene yang terbuka.");
            return;
        }

        // Group by parent Canvas so a shared canvas only gets one redesign + controller.
        var byCanvas = new Dictionary<Canvas, List<MonoBehaviour>>();
        foreach (var p in panels)
        {
            var cv = p.GetComponentInParent<Canvas>(true); // include inactive (embedded/computer canvases load inactive)
            if (cv == null) { Debug.LogWarning($"[ObjectiveRedesign] '{p.name}' tidak punya parent Canvas — dilewati."); continue; }
            if (!byCanvas.TryGetValue(cv, out var list)) { list = new List<MonoBehaviour>(); byCanvas[cv] = list; }
            list.Add(p);
        }

        int canvases = 0, wired = 0;
        foreach (var kv in byCanvas)
        {
            var controller = BuildUnderCanvas(kv.Key);
            canvases++;
            foreach (var panel in kv.Value)
            {
                var f = panel.GetType().GetField("redesign");
                if (f != null) { f.SetValue(panel, controller); EditorUtility.SetDirty(panel); wired++; }
                else Debug.LogWarning($"[ObjectiveRedesign] '{panel.GetType().Name}' tidak punya field 'redesign' — forwarding tidak aktif.");
            }
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[ObjectiveRedesign] '{scene.name}': {canvases} canvas, {wired} panel di-wire.");
    }

    static float ComputeScale(Canvas canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
            && scaler.referenceResolution.x > 0f)
            return scaler.referenceResolution.x / BaselineWidth;
        return 1f;
    }

    static ObjectiveRedesignUI BuildUnderCanvas(Canvas canvas)
    {
        S = ComputeScale(canvas);
        var t = canvas.transform;

        Remove(t, PopupName);
        Remove(t, SideName);
        Remove(t, ControllerName);
        Remove(t, QuestButtonName);

        BuildPopup(t);
        BuildSide(t);

        var ctrlGo = new GameObject(ControllerName, typeof(RectTransform));
        ctrlGo.transform.SetParent(t, false);
        var crt = ctrlGo.GetComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

        var ctrl = ctrlGo.AddComponent<ObjectiveRedesignUI>();
        ctrl.popupRoot = t.Find(PopupName).gameObject;
        ctrl.sideRoot  = t.Find(SideName).gameObject;
        ctrl.popupRoot.SetActive(false);
        ctrl.sideRoot.SetActive(false);
        EnsureQuestButton(canvas, ctrl);
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    // =====================================================================
    // TOPIC 6 ENTRY (original) — build under a Canvas named "ObjectiveCanvas"
    // =====================================================================
    [MenuItem("Game/Setup Topic 6 - Objective Redesign (New Panels)")]
    public static void Build()
    {
        S = 1f;
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
    // PREFAB — self-contained HUD with its OWN 1920 Canvas (drop-in reuse)
    // =====================================================================
    const string PrefabRootName = "ObjectiveRedesignHUD";
    const string PrefabPath      = "Assets/!Prefab/ObjectiveRedesignHUD.prefab";

    [MenuItem("Game/Create-Update Objective Redesign Prefab")]
    public static void CreateOrUpdatePrefab()
    {
        var root = BuildPrefabRoot();
        if (!AssetDatabase.IsValidFolder("Assets/!Prefab"))
            AssetDatabase.CreateFolder("Assets", "!Prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log("[ObjectiveRedesign] Prefab " + (prefab != null ? "saved" : "FAILED") + ": " + PrefabPath);
    }

    static GameObject BuildPrefabRoot()
    {
        S = 1920f / BaselineWidth; // 2.4 — prefab carries its own 1920 canvas
        var root = new GameObject(PrefabRootName,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var t = root.transform;
        BuildPopup(t);
        BuildSide(t);

        var ctrl = root.AddComponent<ObjectiveRedesignUI>();
        ctrl.popupRoot = t.Find(PopupName).gameObject;
        ctrl.sideRoot  = t.Find(SideName).gameObject;
        EnsureQuestButton(canvas, ctrl);
        ctrl.popupRoot.SetActive(false);
        ctrl.sideRoot.SetActive(false);
        return root;
    }

    [MenuItem("Game/Use Objective Redesign Prefab (current scene)")]
    public static void UsePrefabInCurrentScene()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogWarning("[ObjectiveRedesign] Prefab belum ada — jalankan 'Create-Update Objective Redesign Prefab' dulu."); return; }

        // idempotent: hapus instance prefab lama + objek baked yang lepas
        foreach (var go in FindAllByName(PrefabRootName)) { if (go) Object.DestroyImmediate(go); }
        foreach (var n in new[]{ PopupName, SideName, QuestButtonName, ControllerName })
            foreach (var go in FindAllByName(n)) { if (go) Object.DestroyImmediate(go); }

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var ctrl = inst.GetComponent<ObjectiveRedesignUI>();

        int wired = 0;
        foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>(true))
            if (mb is IObjectivePanel && !(mb is ObjectiveRedesignUI))
            {
                var f = mb.GetType().GetField("redesign");
                if (f != null) { f.SetValue(mb, ctrl); EditorUtility.SetDirty(mb); wired++; }
            }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[ObjectiveRedesign] Prefab dipasang di '" + scene.name + "', wired " + wired + " panel.");
    }

    static System.Collections.Generic.List<GameObject> FindAllByName(string name)
    {
        var list = new System.Collections.Generic.List<GameObject>();
        foreach (var tr in Object.FindObjectsOfType<Transform>(true))
            if (tr != null && tr.name == name) list.Add(tr.gameObject);
        return list;
    }

    // =====================================================================
    // PANEL 1 — CENTER POPUP (foto 1): diamond glow + cream card + OBJECTIVE
    // =====================================================================
    static void BuildPopup(Transform canvas)
    {
        var root = NewRect(PopupName, canvas);
        Stretch(root, 0, 0, 0, 0);                 // full screen
        var cg = root.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

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
        cardImg.OutlineWidth = F(2f);
        Rounded(cardImg, 10f);

        var txt = Label("txtObjective", card, "Sapa rekan kerja di pagi hari",
                        17f, DarkText, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(txt.rectTransform, 16f, 6f, 16f, 6f);
        txt.enableAutoSizing = true; txt.fontSizeMin = F(11f); txt.fontSizeMax = F(18f);

        // Icon bulat + label OBJECTIVE
        var icon = NewRect("IconCircle", root);
        Anchor(icon, Center, Center, new Vector2(26f, 26f), new Vector2(0f, -50f));
        var iconImg = Shape(icon, DrawShape.Circle, Orange);
        iconImg.StrokeWidth = F(2f);
        var glyph = Label("txtIcon", icon, "❐", 12f, Orange, TextAlignmentOptions.Center);
        Stretch(glyph.rectTransform, 0, 0, 0, 0);

        var lbl = Label("txtLabel", root, "OBJECTIVE", 11f, Orange,
                        TextAlignmentOptions.Center, FontStyles.Bold);
        Anchor(lbl.rectTransform, Center, Center, new Vector2(220f, 18f), new Vector2(0f, -70f));
        lbl.characterSpacing = F(8f);
    }

    static void Diamond(Transform parent, string name, float size, float stroke, Color color, float falloff)
    {
        var rt = NewRect(name, parent);
        Anchor(rt, Center, Center, new Vector2(size, size), Vector2.zero);
        rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var img = Shape(rt, DrawShape.Rectangle, color);
        img.StrokeWidth = F(stroke);
        img.FalloffDistance = F(falloff);
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
        bgImg.OutlineWidth = F(1.5f);
        Rounded(bgImg, 11f);

        // Accent bar oranye (kiri)
        var bar = NewRect("AccentBar", root);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(0f, 1f);
        bar.pivot = new Vector2(0f, 0.5f);
        bar.offsetMin = V(7f, 11f);   // x=7 dari kiri, bawah inset 11
        bar.offsetMax = V(11f, -11f); // width 4 (11-7), atas inset 11
        var barImg = Shape(bar, DrawShape.Rectangle, Orange);
        Rounded(barImg, 2f);

        // Header
        var hdr = Label("txtDetailHeader", root, "TARGET DETAIL", 11f, Orange,
                        TextAlignmentOptions.TopLeft, FontStyles.Bold);
        Anchor(hdr.rectTransform, TopLeft, TopLeft, new Vector2(180f, 16f), new Vector2(18f, -13f));
        hdr.characterSpacing = F(4f);

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
        div.offsetMin = V(18f, -34f);
        div.offsetMax = V(-14f, -32.5f);
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
        body.rectTransform.offsetMin = V(18f, -98f);
        body.rectTransform.offsetMax = V(-14f, -40f);
        body.enableAutoSizing = true; body.fontSizeMin = F(10f); body.fontSizeMax = F(13f);

        // Location (opsional — boleh dikosongkan nanti)
        var loc = Label("txtDetailLocation", root, "◎  RUANG IT · LANTAI 2",
                        10f, MutedText, TextAlignmentOptions.Left);
        Anchor(loc.rectTransform, BottomLeft, BottomLeft, new Vector2(220f, 14f), new Vector2(18f, 10f));
    }

    // =====================================================================
    // QUEST BOX — kotak kecil kanan atas (collapsed). Diklik/key buat munculin side card.
    // =====================================================================
    public static void EnsureQuestButton(Canvas canvas, ObjectiveRedesignUI controller)
    {
        S = ComputeScale(canvas);
        var t = canvas.transform;
        var existing = t.Find(QuestButtonName);
        var btnGo = existing != null ? existing.gameObject : BuildQuestButton(t);
        controller.questButton = btnGo;
        btnGo.SetActive(false);
        EditorUtility.SetDirty(controller);
    }

    static GameObject BuildQuestButton(Transform canvas)
    {
        var root = NewRect(QuestButtonName, canvas);
        Anchor(root, TopRight, TopRight, new Vector2(40f, 40f), new Vector2(-14f, -14f));
        var bg = Shape(root, DrawShape.Rectangle, new Color(SideBG.r, SideBG.g, SideBG.b, 0.85f));
        bg.raycastTarget = true; // clickable
        bg.OutlineColor = new Color(Orange.r, Orange.g, Orange.b, 0.7f);
        bg.OutlineWidth = F(1.5f);
        Rounded(bg, 9f);
        var btn = root.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        var icon = Label("txtQuestIcon", root, "!", 20f, Orange, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(icon.rectTransform, 0, 0, 0, 0);
        return root.gameObject;
    }

    // =====================================================================
    // Helpers (auto-apply scale S to sizes/positions/paddings/fonts/radii)
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

    // Full-stretch with padding (left, top, right, bottom) — scaled
    static void Stretch(RectTransform rt, float l, float t, float r, float b)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = Center;
        rt.offsetMin = V(l, b);
        rt.offsetMax = V(-r, -t);
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = new Vector2(size.x * S, size.y * S);
        rt.anchoredPosition = new Vector2(pos.x * S, pos.y * S);
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
        float rr = radius * S;
        r.CornerRadius = new Vector4(rr, rr, rr, rr);
        img.Rectangle = r;
    }

    static TextMeshProUGUI Label(string name, Transform parent, string text, float size,
                                 Color color, TextAlignmentOptions align,
                                 FontStyles style = FontStyles.Normal)
    {
        var rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size * S;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.raycastTarget = false;
        t.enableWordWrapping = true;
        return t;
    }
}
#endif
