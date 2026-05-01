using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Ensure ScoreManager Awakes before any other script that calls AddScore
[DefaultExecutionOrder(-100)]
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;
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
        score = 0;
        Debug.Log("[ScoreManager] Initialized. Score = " + score);
    }

    public void AddScore(int value)
    {
        if (value == 0) return; // no-op, skip log noise
        score += value;
        Debug.Log("[ScoreManager] AddScore(" + value + ") → Score = " + score);
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }
}
