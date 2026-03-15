using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginController : MonoBehaviour
{
    // ─── Inspector References ─────────────────────────────────────────────────

    [Header("Inputs")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Buttons")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button goToSignUpButton;
    [SerializeField] private Button showHidePasswordButton;

    [Header("Password Visibility")]
    [SerializeField] private Image passwordEyeIcon;
    [SerializeField] private Sprite eyeOpenSprite;
    [SerializeField] private Sprite eyeClosedSprite;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private GameObject loadingSpinner;

    // ─── State ────────────────────────────────────────────────────────────────

    private bool isPasswordVisible = false;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
        goToSignUpButton.onClick.AddListener(OnGoToSignUpClicked);
        showHidePasswordButton.onClick.AddListener(TogglePasswordVisibility);

        // Start with password hidden
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();

        if (errorText != null) errorText.gameObject.SetActive(false);
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

    // ─── Login ────────────────────────────────────────────────────────────────

    public void OnLoginClicked()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Username dan password tidak boleh kosong");
            return;
        }

        HideError();
        AuthUIManager.Instance.ShowLoading(true);
        FirebaseManager.Instance.SignInWithEmail(username, password, OnLoginResult);
    }

    private void OnLoginResult(bool success)
    {
        AuthUIManager.Instance.ShowLoading(false);

        if (success)
        {
            AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_GAME);
        }
        else
        {
            AuthUIManager.Instance.ShowError("Username atau password salah");
        }
    }

    // ─── Navigation ───────────────────────────────────────────────────────────

    public void OnGoToSignUpClicked()
    {
        AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_SIGNUP);
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
}
