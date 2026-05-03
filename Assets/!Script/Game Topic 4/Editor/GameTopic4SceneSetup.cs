using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GameTopic4SceneSetup
{
    [MenuItem("Game/Setup Topic 4 - Computer Interaction Scene")]
    public static void SetupScene()
    {
        Debug.Log("=== Memulai Setup Topic 4 Scene ===");

        FixURLInputField();
        FixWebsiteCanvasScale();
        SetupWebsiteLoginUI();
        SetupDashboardUI();
        SetupGameManager();
        WireChromeButton();
        EnsureScoreManager();

        Debug.Log("=== Setup Topic 4 Scene SELESAI! ===");
        EditorUtility.DisplayDialog("Topic 4 Setup", "Scene Computer_Interaction siap!\n\nSilakan:\n1. Buat URLData_Tp4 asset (Create > Game > URL Data Tp4)\n2. Buat PopupData_Tp4 asset (Create > Game > Popup Data Tp4)\n3. Assign kedua asset ke GameManager_Tp4 di Inspector\n4. Isi data URL dan Popup", "OK");
    }

    private static void SetupGameManager()
    {
        GameManager_Tp4 script = null;
        GameObject existing = GameObject.Find("GameManager_Tp4");
        if (existing != null)
        {
            script = existing.GetComponent<GameManager_Tp4>();
            Debug.Log("[Setup] GameManager_Tp4 sudah ada, update references.");
        }
        else
        {
            GameObject gm = new GameObject("GameManager_Tp4");
            script = gm.AddComponent<GameManager_Tp4>();
            Debug.Log("[Setup] GameManager_Tp4 dibuat.");
        }

        Canvas desktopCanvas = null;
        Canvas websiteCanvas = null;
        foreach (Canvas c in GameObject.FindObjectsOfType<Canvas>(true))
        {
            if (c.name.ToLower().Contains("website")) websiteCanvas = c;
            if (c.name.ToLower().Contains("desktop")) desktopCanvas = c;
        }

        script.desktopCanvas = desktopCanvas != null ? desktopCanvas.gameObject : null;
        script.websiteCanvas = websiteCanvas != null ? websiteCanvas.gameObject : null;

        Transform websitePanel = null;
        if (websiteCanvas != null) websitePanel = websiteCanvas.transform.Find("WebsitePanel");
        if (websitePanel != null)
            script.websiteController = websitePanel.GetComponent<WebsiteLoginController>();

        Transform desktopPanel = null;
        if (desktopCanvas != null) desktopPanel = desktopCanvas.transform.Find("Panel");
        if (desktopPanel != null)
            script.dashboardController = desktopPanel.GetComponent<DashboardPopupController>();

        if (desktopPanel != null)
        {
            DashboardPopupController dpc = desktopPanel.GetComponent<DashboardPopupController>();
            WebsiteLoginController wlc = null;
            if (websitePanel != null) wlc = websitePanel.GetComponent<WebsiteLoginController>();

            if (dpc != null && wlc != null)
            {
                dpc.popupData = script.popupData;
            }
        }

        Debug.Log("[Setup] GameManager_Tp4 references di-wire.");
    }

    private static void FixURLInputField()
    {
        TMP_InputField[] fields = GameObject.FindObjectsOfType<TMP_InputField>(true);
        foreach (var field in fields)
        {
            if (field.contentType == TMP_InputField.ContentType.Password)
            {
                field.contentType = TMP_InputField.ContentType.Standard;
                field.placeholder.GetComponent<TMP_Text>().text = "https://";
                Debug.Log($"[Setup] TMP_InputField '{field.name}' diubah dari Password ke Standard.");
            }
        }
    }

    private static void FixWebsiteCanvasScale()
    {
        Canvas websiteCanvas = null;
        foreach (Canvas c in GameObject.FindObjectsOfType<Canvas>(true))
        {
            if (c.name.ToLower().Contains("website")) { websiteCanvas = c; break; }
        }
        if (websiteCanvas == null) return;

        Transform t = websiteCanvas.transform;
        if (t.localScale == Vector3.zero)
        {
            t.localScale = Vector3.one;
            Debug.Log("[Setup] WebsiteCanvas LocalScale diubah dari (0,0,0) ke (1,1,1).");
        }
    }

    // ===================== WEBSITE =====================
    private static void SetupWebsiteLoginUI()
    {
        Canvas websiteCanvas = null;
        foreach (Canvas c in GameObject.FindObjectsOfType<Canvas>(true))
        {
            if (c.name.ToLower().Contains("website")) { websiteCanvas = c; break; }
        }
        if (websiteCanvas == null)
        {
            Debug.LogError("[Setup] WebsiteCanvas tidak ditemukan!");
            return;
        }

        Transform websitePanel = websiteCanvas.transform.Find("WebsitePanel");
        if (websitePanel == null)
        {
            Debug.LogError("[Setup] WebsitePanel tidak ditemukan!");
            return;
        }

        Transform cardPanel = websitePanel.Find("CardPanel");
        Transform loginPanel = cardPanel?.Find("LoginPanel");

        WebsiteLoginController controller = websitePanel.GetComponent<WebsiteLoginController>();
        if (controller == null)
            controller = websitePanel.gameObject.AddComponent<WebsiteLoginController>();

        controller.websitePanelRoot = websitePanel.gameObject;

        ConvertToInputField(loginPanel?.Find("UsernamePanel"), "Username");
        ConvertToInputField(loginPanel?.Find("PasswordPanel"), "Password");

        Debug.Log("[Setup] WebsiteLoginUI siap.");
    }

    // ===================== DASHBOARD =====================
    private static void SetupDashboardUI()
    {
        Canvas desktopCanvas = GameObject.FindObjectOfType<Canvas>(true);
        if (desktopCanvas == null) return;

        Transform desktopPanel = desktopCanvas.transform.Find("Panel");
        if (desktopPanel == null) return;

        if (desktopPanel.GetComponent<DashboardPopupController>() != null) return;

        desktopPanel.gameObject.AddComponent<DashboardPopupController>();
        Debug.Log("[Setup] DashboardPopupController ditambahkan.");
    }

    // ===================== BUTTON =====================
    private static void WireChromeButton()
    {
        Canvas desktopCanvas = GameObject.FindObjectOfType<Canvas>(true);
        if (desktopCanvas == null) return;

        GameManager_Tp4 gm = GameObject.FindObjectOfType<GameManager_Tp4>();
        if (gm == null) return;

        foreach (Button btn in desktopCanvas.GetComponentsInChildren<Button>(true))
        {
            if (btn.name.ToLower().Contains("chrome"))
            {
                btn.onClick.RemoveAllListeners();
                UnityEventTools.AddPersistentListener(btn.onClick, gm.OnBrowserIconClicked);
            }
        }
    }

    private static void EnsureScoreManager()
    {
        if (GameObject.Find("ScoreManager") == null)
        {
            new GameObject("ScoreManager").AddComponent<ScoreManager>();
        }
    }

    // ===================== UTIL =====================
    private static void ConvertToInputField(Transform panel, string placeholder)
    {
        if (panel == null) return;

        TMP_InputField input = panel.GetComponent<TMP_InputField>();
        if (input == null)
            input = panel.gameObject.AddComponent<TMP_InputField>();

        input.contentType = placeholder == "Password"
            ? TMP_InputField.ContentType.Password
            : TMP_InputField.ContentType.Standard;
    }
}