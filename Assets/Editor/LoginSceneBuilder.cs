using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public static class LoginSceneBuilder
{
    [MenuItem("CyberAware/Build Login Scene UI")]
    public static void BuildLoginScene()
    {
        // Open LoginScene
        string scenePath = "Assets/Scenes/LoginScene.unity";
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
        panelRT.sizeDelta        = new Vector2(420, 520);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment            = TextAnchor.UpperCenter;
        vlg.spacing                   = 14f;
        vlg.padding                   = new RectOffset(24, 24, 32, 32);
        vlg.childControlWidth         = true;
        vlg.childControlHeight        = false;
        vlg.childForceExpandWidth     = true;
        vlg.childForceExpandHeight    = false;

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
        subtitle.text      = "Cybersecurity Awareness Game";
        subtitle.fontSize  = 14;
        subtitle.color     = HexColor("#666666");
        subtitle.alignment = TextAlignmentOptions.Center;
        SetLayoutHeight(subtitleGO, 24f);

        // ── Divider ───────────────────────────────────────────────────────────
        GameObject dividerGO = new GameObject("Divider");
        dividerGO.transform.SetParent(panel.transform, false);
        Image divider = dividerGO.AddComponent<Image>();
        divider.color = HexColor("#E0E0E0");
        SetLayoutHeight(dividerGO, 1f);

        // ── UsernameInput ─────────────────────────────────────────────────────
        GameObject usernameGO = CreateTMPInputField(panel, "UsernameInput", "Username atau Email", false);
        SetLayoutHeight(usernameGO, 48f);

        // ── PasswordRow ───────────────────────────────────────────────────────
        GameObject passwordRow = new GameObject("PasswordRow");
        passwordRow.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup hlg = passwordRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 8f;
        hlg.childAlignment       = TextAnchor.MiddleCenter;
        hlg.childControlHeight   = true;
        hlg.childControlWidth    = false;
        hlg.childForceExpandWidth = false;
        SetLayoutHeight(passwordRow, 48f);

        GameObject passwordGO = CreateTMPInputField(passwordRow, "PasswordInput", "Password", true);
        LayoutElement pwLE = passwordGO.AddComponent<LayoutElement>();
        pwLE.flexibleWidth = 1f;
        pwLE.minHeight     = 48f;
        pwLE.preferredHeight = 48f;

        GameObject showHideBtn = CreateButton(passwordRow, "ShowHideBtn", "👁");
        LayoutElement btnLE = showHideBtn.AddComponent<LayoutElement>();
        btnLE.minWidth       = 48f;
        btnLE.preferredWidth = 48f;
        btnLE.minHeight      = 48f;
        btnLE.preferredHeight = 48f;

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

        // ── LoginButton ───────────────────────────────────────────────────────
        GameObject loginBtn = CreateButton(panel, "LoginButton", "MASUK");
        Image loginBtnImg = loginBtn.GetComponent<Image>();
        if (loginBtnImg != null) loginBtnImg.color = HexColor("#1A1A2E");
        TextMeshProUGUI loginBtnTxt = loginBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (loginBtnTxt != null) loginBtnTxt.color = Color.white;
        SetLayoutHeight(loginBtn, 48f);

        // ── LoadingSpinner ────────────────────────────────────────────────────
        GameObject spinnerGO = new GameObject("LoadingSpinner");
        spinnerGO.transform.SetParent(panel.transform, false);
        Image spinnerImg = spinnerGO.AddComponent<Image>();
        spinnerImg.color = HexColor("#1A1A2E");
        LoadingSpinner spinner = spinnerGO.AddComponent<LoadingSpinner>();
        SerializedObject spinnerSO = new SerializedObject(spinner);
        spinnerSO.FindProperty("spinnerImage").objectReferenceValue = spinnerGO.GetComponent<RectTransform>();
        spinnerSO.ApplyModifiedPropertiesWithoutUndo();
        SetLayoutHeight(spinnerGO, 36f);
        LayoutElement spinnerLE = spinnerGO.AddComponent<LayoutElement>();
        spinnerLE.preferredWidth  = 36f;
        spinnerLE.preferredHeight = 36f;
        spinnerGO.SetActive(false);

        // ── SignUpRow ─────────────────────────────────────────────────────────
        GameObject signUpRow = new GameObject("SignUpRow");
        signUpRow.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup signUpHLG = signUpRow.AddComponent<HorizontalLayoutGroup>();
        signUpHLG.childAlignment       = TextAnchor.MiddleCenter;
        signUpHLG.spacing              = 4f;
        signUpHLG.childControlHeight   = true;
        signUpHLG.childControlWidth    = true;
        signUpHLG.childForceExpandWidth = false;
        SetLayoutHeight(signUpRow, 28f);

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(signUpRow.transform, false);
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text      = "Belum punya akun?";
        label.fontSize  = 13;
        label.color     = HexColor("#666666");
        label.alignment = TextAlignmentOptions.Center;

        GameObject goToSignUpBtn = CreateButton(signUpRow, "GoToSignUpBtn", "Daftar");
        Image goToSignUpImg = goToSignUpBtn.GetComponent<Image>();
        if (goToSignUpImg != null) goToSignUpImg.color = Color.clear;
        TextMeshProUGUI goToSignUpTxt = goToSignUpBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (goToSignUpTxt != null)
        {
            goToSignUpTxt.fontSize = 13;
            goToSignUpTxt.color    = HexColor("#1565C0");
        }

        // ── Attach LoginController ────────────────────────────────────────────
        LoginController controller = panel.AddComponent<LoginController>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("usernameInput").objectReferenceValue        = usernameGO.GetComponent<TMP_InputField>();
        so.FindProperty("passwordInput").objectReferenceValue        = passwordGO.GetComponent<TMP_InputField>();
        so.FindProperty("loginButton").objectReferenceValue          = loginBtn.GetComponent<Button>();
        so.FindProperty("goToSignUpButton").objectReferenceValue     = goToSignUpBtn.GetComponent<Button>();
        so.FindProperty("showHidePasswordButton").objectReferenceValue = showHideBtn.GetComponent<Button>();
        so.FindProperty("errorText").objectReferenceValue            = errorTxt;
        so.FindProperty("loadingSpinner").objectReferenceValue       = spinnerGO;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Wire FadeOverlay to AuthUIManager if present
        AuthUIManager authUI = Object.FindObjectOfType<AuthUIManager>();
        if (authUI != null)
        {
            SerializedObject authSO = new SerializedObject(authUI);
            authSO.FindProperty("fadeOverlay").objectReferenceValue = fadeGroup;
            authSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // Move FadeOverlay to top of canvas hierarchy (renders last = on top)
        fadeGO.transform.SetAsLastSibling();

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[LoginSceneBuilder] LoginScene UI built and saved.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject CreateFullScreenImage(GameObject parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        return go;
    }

    static GameObject CreateTMPInputField(GameObject parent, string name, string placeholder, bool isPassword)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        Image bg = go.AddComponent<Image>();
        bg.color = Color.white;

        TMP_InputField field = go.AddComponent<TMP_InputField>();

        // Text child
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize  = 16;
        text.color     = HexColor("#1A1A2E");
        text.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin  = Vector2.zero;
        textRT.anchorMax  = Vector2.one;
        textRT.offsetMin  = new Vector2(12, 0);
        textRT.offsetMax  = new Vector2(-12, 0);

        // Placeholder child
        GameObject phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI ph = phGO.AddComponent<TextMeshProUGUI>();
        ph.text      = placeholder;
        ph.fontSize  = 16;
        ph.color     = HexColor("#AAAAAA");
        ph.fontStyle = FontStyles.Italic;
        ph.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform phRT = phGO.GetComponent<RectTransform>();
        phRT.anchorMin  = Vector2.zero;
        phRT.anchorMax  = Vector2.one;
        phRT.offsetMin  = new Vector2(12, 0);
        phRT.offsetMax  = new Vector2(-12, 0);

        field.textComponent  = text;
        field.placeholder    = ph;
        field.contentType    = isPassword
            ? TMP_InputField.ContentType.Password
            : TMP_InputField.ContentType.Standard;

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
        txtRT.anchorMin  = Vector2.zero;
        txtRT.anchorMax  = Vector2.one;
        txtRT.offsetMin  = Vector2.zero;
        txtRT.offsetMax  = Vector2.zero;

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
