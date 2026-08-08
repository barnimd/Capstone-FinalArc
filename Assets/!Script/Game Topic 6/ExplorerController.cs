using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExplorerController : MonoBehaviour
{
    [Header("=== Panel ===")]
    public GameObject explorerPanel;
    public Button btnClose;

    [Header("=== Content ===")]
    public Transform fileListContainer;
    public FileItem[] poolItems;

    [Header("=== Folder Labels ===")]
    public TMP_Text labelWork, labelPersonal, labelDownloads, labelImp, labelTrash;

    [Header("=== Warning ===")]
    public GameObject warningPanel;
    public TMP_Text txtWarning;
    public Button btnWarningOk;

    private bool _lost;

    void Awake()
    {
        if (btnClose) btnClose.onClick.AddListener(() => explorerPanel.SetActive(false));
        if (btnWarningOk) btnWarningOk.onClick.AddListener(OnWarningOk);
        if (explorerPanel) explorerPanel.SetActive(false);
        if (warningPanel) warningPanel.SetActive(false);

        if (poolItems == null || poolItems.Length == 0)
            poolItems = fileListContainer != null ? fileListContainer.GetComponentsInChildren<FileItem>(true) : new FileItem[0];
    }

    void Start()
    {
        FileSystemManager.Instance.OnDataChanged += RefreshUI;
        FileSystemManager.Instance.OnImportantLost += OnImportantLost;
    }

    public void ShowExplorer()
    {
        _lost = false;
        explorerPanel.SetActive(true);
        explorerPanel.transform.SetAsLastSibling();
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (!explorerPanel || !explorerPanel.activeSelf) return;

        var files = FileSystemManager.Instance.allFiles;

        for (int i = 0; i < poolItems.Length; i++)
        {
            if (i >= files.Count)
            {
                poolItems[i].gameObject.SetActive(false);
                continue;
            }

            bool show = !files[i].isDeleted && files[i].location == FileLocation.Desktop;
            poolItems[i].gameObject.SetActive(show);
            if (show)
            {
                poolItems[i].Setup(files[i]);
                WireRow(poolItems[i]);
            }
        }

        UpdateLabels();
    }

    void WireRow(FileItem fi)
    {
        if (fi.renameBtn)
        {
            fi.renameBtn.onClick.RemoveAllListeners();
            fi.renameBtn.onClick.AddListener(() =>
            {
                string nn = fi.inputRename?.text;
                if (!string.IsNullOrWhiteSpace(nn) && fi.file != null)
                    FileSystemManager.Instance.RenameFile(fi.file.name, nn);
            });
        }
        if (fi.deleteBtn)
        {
            fi.deleteBtn.onClick.RemoveAllListeners();
            fi.deleteBtn.onClick.AddListener(() =>
            {
                if (fi.file != null)
                    FileSystemManager.Instance.DeleteFile(fi.file.name);
            });
        }
    }

    void UpdateLabels()
    {
        if (labelWork) labelWork.text = "Work Files (" + FileSystemManager.Instance.GetFilesAt(FileLocation.WorkFiles).Count + ")";
        if (labelPersonal) labelPersonal.text = "Personal (" + FileSystemManager.Instance.GetFilesAt(FileLocation.Personal).Count + ")";
        if (labelDownloads) labelDownloads.text = "Downloads (" + FileSystemManager.Instance.GetFilesAt(FileLocation.Downloads).Count + ")";
        if (labelImp) labelImp.text = "Important_Project";
        if (labelTrash) labelTrash.text = "Trash (" + FileSystemManager.Instance.GetDeletedFiles().Count + ")";
    }

    void OnImportantLost()
    {
        if (_lost) return; _lost = true;
        if (explorerPanel) explorerPanel.SetActive(false);
        warningPanel.SetActive(true);
        txtWarning.text = "File penting hilang!\n\nImportant_Project telah dihapus, dipindahkan, atau direname.";
    }

    void OnWarningOk()
    {
        warningPanel.SetActive(false);
        GameManager_Tp6.Instance.OnImportantLost();
    }
}
