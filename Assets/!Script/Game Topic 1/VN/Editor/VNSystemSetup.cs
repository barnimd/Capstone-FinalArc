using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-click setup for the Topic 1 Visual Novel system.
///
/// Tools menu:
///   - Tools → VN System (Topic 1) → Create VN Canvas in Active Scene
///   - Tools → VN System (Topic 1) → Create Sample Receptionist Dialogue
///
/// Both tools are 100% optional and only used during setup. Safe to keep
/// in the project — they live in an Editor folder and are excluded from builds.
/// </summary>
public static class VNSystemSetup
{
    private const string SAMPLE_DIALOGUE_PATH =
        "Assets/!Script/Game Topic 1/VN/VNDialog_Receptionist_Topic1.asset";

    private const string TALKING_DIALOGUE_PATH =
        "Assets/!Script/Game Topic 1/VN/VNDialog_Talking_Topic1.asset";

    // ------------------------------------------------------------------
    // 1. Build the entire VN Canvas hierarchy in the active scene
    // ------------------------------------------------------------------
    [MenuItem("Tools/VN System (Topic 1)/Create VN Canvas in Active Scene")]
    public static void CreateVNCanvasInScene()
    {
        // ---- Canvas root ----
        GameObject canvasGo = new GameObject("VN_Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // above the world

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // ---- VN_Root (child of canvas — toggled on/off by the manager) ----
        GameObject rootGo = NewUI("VN_Root", canvasGo.transform);
        StretchFull(rootGo.GetComponent<RectTransform>());

        // ---- Background (full-screen Image) ----
        GameObject bgGo = NewUI("Background", rootGo.transform, true);
        Image bg = bgGo.GetComponent<Image>();
        bg.sprite = GetBuiltinUISprite();
        bg.color = new Color(0.20f, 0.25f, 0.32f, 1f);
        StretchFull(bgGo.GetComponent<RectTransform>());

        // ---- Player portrait (LEFT) ----
        GameObject playerGo = NewUI("PlayerPortrait", rootGo.transform, true);
        Image playerImg = playerGo.GetComponent<Image>();
        playerImg.sprite = GetBuiltinUISprite();
        playerImg.color = new Color(0.30f, 0.55f, 0.85f, 1f);
        playerImg.preserveAspect = true;
        AnchorBottomLeftPortrait(playerGo.GetComponent<RectTransform>());

        GameObject playerLabel = AddPortraitLabel(playerGo.transform, "PLAYER");

        // ---- NPC portrait (RIGHT) ----
        GameObject npcGo = NewUI("NpcPortrait", rootGo.transform, true);
        Image npcImg = npcGo.GetComponent<Image>();
        npcImg.sprite = GetBuiltinUISprite();
        npcImg.color = new Color(0.85f, 0.45f, 0.65f, 1f);
        npcImg.preserveAspect = true;
        AnchorBottomRightPortrait(npcGo.GetComponent<RectTransform>());

        GameObject npcLabel = AddPortraitLabel(npcGo.transform, "NPC");

        // ---- Dialogue Box ----
        GameObject boxGo = NewUI("DialogueBox", rootGo.transform, true);
        Image boxImg = boxGo.GetComponent<Image>();
        boxImg.sprite = GetBuiltinUISprite();
        // Strong dark background so text is always readable on bright BGs.
        boxImg.color = new Color(0f, 0f, 0f, 0.88f);
        boxImg.raycastTarget = false;
        AnchorBottomBox(boxGo.GetComponent<RectTransform>());

        // Speaker name
        GameObject nameGo = NewUI("SpeakerName", boxGo.transform);
        TextMeshProUGUI nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "Speaker";
        nameTmp.fontSize = 32;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = new Color(1f, 0.9f, 0.4f);
        AnchorTopLeftInset(nameGo.GetComponent<RectTransform>(),
            new Vector2(40, -20), new Vector2(360, 50));

        // Dialogue text
        GameObject textGo = NewUI("DialogueText", boxGo.transform);
        TextMeshProUGUI textTmp = textGo.AddComponent<TextMeshProUGUI>();
        textTmp.text = "Dialogue goes here…";
        textTmp.fontSize = 26;
        textTmp.color = Color.white;
        textTmp.enableWordWrapping = true;
        StretchInset(textGo.GetComponent<RectTransform>(),
            new Vector2(40, 30), new Vector2(-40, -70));

        // Continue indicator (optional little arrow text)
        GameObject contGo = NewUI("ContinueIndicator", boxGo.transform);
        TextMeshProUGUI contTmp = contGo.AddComponent<TextMeshProUGUI>();
        contTmp.text = "▼";
        contTmp.fontSize = 28;
        contTmp.color = Color.white;
        contTmp.alignment = TextAlignmentOptions.Center;
        AnchorBottomRightInset(contGo.GetComponent<RectTransform>(),
            new Vector2(-30, 20), new Vector2(40, 40));

        // Accept / Reject buttons (hidden until end-of-line + hasChoice)
        GameObject acceptGo = CreateChoiceButton("AcceptButton", boxGo.transform,
            "Ya", new Vector2(-260, 30), new Color(0.30f, 0.65f, 0.30f));
        GameObject rejectGo = CreateChoiceButton("RejectButton", boxGo.transform,
            "Tidak", new Vector2(-50, 30), new Color(0.75f, 0.30f, 0.30f));

        // ---- Wire up the manager ----
        VNDialogueManager mgr = canvasGo.AddComponent<VNDialogueManager>();
        mgr.vnRoot = rootGo;
        mgr.backgroundImage = bg;
        mgr.playerPortraitImage = playerImg;
        mgr.npcPortraitImage = npcImg;
        mgr.playerPortraitLabel = playerLabel;
        mgr.npcPortraitLabel = npcLabel;
        mgr.dialogueBox = boxGo;
        mgr.dialogueBoxImage = boxImg;
        mgr.speakerNameText = nameTmp;
        mgr.dialogueText = textTmp;
        mgr.continueIndicator = contGo;
        mgr.acceptButton = acceptGo.GetComponent<Button>();
        mgr.rejectButton = rejectGo.GetComponent<Button>();
        mgr.acceptButtonText = acceptGo.GetComponentInChildren<TextMeshProUGUI>();
        mgr.rejectButtonText = rejectGo.GetComponentInChildren<TextMeshProUGUI>();

        // VN_Root starts disabled (manager turns it on when dialogue begins)
        rootGo.SetActive(false);

        // Make sure there's an EventSystem in the scene (buttons need it)
        EnsureEventSystem();

        Selection.activeGameObject = canvasGo;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[VN System] VN_Canvas built and wired up. " +
                  "Add a VNNPCInteractable to your receptionist NPC and assign a VNDialogueData.");
    }

    // ------------------------------------------------------------------
    // 1b. Patch the existing VN_Canvas in the active scene
    //      - wires up playerPortraitLabel / npcPortraitLabel / dialogueBoxImage
    //        if you built the canvas before those fields existed
    //      - guarantees the dialogue box has a visible dark background
    // ------------------------------------------------------------------
    [MenuItem("Tools/VN System (Topic 1)/Patch Existing VN Canvas in Active Scene")]
    public static void PatchExistingCanvas()
    {
        VNDialogueManager mgr = Object.FindObjectOfType<VNDialogueManager>();
        if (mgr == null)
        {
            Debug.LogWarning("[VN System] No VNDialogueManager found in the active scene. " +
                "Run 'Create VN Canvas in Active Scene' first.");
            return;
        }

        int patches = 0;

        // Wire portrait labels (they should be children of the portrait images, named "Label")
        if (mgr.playerPortraitLabel == null && mgr.playerPortraitImage != null)
        {
            Transform t = mgr.playerPortraitImage.transform.Find("Label");
            if (t != null) { mgr.playerPortraitLabel = t.gameObject; patches++; }
        }
        if (mgr.npcPortraitLabel == null && mgr.npcPortraitImage != null)
        {
            Transform t = mgr.npcPortraitImage.transform.Find("Label");
            if (t != null) { mgr.npcPortraitLabel = t.gameObject; patches++; }
        }

        // Wire dialogueBoxImage from the dialogueBox
        if (mgr.dialogueBoxImage == null && mgr.dialogueBox != null)
        {
            Image boxImg = mgr.dialogueBox.GetComponent<Image>();
            if (boxImg != null)
            {
                mgr.dialogueBoxImage = boxImg;
                patches++;

                // Force a strong dark background so it's always readable
                if (boxImg.sprite == null)
                    boxImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                boxImg.color = new Color(0f, 0f, 0f, 0.88f);
            }
        }

        EditorUtility.SetDirty(mgr);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[VN System] Patched VN_Canvas — " + patches + " field(s) wired up. " +
            "Save the scene (Ctrl+S).");
    }

    // ------------------------------------------------------------------
    // 2. Create a sample receptionist VNDialogueData asset
    // ------------------------------------------------------------------
    [MenuItem("Tools/VN System (Topic 1)/Create Sample Receptionist Dialogue")]
    public static void CreateSampleDialogue()
    {
        VNDialogueData data = ScriptableObject.CreateInstance<VNDialogueData>();
        data.playerName = "You";
        data.npcName = "Receptionist";
        data.actionID = "VN_Receptionist_Topic1";
        data.hasChoice = false;
        data.acceptText = "Ya";
        data.rejectText = "Tidak";

        data.lines = new VNLine[]
        {
            NewLine(VNSpeaker.NPC,    "Selamat datang di kantor! Apakah ini hari pertamamu?"),
            NewLine(VNSpeaker.Player, "Iya, hari pertama. Saya masih bingung harus mulai dari mana."),
            NewLine(VNSpeaker.NPC,    "Tenang, saya bantu. Meja kerjamu ada di lantai 2, sebelah kiri tangga."),
            NewLine(VNSpeaker.NPC,    "Sebelum mulai — jangan asal klik link di email atau pasang aplikasi sembarangan, ya."),
            NewLine(VNSpeaker.Player, "Baik, saya akan hati-hati."),
            NewLine(VNSpeaker.NPC,    "Bagus. Selamat bekerja!"),
        };

        // Make sure the destination folder exists
        string folder = Path.GetDirectoryName(SAMPLE_DIALOGUE_PATH).Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string path = AssetDatabase.GenerateUniqueAssetPath(SAMPLE_DIALOGUE_PATH);
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = data;

        Debug.Log("[VN System] Sample receptionist dialogue created at: " + path);
    }

    // ------------------------------------------------------------------
    // 3. Create sample NPC Talking dialogue (2 NPC, semua mood tersedia)
    // ------------------------------------------------------------------
    [MenuItem("Tools/VN System (Topic 1)/Create Sample NPC Talking Dialogue")]
    public static void CreateTalkingDialogue()
    {
        VNDialogueData data = ScriptableObject.CreateInstance<VNDialogueData>();

        // --- Identitas karakter (sprite diisi manual di Inspector) ---
        data.playerName  = "Kamu";
        data.npcName     = "Rio";    // NPC Talking 1 — slot kanan, speaker = NPC
        data.npc2Name    = "Bagas";  // NPC Talking 2 — slot kanan (swap), speaker = NPC2
        data.actionID    = "NPC_Talking1and2";
        data.hasChoice   = false;
        data.acceptText  = "Ya";
        data.rejectText  = "Tidak";

        data.lines = new VNLine[]
        {
            NewLine(VNSpeaker.NPC,    VNExpression.Default,  "Eh, kamu karyawan baru ya? Semangat pagi! Kok kayak kurang tidur gitu?"),
            NewLine(VNSpeaker.Player, VNExpression.Default,  "Hehe iya, masih adaptasi. Tadi juga nyasar dulu di lorong."),
            NewLine(VNSpeaker.NPC2,   VNExpression.Smiling,  "Sini duduk dulu, minum kopi bro — biar melek!"),
            NewLine(VNSpeaker.Player, VNExpression.Smiling,  "Wah makasih banyak!"),
            NewLine(VNSpeaker.NPC,    VNExpression.Thinking, "Ngomong-ngomong, kamu udah denger belum? Kemarin ada kasus phishing di sini — ada orang nyamar jadi IT support."),
            NewLine(VNSpeaker.Player, VNExpression.Default,  "Serius? Terus gimana ngatasinnya?"),
            NewLine(VNSpeaker.NPC2,   VNExpression.Thinking, "Makanya kalau ada yang ngaku IT support dan minta file, verifikasi dulu — pastiin dia beneran karyawan sini."),
            NewLine(VNSpeaker.NPC,    VNExpression.Default,  "Iya, jangan langsung kasih file ke siapapun sebelum kamu yakin identitasnya."),
            NewLine(VNSpeaker.NPC2,   VNExpression.Default,  "Satu lagi — jangan install aplikasi sembarangan di laptop kantor, apalagi yang gaada lisensinya."),
            NewLine(VNSpeaker.Player, VNExpression.Smiling,  "Oke, sip! Makasih infonya Rio, Bagas. Berguna banget nih buat hari pertama."),
            NewLine(VNSpeaker.NPC,    VNExpression.Smiling,  "Sama-sama! Selamat kerja ya, stay safe!"),
        };

        string folder = Path.GetDirectoryName(TALKING_DIALOGUE_PATH).Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(folder))
            Directory.CreateDirectory(folder);

        string path = AssetDatabase.GenerateUniqueAssetPath(TALKING_DIALOGUE_PATH);
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = data;

        Debug.Log("[VN System] NPC Talking dialogue created at: " + path +
                  "\nTinggal isi sprite Background, PlayerPortrait, NpcPortrait (NPC1), Npc2Portrait (NPC2) di Inspector.");
    }

    // ------------------------------------------------------------------
    // helpers
    // ------------------------------------------------------------------
    private static VNLine NewLine(VNSpeaker speaker, string text)
    {
        return new VNLine { speaker = speaker, text = text, mood = VNExpression.Default };
    }

    private static VNLine NewLine(VNSpeaker speaker, VNExpression mood, string text)
    {
        return new VNLine { speaker = speaker, mood = mood, text = text };
    }

    private static GameObject NewUI(string name, Transform parent, bool withImage = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        if (withImage) go.AddComponent<Image>();
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void StretchInset(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static void AnchorBottomLeftPortrait(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(60, 240);
        rt.sizeDelta = new Vector2(520, 760);
    }

    private static void AnchorBottomRightPortrait(RectTransform rt)
    {
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-60, 240);
        rt.sizeDelta = new Vector2(520, 760);
    }

    private static void AnchorBottomBox(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(40, 30);
        rt.offsetMax = new Vector2(-40, 220 + 30); // 220px tall, 30px from bottom
    }

    private static void AnchorTopLeftInset(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    private static void AnchorBottomRightInset(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    private static GameObject CreateChoiceButton(string name, Transform parent,
        string label, Vector2 anchoredPos, Color tint)
    {
        GameObject go = NewUI(name, parent, true);
        Image img = go.GetComponent<Image>();
        img.sprite = GetBuiltinUISprite();
        img.color = tint;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(200, 60);

        GameObject txtGo = NewUI("Text", go.transform);
        TextMeshProUGUI tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        StretchFull(txtGo.GetComponent<RectTransform>());

        return go;
    }

    private static GameObject AddPortraitLabel(Transform parent, string label)
    {
        GameObject txtGo = NewUI("Label", parent);
        TextMeshProUGUI tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 48;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 1f, 1f, 0.85f);
        tmp.alignment = TextAlignmentOptions.Center;
        StretchFull(txtGo.GetComponent<RectTransform>());
        return txtGo;
    }

    /// <summary>
    /// Returns Unity's built-in 'UISprite' (default UI rounded box).
    /// Falls back to 'Background' if needed. No external assets required.
    /// </summary>
    private static Sprite GetBuiltinUISprite()
    {
        Sprite s = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (s == null)
            s = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        return s;
    }

    private static void EnsureEventSystem()
    {
        var existing = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (existing != null) return;

        GameObject es = new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));
        Debug.Log("[VN System] No EventSystem found — created one.");
    }
}
