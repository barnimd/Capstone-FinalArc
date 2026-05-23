using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GameTopic1PrivasiSetup
{
    [MenuItem("Game/Setup Topic 1 - Privasi Keamanan")]
    public static void SetupScene()
    {
        Debug.Log("=== Setup Topic 1 Privasi ===");

        FixCanvases();
        SetupInstallerCrash();
        WireGameFlowCrash();

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("=== Setup Topic 1 Privasi SELESAI ===");
    }

    static void FixCanvases()
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
            if (c.transform.localScale == Vector3.zero)
                c.transform.localScale = Vector3.one;
    }

    static void SetupInstallerCrash()
    {
        // Check if already wired
        var flow = Object.FindObjectOfType<InstallerFlow>(true);
        if (flow != null && flow.crash != null)
        {
            Debug.Log("[Setup] InstallerFlow.crash already wired. Skip.");
            return;
        }

        // Delete old if exists
        var old = GameObject.Find("CrashOverlayCanvas_Installer");
        if (old != null) Object.DestroyImmediate(old);

        var go = new GameObject("CrashOverlayCanvas_Installer",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var cv = go.GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        cv.overrideSorting = true;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(800, 600);
        go.SetActive(false);

        var bg = new GameObject("CrashOverlay", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.sizeDelta = brt.anchoredPosition = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0, 0, 0, 0.95f);

        var txt = new GameObject("CrashText", typeof(RectTransform));
        txt.transform.SetParent(bg.transform, false);
        var trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0, 60);
        trt.sizeDelta = new Vector2(500, 300);
        var ttmp = txt.AddComponent<TextMeshProUGUI>();
        ttmp.fontSize = 15;
        ttmp.alignment = TextAlignmentOptions.Left;

        var ctrl = go.AddComponent<CrashOverlayController_Tp1_Installer>();
        ctrl.crashCanvas = go;
        ctrl.mainBg = bg.GetComponent<Image>();
        ctrl.mainText = ttmp;

        if (flow != null)
        {
            flow.crash = ctrl;
            Debug.Log("[Setup] InstallerFlow.crash wired to CrashOverlayController_Tp1_Installer.");
        }
    }

    static void WireGameFlowCrash()
    {
        var gf = Object.FindObjectOfType<GameFlowManager>(true);
        if (gf == null || gf.crash != null) return;

        var existingCrash = Object.FindObjectOfType<CrashOverlayController_Tp1>(true);
        if (existingCrash != null)
        {
            gf.crash = existingCrash;
            Debug.Log("[Setup] GameFlowManager.crash wired to existing CrashOverlayController_Tp1.");
        }
    }
}
