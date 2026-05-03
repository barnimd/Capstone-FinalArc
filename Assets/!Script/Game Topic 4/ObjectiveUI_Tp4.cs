using System.Collections;
using UnityEngine;
using TMPro;

public class ObjectiveUI_Tp4 : MonoBehaviour
{
    [Header("=== UI Reference ===")]
    public TMP_Text objectiveText;
    public GameObject panelRoot;

    [Header("=== Animation Settings ===")]
    public float highlightDuration = 5f;
    public float moveDuration = 0.5f;

    [Header("=== Panel Positions ===")]
    public Vector2 centerPosition = Vector2.zero;
    public Vector2 cornerPosition = new Vector2(-10, -10);
    public Vector2 centerSize = new Vector2(600, 100);
    public Vector2 cornerSize = new Vector2(300, 50);

    private RectTransform _rect;
    private Coroutine _moveRoutine;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;
        _rect = panelRoot.GetComponent<RectTransform>();
        if (_rect == null)
            _rect = GetComponent<RectTransform>();
    }

    public void ShowObjective(string text)
    {
        if (objectiveText != null)
            objectiveText.text = text;

        panelRoot.SetActive(true);

        if (_rect != null)
        {
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = centerPosition;
            _rect.sizeDelta = centerSize;
        }

        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(MoveToCornerRoutine());
    }

    private IEnumerator MoveToCornerRoutine()
    {
        yield return new WaitForSeconds(highlightDuration);
        if (_rect == null) yield break;

        _rect.anchorMin = new Vector2(1, 1);
        _rect.anchorMax = new Vector2(1, 1);

        Vector2 startPos = _rect.anchoredPosition;
        Vector2 startSize = _rect.sizeDelta;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / moveDuration));
            _rect.anchoredPosition = Vector2.Lerp(startPos, cornerPosition, t);
            _rect.sizeDelta = Vector2.Lerp(startSize, cornerSize, t);
            yield return null;
        }

        _rect.anchoredPosition = cornerPosition;
        _rect.sizeDelta = cornerSize;
    }
}
