using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ObjectivePanel : MonoBehaviour
{
    [Header("=== Isi Teks Objective ===")]
    [TextArea(1, 3)] public string judulObjective = "Telusuri email dan tentukan decision yang benar";
    [TextArea(1, 2)] public string instruksi1 = "1. Balas jika email AMAN";
    [TextArea(1, 2)] public string instruksi2 = "2. Hapus jika email MENCURIGAKAN";
    [TextArea(1, 2)] public string instruksi3 = "3. Laporkan jika email TIDAK AMAN";
    [TextArea(1, 2)] public string labelTombol = "Mengerti";

    [Header("=== Layout (prosedural) ===")]
    public float padding = 26f;
    public float titleHeight = 40f;
    public float rowHeight = 34f;
    public float dividerThickness = 2f;
    public float gapAfterDivider = 12f;
    public float buttonWidth = 180f;
    public float buttonHeight = 44f;

    // ── Referensi hasil build (dipakai runtime) ──
    public Button btnTutup;
    public TMP_Text txtTitle;
    public TMP_Text txtInstruction1;
    public TMP_Text txtInstruction2;
    public TMP_Text txtInstruction3;
    public RectTransform divider;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            transform.GetChild(i).gameObject.SetActive(false);

        BuildUi();
    }

    public void TampilkanJikaPerlu()
    {
        // Gunakan static bool agar cukup sekali per session
        if (!ObjectiveState.SudahTampil)
        {
            ObjectiveState.SudahTampil = true;
            gameObject.SetActive(true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void TutupPanel()
    {
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    [ContextMenu("Reset Objective (untuk testing)")]
    public void ResetObjective()
    {
        ObjectiveState.SudahTampil = false;
        Debug.Log("[ObjectivePanel] Reset selesai. Panel akan muncul lagi saat Play.");
    }


    private void BuildUi()
    {
        RectTransform panel = (RectTransform)transform;
        float w = panel.rect.width;
        float contentW = w - padding * 2f;
        float y = -padding; // dari atas panel

        // Title
        txtTitle = MakeText("Title", judulObjective, 0f, y, contentW, titleHeight,
            22f, FontStyles.Bold, TextAlignmentOptions.Center);
        txtTitle.color = new Color(0.16f, 0.17f, 0.20f, 1f);
        y -= titleHeight;

        // Divider
        divider = MakeDivider("Divider", 0f, y, contentW, dividerThickness);
        y -= dividerThickness + gapAfterDivider;

        // Instruction rows
        txtInstruction1 = MakeText("Instruction1", instruksi1, 0f, y, contentW, rowHeight,
            17f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        y -= rowHeight;

        txtInstruction2 = MakeText("Instruction2", instruksi2, 0f, y, contentW, rowHeight,
            17f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        y -= rowHeight;

        txtInstruction3 = MakeText("Instruction3", instruksi3, 0f, y, contentW, rowHeight,
            17f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        y -= rowHeight + 6f;

        // Button
        btnTutup = MakeButton("Button", 0f, y, buttonWidth, buttonHeight, labelTombol);
    }

    private TMP_Text MakeText(string objName, string text, float x, float y,
        float w, float h, float fontSize, FontStyles style, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(objName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = new Color(0.25f, 0.27f, 0.30f, 1f);
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private RectTransform MakeDivider(string objName, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(objName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.75f, 0.77f, 0.80f, 0.9f);
        img.raycastTarget = false;
        return rt;
    }

    private Button MakeButton(string objName, float x, float y, float w, float h, string label)
    {
        GameObject go = new GameObject(objName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.20f, 0.46f, 0.86f, 1f);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(TutupPanel);

        GameObject labelGO = new GameObject("Label",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);

        RectTransform lrt = (RectTransform)labelGO.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        TextMeshProUGUI ltmp = labelGO.GetComponent<TextMeshProUGUI>();
        ltmp.text = label;
        ltmp.fontSize = 18;
        ltmp.fontStyle = FontStyles.Bold;
        ltmp.alignment = TextAlignmentOptions.Center;
        ltmp.color = Color.white;
        ltmp.raycastTarget = false;
        return btn;
    }
}

/// <summary>
/// Static class untuk menyimpan state objective selama session berjalan.
/// </summary>
public static class ObjectiveState
{
    public static bool SudahTampil = false;
}
