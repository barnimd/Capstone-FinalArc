using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit-based sign-up controller. Drop-in replacement for SignUpController (uGUI).
/// Includes real-time password strength meter (4 levels: weak/medium/strong/super).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class SignUpControllerUIToolkit : MonoBehaviour
{
    private const string GoogleClientId = "211220332327-0o4c2mi8drpum0crjfvt8uosord5s65r.apps.googleusercontent.com";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void GoogleAuth_Init(string clientId);
    [DllImport("__Internal")] private static extern void GoogleAuth_SignIn(string gameObjectName);
#endif

    private UIDocument    _document;
    private VisualElement _root;
    private TextField     _emailInput;
    private TextField     _nicknameInput;
    private TextField     _passwordInput;
    private TextField     _confirmInput;
    private Button        _signupButton;
    private Button        _googleButton;
    private Label         _signinLink;
    private Label         _guestLink;
    private Label         _errorLabel;
    private Label         _strengthLabel;
    private VisualElement[] _strengthSegments = new VisualElement[4];

    void OnEnable()
    {
        _document = GetComponent<UIDocument>();
        _root     = _document != null ? _document.rootVisualElement : null;
        if (_root == null) return;

        _emailInput     = _root.Q<TextField>("email-input");
        _nicknameInput  = _root.Q<TextField>("nickname-input");
        _passwordInput  = _root.Q<TextField>("password-input");
        _confirmInput   = _root.Q<TextField>("confirm-input");
        _signupButton   = _root.Q<Button>("signup-button");
        _googleButton   = _root.Q<Button>("google-button");
        _signinLink     = _root.Q<Label>("signin-link");
        _guestLink      = _root.Q<Label>("guest-link");
        _errorLabel     = _root.Q<Label>("signup-error");
        _strengthLabel  = _root.Q<Label>("strength-label");

        for (int i = 0; i < 4; i++)
            _strengthSegments[i] = _root.Q<VisualElement>($"strength-seg-{i + 1}");

        if (_passwordInput != null) _passwordInput.isPasswordField = true;
        if (_confirmInput  != null) _confirmInput.isPasswordField  = true;

        if (_signupButton != null) _signupButton.clicked += OnSignUpClicked;
        if (_googleButton != null) _googleButton.clicked += OnGoogleSignUpClicked;
        if (_signinLink   != null) _signinLink.RegisterCallback<ClickEvent>(evt => OnGoToLogin());
        if (_guestLink    != null) _guestLink.RegisterCallback<ClickEvent>(evt => OnPlayAsGuest());

        // Live password strength feedback
        if (_passwordInput != null)
            _passwordInput.RegisterValueChangedCallback(evt => UpdatePasswordStrength(evt.newValue));

        // Enter key submits
        _root.RegisterCallback<KeyDownEvent>(OnKeyDown);

        HideError();
        UpdatePasswordStrength("");

#if UNITY_WEBGL && !UNITY_EDITOR
        GoogleAuth_Init(GoogleClientId);
#endif
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            OnSignUpClicked();
    }

    // ── Password strength logic ──────────────────────────────────────────────

    public enum StrengthLevel { None = 0, Weak = 1, Medium = 2, Strong = 3, Super = 4 }

    public static StrengthLevel CalculateStrength(string password)
    {
        if (string.IsNullOrEmpty(password)) return StrengthLevel.None;

        bool hasLower   = false;
        bool hasUpper   = false;
        bool hasDigit   = false;
        bool hasSpecial = false;

        foreach (char c in password)
        {
            if (char.IsLower(c))      hasLower   = true;
            else if (char.IsUpper(c)) hasUpper   = true;
            else if (char.IsDigit(c)) hasDigit   = true;
            else if (!char.IsWhiteSpace(c)) hasSpecial = true;
        }

        bool hasLetter = hasLower || hasUpper;

        // Only chars (letters) → Weak
        if (hasLetter && !hasDigit && !hasSpecial && !hasUpper) return StrengthLevel.Weak;
        // Just letters w/ upper (no digit, no special) → still weak-ish, treat as Weak
        if (hasLetter && !hasDigit && !hasSpecial)              return StrengthLevel.Weak;
        // Letters + digits (no special, no upper) → Medium
        if (hasLetter && hasDigit && !hasSpecial && !hasUpper)  return StrengthLevel.Medium;
        if (hasLetter && hasDigit && !hasSpecial)               return StrengthLevel.Medium;
        // Letters + digits + special (no upper) → Strong
        if (hasLetter && hasDigit && hasSpecial && !hasUpper)   return StrengthLevel.Strong;
        // All four (lower + upper + digit + special) → Super
        if (hasLower && hasUpper && hasDigit && hasSpecial)     return StrengthLevel.Super;
        // Fallback for edge cases (e.g. only digits)
        if (hasDigit && !hasLetter && !hasSpecial)              return StrengthLevel.Weak;
        return StrengthLevel.Weak;
    }

    private void UpdatePasswordStrength(string password)
    {
        StrengthLevel level = CalculateStrength(password);
        int filled = (int)level;

        // Reset all segments to base
        foreach (VisualElement seg in _strengthSegments)
        {
            if (seg == null) continue;
            seg.RemoveFromClassList("filled-weak");
            seg.RemoveFromClassList("filled-medium");
            seg.RemoveFromClassList("filled-strong");
            seg.RemoveFromClassList("filled-super");
        }

        string fillClass = level switch
        {
            StrengthLevel.Weak   => "filled-weak",
            StrengthLevel.Medium => "filled-medium",
            StrengthLevel.Strong => "filled-strong",
            StrengthLevel.Super  => "filled-super",
            _ => null,
        };

        if (fillClass != null)
        {
            for (int i = 0; i < filled && i < _strengthSegments.Length; i++)
            {
                if (_strengthSegments[i] != null)
                    _strengthSegments[i].AddToClassList(fillClass);
            }
        }

        if (_strengthLabel != null)
        {
            _strengthLabel.RemoveFromClassList("weak");
            _strengthLabel.RemoveFromClassList("medium");
            _strengthLabel.RemoveFromClassList("strong");
            _strengthLabel.RemoveFromClassList("super");
            _strengthLabel.RemoveFromClassList("none");
            switch (level)
            {
                case StrengthLevel.None:   _strengthLabel.text = "";              _strengthLabel.AddToClassList("none");   break;
                case StrengthLevel.Weak:   _strengthLabel.text = "Weak";          _strengthLabel.AddToClassList("weak");   break;
                case StrengthLevel.Medium: _strengthLabel.text = "Medium";        _strengthLabel.AddToClassList("medium"); break;
                case StrengthLevel.Strong: _strengthLabel.text = "Strong";        _strengthLabel.AddToClassList("strong"); break;
                case StrengthLevel.Super:  _strengthLabel.text = "Super Strong";  _strengthLabel.AddToClassList("super");  break;
            }
        }
    }

    // ── Submit ───────────────────────────────────────────────────────────────

    private void OnSignUpClicked()
    {
        string email    = _emailInput   != null ? _emailInput.value?.Trim()    : "";
        string nickname = _nicknameInput!= null ? _nicknameInput.value?.Trim() : "";
        string password = _passwordInput!= null ? _passwordInput.value         : "";
        string confirm  = _confirmInput != null ? _confirmInput.value          : "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(nickname) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirm))
        {
            ShowError("Semua field harus diisi");
            return;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            ShowError("Format email tidak valid");
            return;
        }

        if (nickname.Length < 3)
        {
            ShowError("Nickname minimal 3 karakter");
            return;
        }

        if (password.Length < 8)
        {
            ShowError("Password minimal 8 karakter");
            return;
        }

        if (password != confirm)
        {
            ShowError("Password tidak cocok");
            return;
        }

        HideError();
        SetLoading(true);

        if (FirebaseManager.Instance == null)
        {
            SetLoading(false);
            ShowError("Auth service belum siap. Coba lagi sebentar.");
            return;
        }

        FirebaseManager.Instance.SignUpWithEmail(email, password, nickname, (success, errorResp) =>
        {
            SetLoading(false);
            if (success)
            {
                AuthUIManager.Instance.ShowSuccess("Akun berhasil dibuat!");
                StartCoroutine(DelayedTransition(AuthUIManager.SCENE_LOGIN, 1.5f));
            }
            else
            {
                ShowError(ParseError(errorResp));
            }
        });
    }

    private string ParseError(string error)
    {
        if (string.IsNullOrEmpty(error))                       return "Terjadi kesalahan, coba lagi";
        if (error.Contains("EMAIL_EXISTS"))                    return "Email sudah terdaftar";
        if (error.Contains("INVALID_EMAIL"))                   return "Format email tidak valid";
        if (error.Contains("WEAK_PASSWORD"))                   return "Password terlalu lemah";
        if (error.Contains("TOO_MANY_ATTEMPTS"))               return "Terlalu banyak percobaan, coba lagi nanti";
        if (error.Contains("OPERATION_NOT_ALLOWED"))           return "Pendaftaran tidak diizinkan";
        if (error.Contains("Gagal"))                           return error;
        return "Terjadi kesalahan, coba lagi";
    }

    private IEnumerator DelayedTransition(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        AuthUIManager.Instance.TransitionToScene(sceneName);
    }

    private void OnGoToLogin()
    {
        AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_LOGIN);
    }

    private void OnPlayAsGuest()
    {
        AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_GUEST);
    }

    private void OnGoogleSignUpClicked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        HideError();
        AuthUIManager.Instance.ShowLoading(true);
        GoogleAuth_SignIn(gameObject.name);
#else
        ShowError("Google Sign-In hanya tersedia di WebGL build");
#endif
    }

    public void OnGoogleSignInSuccess(string json)
    {
        string accessToken = ExtractJsonField(json, "accessToken");
        FirebaseManager.Instance.SignInWithGoogle(accessToken, (success, error) =>
        {
            AuthUIManager.Instance.ShowLoading(false);
            if (success)
            {
                AuthUIManager.Instance.ShowSuccess("Login berhasil!");
                StartCoroutine(DelayedTransition(AuthUIManager.SCENE_GAME, 1.5f));
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetLoading(bool isLoading)
    {
        if (_signupButton != null) _signupButton.SetEnabled(!isLoading);
        if (_googleButton != null) _googleButton.SetEnabled(!isLoading);
    }

    private void ShowError(string msg)
    {
        if (_errorLabel == null) return;
        _errorLabel.text = msg;
        _errorLabel.AddToClassList("visible");
    }

    private void HideError()
    {
        if (_errorLabel == null) return;
        _errorLabel.text = "";
        _errorLabel.RemoveFromClassList("visible");
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
