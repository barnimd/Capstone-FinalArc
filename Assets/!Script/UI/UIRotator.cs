using UnityEngine;

public class UIRotator : MonoBehaviour
{
    [SerializeField] private float speed = 360f;

    void Update()
    {
        transform.Rotate(0f, 0f, -speed * Time.deltaTime);
    }
}
