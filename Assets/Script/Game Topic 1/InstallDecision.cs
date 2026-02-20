using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InstallDecision : MonoBehaviour
{
    public int safeScore = 10;
    public int penaltyScore = -10;

    public void Install()
    {
        ScoreManager.instance.AddScore(penaltyScore);
        CloseAll();
    }

    public void Cancel()
    {
        ScoreManager.instance.AddScore(safeScore);
        CloseAll();
    }

    void CloseAll()
    {
        transform.parent.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }
}