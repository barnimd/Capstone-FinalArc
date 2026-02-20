using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InstallerFlow : MonoBehaviour
{
    public GameObject confirmPopup;
    public GameObject progressPanel;
    public GameObject securityPopup;

    public Slider progressBar;
    public TMP_Text statusText;

    public int score = 0;

    public void StartInstall()
    {
        confirmPopup.SetActive(false);
        progressPanel.SetActive(true);
        StartCoroutine(InstallProcess());
    }

    IEnumerator InstallProcess()
    {
        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime / 3f; // 3 detik loading
            progressBar.value = progress;

            if (progress < 0.3f)
                statusText.text = "Extracting files...";
            else if (progress < 0.6f)
                statusText.text = "Granting permissions...";
            else
                statusText.text = "Connecting to server...";

            yield return null;
        }

        progressPanel.SetActive(false);

        score -= 10;
        Debug.Log("Data Bocor! Score: " + score);

        securityPopup.SetActive(true);
    }

    public void CancelInstall()
    {
        confirmPopup.SetActive(false);
        score += 10;
        Debug.Log("Instalasi dibatalkan. Score: " + score);
    }

    public void CloseSecurityPopup()
    {
        securityPopup.SetActive(false);
    }
}