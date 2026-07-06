using MPUIKIT;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Membangun ulang UI TrashPanel (Topic 6) dengan tema gelap yang sama seperti
/// FileExplorerPanel (Windows 11 dark). Semua background memakai MPImage.
/// Judul "Trash" TANPA ikon tempat sampah. Jalankan dari menu:
/// Tools > Topic6 > Build UpdateTrashPanel
/// </summary>
public static class UpdateTrashPanelBuilder
{
    // ── warna tema (sama dengan UpdateFileExplorerPanelBuilder) ──
    static readonly Color cRoot     = new Color(0.125f, 0.125f, 0.125f, 1f); // #202020
    static readonly Color cBar      = new Color(0.122f, 0.122f, 0.122f, 1f); // #1F1F1F
    static readonly Color cContent  = new Color(0.106f, 0.106f, 0.106f, 1f); // #1B1B1B
    static readonly Color cRow      = new Color(0.176f, 0.176f, 0.176f, 1f); // #2D2D2D
    static readonly Color cBlue     = new Color(0f, 0.47f, 0.83f, 1f);       // #0078D4
    static readonly Color cBorder   = new Color(0.227f, 0.227f, 0.227f, 1f); // #3A3A3A
    static readonly Color cText     = new Color(0.91f, 0.91f, 0.91f, 1f);    // #E8E8E8
    static readonly Color cTextDim  = new Color(0.5f, 0.5f, 0.5f, 1f);       // #808080
    static readonly Color cNote     = new Color(0.416f, 0.416f, 0.416f, 1f); // #6A6A6A

    [MenuItem("Tools/Topic6/Build UpdateTrashPanel")]
    public static void Build()
    {
        GameObject panel = GameObject.Find("DesktopCanvas/TrashPanel");
        if (panel == null)
        {
            Debug.LogError("[TrashBuilder] DesktopCanvas/TrashPanel tidak ditemukan di scene aktif");
            return;
        }
        var tc = panel.GetComponent<TrashController>();
        if (tc == null)
        {
            Debug.LogError("[TrashBuilder] TrashController tidak ada di TrashPanel");
            return;
        }

        // hapus build lama jika ada
        Transform prev = panel.transform.Find("updatetrashpanel");
        if (prev != null) Object.DestroyImmediate(prev.gameObject);

        // perbesar root & jaga background lama tetap memblok raycast (ketutup child opaque)
        var pr = panel.GetComponent<RectTransform>();
        pr.sizeDelta = new Vector2(480, 400);

        // ════ ROOT (window) ════
        GameObject root = MakeGO("updatetrashpanel", panel.transform);
        SetRect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddMP(root, cRoot, new Vector4(14, 14, 14, 14));

        // ════ TITLE (tanpa ikon) ════
        GameObject title = MakeGO("Title", root.transform);
        SetRect(title, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -56), new Vector2(-16, -16));
        AddText(title, "Trash", 26, cText, TextAlignmentOptions.Center, true);

        // ════ SELECTED BAR ════
        GameObject bar = MakeGO("SelectedBar", root.transform);
        SetRect(bar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -108), new Vector2(-16, -64));
        AddMP(bar, cBar, new Vector4(8, 8, 8, 8));
        GameObject selTxt = MakeGO("txtSelected", bar.transform);
        SetRect(selTxt, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-16, 0));
        TextMeshProUGUI txtSelected = AddText(selTxt, "Selected: none", 16, cText, TextAlignmentOptions.MidlineLeft, true);

        // ════ CONTENT AREA ════
        GameObject content = MakeGO("ContentArea", root.transform);
        SetRect(content, Vector2.zero, Vector2.one, new Vector2(16, 96), new Vector2(-16, -116));
        MPImage contentImg = AddMP(content, cContent, new Vector4(10, 10, 10, 10));
        contentImg.OutlineWidth = 1.5f;
        contentImg.OutlineColor = cBorder;

        // container list (target instantiate item trash)
        GameObject list = MakeGO("ListContainer", content.transform);
        SetRect(list, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -12));
        var vlg = list.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandHeight = false;

        // empty-state text (overlay tengah)
        GameObject empty = MakeGO("txtEmpty", content.transform);
        SetRect(empty, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-140, -16), new Vector2(140, 16));
        TextMeshProUGUI txtEmpty = AddText(empty, "Trash is empty", 16, cTextDim, TextAlignmentOptions.Center, false);

        // ════ NOTE ════
        GameObject note = MakeGO("Note", root.transform);
        SetRect(note, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 64), new Vector2(-16, 86));
        AddText(note, "Items are permanently removed after 30 days.", 13, cNote, TextAlignmentOptions.Center, false);

        // ════ TOMBOL CLOSE (kiri) ════
        GameObject closeGo = MakeGO("BtnClose", root.transform);
        SetRect(closeGo, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(16, 12), new Vector2(-8, 56));
        MPImage closeImg = AddMP(closeGo, cRow, new Vector4(8, 8, 8, 8));
        Button btnClose = closeGo.AddComponent<Button>();
        btnClose.targetGraphic = closeImg;
        GameObject closeLbl = MakeGO("Label", closeGo.transform);
        SetRect(closeLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddText(closeLbl, "Close", 16, cText, TextAlignmentOptions.Center, true);

        // ════ TOMBOL RESTORE (kanan, aksen biru) ════
        GameObject restGo = MakeGO("BtnRestore", root.transform);
        SetRect(restGo, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(8, 12), new Vector2(-16, 56));
        MPImage restImg = AddMP(restGo, cBlue, new Vector4(8, 8, 8, 8));
        Button btnRestore = restGo.AddComponent<Button>();
        btnRestore.targetGraphic = restImg;
        GameObject restLbl = MakeGO("Label", restGo.transform);
        SetRect(restLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddText(restLbl, "↻ Restore", 16, Color.white, TextAlignmentOptions.Center, true);

        // ════ TEMPLATE ITEM TRASH (inactive, di luar layout) ════
        GameObject tmpl = MakeGO("TrashItem", root.transform);
        tmpl.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
        MPImage tmplImg = AddMP(tmpl, cRow, new Vector4(6, 6, 6, 6));
        Button tmplBtn = tmpl.AddComponent<Button>();
        tmplBtn.targetGraphic = tmplImg;
        GameObject tmplLbl = MakeGO("Label", tmpl.transform);
        SetRect(tmplLbl, Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-14, 0));
        AddText(tmplLbl, "file", 15, cText, TextAlignmentOptions.MidlineLeft, false);
        tmpl.SetActive(false);

        // ════ NONAKTIFKAN UI LAMA ════
        foreach (string oldName in new[] {
            "Title", "Content", "txtTrashEmpty", "txtRestoreSelected", "BtnClose", "BtnRestore" })
        {
            Transform t = panel.transform.Find(oldName);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
        // template "TrashItem" lama adalah anak langsung panel; template baru ada di dalam root
        Transform oldItem = panel.transform.Find("TrashItem");
        if (oldItem != null && oldItem != tmpl.transform)
            Object.DestroyImmediate(oldItem.gameObject);
        // background lama panel dibuat gelap solid (ketutup child, tetap blok raycast)
        var panelImg = panel.GetComponent<Image>();
        if (panelImg != null) panelImg.color = new Color(0f, 0f, 0f, 0.6f);

        // ════ REWIRE TrashController ════
        tc.trashPanel = panel;
        tc.content = list.transform;
        tc.trashItemPrefab = tmpl;
        tc.txtEmpty = txtEmpty;
        tc.txtSelected = txtSelected;
        tc.btnClose = btnClose;
        tc.btnRestore = btnRestore;

        EditorUtility.SetDirty(tc);
        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(panel.scene);
        EditorSceneManager.SaveScene(panel.scene);

        Debug.Log("[TrashBuilder] updatetrashpanel selesai dibangun & TrashController di-rewire. Scene disimpan.");
    }

    // ── helpers (sama seperti UpdateFileExplorerPanelBuilder) ──
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
