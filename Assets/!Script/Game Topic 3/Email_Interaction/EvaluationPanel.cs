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

    [Header("=== Result UI ===")]
    public GameObject resultContainer;
    public TMP_Text txtResultScore;
    public TMP_Text txtResultDetail;
    public Button btnSelesai;

    [Header("=== Choice Button Colors ===")]
    public Color defaultColor = Color.white;
    public Color selectedColor = new Color(1f, 0.76f, 0.2f);

    private EvaluationData _data;
    private int _currentQuestionIndex = 0;
    private int _selectedAnswerIndex = -1;
    private int _correctCount = 0;
    private int[] _playerAnswers;
    private Image[] _choiceImages;

    private void Awake()
    {
        _choiceImages = new Image[2];
        if (btnChoiceA != null) _choiceImages[0] = btnChoiceA.GetComponent<Image>();
        if (btnChoiceB != null) _choiceImages[1] = btnChoiceB.GetComponent<Image>();

        btnChoiceA.onClick.AddListener(() => PilihJawaban(0));
        btnChoiceB.onClick.AddListener(() => PilihJawaban(1));

        btnNext.onClick.AddListener(OnNextClicked);
        btnSelesai.onClick.AddListener(OnSelesaiClicked);
    }

    public void MulaiEvaluasi(EvaluationData data)
    {
        _data = data;
        _currentQuestionIndex = 0;
        _correctCount = 0;

        _playerAnswers = new int[data.questions.Length];
        for (int i = 0; i < _playerAnswers.Length; i++)
            _playerAnswers[i] = -1;

        questionContainer.SetActive(true);
        resultContainer.SetActive(false);
        evaluationPanelRoot.SetActive(true);

        TampilkanSoal(0);
    }

    private void TampilkanSoal(int index)
    {
        if (_data == null || index >= _data.questions.Length) return;

        _selectedAnswerIndex = _playerAnswers[index];
        EvaluationQuestion soal = _data.questions[index];

        txtQuestionNumber.text = $"Pertanyaan {index + 1} dari {_data.questions.Length}";
        txtQuestionText.text = soal.questionText;

        txtChoiceA.text = $"A. {soal.choiceA}";
        txtChoiceB.text = $"B. {soal.choiceB}";

        var label = btnNext.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = (index == _data.questions.Length - 1) ? "Selesai" : "Selanjutnya";

        UpdateButtonColors();
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
            if (_choiceImages[i] != null)
                _choiceImages[i].color = (i == _selectedAnswerIndex) ? selectedColor : defaultColor;
        }
    }

    private void OnNextClicked()
    {
        if (_data == null) return;

        int jawabanBenar = _data.questions[_currentQuestionIndex].correctAnswerIndex;
        bool correct = _playerAnswers[_currentQuestionIndex] == jawabanBenar;
        PlayerRunRecorder.RecordDetailed(
            "evaluation.question_" + (_currentQuestionIndex + 1),
            "choice_" + (char)('a' + _playerAnswers[_currentQuestionIndex]),
            correct ? "correct" : "incorrect",
            0,
            "correct_choice", "choice_" + (char)('a' + jawabanBenar));

        if (correct)
            _correctCount++;

        _currentQuestionIndex++;

        if (_currentQuestionIndex >= _data.questions.Length)
        {
            TampilkanHasil();
        }
        else
        {
            TampilkanSoal(_currentQuestionIndex);
        }
    }

    private void TampilkanHasil()
    {
        questionContainer.SetActive(false);
        resultContainer.SetActive(true);

        int total = _data.questions.Length;
        txtResultScore.text = $"Skor: {_correctCount} / {total}";

        string detail = "";
        for (int i = 0; i < _data.questions.Length; i++)
        {
            bool benar = _playerAnswers[i] == _data.questions[i].correctAnswerIndex;
            string icon = benar ? "[OK]" : "[X]";
            detail += $"{icon} Soal {i + 1}: {_data.questions[i].explanation}\n\n";
        }

        txtResultDetail.text = detail;
    }

    private void OnSelesaiClicked()
    {
        evaluationPanelRoot.SetActive(false);

        if (EvaluationManager.Instance != null)
            EvaluationManager.Instance.SelesaiEvaluasi();

            emailCanvas.SetActive(false);
    }

    public int GetCorrectCount() => _correctCount;
    public int GetTotalQuestions() => _data != null ? _data.questions.Length : 0;
}
