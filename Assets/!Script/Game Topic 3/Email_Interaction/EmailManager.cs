using System.Collections;
using UnityEngine;

public class EmailManager : MonoBehaviour
{
    [Header("Email Canvas")]
    [SerializeField] private GameObject emailCanvas;
    [SerializeField] private float fadeInDuration = 0.3f;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (emailCanvas != null)
        {
            _canvasGroup = emailCanvas.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = emailCanvas.AddComponent<CanvasGroup>();
        }
    }

    public void OnEmailIconClicked()
    {
        if (emailCanvas == null) return;
        StartCoroutine(FadeInEmailCanvas());
    }

    public void CloseEmailCanvas()
    {
        if (emailCanvas != null)
            emailCanvas.SetActive(false);
    }

    private IEnumerator FadeInEmailCanvas()
    {
        _canvasGroup.alpha = 0f;
        emailCanvas.SetActive(true);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
    }
}
