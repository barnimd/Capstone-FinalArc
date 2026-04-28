using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlideshowController : MonoBehaviour
{
    [Header("Slideshow Images")]
    public Sprite[] slides; // Masukkan semua gambar slideshow di sini

    [Header("UI References")]
    public Image displayImage;     // Image component yang menampilkan slide
    public GameObject arrowLeft;   // Tombol panah kiri
    public GameObject arrowRight;  // Tombol panah kanan

    private int currentIndex = 0;

    void Start()
    {
        if (slides.Length > 0)
            UpdateSlide();
    }

    public void NextSlide()
    {
        if (currentIndex < slides.Length - 1)
        {
            currentIndex++;
            UpdateSlide();
        }
    }

    public void PrevSlide()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateSlide();
        }
    }

    void UpdateSlide()
    {
        displayImage.sprite = slides[currentIndex];

    }
}