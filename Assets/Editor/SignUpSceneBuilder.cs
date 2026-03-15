using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public static class SignUpSceneBuilder
{
    [MenuItem("CyberAware/Build SignUp Scene UI")]
    public static void BuildSignUpScene()
    {
        // Open SignUpScene
        string scenePath = "Assets/Scenes/SignUpScene.unity";
        EditorSceneManager.OpenScene(scenePath);

        // Clear existing UI (keep Camera, Light, EventSystem)
        Canvas existing = Object.FindObjectOfType<Canvas>();
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // ── Canvas ────────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── FadeOverlay ───────────────────────────────────────────────────────
        GameObject fadeGO = CreateFullScreenImage(canvasGO, "FadeOverlay", new Color(0, 0, 0, 0));
        CanvasGroup fadeGroup = fadeGO.AddComponent<CanvasGroup>();
        fadeGroup.alpha          = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable   = false;

        // ── Background ────────────────────────────────────────────────────────
        CreateFullScreenImage(canvasGO, "Background", HexColor("#F5F5F5"));

        // ── CenterPanel ───────────────────────────────────────────────────────
        GameObject panel = new GameObject("CenterPanel");
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta        = new Vector2(420, 640);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.spacing                = 14f;
        vlg.padding                = new RectOffset(24, 24, 32, 32);
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ── Logo ──────────────────────────────────────────────────────────────
        GameObject logoGO = new GameObject("Logo");
        logoGO.transform.SetParent(panel.transform, false);
        TextMeshProUGUI logo = logoGO.AddComponent<TextMeshProUGUI>();
        logo.text      = "CyberAware";
        logo.fontSize  = 32;
        logo.fontStyle = FontStyles.Bold;
        logo.color     = HexColor("#1A1A2E");
        logo.alignment = TextAlignmentOptions.Center;
        SetLayoutHeight(logoGO, 44f);

        // ── Subtitle ──────────────────────────────────────────────────────────
        GameObject subtitleGO = new GameObject("Subtitle");
        subtitleGO.transform.SetParent(panel.transform, false);
        TextMeshProUGUI subtitle = subtitleGO.AddComponent<TextMeshProUGUI>();
        subtitle.text      = "Buat Akun Baru";
        subtitle.fontSize  = 16;
        subtitle.color     = HexColor("#666666");
        subtitle.alignment = TextAlignmentOptions.Center;
        SetLayoutHeight(subtitleGO, 26f);

        // ── Divider ───────────────────────────────────────────────────────────
        GameObject dividerGO = new GameObject("Divider");
        dividerGO.transform.SetParent(panel.transform, false);
        Image divider = dividerGO.AddComponent<Image>();
        divider.color = HexColor("#E0E0E0");
        SetLayoutHeight(dividerGO, 1f);

        // ── EmailInput ────────────────────────────────────────────────────────
        GameObject emailGO = CreateTMPInputField(panel, "EmailInput", "Email", TMP_InputField.ContentType.EmailAddress);
        SetLayoutHeight(emailGO, 48f);

        // ── UsernameInput ─────────────────────────────────────────────────────
        GameObject usernameGO = CreateTMPInputField(panel, "UsernameInput", "Username", TMP_InputField.ContentType.Standard);
        SetLayoutHeight(usernameGO, 48f);

        // ── PasswordRow ───────────────────────────────────────────────────────
        GameObject passwordRow   = CreatePasswordRow(panel, "PasswordRow", "PasswordInput", "Password (min. 8 karakter)", "ShowHideBtn",
            out GameObject passwordGO, out GameObject showHideBtn);

        // ── ConfirmPasswordRow ────────────────────────────────────────────────
        GameObject confirmRow = CreatePasswordRow(panel, "ConfirmPasswordRow", "ConfirmPasswordInput", "Konfirmasi Password", "ShowHideConfirmBtn",
            out GameObject confirmPasswordGO, out GameObject showHideConfirmBtn);

        // ── ErrorText ─────────────────────────────────────────────────────────
        GameObject errorGO = new GameObject("ErrorText");
        errorGO.transform.SetParent(panel.transform, false);
        TextMeshProUGUI errorTxt = errorGO.AddComponent<TextMeshProUGUI>();
        errorTxt.text      = "";
        errorTxt.fontSize  = 12;
        errorTxt.color     = HexColor("#E53935");
        errorTxt.alignment = TextAlignmentOptions.Left;
        SetLayoutHeight(errorGO, 18f);
        errorGO.SetActive(false);

        // ── SuccessText ───────────────────────────────────────────────────────
        GameObject successGO = new GameObject("SuccessText");
        successGO.transform.SetParent(panel.transform, false);
        TextMeshProUGUI successTxt = successGO.AddComponent<TextMeshProUGUI>();
        successTxt.text      = "";
        successTxt.fontSize  = 12;
        successTxt.color     = HexColor("#2E7D32");
        successTxt.alignment = TextAlignmentOptions.Left;
        SetLayoutHeight(successGO, 18f);
        successGO.SetActive(false);

        // ── SignUpButton ──────────────────────────────────────────────────────
        GameObject signUpBtn = CreateButton(panel, "SignUpButton", "DAFTAR");
        Image signUpBtnImg = signUpBtn.GetComponent<Image>();
        if (signUpBtnImg != null) signUpBtnImg.color = HexColor("#1A1A2E");
        TextMeshProUGUI signUpBtnTxt = signUpBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (signUpBtnTxt != null) signUpBtnTxt.color = Color.white;
        SetLayoutHeight(signUpBtn, 48f);

        // ── LoadingSpinner ────────────────────────────────────────────────────
        GameObject spinnerGO = new GameObject("LoadingSpinner");
        spinnerGO.transform.SetParent(panel.transform, false);
        Image spinnerImg = spinnerGO.AddComponent<Image>();
        spinnerImg.color = HexColor("#1A1A2E");
        LoadingSpinner spinner = spinnerGO.AddComponent<LoadingSpinner>();
        SerializedObject spinnerSO = new SerializedObject(spinner);
        spinnerSO.FindProperty("spinnerImage").objectReferenceValue = spinnerGO.GetComponent<RectTransform>();
        spinnerSO.ApplyModifiedPropertiesWithoutUndo();
        LayoutElement spinnerLE = spinnerGO.AddComponent<LayoutElement>();
        spinnerLE.preferredWidth  = 36f;
        spinnerLE.preferredHeight = 36f;
        spinnerLE.minHeight       = 36f;
        spinnerGO.SetActive(false);

        // ── LoginRow ──────────────────────────────────────────────────────────
        GameObject loginRow = new GameObject("LoginRow");
        loginRow.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup loginHLG = loginRow.AddComponent<HorizontalLayoutGroup>();
        loginHLG.childAlignment        = TextAnchor.MiddleCenter;
        loginHLG.spacing               = 4f;
        loginHLG.childControlHeight    = true;
        loginHLG.childControlWidth     = true;
        loginHLG.childForceExpandWidth = false;
        SetLayoutHeight(loginRow, 28f);

        GameObject loginLabelGO = new GameObject("Label");
        loginLabelGO.transform.SetParent(loginRow.transform, false);
        TextMeshProUGUI loginLabel = loginLabelGO.AddComponent<TextMeshProUGUI>();
        loginLabel.text      = "Sudah punya akun?";
        loginLabel.fontSize  = 13;
        loginLabel.color     = HexColor("#666666");
        loginLabel.alignment = TextAlignmentOptions.Center;

        GameObject goToLoginBtn = CreateButton(loginRow, "GoToLoginBtn", "Masuk");
        Image goToLoginImg = goToLoginBtn.GetComponent<Image>();
        if (goToLoginImg != null) goToLoginImg.color = Color.clear;
        TextMeshProUGUI goToLoginTxt = goToLoginBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (goToLoginTxt != null)
        {
            goToLoginTxt.fontSize = 13;
            goToLoginTxt.color    = HexColor("#1565C0");
        }

        // ── Attach SignUpController ───────────────────────────────────────────
        SignUpController controller = panel.AddComponent<SignUpController>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("emailInput").objectReferenceValue                   = emailGO.GetComponent<TMP_InputField>();
        so.FindProperty("usernameInput").objectReferenceValue                = usernameGO.GetComponent<TMP_InputField>();
        so.FindProperty("passwordInput").objectReferenceValue                = passwordGO.GetComponent<TMP_InputField>();
        so.FindProperty("confirmPasswordInput").objectReferenceValue         = confirmPasswordGO.GetComponent<TMP_InputField>();
        so.FindProperty("signUpButton").objectReferenceValue                 = signUpBtn.GetComponent<Button>();
        so.FindProperty("goToLoginButton").objectReferenceValue              = goToLoginBtn.GetComponent<Button>();
        so.FindProperty("showHidePasswordButton").objectReferenceValue       = showHideBtn.GetComponent<Button>();
        so.FindProperty("showHideConfirmPasswordButton").objectReferenceValue = showHideConfirmBtn.GetComponent<Button>();
        so.FindProperty("errorText").objectReferenceValue                    = errorTxt;
        so.FindProperty("loadingSpinner").objectReferenceValue               = spinnerGO;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Wire FadeOverlay to AuthUIManager if present
        AuthUIManager authUI = Object.FindObjectOfType<AuthUIManager>();
        if (authUI != null)
        {
            SerializedObject authSO = new SerializedObject(authUI);
            authSO.FindProperty("fadeOverlay").objectReferenceValue = fadeGroup;
            authSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // FadeOverlay on top
        fadeGO.transform.SetAsLastSibling();

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[SignUpSceneBuilder] SignUpScene UI built and saved.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject CreatePasswordRow(GameObject parent, string rowName,
        string inputName, string placeholder, string btnName,
        out GameObject inputGO, out GameObject btnGO)
    {
        GameObject row = new GameObject(rowName);
        row.transform.SetParent(parent.transform, false);
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 8f;
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlHeight    = true;
        hlg.childControlWidth     = false;
        hlg.childForceExpandWidth = false;
        SetLayoutHeight(row, 48f);

        inputGO = CreateTMPInputField(row, inputName, placeholder, TMP_InputField.ContentType.Password);
        LayoutElement inputLE = inputGO.AddComponent<LayoutElement>();
        inputLE.flexibleWidth    = 1f;
        inputLE.minHeight        = 48f;
        inputLE.preferredHeight  = 48f;

        btnGO = CreateButton(row, btnName, "👁");
        LayoutElement btnLE = btnGO.AddComponent<LayoutElement>();
        btnLE.minWidth       = 48f;
        btnLE.preferredWidth = 48f;
        btnLE.minHeight      = 48f;
        btnLE.preferredHeight = 48f;

        return row;
    }

    static GameObject CreateFullScreenImage(GameObject parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    static GameObject CreateTMPInputField(GameObject parent, string name, string placeholder, TMP_InputField.ContentType contentType)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image bg = go.AddComponent<Image>();
        bg.color = Color.white;

        TMP_InputField field = go.AddComponent<TMP_InputField>();

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize  = 16;
        text.color     = HexColor("#1A1A2E");
        text.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(12, 0);
        textRT.offsetMax = new Vector2(-12, 0);

        GameObject phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI ph = phGO.AddComponent<TextMeshProUGUI>();
        ph.text      = placeholder;
        ph.fontSize  = 16;
        ph.color     = HexColor("#AAAAAA");
        ph.fontStyle = FontStyles.Italic;
        ph.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform phRT = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = new Vector2(12, 0);
        phRT.offsetMax = new Vector2(-12, 0);

        field.textComponent = text;
        field.placeholder   = ph;
        field.contentType   = contentType;

        return go;
    }

    static GameObject CreateButton(GameObject parent, string name, string label)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = HexColor("#1A1A2E");
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 16;
        txt.color     = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        RectTransform txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        return go;
    }

    static void SetLayoutHeight(GameObject go, float height)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        le.minHeight       = height;
        le.preferredHeight = height;
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
