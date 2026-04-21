using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginController : MonoBehaviour
{
    // ─── Inspector References ─────────────────────────────────────────────────
    // These are auto-resolved by name in Awake if not set in the Inspector.

    [Header("Input")]
    [SerializeField] private TMP_InputField usernameInput;

    [Header("Button")]
    [SerializeField] private Button loginButton;

    [Header("Feedback (optional)")]
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private GameObject loadingSpinner;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        // Auto-resolve references by GameObject name if not set in Inspector
        if (usernameInput == null)
        {
            GameObject go = GameObject.Find("input username");
            if (go != null) usernameInput = go.GetComponent<TMP_InputField>();
        }
        if (loginButton == null)
        {
            GameObject go = GameObject.Find("submit username btn");
            if (go != null) loginButton = go.GetComponent<Button>();
        }
    }

    void Start()
    {
        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginClicked);

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        if (loadingSpinner != null)
            loadingSpinner.SetActive(false);
    }

    // ─── Login ────────────────────────────────────────────────────────────────

    public void OnLoginClicked()
    {
        if (usernameInput == null)
        {
            Debug.LogError("[LoginController] usernameInput not found.");
            return;
        }

        string username = usernameInput.text.Trim();

        if (string.IsNullOrEmpty(username) || username.Length < 3)
        {
            ShowError("Username minimal 3 karakter");
            return;
        }

        HideError();
        SetLoading(true);

        FirebaseManager.Instance.SignInAsGuest(username, OnLoginResult);
    }

    private void OnLoginResult(bool success, string errorMessage)
    {
        SetLoading(false);

        if (success)
        {
            AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_GAME);
        }
        else
        {
            ShowError(errorMessage ?? "Gagal terhubung. Coba lagi.");
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void SetLoading(bool isLoading)
    {
        if (loginButton != null)    loginButton.interactable = !isLoading;
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
