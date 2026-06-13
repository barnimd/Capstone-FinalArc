using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SummaryReturnToMenu : MonoBehaviour
{
    private const string MenuSceneName = "All_Menu";

    private Button _button;
    private bool _isLoading;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(ReturnToMenu);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(ReturnToMenu);
    }

    public void ReturnToMenu()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        Time.timeScale = 1f;

        if (MusicManager.Instance != null)
            MusicManager.Instance.StopImmediately();

        SceneManager.LoadScene(MenuSceneName);
    }
}
