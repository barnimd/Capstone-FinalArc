using UnityEngine;

[System.Serializable]
public class EvaluationQuestion
{
    [TextArea(3, 6)]
    public string questionText;

    public string choiceA;
    public string choiceB;
    public string choiceC;
    public string choiceD;

    [Tooltip("Index jawaban benar: 0=A, 1=B, 2=C, 3=D")]
    [Range(0, 3)]
    public int correctAnswerIndex;

    [TextArea(2, 4)]
    public string explanation;
}

[CreateAssetMenu(fileName = "EvaluationData_Tp3", menuName = "Game/Evaluation Data Tp3")]
public class EvaluationData : ScriptableObject
{
    [Tooltip("Isi 5 pertanyaan evaluasi di sini.")]
    public EvaluationQuestion[] questions = new EvaluationQuestion[5];
}
