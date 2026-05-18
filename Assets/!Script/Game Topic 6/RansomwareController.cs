using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RansomwareController : MonoBehaviour
{
    public GameObject ransomwarePanel;
    public TMP_Text txtTitle;
    public TMP_Text txtDetail;
    public Button btnOk;
    private System.Action _onDone;

    void Awake()
    {
        if (btnOk) btnOk.onClick.AddListener(OnOkClicked);
        if (ransomwarePanel) ransomwarePanel.SetActive(false);
    }

    // FIX R4: tombol OK hanya bisa diklik sekali untuk mencegah double-invoke
    private bool _done;

    void OnOkClicked()
    {
        if (_done) return;
        _done = true;
        ransomwarePanel.SetActive(false);
        _onDone?.Invoke();
    }

    public void Show(System.Action onDone)
    {
        _done = false;
        _onDone = onDone;
        if (txtTitle) txtTitle.text = "⚠ YOUR FILES HAVE BEEN ENCRYPTED! ⚠";
        if (txtDetail) txtDetail.text = "Ransomware telah mengenkripsi semua file.\n\nSemua dokumen dan folder tidak dapat diakses.\n\nHanya backup yang bisa menyelamatkan data kamu.";
        ransomwarePanel.SetActive(true);
        ransomwarePanel.transform.SetAsLastSibling();
    }
}
