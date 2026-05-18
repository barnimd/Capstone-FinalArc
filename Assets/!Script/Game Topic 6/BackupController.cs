using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BackupController : MonoBehaviour
{
    [Header("=== Question ===")]
    public GameObject questionPanel;
    public Button btnYes, btnNo;

    [Header("=== Recovery ===")]
    public GameObject recoveryPanel;
    public Button btnCloud, btnExternal;

    [Header("=== Setup ===")]
    public GameObject setupPanel;
    public TMP_Dropdown ddLocation, ddSchedule;
    public Button btnSave;

    // FIX R6: track sumber recovery yang dipilih player
    private bool _choseCloud;

    void Awake()
    {
        if (btnYes) btnYes.onClick.AddListener(() =>
        {
            questionPanel.SetActive(false);
            GameManager_Tp6.Instance.GoToState(Tp6State.RecoveryChoice);
        });

        if (btnNo) btnNo.onClick.AddListener(() =>
        {
            questionPanel.SetActive(false);
            GameManager_Tp6.Instance.OnBackupQuestionNo();
        });

        // FIX R6: catat pilihan sumber backup sebelum lanjut
        if (btnCloud) btnCloud.onClick.AddListener(() =>
        {
            _choseCloud = true;
            recoveryPanel.SetActive(false);
            GameManager_Tp6.Instance.GoToState(Tp6State.BackupSetup);
        });

        if (btnExternal) btnExternal.onClick.AddListener(() =>
        {
            _choseCloud = false;
            recoveryPanel.SetActive(false);
            GameManager_Tp6.Instance.GoToState(Tp6State.BackupSetup);
        });

        if (btnSave) btnSave.onClick.AddListener(OnSave);
        HideAll();
    }

    void HideAll()
    {
        if (questionPanel) questionPanel.SetActive(false);
        if (recoveryPanel) recoveryPanel.SetActive(false);
        if (setupPanel)    setupPanel.SetActive(false);
    }

    public void ShowBackupQuestion() { HideAll(); ShowPanel(questionPanel); }
    public void ShowRecoveryChoice() { HideAll(); ShowPanel(recoveryPanel); }
    public void ShowBackupSetup()    { HideAll(); ShowPanel(setupPanel); }

    void ShowPanel(GameObject p)
    {
        if (p) { p.SetActive(true); p.transform.SetAsLastSibling(); }
    }

    void OnSave()
    {
        int loc = ddLocation ? ddLocation.value : 0;
        int sched = ddSchedule ? ddSchedule.value : 0;

        bool locationSafe = (loc == 1 || loc == 2);
        bool scheduleSafe = (sched == 1 || sched == 2);
        bool correct = locationSafe && scheduleSafe;

        // FIX: Tutup panel setup setelah user menekan tombol Save
        if (setupPanel) setupPanel.SetActive(false);

        GameManager_Tp6.Instance.OnBackupSetupDone(correct);
    }
}
