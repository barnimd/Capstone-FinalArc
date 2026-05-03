using UnityEngine;

[System.Serializable]
public class EvaluationQuestion_Tp4
{
    [TextArea(3, 6)]
    public string questionText;

    public string choiceA;
    public string choiceB;

    [Tooltip("Index jawaban benar: 0=A, 1=B")]
    [Range(0, 1)]
    public int correctAnswerIndex;

    [TextArea(2, 4)]
    public string explanation;
}

[CreateAssetMenu(fileName = "EvaluationData_Tp4", menuName = "Game/Evaluation Data Tp4")]
public class EvaluationData_Tp4 : ScriptableObject
{
    [Tooltip("Isi 5 pertanyaan evaluasi di sini.")]
    public EvaluationQuestion_Tp4[] questions = new EvaluationQuestion_Tp4[5];
}
