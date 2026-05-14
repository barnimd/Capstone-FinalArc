using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GameTopic6SceneSetup
{
    [MenuItem("Game/Setup Topic 6 - Computer Interaction Scene")]
    public static void SetupScene()
    {
        Debug.Log("=== Setup Topic 6 ===");
        FixCanvases();
        EnsureEventSystem();
        Cleanup();
        SetupFileSystem();
        SetupExplorerPanel();
        SetupTrashPanel();
        SetupCrashOverlay();
        SetupRansomwarePanel();
        SetupBackupPanels();
        SetupFeedbackPanel();   // FIX R5: BtnFeedbackOk dibuat dengan nama tepat
        SetupSimulationPanel(); // FIX R7-A: panel baru untuk Round 8 Simulation
        SetupGameManager();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Topic 6 Setup", "8-round simulation siap!", "OK");
    }

    // ===================== CANVAS & EVENT SYSTEM =====================

    static void FixCanvases()
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
            if (c.transform.localScale == Vector3.zero)
                c.transform.localScale = Vector3.one;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>(true) != null) return;
        new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));
        Debug.Log("[Setup] EventSystem created.");
    }

    // ===================== CLEANUP =====================

    static void Cleanup()
    {
        // FIX R7-A: tambahkan "SimulationPanel" ke daftar cleanup
        foreach (string n in new[] {
            "FileExplorerPanel","TrashPanel","CrashOverlay","RansomwarePanel",
            "BackupQuestionPanel","RecoveryPanel","BackupSetupPanel",
            "SimulationPanel",
            "FeedbackPanel","GameManager_Tp6","WarningPopup",
            "FileManager","BackupController","EvaluationManager_Tp6" })
        {
            var go = GameObject.Find(n);
            while (go) { Object.DestroyImmediate(go); go = GameObject.Find(n); }
        }
        foreach (var c in Object.FindObjectsOfType<ExplorerController>(true)) Object.DestroyImmediate(c.gameObject);
        foreach (var c in Object.FindObjectsOfType<TrashController>(true)) Object.DestroyImmediate(c.gameObject);
        foreach (var c in Object.FindObjectsOfType<CrashController>(true)) Object.DestroyImmediate(c.gameObject);
        foreach (var c in Object.FindObjectsOfType<RansomwareController>(true)) Object.DestroyImmediate(c.gameObject);
        foreach (var c in Object.FindObjectsOfType<BackupController>(true)) Object.DestroyImmediate(c.gameObject);
        foreach (var c in Object.FindObjectsOfType<GameManager_Tp6>(true)) Object.DestroyImmediate(c.gameObject);
        foreach (var c in Object.FindObjectsOfType<FileSystemManager>(true)) Object.DestroyImmediate(c.gameObject);
        foreach (var c in Object.FindObjectsOfType<EvaluationManager_Tp6>(true)) Object.DestroyImmediate(c.gameObject);
    }

    // ===================== FILE SYSTEM =====================

    static void SetupFileSystem()
    {
        if (!Object.FindObjectOfType<FileSystemManager>(true))
            new GameObject("FileManager").AddComponent<FileSystemManager>();
    }

    // ===================== EXPLORER (Round 1) =====================

    static void SetupExplorerPanel()
    {
        var dc = FindCanvas("desktop"); if (!dc) return;
        var panel = Modal(dc.transform, "FileExplorerPanel", 560, 440);

        Txt(panel.transform, "Title", "📁 File Explorer", 20, Color.white, 0, 195, 400, 32).alignment = TextAlignmentOptions.Center;

        var list = new GameObject("FileList", typeof(RectTransform), typeof(Image));
        list.transform.SetParent(panel.transform, false);
        S(list.GetComponent<RectTransform>(), -135, -20, 270, 320);
        list.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.9f);

        var zones = new GameObject("FolderZones", typeof(RectTransform));
        zones.transform.SetParent(panel.transform, false);
        S(zones.GetComponent<RectTransform>(), 135, -20, 250, 320);

        var exp = panel.AddComponent<ExplorerController>();
        exp.explorerPanel = panel;
        exp.fileListContainer = list.transform;
        exp.btnClose = Btn(panel.transform, "BtnClose", "Close", new Color(0.35f, 0.35f, 0.35f), 0, -195, 140, 32);

        float startY = 145f;
        float step = 43f;
        exp.poolItems = new FileItem[7];
        for (int i = 0; i < 7; i++)
        {
            var fi = CreateFileItem(list.transform, "FileItem_" + i, i, startY - (i * step));
            exp.poolItems[i] = fi;
        }

        exp.labelWork = MakeZone(zones.transform, "Work Files", FileLocation.WorkFiles, 0, 130);
        exp.labelPersonal = MakeZone(zones.transform, "Personal", FileLocation.Personal, 0, 60);
        exp.labelDownloads = MakeZone(zones.transform, "Downloads", FileLocation.Downloads, 0, -10);
        exp.labelImp = MakeZone(zones.transform, "Important_Project", FileLocation.ImportantProject, 0, -80);
        exp.labelTrash = MakeZone(zones.transform, "Trash", FileLocation.Trash, 0, -150);

        var warn = Modal(dc.transform, "WarningPopup", 420, 200);
        warn.SetActive(false);
        exp.warningPanel = warn;
        exp.txtWarning = Txt(warn.transform, "txtWarning", "Important file missing!", 16, Color.white, 0, 30, 380, 80);
        exp.txtWarning.alignment = TextAlignmentOptions.Center;
        exp.btnWarningOk = Btn(warn.transform, "BtnWarningOk", "OK — Lanjutkan", new Color(1f, 0.76f, 0.2f), 0, -40, 180, 36);

        panel.transform.SetAsLastSibling();
        warn.transform.SetAsLastSibling();
    }

    static FileItem CreateFileItem(Transform parent, string name, int index, float y)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(FileItem));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(250, 36);
        go.GetComponent<Image>().color = new Color(1, 1, 1, 0.12f);
        go.GetComponent<CanvasGroup>().blocksRaycasts = true;

        var nameTxt = Txt(go.transform, "txtName", "file_" + index, 12, Color.white, -80, 0, 160, 22);
        nameTxt.alignment = TextAlignmentOptions.Left;

        var inputGO = new GameObject("InputRename", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputGO.transform.SetParent(go.transform, false);
        S(inputGO.GetComponent<RectTransform>(), 30, 0, 110, 24);
        inputGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);
        var inp = inputGO.GetComponent<TMP_InputField>();
        inp.textComponent = ChildTxt(inputGO.transform, "Text", "", 11, Color.white);
        var ph = ChildTxt(inputGO.transform, "Placeholder", "rename...", 11, new Color(0.4f, 0.4f, 0.4f));
        ph.fontStyle = FontStyles.Italic; inp.placeholder = ph;

        var renameBtn = Btn(go.transform, "renameBtn", "↻", new Color(0.25f, 0.55f, 0.85f), 100, 0, 26, 24);
        var deleteBtn = Btn(go.transform, "deleteBtn", "🗑", new Color(0.85f, 0.2f, 0.2f), 125, 0, 26, 24);

        var lk = new GameObject("lockIcon", typeof(RectTransform), typeof(Image));
        lk.transform.SetParent(go.transform, false);
        S(lk.GetComponent<RectTransform>(), 108, 0, 14, 14);
        lk.GetComponent<Image>().color = Color.red;
        lk.SetActive(false);

        var fi = go.GetComponent<FileItem>();
        fi.txtName = nameTxt;
        fi.bgImage = go.GetComponent<Image>();
        fi.inputRename = inp;
        fi.renameBtn = renameBtn;
        fi.deleteBtn = deleteBtn;
        fi.lockIcon = lk.GetComponent<Image>();

        return fi;
    }

    static TMP_Text MakeZone(Transform p, string name, FileLocation loc, float x, float y)
    {
        var go = new GameObject("Zone_" + name, typeof(RectTransform), typeof(Image), typeof(DropTarget));
        go.transform.SetParent(p, false);
        S(go.GetComponent<RectTransform>(), x, y, 240, 55);
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.24f, 0.95f);
        var dt = go.GetComponent<DropTarget>(); dt.targetLocation = loc; dt.bg = go.GetComponent<Image>();
        var lbl = Txt(go.transform, "Label", "📁 " + name, 13, Color.white, -100, 0, 200, 24);
        lbl.alignment = TextAlignmentOptions.Left;
        return lbl;
    }

    // ===================== TRASH (Round 2) =====================

    static void SetupTrashPanel()
    {
        var dc = FindCanvas("desktop"); if (!dc) return;
        var panel = Modal(dc.transform, "TrashPanel", 460, 340);
        Txt(panel.transform, "Title", "🗑 Trash", 20, Color.white, 0, 145, 300, 32).alignment = TextAlignmentOptions.Center;

        var c = new GameObject("Content", typeof(RectTransform), typeof(Image));
        c.transform.SetParent(panel.transform, false);
        S(c.GetComponent<RectTransform>(), 0, -10, 400, 200);
        c.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.9f);

        var et = Txt(panel.transform, "txtTrashEmpty", "Trash is empty", 14, new Color(0.5f, 0.5f, 0.5f), 0, 40, 200, 24);
        et.alignment = TextAlignmentOptions.Center;

        // FIX R2-A: txtRestoreSelected juga dipakai untuk pesan error "Pilih file terlebih dahulu!"
        var st = Txt(panel.transform, "txtRestoreSelected", "Selected: none", 12, Color.white, 0, 80, 300, 20);
        st.alignment = TextAlignmentOptions.Center;

        var t = panel.AddComponent<TrashController>();
        t.trashPanel = panel;
        t.content = c.transform;
        t.txtEmpty = et;
        t.txtSelected = st;
        t.btnClose = Btn(panel.transform, "BtnClose", "Close", new Color(0.35f, 0.35f, 0.35f), -100, -140, 140, 30);
        t.btnRestore = Btn(panel.transform, "BtnRestore", "Restore", new Color(0.2f, 0.65f, 0.2f), 100, -140, 140, 30);

        panel.transform.SetAsLastSibling();

        var pref = new GameObject("TrashItem", typeof(RectTransform), typeof(Image), typeof(Button));
        pref.SetActive(false);
        pref.transform.SetParent(panel.transform, false);
        S(pref.GetComponent<RectTransform>(), 0, 0, 380, 28);
        pref.GetComponent<Image>().color = new Color(1, 1, 1, 0.1f);
        Txt(pref.transform, "Label", "file", 13, Color.white, 0, 0, 350, 22).alignment = TextAlignmentOptions.Left;
        t.trashItemPrefab = pref;
    }

    // ===================== CRASH (Round 3) =====================

    static void SetupCrashOverlay()
    {
        var dc = FindCanvas("desktop"); if (!dc) return;
        var go = FullScreen(dc.transform, "CrashOverlay", new Color(0, 0, 0, 0.95f));
        go.SetActive(false);
        var ctrl = go.AddComponent<CrashController>();
        ctrl.crashOverlay = go;
        ctrl.crashBg = go.GetComponent<Image>();
        ctrl.crashText = Txt(go.transform, "CrashText", "", 36, Color.white, 0, 0, 500, 50);
        ctrl.crashText.alignment = TextAlignmentOptions.Center;
        // FIX R3: CrashController.cs kini memanggil OnCrashFinished() sebelum callback
        // untuk mengunci Trash. Tidak perlu field tambahan di Inspector.
    }

    // ===================== RANSOMWARE (Round 4) =====================

    static void SetupRansomwarePanel()
    {
        var dc = FindCanvas("desktop"); if (!dc) return;
        var go = FullScreen(dc.transform, "RansomwarePanel", new Color(0.85f, 0.05f, 0.05f, 0.95f));
        go.SetActive(false);
        var ctrl = go.AddComponent<RansomwareController>();
        ctrl.ransomwarePanel = go;
        ctrl.txtTitle = Txt(go.transform, "txtTitle", "⚠ YOUR FILES HAVE BEEN ENCRYPTED! ⚠", 32, Color.white, 0, 80, 600, 50);
        ctrl.txtTitle.alignment = TextAlignmentOptions.Center;
        ctrl.txtDetail = Txt(go.transform, "txtDetail", "...", 18, new Color(0.95f, 0.9f, 0.9f), 0, -10, 500, 140);
        ctrl.txtDetail.alignment = TextAlignmentOptions.Center;
        // FIX R4: RansomwareController.cs kini punya guard _done agar btnOk tidak double-invoke
        ctrl.btnOk = Btn(go.transform, "BtnOk", "OK — Lanjutkan", new Color(1f, 0.76f, 0.2f), 0, -100, 200, 42);
    }

    // ===================== BACKUP (Round 5–7) =====================

    static void SetupBackupPanels()
    {
        var dc = FindCanvas("desktop"); if (!dc) return;
        var ctrl = new GameObject("BackupController").AddComponent<BackupController>();

        // Round 5: pertanyaan backup
        var q = Modal(dc.transform, "BackupQuestionPanel", 420, 240);
        q.SetActive(false);
        Txt(q.transform, "Title", "⚠ Data Hilang!", 22, Color.white, 0, 70, 350, 35).alignment = TextAlignmentOptions.Center;
        Txt(q.transform, "Desc", "Do you have a backup?", 16, Color.white, 0, 20, 350, 30).alignment = TextAlignmentOptions.Center;
        ctrl.questionPanel = q;
        ctrl.btnYes = Btn(q.transform, "BtnYes", "✅ Yes, I have backup", new Color(0.2f, 0.7f, 0.2f), -85, -50, 170, 40);
        ctrl.btnNo = Btn(q.transform, "BtnNo", "❌ No backup", new Color(0.85f, 0.25f, 0.25f), 85, -50, 170, 40);

        // Round 6: pilih sumber recovery — FIX R6: deskripsi tambahan
        var r = Modal(dc.transform, "RecoveryPanel", 420, 240);
        r.SetActive(false);
        Txt(r.transform, "Title", "🔄 Select Recovery Source", 20, Color.white, 0, 70, 350, 35).alignment = TextAlignmentOptions.Center;
        Txt(r.transform, "Desc", "Pilih sumber backup yang akan digunakan:", 13, new Color(0.8f, 0.8f, 0.8f), 0, 25, 360, 22).alignment = TextAlignmentOptions.Center;
        ctrl.recoveryPanel = r;
        ctrl.btnCloud = Btn(r.transform, "BtnCloud", "☁ Cloud Backup", new Color(0.2f, 0.5f, 0.9f), -90, -20, 170, 44);
        ctrl.btnExternal = Btn(r.transform, "BtnExternal", "💾 External Drive", new Color(0.2f, 0.7f, 0.2f), 90, -20, 170, 44);

        // Round 7: setup jadwal backup
        var s = Modal(dc.transform, "BackupSetupPanel", 480, 380);
        s.SetActive(false);
        Txt(s.transform, "Title", "⚙ Setup Automatic Backup", 20, Color.white, 0, 160, 400, 35).alignment = TextAlignmentOptions.Center;
        Txt(s.transform, "LocLbl", "Backup Location:", 14, Color.white, -140, 80, 180, 24).alignment = TextAlignmentOptions.Right;
        ctrl.ddLocation = Drop(s.transform, "LocDD", 50, 80, new[] { "Same Disk (unsafe)", "💾 External Drive (safe)", "☁ Cloud (safe)" });
        Txt(s.transform, "SchedLbl", "Backup Schedule:", 14, Color.white, -140, 20, 180, 24).alignment = TextAlignmentOptions.Right;
        ctrl.ddSchedule = Drop(s.transform, "SchedDD", 50, 20, new[] { "Never (unsafe)", "Weekly", "Daily (safe)" });
        ctrl.setupPanel = s;
        ctrl.btnSave = Btn(s.transform, "BtnSave", "💾 Save Backup Config", new Color(1f, 0.76f, 0.2f), 0, -60, 220, 44);
    }

    // ===================== SIMULATION PANEL (Round 8) =====================
    // FIX R7-A: panel ini ditampilkan saat state Simulation berjalan
    static void SetupSimulationPanel()
    {
        var dc = FindCanvas("desktop"); if (!dc) return;
        var panel = Modal(dc.transform, "SimulationPanel", 460, 260);
        panel.SetActive(false);

        Txt(panel.transform, "txtSimTitle",
            "⚙ Running Simulation...", 22, Color.white, 0, 80, 400, 36).alignment = TextAlignmentOptions.Center;

        Txt(panel.transform, "txtSimDetail",
            "Sistem sedang menguji konfigurasi backup kamu...", 15,
            new Color(0.85f, 0.85f, 0.85f), 0, 10, 400, 60).alignment = TextAlignmentOptions.Center;

        // Progress bar visual sederhana: background + fill
        var barBg = new GameObject("ProgressBarBg", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(panel.transform, false);
        S(barBg.GetComponent<RectTransform>(), 0, -60, 380, 18);
        barBg.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

        var barFill = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
        barFill.transform.SetParent(barBg.transform, false);
        var fillRt = barFill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(1, 1);
        fillRt.sizeDelta = Vector2.zero;
        barFill.GetComponent<Image>().color = new Color(0.2f, 0.65f, 1f);

        panel.transform.SetAsLastSibling();
    }

    // ===================== FEEDBACK PANEL =====================

    static void SetupFeedbackPanel()
    {
        var dc = FindCanvas("desktop"); if (!dc) return;
        var panel = Modal(dc.transform, "FeedbackPanel", 460, 280);
        panel.SetActive(false);
        Txt(panel.transform, "txtFeedbackTitle", "Result", 26, Color.white, 0, 80, 400, 40).alignment = TextAlignmentOptions.Center;
        Txt(panel.transform, "txtFeedbackDetail", "...", 16, Color.white, 0, -10, 380, 120).alignment = TextAlignmentOptions.Center;

        // FIX R5: nama tombol HARUS "BtnFeedbackOk" persis
        // GameManager mencari nama ini via transform.Find("BtnFeedbackOk") untuk enable/disable-nya
        var okBtn = Btn(panel.transform, "BtnFeedbackOk", "OK", new Color(1f, 0.76f, 0.2f), 0, -100, 150, 36);
        okBtn.onClick.AddListener(() => panel.SetActive(false));
    }

    // ===================== GAME MANAGER =====================

    static void SetupGameManager()
    {
        var gm = new GameObject("GameManager_Tp6").AddComponent<GameManager_Tp6>();
        gm.desktopCanvas = FindCanvas("desktop")?.gameObject;
        gm.objectiveUI = Object.FindObjectOfType<ObjectiveUI_Tp4>(true);
        gm.explorer = Object.FindObjectOfType<ExplorerController>(true);
        gm.trash = Object.FindObjectOfType<TrashController>(true);   // FIX R3: wajib
        gm.crash = Object.FindObjectOfType<CrashController>(true);
        gm.ransomware = Object.FindObjectOfType<RansomwareController>(true);
        gm.backup = Object.FindObjectOfType<BackupController>(true);
        gm.feedbackPanel = GameObject.Find("FeedbackPanel");                 // FIX R5: wajib
        gm.summaryCanvas = FindCanvas("summary")?.gameObject
                        ?? FindCanvas("CanvasSummaryPanel")?.gameObject;

        // Validasi field wajib — tampilkan warning di Console jika ada yang null
        if (gm.trash == null) Debug.LogWarning("[Tp6 Setup] TrashController tidak ditemukan! Fix R3 tidak aktif.");
        if (gm.crash == null) Debug.LogWarning("[Tp6 Setup] CrashController tidak ditemukan!");
        if (gm.feedbackPanel == null) Debug.LogWarning("[Tp6 Setup] FeedbackPanel tidak ditemukan! Fix R5 tidak aktif.");
        if (gm.summaryCanvas == null) Debug.LogWarning("[Tp6 Setup] SummaryCanvas tidak ditemukan — cek nama canvas mengandung 'summary' atau 'CanvasSummaryPanel'.");

        var em = new GameObject("EvaluationManager_Tp6").AddComponent<EvaluationManager_Tp6>();
        em.evaluationCanvas = FindCanvas("evaluation")?.gameObject;
        em.evaluationPanel = Object.FindObjectOfType<EvaluationPanel_Tp4>(true);
        gm.evaluation = em;

        if (GameObject.Find("ScoreManager") == null)
            new GameObject("ScoreManager").AddComponent<ScoreManager>();
    }

    // ===================== HELPERS =====================

    static Canvas FindCanvas(string p)
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
            if (c.name.ToLower().Contains(p.ToLower())) return c;
        return null;
    }

    static TMP_Text Txt(Transform p, string n, string t, int s, Color c, float x, float y, float w, float h)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(p, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = t; tmp.fontSize = s; tmp.color = c;
        S(go.GetComponent<RectTransform>(), x, y, w, h);
        return tmp;
    }

    static TMP_Text ChildTxt(Transform p, string n, string t, int s, Color c)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(p, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = t; tmp.fontSize = s; tmp.color = c;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        return tmp;
    }

    static Button Btn(Transform p, string n, string l, Color c, float x, float y, float w, float h)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(p, false);
        S(go.GetComponent<RectTransform>(), x, y, w, h);
        go.GetComponent<Image>().color = c;
        var lb = new GameObject("Label", typeof(RectTransform));
        lb.transform.SetParent(go.transform, false);
        var lt = lb.AddComponent<TextMeshProUGUI>();
        lt.text = l; lt.fontSize = 15; lt.alignment = TextAlignmentOptions.Center; lt.color = Color.white;
        var lrt = lb.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        return go.GetComponent<Button>();
    }

    static GameObject Modal(Transform p, string n, float w, float h)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(p, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.96f);
        return go;
    }

    static GameObject FullScreen(Transform p, string n, Color c)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(p, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = c;
        return go;
    }

    static TMP_Dropdown Drop(Transform p, string n, float x, float y, string[] opts)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
        go.transform.SetParent(p, false);
        S(go.GetComponent<RectTransform>(), x, y, 220, 34);
        go.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f);
        var dd = go.GetComponent<TMP_Dropdown>();
        dd.options.Clear();
        foreach (var o in opts) dd.options.Add(new TMP_Dropdown.OptionData(o));
        var cl = new GameObject("Label", typeof(RectTransform));
        cl.transform.SetParent(go.transform, false);
        var ct = cl.AddComponent<TextMeshProUGUI>();
        ct.fontSize = 14; ct.alignment = TextAlignmentOptions.Left; ct.color = Color.white;
        var crt = cl.GetComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(10, 2); crt.offsetMax = new Vector2(-28, -2);
        dd.captionText = ct;
        return dd;
    }

    static void S(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}