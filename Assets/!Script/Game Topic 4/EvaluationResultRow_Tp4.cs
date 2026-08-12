using MPUIKIT;
using TMPro;
using UnityEngine;

/// <summary>
/// Satu baris di ResultPanel (rekap evaluasi). Dipakai sebagai template lalu di-Instantiate
/// oleh <see cref="EvaluationPanel_Tp4"/> untuk tiap soal.
/// </summary>
public class EvaluationResultRow_Tp4 : MonoBehaviour
{
    [Header("=== Refs ===")]
    public MPImage rowBg;
    public MPImage iconBox;
    public GameObject iconCheck;
    public GameObject iconCross;
    public TMP_Text txtIndex;
    public TMP_Text txtQuestion;
    public MPImage answerPill;
    public TMP_Text txtAnswer;
    public GameObject divider;

    [Header("=== Warna: Benar ===")]
    public Color correctRowBg = new Color(0f, 0f, 0f, 0f);
    public Color correctIconBg = new Color(0.078f, 0.216f, 0.169f, 1f);
    public Color correctPillBg = new Color(0.086f, 0.204f, 0.161f, 1f);
    public Color correctPillText = new Color(0.290f, 0.871f, 0.612f, 1f);

    [Header("=== Warna: Salah ===")]
    public Color wrongRowBg = new Color(0.133f, 0.086f, 0.059f, 1f);
    public Color wrongIconBg = new Color(0.231f, 0.129f, 0.075f, 1f);
    public Color wrongPillBg = new Color(0.231f, 0.129f, 0.075f, 1f);
    public Color wrongPillText = new Color(0.941f, 0.518f, 0.169f, 1f);

    public void Bind(int number, string questionText, bool correct, string answerText, bool showDivider)
    {
        if (txtIndex != null) txtIndex.text = number.ToString("00");
        if (txtQuestion != null) txtQuestion.text = questionText;
        if (txtAnswer != null) txtAnswer.text = answerText;

        if (iconCheck != null) iconCheck.SetActive(correct);
        if (iconCross != null) iconCross.SetActive(!correct);

        if (rowBg != null) rowBg.color = correct ? correctRowBg : wrongRowBg;
        if (iconBox != null) iconBox.color = correct ? correctIconBg : wrongIconBg;
        if (answerPill != null) answerPill.color = correct ? correctPillBg : wrongPillBg;
        if (txtAnswer != null) txtAnswer.color = correct ? correctPillText : wrongPillText;

        if (divider != null) divider.SetActive(showDivider);
    }
}
