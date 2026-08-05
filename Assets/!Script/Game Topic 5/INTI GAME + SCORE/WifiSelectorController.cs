using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WifiSelectorController : MonoBehaviour
{
    [Header("=== Panels ===")]
    public GameObject popupPanel;
    public GameObject popupListWifi;
    public GameObject confirmationPanel;
    public GameObject feedbackPanel;


    [Header("=== Feedback Canvas ===")]
    public GameObject feedbackCanvas;

    [Header("=== Wi-Fi List Texts ===")]
    public TMP_Text txtListWifiDetail_0;
    public TMP_Text txtListWifiDetail_1;
    public TMP_Text txtListWifiDetail_2;

    [Header("=== Buttons ===")]
    public Button btnClose;
    public Button btnConnect;
    public Button btnAccept;
    public Button btnCancel;
    public Button btnFeedbackOk;

    [Header("=== Feedback ===")]
    public TMP_Text txtFeedbackTitle;
    public TMP_Text txtFeedbackDetail;

    [Header("=== UI ===")]
    public ObjectiveUI_Tp4 objectiveUI;

    [Header("=== Wi-Fi Data ===")]
    [TextArea(1, 2)] public string correctWifiName = "CafeBean_Free";
    [TextArea(1, 2)] public string fakeWifiName_1    = "CafeBean_Free_5G";
    [TextArea(1, 2)] public string fakeWifiName_2    = "FREE_CAFE_WIFI";

    private List<WifiOption> _wifiOptions;
    private int _selectedIndex = -1;
    private int _correctIndex;
    private System.Action<bool> _onComplete;

    private struct WifiOption
    {
        public string name;
        public bool isCorrect;
    }

    private void Awake()
    {
        if (btnClose != null) btnClose.onClick.AddListener(OnCloseClicked);
        if (btnConnect != null) btnConnect.onClick.AddListener(OnConnectClicked);
        if (btnAccept != null) btnAccept.onClick.AddListener(OnAcceptClicked);
        if (btnCancel != null) btnCancel.onClick.AddListener(OnCancelClicked);
        if (btnFeedbackOk != null) btnFeedbackOk.onClick.AddListener(OnFeedbackOkClicked);

        if (popupPanel != null) popupPanel.SetActive(false);
        if (popupListWifi != null) popupListWifi.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        WireWifiItems();
    }

    private void WireWifiItems()
    {
        string[] itemNames = { "Wifi_Item0", "Wifi_Item1", "Wifi_Item2" };
        for (int i = 0; i < itemNames.Length; i++)
        {
            Transform parent = transform.Find(itemNames[i]);
            if (parent == null) continue;

            Image img = parent.GetComponent<Image>();
            if (img == null)
            {
                img = parent.gameObject.AddComponent<Image>();
                img.color = new Color(1, 1, 1, 0.15f);
            }
            img.raycastTarget = true;

            Button btn = parent.GetComponent<Button>();
            if (btn == null) btn = parent.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;

            int index = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectWifi(index));

            Debug.Log($"[WifiSelector] {itemNames[i]} wired (Button + Image).");
        }
    }

    public void StartWifiPhase(System.Action<bool> onComplete)
    {
        _onComplete = onComplete;
        popupPanel.SetActive(true);
        popupListWifi.SetActive(false);
        confirmationPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }

    private void OnCloseClicked()
    {
        popupPanel.SetActive(false);
        popupListWifi.SetActive(true);
        GenerateWifiList();
        PopulateWifiList();

        if (objectiveUI != null)
            objectiveUI.ShowObjective("Pilih jaringan Wi-Fi yang aman");
    }

    private void GenerateWifiList()
    {
        _wifiOptions = new List<WifiOption>
        {
            new WifiOption { name = correctWifiName, isCorrect = true },
            new WifiOption { name = fakeWifiName_1,  isCorrect = false },
            new WifiOption { name = fakeWifiName_2,  isCorrect = false }
        };
        _wifiOptions = new List<WifiOption>(_wifiOptions);
        Acak(_wifiOptions);
        _correctIndex = _wifiOptions.FindIndex(o => o.isCorrect);
        _selectedIndex = -1;
    }

    private void PopulateWifiList()
    {
        string[] itemNames = { "Wifi_Item0", "Wifi_Item1", "Wifi_Item2" };

        for (int i = 0; i < itemNames.Length; i++)
        {
            Transform parent = transform.Find(itemNames[i]);
            if (parent == null) continue;

            TMP_Text text = parent.GetComponentInChildren<TMP_Text>();
            if (text == null) continue;

            if (i < _wifiOptions.Count)
            {
                text.text = _wifiOptions[i].name;
                SetItemColor(parent, Color.white);
            }
        }
    }

    public void SelectWifi(int index)
    {
        if (index < 0 || index >= _wifiOptions.Count) return;
        _selectedIndex = index;

        string[] itemNames = { "Wifi_Item0", "Wifi_Item1", "Wifi_Item2" };
        for (int i = 0; i < itemNames.Length; i++)
        {
            Transform parent = transform.Find(itemNames[i]);
            if (parent == null) continue;
            Color c = (i == index) ? new Color(1f, 0.76f, 0.2f) : new Color(1, 1, 1, 0.15f);
            SetItemColor(parent, c);
        }
    }

    private void SetItemColor(Transform parent, Color color)
    {
        Image img = parent.GetComponent<Image>();
        if (img != null) img.color = color;

        Button btn = parent.GetComponent<Button>();
        if (btn != null)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = color;
            cb.highlightedColor = color;
            cb.pressedColor = color;
            cb.selectedColor = color;
            btn.colors = cb;
        }
    }

    private void OnConnectClicked()
    {
        if (_selectedIndex < 0) return;
        confirmationPanel.SetActive(true);
    }

    private void OnAcceptClicked()
    {
        if (_selectedIndex < 0) return;
        bool isCorrect = _wifiOptions[_selectedIndex].isCorrect;
        string selectedNetworkId = ResolveNetworkId(_wifiOptions[_selectedIndex].name);
        PlayerRunRecorder.RecordDetailed(
            "wifi.selection",
            selectedNetworkId,
            isCorrect ? "trusted_network" : "rogue_network_game_over",
            0,
            "classification", isCorrect ? "legitimate" : "lookalike");
        confirmationPanel.SetActive(false);
        popupListWifi.SetActive(false);

        if (isCorrect)
        {
            _onComplete?.Invoke(true);
        }
        else
        {
            feedbackCanvas.SetActive(true);

            if (feedbackPanel != null && txtFeedbackTitle != null && txtFeedbackDetail != null)
            {
                feedbackPanel.SetActive(true);
                txtFeedbackTitle.text = "GAME OVER!";
                txtFeedbackDetail.text = $"Kamu terhubung ke jaringan palsu: {_wifiOptions[_selectedIndex].name}\n\nIni adalah Wi-Fi publik berbahaya! Hacker bisa mencuri data kamu.";
            }
            else
            {
                Debug.Log($"[WifiSelector] FAIL: {_wifiOptions[_selectedIndex].name}");
                _onComplete?.Invoke(false);
            }
        }
    }

    private void OnCancelClicked()
    {
        confirmationPanel.SetActive(false);
    }

    private void OnFeedbackOkClicked()
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        _onComplete?.Invoke(false);
    }

    private static void Acak<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private string ResolveNetworkId(string networkName)
    {
        if (networkName == correctWifiName) return "cafebean_free";
        if (networkName == fakeWifiName_1) return "cafebean_free_5g";
        if (networkName == fakeWifiName_2) return "free_cafe_wifi";
        return "unknown_network";
    }
}
