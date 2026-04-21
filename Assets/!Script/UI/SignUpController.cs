using System.Collections;
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

    [Header("Password Visibility Icons")]
    [SerializeField] private Image passwordEyeIcon;
    [SerializeField] private Image confirmPasswordEyeIcon;
    [SerializeField] private Sprite eyeOpenSprite;
    [SerializeField] private Sprite eyeClosedSprite;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private GameObject loadingSpinner;

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

    private void OnSignUpResult(bool success)
    {
        AuthUIManager.Instance.ShowLoading(false);

        if (success)
        {
            AuthUIManager.Instance.ShowSuccess("Akun berhasil dibuat!");
            StartCoroutine(DelayedTransition(AuthUIManager.SCENE_LOGIN, 1.5f));
        }
        else
        {
            AuthUIManager.Instance.ShowError("Email sudah terdaftar atau terjadi kesalahan");
        }
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
}
