using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class ClassPageSetup
{
    [MenuItem("SECMIND/Setup Class Page")]
    public static void Setup()
    {
        GameObject contentContainerGO = GameObject.Find("ContentContainer");
        if (contentContainerGO == null) { Debug.LogError("ContentContainer not found!"); return; }

        // Fix ContentContainer RectTransform for vertical scrolling
        RectTransform contentRT = contentContainerGO.GetComponent<RectTransform>();
        if (contentRT != null)
        {
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0, 0);
        }

        Color gold = new Color(0.784f, 0.439f, 0.063f, 1f);
        Color unlockedCardBg = new Color(0.961f, 0.941f, 0.878f, 1f);
        Color headerColor = Color.white;

        // UNLOCKED SECTION
        GameObject unlockedSection = MakePanel("UnlockedSection", contentContainerGO.transform);
        AddVLG(unlockedSection, 12);
        AddCSF(unlockedSection);

        GameObject headerU = MakeText("HeaderUnlocked", unlockedSection.transform, "Unlocked", 22f, headerColor);
        SetRTHeight(headerU, 40);

        GameObject unlockedGrid = MakePanel("UnlockedGrid", unlockedSection.transform);
        AddGrid(unlockedGrid, 3, new Vector2(200, 220), new Vector2(16, 16));
        AddCSF(unlockedGrid);

        // LOCKED SECTION
        GameObject lockedSection = MakePanel("LockedSection", contentContainerGO.transform);
        AddVLG(lockedSection, 12);
        AddCSF(lockedSection);

        GameObject headerL = MakeText("HeaderLocked", lockedSection.transform, "Locked", 22f, headerColor);
        SetRTHeight(headerL, 40);

        GameObject lockedGrid = MakePanel("LockedGrid", lockedSection.transform);
        AddGrid(lockedGrid, 3, new Vector2(200, 220), new Vector2(16, 16));
        AddCSF(lockedGrid);

        // LESSON CARD PREFAB
        string prefabPath = "Assets/Prefab/LessonCard.prefab";
        GameObject cardObj = BuildCard(gold, unlockedCardBg);
        PrefabUtility.SaveAsPrefabAsset(cardObj, prefabPath);
        Object.DestroyImmediate(cardObj);
        Debug.Log("LessonCard prefab saved to " + prefabPath);

        // CLASSPAGE MANAGER
        GameObject mgrGO = new GameObject("ClassPageManager");
        ClassPageManager mgr = mgrGO.AddComponent<ClassPageManager>();
        SerializedObject so = new SerializedObject(mgr);
        so.FindProperty("unlockedGrid").objectReferenceValue = unlockedGrid.transform;
        so.FindProperty("lockedGrid").objectReferenceValue = lockedGrid.transform;
        GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (cardPrefab != null)
            so.FindProperty("lessonCardPrefab").objectReferenceValue = cardPrefab;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(contentContainerGO.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Class Page setup complete!");
    }

    static GameObject MakePanel(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject MakeText(string name, Transform parent, string text, float size, Color color)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 40);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return go;
    }

    static void SetRTHeight(GameObject go, float h)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
    }

    static void AddVLG(GameObject go, float spacing)
    {
        VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(0, 0, 0, 0);
    }

    static void AddCSF(GameObject go)
    {
        ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    static void AddGrid(GameObject go, int cols, Vector2 cellSize, Vector2 spacing)
    {
        GridLayoutGroup glg = go.AddComponent<GridLayoutGroup>();
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = cols;
        glg.cellSize = cellSize;
        glg.spacing = spacing;
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.padding = new RectOffset(0, 0, 0, 0);
    }

    static GameObject BuildCard(Color gold, Color cardBg)
    {
        GameObject root = new GameObject("LessonCard");
        root.layer = 5;
        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(200, 220);
        Image rootImg = root.AddComponent<Image>();
        rootImg.color = cardBg;
        Button btn = root.AddComponent<Button>();
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = cardBg;
        cb.highlightedColor = new Color(0.9f, 0.85f, 0.75f, 1f);
        cb.pressedColor = new Color(0.8f, 0.75f, 0.65f, 1f);
        btn.colors = cb;
        btn.targetGraphic = rootImg;

        // Number Badge
        GameObject badge = MakeChild("NumberBadge", root.transform);
        RectTransform badgeRT = badge.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0, 1);
        badgeRT.anchorMax = new Vector2(0, 1);
        badgeRT.pivot = new Vector2(0, 1);
        badgeRT.anchoredPosition = new Vector2(12, -12);
        badgeRT.sizeDelta = new Vector2(48, 48);
        badge.AddComponent<Image>().color = gold;

        GameObject numTextGO = MakeChild("NumberText", badge.transform);
        RectTransform ntRT = numTextGO.GetComponent<RectTransform>();
        ntRT.anchorMin = Vector2.zero;
        ntRT.anchorMax = Vector2.one;
        ntRT.sizeDelta = Vector2.zero;
        ntRT.anchoredPosition = Vector2.zero;
        TextMeshProUGUI nt = numTextGO.AddComponent<TextMeshProUGUI>();
        nt.text = "01";
        nt.fontSize = 18;
        nt.fontStyle = FontStyles.Bold;
        nt.color = Color.white;
        nt.alignment = TextAlignmentOptions.Center;

        // Icon
        GameObject iconGO = MakeChild("IconImage", root.transform);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.pivot = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = new Vector2(0, 20);
        iconRT.sizeDelta = new Vector2(80, 80);
        iconGO.AddComponent<Image>().color = new Color(gold.r, gold.g, gold.b, 0.7f);

        // Title
        GameObject titleGO = MakeChild("TitleText", root.transform);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(1, 0);
        titleRT.pivot = new Vector2(0.5f, 0);
        titleRT.anchoredPosition = new Vector2(0, 16);
        titleRT.sizeDelta = new Vector2(-24, 50);
        TextMeshProUGUI tt = titleGO.AddComponent<TextMeshProUGUI>();
        tt.text = "Lesson Title";
        tt.fontSize = 14;
        tt.fontStyle = FontStyles.Bold;
        tt.color = new Color(0.15f, 0.10f, 0.05f, 1f);
        tt.alignment = TextAlignmentOptions.Center;

        // Lock overlay
        GameObject overlay = MakeChild("LockOverlay", root.transform);
        RectTransform overlayRT = overlay.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;
        overlayRT.anchoredPosition = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        overlay.SetActive(false);

        GameObject lockIcon = MakeChild("LockIcon", overlay.transform);
        RectTransform liRT = lockIcon.GetComponent<RectTransform>();
        liRT.anchorMin = new Vector2(0.5f, 0.5f);
        liRT.anchorMax = new Vector2(0.5f, 0.5f);
        liRT.pivot = new Vector2(0.5f, 0.5f);
        liRT.anchoredPosition = Vector2.zero;
        liRT.sizeDelta = new Vector2(60, 60);
        lockIcon.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);

        root.AddComponent<LessonCard>();
        return root;
    }

    static GameObject MakeChild(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.layer = 5;
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    [MenuItem("SECMIND/Wire Lessons to ClassPageManager")]
    public static void WireLessons()
    {
        ClassPageManager mgr = Object.FindObjectOfType<ClassPageManager>();
        if (mgr == null) { Debug.LogError("ClassPageManager not found in scene!"); return; }

        string[] guids = AssetDatabase.FindAssets("t:LessonData", new[] { "Assets/Resources/Lessons" });
        LessonData[] lessons = new LessonData[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            lessons[i] = AssetDatabase.LoadAssetAtPath<LessonData>(path);
        }

        // Sort by asset name so order is deterministic
        System.Array.Sort(lessons, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        SerializedObject so = new SerializedObject(mgr);
        SerializedProperty lessonsProp = so.FindProperty("lessons");
        lessonsProp.arraySize = lessons.Length;
        for (int i = 0; i < lessons.Length; i++)
            lessonsProp.GetArrayElementAtIndex(i).objectReferenceValue = lessons[i];
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(mgr.gameObject.scene);
        Debug.Log($"Wired {lessons.Length} LessonData assets into ClassPageManager.");
    }
}
