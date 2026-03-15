using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class AuthUIManager : MonoBehaviour
{
    public static AuthUIManager Instance { get; private set; }

    // ─── Scene Constants ─────────────────────────────────────────────────────

    public static readonly string SCENE_LOGIN  = "LoginScene";
    public static readonly string SCENE_SIGNUP = "SignUpScene";
    public static readonly string SCENE_GAME   = "MainGame";

    // ─── Inspector References ─────────────────────────────────────────────────

    [Header("Loading")]
    [SerializeField] private GameObject loadingOverlay;

    [Header("Error")]
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Success")]
    [SerializeField] private GameObject successPanel;
    [SerializeField] private TextMeshProUGUI successText;

    [Header("Scene Transition")]
    [SerializeField] private CanvasGroup fadeOverlay;

    private const float FadeDuration      = 0.3f;
    private const float ErrorDisplayTime  = 3f;
    private const float SuccessDisplayTime = 2f;

    private Coroutine errorCoroutine;
    private Coroutine successCoroutine;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure overlay starts invisible and non-blocking
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha          = 0f;
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable   = false;
        }

        HideAll();
    }

    // ─── Loading ──────────────────────────────────────────────────────────────

    public void ShowLoading(bool show)
    {
        if (loadingOverlay != null)
            loadingOverlay.SetActive(show);
    }

    // ─── Error ────────────────────────────────────────────────────────────────

    public void ShowError(string message)
    {
        if (errorCoroutine != null)
            StopCoroutine(errorCoroutine);

        if (errorText != null)   errorText.text = message;
        if (errorPanel != null)  errorPanel.SetActive(true);

        errorCoroutine = StartCoroutine(AutoHide(errorPanel, ErrorDisplayTime));
    }

    // ─── Success ──────────────────────────────────────────────────────────────

    public void ShowSuccess(string message)
    {
        if (successCoroutine != null)
            StopCoroutine(successCoroutine);

        if (successText != null)   successText.text = message;
        if (successPanel != null)  successPanel.SetActive(true);

        successCoroutine = StartCoroutine(AutoHide(successPanel, SuccessDisplayTime));
    }

    // ─── Scene Transition ─────────────────────────────────────────────────────

    public void TransitionToScene(string sceneName)
    {
        StartCoroutine(TransitionCoroutine(sceneName));
    }

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        if (fadeOverlay == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        // Block input during transition
        fadeOverlay.blocksRaycasts = true;
        fadeOverlay.interactable   = true;

        // Fade in (black)
        float elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed             += Time.deltaTime;
            fadeOverlay.alpha    = Mathf.Clamp01(elapsed / FadeDuration);
            yield return null;
        }
        fadeOverlay.alpha = 1f;

        SceneManager.LoadScene(sceneName);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private IEnumerator AutoHide(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (panel != null)
            panel.SetActive(false);
    }

    private void HideAll()
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(false);
        if (errorPanel     != null) errorPanel.SetActive(false);
        if (successPanel   != null) successPanel.SetActive(false);
    }
}
