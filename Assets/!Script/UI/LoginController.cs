using System.Collections;
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
        if (loginButton    != null) loginButton.onClick.AddListener(OnLoginClicked);
        if (goToSignUpButton != null) goToSignUpButton.onClick.AddListener(OnGoToSignUpClicked);

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
}
