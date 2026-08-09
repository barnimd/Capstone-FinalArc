using System.Collections.Generic;
using System.Linq;
using MPUIKIT;   // catatan: asmdef-nya "MPUIKit" tapi namespace-nya MPUIKIT
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pasang panel Settings in-game + Control Legend ke scene topic yang belum punya.
/// Cetakannya diambil dari Map_Topic_3 (CanvasPause/PauseController).
///
/// Cara kerjanya:
///   1. Cari <see cref="PausePanel"/> di scene aktif, ambil pausedPanel-nya.
///   2. Duplikat panel itu jadi SettingsPanel (biar style/warna ikut otomatis),
///      buang tombol yang gak kepake, sisain satu jadi Btn_Back.
///   3. Bikin slider + 2 toggle + label pakai warna yang di-sample dari panel asal.
///   4. Sisipin Btn_Settings ke menu pause, rapiin ulang posisi 4 tombolnya.
///   5. Drag prefab ControlLegend, isi hideWhenAnyActive dari tabel per scene.
///   6. Kalau scene gak punya ObjectiveRedesignUI, entry Q/OBJECTIVE dibuang
///      dan kartu legend di-reflow biar gak nyisain celah.
///
/// Aman dijalanin berkali-kali — SettingsPanel & Btn_Settings lama dibuang dulu.
/// </summary>
public static class SettingsLegendPortSetup
{
    private const string LEGEND_PREFAB_PATH = "Assets/!Prefab/UI/ControlLegend.prefab";

    // ── Layout panel Settings (nyontek persis dari Map_Topic_3) ──────────────
    private const float PANEL_W = 460f;

    // Posisi Y tiap tombol di menu pause setelah Btn_Settings disisipin
    private const float BTN_RESUME_Y   = -300f;
    private const float BTN_SETTINGS_Y = -380f;
    private const float BTN_RETRY_Y    = -460f;
    private const float BTN_HOME_Y     = -540f;

    // ── Layout kartu legend (angka diambil dari ControlLegend.prefab) ────────
    private const float LEGEND_PAD_X    = 30f;  // padding kiri/kanan kartu
    private const float LEGEND_GAP      = 34f;  // jarak antar entry (separator di tengahnya)
    private const float LEGEND_SEP_X    = 17f;  // offset separator dari ujung entry sebelumnya
    private const float KEY_MIN_W       = 38f;  // lebar minimum keycap
    private const float KEY_TEXT_PAD    = 22f;  // padding teks di dalam keycap
    private const float KEY_GAP         = 6f;   // jarak antar keycap dalam satu entry
    private const float KEY_LABEL_GAP   = 13f;  // jarak keycap terakhir ke label
    private const float LABEL_PAD       = 4f;   // padding lebar label

    // ── Layout vertikal (kartu 2 kolom di pojok kiri atas) ───────────────────
    private const float V_PAD_X   = 24f;  // padding kiri/kanan kartu
    private const float V_PAD_Y   = 14f;  // padding atas/bawah kartu
    private const float V_ROW_H   = 38f;  // tinggi satu baris
    private const float V_ROW_GAP = 10f;  // jarak antar baris
    private const float V_COL_GAP = 28f;  // jarak antar kolom (divider di tengahnya)
    private const float V_MARGIN  = 26f;  // jarak kartu dari pojok layar

    // ── Tombol HINT + gaya MPUIKit ───────────────────────────────────────────
    private const float HINT_SIZE      = 72f;  // sisi tombol HINT
    private const float HINT_CAPTION_H = 22f;  // tinggi tulisan "HINT"
    private const float HINT_CAPTION_W = 140f; // lebar caption, lebih lebar dari tombol biar gak kepotong
    private const float HINT_TO_CARD   = 12f;  // jarak tombol ke kartu
    private const float R_CARD         = 14f;  // radius sudut kartu
    private const float R_KEY          = 7f;   // radius sudut keycap
    private const float R_HINT         = 10f;  // radius sudut tombol HINT
    private const float OUTLINE_W      = 3f;   // tebal garis tepi

    /// <summary>Satu baris legend: daftar keycap + labelnya.</summary>
    private class LegendEntry
    {
        public string[] Keys;
        public string Label;
        public LegendEntry(string label, params string[] keys) { Label = label; Keys = keys; }
    }

    /// <summary>
    /// Scene side-scroll: player nempel di zona bawah layar terus, jadi kartu
    /// horizontal di bawah-tengah bakal senggolan sama karakter. Dipindah ke
    /// pojok kiri atas, disusun 2 kolom.
    /// </summary>
    private static readonly HashSet<string> VerticalLegendScenes =
        new HashSet<string> { "Privasi_Keamanan", "Office_Environment", "Office_Level1_Prototype2" };

    /// <summary>Kontrol side-scroll (Topic 1 &amp; 2): cuma kiri/kanan + lompat.</summary>
    private static LegendEntry[] SideScrollSpec()
    {
        return new[]
        {
            new LegendEntry("MOVE", "A", "D"),
            new LegendEntry("JUMP", "SPACE"),
            new LegendEntry("RUN", "SHIFT"),
            new LegendEntry("OBJECTIVE", "Q"),
            new LegendEntry("INTERACT", "E"),
            new LegendEntry("SKIP DIALOGUE", "CTRL"),
            new LegendEntry("PAUSE", "ESC"),
        };
    }

    /// <summary>
    /// Isi legend khusus per scene. Kalau scene gak ada di sini, isi legend
    /// diturunin otomatis dari prefab (entry yang tombolnya gak ada di scene dibuang).
    /// </summary>
    private static readonly Dictionary<string, LegendEntry[]> LegendSpecByScene =
        new Dictionary<string, LegendEntry[]>
        {
            // Topic 1 & 2 (scene yang kepake di Build Settings). Dua-duanya side-scroll
            // pakai _PlayerMovement + _PlayerJump, dan punya VN + ObjectiveRedesignUI.
            { "Privasi_Keamanan",   SideScrollSpec() },
            { "Office_Environment", SideScrollSpec() },

            // Scene prototype lama, gak masuk Build Settings. Gak ada VN & objective.
            { "Office_Level1_Prototype2", new[]
                {
                    new LegendEntry("MOVE", "A", "D"),
                    new LegendEntry("JUMP", "SPACE"),
                    new LegendEntry("RUN", "SHIFT"),
                    new LegendEntry("INTERACT", "E"),
                    new LegendEntry("PAUSE", "ESC"),
                }
            },
        };

    /// <summary>
    /// Blocker legend per scene. Cuma objek yang beneran di-SetActive(false)
    /// saat panelnya ketutup — kalau objeknya permanen aktif, legend nyangkut ilang.
    /// Nama dicocokin ke SEMUA GameObject dengan nama itu (Topic 4 punya 3 laptop).
    /// </summary>
    private static readonly Dictionary<string, string[]> BlockersByScene =
        new Dictionary<string, string[]>
        {
            { "Office_Environment", new[] { "CanvasInstructor", "CanvasSummaryPanel", "DesktopCanvas", "WebsiteCanvas", "EvaluationCanvas" } },
            { "Map_Topic4",         new[] { "EmbeddedComputerInteraction" } },
            { "Map_Topic_5",        new[] { "EmbeddedComputerInteraction" } },
            { "Map_Topic6",         new[] { "DesktopCanvas", "CanvasSummaryPanel", "EvaluationCanvas" } },
            { "Privasi_Keamanan", new[] { "CanvasSummaryPanel", "CanvasInstructor", "CrashOverlayCanvas", "CrashOverlayCanvas_Installer", "DesktopCanvas-Installdatasyn", "DesktopCanvas-chatdesktop", "EvaluationCanvas" } },
            { "Office_Level1_Prototype2", new[] { "CanvasSummaryPanel", "DesktopCanvas-chatdesktop", "DesktopCanvas-Installdatasyn" } },
        };

    private static readonly string[] BatchScenes =
    {
        "Assets/!Scenes/Game/Game Topic 2/Office_Environment.unity",
        "Assets/!Scenes/Game/Game Topic 4/Map_Topic4.unity",
        "Assets/!Scenes/Game/Game Topic 5/Map_Topic_5.unity",
        "Assets/!Scenes/Game/Game Topic 6/Map_Topic6.unity",
    };

    // ── Menu ─────────────────────────────────────────────────────────────────

    [MenuItem("Tools/SecMind/Port Settings + Legend (Scene Aktif)")]
    public static void RunActiveScene()
    {
        var log = new System.Text.StringBuilder();
        bool ok = Port(SceneManager.GetActiveScene(), log);
        Debug.Log(log.ToString());
        if (ok) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    [MenuItem("Tools/SecMind/Port Settings + Legend (Topic 2,4,5,6 + Save)")]
    public static void RunBatch()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var log = new System.Text.StringBuilder();
        foreach (var path in BatchScenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            log.AppendLine("═══ " + scene.name + " ═══");
            if (Port(scene, log))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.AppendLine("  → scene disimpan.");
            }
            else
            {
                log.AppendLine("  → DILEWATI, scene gak disimpan.");
            }
            log.AppendLine();
        }
        Debug.Log(log.ToString());
    }

    // ── Inti ─────────────────────────────────────────────────────────────────

    private static bool Port(Scene scene, System.Text.StringBuilder log)
    {
        var pausePanels = AllInScene(scene)
            .SelectMany(go => go.GetComponents<PausePanel>())
            .Where(p => p != null)
            .ToList();

        if (pausePanels.Count == 0)
        {
            log.AppendLine("  ERROR: gak ada PausePanel di scene ini. Skip.");
            return false;
        }

        // Kalau ada lebih dari satu, yang dipakai cuma yang pausedPanel-nya keisi.
        var pause = pausePanels.FirstOrDefault(p => GetObj(p, "pausedPanel") != null);
        if (pause == null)
        {
            log.AppendLine("  ERROR: PausePanel ada tapi pausedPanel-nya kosong semua. Skip.");
            return false;
        }

        foreach (var extra in pausePanels.Where(p => p != pause))
        {
            if (!extra.enabled) continue;
            extra.enabled = false;
            EditorUtility.SetDirty(extra);
            log.AppendLine("  ! PausePanel duplikat di '" + PathOf(extra.transform) + "' di-disable.");
        }

        var panel = (GameObject)GetObj(pause, "pausedPanel");
        var panelRT = panel.transform as RectTransform;
        var parent = panel.transform.parent;

        // ── Warna & font di-sample dari panel asal ───────────────────────────
        var panelBg = panel.GetComponent<Graphic>();
        var titleTMP = panel.transform.Find("Title")?.GetComponent<TMP_Text>();
        var resumeBtn = (Button)GetObj(pause, "btnResume");
        var retryBtn = (Button)GetObj(pause, "btnRetry");
        var homeBtn = (Button)GetObj(pause, "btnMainMenu");

        if (resumeBtn == null || retryBtn == null || homeBtn == null || titleTMP == null)
        {
            log.AppendLine("  ERROR: struktur panel gak sesuai cetakan (butuh Title + 3 tombol). Skip.");
            return false;
        }

        Color bgCol = panelBg != null ? panelBg.color : new Color(0.198f, 0.062f, 0f, 1f);
        Color accent = resumeBtn.GetComponent<Graphic>().color;
        Color textCol = titleTMP.color;
        Color trackCol = Color.Lerp(bgCol, Color.black, 0.4f);
        trackCol.a = 1f;
        var font = titleTMP.font;

        log.AppendLine("  panel=" + PathOf(panel.transform) + "  bg=" + bgCol + " accent=" + accent);

        // ── 1. Rapiin menu pause + sisipin Btn_Settings ──────────────────────
        var oldSettingsBtn = panel.transform.Find("Btn_Settings");
        if (oldSettingsBtn != null) Object.DestroyImmediate(oldSettingsBtn.gameObject);

        var btnSettings = DuplicateUnder(resumeBtn.gameObject, panel.transform, "Btn_Settings");
        SetLabel(btnSettings, "Settings");
        ClearPersistentOnClick(btnSettings, log);
        SetAnchoredY(btnSettings.transform as RectTransform, BTN_SETTINGS_Y);
        SetAnchoredY(resumeBtn.transform as RectTransform, BTN_RESUME_Y);
        SetAnchoredY(retryBtn.transform as RectTransform, BTN_RETRY_Y);
        SetAnchoredY(homeBtn.transform as RectTransform, BTN_HOME_Y);
        btnSettings.transform.SetSiblingIndex(retryBtn.transform.GetSiblingIndex());

        // ── 2. SettingsPanel = duplikat Panel ────────────────────────────────
        var old = parent.Find("SettingsPanel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var settingsGO = DuplicateUnder(panel, parent, "SettingsPanel");
        var settingsRT = settingsGO.transform as RectTransform;
        CopyRect(panelRT, settingsRT);
        settingsGO.SetActive(false);

        // Buang isi yang gak dipakai, sisain satu tombol jadi Btn_Back.
        foreach (var n in new[] { "IconBox", "Btn_Resume", "Btn_Retry", "Btn_Settings" })
        {
            var t = settingsGO.transform.Find(n);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        var back = settingsGO.transform.Find("Btn_Home");
        if (back == null)
        {
            log.AppendLine("  ERROR: Btn_Home gak ketemu di duplikat. Skip.");
            Object.DestroyImmediate(settingsGO);
            return false;
        }
        back.name = "Btn_Back";
        SetLabel(back.gameObject, "Back");
        SetAnchoredY(back as RectTransform, -545f);
        ClearPersistentOnClick(back.gameObject, log);

        var sTitle = settingsGO.transform.Find("Title").GetComponent<TMP_Text>();
        sTitle.text = "Settings";
        sTitle.fontSize = 46f;
        SetAnchoredY(sTitle.transform as RectTransform, -70f);

        // ── 3. Isi panel Settings ────────────────────────────────────────────
        MakeLabel(settingsGO.transform, "Hdr_Movement", "MOVEMENT", 22f, accent,
                  TextAlignmentOptions.Left, font, -140f, 30f);
        MakeLabel(settingsGO.transform, "Lbl_Speed", "Player Speed", 24f, textCol,
                  TextAlignmentOptions.Left, font, -185f, 30f);
        var speedValue = MakeLabel(settingsGO.transform, "Txt_SpeedValue", "50%", 24f, accent,
                                   TextAlignmentOptions.Right, font, -185f, 30f);

        var slider = MakeSlider(settingsGO.transform, "Slider_Speed", -232f, accent, trackCol, textCol);

        var hintFaded = new Color(textCol.r, textCol.g, textCol.b, 0.55f);
        MakeLabel(settingsGO.transform, "Hint_Speed", "0% = pelan banget    -    100% = paling cepat",
                  18f, hintFaded, TextAlignmentOptions.Center, font, -266f, 26f);
        MakeLabel(settingsGO.transform, "Hint_Run", "Tahan <b>Shift</b> buat lari (+75% kecepatan)",
                  18f, hintFaded, TextAlignmentOptions.Center, font, -290f, 26f);

        var divider = NewUI("Divider", settingsGO.transform, -318f, PANEL_W, 2f);
        var divImg = divider.gameObject.AddComponent<Image>();
        divImg.sprite = Builtin("UI/Skin/UISprite.psd");
        divImg.type = Image.Type.Sliced;
        divImg.color = new Color(accent.r, accent.g, accent.b, 0.35f);

        MakeLabel(settingsGO.transform, "Hdr_Sound", "SOUND", 22f, accent,
                  TextAlignmentOptions.Left, font, -352f, 30f);

        var tgMusic = MakeToggle(settingsGO.transform, "Toggle_Music", "Background Music",
                                 -400f, accent, trackCol, textCol, font);
        var tgDialogue = MakeToggle(settingsGO.transform, "Toggle_Dialogue", "Dialogue",
                                    -455f, accent, trackCol, textCol, font);

        // ── 4. Wiring ────────────────────────────────────────────────────────
        var ui = settingsGO.GetComponent<SettingsPanelUI>() ?? settingsGO.AddComponent<SettingsPanelUI>();
        var uiSO = new SerializedObject(ui);
        uiSO.FindProperty("moveSpeedSlider").objectReferenceValue = slider;
        uiSO.FindProperty("moveSpeedValueText").objectReferenceValue = speedValue;
        uiSO.FindProperty("musicToggle").objectReferenceValue = tgMusic;
        uiSO.FindProperty("dialogueToggle").objectReferenceValue = tgDialogue;
        uiSO.ApplyModifiedPropertiesWithoutUndo();

        var pauseSO = new SerializedObject(pause);
        pauseSO.FindProperty("settingsPanel").objectReferenceValue = settingsGO;
        pauseSO.FindProperty("btnSettings").objectReferenceValue = btnSettings.GetComponent<Button>();
        pauseSO.FindProperty("btnSettingsBack").objectReferenceValue = back.GetComponent<Button>();
        pauseSO.ApplyModifiedPropertiesWithoutUndo();

        log.AppendLine("  SettingsPanel dibuat + PausePanel di-wire.");

        // ── 5. Control Legend ────────────────────────────────────────────────
        SetupLegend(scene, log);

        return true;
    }

    // ── Control Legend ───────────────────────────────────────────────────────

    private static void SetupLegend(Scene scene, System.Text.StringBuilder log)
    {
        var existing = AllInScene(scene).SelectMany(g => g.GetComponents<ControlLegendUI>()).FirstOrDefault();
        GameObject legendGO;

        if (existing != null)
        {
            legendGO = existing.gameObject;
            log.AppendLine("  legend udah ada di '" + legendGO.name + "', dipakai ulang.");
        }
        else
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LEGEND_PREFAB_PATH);
            if (prefab == null) { log.AppendLine("  ERROR: prefab legend gak ketemu di " + LEGEND_PREFAB_PATH); return; }

            legendGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            legendGO.name = "ControlLegendCanvas";
            log.AppendLine("  legend di-instantiate.");
        }

        var legend = legendGO.GetComponent<ControlLegendUI>();

        // Cari lewat nama, bukan path — hierarki-nya berubah kalau tombol HINT dipasang.
        var card = FindDeep(legendGO.transform, "Card");
        var shadow = FindDeep(legendGO.transform, "Shadow");

        var current = ReadSpec(card);
        var wanted = ResolveSpec(scene, current);

        bool wantVertical = VerticalLegendScenes.Contains(scene.name);
        bool isVertical = card.pivot.y > 0.5f;   // layout vertikal pivot-nya di atas

        if (!SameSpec(current, wanted) || wantVertical != isVertical)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(legendGO))
            {
                PrefabUtility.UnpackPrefabInstance(legendGO, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                card = FindDeep(legendGO.transform, "Card");
                shadow = FindDeep(legendGO.transform, "Shadow");
                log.AppendLine("  legend di-unpack (isi kartu beda dari prefab).");
            }
            RebuildCard(card, shadow, wanted, wantVertical);
            log.AppendLine("  kartu legend disusun ulang (" + (wantVertical ? "vertikal 2 kolom, kiri atas" : "horizontal, bawah tengah") + "): " +
                string.Join(" | ", wanted.Select(e => string.Join("+", e.Keys) + " " + e.Label).ToArray()));
        }
        else
        {
            log.AppendLine("  isi kartu legend sama kayak prefab, gak diubah.");
        }

        // Restyle MPUIKit + tombol HINT dipasang di semua topic. Keduanya ngubah
        // struktur (buang child "Inner", sisipin wrapper "Visible"), dan itu gak
        // boleh dilakuin ke prefab instance — jadi unpack dulu kalau masih nyambung.
        if (PrefabUtility.IsPartOfPrefabInstance(legendGO))
        {
            PrefabUtility.UnpackPrefabInstance(legendGO, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            card = FindDeep(legendGO.transform, "Card");
            shadow = FindDeep(legendGO.transform, "Shadow");
            log.AppendLine("  legend di-unpack (perlu restyle MPUIKit + tombol HINT).");
        }

        StyleCardMPUI(card, shadow, log);
        SetupHintButton(legendGO, legend, card, shadow, wantVertical, log);

        // Blocker
        string[] names;
        if (!BlockersByScene.TryGetValue(scene.name, out names))
        {
            log.AppendLine("  ! scene '" + scene.name + "' gak ada di tabel blocker — hideWhenAnyActive dibiarin kosong.");
            names = new string[0];
        }

        var blockers = new List<Object>();
        foreach (var n in names)
        {
            var found = AllInScene(scene).Where(g => g.name == n).ToList();
            if (found.Count == 0) { log.AppendLine("  ! blocker '" + n + "' gak ketemu."); continue; }
            foreach (var g in found)
            {
                // Jaring pengaman: objek yang gak pernah mati bikin legend ilang selamanya.
                blockers.Add(g);
                log.AppendLine("  blocker: " + PathOf(g.transform) + (g.activeInHierarchy ? "  (aktif di editor — dimatiin runtime)" : ""));
            }
        }

        var lSO = new SerializedObject(legend);
        var arr = lSO.FindProperty("hideWhenAnyActive");
        arr.arraySize = blockers.Count;
        for (int i = 0; i < blockers.Count; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = blockers[i];
        lSO.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Baca isi kartu legend yang sekarang jadi bentuk spec.</summary>
    private static LegendEntry[] ReadSpec(Transform card)
    {
        var list = new List<LegendEntry>();
        foreach (Transform entry in card)
        {
            if (!entry.name.StartsWith("Entry_")) continue;

            var keys = new List<string>();
            string label = "";
            foreach (Transform ch in entry)
            {
                if (ch.name.StartsWith("Key_"))
                {
                    var txt = ch.Find("Txt")?.GetComponent<TMP_Text>();
                    if (txt != null) keys.Add(txt.text);
                }
                else if (ch.name == "Label")
                {
                    var txt = ch.GetComponent<TMP_Text>();
                    if (txt != null) label = txt.text;
                }
            }
            list.Add(new LegendEntry(label, keys.ToArray()));
        }
        return list.ToArray();
    }

    /// <summary>
    /// Tentuin isi legend yang bener buat scene ini. Kalau ada spec manual pakai itu,
    /// kalau enggak buang entry yang tombolnya gak ngapa-ngapain di scene tersebut.
    /// </summary>
    private static LegendEntry[] ResolveSpec(Scene scene, LegendEntry[] current)
    {
        LegendEntry[] manual;
        if (LegendSpecByScene.TryGetValue(scene.name, out manual)) return manual;

        bool hasObjective = AllInScene(scene).Any(g => g.GetComponent<ObjectiveRedesignUI>() != null);
        bool hasVN = AllInScene(scene).Any(g => g.GetComponents<MonoBehaviour>()
                          .Any(m => m != null && m.GetType().Name == "VNDialogueManager"));

        return current.Where(e =>
        {
            if (!hasObjective && e.Label == "OBJECTIVE") return false;
            if (!hasVN && e.Label == "SKIP DIALOGUE") return false;
            return true;
        }).ToArray();
    }

    private static bool SameSpec(LegendEntry[] a, LegendEntry[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i].Label != b[i].Label) return false;
            if (!a[i].Keys.SequenceEqual(b[i].Keys)) return false;
        }
        return true;
    }

    /// <summary>
    /// Bangun ulang isi kartu legend dari spec. Keycap, label, dan separator
    /// di-clone dari yang udah ada supaya style-nya persis sama.
    /// Lebar keycap & label dihitung dari lebar teks TMP, jadi gak ada angka hardcode.
    /// </summary>
    private static void RebuildCard(RectTransform card, RectTransform shadow, LegendEntry[] spec, bool vertical)
    {
        // Ambil contoh keycap/label/separator dari kartu yang ada — jangan hardcode
        // nama "Key_W" / "Sep_0", isinya bisa udah beda kalau tool ini dijalanin ulang.
        var all = card.GetComponentsInChildren<Transform>(true);
        var keySrc = all.FirstOrDefault(t => t.name.StartsWith("Key_"));
        var labelSrc = all.FirstOrDefault(t => t.name == "Label" && t.parent.name.StartsWith("Entry_"));
        var sepSrc = all.FirstOrDefault(t => t.name.StartsWith("Sep_"));
        if (keySrc == null || labelSrc == null || sepSrc == null)
        {
            Debug.LogError("[PortSettings] Kartu legend gak punya contoh keycap/label/separator. Batal.");
            return;
        }

        var keyProto = Object.Instantiate(keySrc.gameObject);
        var labelProto = Object.Instantiate(labelSrc.gameObject);
        var sepProto = Object.Instantiate(sepSrc.gameObject);

        foreach (var old in card.Cast<Transform>().Select(t => t.gameObject).ToList())
            Object.DestroyImmediate(old);

        var entries = spec.Select(e => BuildEntry(card, e, keyProto, labelProto)).ToList();

        if (vertical) LayoutVertical(card, shadow, entries, sepProto);
        else          LayoutHorizontal(card, shadow, entries, sepProto);

        Object.DestroyImmediate(keyProto);
        Object.DestroyImmediate(labelProto);
        Object.DestroyImmediate(sepProto);
    }

    /// <summary>
    /// Bikin satu baris legend (keycap-keycap + label). Lebarnya dihitung dari
    /// lebar teks TMP, bukan angka hardcode. Anchor/posisi diatur si layout.
    /// </summary>
    private static RectTransform BuildEntry(RectTransform card, LegendEntry e,
                                            GameObject keyProto, GameObject labelProto)
    {
        var entryRT = (RectTransform)new GameObject("Entry_" + e.Label.Replace(' ', '_'), typeof(RectTransform)).transform;
        entryRT.SetParent(card, false);

        float x = 0f;
        foreach (var key in e.Keys)
        {
            var kGO = Object.Instantiate(keyProto);
            kGO.name = "Key_" + key;
            var kRT = (RectTransform)kGO.transform;
            kRT.SetParent(entryRT, false);
            kRT.anchorMin = kRT.anchorMax = new Vector2(0f, 0.5f);
            kRT.pivot = new Vector2(0f, 0.5f);

            var kTxt = kGO.transform.Find("Txt").GetComponent<TMP_Text>();
            kTxt.text = key;
            float w = Mathf.Max(KEY_MIN_W, kTxt.GetPreferredValues(key).x + KEY_TEXT_PAD);

            kRT.anchoredPosition = new Vector2(x, 0f);
            kRT.sizeDelta = new Vector2(w, V_ROW_H);
            x += w + KEY_GAP;
        }
        x = x - KEY_GAP + KEY_LABEL_GAP;

        var lGO = Object.Instantiate(labelProto);
        lGO.name = "Label";
        var lRT = (RectTransform)lGO.transform;
        lRT.SetParent(entryRT, false);
        lRT.anchorMin = lRT.anchorMax = new Vector2(0f, 0.5f);
        lRT.pivot = new Vector2(0f, 0.5f);
        var lTxt = lGO.GetComponent<TMP_Text>();
        lTxt.text = e.Label;
        float lw = lTxt.GetPreferredValues(e.Label).x + LABEL_PAD;
        lRT.anchoredPosition = new Vector2(x, 0f);
        lRT.sizeDelta = new Vector2(lw, V_ROW_H);

        entryRT.sizeDelta = new Vector2(x + lw, V_ROW_H);
        return entryRT;
    }

    /// <summary>Strip memanjang di bawah-tengah layar. Dipakai scene top-down.</summary>
    private static void LayoutHorizontal(RectTransform card, RectTransform shadow,
                                         List<RectTransform> entries, GameObject sepProto)
    {
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0f);
        card.pivot = new Vector2(0.5f, 0f);

        float total = LEGEND_PAD_X * 2f
                    + entries.Sum(e => e.sizeDelta.x)
                    + LEGEND_GAP * (entries.Count - 1);

        var seps = new List<RectTransform>();
        for (int i = 0; i < entries.Count - 1; i++)
            seps.Add(NewSep(card, sepProto, "Sep_" + i, new Vector2(0.5f, 0.5f), new Vector2(2f, 24f)));

        float cursor = -total / 2f + LEGEND_PAD_X;
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].anchorMin = entries[i].anchorMax = new Vector2(0.5f, 0.5f);
            entries[i].pivot = new Vector2(0f, 0.5f);
            entries[i].anchoredPosition = new Vector2(cursor, 0f);
            cursor += entries[i].sizeDelta.x;
            if (i < seps.Count)
            {
                seps[i].anchoredPosition = new Vector2(cursor + LEGEND_SEP_X, 0f);
                cursor += LEGEND_GAP;
            }
        }

        Interleave(entries, seps);

        card.sizeDelta = new Vector2(total, V_ROW_H + 30f);
        card.anchoredPosition = new Vector2(0f, V_MARGIN);
        if (shadow != null)
        {
            shadow.anchorMin = shadow.anchorMax = card.anchorMin;
            shadow.pivot = card.pivot;
            shadow.sizeDelta = new Vector2(total + 10f, card.sizeDelta.y);
            shadow.anchoredPosition = card.anchoredPosition + new Vector2(0f, -5f);
        }
    }

    /// <summary>
    /// Kotak 2 kolom di pojok kiri atas. Dipakai scene side-scroll, karena di sana
    /// player nempel di zona bawah layar terus.
    /// </summary>
    private static void LayoutVertical(RectTransform card, RectTransform shadow,
                                       List<RectTransform> entries, GameObject sepProto)
    {
        card.anchorMin = card.anchorMax = new Vector2(0f, 1f);
        card.pivot = new Vector2(0f, 1f);

        // Kolom kiri diisi duluan sampai penuh, sisanya ke kolom kanan.
        int rows = Mathf.CeilToInt(entries.Count / 2f);
        float col0W = entries.Take(rows).Max(e => e.sizeDelta.x);
        float col1W = entries.Count > rows ? entries.Skip(rows).Max(e => e.sizeDelta.x) : 0f;

        float contentH = rows * V_ROW_H + (rows - 1) * V_ROW_GAP;
        float cardW = V_PAD_X * 2f + col0W + (col1W > 0f ? V_COL_GAP + col1W : 0f);
        float cardH = V_PAD_Y * 2f + contentH;

        for (int i = 0; i < entries.Count; i++)
        {
            int col = i < rows ? 0 : 1;
            int row = i < rows ? i : i - rows;

            var rt = entries[i];
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(
                V_PAD_X + (col == 0 ? 0f : col0W + V_COL_GAP),
                -(V_PAD_Y + row * (V_ROW_H + V_ROW_GAP) + V_ROW_H / 2f));
        }

        // Satu garis vertikal di antara dua kolom, bukan separator per entry.
        var seps = new List<RectTransform>();
        if (col1W > 0f)
        {
            var sep = NewSep(card, sepProto, "Sep_Column", new Vector2(0f, 1f), new Vector2(2f, contentH));
            sep.anchoredPosition = new Vector2(V_PAD_X + col0W + V_COL_GAP / 2f, -(V_PAD_Y + contentH / 2f));
            seps.Add(sep);
        }

        Interleave(entries, seps);

        card.sizeDelta = new Vector2(cardW, cardH);
        card.anchoredPosition = new Vector2(V_MARGIN, -V_MARGIN);
        if (shadow != null)
        {
            shadow.anchorMin = shadow.anchorMax = card.anchorMin;
            shadow.pivot = card.pivot;
            shadow.sizeDelta = new Vector2(cardW + 10f, cardH);
            shadow.anchoredPosition = card.anchoredPosition + new Vector2(-5f, -5f);
        }
    }

    private static RectTransform NewSep(RectTransform card, GameObject proto, string name,
                                        Vector2 anchor, Vector2 size)
    {
        var go = Object.Instantiate(proto);
        go.name = name;
        var rt = (RectTransform)go.transform;
        rt.SetParent(card, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        return rt;
    }

    /// <summary>Rapiin urutan hierarchy: entry & separator selang-seling.</summary>
    private static void Interleave(List<RectTransform> entries, List<RectTransform> seps)
    {
        int idx = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].SetSiblingIndex(idx++);
            if (i < seps.Count) seps[i].SetSiblingIndex(idx++);
        }
    }

    // ── Gaya MPUIKit ─────────────────────────────────────────────────────────

    /// <summary>
    /// Tuker semua Image di kartu jadi MPImage: sudut membulat + garis tepi.
    /// Keycap yang tadinya 2 objek bertumpuk (kotak hitam + isi krem inset 3px)
    /// jadi satu MPImage dengan outline — child "Inner"-nya dibuang.
    /// </summary>
    private static void StyleCardMPUI(RectTransform card, RectTransform shadow, System.Text.StringBuilder log)
    {
        // Warna tinta diambil dari teks label, bukan hardcode.
        var anyLabel = card.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "Label");
        Color ink = anyLabel != null ? anyLabel.color : new Color(0.067f, 0.067f, 0.067f, 1f);

        var cardImg = card.GetComponent<Image>();
        Color cream = cardImg != null ? cardImg.color : new Color(0.969f, 0.953f, 0.906f, 0.97f);

        int keys = 0;
        foreach (var keyT in card.GetComponentsInChildren<Transform>(true)
                                 .Where(t => t.name.StartsWith("Key_")).ToList())
        {
            var outer = keyT.GetComponent<Image>();
            var innerT = keyT.Find("Inner");
            Color capOutline = outer != null ? outer.color : ink;
            Color capFill = cream;
            if (innerT != null)
            {
                var innerImg = innerT.GetComponent<Image>();
                if (innerImg != null) capFill = innerImg.color;
                Object.DestroyImmediate(innerT.gameObject);
            }
            AsRoundedRect(keyT.gameObject, capFill, R_KEY, capOutline, OUTLINE_W, false);
            keys++;
        }

        foreach (var sepT in card.Cast<Transform>().Where(t => t.name.StartsWith("Sep_")).ToList())
        {
            var img = sepT.GetComponent<Image>();
            Color c = img != null ? img.color : new Color(ink.r, ink.g, ink.b, 0.22f);
            AsRoundedRect(sepT.gameObject, c, 1f, Color.clear, 0f, false);
        }

        AsRoundedRect(card.gameObject, cream, R_CARD, ink, OUTLINE_W, false);

        if (shadow != null)
        {
            var simg = shadow.GetComponent<Image>();
            Color sc = simg != null ? simg.color : new Color(0f, 0f, 0f, 0.2f);
            AsRoundedRect(shadow.gameObject, sc, R_CARD, Color.clear, 0f, false);
        }

        log.AppendLine("  kartu legend di-restyle MPUIKit (" + keys + " keycap, child 'Inner' dibuang).");
    }

    /// <summary>Ganti Image biasa jadi MPImage kotak-membulat. Idempoten.</summary>
    private static MPImage AsRoundedRect(GameObject go, Color fill, float radius,
                                         Color outline, float outlineWidth, bool raycast)
    {
        var mp = EnsureMPImage(go);
        mp.raycastTarget = raycast;
        mp.type = Image.Type.Simple;
        mp.sprite = null;
        mp.DrawShape = DrawShape.Rectangle;
        mp.color = fill;
        mp.Init();
        mp.OutlineColor = outline;
        mp.OutlineWidth = outlineWidth;

        var r = mp.Rectangle;
        r.CornerRadius = new Vector4(radius, radius, radius, radius);
        mp.Rectangle = r;
        return mp;
    }

    private static MPImage AsCircle(GameObject go, Color fill, Color outline, float outlineWidth)
    {
        var mp = EnsureMPImage(go);
        mp.raycastTarget = false;
        mp.type = Image.Type.Simple;
        mp.sprite = null;
        mp.DrawShape = DrawShape.Circle;
        mp.color = fill;
        mp.Init();
        mp.OutlineColor = outline;
        mp.OutlineWidth = outlineWidth;

        var c = mp.Circle;
        c.FitToRect = true;
        mp.Circle = c;
        return mp;
    }

    /// <summary>
    /// MPImage turunan Image, jadi GetComponent&lt;Image&gt;() bisa balikin MPImage juga.
    /// Yang dibuang cuma Image polos — kalau udah MPImage, dipakai ulang.
    /// </summary>
    private static MPImage EnsureMPImage(GameObject go)
    {
        var existing = go.GetComponent<Image>();
        var asMP = existing as MPImage;
        if (asMP != null) return asMP;

        if (existing != null) Object.DestroyImmediate(existing);
        return go.AddComponent<MPImage>();
    }

    // ── Tombol HINT ──────────────────────────────────────────────────────────

    /// <summary>
    /// Bungkus legend dalam wrapper "Visible", terus taruh tombol HINT di sebelahnya.
    ///
    ///   ControlLegendCanvas  [ControlLegendUI + ControlLegendHintToggle]
    ///   └── Visible            ← legendRoot-nya ControlLegendUI (pause / blocker)
    ///       ├── HintButton     ← tetap keliatan pas kartu ditutup
    ///       └── Legend         ← di-toggle tombol HINT, auto-hide 4 detik
    ///
    /// Posisi tombol ngikut layout kartunya:
    ///   vertikal (side-scroll) → pojok kiri ATAS, kartu turun di bawahnya
    ///   horizontal (top-down)  → pojok kiri BAWAH, kartu tetap di bawah-tengah
    /// </summary>
    private static void SetupHintButton(GameObject legendGO, ControlLegendUI legend,
                                        RectTransform card, RectTransform shadow,
                                        bool vertical, System.Text.StringBuilder log)
    {
        var legendPanel = card.parent as RectTransform;   // objek "Legend"

        // 1. Wrapper "Visible"
        var visible = legendGO.transform.Find("Visible") as RectTransform;
        if (visible == null)
        {
            visible = (RectTransform)new GameObject("Visible", typeof(RectTransform)).transform;
            visible.SetParent(legendGO.transform, false);
            Stretch(visible);
            legendPanel.SetParent(visible, false);
            Stretch(legendPanel);
        }
        visible.SetSiblingIndex(0);

        // 2. Warna diambil dari kartu yang barusan di-style
        var cardImg = card.GetComponent<MPImage>();
        Color cream = cardImg != null ? cardImg.color : new Color(0.969f, 0.953f, 0.906f, 0.97f);
        Color ink = cardImg != null ? cardImg.OutlineColor : new Color(0.067f, 0.067f, 0.067f, 1f);
        var font = card.GetComponentsInChildren<TMP_Text>(true).Select(t => t.font).FirstOrDefault(f => f != null);

        // 3. Tombol
        var oldBtn = visible.Find("HintButton");
        if (oldBtn != null) Object.DestroyImmediate(oldBtn.gameObject);

        var btnRT = (RectTransform)new GameObject("HintButton", typeof(RectTransform)).transform;
        btnRT.SetParent(visible, false);
        btnRT.anchorMin = btnRT.anchorMax = new Vector2(0f, vertical ? 1f : 0f);
        btnRT.pivot = new Vector2(0f, vertical ? 1f : 0f);
        btnRT.anchoredPosition = new Vector2(V_MARGIN, vertical ? -V_MARGIN : V_MARGIN);
        btnRT.sizeDelta = new Vector2(HINT_SIZE, HINT_SIZE);

        var btnImg = AsRoundedRect(btnRT.gameObject, cream, R_HINT, ink, OUTLINE_W, true);
        var button = btnRT.gameObject.AddComponent<Button>();
        button.targetGraphic = btnImg;

        // Ikon bohlam: lingkaran garis + dudukan + ulir.
        var glass = NewChild(btnRT, "Bulb_Glass", new Vector2(0f, 9f), new Vector2(30f, 30f));
        AsCircle(glass.gameObject, new Color(cream.r, cream.g, cream.b, 0f), ink, 2.5f);

        var baseRT = NewChild(btnRT, "Bulb_Base", new Vector2(0f, -11f), new Vector2(16f, 9f));
        AsRoundedRect(baseRT.gameObject, ink, 2f, Color.clear, 0f, false);

        var thread = NewChild(btnRT, "Bulb_Thread", new Vector2(0f, -18f), new Vector2(11f, 3f));
        AsRoundedRect(thread.gameObject, ink, 1.5f, Color.clear, 0f, false);

        // 4. Tulisan "HINT" — isi kartunya emang daftar kontrol, bukan petunjuk soal.
        //    Kartu vertikal tombolnya di atas -> caption di bawah;
        //    kartu horizontal tombolnya nempel dasar layar -> caption di atas,
        //    kalau di bawah bakal kepotong tepi layar.
        var capRT = (RectTransform)new GameObject("HintCaption", typeof(RectTransform)).transform;
        capRT.SetParent(btnRT, false);
        capRT.anchorMin = capRT.anchorMax = new Vector2(0.5f, vertical ? 0f : 1f);
        capRT.pivot = new Vector2(0.5f, vertical ? 1f : 0f);
        capRT.anchoredPosition = new Vector2(0f, vertical ? -4f : 4f);
        capRT.sizeDelta = new Vector2(HINT_CAPTION_W, HINT_CAPTION_H);
        var cap = capRT.gameObject.AddComponent<TextMeshProUGUI>();
        cap.text = "HINT";
        cap.fontSize = 16f;
        cap.enableWordWrapping = false;
        cap.color = ink;
        cap.alignment = vertical ? TextAlignmentOptions.Top : TextAlignmentOptions.Bottom;
        cap.fontStyle = FontStyles.Bold;
        cap.raycastTarget = false;
        if (font != null) cap.font = font;

        // 5. Cuma layout vertikal yang perlu geser kartu — tombolnya nempatin
        //    pojok yang sama. Layout horizontal kartunya di tengah, gak nabrak.
        if (vertical)
        {
            float drop = HINT_SIZE + HINT_CAPTION_H + HINT_TO_CARD;
            card.anchoredPosition = new Vector2(V_MARGIN, -(V_MARGIN + drop));
            if (shadow != null) shadow.anchoredPosition = card.anchoredPosition + new Vector2(-5f, -5f);
        }

        // 6. legendRoot pindah ke wrapper, biar tombol ikut ilang pas ada panel nutupin
        var lSO = new SerializedObject(legend);
        lSO.FindProperty("legendRoot").objectReferenceValue = visible.gameObject;
        lSO.ApplyModifiedPropertiesWithoutUndo();

        // 7. Script toggle-nya
        var toggle = legendGO.GetComponent<ControlLegendHintToggle>()
                  ?? legendGO.AddComponent<ControlLegendHintToggle>();
        var tSO = new SerializedObject(toggle);
        tSO.FindProperty("legendPanel").objectReferenceValue = legendPanel.gameObject;
        tSO.FindProperty("hintButton").objectReferenceValue = button;
        tSO.FindProperty("autoHideSeconds").floatValue = 4f;
        tSO.FindProperty("visibleOnStart").boolValue = false;
        tSO.ApplyModifiedPropertiesWithoutUndo();

        legendPanel.gameObject.SetActive(false);

        log.AppendLine("  tombol HINT dipasang. legendRoot -> 'Visible', kartu auto-hide 4 detik.");
    }

    private static RectTransform NewChild(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private static RectTransform FindDeep(Transform root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true)
                   .FirstOrDefault(t => t.name == name) as RectTransform;
    }

    // ── Builder UI ───────────────────────────────────────────────────────────

    private static RectTransform NewUI(string name, Transform parent, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    private static TMP_Text MakeLabel(Transform parent, string name, string text, float size,
                                      Color col, TextAlignmentOptions align, TMP_FontAsset font,
                                      float y, float h)
    {
        var rt = NewUI(name, parent, y, PANEL_W, h);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = col;
        tmp.alignment = align;
        if (font != null) tmp.font = font;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Slider MakeSlider(Transform parent, string name, float y,
                                     Color accent, Color track, Color handleCol)
    {
        var rt = NewUI(name, parent, y, PANEL_W, 24f);
        var slider = rt.gameObject.AddComponent<Slider>();

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        var bgRT = (RectTransform)bg.transform;
        bgRT.SetParent(rt, false);
        bgRT.anchorMin = new Vector2(0f, 0.25f);
        bgRT.anchorMax = new Vector2(1f, 0.75f);
        bgRT.sizeDelta = Vector2.zero;
        bgRT.anchoredPosition = Vector2.zero;
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = Builtin("UI/Skin/Background.psd");
        bgImg.type = Image.Type.Sliced;
        bgImg.color = track;

        var fillArea = (RectTransform)new GameObject("Fill Area", typeof(RectTransform)).transform;
        fillArea.SetParent(rt, false);
        fillArea.anchorMin = new Vector2(0f, 0.25f);
        fillArea.anchorMax = new Vector2(1f, 0.75f);
        fillArea.anchoredPosition = Vector2.zero;
        fillArea.sizeDelta = new Vector2(-16f, 0f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRT = (RectTransform)fill.transform;
        fillRT.SetParent(fillArea, false);
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(0.5f, 1f);
        fillRT.anchoredPosition = Vector2.zero;
        fillRT.sizeDelta = Vector2.zero;
        var fillImg = fill.GetComponent<Image>();
        fillImg.sprite = Builtin("UI/Skin/UISprite.psd");
        fillImg.type = Image.Type.Sliced;
        fillImg.color = accent;

        var slideArea = (RectTransform)new GameObject("Handle Slide Area", typeof(RectTransform)).transform;
        slideArea.SetParent(rt, false);
        slideArea.anchorMin = Vector2.zero;
        slideArea.anchorMax = Vector2.one;
        slideArea.anchoredPosition = Vector2.zero;
        slideArea.sizeDelta = new Vector2(-22f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        var handleRT = (RectTransform)handle.transform;
        handleRT.SetParent(slideArea, false);
        handleRT.anchorMin = new Vector2(0.5f, 0f);
        handleRT.anchorMax = new Vector2(0.5f, 1f);
        handleRT.anchoredPosition = Vector2.zero;
        handleRT.sizeDelta = new Vector2(22f, 0f);
        var handleImg = handle.GetComponent<Image>();
        handleImg.sprite = Builtin("UI/Skin/Knob.psd");
        handleImg.type = Image.Type.Sliced;
        handleImg.color = handleCol;

        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = GameSettings.DEFAULT_MOVE_SPEED;
        return slider;
    }

    private static Toggle MakeToggle(Transform parent, string name, string label, float y,
                                     Color accent, Color box, Color textCol, TMP_FontAsset font)
    {
        var rt = NewUI(name, parent, y, PANEL_W, 44f);
        var toggle = rt.gameObject.AddComponent<Toggle>();

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        var bgRT = (RectTransform)bg.transform;
        bgRT.SetParent(rt, false);
        bgRT.anchorMin = bgRT.anchorMax = new Vector2(0f, 0.5f);
        bgRT.pivot = new Vector2(0f, 0.5f);
        bgRT.anchoredPosition = Vector2.zero;
        bgRT.sizeDelta = new Vector2(36f, 36f);
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = Builtin("UI/Skin/UISprite.psd");
        bgImg.type = Image.Type.Sliced;
        bgImg.color = box;

        var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        var checkRT = (RectTransform)check.transform;
        checkRT.SetParent(bgRT, false);
        checkRT.anchorMin = Vector2.zero;
        checkRT.anchorMax = Vector2.one;
        checkRT.anchoredPosition = Vector2.zero;
        checkRT.sizeDelta = new Vector2(-8f, -8f);
        var checkImg = check.GetComponent<Image>();
        checkImg.sprite = Builtin("UI/Skin/Checkmark.psd");
        checkImg.color = accent;

        var lbl = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var lblRT = (RectTransform)lbl.transform;
        lblRT.SetParent(rt, false);
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.anchoredPosition = new Vector2(26f, 0f);
        lblRT.sizeDelta = new Vector2(-52f, 0f);
        var lblTMP = lbl.GetComponent<TextMeshProUGUI>();
        lblTMP.text = label;
        lblTMP.fontSize = 24f;
        lblTMP.color = textCol;
        lblTMP.alignment = TextAlignmentOptions.Left;
        if (font != null) lblTMP.font = font;
        lblTMP.raycastTarget = false;

        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = true;
        return toggle;
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static GameObject DuplicateUnder(GameObject source, Transform parent, string newName)
    {
        var copy = Object.Instantiate(source);
        copy.name = newName;
        copy.transform.SetParent(parent, false);
        CopyRect(source.transform as RectTransform, copy.transform as RectTransform);
        copy.SetActive(true);
        return copy;
    }

    private static void CopyRect(RectTransform from, RectTransform to)
    {
        if (from == null || to == null) return;
        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.pivot = from.pivot;
        to.anchoredPosition3D = from.anchoredPosition3D;
        to.sizeDelta = from.sizeDelta;
        to.localScale = from.localScale;
        to.localRotation = from.localRotation;
    }

    private static void SetAnchoredY(RectTransform rt, float y)
    {
        if (rt == null) return;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
    }

    private static void SetLabel(GameObject buttonGO, string text)
    {
        var tmp = buttonGO.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) tmp.text = text;
    }

    /// <summary>
    /// Tombol hasil duplikat ikut bawa onClick yang di-set lewat Inspector.
    /// Kalau gak dibuang, Btn_Back bisa ikut pindah scene kayak Btn_Home aslinya.
    /// </summary>
    private static void ClearPersistentOnClick(GameObject buttonGO, System.Text.StringBuilder log)
    {
        var btn = buttonGO.GetComponent<Button>();
        if (btn == null) return;

        var so = new SerializedObject(btn);
        var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        if (calls == null || calls.arraySize == 0) return;

        log.AppendLine("  ! " + buttonGO.name + ": " + calls.arraySize + " persistent onClick dibuang.");
        calls.arraySize = 0;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite Builtin(string path)
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
    }

    private static Object GetObj(Object owner, string prop)
    {
        return new SerializedObject(owner).FindProperty(prop).objectReferenceValue;
    }

    private static IEnumerable<GameObject> AllInScene(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                yield return t.gameObject;
    }

    private static string PathOf(Transform t)
    {
        var s = t.name;
        var p = t.parent;
        while (p != null) { s = p.name + "/" + s; p = p.parent; }
        return s;
    }
}
