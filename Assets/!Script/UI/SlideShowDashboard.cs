using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlideshowController : MonoBehaviour
{
    [Header("Slideshow Images")]
    public Sprite[] slides;

    [Header("UI References")]
    public Image displayImage;
    public GameObject arrowLeft;
    public GameObject arrowRight;

    [Header("Auto Slide Settings")]
    public float slideInterval = 3f;
    public bool loop = true;

    private int currentIndex = 0;

    void Start()
    {
        if (slides.Length > 0)
        {
            UpdateSlide();
            StartCoroutine(AutoSlide());
        }
    }

    IEnumerator AutoSlide()
    {
        while (true)
        {
            yield return new WaitForSeconds(slideInterval);

            if (currentIndex < slides.Length - 1)
                currentIndex++;
            else if (loop)
                currentIndex = 0;
            else
                yield break; // stop if no loop and at last slide

            UpdateSlide();
        }
    }

    public void NextSlide()
    {
        if (currentIndex < slides.Length - 1)
        {
            currentIndex++;
            UpdateSlide();
            RestartAutoSlide(); // reset timer after manual click
        }
    }

    public void PrevSlide()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateSlide();
            RestartAutoSlide(); // reset timer after manual click
        }
    }

    void RestartAutoSlide()
    {
        StopAllCoroutines();
        StartCoroutine(AutoSlide());
    }

    void UpdateSlide()
    {
        displayImage.sprite = slides[currentIndex];
    }
}