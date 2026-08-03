using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Attach to HUD_Canvas (always-active parent).
/// PausedPanel itself can start disabled.
/// </summary>
public class PausePanel : MonoBehaviour
{
    [Header("Pause Panel")]
    [SerializeField] private GameObject pausedPanel;

    [Header("Buttons")]
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnRetry;
    [SerializeField] private Button btnMainMenu;

    [Header("Settings Sub-Panel (opsional)")]
    [Tooltip("Panel setting in-game. Boleh kosong kalau scene ini belum punya.")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnSettingsBack;

    [Header("Main Menu Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset mainMenuSceneAsset;
#endif
    [HideInInspector] public string mainMenuSceneName;

    private bool _isPaused;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (mainMenuSceneAsset != null)
            mainMenuSceneName = mainMenuSceneAsset.name;
    }
#endif

    private void Start()
    {
        pausedPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        btnResume.onClick.AddListener(Resume);
        btnRetry.onClick.AddListener(Retry);
        btnMainMenu.onClick.AddListener(GoToMainMenu);

        if (btnSettings != null)     btnSettings.onClick.AddListener(OpenSettings);
        if (btnSettingsBack != null) btnSettingsBack.onClick.AddListener(CloseSettings);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Esc saat panel setting kebuka = balik ke menu pause, bukan resume
            if (settingsPanel != null && settingsPanel.activeSelf) CloseSettings();
            else if (_isPaused) Resume();
            else                Pause();
        }
    }

    private void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        pausedPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        pausedPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingsPanel == null) return;
        pausedPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(false);
        pausedPanel.SetActive(true);
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
            Debug.LogError("[PausePanel] Main menu scene belum di-assign di Inspector!");
    }
}
