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

        btnResume.onClick.AddListener(Resume);
        btnRetry.onClick.AddListener(Retry);
        btnMainMenu.onClick.AddListener(GoToMainMenu);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused) Resume();
            else           Pause();
        }
    }

    private void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        pausedPanel.SetActive(true);
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        pausedPanel.SetActive(false);
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
