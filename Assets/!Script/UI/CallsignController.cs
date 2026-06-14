using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the username-only "callsign" quick-play screen.
/// Mirrors the LoginController / SignUpController pattern but calls
/// FirebaseManager.SignInAsGuest (anonymous Firebase + unique-per-day callsign).
/// </summary>
public class CallsignController : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField callsignInput;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button goToSignInButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private TextMeshProUGUI charCounterText;
    [SerializeField] private GameObject loadingSpinner;

    // ─── Validation rules ─────────────────────────────────────────────────────

    private const int MinLength = 3;
    private const int MaxLength = 16;
    private static readonly Regex Alphanumeric = new Regex("^[a-zA-Z0-9]+$");

    void Start()
    {
        if (playButton        != null) playButton.onClick.AddListener(OnPlayClicked);
        if (goToSignInButton  != null) goToSignInButton.onClick.AddListener(OnGoToSignInClicked);

        if (callsignInput != null)
        {
            callsignInput.characterLimit = MaxLength;
            callsignInput.onValueChanged.AddListener(OnCallsignChanged);
        }

        HideError();
        UpdateCharCounter("");
        if (loadingSpinner != null) loadingSpinner.SetActive(false);
    }

    private void OnCallsignChanged(string value)
    {
        UpdateCharCounter(value);
        HideError();
    }

    private void UpdateCharCounter(string value)
    {
        if (charCounterText == null) return;
        int len = value != null ? value.Length : 0;
        charCounterText.text = len == 0
            ? MinLength + "-" + MaxLength + " chars"
            : len + "/" + MaxLength + " chars";
    }

    public void OnPlayClicked()
    {
        string callsign = callsignInput != null ? callsignInput.text.Trim() : "";

        if (string.IsNullOrEmpty(callsign))
        {
            ShowError("Callsign tidak boleh kosong");
            return;
        }

        if (callsign.Length < MinLength)
        {
            ShowError("Callsign minimal " + MinLength + " karakter");
            return;
        }

        if (callsign.Length > MaxLength)
        {
            ShowError("Callsign maksimal " + MaxLength + " karakter");
            return;
        }

        if (!Alphanumeric.IsMatch(callsign))
        {
            ShowError("Callsign hanya boleh huruf dan angka");
            return;
        }

        HideError();
        SetLoading(true);

        FirebaseManager.Instance.SignInAsGuest(callsign, OnGuestResult);
    }

    private void OnGuestResult(bool success, string errorMessage)
    {
        SetLoading(false);

        if (success)
        {
            AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_GAME);
        }
        else
        {
            ShowError(string.IsNullOrEmpty(errorMessage)
                ? "Terjadi kesalahan, coba lagi"
                : errorMessage);
        }
    }

    public void OnGoToSignInClicked()
    {
        AuthUIManager.Instance.TransitionToScene(AuthUIManager.SCENE_LOGIN);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void SetLoading(bool isLoading)
    {
        if (playButton     != null) playButton.interactable = !isLoading;
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
