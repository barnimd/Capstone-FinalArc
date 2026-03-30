using UnityEngine;

[CreateAssetMenu(fileName = "LessonData", menuName = "SECMIND/LessonData")]
public class LessonData : ScriptableObject
{
    public string title;
    public Sprite icon;
    public bool isUnlocked;
}
