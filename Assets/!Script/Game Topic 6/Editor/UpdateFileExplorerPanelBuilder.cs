using MPUIKIT;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Membangun ulang UI FileExplorerPanel (Topic 6) dengan desain ala Windows File Explorer:
/// sidebar folder di kiri (drop zone), daftar file di kanan, tombol X di title bar.
/// Semua background memakai MPImage. Jalankan dari menu: Tools > Topic6 > Build UpdateFileExplorerPanel
/// </summary>
public static class UpdateFileExplorerPanelBuilder
{
    // ── warna tema (Windows 11 dark) ──
    static readonly Color cRoot     = new Color(0.125f, 0.125f, 0.125f, 1f); // #202020
    static readonly Color cTitle    = new Color(0.169f, 0.169f, 0.169f, 1f); // #2B2B2B
    static readonly Color cSidebar  = new Color(0.145f, 0.145f, 0.145f, 1f); // #252525
    static readonly Color cRow      = new Color(0.176f, 0.176f, 0.176f, 1f); // #2D2D2D
    static readonly Color cFileArea = new Color(0.106f, 0.106f, 0.106f, 1f); // #1B1B1B
    static readonly Color cInput    = new Color(0.122f, 0.122f, 0.122f, 1f); // #1F1F1F
    static readonly Color cBlue     = new Color(0f, 0.47f, 0.83f, 1f);       // #0078D4
    static readonly Color cRed      = new Color(0.77f, 0.17f, 0.11f, 1f);    // #C42B1C
    static readonly Color cText     = new Color(0.91f, 0.91f, 0.91f, 1f);
    static readonly Color cTextDim  = new Color(0.6f, 0.6f, 0.6f, 1f);

    [MenuItem("Tools/Topic6/Build UpdateFileExplorerPanel")]
    public static void Build()
    {
        GameObject panel = GameObject.Find("DesktopCanvas/FileExplorerPanel");
        if (panel == null)
        {
            Debug.LogError("[Builder] DesktopCanvas/FileExplorerPanel tidak ditemukan di scene aktif");
            return;
        }
        var ec = panel.GetComponent<ExplorerController>();
        if (ec == null)
        {
            Debug.LogError("[Builder] ExplorerController tidak ada di FileExplorerPanel");
            return;
        }

        // hapus build lama jika ada
        Transform prev = panel.transform.Find("updatefileexplorerpanel");
        if (prev != null) Object.DestroyImmediate(prev.gameObject);

        // ════ ROOT ════
        GameObject root = MakeGO("updatefileexplorerpanel", panel.transform);
        SetRect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddMP(root, cRoot, new Vector4(14, 14, 14, 14));

        // ════ TITLE BAR ════
        GameObject titleBar = MakeGO("TitleBar", root.transform);
        SetRect(titleBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -56), Vector2.zero);
        AddMP(titleBar, cTitle, new Vector4(0, 14, 14, 0));

        GameObject titleTxt = MakeGO("TxtTitle", titleBar.transform);
        SetRect(titleTxt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-70, 0));
        AddText(titleTxt, "File Explorer", 24, cText, TextAlignmentOptions.MidlineLeft, true);

        // tombol X
        GameObject btnX = MakeGO("BtnCloseX", titleBar.transform);
        SetRect(btnX, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-56, 0), Vector2.zero);
        MPImage xImg = AddMP(btnX, cRed, new Vector4(0, 14, 0, 0));
        Button xBtn = btnX.AddComponent<Button>();
        xBtn.targetGraphic = xImg;
        GameObject xLbl = MakeGO("Label", btnX.transform);
        SetRect(xLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddText(xLbl, "X", 24, Color.white, TextAlignmentOptions.Center, true);

        // ════ SIDEBAR (folder, kiri) ════
        GameObject sidebar = MakeGO("Sidebar", root.transform);
        SetRect(sidebar, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(250, -56));
        AddMP(sidebar, cSidebar, new Vector4(14, 0, 0, 0));
        var sbLayout = sidebar.AddComponent<VerticalLayoutGroup>();
        sbLayout.padding = new RectOffset(12, 12, 14, 14);
        sbLayout.spacing = 8;
        sbLayout.childAlignment = TextAnchor.UpperCenter;
        sbLayout.childControlWidth = true;
        sbLayout.childForceExpandWidth = true;
        sbLayout.childControlHeight = false;
        sbLayout.childForceExpandHeight = false;

        string[] folderNames = { "Work Files", "Personal", "Downloads", "Important_Project", "Trash" };
        FileLocation[] folderLocs =
        {
            FileLocation.WorkFiles, FileLocation.Personal, FileLocation.Downloads,
            FileLocation.ImportantProject, FileLocation.Trash
        };
        var folderLabels = new TextMeshProUGUI[5];

        for (int i = 0; i < 5; i++)
        {
            GameObject fz = MakeGO("Folder_" + folderNames[i].Replace(" ", ""), sidebar.transform);
            fz.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 48);
            MPImage fzImg = AddMP(fz, cRow, new Vector4(8, 8, 8, 8));
            var dt = fz.AddComponent<DropTarget>();
            dt.targetLocation = folderLocs[i];
            dt.bg = fzImg;

            GameObject fzLbl = MakeGO("Label", fz.transform);
            SetRect(fzLbl, Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-8, 0));
            folderLabels[i] = AddText(fzLbl, folderNames[i], 17, cText, TextAlignmentOptions.MidlineLeft, true);
        }

        // ════ FILE AREA (kanan) ════
        GameObject fileArea = MakeGO("FileArea", root.transform);
        SetRect(fileArea, new Vector2(0, 0), new Vector2(1, 1), new Vector2(250, 0), new Vector2(0, -56));
        AddMP(fileArea, cFileArea, new Vector4(0, 0, 0, 14));

        GameObject fileList = MakeGO("FileListContainer", fileArea.transform);
        SetRect(fileList, Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -14));
        var flLayout = fileList.AddComponent<VerticalLayoutGroup>();
        flLayout.spacing = 8;
        flLayout.childAlignment = TextAnchor.UpperCenter;
        flLayout.childControlWidth = true;
        flLayout.childForceExpandWidth = true;
        flLayout.childControlHeight = false;
        flLayout.childForceExpandHeight = false;

        var newItems = new FileItem[7];
        for (int i = 0; i < 7; i++)
            newItems[i] = BuildFileRow(fileList.transform, i, hasDelete: i == 6);

        // ════ NONAKTIFKAN UI LAMA ════
        foreach (string oldName in new[] { "Title", "FileList", "FolderZones", "BtnClose" })
        {
            Transform t = panel.transform.Find(oldName);
            if (t != null) t.gameObject.SetActive(false);
        }
        // bg lama panel dibuat transparan tapi tetap memblok raycast
        var panelImg = panel.GetComponent<MPImage>();
        if (panelImg != null)
        {
            Color c = panelImg.color;
            c.a = 0f;
            panelImg.color = c;
        }

        // ════ REWIRE ExplorerController ════
        ec.fileListContainer = fileList.transform;
        ec.poolItems = newItems;
        ec.btnClose = xBtn;
        ec.labelWork = folderLabels[0];
        ec.labelPersonal = folderLabels[1];
        ec.labelDownloads = folderLabels[2];
        ec.labelImp = folderLabels[3];
        ec.labelTrash = folderLabels[4];

        EditorUtility.SetDirty(ec);
        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(panel.scene);
        EditorSceneManager.SaveScene(panel.scene);

        Debug.Log("[Builder] updatefileexplorerpanel selesai dibangun & ExplorerController di-rewire. Scene disimpan.");
    }

    /// <summary>
    /// Bikin kolom rename + tombol OK di tiap baris. Dimatikan sejak fitur rename dicabut.
    /// Sengaja static readonly, bukan const, biar compiler nggak ngeluh "unreachable code".
    /// </summary>
    static readonly bool ENABLE_RENAME_UI = false;

    static FileItem BuildFileRow(Transform parent, int index, bool hasDelete)
    {
        GameObject row = MakeGO("FileItem_" + index, parent);
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 52);
        MPImage rowImg = AddMP(row, cRow, new Vector4(8, 8, 8, 8));
        row.AddComponent<CanvasGroup>();
        var fi = row.AddComponent<FileItem>();
        fi.bgImage = rowImg;

        // nama file
        GameObject nameGo = MakeGO("txtName", row.transform);
        SetRect(nameGo, new Vector2(0, 0), new Vector2(0, 1), new Vector2(16, 0), new Vector2(280, 0));
        fi.txtName = AddText(nameGo, "file_" + index, 17, cText, TextAlignmentOptions.MidlineLeft, false);

        // lock icon
        GameObject lockGo = MakeGO("lockIcon", row.transform);
        SetRect(lockGo, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(286, -11), new Vector2(308, 11));
        var lockImg = lockGo.AddComponent<Image>();
        lockImg.color = new Color(1f, 0.85f, 0.2f, 1f);
        lockImg.raycastTarget = false;
        fi.lockIcon = lockImg;
        lockGo.SetActive(false);

        const float btnW = 40f;
        float deleteOffset = hasDelete ? btnW + 8f : 0f;

        // tombol delete (hanya row terakhir)
        if (hasDelete)
        {
            GameObject delGo = MakeGO("deleteBtn", row.transform);
            SetRect(delGo, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10 - btnW, -18), new Vector2(-10, 18));
            MPImage delImg = AddMP(delGo, cRed, new Vector4(6, 6, 6, 6));
            var delBtn = delGo.AddComponent<Button>();
            delBtn.targetGraphic = delImg;
            GameObject delLbl = MakeGO("Label", delGo.transform);
            SetRect(delLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddText(delLbl, "X", 16, Color.white, TextAlignmentOptions.Center, true);
            fi.deleteBtn = delBtn;
        }

        // Fitur rename dimatikan: tiap baris cuma nampilin nama file + lock icon,
        // plus tombol delete di baris terakhir. Kalau suatu saat mau dihidupkan lagi,
        // set ENABLE_RENAME_UI = true di atas — ExplorerController sudah siap
        // (WireRow tetap ngecek null, jadi aman kalau tombolnya nggak ada).
        if (ENABLE_RENAME_UI)
        {
            // tombol rename
            GameObject renGo = MakeGO("renameBtn", row.transform);
            SetRect(renGo, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-10 - deleteOffset - btnW, -18), new Vector2(-10 - deleteOffset, 18));
            MPImage renImg = AddMP(renGo, cBlue, new Vector4(6, 6, 6, 6));
            var renBtn = renGo.AddComponent<Button>();
            renBtn.targetGraphic = renImg;
            GameObject renLbl = MakeGO("Label", renGo.transform);
            SetRect(renLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddText(renLbl, "OK", 13, Color.white, TextAlignmentOptions.Center, true);
            fi.renameBtn = renBtn;

            // input rename (TMP_InputField)
            float inputRight = 10 + deleteOffset + btnW + 8;
            GameObject inGo = MakeGO("InputRename", row.transform);
            SetRect(inGo, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-inputRight - 190, -18), new Vector2(-inputRight, 18));
            MPImage inImg = AddMP(inGo, cInput, new Vector4(6, 6, 6, 6));
            var inField = inGo.AddComponent<TMP_InputField>();
            inField.targetGraphic = inImg;

            GameObject textArea = MakeGO("TextArea", inGo.transform);
            SetRect(textArea, Vector2.zero, Vector2.one, new Vector2(10, 2), new Vector2(-10, -2));
            textArea.AddComponent<RectMask2D>();

            GameObject phGo = MakeGO("Placeholder", textArea.transform);
            SetRect(phGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI phTxt = AddText(phGo, "rename...", 14, cTextDim, TextAlignmentOptions.MidlineLeft, false);
            phTxt.fontStyle = FontStyles.Italic;

            GameObject txGo = MakeGO("Text", textArea.transform);
            SetRect(txGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            TextMeshProUGUI txTxt = AddText(txGo, "", 14, cText, TextAlignmentOptions.MidlineLeft, false);

            inField.textViewport = textArea.GetComponent<RectTransform>();
            inField.textComponent = txTxt;
            inField.placeholder = phTxt;
            fi.inputRename = inField;
        }

        return fi;
    }

    /// <summary>
    /// Membangun panel prompt interaksi "[E] ..." gaya lama untuk sistem baru
    /// (PlayerInteraction + InteractionPromptUI). Bottom-center, auto-hide saat Awake.
    /// </summary>
    [MenuItem("Tools/Topic6/Build InteractionPromptUI")]
    public static void BuildInteractionPrompt()
    {
        // hapus yang lama jika ada
        var existing = Object.FindObjectOfType<InteractionPromptUI>(true);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // root canvas
        var canvasGo = new GameObject("InteractionPromptCanvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // panel bottom-center
        GameObject panel = MakeGO("PromptPanel", canvasGo.transform);
        var pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 0);
        pr.anchorMax = new Vector2(0.5f, 0);
        pr.pivot = new Vector2(0.5f, 0);
        pr.anchoredPosition = new Vector2(0, 48);
        pr.sizeDelta = new Vector2(520, 64);
        AddMP(panel, new Color(0f, 0f, 0f, 0.78f), new Vector4(12, 12, 12, 12));

        // kotak tombol "E"
        GameObject keyBox = MakeGO("KeyBox", panel.transform);
        SetRect(keyBox, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, -20), new Vector2(52, 20));
        AddMP(keyBox, new Color(1f, 0.85f, 0.3f, 1f), new Vector4(8, 8, 8, 8));
        GameObject keyLbl = MakeGO("KeyText", keyBox.transform);
        SetRect(keyLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddText(keyLbl, "E", 22, new Color(0.1f, 0.1f, 0.1f, 1f), TextAlignmentOptions.Center, true);

        // teks prompt (diisi dinamis oleh InteractionPromptUI.ShowPrompt)
        GameObject promptTxt = MakeGO("PromptText", panel.transform);
        SetRect(promptTxt, Vector2.zero, Vector2.one, new Vector2(64, 0), new Vector2(-16, 0));
        TextMeshProUGUI txt = AddText(promptTxt, "Tekan E untuk berinteraksi", 20, Color.white, TextAlignmentOptions.MidlineLeft, false);

        // pasang & wire InteractionPromptUI
        var ui = canvasGo.AddComponent<InteractionPromptUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("promptPanel").objectReferenceValue = panel;
        so.FindProperty("promptText").objectReferenceValue = txt;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(canvasGo);
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        EditorSceneManager.SaveScene(canvasGo.scene);
        Debug.Log("[Builder] InteractionPromptCanvas dibuat & scene disimpan.");
    }

    // ── helpers ──
    static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return go;
    }

    static void SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = aMin;
        r.anchorMax = aMax;
        r.offsetMin = oMin;
        r.offsetMax = oMax;
    }

    static MPImage AddMP(GameObject go, Color col, Vector4 cornerRadius)
    {
        var img = go.AddComponent<MPImage>();
        img.color = col;
        img.DrawShape = DrawShape.Rectangle;
        Rectangle rect = img.Rectangle;
        rect.CornerRadius = cornerRadius;
        img.Rectangle = rect;
        return img;
    }

    static TextMeshProUGUI AddText(GameObject go, string txt, float size, Color col, TextAlignmentOptions align, bool bold)
    {
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = txt;
        t.fontSize = size;
        t.color = col;
        t.alignment = align;
        if (bold) t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        return t;
    }
}
