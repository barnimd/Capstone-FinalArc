using UnityEngine;

[System.Serializable]
public class PopupEntry
{
    [Tooltip("Judul popup, contoh: WARNING - Virus Detected!")]
    public string title;

    [TextArea(3, 6)]
    [Tooltip("Isi pesan popup")]
    public string message;

    [Tooltip("True jika ini popup phishing/fake, false jika legitimate/real")]
    public bool isPhishing;

    [Tooltip("Teks tombol aksi yang benar (Accept untuk real, Close untuk phishing)")]
    public string correctActionLabel;

    [Tooltip("Teks tombol aksi yang berbahaya (Click Here untuk phishing, Report untuk real)")]
    public string wrongActionLabel;

    [TextArea(2, 3)]
    [Tooltip("Penjelasan kenapa ini phishing atau real")]
    public string explanation;
}

[CreateAssetMenu(fileName = "PopupData_Tp4", menuName = "Game/Popup Data Tp4")]
public class PopupData_Tp4 : ScriptableObject
{
    [Tooltip("Pool popup phishing/fake (min 3 direkomendasikan)")]
    public PopupEntry[] phishingPopups;

    [Tooltip("Pool popup legitimate/real (min 2 direkomendasikan)")]
    public PopupEntry[] legitimatePopups;
}
