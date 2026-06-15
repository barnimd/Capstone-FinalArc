using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginController : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Buttons")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button goToSignUpButton;
    [SerializeField] private Button googleSignInButton;
    [SerializeField] private Button playAsGuestButton;

    // ─── Google Client ID ─────────────────────────────────────────────────────

    private const string GoogleClientId = "211220332327-0o4c2mi8drpum0crjfvt8uosord5s65r.apps.googleusercontent.com";

    // ─── JS Bridge (WebGL only) ───────────────────────────────────────────────

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void GoogleAuth_Init(string clientId);
    [DllImport("__Internal")] private static extern void GoogleAuth_SignIn(string gameObjectName);
#endif

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private GameObject loadingSpinner;

    void Awake()
    {
        if (errorText == null)
        {
            GameObject go = GameObject.Find("Error");
            if (go != null) errorText = go.GetComponent<TextMeshProUGUI>();
        }
    }

    void Start()
    {
        if (loginButton      != null) loginButton.onClick.AddListener(OnLoginClicked);
        if (goToSignUpButton != null) goToSignUpButton.onClick.AddListener(OnGoToSignUpClicked);
        if (googleSignInButton != null) googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);
        if (playAsGuestButton != null) playAsGuestButton.onClick.AddListener(OnPlayAsGuestClicked);

#if UNITY_WEBGL && !UNITY_EDITOR
        GoogleAuth_Init(GoogleClientId);
#endif

        if (passwordInput != null)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            passwordInput.ForceLabelUpdate();
        }

        HideError();
        if (loadingSpinner != null) loadingSpinner.SetActive(false);
    }

    public void OnLoginClicked()
    {
        string username = usernameInput != null ? usernameInput.text.Trim() : "";
        string password = passwordInput != null ? passwordInput.text         : "";

        if (string.IsNullOrEmpty(username))
        {
            ShowError("Username tidak boleh kosong");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowError("Password tidak boleh kosong");
            return;
        }

        HideError();
        SetLoading(true);

        FirebaseManager.Instance.SignInWithUsernameAndPassword(username, password, OnLoginResult);
    }

    private void OnLoginResult(bool success, string errorResponse)
    {
        SetLoading(false);

        if (success)
        {
            AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_GAME);
        }
        else
        {
            ShowError(ParseError(errorResponse));
        }
    }

    private string ParseError(string error)
    {
        if (string.IsNullOrEmpty(error))                    return "Terjadi kesalahan, coba lagi";
        if (error.Contains("tidak ditemukan"))              return error;
        if (error.Contains("Gagal"))                        return error;
        if (error.Contains("INVALID_PASSWORD"))             return "Password salah";
        if (error.Contains("INVALID_LOGIN_CREDENTIALS"))   return "Username atau password salah";
        if (error.Contains("USER_DISABLED"))                return "Akun dinonaktifkan";
        if (error.Contains("TOO_MANY_ATTEMPTS"))            return "Terlalu banyak percobaan, coba lagi nanti";
        return "Username atau password salah";
    }

    public void OnGoToSignUpClicked()
    {
        AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_SIGNUP);
    }

    public void OnPlayAsGuestClicked()
    {
        AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_GUEST);
    }

    private void SetLoading(bool isLoading)
    {
        if (loginButton    != null) loginButton.interactable = !isLoading;
        if (loadingSpinner != null) loadingSpinner.SetActive(isLoading);
    }

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
                Debug.LogWarning("[LoginController] Google sign-in Firebase error: " + error);
            }
        });
    }

    // Called by JS via SendMessage
    public void OnGoogleSignInFailed(string error)
    {
        AuthUIManager.Instance.ShowLoading(false);
        if (error != "Sign-in dismissed" && error != "Dismissed")
            ShowError("Login Google gagal. Coba lagi.");
        Debug.LogWarning("[LoginController] Google sign-in JS error: " + error);
    }

    private IEnumerator DelayedTransition(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        AuthUIManager.Instance.TransitionToScene(sceneName);
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
