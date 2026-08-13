using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class AuthUIManager : MonoBehaviour
{
    private static AuthUIManager _instance;
    public static AuthUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("AuthUIManager");
                _instance = go.AddComponent<AuthUIManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ─── Scene Constants ─────────────────────────────────────────────────────

    public static readonly string SCENE_LOGIN  = "LoginScene(W password)";
    public static readonly string SCENE_SIGNUP = "SignUpScene new";
    public static readonly string SCENE_GUEST  = "LoginScene";
    public static readonly string SCENE_GAME   = "All_Menu";

    // One-time character pick. All three auth paths route here first when
    // PlayerProfile.HasChosenGender is false, then on to SCENE_GAME.
    public static readonly string SCENE_CHARACTER = "CharacterSelect";

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
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
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
        // Gate the game scene behind the one-time character pick.
        //
        // Done here rather than at each call site because eight different places
        // across five auth controllers transition into SCENE_GAME. Patching each
        // one means any auth path added later silently skips the pick. Gating the
        // destination catches every path, including future ones.
        if (sceneName == SCENE_GAME && !PlayerProfile.HasChosenGender)
        {
            StartCoroutine(RouteAfterAuth());
            return;
        }

        StartCoroutine(TransitionCoroutine(sceneName));
    }

    /// <summary>
    /// Decide between CharacterSelect and the game for a player whose local cache
    /// says they have not picked yet.
    ///
    /// The cache alone is not enough: someone who picked on another browser has an
    /// empty cache here, and sending them back through the pick screen would let
    /// them choose a character the server will then refuse to change. So confirm
    /// with Neon first, and only show the screen if it really is their first time.
    /// </summary>
    private IEnumerator RouteAfterAuth()
    {
        string destination = SCENE_CHARACTER;

        if (APIClient.Instance != null)
        {
            string displayName = FirebaseManager.Instance != null
                ? FirebaseManager.Instance.Username
                : PlayerProfile.DisplayName;

            PlayerProfile.SetDisplayName(displayName);

            bool done = false;
            APIClient.Instance.UserSync(displayName, (ok, resp, raw) =>
            {
                if (ok && resp != null && resp.user != null)
                {
                    PlayerProfile.ApplyFromServer(resp.user);
                    if (PlayerProfile.HasChosenGender) destination = SCENE_GAME;
                }
                else
                {
                    // Offline or the call failed. Showing the pick screen is the safer
                    // wrong answer: at worst a returning player re-picks and the server
                    // keeps their original choice. Skipping it would strand a genuinely
                    // new player without a character.
                    Debug.LogWarning("[AuthUIManager] UserSync failed before routing: " + raw);
                }
                done = true;
            });

            yield return new WaitUntil(() => done);
        }

        yield return TransitionCoroutine(destination);
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
