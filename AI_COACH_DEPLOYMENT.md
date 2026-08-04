# SecMind Post-Game AI Coach — Deployment & Handoff Guide

Dokumen ini menjelaskan status implementasi, arsitektur, file yang berubah, konfigurasi server, migrasi database, proses deployment, pengujian, dan troubleshooting fitur **Post-Game AI Coach**.

Terakhir diperbarui: 4 Agustus 2026.

## 1. Status Saat Ini

Yang sudah selesai:

- AI coach terhubung ke summary panel bersama untuk Topic 1–6.
- Layout summary menjadi dua kolom: skor di kiri dan chat AI di kanan.
- Tombol akhir hanya `Back to Menu` dan `Retry`.
- Pilihan penting pemain dicatat sebagai event terstruktur dan dikirim sebagai konteks AI.
- Backend session, chat, validasi, fallback, penyimpanan Neon, dan cleanup session sudah dibuat.
- Autentikasi memakai Firebase ID token.
- Unit test backend semantic-context lulus **15/15**.
- Kompilasi C# lulus dengan **0 error**. Warning lama dari dependency/serialized field masih ada.

Yang masih harus dilakukan sebelum production:

- Review dan commit hanya file AI coach yang tercantum dalam dokumen ini.
- Jalankan migrasi `docs/db/schema_ai_coach.sql` pada Neon.
- Tambahkan dan verifikasi environment variables di Vercel.
- Deploy backend ke Preview dan lakukan smoke test.
- Build ulang Unity WebGL ke folder `docs/`.
- Pastikan WebGL build tidak menghapus file backend.
- Lakukan test end-to-end Topic 1–6 dengan akun Firebase yang login.
- Deploy ke Production dan monitor log serta penggunaan token.

> Penting: perubahan AI coach masih berada di working tree lokal dan belum menjadi deployment production sampai sudah di-commit, WebGL dibangun ulang, dan branch yang dipakai Vercel sudah di-push.

## 2. Alur Sistem

```text
Gameplay Topic 1–6
        |
        v
PlayerRunRecorder mencatat contentVersion + eventId + choiceId + outcomeId + scoreDelta + fakta aman + waktu
        |
        v
SummaryManager membuka PostGameAICoachController
        |
        v
Unity mengirim snapshot run + Firebase ID token
        |
        v
Vercel API memverifikasi user dan memvalidasi payload
        |
        +----> Neon menyimpan ai_sessions dan ai_messages
        |
        v
DeepSeek V4 Flash menerima system prompt topic + konteks run
        |
        v
Backend memvalidasi JSON sebelum dikirim ke Unity
```

Unity tidak menerima API key, system prompt, atau riwayat user lain. Semua hal sensitif tetap berada di server.

### Semantic context v2

`contentVersion` aktif adalah `v2-2026-08-04`. Backend memperkaya stable ID menjadi scenario, pilihan yang diambil, semua pilihan alternatif, assessment, outcome, evidence catalog, risk indicators, dan fakta run. Legacy ID dari WebGL lama tetap diterima selama rollout, tetapi build baru harus mengirim versi v2.

## 3. Stage ID yang Dipakai

Stage ID harus sama di Unity dan backend.

| Topic | Stage ID | Materi |
|---|---|---|
| Topic 1 | `phishing` | Phishing & Social Engineering |
| Topic 2 | `2fa` | Password Security & MFA |
| Topic 3 | `password-security` | Email & Password Security |
| Topic 4 | `malware-awareness` | Malware & Website Awareness |
| Topic 5 | `wifi-security` | Wi-Fi & Website Security |
| Topic 6 | `ransomware` | Ransomware & Backup |

Jangan mengganti stage ID hanya di salah satu sisi. Payload akan ditolak jika stage atau prefix event tidak sesuai konfigurasi topic.

## 4. File Implementasi

### 4.1 File Unity baru

| File | Fungsi |
|---|---|
| `Assets/!Script/Backend/PlayerRunRecorder.cs` | Singleton lintas scene yang memulai run, mencatat maksimal 100 keputusan, membuat `runId`, dan menghasilkan snapshot akhir. |
| `Assets/!Script/Backend/PlayerRunRecorder.cs.meta` | Metadata Unity untuk recorder. |
| `Assets/!Script/Sidiq/PostGameAICoachController.cs` | Membangun layout dua kolom, memulai session, menampilkan bubble chat, membatasi input, dan menangani retry. |
| `Assets/!Script/Sidiq/PostGameAICoachController.cs.meta` | Metadata Unity untuk controller UI. |

### 4.2 File Unity lama yang dimodifikasi

| File | Perubahan AI coach |
|---|---|
| `Assets/!Script/Backend/SecMindAPI.cs` | Mengganti endpoint chat lama dengan endpoint session AI baru. |
| `Assets/!Script/Game Topic 1/AI/DeepSeekChatService.cs` | Diubah dari chat bebas menjadi client session terautentikasi dengan DTO respons terstruktur. |
| `Assets/!Script/Game Topic 1/Topic1ProgressionController.cs` | Memulai recorder Topic 1 dan mencatat keputusan evidence, installer, serta file request. |
| `Assets/!Script/Game Topic 1/VN/VNDialogueManager.cs` | Mencatat pilihan `accept` dan `reject` pada VN yang memakai manager bersama. |
| `Assets/!Script/Ghaza/PasswordValidator.cs` | Memulai recorder Topic 2 serta mencatat strength password dan pilihan MFA. |
| `Assets/!Script/Game Topic 3/Email_Interaction/EmailManager.cs` | Memulai recorder Topic 3 dan mencatat aksi pemain pada email. |
| `Assets/!Script/Game Topic 3/Email_Interaction/EvaluationPanel.cs` | Mencatat hasil jawaban evaluasi Topic 3. |
| `Assets/!Script/Game Topic 4/GameManager_Tp4.cs` | Memulai recorder Topic 4. |
| `Assets/!Script/Game Topic 4/WebsiteLoginController.cs` | Mencatat keputusan login/cancel pada website phishing atau legitimate. |
| `Assets/!Script/Game Topic 4/DashboardPopupController.cs` | Mencatat hasil keputusan popup. |
| `Assets/!Script/Game Topic 4/EvaluationPanel_Tp4.cs` | Mencatat hasil evaluasi Topic 4. |
| `Assets/!Script/Game Topic 5/GameManager_Tp5.cs` | Memulai recorder Topic 5. |
| `Assets/!Script/Game Topic 5/WifiSelectorController.cs` | Mencatat pemilihan Wi-Fi aman atau look-alike. |
| `Assets/!Script/Game Topic 5/WebsiteSecurityController.cs` | Mencatat VPN dan login public Wi-Fi. |
| `Assets/!Script/Game Topic 5/Topic5ProgressionController.cs` | Mencatat pilihan respons investigasi. |
| `Assets/!Script/Game Topic 6/GameManager_Tp6.cs` | Memulai recorder Topic 6 dan mencatat recovery serta konfigurasi backup. |
| `Assets/!Script/Game Topic 6/BackupController.cs` | Mencatat keberadaan dan sumber backup. |
| `Assets/!Script/Sidiq/SummaryManager.cs` | Menghubungkan summary bersama dengan `PostGameAICoachController`. |
| `Assets/!Script/Sidiq/Editor/SummaryPanelRedesignSetup.cs` | Menyesuaikan ukuran summary, menghapus Next Level, dan mengubah Replay menjadi Retry. |

Semantic-context v2 juga memperbarui data pendukung game: minimum Strong Topic 2 menjadi 12 karakter, penjelasan URL Topic 4 diisi, SSID Topic 5 diselaraskan dengan cerita kafe, dan validasi backup Topic 6 diselaraskan dengan label UI. Branching VN tidak diubah.

File tambahan yang berubah pada semantic-context v2:

- Topic 1: `Topic1EvidenceCase`, `Topic1EvidenceChecklistController`, dan tiga evidence asset untuk stable evidence ID serta red flag yang dipilih/terlewat.
- Topic 2: `PasswordValidator` dan `MFAFlowController` untuk kriteria password privacy-safe, outcome, serta jumlah attempt/resend OTP tanpa merekam kode.
- Topic 3: `EmailData`, `EmailDetailButtons`, dan `EmailManager` untuk stable template ID dan risk indicator.
- Topic 4: `URLData_Tp4`, `PopupData_Tp4`, controller website/popup, dan asset datanya untuk stable ID serta penjelasan URL.
- Topic 5: progression, Wi-Fi selector, VPN controller, dan scene computer untuk clue, pilihan Mas Anto, SSID asli, dan outcome.
- Topic 6: backup, recovery, scene computer, serta scene builder untuk lokasi/jadwal yang benar-benar dipilih.
- Evaluasi Topic 2–6 sekarang mengirim pilihan A/B dan correct choice, bukan hanya `correct/incorrect`.

### 4.3 File backend baru

| File | Fungsi |
|---|---|
| `docs/api/ai/session/start.js` | Membuat atau memulihkan session berdasarkan `userId + runId`, membuat opening debrief, dan menyimpannya. |
| `docs/api/ai/chat.js` | Menerima pertanyaan lanjutan, memastikan session milik user, membatasi tiga pertanyaan, dan menyimpan history. |
| `docs/api/ai/session.js` | Mengambil ulang session aktif dan message history berdasarkan `runId`. |
| `docs/api/cron/cleanup-ai-sessions.js` | Menghapus session kedaluwarsa; message ikut terhapus melalui cascade. |
| `docs/lib/ai-coach.js` | Konfigurasi prompt Topic 1–6, validasi run, sanitasi input/output, dan fallback response. |
| `docs/lib/game-context-catalog.js` | Katalog v2 untuk scenario, pilihan, alternatif, evidence, outcome, serta evaluasi Topic 1–6. |
| `docs/lib/deepseek.js` | Client DeepSeek dengan timeout 20 detik, satu retry, JSON Output, dan validasi response. |
| `docs/db/schema_ai_coach.sql` | Membuat tabel `ai_sessions`, `ai_messages`, constraint, dan index. |
| `docs/test/ai-coach.test.js` | Unit test payload, keamanan event, prompt, output JSON, dan batas pesan. |
| `docs/vercel.json` | Menjadwalkan cleanup session setiap hari. |

### 4.4 File backend lama yang dimodifikasi

| File | Perubahan |
|---|---|
| `docs/api/chat.js` | Endpoint lama dinonaktifkan dengan HTTP `410`; client harus memakai endpoint session baru. |
| `docs/package.json` | Script `npm test` menjalankan test AI coach dengan Node test runner. |

### 4.5 File yang bukan bagian AI coach

Jangan ikut stage/commit kecuali memang ada perubahan terpisah yang disetujui:

- `AGENTS.md`
- `ProjectSettings/Packages/com.unity.testtools.codecoverage/Settings.json`
- File temporary dalam `Library/`, `Temp/`, atau `Logs/`

Selalu cek hasil staging:

```powershell
git diff --cached --stat
git diff --cached
```

## 5. Kontrak dan Batasan AI

Backend menerapkan batas berikut:

- User wajib memiliki Firebase ID token yang valid.
- Satu session hanya dapat diakses oleh user pemiliknya.
- Maksimal tiga pertanyaan lanjutan per run.
- Pesan user maksimal 400 karakter.
- Maksimal 100 keputusan gameplay per run.
- Maksimal 12 fakta privacy-safe per keputusan.
- Event dan choice harus terdaftar pada katalog topic.
- Durasi maksimal payload adalah 24 jam.
- Session berlaku tujuh hari.
- AI hanya boleh menjawab materi topic yang baru diselesaikan.
- AI tidak boleh mengarang score atau keputusan yang tidak tercatat.
- Permintaan ofensif seperti phishing operasional, pencurian credential, malware, exploitation, atau evasion harus ditolak.
- Respons harus JSON dengan tepat empat field:

```json
{
  "answer": "string",
  "evidence": ["string"],
  "nextAction": "string",
  "outOfScope": false
}
```

- Seluruh respons maksimal 120 kata.
- `answer` maksimal tiga kalimat.
- `evidence` maksimal dua item.
- `nextAction` tepat satu tindakan praktis.
- HTML, Markdown, link, dan key tambahan ditolak.
- Default bahasa adalah Indonesia; bahasa Inggris digunakan jika pemain bertanya dalam bahasa Inggris.

Jika DeepSeek timeout, key tidak ada, response terpotong, atau JSON gagal validasi, server memakai fallback aman. Karena fallback tetap mengembalikan HTTP sukses, cek log Vercel untuk memastikan respons benar-benar berasal dari DeepSeek.

## 6. Data yang Disimpan

### `ai_sessions`

Menyimpan:

- Session UUID dan run UUID.
- Firebase user ID.
- Stage ID, score, max score, dan durasi.
- Konteks keputusan gameplay terstruktur.
- Jumlah pertanyaan.
- Penggunaan prompt/completion token.
- Waktu pembuatan dan kedaluwarsa.

### `ai_messages`

Menyimpan:

- Role `user` atau `assistant`.
- Pesan pemain.
- Respons AI dalam teks dan JSON tervalidasi.
- Waktu pembuatan.

Recorder tidak mengambil password yang diketik, kode OTP, credential, isi email, atau teks bebas dari gameplay. Hanya ID template, indikator risiko, bucket panjang password, boolean kriteria, jumlah percobaan OTP yang dibatasi, pilihan, outcome, dan perubahan skor yang dikirim. Chat yang sengaja dikirim pemain tetap disimpan sebagai message session selama masa retensi.

## 7. Migrasi Neon

### 7.1 Rekomendasi

Uji lebih dulu pada Neon development branch. Setelah Preview lolos, jalankan migrasi yang sama pada database Production.

### 7.2 Jalankan migrasi

Buka Neon SQL Editor dan jalankan seluruh isi:

```text
docs/db/schema_ai_coach.sql
```

Script memakai `CREATE TABLE IF NOT EXISTS`, tetapi tetap review schema sebelum menjalankannya. Tabel `users` harus sudah ada karena `ai_sessions.user_id` memiliki foreign key ke `users.user_id`.

### 7.3 Verifikasi

```sql
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('ai_sessions', 'ai_messages');

SELECT indexname
FROM pg_indexes
WHERE tablename IN ('ai_sessions', 'ai_messages');
```

Setelah smoke test:

```sql
SELECT id, run_id, user_id, stage_id, score, question_count,
       prompt_tokens, completion_tokens, created_at, expires_at
FROM ai_sessions
ORDER BY created_at DESC
LIMIT 20;

SELECT session_id, role, content_text, created_at
FROM ai_messages
ORDER BY created_at DESC
LIMIT 50;
```

## 8. Environment Variables Vercel

Pastikan Vercel project menggunakan root directory `docs/`.

| Variable | Wajib | Keterangan |
|---|---:|---|
| `DATABASE_URL` | Ya | Neon pooled connection string. Gunakan database berbeda untuk Preview bila memungkinkan. |
| `FIREBASE_PROJECT_ID` | Ya | Firebase project ID untuk verifikasi ID token. |
| `FIREBASE_CLIENT_EMAIL` | Ya | Service-account client email. |
| `FIREBASE_PRIVATE_KEY` | Ya | Private key server; pertahankan newline dengan benar. |
| `DEEPSEEK_API_KEY` | Ya untuk AI asli | API key server-side. Jangan pernah dimasukkan ke Unity atau repository. |
| `CRON_SECRET` | Ya | Melindungi endpoint cleanup session. Gunakan nilai acak yang panjang. |

Konfigurasikan scope **Preview** dan **Production** secara sengaja. Jangan berasumsi variable Production otomatis tersedia pada Preview.

Contoh local-only `docs/.env.local`:

```env
DATABASE_URL=postgresql://...
FIREBASE_PROJECT_ID=...
FIREBASE_CLIENT_EMAIL=...
FIREBASE_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n"
DEEPSEEK_API_KEY=...
CRON_SECRET=...
```

`.env.local` sudah di-ignore. Jangan menyalin nilai rahasia ke dokumen, screenshot, Unity Inspector, atau commit Git.

## 9. DeepSeek Configuration

Backend saat ini memakai:

- Base URL: `https://api.deepseek.com/chat/completions`
- Model: `deepseek-v4-flash`
- Thinking: disabled
- JSON Output: enabled
- Temperature: `0.2`
- Maksimum output: `256` tokens
- Timeout: 20 detik
- Retry: satu kali

Konfigurasi model, thinking toggle, dan `response_format: {"type":"json_object"}` sesuai dokumentasi API resmi DeepSeek per 4 Agustus 2026:

- https://api-docs.deepseek.com/api/create-chat-completion
- https://api-docs.deepseek.com/guides/json_mode/
- https://api-docs.deepseek.com/quick_start/pricing

Sebelum production, pastikan akun DeepSeek memiliki key aktif, saldo/quota, dan akses ke model tersebut. Pantau `prompt_tokens` dan `completion_tokens` dari Neon untuk memperkirakan biaya.

## 10. Urutan Deployment yang Aman

### Tahap A — Review dan commit source/backend

1. Jalankan `git status`.
2. Stage hanya file pada bagian **File Implementasi**.
3. Jangan stage `AGENTS.md` atau perubahan otomatis code-coverage.
4. Review dengan `git diff --cached`.
5. Jalankan test backend dan compile C#.
6. Commit sebagai snapshot sebelum Unity WebGL build.

```powershell
cd docs
npm install
npm test
cd ..
dotnet build Capstone-FinalArc.sln --no-restore
```

Expected result:

- Backend: 15 test passed.
- C#: 0 errors.

### Tahap B — Database dan Preview backend

1. Jalankan migrasi pada Neon Preview/development.
2. Isi environment variables untuk Vercel Preview.
3. Push branch atau jalankan Preview deployment sesuai workflow tim.
4. Periksa Function Logs pada endpoint AI.
5. Pastikan request tanpa token menerima `401`, bukan `500`.

### Tahap C — Build Unity WebGL

Gunakan Unity **2022.3.62f3** dan target WebGL.

Build output harus menuju folder `docs/`.

> Peringatan: Unity WebGL build dapat menimpa isi folder `docs/`, termasuk backend.

Workflow wajib:

1. Pastikan source/backend sudah di-commit sebagai snapshot.
2. Build WebGL ke `docs/`.
3. Pastikan file berikut masih ada:

```text
docs/api/
docs/lib/
docs/db/
docs/test/
docs/package.json
docs/vercel.json
```

4. Jika backend hilang karena build, pulihkan dari commit snapshot:

```powershell
git restore --source=HEAD -- docs/api docs/lib docs/db docs/test docs/package.json docs/vercel.json
```

5. Buka WebGL lokal dan lakukan smoke test.
6. Commit hasil WebGL build secara terpisah agar perubahan mudah diaudit.

### Tahap D — Production

1. Jalankan migrasi pada Neon Production.
2. Verifikasi seluruh environment variable Production.
3. Pastikan `SecMindAPI.BaseUrl` tetap `https://secmind.vercel.app` untuk build Production.
4. Merge/push commit source, backend, dan WebGL build ke branch production.
5. Tunggu deployment Vercel selesai.
6. Test menggunakan akun Firebase non-admin biasa.
7. Monitor Vercel Function Logs, Neon query, dan penggunaan DeepSeek.

## 11. Cara Test Lokal End-to-End

### Backend

```powershell
cd docs
npm install
npx vercel dev --listen 3000
```

Buat `docs/.env.local` dengan environment variable development. Jalankan schema pada database development.

### Unity Editor

Untuk test sementara, arahkan `SecMindAPI.BaseUrl` ke:

```csharp
public const string BaseUrl = "http://localhost:3000";
```

Setelah selesai, kembalikan ke URL production. Lebih aman menambahkan switch `UNITY_EDITOR` pada perubahan terpisah agar URL lokal tidak ikut ke WebGL Production.

Urutan test:

1. Mulai dari `LoginScene`, bukan langsung dari map topic.
2. Login dengan akun Firebase.
3. Masuk ke salah satu topic melalui menu.
4. Buat beberapa pilihan gameplay.
5. Selesaikan topic.
6. Pastikan opening debrief muncul.
7. Kirim tiga pertanyaan dan pastikan counter menjadi 2, 1, lalu 0.
8. Periksa terminal Vercel local dan isi tabel Neon.

Pesan `AI coach requires an authenticated account` berarti scene dimainkan tanpa ID token Firebase. `FirebaseManager` bersifat `DontDestroyOnLoad`, sehingga flow normal harus dimulai dari login.

## 12. Checklist QA Topic 1–6

Ulangi checklist berikut pada setiap topic:

- [ ] Login Firebase berhasil sebelum masuk topic.
- [ ] Topic menggunakan stage ID yang benar.
- [ ] Pilihan penting masuk ke `PlayerRunRecorder`.
- [ ] Score, max score, durasi, dan decisions diterima backend.
- [ ] Summary menjadi dua kolom tanpa menutupi konten.
- [ ] Tombol hanya Back to Menu dan Retry.
- [ ] Opening debrief relevan dengan topic dan pilihan pemain.
- [ ] AI tidak mengarang pilihan yang tidak ada.
- [ ] Pertanyaan di luar topic diarahkan kembali.
- [ ] Pertanyaan ofensif ditolak secara aman.
- [ ] Bahasa respons mengikuti bahasa pemain.
- [ ] Maksimal tiga pertanyaan diterapkan.
- [ ] Retry membuat run baru dan recorder tidak membawa pilihan run lama.
- [ ] Back to Menu membersihkan current run.
- [ ] Session dan messages tersimpan di Neon.
- [ ] WebGL tidak mengalami CORS atau mixed-content error.
- [ ] Tidak ada error baru pada Unity Console atau Vercel Function Logs.

Topic-specific evidence yang harus muncul minimal sekali:

| Topic | Contoh event |
|---|---|
| Topic 1 | `evidence.*`, `installer.untrusted`, `file_request.computer`, atau `vn.*` |
| Topic 2 | `password.creation`, `password.outcome`, `mfa.choice`, `mfa.verification`, atau `evaluation.*` |
| Topic 3 | `email.<template_id>` atau `evaluation.*` |
| Topic 4 | `website.*`, `popup.*`, atau `evaluation.*` |
| Topic 5 | `wifi.clue.*`, `wifi.mas_anto_response`, `wifi.selection`, `vpn.choice`, atau `public_wifi.login` |
| Topic 6 | `file.*`, `backup.*`, atau `evaluation.*` |

## 13. Endpoint Reference

| Method | Endpoint | Auth | Fungsi |
|---|---|---:|---|
| POST | `/api/ai/session/start` | Firebase Bearer token | Membuat/memulihkan session dan opening debrief. |
| POST | `/api/ai/chat` | Firebase Bearer token | Mengirim pertanyaan lanjutan. |
| GET | `/api/ai/session?runId=<uuid>` | Firebase Bearer token | Mengambil session dan history aktif. |
| GET | `/api/cron/cleanup-ai-sessions` | `CRON_SECRET` | Menghapus session kedaluwarsa. |
| Semua | `/api/chat` | — | Legacy endpoint; selalu mengembalikan HTTP 410. |

Semua endpoint AI mengirim `Cache-Control: no-store` agar data coaching tidak di-cache oleh proxy/browser.

## 14. Troubleshooting

### `AI coach requires an authenticated account`

Penyebab: game dimulai langsung dari map atau token Firebase belum tersedia.

Solusi: mulai dari LoginScene, login, lalu masuk topic melalui flow normal.

### HTTP 404

Penyebab umum:

- Unity masih menunjuk ke production sementara backend baru hanya ada lokal.
- File nested API tidak ikut deployment.
- Vercel root directory bukan `docs/`.

### HTTP 401

Periksa:

- Header `Authorization: Bearer <Firebase ID token>`.
- `FIREBASE_PROJECT_ID`.
- `FIREBASE_CLIENT_EMAIL`.
- Format newline pada `FIREBASE_PRIVATE_KEY`.
- Token belum kedaluwarsa; `APIClient` akan mencoba refresh sekali pada 401.

### HTTP 400 saat memulai session

Periksa stage ID, score, max score, durasi, format run UUID, dan prefix event. Backend menolak event Topic 6 bila dikirim sebagai konteks Topic 5.

### HTTP 500 `Failed to create AI session`

Periksa:

- `DATABASE_URL`.
- Tabel `users` sudah ada.
- Migrasi `ai_sessions` dan `ai_messages` sudah dijalankan.
- Vercel Function Logs untuk error SQL asli.

### UI sukses tetapi jawaban generik

Kemungkinan backend memakai fallback. Cari log:

```text
[ai/session/start] DeepSeek fallback
[ai/chat] DeepSeek fallback
```

Periksa API key, quota/saldo, akses model, timeout, dan validitas JSON respons.

### HTTP 429

Tiga pertanyaan sudah digunakan. Ini perilaku yang benar, bukan error deployment.

### Session tidak ditemukan

Session mungkin milik user lain, run ID salah, atau sudah melewati masa retensi tujuh hari.

## 15. Monitoring Setelah Rilis

Pada 24 jam pertama, monitor:

- Jumlah response 401, 400, 429, dan 500.
- Log fallback DeepSeek.
- Latency endpoint session/start dan chat.
- `prompt_tokens` serta `completion_tokens`.
- Pertumbuhan tabel `ai_sessions` dan `ai_messages`.
- Hasil cron cleanup.
- Keluhan layout pada resolusi WebGL berbeda.

Query ringkas:

```sql
SELECT stage_id,
       COUNT(*) AS sessions,
       SUM(question_count) AS questions,
       SUM(prompt_tokens) AS prompt_tokens,
       SUM(completion_tokens) AS completion_tokens
FROM ai_sessions
WHERE created_at >= NOW() - INTERVAL '24 hours'
GROUP BY stage_id
ORDER BY stage_id;
```

## 16. Rollback

Jika deployment bermasalah:

1. Rollback deployment Vercel ke commit sebelumnya.
2. Jangan langsung menghapus tabel AI; tabel tidak mengganggu fitur lama dan menyimpan data untuk diagnosis.
3. Menghapus `DEEPSEEK_API_KEY` **tidak mematikan UI AI**, karena backend akan memakai fallback.
4. Untuk benar-benar menonaktifkan fitur, rollback integrasi `PostGameAICoachController.Configure(...)` atau deploy commit sebelum fitur ini.
5. Setelah masalah selesai, deploy ulang backend lebih dulu, lalu WebGL yang memanggil endpoint baru.

## 17. Definition of Done

Fitur dianggap siap Production setelah semua poin ini terpenuhi:

- [ ] File AI coach sudah direview dan di-commit tanpa file lokal yang tidak terkait.
- [ ] Migration Preview dan Production berhasil.
- [ ] Environment variables Preview dan Production lengkap.
- [ ] `npm test` lulus.
- [ ] C# compile lulus di Unity 2022.3.62f3.
- [ ] WebGL build baru sudah dibuat tanpa menghapus backend.
- [ ] Topic 1–6 lulus test end-to-end.
- [ ] DeepSeek asli terverifikasi dari log, bukan fallback.
- [ ] Batas tiga pertanyaan terverifikasi.
- [ ] Ownership session antar-user terverifikasi.
- [ ] Cron cleanup terverifikasi.
- [ ] Monitoring biaya/token tersedia.
- [ ] Rollback commit/deployment sudah diketahui tim.
