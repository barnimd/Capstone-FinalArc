using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Ensure ScoreManager Awakes before any other script that calls AddScore
[DefaultExecutionOrder(-100)]
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Tooltip("Skor awal saat game mulai. Default 0 (skor aditif). Topic 1 dimulai dari 100 dan berkurang saat pemain salah.")]
    public int startingScore = 0;
    public int score;
    public TextMeshProUGUI scoreText; // Optional HUD display — can be left unassigned

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        score = startingScore;
        if (scoreText != null)
            scoreText.text = "Score: " + score;
        Debug.Log("[ScoreManager] Initialized. Score = " + score);
    }

    public void AddScore(int value)
    {
        if (value == 0) return; // no-op, skip log noise
        SetScore(score + value);
        Debug.Log("[ScoreManager] AddScore(" + value + ") → Score = " + score);
    }

    public int SetScore(int value)
    {
        score = value;
        RefreshDisplay();
        return score;
    }

    public int ClampScore(int min, int max)
    {
        return SetScore(Mathf.Clamp(score, min, max));
    }

    private void RefreshDisplay()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }
}
