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

    void Awake()
    {
        if (btnYes) btnYes.onClick.AddListener(() =>
        {
            PlayerRunRecorder.RecordDetailed("backup.exists", "yes", "recovery_available", 20);
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
            PlayerRunRecorder.RecordDetailed("backup.recovery_source", "cloud", "restore_started", 20);
            recoveryPanel.SetActive(false);
            GameManager_Tp6.Instance.GoToState(Tp6State.BackupSetup);
        });

        if (btnExternal) btnExternal.onClick.AddListener(() =>
        {
            PlayerRunRecorder.RecordDetailed("backup.recovery_source", "external_drive", "restore_started", 20);
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
        bool scheduleSafe = sched == 2;
        bool correct = locationSafe && scheduleSafe;

        string locationId = loc == 1 ? "external_drive" : loc == 2 ? "cloud" : "same_disk";
        string scheduleId = sched == 2 ? "daily" : sched == 1 ? "weekly" : "never";

        // FIX: Tutup panel setup setelah user menekan tombol Save
        if (setupPanel) setupPanel.SetActive(false);

        GameManager_Tp6.Instance.OnBackupSetupDone(correct, locationId, scheduleId);
    }
}
