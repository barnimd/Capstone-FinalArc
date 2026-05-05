using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GameTopic5SceneSetup
{
    [MenuItem("Game/Setup Topic 5 - Computer Interaction Scene")]
    public static void SetupScene()
    {
        Debug.Log("=== Memulai Setup Topic 5 Scene ===");

        FixAllCanvasScales();
        CleanupOldTopic4Scripts();
        SetupWifiSelector();
        SetupWebsiteSecurity();
        SetupGameManager();
        EnsureScoreManager();
        WireObjectiveUI();

        Debug.Log("=== Setup Topic 5 Scene SELESAI! ===");
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Topic 5 Setup",
            "Scene Computer_Interaction siap!\n\nTidak perlu asset tambahan. Play langsung.", "OK");
    }

    private static void FixAllCanvasScales()
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
        {
            if (c.transform.localScale == Vector3.zero)
            {
                c.transform.localScale = Vector3.one;
                Debug.Log($"[Setup] {c.name} LocalScale (0,0,0) -> (1,1,1).");
            }
        }
    }

    private static void CleanupOldTopic4Scripts()
    {
        Canvas desktopCanvas = FindCanvas("desktop");
        if (desktopCanvas == null) return;

        Transform panel = desktopCanvas.transform.Find("Panel");
        if (panel == null) return;

        DashboardPopupController old = panel.GetComponent<DashboardPopupController>();
        if (old != null)
        {
            Object.DestroyImmediate(old);
            Debug.Log("[Setup] DashboardPopupController lama dihapus dari Desktop Panel.");
        }
    }

    // ===================== GAME MANAGER =====================

    private static void SetupGameManager()
    {
        GameManager_Tp5 gm = Object.FindObjectOfType<GameManager_Tp5>(true);
        if (gm == null)
            gm = new GameObject("GameManager_Tp5").AddComponent<GameManager_Tp5>();

        gm.desktopCanvas = FindCanvas("desktop")?.gameObject;
        gm.websiteCanvas = FindCanvas("website")?.gameObject;
        gm.objectiveUI = Object.FindObjectOfType<ObjectiveUI_Tp4>(true);
        gm.wifiController = Object.FindObjectOfType<WifiSelectorController>(true);
        gm.websiteController = Object.FindObjectOfType<WebsiteSecurityController>(true);

        Debug.Log("[Setup] GameManager_Tp5 wired.");
    }

    // ===================== WIFI SELECTOR =====================

    private static void SetupWifiSelector()
    {
        Canvas desktopCanvas = FindCanvas("desktop");
        if (desktopCanvas == null) { Debug.LogError("[Setup] DesktopCanvas tidak ditemukan!"); return; }

        Transform panel = desktopCanvas.transform.Find("Panel");
        if (panel == null) { Debug.LogError("[Setup] Panel di DesktopCanvas tidak ditemukan!"); return; }

        Transform popupListWifi = panel.Find("PopupListWifi");
        if (popupListWifi == null) { Debug.LogError("[Setup] PopupListWifi tidak ditemukan!"); return; }

        WifiSelectorController ctrl = popupListWifi.GetComponent<WifiSelectorController>();
        if (ctrl == null) ctrl = popupListWifi.gameObject.AddComponent<WifiSelectorController>();

        Transform popup = panel.Find("PopupPanel");
        if (popup != null)
        {
            ctrl.popupPanel = popup.gameObject;
            Transform row = popup.Find("ActionButtonRow");
            if (row != null)
                ctrl.btnClose = row.Find("BtnAction")?.GetComponent<Button>();
        }

        ctrl.popupListWifi = popupListWifi.gameObject;

        ctrl.txtListWifiDetail_0 = popupListWifi.Find("Wifi_Item0/text")?.GetComponent<TMP_Text>();
        ctrl.txtListWifiDetail_1 = popupListWifi.Find("Wifi_Item1/text")?.GetComponent<TMP_Text>();
        ctrl.txtListWifiDetail_2 = popupListWifi.Find("Wifi_Item2/text")?.GetComponent<TMP_Text>();

        Transform existingConnect = popupListWifi.Find("BtnConnect");
        if (existingConnect != null)
        {
            ctrl.btnConnect = existingConnect.GetComponent<Button>();
        }
        else
        {
            GameObject connectGO = new GameObject("BtnConnect", typeof(RectTransform), typeof(Image), typeof(Button));
            connectGO.transform.SetParent(popupListWifi, false);
            RectTransform crt = connectGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0); crt.anchorMax = new Vector2(0.5f, 0);
            crt.anchoredPosition = new Vector2(0, 40);
            crt.sizeDelta = new Vector2(180, 36);
            connectGO.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);

            GameObject clbl = new GameObject("Label", typeof(RectTransform));
            clbl.transform.SetParent(connectGO.transform, false);
            RectTransform lrt = clbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
            TMP_Text lt = clbl.AddComponent<TextMeshProUGUI>();
            lt.text = "Connect"; lt.fontSize = 16; lt.alignment = TextAlignmentOptions.Center;
            lt.color = Color.white; lt.fontStyle = FontStyles.Bold;

            ctrl.btnConnect = connectGO.GetComponent<Button>();
            Debug.Log("[Setup] BtnConnect dibuat di PopupListWifi.");
        }

        Transform confirm = popupListWifi.Find("ConfirmationPanel");
        if (confirm != null)
        {
            ctrl.confirmationPanel = confirm.gameObject;
            ctrl.btnAccept = confirm.Find("AcceptButton")?.GetComponent<Button>();
            ctrl.btnCancel = confirm.Find("CancelButton")?.GetComponent<Button>();
        }

        GameObject feedbackGO = GameObject.Find("FeedbackPanel");
        if (feedbackGO != null)
        {
            Transform fb = feedbackGO.transform;
            ctrl.feedbackPanel = fb.gameObject;
            ctrl.txtFeedbackTitle = fb.Find("txtFeedbackTitle")?.GetComponent<TMP_Text>();
            ctrl.txtFeedbackDetail = fb.Find("txtFeedbackDetail")?.GetComponent<TMP_Text>();
            ctrl.btnFeedbackOk = fb.Find("BtnFeedbackOk")?.GetComponent<Button>();
            Debug.Log("[Setup] FeedbackPanel wired ke WifiSelectorController.");
        }

        Debug.Log("[Setup] WifiSelectorController wired.");
    }

    // ===================== WEBSITE SECURITY =====================

    private static void SetupWebsiteSecurity()
    {
        Canvas websiteCanvas = FindCanvas("website");
        if (websiteCanvas == null) { Debug.LogError("[Setup] WebsiteCanvas tidak ditemukan!"); return; }

        Transform websitePanel = websiteCanvas.transform.Find("WebsitePanel");
        if (websitePanel == null) { Debug.LogError("[Setup] WebsitePanel tidak ditemukan!"); return; }

        WebsiteSecurityController ctrl = websitePanel.GetComponent<WebsiteSecurityController>();
        if (ctrl == null) ctrl = websitePanel.gameObject.AddComponent<WebsiteSecurityController>();

        // --- PopupPanel "not secure" (direct child of WebsitePanel) ---
        Transform popup = websitePanel.Find("PopupPanel");
        if (popup != null)
        {
            ctrl.popupPanel = popup.gameObject;
            Transform row = popup.Find("ActionButtonRow");
            if (row != null)
            {
                ctrl.btnActivate = row.Find("BtnActionB")?.GetComponent<Button>();
                ctrl.btnClose = row.Find("BtnActionA")?.GetComponent<Button>();
            }
        }

        // --- PopupToggleVpn (child of WebsitePanel) ---
        Transform vpnPopup = websitePanel.Find("PopupToggleVpn");
        if (vpnPopup != null)
        {
            ctrl.popupToggleVpn = vpnPopup.gameObject;
            ctrl.toggleVPN = vpnPopup.Find("Toggle")?.GetComponent<Toggle>();
            ctrl.txtPopupStatus = vpnPopup.Find("txtPopupStatus")?.GetComponent<TMP_Text>();

            Transform abRow = vpnPopup.Find("ActionButtonRow");
            if (abRow != null)
                ctrl.btnConfirmActivate = abRow.Find("BtnAction")?.GetComponent<Button>();
        }

        // --- LoginPanel (under CardPanel) ---
        Transform loginPanel = websitePanel.Find("CardPanel/LoginPanel");
        if (loginPanel != null)
        {
            ctrl.loginPanel = loginPanel.gameObject;
            ctrl.btnLogin = loginPanel.Find("BtnLogin")?.GetComponent<Button>();
            ctrl.inputUsername = DeepFindInput(loginPanel, "UsernamePanel");
            ctrl.inputPassword = DeepFindInput(loginPanel, "PasswordPanel");
        }
        else
        {
            loginPanel = websitePanel.Find("LoginPanel");
            if (loginPanel != null)
            {
                ctrl.loginPanel = loginPanel.gameObject;
                ctrl.btnLogin = loginPanel.Find("BtnLogin")?.GetComponent<Button>();
                ctrl.inputUsername = DeepFindInput(loginPanel, "UsernamePanel");
                ctrl.inputPassword = DeepFindInput(loginPanel, "PasswordPanel");
            }
        }

        // --- Success, Attack, Hack Panels ---
        ctrl.successPanel = websitePanel.Find("SuccessPanel")?.gameObject;
        ctrl.attackPanel = websitePanel.Find("AttackPanel")?.gameObject;
        ctrl.instantHackPanel = websitePanel.Find("InstantHackPanel")?.gameObject;

        // --- FeedbackPanel (di ROOT scene, bukan child WebsitePanel) ---
        GameObject feedbackGO = GameObject.Find("FeedbackPanel");
        if (feedbackGO != null)
        {
            Transform fb = feedbackGO.transform;
            ctrl.feedbackPanel = fb.gameObject;
            ctrl.txtFeedbackTitle = fb.Find("txtFeedbackTitle")?.GetComponent<TMP_Text>();
            ctrl.txtFeedbackDetail = fb.Find("txtFeedbackDetail")?.GetComponent<TMP_Text>();
            ctrl.btnFeedbackOk = fb.Find("BtnFeedbackOk")?.GetComponent<Button>();
            Debug.Log("[Setup] FeedbackPanel (root) wired ke WebsiteSecurityController.");
        }

        Debug.Log("[Setup] WebsiteSecurityController wired.");
    }

    private static TMP_InputField DeepFindInput(Transform parent, string panelName)
    {
        Transform panel = parent.Find(panelName);
        if (panel == null) return null;

        TMP_InputField input = panel.GetComponent<TMP_InputField>();
        if (input != null) return input;

        input = panel.GetComponentInChildren<TMP_InputField>(true);
        return input;
    }

    // ===================== HELPERS =====================

    private static void WireObjectiveUI()
    {
        ObjectiveUI_Tp4 objUI = Object.FindObjectOfType<ObjectiveUI_Tp4>(true);
        if (objUI == null) return;

        GameManager_Tp5 gm = Object.FindObjectOfType<GameManager_Tp5>(true);
        if (gm != null) gm.objectiveUI = objUI;

        WifiSelectorController wc = Object.FindObjectOfType<WifiSelectorController>(true);
        if (wc != null) wc.objectiveUI = objUI;

        WebsiteSecurityController ws = Object.FindObjectOfType<WebsiteSecurityController>(true);
        if (ws != null) ws.objectiveUI = objUI;

        Debug.Log("[Setup] ObjectiveUI wired ke semua controller.");
    }

    private static Canvas FindCanvas(string partial)
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
            if (c.name.ToLower().Contains(partial.ToLower())) return c;
        return null;
    }

    private static void EnsureScoreManager()
    {
        if (GameObject.Find("ScoreManager") == null)
            new GameObject("ScoreManager").AddComponent<ScoreManager>();
    }
}
