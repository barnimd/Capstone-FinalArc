using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrashController : MonoBehaviour
{
    public GameObject crashOverlay;
    public Image crashBg;
    public TMP_Text crashText;

    void Awake() { if (crashOverlay) crashOverlay.SetActive(false); }

    public void PlayCrash(System.Action onDone) { StartCoroutine(CrashRoutine(onDone)); }

    IEnumerator CrashRoutine(System.Action onDone)
    {
        if (!crashOverlay) { onDone?.Invoke(); yield break; }
        crashOverlay.SetActive(true);
        crashOverlay.transform.SetAsLastSibling();

        // Freeze
        if (crashBg) crashBg.color = new Color(0, 0, 0, 0.95f);
        if (crashText) crashText.text = "";
        yield return new WaitForSeconds(1.5f);

        // Glitch
        string[] lines = { "ERROR 0x000000F", "SYSTEM FAILURE", "KERNEL PANIC", "0xDEADBEEF", "CRITICAL PROCESS DIED" };
        for (int i = 0; i < 5; i++)
        {
            crashBg.color = (i % 2 == 0) ? new Color(0.9f, 0, 0, 0.9f) : new Color(0, 0, 0, 0.95f);
            crashText.text = lines[Random.Range(0, lines.Length)];
            yield return new WaitForSeconds(0.15f);
        }

        // Shutdown
        crashBg.color = Color.black;
        crashText.text = "SHUTTING DOWN...";
        yield return new WaitForSeconds(1.5f);

        // Reboot
        if (crashText) crashText.text = "Rebooting...";
        yield return new WaitForSeconds(1f);

        // Jalankan callback DULU sebelum mematikan diri sendiri
        if (GameManager_Tp6.Instance != null)
            GameManager_Tp6.Instance.OnCrashFinished();

        onDone?.Invoke();

        // Matikan overlay terakhir
        crashOverlay.SetActive(false);
    }
}
