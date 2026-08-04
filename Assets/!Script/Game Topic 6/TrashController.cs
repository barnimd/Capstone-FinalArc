using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrashController : MonoBehaviour
{
    [Header("=== Panel ===")]
    public GameObject trashPanel;
    public Transform content;
    public GameObject trashItemPrefab;
    public TMP_Text txtEmpty;
    public TMP_Text txtSelected;
    public Button btnClose;
    public Button btnRestore;

    // FIX R2-A: track whether trash is locked (post-crash state)
    private bool _locked;
    private string _selected;
    private bool _done;

    void Awake()
    {
        if (btnClose) btnClose.onClick.AddListener(() => trashPanel.SetActive(false));
        if (btnRestore) btnRestore.onClick.AddListener(OnRestore);
        if (trashPanel) trashPanel.SetActive(false);
    }

    public void ShowTrash()
    {
        // FIX R3: jika trash dikunci (post-crash), langsung tampilkan kosong & nonaktifkan restore
        if (_locked)
        {
            _done = false; _selected = null;
            trashPanel.SetActive(true);
            trashPanel.transform.SetAsLastSibling();
            // Kosongkan list — seolah Trash hilang setelah crash
            foreach (Transform t in content) Destroy(t.gameObject);
            if (txtEmpty)
            {
                txtEmpty.text = "Trash is empty";
                txtEmpty.gameObject.SetActive(true);
            }
            if (txtSelected) txtSelected.text = "Selected: none";
            if (btnRestore) btnRestore.interactable = false;
            return;
        }

        Debug.Log("[TrashController] ShowTrash called, trashPanel=" + (trashPanel != null));
        _done = false; _selected = null;
        if (btnRestore) btnRestore.interactable = true;
        trashPanel.SetActive(true);
        trashPanel.transform.SetAsLastSibling();
        RefreshList();
    }

    // FIX R3: dipanggil GameManager saat crash selesai — kunci trash selamanya untuk sesi ini
    public void LockTrashAfterCrash()
    {
        _locked = true;
        // Jika panel kebetulan sedang terbuka, langsung nonaktifkan restore & kosongkan list
        if (trashPanel && trashPanel.activeSelf)
        {
            foreach (Transform t in content) Destroy(t.gameObject);
            if (txtEmpty)
            {
                txtEmpty.text = "Trash is empty";
                txtEmpty.gameObject.SetActive(true);
            }
            if (btnRestore) btnRestore.interactable = false;
        }
    }

    void RefreshList()
    {
        foreach (Transform t in content) Destroy(t.gameObject);
        var deleted = FileSystemManager.Instance.GetDeletedFiles();
        foreach (var f in deleted)
        {
            var go = Object.Instantiate(trashItemPrefab, content);
            go.SetActive(true);
            var btn = go.GetComponent<Button>();
            var txt = go.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = f.name;
            string fn = f.name;
            if (btn) btn.onClick.AddListener(() =>
            {
                _selected = fn;
                if (txtSelected) txtSelected.text = "Selected: " + fn;
            });
        }
        bool isEmpty = deleted.Count == 0;
        if (txtEmpty) txtEmpty.gameObject.SetActive(isEmpty);

        // FIX R2-A: nonaktifkan tombol Restore jika trash kosong
        if (btnRestore) btnRestore.interactable = !isEmpty;
    }

    void OnRestore()
    {
        if (_done) return;

        // FIX R2-A: harus ada file dipilih sebelum Restore bisa dilanjutkan
        if (string.IsNullOrEmpty(_selected))
        {
            if (txtSelected) txtSelected.text = "Pilih file terlebih dahulu!";
            return;
        }

        _done = true;
        FileSystemManager.Instance.RestoreFile(_selected);
        trashPanel.SetActive(false);
        GameManager_Tp6.Instance.OnRecoveryAttempted(_selected);
    }
}
