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
        SetupObjectiveUI();
        SetupWebsiteLoginUI();
        SetupDashboardUI();
        SetupEvaluationUI();
        SetupGameManager();
        WireChromeButton();
        EnsureScoreManager();

        Debug.Log("=== Setup Topic 4 Scene SELESAI! ===");
        EditorUtility.DisplayDialog("Topic 4 Setup",
            "Scene Computer_Interaction siap!\n\n" +
            "Langkah berikutnya:\n" +
            "1. Buat URLData_Tp4   (Create > Game > URL Data Tp4) + isi URL\n" +
            "2. Buat PopupData_Tp4  (Create > Game > Popup Data Tp4) + isi Popup\n" +
            "3. Buat EvaluationData_Tp4 (Create > Game > Evaluation Data Tp4) + isi 5 soal\n" +
            "4. Assign ketiga asset ke GameManager_Tp4 di Inspector", "OK");
    }

    private static void FixURLInputField()
    {
        foreach (var field in Object.FindObjectsOfType<TMP_InputField>(true))
        {
            if (field.contentType == TMP_InputField.ContentType.Password)
            {
                field.contentType = TMP_InputField.ContentType.Standard;
                if (field.placeholder != null)
                    field.placeholder.GetComponent<TMP_Text>().text = "https://";
                Debug.Log($"[Setup] TMP_InputField '{field.name}' diubah ke Standard.");
            }
        }
    }

    private static void FixWebsiteCanvasScale()
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
        {
            if (!c.name.ToLower().Contains("website")) continue;
            if (c.transform.localScale == Vector3.zero)
            {
                c.transform.localScale = Vector3.one;
                Debug.Log("[Setup] WebsiteCanvas LocalScale diubah (0,0,0) -> (1,1,1).");
            }
            break;
        }
    }

    // ===================== OBJECTIVE =====================

    private static void SetupObjectiveUI()
    {
        Canvas objectiveCanvas = FindCanvas("objective");
        if (objectiveCanvas == null) { Debug.LogWarning("[Setup] ObjectiveCanvas tidak ditemukan."); return; }

        Transform panel = objectiveCanvas.transform.Find("ObjectivePanel");
        if (panel == null) { Debug.LogWarning("[Setup] ObjectivePanel tidak ditemukan."); return; }

        ObjectiveUI_Tp3 old = panel.GetComponent<ObjectiveUI_Tp3>();
        if (old != null)
        {
            TMP_Text textRef = old.objectiveText;
            Object.DestroyImmediate(old);
            ObjectiveUI_Tp4 obj = panel.gameObject.AddComponent<ObjectiveUI_Tp4>();
            obj.objectiveText = textRef;
            obj.panelRoot = panel.gameObject;
            obj.centerPosition = Vector2.zero;
            obj.cornerPosition = new Vector2(-10, -10);
            obj.centerSize = new Vector2(600, 100);
            obj.cornerSize = new Vector2(300, 50);
            Debug.Log("[Setup] ObjectiveUI_Tp3 diganti ke ObjectiveUI_Tp4.");
        }
        else if (panel.GetComponent<ObjectiveUI_Tp4>() == null)
        {
            ObjectiveUI_Tp4 obj = panel.gameObject.AddComponent<ObjectiveUI_Tp4>();
            Transform txt = panel.Find("ObjectiveText");
            if (txt != null) obj.objectiveText = txt.GetComponent<TMP_Text>();
            obj.panelRoot = panel.gameObject;
            obj.centerPosition = Vector2.zero;
            obj.cornerPosition = new Vector2(-10, -10);
            obj.centerSize = new Vector2(600, 100);
            obj.cornerSize = new Vector2(300, 50);
            Debug.Log("[Setup] ObjectiveUI_Tp4 terpasang.");
        }
        else
        {
            Debug.Log("[Setup] ObjectiveUI_Tp4 sudah ada.");
        }

        EditorUtility.SetDirty(panel.gameObject);
    }

    // ===================== WEBSITE =====================

    private static void SetupWebsiteLoginUI()
    {
        Canvas websiteCanvas = FindCanvas("website");
        if (websiteCanvas == null) { Debug.LogError("[Setup] WebsiteCanvas tidak ditemukan!"); return; }

        Transform websitePanel = websiteCanvas.transform.Find("WebsitePanel");
        if (websitePanel == null) { Debug.LogError("[Setup] WebsitePanel tidak ditemukan!"); return; }

        Transform cardPanel = websitePanel.Find("CardPanel");
        if (cardPanel == null) { Debug.LogError("[Setup] CardPanel tidak ditemukan!"); return; }

        Transform loginPanel = cardPanel.Find("LoginPanel");
        if (loginPanel == null) { Debug.LogError("[Setup] LoginPanel tidak ditemukan!"); return; }

        WebsiteLoginController ctrl = websitePanel.GetComponent<WebsiteLoginController>();
        if (ctrl == null) ctrl = websitePanel.gameObject.AddComponent<WebsiteLoginController>();
        ctrl.websitePanelRoot = websitePanel.gameObject;

        // URL text
        Transform navbar = websitePanel.Find("NavbarPanel");
        if (navbar != null)
        {
            Transform urlBg = navbar.Find("PasswordTextInput") ?? navbar.Find("PasswordHolder");
            if (urlBg != null) ctrl.urlBarBackground = urlBg.GetComponent<Image>();

            Transform existing = navbar.Find("txtURL");
            if (existing != null)
            {
                ctrl.txtURL = existing.GetComponent<TMP_Text>();
            }
            else
            {
                TMP_Text t = CreateTMP(navbar, "txtURL", "https://example.com/login", 18);
                RectTransform rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = new Vector2(15, 5);
                rt.offsetMax = new Vector2(-15, -5);
                ctrl.txtURL = t;
            }
        }

        // Round info
        Transform ri = websitePanel.Find("RoundInfo");
        if (ri != null) ctrl.txtRoundInfo = ri.GetComponent<TMP_Text>();

        // Convert panel to input fields
        ConvertToInputField(loginPanel.Find("UsernamePanel"), "Username");
        ConvertToInputField(loginPanel.Find("PasswordPanel"), "Password");

        Transform up = loginPanel.Find("UsernamePanel");
        Transform pp = loginPanel.Find("PasswordPanel");
        ctrl.inputUsername = up != null ? up.GetComponent<TMP_InputField>() : null;
        ctrl.inputPassword = pp != null ? pp.GetComponent<TMP_InputField>() : null;

        // Buttons
        Transform btnL = loginPanel.Find("BtnLogin");
        Transform btnC = loginPanel.Find("BtnCancel");
        if (btnL != null) { Button b = EnsureButton(btnL); b.onClick.RemoveAllListeners(); ctrl.btnLogin = b; }
        if (btnC != null) { Button b = EnsureButton(btnC); b.onClick.RemoveAllListeners(); ctrl.btnCancel = b; }

        // Hide sibling LoginForm
        Transform lf = cardPanel.Find("LoginForm");
        if (lf != null) lf.gameObject.SetActive(false);

        // Feedback panel
        Transform fb = websitePanel.Find("FeedbackPanel");
        if (fb != null)
        {
            ctrl.feedbackPanel = fb.gameObject;
            ctrl.txtFeedbackTitle = fb.Find("txtFeedbackTitle")?.GetComponent<TMP_Text>();
            ctrl.txtFeedbackDetail = fb.Find("txtFeedbackDetail")?.GetComponent<TMP_Text>();
            Transform ok = fb.Find("BtnFeedbackOk");
            if (ok != null) ctrl.btnFeedbackOk = ok.GetComponent<Button>();
        }
        else
        {
            GameObject fgo = new GameObject("FeedbackPanel", typeof(RectTransform), typeof(Image));
            fgo.transform.SetParent(websitePanel, false);
            RectTransform frt = fgo.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.sizeDelta = Vector2.zero; frt.anchoredPosition = Vector2.zero;
            fgo.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
            fgo.SetActive(false);
            ctrl.feedbackPanel = fgo;

            var vlg = fgo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(40, 40, 40, 40);
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            ctrl.txtFeedbackTitle = CreateTMP(fgo.transform, "txtFeedbackTitle", "GAME OVER!", 36);
            ctrl.txtFeedbackTitle.alignment = TextAlignmentOptions.Center;
            ctrl.txtFeedbackTitle.color = Color.white;

            ctrl.txtFeedbackDetail = CreateTMP(fgo.transform, "txtFeedbackDetail", "Detail...", 18);
            ctrl.txtFeedbackDetail.alignment = TextAlignmentOptions.Center;
            ctrl.txtFeedbackDetail.color = new Color(0.9f, 0.9f, 0.9f);

            ctrl.btnFeedbackOk = CreateButton(fgo.transform, "BtnFeedbackOk", "OK", new Color(1f, 0.76f, 0.2f));
        }

        Debug.Log("[Setup] WebsiteLoginUI siap.");
    }

    // ===================== DASHBOARD =====================

    private static void SetupDashboardUI()
    {
        Canvas desktopCanvas = FindCanvas("desktop");
        if (desktopCanvas == null) { Debug.LogError("[Setup] DesktopCanvas tidak ditemukan!"); return; }

        Transform panel = desktopCanvas.transform.Find("Panel");
        if (panel == null) { Debug.LogError("[Setup] Panel di DesktopCanvas tidak ditemukan!"); return; }

        DashboardPopupController ctrl = panel.GetComponent<DashboardPopupController>();
        if (ctrl != null) { Debug.Log("[Setup] DashboardPopupController sudah ada."); return; }
        ctrl = panel.gameObject.AddComponent<DashboardPopupController>();
        ctrl.dashboardPanelRoot = desktopCanvas.gameObject;

        // Popup Panel
        Transform pp = panel.Find("PopupPanel");
        if (pp != null)
        {
            ctrl.popupPanel = pp.gameObject;
            ctrl.txtPopupTitle = pp.Find("txtPopupTitle")?.GetComponent<TMP_Text>();
            ctrl.txtPopupMessage = pp.Find("txtPopupMessage")?.GetComponent<TMP_Text>();
            Transform ab = pp.Find("ActionButtonRow");
            if (ab != null)
            {
                Transform a = ab.Find("BtnActionA"); if (a != null) ctrl.btnActionA = a.GetComponent<Button>();
                Transform b = ab.Find("BtnActionB"); if (b != null) ctrl.btnActionB = b.GetComponent<Button>();
                if (ctrl.btnActionA != null) ctrl.txtActionA = ctrl.btnActionA.GetComponentInChildren<TMP_Text>();
                if (ctrl.btnActionB != null) ctrl.txtActionB = ctrl.btnActionB.GetComponentInChildren<TMP_Text>();
            }
        }
        else
        {
            GameObject p = CreateModal(panel, "PopupPanel");
            ctrl.popupPanel = p;
            var vlg = p.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(25, 25, 25, 25); vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.MiddleCenter; vlg.childControlWidth = true; vlg.childControlHeight = false;

            ctrl.txtPopupTitle = CreateTMP(p.transform, "txtPopupTitle", "WARNING!", 24);
            ctrl.txtPopupTitle.alignment = TextAlignmentOptions.Center;
            ctrl.txtPopupTitle.color = new Color(0.9f, 0.2f, 0.2f);

            ctrl.txtPopupMessage = CreateTMP(p.transform, "txtPopupMessage", "Pesan popup...", 16);
            ctrl.txtPopupMessage.alignment = TextAlignmentOptions.Center;

            GameObject row = new GameObject("ActionButtonRow", typeof(RectTransform));
            row.transform.SetParent(p.transform, false);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 45);

            ctrl.btnActionA = CreateButton(row.transform, "BtnActionA", "Click Here", new Color(0.8f, 0.3f, 0.3f));
            ctrl.btnActionB = CreateButton(row.transform, "BtnActionB", "Close", new Color(0.3f, 0.5f, 0.3f));
            ctrl.txtActionA = ctrl.btnActionA.GetComponentInChildren<TMP_Text>();
            ctrl.txtActionB = ctrl.btnActionB.GetComponentInChildren<TMP_Text>();
        }

        // Popup Feedback
        Transform pfb = panel.Find("PopupFeedbackPanel");
        if (pfb != null)
        {
            ctrl.feedbackPanel = pfb.gameObject;
            ctrl.txtFeedbackTitle = pfb.Find("txtFeedbackTitle")?.GetComponent<TMP_Text>();
            ctrl.txtFeedbackDetail = pfb.Find("txtFeedbackDetail")?.GetComponent<TMP_Text>();
            Transform nxt = pfb.Find("BtnFeedbackNext");
            if (nxt != null) ctrl.btnFeedbackNext = nxt.GetComponent<Button>();
        }
        else
        {
            GameObject p = CreateModal(panel, "PopupFeedbackPanel");
            p.SetActive(false); ctrl.feedbackPanel = p;
            var vlg = p.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(30, 30, 30, 30); vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.MiddleCenter; vlg.childControlWidth = true; vlg.childControlHeight = false;

            ctrl.txtFeedbackTitle = CreateTMP(p.transform, "txtFeedbackTitle", "BENAR!", 30);
            ctrl.txtFeedbackTitle.alignment = TextAlignmentOptions.Center;
            ctrl.txtFeedbackDetail = CreateTMP(p.transform, "txtFeedbackDetail", "Detail...", 18);
            ctrl.txtFeedbackDetail.alignment = TextAlignmentOptions.Center;
            ctrl.btnFeedbackNext = CreateButton(p.transform, "BtnFeedbackNext", "Lanjut", new Color(1f, 0.76f, 0.2f));
        }

        // Result Panel
        Transform rp = panel.Find("ResultPanel");
        if (rp != null)
        {
            ctrl.resultPanel = rp.gameObject;
            ctrl.txtResultScore = rp.Find("txtResultScore")?.GetComponent<TMP_Text>();
            ctrl.txtResultDetail = rp.Find("txtResultDetail")?.GetComponent<TMP_Text>();
            Transform sel = rp.Find("BtnResultSelesai");
            if (sel != null) ctrl.btnResultSelesai = sel.GetComponent<Button>();
        }
        else
        {
            GameObject p = CreateModal(panel, "ResultPanel");
            p.SetActive(false); ctrl.resultPanel = p;
            var vlg = p.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(30, 30, 30, 30); vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.MiddleCenter; vlg.childControlWidth = true; vlg.childControlHeight = false;

            ctrl.txtResultScore = CreateTMP(p.transform, "txtResultScore", "Skor: 0 / 4", 28);
            ctrl.txtResultScore.alignment = TextAlignmentOptions.Center;
            ctrl.txtResultDetail = CreateTMP(p.transform, "txtResultDetail", "Detail hasil...", 16);
            ctrl.txtResultDetail.alignment = TextAlignmentOptions.Center;
            ctrl.btnResultSelesai = CreateButton(p.transform, "BtnResultSelesai", "Selesai", new Color(1f, 0.76f, 0.2f));
        }

        Debug.Log("[Setup] DashboardUI siap.");
    }

    // ===================== EVALUATION =====================

    private static void SetupEvaluationUI()
    {
        Canvas evalCanvas = FindCanvas("evaluation");
        if (evalCanvas == null) { Debug.LogWarning("[Setup] EvaluationCanvas tidak ditemukan."); return; }

        Transform evalPanel = evalCanvas.transform.Find("EvaluationPanel");
        if (evalPanel == null) { Debug.LogWarning("[Setup] EvaluationPanel tidak ditemukan."); return; }

        EvaluationPanel_Tp4 ctrl = evalPanel.GetComponent<EvaluationPanel_Tp4>();
        if (ctrl == null) ctrl = evalPanel.gameObject.AddComponent<EvaluationPanel_Tp4>();

        ctrl.evaluationPanelRoot = evalPanel.gameObject;

        Transform qp = evalPanel.Find("QuestionPanel");
        if (qp != null)
        {
            ctrl.questionContainer = qp.gameObject;
            ctrl.txtQuestionNum = qp.Find("TxtQuestionNum")?.GetComponent<TMP_Text>();
            ctrl.txtQuestionText = qp.Find("TxtQuestionText")?.GetComponent<TMP_Text>();
            ctrl.btnChoiceA = qp.Find("ButtonChoiceA")?.GetComponent<Button>();
            ctrl.btnChoiceB = qp.Find("ButtonChoiceB")?.GetComponent<Button>();
            ctrl.btnNext = qp.Find("BtnNext")?.GetComponent<Button>();
        }

        Transform rp = evalPanel.Find("ResultPanel");
        if (rp != null)
        {
            ctrl.resultContainer = rp.gameObject;
            ctrl.txtResultScore = rp.Find("TxtResultScore")?.GetComponent<TMP_Text>();
            ctrl.txtResultDetail = rp.Find("TxtResultDetail")?.GetComponent<TMP_Text>();
            ctrl.btnSelesai = rp.Find("ButtonSelesai")?.GetComponent<Button>();
        }

        GameObject evalMgrObj = GameObject.Find("EvaluationManager_Tp4");
        EvaluationManager_Tp4 em = evalMgrObj?.GetComponent<EvaluationManager_Tp4>();
        if (em == null)
        {
            evalMgrObj = new GameObject("EvaluationManager_Tp4");
            em = evalMgrObj.AddComponent<EvaluationManager_Tp4>();
        }

        em.evaluationPanel = ctrl;
        em.evaluationCanvas = evalCanvas.gameObject;

        Debug.Log("[Setup] EvaluationUI siap.");
    }

    // ===================== GAME MANAGER =====================

    private static void SetupGameManager()
    {
        GameManager_Tp4 gm = Object.FindObjectOfType<GameManager_Tp4>(true);
        if (gm == null)
        {
            gm = new GameObject("GameManager_Tp4").AddComponent<GameManager_Tp4>();
            Debug.Log("[Setup] GameManager_Tp4 dibuat.");
        }

        gm.desktopCanvas = FindCanvas("desktop")?.gameObject;
        gm.websiteCanvas = FindCanvas("website")?.gameObject;
        gm.objectiveUI = Object.FindObjectOfType<ObjectiveUI_Tp4>(true);
        gm.evaluationManager = Object.FindObjectOfType<EvaluationManager_Tp4>(true);

        Canvas wc = FindCanvas("website");
        Canvas dc = FindCanvas("desktop");
        if (wc != null) gm.websiteController = wc.transform.Find("WebsitePanel")?.GetComponent<WebsiteLoginController>();
        if (dc != null) gm.dashboardController = dc.transform.Find("Panel")?.GetComponent<DashboardPopupController>();

        Debug.Log("[Setup] GameManager_Tp4 references di-wire.");
    }

    // ===================== BUTTONS =====================

    private static void WireChromeButton()
    {
        Canvas dc = FindCanvas("desktop");
        if (dc == null) return;
        GameManager_Tp4 gm = Object.FindObjectOfType<GameManager_Tp4>(true);
        if (gm == null) return;

        int wired = 0;
        foreach (Button btn in dc.GetComponentsInChildren<Button>(true))
        {
            if (!btn.name.ToLower().Contains("chrome")) continue;
            btn.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(btn.onClick, gm.OnBrowserIconClicked);
            wired++;
        }
        Debug.Log($"[Setup] {wired} chrome button(s) di-wire.");
    }

    private static void EnsureScoreManager()
    {
        if (GameObject.Find("ScoreManager") == null)
        {
            new GameObject("ScoreManager").AddComponent<ScoreManager>();
            Debug.Log("[Setup] ScoreManager dibuat.");
        }
    }

    // ===================== HELPERS =====================

    private static Canvas FindCanvas(string partial)
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
            if (c.name.ToLower().Contains(partial.ToLower())) return c;
        return null;
    }

    private static Button EnsureButton(Transform t)
    {
        Button b = t.GetComponent<Button>();
        return b != null ? b : t.gameObject.AddComponent<Button>();
    }

    private static void ConvertToInputField(Transform panel, string placeholder)
    {
        if (panel == null) return;
        TMP_InputField input = panel.GetComponent<TMP_InputField>();
        if (input != null)
        {
            input.contentType = placeholder == "Password" ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
            return;
        }

        TMP_Text old = panel.GetComponentInChildren<TMP_Text>();
        if (old != null) Object.DestroyImmediate(old.gameObject);

        input = panel.gameObject.AddComponent<TMP_InputField>();
        input.contentType = placeholder == "Password" ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
        textArea.transform.SetParent(panel, false);
        RectTransform taRT = textArea.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(10, 5); taRT.offsetMax = new Vector2(-10, -5);

        GameObject phGO = new GameObject("Placeholder", typeof(RectTransform));
        phGO.transform.SetParent(textArea.transform, false);
        RectTransform phRT = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one; phRT.sizeDelta = Vector2.zero;

        TMP_Text phText = phGO.AddComponent<TextMeshProUGUI>();
        phText.text = placeholder + "...";
        phText.fontSize = 14;
        phText.color = new Color(0.5f, 0.5f, 0.5f);
        phText.alignment = TextAlignmentOptions.Left;
        phText.fontStyle = FontStyles.Italic;

        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(textArea.transform, false);
        RectTransform tRT = textGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one; tRT.sizeDelta = Vector2.zero;

        TMP_Text textComp = textGO.AddComponent<TextMeshProUGUI>();
        textComp.fontSize = 14; textComp.alignment = TextAlignmentOptions.Left; textComp.color = Color.white;

        input.textViewport = taRT;
        input.placeholder = phText;
        input.textComponent = textComp;

        Debug.Log($"[Setup] {panel.name} dikonversi ke TMP_InputField.");
    }

    private static TMP_Text CreateTMP(Transform parent, string name, string text, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 40);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.alignment = TextAlignmentOptions.Left;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 40);
        go.GetComponent<Image>().color = color;
        GameObject lbl = new GameObject("Label", typeof(RectTransform));
        lbl.transform.SetParent(go.transform, false);
        RectTransform lrt = lbl.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.sizeDelta = Vector2.zero;
        TMP_Text lt = lbl.AddComponent<TextMeshProUGUI>();
        lt.text = label; lt.fontSize = 16; lt.alignment = TextAlignmentOptions.Center;
        lt.color = Color.white; lt.fontStyle = FontStyles.Bold;
        return go.GetComponent<Button>();
    }

    private static GameObject CreateModal(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(450, 300);
        go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        go.SetActive(false);
        return go;
    }
}
