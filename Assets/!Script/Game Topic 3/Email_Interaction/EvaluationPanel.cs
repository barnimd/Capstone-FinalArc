using System.Collections.Generic;
using MPUIKIT;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EvaluationPanel : MonoBehaviour
{
    [Header("=== Root Panel ===")]
    public GameObject evaluationPanelRoot;
    public GameObject emailCanvas;

    [Header("=== Question UI ===")]
    public GameObject questionContainer;
    public TMP_Text txtQuestionNumber;
    public TMP_Text txtQuestionText;
    public Button btnChoiceA;
    public Button btnChoiceB;
    public TMP_Text txtChoiceA;
    public TMP_Text txtChoiceB;
    public Button btnNext;

    [Header("=== Question UI - desain baru (opsional) ===")]
    [Tooltip("Label topik di header, mis. 'KEAMANAN EMAIL'. Kosongkan kalau teksnya sudah diisi manual.")]
    public string topicLabel = "";
    public TMP_Text txtTopicLabel;
    public Button btnBack;
    public RectTransform progressContainer;
    public GameObject progressSegmentTemplate;
    [Tooltip("Kalau diisi, teks pilihan TIDAK diberi prefix 'A. ' / 'B. ' (sudah ada badge).")]
    public bool useChoiceBadges;
    public MPImage badgeChoiceA;
    public MPImage badgeChoiceB;
    public TMP_Text badgeTextA;
    public TMP_Text badgeTextB;

    [Header("=== Result UI ===")]
    public GameObject resultContainer;
    public TMP_Text txtResultScore;
    public TMP_Text txtResultDetail;
    public Button btnSelesai;

    [Header("=== Result UI - desain baru (opsional) ===")]
    [Tooltip("Kalau diisi, txtResultScore hanya berisi jumlah benar dan field ini berisi '/total'.")]
    public TMP_Text txtResultScoreTotal;
    public RectTransform resultRowsContainer;
    public EvaluationResultRow_Tp4 resultRowTemplate;

    [Header("=== Choice Button Colors ===")]
    public Color defaultColor = Color.white;
    public Color selectedColor = new Color(1f, 0.76f, 0.2f);
    public Color choiceTextDefaultColor = new Color(0.957f, 0.929f, 0.890f, 1f);
    public Color choiceTextSelectedColor = new Color(0.957f, 0.929f, 0.890f, 1f);

    [Header("=== Choice Outline (MPImage) ===")]
    [Tooltip("0 = tanpa outline (dipakai desain lama).")]
    public float choiceOutlineWidth = 0f;
    public Color choiceOutlineDefaultColor = new Color(0f, 0f, 0f, 0f);
    public Color choiceOutlineSelectedColor = new Color(0.941f, 0.518f, 0.169f, 1f);

    [Header("=== Choice Badge Colors ===")]
    public Color badgeDefaultColor = new Color(0.180f, 0.149f, 0.129f, 1f);
    public Color badgeSelectedColor = new Color(0.941f, 0.518f, 0.169f, 1f);
    public Color badgeTextDefaultColor = new Color(0.710f, 0.655f, 0.612f, 1f);
    public Color badgeTextSelectedColor = new Color(0.106f, 0.071f, 0.024f, 1f);

    [Header("=== Progress Bar Colors ===")]
    public Color progressOnColor = new Color(0.941f, 0.518f, 0.169f, 1f);
    public Color progressOffColor = new Color(0.227f, 0.184f, 0.153f, 1f);

    private EvaluationData _data;
    private int _currentQuestionIndex = 0;
    private int _selectedAnswerIndex = -1;
    private int _correctCount = 0;
    private int[] _playerAnswers;
    private bool[] _recorded;
    private Image[] _choiceImages;

    private readonly List<Graphic> _progressSegments = new List<Graphic>();
    private readonly List<GameObject> _spawnedRows = new List<GameObject>();

    private void Awake()
    {
        _choiceImages = new Image[2];
        if (btnChoiceA != null) _choiceImages[0] = btnChoiceA.GetComponent<Image>();
        if (btnChoiceB != null) _choiceImages[1] = btnChoiceB.GetComponent<Image>();

        btnChoiceA.onClick.AddListener(() => PilihJawaban(0));
        btnChoiceB.onClick.AddListener(() => PilihJawaban(1));

        btnNext.onClick.AddListener(OnNextClicked);
        btnSelesai.onClick.AddListener(OnSelesaiClicked);

        if (btnBack != null) btnBack.onClick.AddListener(OnBackClicked);

        if (progressSegmentTemplate != null) progressSegmentTemplate.SetActive(false);
        if (resultRowTemplate != null) resultRowTemplate.gameObject.SetActive(false);
    }

    public void MulaiEvaluasi(EvaluationData data)
    {
        _data = data;
        _currentQuestionIndex = 0;
        _correctCount = 0;

        _playerAnswers = new int[data.questions.Length];
        _recorded = new bool[data.questions.Length];
        for (int i = 0; i < _playerAnswers.Length; i++)
            _playerAnswers[i] = -1;

        if (txtTopicLabel != null && !string.IsNullOrEmpty(topicLabel))
            txtTopicLabel.text = topicLabel;

        BangunProgress(data.questions.Length);

        questionContainer.SetActive(true);
        resultContainer.SetActive(false);
        evaluationPanelRoot.SetActive(true);

        TampilkanSoal(0);
    }

    private void TampilkanSoal(int index)
    {
        if (_data == null || index >= _data.questions.Length) return;

        _currentQuestionIndex = index;
        _selectedAnswerIndex = _playerAnswers[index];
        EvaluationQuestion soal = _data.questions[index];

        if (txtQuestionNumber != null)
            txtQuestionNumber.text = $"Pertanyaan {index + 1} dari {_data.questions.Length}";
        if (txtQuestionText != null)
            txtQuestionText.text = soal.questionText;

        // Desain baru: huruf A/B sudah tampil di badge, jadi teksnya polos.
        if (txtChoiceA != null) txtChoiceA.text = useChoiceBadges ? soal.choiceA : $"A. {soal.choiceA}";
        if (txtChoiceB != null) txtChoiceB.text = useChoiceBadges ? soal.choiceB : $"B. {soal.choiceB}";

        var label = btnNext.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = (index == _data.questions.Length - 1) ? "Selesai" : "Selanjutnya";

        if (btnBack != null) btnBack.interactable = index > 0;

        UpdateButtonColors();
        UpdateProgress();
        btnNext.interactable = _playerAnswers[index] >= 0;
    }

    private void PilihJawaban(int choiceIndex)
    {
        if (_data == null || _currentQuestionIndex >= _data.questions.Length) return;

        _playerAnswers[_currentQuestionIndex] = choiceIndex;
        _selectedAnswerIndex = choiceIndex;

        UpdateButtonColors();
        btnNext.interactable = true;
    }

    private void UpdateButtonColors()
    {
        for (int i = 0; i < 2; i++)
        {
            bool selected = i == _selectedAnswerIndex;

            if (_choiceImages[i] != null)
            {
                _choiceImages[i].color = selected ? selectedColor : defaultColor;

                var mp = _choiceImages[i] as MPImage;
                if (mp != null && choiceOutlineWidth > 0f)
                {
                    mp.OutlineWidth = choiceOutlineWidth;
                    mp.OutlineColor = selected ? choiceOutlineSelectedColor : choiceOutlineDefaultColor;
                }
            }

            MPImage badge = i == 0 ? badgeChoiceA : badgeChoiceB;
            if (badge != null) badge.color = selected ? badgeSelectedColor : badgeDefaultColor;

            TMP_Text badgeText = i == 0 ? badgeTextA : badgeTextB;
            if (badgeText != null) badgeText.color = selected ? badgeTextSelectedColor : badgeTextDefaultColor;

            TMP_Text choiceLabel = i == 0 ? txtChoiceA : txtChoiceB;
            if (choiceLabel != null && useChoiceBadges)
                choiceLabel.color = selected ? choiceTextSelectedColor : choiceTextDefaultColor;
        }
    }

    // ── Progress bar ──────────────────────────────────────────────────────────

    private void BangunProgress(int count)
    {
        if (progressContainer == null || progressSegmentTemplate == null) return;

        progressSegmentTemplate.SetActive(false);

        foreach (var seg in _progressSegments)
            if (seg != null) Destroy(seg.gameObject);
        _progressSegments.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(progressSegmentTemplate, progressContainer);
            go.name = $"Segment_{i + 1}";
            go.SetActive(true);
            var g = go.GetComponent<Graphic>();
            if (g != null) _progressSegments.Add(g);
        }
    }

    private void UpdateProgress()
    {
        for (int i = 0; i < _progressSegments.Count; i++)
            if (_progressSegments[i] != null)
                _progressSegments[i].color = i <= _currentQuestionIndex ? progressOnColor : progressOffColor;
    }

    // ── Navigasi ──────────────────────────────────────────────────────────────

    private void OnBackClicked()
    {
        if (_data == null || _currentQuestionIndex <= 0) return;
        TampilkanSoal(_currentQuestionIndex - 1);
    }

    private void OnNextClicked()
    {
        if (_data == null) return;

        int jawabanBenar = _data.questions[_currentQuestionIndex].correctAnswerIndex;
        bool correct = _playerAnswers[_currentQuestionIndex] == jawabanBenar;

        // Cuma direkam sekali per soal, biar tombol "Kembali" nggak bikin dobel.
        if (!_recorded[_currentQuestionIndex])
        {
            _recorded[_currentQuestionIndex] = true;
            PlayerRunRecorder.RecordDetailed(
                "evaluation.question_" + (_currentQuestionIndex + 1),
                "choice_" + (char)('a' + _playerAnswers[_currentQuestionIndex]),
                correct ? "correct" : "incorrect",
                0,
                "correct_choice", "choice_" + (char)('a' + jawabanBenar));
        }

        if (_currentQuestionIndex + 1 >= _data.questions.Length)
            TampilkanHasil();
        else
            TampilkanSoal(_currentQuestionIndex + 1);
    }

    // ── Hasil ─────────────────────────────────────────────────────────────────

    private void TampilkanHasil()
    {
        questionContainer.SetActive(false);
        resultContainer.SetActive(true);

        int total = _data.questions.Length;

        _correctCount = 0;
        for (int i = 0; i < total; i++)
            if (_playerAnswers[i] == _data.questions[i].correctAnswerIndex)
                _correctCount++;

        if (txtResultScoreTotal != null)
        {
            if (txtResultScore != null) txtResultScore.text = _correctCount.ToString();
            txtResultScoreTotal.text = $"/{total}";
        }
        else if (txtResultScore != null)
        {
            txtResultScore.text = $"Skor: {_correctCount} / {total}";
        }

        if (resultRowsContainer != null && resultRowTemplate != null)
            BangunBarisHasil();
        else if (txtResultDetail != null)
            txtResultDetail.text = BuatDetailTeks();
    }

    private string BuatDetailTeks()
    {
        string detail = "";
        for (int i = 0; i < _data.questions.Length; i++)
        {
            bool benar = _playerAnswers[i] == _data.questions[i].correctAnswerIndex;
            string icon = benar ? "[OK]" : "[X]";
            detail += $"{icon} Soal {i + 1}: {_data.questions[i].explanation}\n\n";
        }
        return detail;
    }

    private void BangunBarisHasil()
    {
        resultRowTemplate.gameObject.SetActive(false);

        foreach (var go in _spawnedRows)
            if (go != null) Destroy(go);
        _spawnedRows.Clear();

        int total = _data.questions.Length;
        for (int i = 0; i < total; i++)
        {
            EvaluationQuestion soal = _data.questions[i];
            bool benar = _playerAnswers[i] == soal.correctAnswerIndex;

            string jawaban = "-";
            if (_playerAnswers[i] == 0) jawaban = soal.choiceA;
            else if (_playerAnswers[i] == 1) jawaban = soal.choiceB;

            var row = Instantiate(resultRowTemplate, resultRowsContainer);
            row.name = $"Row_{i + 1}";
            row.gameObject.SetActive(true);
            row.Bind(i + 1, soal.questionText, benar, jawaban, i < total - 1);
            _spawnedRows.Add(row.gameObject);
        }
    }

    private void OnSelesaiClicked()
    {
        evaluationPanelRoot.SetActive(false);

        if (EvaluationManager.Instance != null)
            EvaluationManager.Instance.SelesaiEvaluasi();

        if (emailCanvas != null) emailCanvas.SetActive(false);
    }

    public int GetCorrectCount() => _correctCount;
    public int GetTotalQuestions() => _data != null ? _data.questions.Length : 0;
}
