using UnityEngine;

public class LoadingSpinner : MonoBehaviour
{
    [SerializeField] private RectTransform spinnerImage;
    [SerializeField] private float rotationSpeed = 200f;

    void Awake()
    {
        if (spinnerImage == null)
            spinnerImage = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (spinnerImage != null)
            spinnerImage.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }
}
