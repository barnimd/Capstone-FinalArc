using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WebsiteSecurityController : MonoBehaviour
{
    [Header("=== Panels ===")]
    public GameObject popupPanel;
    public GameObject popupToggleVpn;
    public GameObject loginPanel;
    public GameObject successPanel;
    public GameObject attackPanel;
    public GameObject instantHackPanel;
    public GameObject feedbackPanel;

    [Header("=== Feedback Canvas ===")]
    public GameObject feedbackCanvas;


    [Header("=== Popup Buttons ===")]
    public Button btnActivate;
    public Button btnClose;

    [Header("=== VPN ===")]
    public Toggle toggleVPN;
    public TMP_Text txtPopupStatus;
    public Button btnConfirmActivate;

    [Header("=== Login ===")]
    public TMP_InputField inputUsername;
    public TMP_InputField inputPassword;
    public Button btnLogin;

    [Header("=== Feedback ===")]
    public TMP_Text txtFeedbackTitle;
    public TMP_Text txtFeedbackDetail;
    public Button btnFeedbackOk;

    [Header("=== UI ===")]
    public ObjectiveUI_Tp4 objectiveUI;

    [Header("=== Timing ===")]
    public float attackPanelDuration = 3f;
    public float instantHackDuration = 2f;

    [Header("=== Success Panel Fade ===")]
    public float successPanelFadeInDuration = 0.35f;
    public float successPanelFadeOutDuration = 0.35f;
    public float successPanelHoldDuration = 1.5f;

    [Header("=== Hack Panel Fade ===")]
    public float attackPanelFadeInDuration = 0.35f;
    public float instantHackFadeInDuration = 0.35f;
    public float feedbackCanvasFadeInDuration = 0.35f;

    [Header("=== Attack Progress ===")]
    public Slider attackProgressBar;
    public float attackProgressDuration = 1.2f;

    private bool _vpnActivated;
    private System.Action<bool> _onComplete;

    private void Awake()
    {
        if (btnActivate != null) btnActivate.onClick.AddListener(OnActivateClicked);
        if (btnClose != null) btnClose.onClick.AddListener(OnCloseClicked);
        if (btnConfirmActivate != null) btnConfirmActivate.onClick.AddListener(OnConfirmActivateClicked);
        if (toggleVPN != null) toggleVPN.onValueChanged.AddListener(OnToggleChanged);
        if (btnLogin != null) btnLogin.onClick.AddListener(OnLoginClicked);
        if (btnFeedbackOk != null) btnFeedbackOk.onClick.AddListener(OnFeedbackOkClicked);

        if (popupPanel != null) popupPanel.SetActive(false);
        if (popupToggleVpn != null) popupToggleVpn.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (successPanel != null) successPanel.SetActive(false);
        if (attackPanel != null) attackPanel.SetActive(false);
        if (instantHackPanel != null) instantHackPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }

    public void StartWebsitePhase(System.Action<bool> onComplete)
    {
        _onComplete = onComplete;
        _vpnActivated = false;

        if (objectiveUI != null)
            objectiveUI.ShowObjective("Koneksi tidak aman! Aktifkan VPN atau lanjutkan dengan resiko");

        popupPanel.SetActive(true);
        popupToggleVpn.SetActive(false);
        loginPanel.SetActive(false);
        successPanel.SetActive(false);
        attackPanel.SetActive(false);
        instantHackPanel.SetActive(false);
        feedbackPanel.SetActive(false);
    }

    private void OnActivateClicked()
    {
        popupPanel.SetActive(false);
        popupToggleVpn.SetActive(true);

        if (toggleVPN != null) toggleVPN.isOn = false;
        if (txtPopupStatus != null)
        {
            txtPopupStatus.text = "Not Secure";
            txtPopupStatus.color = Color.red;
        }
    }

    private void OnToggleChanged(bool isOn)
    {
        if (txtPopupStatus != null)
        {
            txtPopupStatus.text = isOn ? "Secure" : "Not Secure";
            txtPopupStatus.color = isOn ? Color.green : Color.red;
        }
    }

    private void OnConfirmActivateClicked()
    {
        if (toggleVPN != null && !toggleVPN.isOn) return;

        _vpnActivated = true;
        PlayerRunRecorder.RecordDetailed("vpn.choice", "enable", "protected_connection", 0);
        popupToggleVpn.SetActive(false);

        if (objectiveUI != null)
            objectiveUI.ShowObjective("Login untuk melanjutkan");

        loginPanel.SetActive(true);
    }

    private void OnCloseClicked()
    {
        _vpnActivated = false;
        PlayerRunRecorder.RecordDetailed("vpn.choice", "skip", "unprotected_connection", 0);
        popupPanel.SetActive(false);

        if (objectiveUI != null)
            objectiveUI.ShowObjective("Login untuk melanjutkan");

        loginPanel.SetActive(true);
    }

    private void OnLoginClicked()
    {
        PlayerRunRecorder.RecordDetailed(
            "public_wifi.login",
            _vpnActivated ? "with_vpn" : "without_vpn",
            _vpnActivated ? "credentials_protected" : "credentials_intercepted",
            0);
        loginPanel.SetActive(false);

        if (_vpnActivated)
        {
            StartCoroutine(SuccessRoutine());
        }
        else
        {
            StartCoroutine(HackRoutine());
        }
    }

    private IEnumerator SuccessRoutine()
    {
        successPanel.SetActive(true);
        yield return FadeCanvasGroup(successPanel, 0f, 1f, successPanelFadeInDuration);

        yield return new WaitForSeconds(successPanelHoldDuration);

        yield return FadeCanvasGroup(successPanel, 1f, 0f, successPanelFadeOutDuration);
        successPanel.SetActive(false);

        feedbackCanvas.SetActive(true);

        if (feedbackPanel != null && txtFeedbackTitle != null && txtFeedbackDetail != null)
        {
            feedbackPanel.SetActive(true);
            txtFeedbackTitle.text = "MISI SELESAI!";
            txtFeedbackDetail.text = "Kamu berhasil melindungi koneksi kamu dengan VPN.\n\nVPN membuat data kamu tetap aman saat menggunakan Wi-Fi publik,\n\nsehingga mengurangi risiko serangan hacker.";
        }
        else
        {
            _onComplete?.Invoke(true);
        }
    }

    private IEnumerator HackRoutine()
    {
        attackPanel.SetActive(true);
        yield return FadeCanvasGroup(attackPanel, 0f, 1f, attackPanelFadeInDuration);

        if (attackProgressBar != null) attackProgressBar.value = 0f;
        yield return AnimateAttackProgress(attackProgressDuration);

        float holdLeft = Mathf.Max(0f, attackPanelDuration - attackProgressDuration);
        yield return new WaitForSeconds(holdLeft);

        attackPanel.SetActive(false);

        instantHackPanel.SetActive(true);
        yield return FadeCanvasGroup(instantHackPanel, 0f, 1f, instantHackFadeInDuration);
        yield return new WaitForSeconds(instantHackDuration);
        instantHackPanel.SetActive(false);

        feedbackCanvas.SetActive(true);
        yield return FadeCanvasGroup(feedbackCanvas, 0f, 1f, feedbackCanvasFadeInDuration);

        if (feedbackPanel != null && txtFeedbackTitle != null && txtFeedbackDetail != null)
        {
            feedbackPanel.SetActive(true);
            txtFeedbackTitle.text = "GAME OVER!";
            txtFeedbackDetail.text = "Akun kamu berhasil diretas!\n\nTanpa VPN, hacker mencegat koneksi Wi-Fi publik dan mencuri data login kamu.";
        }
        else
        {
            _onComplete?.Invoke(false);
        }
    }

    private void OnFeedbackOkClicked()
    {
        feedbackPanel.SetActive(false);
        if (feedbackCanvas != null)
            feedbackCanvas.SetActive(false);

        _onComplete?.Invoke(_vpnActivated);
    }

    private IEnumerator AnimateAttackProgress(float duration)
    {
        if (attackProgressBar == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        attackProgressBar.value = 0f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            attackProgressBar.value = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / duration));
            yield return null;
        }
        attackProgressBar.value = 1f;
    }

    private IEnumerator FadeCanvasGroup(GameObject target, float from, float to, float duration)
    {
        if (target == null) yield break;

        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.AddComponent<CanvasGroup>();

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        cg.alpha = to;
    }
}
