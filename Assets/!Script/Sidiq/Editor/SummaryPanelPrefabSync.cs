#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Makes every game scene use the SAME summary panel: an instance of
/// Assets/!Prefab/Canvas/CanvasSummaryPanel.prefab (the redesigned
/// "Level Complete!" layout built by <see cref="SummaryPanelRedesignSetup"/>).
///
/// Some scenes ended up with an unlinked hand-made copy of CanvasSummaryPanel,
/// so they kept showing the old bare panel while the rest of the game showed the
/// redesign. This replaces those copies with a real prefab instance and rewires
/// every scene reference that pointed at the old objects (matched by GameObject
/// path first, then by GameObject name) so nothing is left null.
/// </summary>
public static class SummaryPanelPrefabSync
{
    const string PrefabPath = "Assets/!Prefab/Canvas/CanvasSummaryPanel.prefab";

    static readonly string[] GameScenes =
    {
        "Assets/!Scenes/Game/Game Topic 1/Office_Level1_Prototype2.unity",
        "Assets/!Scenes/Game/Game Topic 1/Privasi_Keamanan.unity",
        "Assets/!Scenes/Game/Game Topic 2/Office_Environment.unity",
        "Assets/!Scenes/Game/Game Topic 3/Map_Topic_3.unity",
        "Assets/!Scenes/Game/Game Topic 4/Map_Topic4.unity",
        "Assets/!Scenes/Game/Game Topic 4/Computer_Interaction.unity",
        "Assets/!Scenes/Game/Game Topic 5/Map_Topic_5.unity",
        "Assets/!Scenes/Game/Game Topic 5/Computer_Interaction.unity",
        "Assets/!Scenes/Game/Game Topic 6/Map_Topic6.unity",
        "Assets/!Scenes/Game/Game Topic 6/computer_interaction.unity",
    };

    [MenuItem("Game/Audit Summary Panels (All Game Scenes)")]
    public static void Audit() { Run(false); }

    [MenuItem("Game/Sync Summary Panels to Prefab (All Game Scenes)")]
    public static void Sync() { Run(true); }

    static void Run(bool fix)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[SummaryPanelSync] Prefab not found at " + PrefabPath);
            return;
        }

        string reopen = EditorSceneManager.GetActiveScene().path;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        foreach (string scenePath in GameScenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                Debug.LogWarning("[SummaryPanelSync] Scene missing, skipped: " + scenePath);
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var panels = FindAllByName(scene, "CanvasSummaryPanel");
            Debug.Log($"[SummaryPanelSync] --- scanning {scene.name} ({panels.Count} panel(s)) ---");

            if (panels.Count == 0)
            {
                Debug.Log($"[SummaryPanelSync] {scene.name}: no CanvasSummaryPanel.");
                continue;
            }

            bool dirty = false;
            foreach (GameObject panel in panels)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(panel);
                bool linked = source != null &&
                              AssetDatabase.GetAssetPath(source) == PrefabPath;

                if (linked)
                {
                    Debug.Log($"[SummaryPanelSync] {scene.name}: '{Path(panel)}' already a prefab instance — OK.");
                    continue;
                }

                if (!fix)
                {
                    Debug.LogWarning($"[SummaryPanelSync] {scene.name}: '{Path(panel)}' is an UNLINKED copy (old design).");
                    continue;
                }

                ReplaceWithPrefabInstance(scene, panel, prefab);
                dirty = true;
            }

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[SummaryPanelSync] {scene.name}: SAVED.");
            }
        }

        if (!string.IsNullOrEmpty(reopen))
            EditorSceneManager.OpenScene(reopen, OpenSceneMode.Single);
    }

    // =====================================================================
    static void ReplaceWithPrefabInstance(Scene scene, GameObject oldPanel, GameObject prefab)
    {
        Transform parent = oldPanel.transform.parent;
        int siblingIndex = oldPanel.transform.GetSiblingIndex();
        bool wasActive = oldPanel.activeSelf;
        string name = oldPanel.name;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = name;
        if (parent != null)
            instance.transform.SetParent(parent, false);
        instance.transform.SetSiblingIndex(siblingIndex);
        instance.SetActive(wasActive);

        var remap = BuildRemap(oldPanel, instance);
        int rewired = RewireSceneReferences(scene, remap);

        Object.DestroyImmediate(oldPanel);

        Debug.Log($"[SummaryPanelSync] {scene.name}: replaced '{Path(instance)}' with prefab instance " +
                  $"(rewired {rewired} reference(s)).");
    }

    /// <summary>
    /// Maps every Object under the old panel (GameObjects + their Components) to the
    /// equivalent under the new prefab instance. Matches by relative transform path
    /// first, then falls back to GameObject name — the redesign moved e.g. ScoreText
    /// into a ScoreTile parent, so paths alone are not enough.
    /// </summary>
    static Dictionary<Object, Object> BuildRemap(GameObject oldRoot, GameObject newRoot)
    {
        var map = new Dictionary<Object, Object> { { oldRoot, newRoot } };

        var newByPath = new Dictionary<string, GameObject>();
        var newByName = new Dictionary<string, GameObject>();
        foreach (Transform t in newRoot.GetComponentsInChildren<Transform>(true))
        {
            newByPath[RelativePath(newRoot.transform, t)] = t.gameObject;
            if (!newByName.ContainsKey(t.gameObject.name))
                newByName[t.gameObject.name] = t.gameObject;
        }

        foreach (Transform t in oldRoot.GetComponentsInChildren<Transform>(true))
        {
            GameObject oldGo = t.gameObject;
            string path = RelativePath(oldRoot.transform, t);

            if (!newByPath.TryGetValue(path, out GameObject newGo))
                newByName.TryGetValue(oldGo.name, out newGo);
            if (newGo == null)
                continue;

            map[oldGo] = newGo;
            MapComponents(oldGo, newGo, map);
        }

        return map;
    }

    static void MapComponents(GameObject oldGo, GameObject newGo, Dictionary<Object, Object> map)
    {
        foreach (Component oldComp in oldGo.GetComponents<Component>())
        {
            if (oldComp == null) continue;

            // Match on the closest assignable component so TMP_Text fields land on
            // TextMeshProUGUI, Image fields on MPImage, and so on.
            Component newComp = newGo.GetComponents<Component>()
                .FirstOrDefault(c => c != null && oldComp.GetType().IsInstanceOfType(c));

            if (newComp != null)
                map[oldComp] = newComp;
        }
    }

    static int RewireSceneReferences(Scene scene, Dictionary<Object, Object> map)
    {
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Component comp in root.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;

                var so = new SerializedObject(comp);
                SerializedProperty p = so.GetIterator();
                bool changed = false;

                while (p.NextVisible(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                    Object value = p.objectReferenceValue;
                    if (value == null) continue;
                    if (!map.TryGetValue(value, out Object replacement)) continue;

                    p.objectReferenceValue = replacement;
                    changed = true;
                    count++;
                }

                if (changed)
                    so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        return count;
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    static List<GameObject> FindAllByName(Scene scene, string name)
    {
        var found = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.name == name)
                    found.Add(t.gameObject);
            }
        }
        return found;
    }

    static string RelativePath(Transform root, Transform t)
    {
        if (t == root) return "";
        var parts = new List<string>();
        while (t != null && t != root)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    static string Path(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}
#endif
