using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SignUpController : MonoBehaviour
{
    // ─── Inspector References ─────────────────────────────────────────────────

    [Header("Inputs")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;

    [Header("Buttons")]
    [SerializeField] private Button signUpButton;
    [SerializeField] private Button goToLoginButton;
    [SerializeField] private Button showHidePasswordButton;
    [SerializeField] private Button showHideConfirmPasswordButton;
    [SerializeField] private Button googleSignInButton;

    [Header("Password Visibility Icons")]
    [SerializeField] private Image passwordEyeIcon;
    [SerializeField] private Image confirmPasswordEyeIcon;
    [SerializeField] private Sprite eyeOpenSprite;
    [SerializeField] private Sprite eyeClosedSprite;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private GameObject loadingSpinner;

    // ─── Google Client ID ─────────────────────────────────────────────────────

    private const string GoogleClientId = "211220332327-0o4c2mi8drpum0crjfvt8uosord5s65r.apps.googleusercontent.com";

    // ─── JS Bridge (WebGL only) ───────────────────────────────────────────────

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void GoogleAuth_Init(string clientId);
    [DllImport("__Internal")] private static extern void GoogleAuth_SignIn(string gameObjectName);
#endif

    // ─── State ────────────────────────────────────────────────────────────────

    private bool isPasswordVisible        = false;
    private bool isConfirmPasswordVisible = false;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    void Start()
    {
        signUpButton.onClick.AddListener(OnSignUpClicked);
        goToLoginButton.onClick.AddListener(OnGoToLoginClicked);
        showHidePasswordButton.onClick.AddListener(TogglePasswordVisibility);
        showHideConfirmPasswordButton.onClick.AddListener(ToggleConfirmPasswordVisibility);
        if (googleSignInButton != null) googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);

#if UNITY_WEBGL && !UNITY_EDITOR
        GoogleAuth_Init(GoogleClientId);
#endif

        // Start both fields hidden
        passwordInput.contentType        = TMP_InputField.ContentType.Password;
        confirmPasswordInput.contentType = TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();
        confirmPasswordInput.ForceLabelUpdate();

        if (errorText      != null) errorText.gameObject.SetActive(false);
        if (loadingSpinner != null) loadingSpinner.SetActive(false);
    }

    // ─── Password Visibility ──────────────────────────────────────────────────

    public void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;

        passwordInput.contentType = isPasswordVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        passwordInput.ForceLabelUpdate();

        if (passwordEyeIcon != null)
            passwordEyeIcon.sprite = isPasswordVisible ? eyeOpenSprite : eyeClosedSprite;
    }

    public void ToggleConfirmPasswordVisibility()
    {
        isConfirmPasswordVisible = !isConfirmPasswordVisible;

        confirmPasswordInput.contentType = isConfirmPasswordVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        confirmPasswordInput.ForceLabelUpdate();

        if (confirmPasswordEyeIcon != null)
            confirmPasswordEyeIcon.sprite = isConfirmPasswordVisible ? eyeOpenSprite : eyeClosedSprite;
    }

    // ─── Validation ───────────────────────────────────────────────────────────

    private bool ValidateInputs()
    {
        string email    = emailInput.text.Trim();
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;
        string confirm  = confirmPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirm))
        {
            ShowError("Semua field harus diisi");
            return false;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            ShowError("Format email tidak valid");
            return false;
        }

        if (password.Length < 8)
        {
            ShowError("Password minimal 8 karakter");
            return false;
        }

        if (password != confirm)
        {
            ShowError("Password tidak cocok");
            return false;
        }

        if (username.Length < 3)
        {
            ShowError("Username minimal 3 karakter");
            return false;
        }

        return true;
    }

    // ─── Sign Up ──────────────────────────────────────────────────────────────

    public void OnSignUpClicked()
    {
        if (!ValidateInputs()) return;

        HideError();
        AuthUIManager.Instance.ShowLoading(true);
        FirebaseManager.Instance.SignUpWithEmail(
            emailInput.text.Trim(),
            passwordInput.text,
            usernameInput.text.Trim(),
            OnSignUpResult
        );
    }

    private void OnSignUpResult(bool success, string errorResponse)
    {
        AuthUIManager.Instance.ShowLoading(false);

        if (success)
        {
            AuthUIManager.Instance.ShowSuccess("Akun berhasil dibuat!");
            StartCoroutine(DelayedTransition(AuthUIManager.SCENE_LOGIN, 1.5f));
        }
        else
        {
            ShowError(ParseFirebaseError(errorResponse));
        }
    }

    private string ParseFirebaseError(string errorResponse)
    {
        if (string.IsNullOrEmpty(errorResponse))       return "Terjadi kesalahan, coba lagi";
        if (errorResponse.Contains("EMAIL_EXISTS"))    return "Email sudah terdaftar";
        if (errorResponse.Contains("INVALID_EMAIL"))   return "Format email tidak valid";
        if (errorResponse.Contains("WEAK_PASSWORD"))   return "Password terlalu lemah";
        if (errorResponse.Contains("TOO_MANY_ATTEMPTS")) return "Terlalu banyak percobaan, coba lagi nanti";
        if (errorResponse.Contains("OPERATION_NOT_ALLOWED")) return "Pendaftaran tidak diizinkan";
        if (errorResponse.Contains("Gagal"))           return errorResponse;
        return "Terjadi kesalahan, coba lagi";
    }

    private IEnumerator DelayedTransition(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        AuthUIManager.Instance.TransitionToScene(sceneName);
    }

    // ─── Navigation ───────────────────────────────────────────────────────────

    public void OnGoToLoginClicked()
    {
        AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_LOGIN);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void ShowError(string message)
    {
        if (errorText == null) return;
        errorText.text = message;
        errorText.gameObject.SetActive(true);
    }

    private void HideError()
    {
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

    // ─── Google Sign-In ───────────────────────────────────────────────────────

    public void OnGoogleSignInClicked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        HideError();
        AuthUIManager.Instance.ShowLoading(true);
        GoogleAuth_SignIn(gameObject.name);
#else
        ShowError("Google Sign-In hanya tersedia di WebGL build");
#endif
    }

    // Called by JS via SendMessage
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
                Debug.LogWarning("[SignUpController] Google sign-in Firebase error: " + error);
            }
        });
    }

    // Called by JS via SendMessage
    public void OnGoogleSignInFailed(string error)
    {
        AuthUIManager.Instance.ShowLoading(false);
        if (error != "Sign-in dismissed" && error != "Dismissed")
            ShowError("Login Google gagal. Coba lagi.");
        Debug.LogWarning("[SignUpController] Google sign-in JS error: " + error);
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
