using System.Collections;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Topic5CameraFramingZone : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private float roomOrthographicSize = 3f;
    [SerializeField] private float transitionDuration = 0.35f;

    private float _defaultOrthographicSize;
    private Coroutine _transition;

    private void Awake()
    {
        if (virtualCamera == null)
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if (virtualCamera != null)
            _defaultOrthographicSize = virtualCamera.m_Lens.OrthographicSize;

        BoxCollider2D zone = GetComponent<BoxCollider2D>();
        zone.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            SetOrthographicSize(roomOrthographicSize);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            SetOrthographicSize(_defaultOrthographicSize);
    }

    private void SetOrthographicSize(float targetSize)
    {
        if (virtualCamera == null)
            return;

        if (_transition != null)
            StopCoroutine(_transition);

        _transition = StartCoroutine(TransitionLens(targetSize));
    }

    private IEnumerator TransitionLens(float targetSize)
    {
        float startSize = virtualCamera.m_Lens.OrthographicSize;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetLens(Mathf.Lerp(startSize, targetSize, Mathf.Clamp01(elapsed / transitionDuration)));
            yield return null;
        }

        SetLens(targetSize);
        _transition = null;
    }

    private void SetLens(float orthographicSize)
    {
        LensSettings lens = virtualCamera.m_Lens;
        lens.OrthographicSize = orthographicSize;
        virtualCamera.m_Lens = lens;
    }
}
