using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InstallDecision : MonoBehaviour
{
    public int safeScore = 0;
    public int penaltyScore = -25;

    public void Install()
    {
        if (ScoreManager.instance != null)
            ScoreManager.instance.AddScore(penaltyScore);
        else
            Debug.LogWarning("[InstallDecision] ScoreManager not found — score not applied.");
        CloseAll();
    }

    public void Cancel()
    {
        if (ScoreManager.instance != null)
            ScoreManager.instance.AddScore(safeScore);
        CloseAll();
    }

    void CloseAll()
    {
        transform.parent.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }
}