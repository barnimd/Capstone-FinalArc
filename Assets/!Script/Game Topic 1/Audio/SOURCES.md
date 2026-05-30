# Audio Assets — Topic 1 (Privasi_Keamanan)

Download each file below and place it in the correct subfolder.

---

## 📁 Music/ — Background Music (BGM)

Pilih **1 track** buat scene Privasi_Keamanan. Rekomendasi terbaik ada di urutan atas.

### Opsi 1 — Pixabay (No attribution needed, 100% free)

| # | Judul | Mood | Durasi | Link Download |
|---|-------|------|--------|---------------|
| 🥇 | **the coffee shop (chill lofi)** | Santai, office vibe | ~2 min | [Download di Pixabay](https://pixabay.com/music/beats-the-coffee-shop-chill-lofi-317116/) |
| 🥈 | **Cozy coffee shop - chill lofi** | Hangat, casual | ~2 min | [Download di Pixabay](https://pixabay.com/music/beats-cozy-coffee-shop-chill-lofi-music-385853/) |

> Cara download Pixabay: buka link → klik tombol **Download** (hijau, pojok kanan) → pilih MP3 → save.
> License: **Pixabay Content License** — bebas dipakai di game, tidak perlu credit.

---

### Opsi 2 — Bensound (Perlu credit di game)

| # | Judul | Artist | Mood | Durasi | Link |
|---|-------|--------|------|--------|------|
| ⭐ | **Moonlight Coffee** | Yunior Arronte | Shiny Lo-Fi, piano + synth | 4:21 | [bensound.com](https://www.bensound.com/royalty-free-music/track/moonlight-coffee-shiny-lo-fi) |
| ⭐ | **Morning Vibes** | Nick Petrov | Lofi Smooth, acoustic guitar | 2:37 | [bensound.com](https://www.bensound.com/royalty-free-music/track/morning-vibe-lofi-smooth) |
| ⭐ | **Long Night** | Aventure | Calm Warm, soft piano | 2:54 | [bensound.com](https://www.bensound.com/royalty-free-music/track/long-night-calm-warm) |

> Cara download Bensound: buka link → scroll bawah → klik **Free Download** → isi nama/email → download.
> License: **Bensound Free License** — wajib tulis credit: *"Music: bensound.com"* di credits/menu game.

---

### Cara rename & simpan

Setelah download, rename file dan taruh di:

```
Assets/!Script/Game Topic 1/Audio/Music/
└── bgm_privasi_keamanan.mp3     ← rename file yang kamu pilih
```

---

## 📁 SFX/ — Typing Sound Effect

Dua jenis SFX yang dibutuhkan:

### SFX 1 — Single Keypress (untuk typewriter effect, per karakter)

Ini yang diplay setiap karakter muncul satu per satu di dialog.

| # | Judul | License | Durasi | Link |
|---|-------|---------|--------|------|
| 🥇 | **Computer Keyboard - single key - type 2** | CC0 (No credit needed) | 0.22 detik | [freesound.org/380141](https://freesound.org/people/yottasounds/sounds/380141/) |
| 🥈 | **Computer Keyboard - single key - type 1** | CC0 (No credit needed) | ~0.2 detik | [freesound.org/380142](https://freesound.org/people/yottasounds/sounds/380142/) |
| 🎯 | **Keyboard Press SFX** | CC0 | pendek | [freesound.org/537618](https://freesound.org/people/Code_Redder/sounds/537618/) |

> Cara download Freesound: buka link → klik **Download** (butuh akun gratis) atau klik ikon download di sebelah player.

---

### SFX 2 — Ambient Typing Loop (opsional, suara ngetik continuous)

Diplay sebagai loop saat scene dimulai atau saat ada adegan "ngetik di komputer".

| # | Judul | License | Durasi | Link |
|---|-------|---------|--------|------|
| ⭐ | **Keyboard Typing** by Trollarch2 | CC0 (No credit) | 15 detik | [freesound.org/331656](https://freesound.org/people/Trollarch2/sounds/331656/) |
| ⭐ | **Mechanical keyboard typing (slow)** by mccreery | CC0 (No credit) | 8 detik | [freesound.org/509171](https://freesound.org/people/mccreery/sounds/509171/) |

---

### Cara rename & simpan

```
Assets/!Script/Game Topic 1/Audio/SFX/
├── sfx_keypress.wav       ← SFX 1 single keypress (rename dari download)
└── sfx_typing_loop.mp3    ← SFX 2 ambient typing (opsional)
```

---

## Cara pasang di Unity setelah download

### BGM (background music)

1. Import file ke `Assets/!Script/Game Topic 1/Audio/Music/`
2. Klik file di Project panel → Inspector:
   - **Audio Clip Type:** Music
   - **Load Type:** Streaming *(untuk file musik panjang, hemat RAM)*
   - **Compression Format:** Vorbis
3. Tambahkan `AudioSource` component di scene (GameObject kosong bernama `BGM_Manager`)
4. Set **AudioClip** ke file ini, centang **Loop**, set **Volume** = 0.4–0.6

### SFX Keypress (typewriter per karakter)

1. Import `sfx_keypress.wav` ke `Assets/!Script/Game Topic 1/Audio/SFX/`
2. Klik file → Inspector:
   - **Load Type:** Decompress On Load *(file pendek, butuh respons cepat)*
   - **Compression Format:** PCM atau ADPCM
3. Di `VNDialogueManager`, tambahkan field:
   ```csharp
   [SerializeField] private AudioSource sfxSource;
   [SerializeField] private AudioClip keypressClip;
   ```
4. Di coroutine `TypeSentence()`, tambahkan setelah setiap karakter:
   ```csharp
   if (sfxSource != null && keypressClip != null)
       sfxSource.PlayOneShot(keypressClip, 0.6f);
   ```

---

## Summary — File yang perlu di-download

| File | Sumber | Taruh di |
|------|--------|----------|
| `bgm_privasi_keamanan.mp3` | Pixabay — *the coffee shop* | `Audio/Music/` |
| `sfx_keypress.wav` | Freesound #380141 — *yottasounds* | `Audio/SFX/` |
| `sfx_typing_loop.mp3` | Freesound #331656 — *Trollarch2* (opsional) | `Audio/SFX/` |
