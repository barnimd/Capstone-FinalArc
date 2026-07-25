using UnityEngine;

[CreateAssetMenu(fileName = "LessonData", menuName = "SECMIND/LessonData")]
public class LessonData : ScriptableObject
{
    public string title;
    public Sprite icon;
    public bool isUnlocked;
    public string sceneName;

    // isUnlocked hanya default bawaan. Lock state sebenarnya dipegang server dan
    // bisa diubah admin lewat Menu Admin > Manage Class — baca selalu lewat sini.
    public bool IsUnlocked => LessonLocks.IsUnlocked(this);
}
