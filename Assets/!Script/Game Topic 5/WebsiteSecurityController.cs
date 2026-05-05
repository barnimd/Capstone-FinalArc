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
        popupToggleVpn.SetActive(false);

        if (objectiveUI != null)
            objectiveUI.ShowObjective("Login untuk melanjutkan");

        loginPanel.SetActive(true);
    }

    private void OnCloseClicked()
    {
        _vpnActivated = false;
        popupPanel.SetActive(false);

        if (objectiveUI != null)
            objectiveUI.ShowObjective("Login untuk melanjutkan");

        loginPanel.SetActive(true);
    }

    private void OnLoginClicked()
    {
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
        yield return new WaitForSeconds(2f);
        successPanel.SetActive(false);

        feedbackCanvas.SetActive(true);

        if (feedbackPanel != null && txtFeedbackTitle != null && txtFeedbackDetail != null)
        {
            feedbackPanel.SetActive(true);
            txtFeedbackTitle.text = "MISI SELESAI!";
            txtFeedbackDetail.text = "Kamu berhasil melindungi koneksi dengan VPN.\n\nData login kamu aman dari serangan MITM!";
        }
        else
        {
            _onComplete?.Invoke(true);
        }
    }

    private IEnumerator HackRoutine()
    {
        attackPanel.SetActive(true);
        yield return new WaitForSeconds(attackPanelDuration);
        attackPanel.SetActive(false);

        instantHackPanel.SetActive(true);
        yield return new WaitForSeconds(instantHackDuration);
        instantHackPanel.SetActive(false);

        feedbackCanvas.SetActive(true);

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
}
