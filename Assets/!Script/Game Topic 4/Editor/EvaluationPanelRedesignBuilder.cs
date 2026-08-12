using System.Collections.Generic;
using MPUIKIT;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Membangun ulang QuestionPanel + ResultPanel evaluasi dengan desain gelap-oranye.
/// Semua background pakai MPImage. Menu:
///   Tools > Evaluation > Redesign Evaluation Panel (Active Scene)
///   Tools > Evaluation > Redesign All Build Scenes
///
/// Dukung dua komponen panel yang isinya sama tapi beda kelas:
///   - EvaluationPanel_Tp4 (Topic 1, 2, 4, 5, 6)
///   - EvaluationPanel     (Topic 3, versi email)
///
/// Aman dipanggil berulang: isi lama QuestionPanel/ResultPanel dihapus lalu dibangun ulang.
/// GameObject panel/QuestionPanel/ResultPanel sendiri TIDAK diganti supaya referensi di
/// EvaluationManager_* tetap nyambung.
/// </summary>
public static class EvaluationPanelRedesignBuilder
{
    // ── palette ──
    static readonly Color cScrim      = Hex(0x0B, 0x09, 0x08, 0.94f);
    static readonly Color cCard       = Hex(0x17, 0x13, 0x10);
    static readonly Color cBorder     = Hex(0x2C, 0x23, 0x1D);
    static readonly Color cChoiceOff  = Hex(0x22, 0x1B, 0x16);
    static readonly Color cChoiceOn   = Hex(0x33, 0x21, 0x0F);
    static readonly Color cAccent     = Hex(0xF0, 0x84, 0x2B);
    static readonly Color cOnAccent   = Hex(0x1B, 0x12, 0x06);
    static readonly Color cText       = Hex(0xF5, 0xED, 0xE3);
    static readonly Color cDim        = Hex(0xA0, 0x93, 0x8A);
    static readonly Color cFaint      = Hex(0x6E, 0x5C, 0x4E);
    static readonly Color cBadgeOff   = Hex(0x2E, 0x26, 0x21);
    static readonly Color cBadgeOffTx = Hex(0xB5, 0xA7, 0x9C);
    static readonly Color cTrackOff   = Hex(0x3A, 0x2F, 0x28);
    static readonly Color cGreen      = Hex(0x4A, 0xDE, 0x9B);
    static readonly Color cGreenBg    = Hex(0x14, 0x3A, 0x2C);
    static readonly Color cWrongBg    = Hex(0x22, 0x15, 0x0F);
    static readonly Color cWrongIcon  = Hex(0x3B, 0x21, 0x13);
    static readonly Color cDivider    = Hex(0x24, 0x1C, 0x17);

    const string FontPath = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset";
    static TMP_FontAsset _font;

    // ── ukuran desain (dirancang untuk canvas 1920x1080, diskalakan otomatis) ──
    const float QPanelW = 1180f, QPanelH = 760f;
    const float RPanelW = 1180f, RPanelH = 800f;

    /// <summary>Label topik per scene. Key = nama file scene tanpa ekstensi.</summary>
    static readonly Dictionary<string, string> TopicLabels = new Dictionary<string, string>
    {
        { "Privasi_Keamanan",     "PHISHING & REKAYASA SOSIAL" },
        { "Office_Environment",   "KEAMANAN KATA SANDI & MFA" },
        { "Map_Topic_3",          "KEAMANAN EMAIL & PHISHING" },
        { "Map_Topic4",           "KEAMANAN URL & SITUS WEB" },
        { "Computer_Interaction", "KEAMANAN URL & SITUS WEB" },
        { "Map_Topic_5",          "KEAMANAN WI-FI PUBLIK" },
        { "Map_Topic6",           "RANSOMWARE & BACKUP" },
        { "computer_interaction", "RANSOMWARE & BACKUP" },
    };

    // ══════════════════════════════════════════════════════════════════════════
    // ENTRY POINTS
    // ══════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Evaluation/Redesign Evaluation Panel (Active Scene)")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        int n = BuildActiveScene();
        if (n > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[EvalRedesign] {scene.name}: {n} panel dibangun ulang. Jangan lupa save scene.");
        }
        else
        {
            Debug.LogWarning($"[EvalRedesign] {scene.name}: tidak ada panel evaluasi yang ketemu.");
        }
    }

    [MenuItem("Tools/Evaluation/Redesign All Build Scenes")]
    public static void BuildAll()
    {
        var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (current.isDirty)
        {
            Debug.LogError("[EvalRedesign] Dibatalkan — scene aktif masih ada perubahan yang belum disimpan. Save dulu.");
            return;
        }

        var report = new System.Text.StringBuilder("[EvalRedesign] Hasil batch:\n");
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!s.enabled) continue;
            if (!s.path.Contains("/Game/")) continue;   // scene menu/auth dilewati

            EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            int n = BuildActiveScene();
            if (n > 0)
            {
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
            }
            report.AppendLine($"  {System.IO.Path.GetFileNameWithoutExtension(s.path)}: {n} panel");
        }
        AssetDatabase.SaveAssets();
        Debug.Log(report.ToString());
    }

    /// <summary>Bangun ulang semua panel evaluasi di scene aktif. Return jumlah panel.</summary>
    static int BuildActiveScene()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        string label;
        if (!TopicLabels.TryGetValue(scene.name, out label))
            label = "EVALUASI";

        int count = 0;

        foreach (var p in Object.FindObjectsOfType<EvaluationPanel_Tp4>(true))
        {
            QRefs q; RRefs r;
            if (!BuildPanel(p, p.questionContainer, p.resultContainer, label, out q, out r)) continue;

            p.questionContainer = q.container;
            p.txtQuestionNum = q.num;
            p.txtQuestionText = q.qText;
            p.txtTopicLabel = q.topic;
            p.btnChoiceA = q.a;
            p.btnChoiceB = q.b;
            p.btnNext = q.next;
            p.btnBack = q.back;
            p.progressContainer = q.progress;
            p.progressSegmentTemplate = q.segTemplate;
            p.labelChoiceA = q.labelA;
            p.labelChoiceB = q.labelB;
            p.badgeChoiceA = q.badgeA;
            p.badgeChoiceB = q.badgeB;
            p.badgeTextA = q.badgeTxtA;
            p.badgeTextB = q.badgeTxtB;

            p.resultContainer = r.container;
            p.txtResultScore = r.score;
            p.txtResultScoreTotal = r.total;
            p.txtResultDetail = null;
            p.resultRowsContainer = r.rows;
            p.resultRowTemplate = r.rowTemplate;
            p.btnSelesai = r.selesai;

            p.topicLabel = "";
            p.defaultColor = cChoiceOff;
            p.selectedColor = cChoiceOn;
            p.choiceTextDefaultColor = cText;
            p.choiceTextSelectedColor = cText;
            p.choiceOutlineWidth = 2f;
            p.choiceOutlineDefaultColor = new Color(0, 0, 0, 0);
            p.choiceOutlineSelectedColor = cAccent;
            p.badgeDefaultColor = cBadgeOff;
            p.badgeSelectedColor = cAccent;
            p.badgeTextDefaultColor = cBadgeOffTx;
            p.badgeTextSelectedColor = cOnAccent;
            p.progressOnColor = cAccent;
            p.progressOffColor = cTrackOff;

            EditorUtility.SetDirty(p);
            count++;
        }

        foreach (var p in Object.FindObjectsOfType<EvaluationPanel>(true))
        {
            QRefs q; RRefs r;
            if (!BuildPanel(p, p.questionContainer, p.resultContainer, label, out q, out r)) continue;

            p.questionContainer = q.container;
            p.txtQuestionNumber = q.num;
            p.txtQuestionText = q.qText;
            p.txtTopicLabel = q.topic;
            p.btnChoiceA = q.a;
            p.btnChoiceB = q.b;
            p.txtChoiceA = q.labelA;
            p.txtChoiceB = q.labelB;
            p.btnNext = q.next;
            p.btnBack = q.back;
            p.progressContainer = q.progress;
            p.progressSegmentTemplate = q.segTemplate;
            p.useChoiceBadges = true;
            p.badgeChoiceA = q.badgeA;
            p.badgeChoiceB = q.badgeB;
            p.badgeTextA = q.badgeTxtA;
            p.badgeTextB = q.badgeTxtB;

            p.resultContainer = r.container;
            p.txtResultScore = r.score;
            p.txtResultScoreTotal = r.total;
            p.txtResultDetail = null;
            p.resultRowsContainer = r.rows;
            p.resultRowTemplate = r.rowTemplate;
            p.btnSelesai = r.selesai;

            p.topicLabel = "";
            p.defaultColor = cChoiceOff;
            p.selectedColor = cChoiceOn;
            p.choiceTextDefaultColor = cText;
            p.choiceTextSelectedColor = cText;
            p.choiceOutlineWidth = 2f;
            p.choiceOutlineDefaultColor = new Color(0, 0, 0, 0);
            p.choiceOutlineSelectedColor = cAccent;
            p.badgeDefaultColor = cBadgeOff;
            p.badgeSelectedColor = cAccent;
            p.badgeTextDefaultColor = cBadgeOffTx;
            p.badgeTextSelectedColor = cOnAccent;
            p.progressOnColor = cAccent;
            p.progressOffColor = cTrackOff;

            EditorUtility.SetDirty(p);
            count++;
        }

        return count;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SHARED
    // ══════════════════════════════════════════════════════════════════════════

    class QRefs
    {
        public GameObject container;
        public TMP_Text num, qText, topic, labelA, labelB, badgeTxtA, badgeTxtB;
        public Button a, b, next, back;
        public RectTransform progress;
        public GameObject segTemplate;
        public MPImage badgeA, badgeB;
    }

    class RRefs
    {
        public GameObject container;
        public TMP_Text score, total;
        public RectTransform rows;
        public EvaluationResultRow_Tp4 rowTemplate;
        public Button selesai;
    }

    static bool BuildPanel(MonoBehaviour panel, GameObject questionContainer, GameObject resultContainer,
        string topicLabel, out QRefs q, out RRefs r)
    {
        q = null; r = null;

        Transform root = panel.transform;
        Transform qp = questionContainer != null ? questionContainer.transform : root.Find("QuestionPanel");
        Transform rp = resultContainer != null ? resultContainer.transform : root.Find("ResultPanel");
        if (qp == null || rp == null)
        {
            Debug.LogError($"[EvalRedesign] QuestionPanel/ResultPanel tidak ketemu di {panel.name} ({panel.GetType().Name})");
            return false;
        }

        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Redesign Evaluation Panel");

        // Root panel jadi scrim gelap full-canvas.
        var rootRect = root as RectTransform;
        if (rootRect != null)
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }
        var scrim = root.GetComponent<Graphic>();
        if (scrim == null) scrim = AddMP(root.gameObject, cScrim, Vector4.zero);
        scrim.color = cScrim;
        var scrimMp = scrim as MPImage;
        if (scrimMp != null) SetRadius(scrimMp, Vector4.zero);
        // Beberapa scene pakai Image biasa dengan sprite 'Background' sliced — dibuang
        // supaya scrim-nya rata gelap, bukan kotak membulat.
        var scrimImg = scrim as Image;
        if (scrimImg != null && scrimMp == null) scrimImg.sprite = null;
        scrim.raycastTarget = true;

        // Canvas: EvaluationCanvas yang isinya cuma panel ini di-upgrade ke 1920x1080.
        // Canvas bersama (mis. 'Canvas Email' di Topic 3) dibiarkan, panelnya yang diskalakan.
        float scale = 1f;
        var canvas = panel.GetComponentInParent<Canvas>(true);
        if (canvas != null)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            bool dedicated = canvas.transform.childCount == 1 &&
                             canvas.name.ToLowerInvariant().Contains("evaluation");
            if (scaler != null && dedicated)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                EditorUtility.SetDirty(scaler);
            }
            Vector2 refRes = scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                ? scaler.referenceResolution
                : new Vector2(1920, 1080);
            scale = Mathf.Min(1f, (refRes.x - 48f) / QPanelW, (refRes.y - 48f) / RPanelH);
        }

        ClearChildren(qp);
        ClearChildren(rp);

        q = BuildQuestionPanel(qp, topicLabel, scale);
        r = BuildResultPanel(rp, scale);

        qp.gameObject.SetActive(true);
        rp.gameObject.SetActive(false);

        EditorUtility.SetDirty(root.gameObject);
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // QUESTION PANEL
    // ══════════════════════════════════════════════════════════════════════════
    static QRefs BuildQuestionPanel(Transform qp, string topicLabel, float scale)
    {
        var q = new QRefs { container = qp.gameObject };

        var qpRect = qp.GetComponent<RectTransform>();
        qpRect.anchorMin = qpRect.anchorMax = qpRect.pivot = new Vector2(0.5f, 0.5f);
        qpRect.anchoredPosition = Vector2.zero;
        qpRect.sizeDelta = new Vector2(QPanelW, QPanelH);
        qpRect.localScale = Vector3.one * scale;

        // QuestionPanel cuma wadah transparan; kartu gelapnya anak sendiri (sesuai mockup:
        // header + progress + tombol berada DI LUAR kartu).
        HideGraphic(qp.gameObject);

        // ── header kiri: "EVALUASI DIRI | <TOPIK>" ──
        GameObject headerLeft = MakeGO("HeaderLeft", qp);
        Anchored(headerLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -8), new Vector2(700, 32));
        var hl = headerLeft.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 16;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;
        var hlFit = headerLeft.AddComponent<ContentSizeFitter>();
        hlFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject eyebrow = MakeGO("TxtEyebrow", headerLeft.transform);
        AddText(eyebrow, "EVALUASI DIRI", 20, cAccent, TextAlignmentOptions.MidlineLeft, true, 8f);

        GameObject sep = MakeGO("Separator", headerLeft.transform);
        AddMP(sep, cTrackOff, new Vector4(1, 1, 1, 1));
        var sepLe = sep.AddComponent<LayoutElement>();
        sepLe.preferredWidth = 2; sepLe.minWidth = 2;
        sepLe.preferredHeight = 20; sepLe.minHeight = 20;

        GameObject topic = MakeGO("TxtTopicLabel", headerLeft.transform);
        q.topic = AddText(topic, topicLabel, 20, cText, TextAlignmentOptions.MidlineLeft, true, 8f);

        // ── header kanan: "Pertanyaan 1 dari 5" ──
        GameObject num = MakeGO("TxtQuestionNum", qp);
        Anchored(num, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, -8), new Vector2(420, 32));
        q.num = AddText(num, "Pertanyaan 1 dari 5", 21, cDim, TextAlignmentOptions.MidlineRight, false);

        // ── progress bar (segmen dibuat runtime dari template) ──
        GameObject progress = MakeGO("ProgressRow", qp);
        Stretch(progress, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), new Vector2(0, -56));
        var pl = progress.AddComponent<HorizontalLayoutGroup>();
        pl.spacing = 14;
        pl.childControlWidth = true;
        pl.childControlHeight = true;
        pl.childForceExpandWidth = true;
        pl.childForceExpandHeight = true;
        q.progress = progress.GetComponent<RectTransform>();

        GameObject segTemplate = MakeGO("SegmentTemplate", progress.transform);
        AddMP(segTemplate, cTrackOff, new Vector4(4, 4, 4, 4));
        segTemplate.SetActive(false);
        q.segTemplate = segTemplate;

        // ── kartu soal ──
        GameObject card = MakeGO("Card", qp);
        Stretch(card, Vector2.zero, Vector2.one, new Vector2(0, 92), new Vector2(0, -96));
        AddMP(card, cCard, new Vector4(24, 24, 24, 24), 1.5f, cBorder);

        GameObject qText = MakeGO("TxtQuestionText", card.transform);
        Stretch(qText, new Vector2(0, 1), new Vector2(1, 1), new Vector2(64, -336), new Vector2(-64, -72));
        q.qText = AddText(qText, "Pertanyaan", 48, cText, TextAlignmentOptions.TopLeft, true);
        q.qText.enableAutoSizing = true;
        q.qText.fontSizeMin = 30;
        q.qText.fontSizeMax = 48;
        q.qText.lineSpacing = -8f;

        GameObject choiceRow = MakeGO("ChoiceRow", card.transform);
        Stretch(choiceRow, Vector2.zero, new Vector2(1, 0), new Vector2(64, 72), new Vector2(-64, 188));
        var cl = choiceRow.AddComponent<HorizontalLayoutGroup>();
        cl.spacing = 28;
        cl.childControlWidth = true;
        cl.childControlHeight = true;
        cl.childForceExpandWidth = true;
        cl.childForceExpandHeight = true;

        q.a = BuildChoice(choiceRow.transform, "ButtonChoiceA", "A", out q.badgeA, out q.badgeTxtA, out q.labelA);
        q.b = BuildChoice(choiceRow.transform, "ButtonChoiceB", "B", out q.badgeB, out q.badgeTxtB, out q.labelB);

        // ── footer: Kembali (kiri) & Selanjutnya (kanan) ──
        GameObject back = MakeGO("BtnBack", qp);
        Anchored(back, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 34), new Vector2(200, 68));
        MPImage backImg = AddMP(back, cChoiceOff, new Vector4(16, 16, 16, 16), 1.5f, cTrackOff);
        q.back = AddButton(back, backImg, Hex(0xC8, 0xC8, 0xC8));
        GameObject backChev = MakeGO("Chevron", back.transform);
        Anchored(backChev, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28, 0), new Vector2(20, 40));
        AddText(backChev, "‹", 28, cDim, TextAlignmentOptions.Center, true);
        GameObject backLbl = MakeGO("Label", back.transform);
        Stretch(backLbl, Vector2.zero, Vector2.one, new Vector2(46, 0), new Vector2(-18, 0));
        AddText(backLbl, "Kembali", 24, cText, TextAlignmentOptions.Center, true);

        GameObject next = MakeGO("BtnNext", qp);
        Anchored(next, new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 34), new Vector2(260, 68));
        MPImage nextImg = AddMP(next, cAccent, new Vector4(16, 16, 16, 16));
        q.next = AddButton(next, nextImg, Hex(0xDC, 0xDC, 0xDC));
        GameObject nextLbl = MakeGO("Label", next.transform);   // harus anak PERTAMA (dipakai GetComponentInChildren)
        Stretch(nextLbl, Vector2.zero, Vector2.one, new Vector2(24, 0), new Vector2(-46, 0));
        AddText(nextLbl, "Selanjutnya", 24, cOnAccent, TextAlignmentOptions.Center, true);
        GameObject nextChev = MakeGO("Chevron", next.transform);
        Anchored(nextChev, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-28, 0), new Vector2(20, 40));
        AddText(nextChev, "›", 28, cOnAccent, TextAlignmentOptions.Center, true);

        return q;
    }

    static Button BuildChoice(Transform parent, string name, string letter,
        out MPImage badge, out TMP_Text badgeText, out TMP_Text label)
    {
        GameObject go = MakeGO(name, parent);
        MPImage img = AddMP(go, cChoiceOff, new Vector4(18, 18, 18, 18), 2f, new Color(0, 0, 0, 0));
        Button btn = AddButton(go, img, Color.white);

        // Label ditaruh duluan supaya GetComponentInChildren<TMP_Text> dapat teks pilihan,
        // bukan huruf badge.
        GameObject lbl = MakeGO("Label", go.transform);
        Stretch(lbl, Vector2.zero, Vector2.one, new Vector2(96, 0), new Vector2(-24, 0));
        label = AddText(lbl, "Ya", 30, cText, TextAlignmentOptions.MidlineLeft, true);

        GameObject badgeGo = MakeGO("Badge", go.transform);
        Anchored(badgeGo, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(52, 0), new Vector2(56, 56));
        badge = AddMP(badgeGo, cBadgeOff, new Vector4(14, 14, 14, 14));

        GameObject badgeTxtGo = MakeGO("BadgeText", badgeGo.transform);
        Stretch(badgeTxtGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        badgeText = AddText(badgeTxtGo, letter, 22, cBadgeOffTx, TextAlignmentOptions.Center, true);

        return btn;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // RESULT PANEL
    // ══════════════════════════════════════════════════════════════════════════
    static RRefs BuildResultPanel(Transform rp, float scale)
    {
        var r = new RRefs { container = rp.gameObject };

        var rpRect = rp.GetComponent<RectTransform>();
        rpRect.anchorMin = rpRect.anchorMax = rpRect.pivot = new Vector2(0.5f, 0.5f);
        rpRect.anchoredPosition = Vector2.zero;
        rpRect.sizeDelta = new Vector2(RPanelW, RPanelH);
        rpRect.localScale = Vector3.one * scale;

        HideGraphic(rp.gameObject);

        // ── badge skor ──
        GameObject badge = MakeGO("ScoreBadge", rp);
        Anchored(badge, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(150, 150));
        AddMP(badge, Hex(0x1C, 0x15, 0x12), new Vector4(24, 24, 24, 24), 2f, cAccent);

        GameObject scoreRow = MakeGO("ScoreRow", badge.transform);
        Stretch(scoreRow, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -104), new Vector2(0, -20));
        var sl = scoreRow.AddComponent<HorizontalLayoutGroup>();
        sl.spacing = 2;
        sl.childAlignment = TextAnchor.LowerCenter;
        sl.childControlWidth = true;
        sl.childControlHeight = true;
        sl.childForceExpandWidth = false;
        sl.childForceExpandHeight = false;

        GameObject scoreBig = MakeGO("TxtResultScore", scoreRow.transform);
        r.score = AddText(scoreBig, "0", 64, cText, TextAlignmentOptions.BottomRight, true);

        GameObject scoreTotal = MakeGO("TxtResultScoreTotal", scoreRow.transform);
        r.total = AddText(scoreTotal, "/5", 30, cDim, TextAlignmentOptions.BottomLeft, true);

        GameObject benar = MakeGO("TxtBenarLabel", badge.transform);
        Stretch(benar, Vector2.zero, new Vector2(1, 0), new Vector2(0, 22), new Vector2(0, 46));
        AddText(benar, "BENAR", 14, cAccent, TextAlignmentOptions.Center, true, 6f);

        // ── judul ──
        GameObject title = MakeGO("TxtRekapTitle", rp);
        Stretch(title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(184, -110), new Vector2(-24, -38));
        AddText(title, "Rekap Evaluasi", 54, cText, TextAlignmentOptions.MidlineLeft, true);

        // ── kartu daftar ──
        GameObject listCard = MakeGO("ListCard", rp);
        Stretch(listCard, Vector2.zero, Vector2.one, new Vector2(0, 104), new Vector2(0, -180));
        AddMP(listCard, cCard, new Vector4(24, 24, 24, 24), 1.5f, cBorder);

        GameObject rows = MakeGO("RowsContainer", listCard.transform);
        Stretch(rows, Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -14));
        var rl = rows.AddComponent<VerticalLayoutGroup>();
        rl.spacing = 0;
        rl.childControlWidth = true;
        rl.childControlHeight = true;
        rl.childForceExpandWidth = true;
        rl.childForceExpandHeight = true;
        r.rows = rows.GetComponent<RectTransform>();

        r.rowTemplate = BuildRowTemplate(rows.transform);

        // ── tombol bawah ──
        GameObject done = MakeGO("ButtonSelesai", rp);
        Stretch(done, Vector2.zero, new Vector2(1, 0), new Vector2(0, 8), new Vector2(0, 80));
        MPImage doneImg = AddMP(done, cAccent, new Vector4(16, 16, 16, 16));
        r.selesai = AddButton(done, doneImg, Hex(0xDC, 0xDC, 0xDC));
        // Teks tombol ini nggak pernah ditimpa script, jadi chevron-nya bisa nyatu di satu TMP
        // supaya label + chevron selalu ke-center bareng.
        GameObject doneLbl = MakeGO("Label", done.transform);
        Stretch(doneLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddText(doneLbl, "Selanjutnya   ›", 26, cOnAccent, TextAlignmentOptions.Center, true);

        return r;
    }

    static EvaluationResultRow_Tp4 BuildRowTemplate(Transform parent)
    {
        GameObject row = MakeGO("RowTemplate", parent);
        MPImage rowBg = AddMP(row, new Color(0, 0, 0, 0), new Vector4(14, 14, 14, 14));
        rowBg.raycastTarget = false;
        var le = row.AddComponent<LayoutElement>();
        le.minHeight = 74;
        le.flexibleHeight = 1;

        GameObject iconBox = MakeGO("IconBox", row.transform);
        Anchored(iconBox, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46, 0), new Vector2(48, 48));
        MPImage iconImg = AddMP(iconBox, cGreenBg, new Vector4(14, 14, 14, 14));
        iconImg.raycastTarget = false;

        // Roboto nggak punya glyph centang/silang, jadi ikonnya digambar pakai batang MPImage.
        GameObject check = MakeGO("IconCheck", iconBox.transform);
        Stretch(check, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddBar(check.transform, "Bar1", new Vector2(-9, -4), new Vector2(18, 6), -45f, cGreen);
        AddBar(check.transform, "Bar2", new Vector2(5, 2), new Vector2(28, 6), 45f, cGreen);

        GameObject cross = MakeGO("IconCross", iconBox.transform);
        Stretch(cross, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddBar(cross.transform, "Bar1", Vector2.zero, new Vector2(26, 6), 45f, cAccent);
        AddBar(cross.transform, "Bar2", Vector2.zero, new Vector2(26, 6), -45f, cAccent);
        cross.SetActive(false);

        GameObject idx = MakeGO("TxtIndex", row.transform);
        Anchored(idx, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(84, 0), new Vector2(34, 28));
        var idxTxt = AddText(idx, "01", 15, cFaint, TextAlignmentOptions.MidlineLeft, true, 2f);

        GameObject qGo = MakeGO("TxtQuestion", row.transform);
        Stretch(qGo, Vector2.zero, Vector2.one, new Vector2(124, 6), new Vector2(-336, -6));
        var qTxt = AddText(qGo, "Pertanyaan", 22, cText, TextAlignmentOptions.MidlineLeft, true);
        qTxt.enableAutoSizing = true;
        qTxt.fontSizeMin = 15;
        qTxt.fontSizeMax = 22;

        GameObject caption = MakeGO("TxtCaption", row.transform);
        Anchored(caption, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-118, 0), new Vector2(190, 24));
        AddText(caption, "JAWABAN ANDA", 13, cFaint, TextAlignmentOptions.MidlineRight, true, 4f);

        GameObject pill = MakeGO("AnswerPill", row.transform);
        Anchored(pill, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(88, 40));
        MPImage pillImg = AddMP(pill, cGreenBg, new Vector4(10, 10, 10, 10));
        pillImg.raycastTarget = false;

        GameObject ans = MakeGO("TxtAnswer", pill.transform);
        Stretch(ans, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var ansTxt = AddText(ans, "Ya", 19, cGreen, TextAlignmentOptions.Center, true);

        GameObject divider = MakeGO("Divider", row.transform);
        Stretch(divider, Vector2.zero, new Vector2(1, 0), new Vector2(14, 0), new Vector2(-14, 1));
        var divImg = AddMP(divider, cDivider, Vector4.zero);
        divImg.raycastTarget = false;

        var comp = row.AddComponent<EvaluationResultRow_Tp4>();
        comp.rowBg = rowBg;
        comp.iconBox = iconImg;
        comp.iconCheck = check;
        comp.iconCross = cross;
        comp.txtIndex = idxTxt;
        comp.txtQuestion = qTxt;
        comp.answerPill = pillImg;
        comp.txtAnswer = ansTxt;
        comp.divider = divider;
        comp.correctRowBg = new Color(0, 0, 0, 0);
        comp.correctIconBg = cGreenBg;
        comp.correctPillBg = cGreenBg;
        comp.correctPillText = cGreen;
        comp.wrongRowBg = cWrongBg;
        comp.wrongIconBg = cWrongIcon;
        comp.wrongPillBg = cWrongIcon;
        comp.wrongPillText = cAccent;

        row.SetActive(false);
        return comp;
    }

    static void AddBar(Transform parent, string name, Vector2 pos, Vector2 size, float rotZ, Color col)
    {
        GameObject bar = MakeGO(name, parent);
        Anchored(bar, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var img = AddMP(bar, col, new Vector4(3, 3, 3, 3));
        img.raycastTarget = false;
        bar.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, rotZ);
    }

    // ── helpers ──
    static Color Hex(int r, int g, int b, float a = 1f) =>
        new Color(r / 255f, g / 255f, b / 255f, a);

    /// <summary>Bikin Graphic yang sudah ada jadi tak terlihat, tanpa menghapus komponennya.</summary>
    static void HideGraphic(GameObject go)
    {
        var g = go.GetComponent<Graphic>();
        if (g == null) return;
        g.color = new Color(0f, 0f, 0f, 0f);
        g.raycastTarget = false;
        var mp = g as MPImage;
        if (mp != null) SetRadius(mp, Vector4.zero);
        var img = g as Image;
        if (img != null && mp == null) img.sprite = null;
    }

    static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return go;
    }

    static void Stretch(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = aMin;
        r.anchorMax = aMax;
        r.offsetMin = oMin;
        r.offsetMax = oMax;
    }

    static void Anchored(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = pivot;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    static MPImage AddMP(GameObject go, Color col, Vector4 radius, float outlineWidth = 0f, Color outlineColor = default)
    {
        var img = go.AddComponent<MPImage>();
        img.color = col;
        img.DrawShape = DrawShape.Rectangle;
        SetRadius(img, radius);
        if (outlineWidth > 0f)
        {
            img.OutlineWidth = outlineWidth;
            img.OutlineColor = outlineColor;
        }
        return img;
    }

    static void SetRadius(MPImage img, Vector4 radius)
    {
        Rectangle rect = img.Rectangle;
        rect.CornerRadius = radius;
        img.Rectangle = rect;
    }

    static Button AddButton(GameObject go, Graphic target, Color highlighted)
    {
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = target;
        btn.transition = Selectable.Transition.ColorTint;
        var c = btn.colors;
        c.normalColor = Color.white;
        c.highlightedColor = highlighted;
        c.pressedColor = Hex(0xB4, 0xB4, 0xB4);
        c.selectedColor = Color.white;
        c.disabledColor = Hex(0x80, 0x80, 0x80, 0.5f);
        c.fadeDuration = 0.08f;
        btn.colors = c;
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        return btn;
    }

    static TextMeshProUGUI AddText(GameObject go, string txt, float size, Color col,
        TextAlignmentOptions align, bool bold, float charSpacing = 0f)
    {
        var t = go.AddComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = txt;
        t.fontSize = size;
        t.color = col;
        t.alignment = align;
        if (bold) t.fontStyle = FontStyles.Bold;
        t.characterSpacing = charSpacing;
        t.raycastTarget = false;
        return t;
    }
}
