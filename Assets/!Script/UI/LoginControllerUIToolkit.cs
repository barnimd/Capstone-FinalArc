using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit-based login controller. Drop-in replacement for LoginController (uGUI).
/// Wires nickname/password inputs + login button + Google button + signup link to FirebaseManager.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class LoginControllerUIToolkit : MonoBehaviour
{
    private const string GoogleClientId = "211220332327-0o4c2mi8drpum0crjfvt8uosord5s65r.apps.googleusercontent.com";

    // Jeda supaya pesan "Login berhasil" sempat kebaca sebelum pindah scene
    private const float SuccessDelay = 1.2f;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void GoogleAuth_Init(string clientId);
    [DllImport("__Internal")] private static extern void GoogleAuth_SignIn(string gameObjectName);
#endif

    private UIDocument    _document;
    private VisualElement _root;
    private TextField     _nicknameInput;
    private TextField     _passwordInput;
    private Button        _loginButton;
    private Button        _googleButton;
    private Label         _forgotLink;
    private Label         _signupLink;
    private Label         _guestLink;
    private Label         _contactLink;
    private Label         _errorLabel;
    private bool          _isSubmitting;

    void OnEnable()
    {
        _document = GetComponent<UIDocument>();
        _root     = _document != null ? _document.rootVisualElement : null;
        if (_root == null)
        {
            Debug.LogWarning("[LoginControllerUIToolkit] rootVisualElement null");
            return;
        }

        _nicknameInput  = _root.Q<TextField>("nickname-input");
        _passwordInput  = _root.Q<TextField>("password-input");
        _loginButton    = _root.Q<Button>("login-button");
        _googleButton   = _root.Q<Button>("google-button");
        _forgotLink     = _root.Q<Label>("forgot-link");
        _signupLink     = _root.Q<Label>("signup-link");
        _guestLink      = _root.Q<Label>("guest-link");
        _contactLink    = _root.Q<Label>("link-contact");
        _errorLabel     = _root.Q<Label>("login-error");

        if (_passwordInput != null) _passwordInput.isPasswordField = true;

        if (_loginButton  != null) _loginButton.clicked   += OnLoginClicked;
        if (_googleButton != null) _googleButton.clicked  += OnGoogleSignInClicked;
        if (_signupLink   != null) _signupLink.RegisterCallback<ClickEvent>(evt => OnGoToSignUpClicked());
        if (_guestLink    != null) _guestLink.RegisterCallback<ClickEvent>(evt => OnPlayAsGuestClicked());
        if (_forgotLink   != null) _forgotLink.RegisterCallback<ClickEvent>(evt => Debug.Log("[Login] Forgot password — not implemented"));
        if (_contactLink  != null) _contactLink.RegisterCallback<ClickEvent>(evt => Debug.Log("[Login] Contact support — not implemented"));

        // Enter key submits
        _root.RegisterCallback<KeyDownEvent>(OnKeyDown);

        HideError();

#if UNITY_WEBGL && !UNITY_EDITOR
        GoogleAuth_Init(GoogleClientId);
#endif
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            OnLoginClicked();
    }

    private void OnLoginClicked()
    {
        if (_isSubmitting) return;

        string username = _nicknameInput != null ? _nicknameInput.value?.Trim() : "";
        string password = _passwordInput != null ? _passwordInput.value : "";

        if (string.IsNullOrEmpty(username)) { ShowError("Nama panggilan tidak boleh kosong"); return; }
        if (string.IsNullOrEmpty(password)) { ShowError("Kata sandi tidak boleh kosong"); return; }

        HideError();
        SetLoading(true);

        if (FirebaseManager.Instance == null)
        {
            SetLoading(false);
            ShowError("Auth service belum siap. Coba lagi sebentar.");
            return;
        }

        FirebaseManager.Instance.SignInWithUsernameAndPassword(username, password, (success, error) =>
        {
            if (success)
            {
                // Tetap disable tombol supaya user gak spam login pas transisi
                ShowSuccess("Login berhasil! Menuju menu...");
                StartCoroutine(DelayedTransition(AuthUIManager.SCENE_GAME, SuccessDelay));
            }
            else
            {
                SetLoading(false);
                ShowError(ParseError(error));
            }
        });
    }

    private string ParseError(string error)
    {
        if (string.IsNullOrEmpty(error))                    return "Terjadi kesalahan, coba lagi";
        if (error.Contains("tidak ditemukan"))              return error;
        if (error.Contains("Gagal"))                        return error;
        if (error.Contains("INVALID_PASSWORD"))             return "Kata sandi salah";
        if (error.Contains("INVALID_LOGIN_CREDENTIALS"))    return "Nama panggilan atau kata sandi salah";
        if (error.Contains("USER_DISABLED"))                return "Akun dinonaktifkan";
        if (error.Contains("TOO_MANY_ATTEMPTS"))            return "Terlalu banyak percobaan, coba lagi nanti";
        return "Nama panggilan atau kata sandi salah";
    }

    private void OnGoToSignUpClicked()
    {
        AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_SIGNUP);
    }

    private void OnPlayAsGuestClicked()
    {
        AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_GUEST);
    }

    private void OnGoogleSignInClicked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        HideError();
        AuthUIManager.Instance.ShowLoading(true);
        GoogleAuth_SignIn(gameObject.name);
#else
        ShowError("Google Sign-In hanya tersedia di WebGL build");
#endif
    }

    // Called by JS via SendMessage in WebGL build
    public void OnGoogleSignInSuccess(string json)
    {
        string accessToken = ExtractJsonField(json, "accessToken");
        FirebaseManager.Instance.SignInWithGoogle(accessToken, (success, error) =>
        {
            AuthUIManager.Instance.ShowLoading(false);
            if (success)
            {
                SetLoading(true);
                ShowSuccess("Login berhasil! Menuju menu...");
                StartCoroutine(DelayedTransition(AuthUIManager.SCENE_GAME, SuccessDelay));
            }
            else
            {
                ShowError("Login Google gagal. Coba lagi.");
            }
        });
    }

    public void OnGoogleSignInFailed(string error)
    {
        AuthUIManager.Instance.ShowLoading(false);
        if (error != "Sign-in dismissed" && error != "Dismissed")
            ShowError("Login Google gagal. Coba lagi.");
    }

    private IEnumerator DelayedTransition(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        AuthUIManager.Instance.TransitionToScene(sceneName);
    }

    private void ShowError(string msg)
    {
        if (_errorLabel == null) return;
        _errorLabel.text = msg;
        _errorLabel.RemoveFromClassList("success");
        _errorLabel.AddToClassList("visible");
    }

    private void ShowSuccess(string msg)
    {
        if (_errorLabel == null) return;
        _errorLabel.text = msg;
        _errorLabel.AddToClassList("success");
        _errorLabel.AddToClassList("visible");
    }

    private void HideError()
    {
        if (_errorLabel == null) return;
        _errorLabel.text = "";
        _errorLabel.RemoveFromClassList("visible");
        _errorLabel.RemoveFromClassList("success");
    }

    private void SetLoading(bool isLoading)
    {
        _isSubmitting = isLoading;
        if (_loginButton  != null) _loginButton.SetEnabled(!isLoading);
        if (_googleButton != null) _googleButton.SetEnabled(!isLoading);
    }

    private string ExtractJsonField(string json, string key)
    {
        string search = "\"" + key + "\":\"";
        int start = json.IndexOf(search);
        if (start < 0) return "";
        start += search.Length;
        int end = json.IndexOf("\"", start);
        return end < 0 ? "" : json.Substring(start, end - start);
    }
}
