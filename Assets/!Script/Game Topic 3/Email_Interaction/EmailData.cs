using UnityEngine;

/// <summary>
/// Satu baris data email — dipakai oleh EmailList maupun EmailDetailPanel.
/// </summary>
[System.Serializable]
public class EmailEntry
{
    [Header("=== Tampilan List (Kotak Masuk) ===")]
    public string namaPengirim;   // contoh: "Ahmad", "BeliJual"
    public string subjek;         // contoh: "Meeting Besok - Jangan lupa..."
    public string waktu;          // contoh: "10:44"

    [Header("=== Tampilan Detail ===")]
    public string emailPengirim;  // contoh: "security@bel1jual.com"
    public string teksHeader;     // teks di banner berwarna, contoh: "Password akan Expired"
    public Color  warnaHeader = Color.red;
    [TextArea(4, 10)]
    public string isiEmail;       // body email lengkap

    [Header("=== Klasifikasi ===")]
    public bool isPhishing;       // true = phishing, false = email normal
}

/// <summary>
/// ScriptableObject berisi semua template email.
/// Buat via: klik kanan di Project → Create → Game → EmailData
/// </summary>
[CreateAssetMenu(fileName = "EmailData", menuName = "Game/EmailData")]
public class EmailData : ScriptableObject
{
    [Tooltip("Isi semua template email di sini. Randomizer akan memilih dari daftar ini.")]
    public EmailEntry[] semuaEmail;
}
