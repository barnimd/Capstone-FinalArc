export const TOPIC_KNOWLEDGE_VERSION = 'v1-2026-08-07';

// These references are maintenance sources. URLs are deliberately excluded from
// the model prompt and coach output so the WebGL chat never renders live links.
export const KNOWLEDGE_REFERENCES = {
  nistIdentity: {
    title: 'NIST SP 800-63B Digital Identity Guidelines',
    url: 'https://pages.nist.gov/800-63-4/sp800-63b.html',
  },
  cisaPhishing: {
    title: 'CISA Phishing Guidance: Stopping the Attack Cycle at Phase One',
    url: 'https://www.cisa.gov/sites/default/files/2025-03/Phishing%20Guidance%20-%20Stopping%20the%20Attack%20Cycle%20at%20Phase%20One%20508.pdf',
  },
  cisaWireless: {
    title: 'CISA Using Wireless Technology Securely',
    url: 'https://www.cisa.gov/sites/default/files/publications/Wireless-Security.pdf',
  },
  cisaRansomware: {
    title: 'CISA StopRansomware Guide',
    url: 'https://www.cisa.gov/stopransomware/ransomware-guide',
  },
};

export const TOPIC_KNOWLEDGE = {
  phishing: {
    summary: 'Kenali manipulasi sosial, hentikan tekanan untuk bertindak cepat, lalu verifikasi identitas, kebutuhan akses, dan sumber software melalui jalur resmi.',
    learningOutcomes: [
      'Mengenali authority, urgency, impersonation, dan permintaan data sebagai tanda social engineering.',
      'Memverifikasi permintaan melalui kanal independen sebelum memberi akses atau data.',
      'Menolak installer atau attachment yang belum diverifikasi.',
    ],
    concepts: [
      {
        id: 'social_engineering',
        aliases: ['social engineering', 'rekayasa sosial', 'manipulasi', 'menipu pegawai'],
        definition: 'Social engineering adalah manipulasi manusia agar memberikan informasi, akses, atau melakukan tindakan yang menguntungkan penyerang.',
        howItWorks: 'Penyerang membangun kepercayaan atau tekanan dengan menyamar, membawa nama atasan, menciptakan urgensi, atau memanfaatkan ketidaktahuan korban.',
        gameConnection: 'Rizal mengaku dari Finance dan Pak Budi menekan agar permintaan segera dilakukan tanpa prosedur resmi.',
        realWorldNuance: 'Satu indikator saja belum membuktikan serangan; keputusan dibuat dari gabungan konteks, kewenangan, kebutuhan, dan hasil verifikasi.',
        safeActions: ['Hentikan permintaan dan verifikasi identitas serta kebutuhan melalui supervisor atau kanal resmi.'],
        misconceptions: ['Orang yang mengetahui nama kantor atau jabatan belum tentu memiliki kewenangan.'],
      },
      {
        id: 'independent_verification',
        aliases: ['verifikasi', 'jalur resmi', 'kanal resmi', 'cek identitas', 'supervisor', 'independent verification'],
        definition: 'Verifikasi independen berarti mengonfirmasi identitas dan permintaan melalui kontak atau sistem tepercaya yang tidak diberikan oleh peminta.',
        howItWorks: 'Cara ini memutus skenario penyerang karena konfirmasi dilakukan kepada pemilik proses, supervisor, HR, atau service desk resmi.',
        gameConnection: 'Kak Rani mengingatkan pemain untuk memverifikasi permintaan akses atau file melalui jalur resmi.',
        realWorldNuance: 'Membalas pesan yang sama atau menelepon nomor yang diberikan peminta bukan verifikasi independen.',
        safeActions: ['Gunakan direktori internal atau kontak supervisor yang sudah dikenal.'],
        misconceptions: ['Verifikasi bukan sekadar meminta peminta mengulangi identitasnya.'],
      },
      {
        id: 'sensitive_data_and_least_privilege',
        aliases: ['data sensitif', 'file finance', 'least privilege', 'hak akses', 'minimal disclosure', 'privasi data'],
        definition: 'Least privilege membatasi akses hanya pada data dan fungsi minimum yang dibutuhkan untuk tugas yang sah.',
        howItWorks: 'Permintaan harus memiliki kebutuhan bisnis, kewenangan, dan saluran transfer yang sesuai sebelum data dibuka atau dikirim.',
        gameConnection: 'File Finance Q3 tidak boleh diberikan hanya karena pemain memiliki peran IT Support.',
        realWorldNuance: 'Kemampuan teknis untuk mengakses data tidak sama dengan izin bisnis untuk membagikannya.',
        safeActions: ['Konfirmasi owner data dan gunakan saluran berbagi resmi dengan akses minimum.'],
        misconceptions: ['IT Support tidak otomatis berhak membaca atau mengirim semua data perusahaan.'],
      },
      {
        id: 'untrusted_installer',
        aliases: ['installer', 'attachment', 'lampiran', 'software tidak resmi', 'malware', 'update palsu'],
        definition: 'Installer atau attachment tidak tepercaya dapat menjalankan malware, mencuri data, atau membuka akses ke perangkat.',
        howItWorks: 'File berbahaya sering disamarkan sebagai update atau dokumen kerja dan meminta korban melewati proses distribusi software resmi.',
        gameConnection: 'Software akuntansi dikirim lewat email oleh sumber yang belum diverifikasi, bukan melalui sistem IT resmi.',
        realWorldNuance: 'Attachment tidak otomatis berbahaya, tetapi sumber, signature, hash, kebutuhan, dan kanal distribusinya harus diverifikasi.',
        safeActions: ['Unduh software hanya dari repository perusahaan atau vendor resmi yang disetujui.'],
        misconceptions: ['File yang dikirim pegawai dikenal atau memiliki nama profesional belum tentu aman.'],
      },
    ],
  },
  '2fa': {
    summary: 'Gunakan password panjang dan unik untuk setiap akun, lalu tambahkan faktor autentikasi lain agar kebocoran satu password tidak langsung mengambil alih akun.',
    learningOutcomes: [
      'Memahami manfaat panjang, keunikan, passphrase, dan password manager.',
      'Memahami password reuse dan credential stuffing.',
      'Memahami manfaat dan keterbatasan MFA serta OTP.',
    ],
    concepts: [
      {
        id: 'strong_unique_password',
        aliases: ['password kuat', 'kata sandi kuat', 'password panjang', 'password unik', 'passphrase', 'kompleksitas password'],
        definition: 'Password yang baik terutama panjang, unik untuk satu akun, dan tidak mudah ditebak; passphrase atau password manager dapat membantu.',
        howItWorks: 'Panjang memperbesar ruang tebakan dan keunikan mencegah satu kebocoran membuka akun lain.',
        gameConnection: 'Game memakai panjang serta campuran huruf, angka, dan simbol sebagai latihan membuat password lebih kuat.',
        realWorldNuance: 'Panduan modern lebih menekankan panjang, blocklist, dan keunikan daripada aturan komposisi yang rumit.',
        safeActions: ['Gunakan password manager untuk membuat dan menyimpan password panjang yang berbeda pada setiap akun.'],
        misconceptions: ['Mengganti huruf dengan simbol secara mudah ditebak tidak otomatis membuat password kuat.'],
      },
      {
        id: 'password_rotation',
        aliases: ['ganti password', 'rotasi password', 'ubah password berkala', 'password expiration', 'kapan ganti password'],
        definition: 'Password perlu segera diganti ketika ada indikasi kompromi, reuse, atau permintaan pemulihan yang sah.',
        howItWorks: 'Mengganti secret yang bocor menghentikan pemakaian kredensial lama setelah sesi dan token terkait juga diamankan.',
        gameConnection: 'Evaluasi game menyebut perubahan password sebagai kebiasaan pengamanan akun penting.',
        realWorldNuance: 'NIST tidak menganjurkan pergantian arbitrer secara berkala tanpa bukti kompromi karena pengguna cenderung membuat pola yang lemah.',
        safeActions: ['Ganti password yang terindikasi bocor dan jangan gunakan kembali variasi password lama.'],
        misconceptions: ['Rotasi bulanan tidak otomatis lebih aman daripada password panjang dan unik yang belum bocor.'],
      },
      {
        id: 'password_reuse',
        aliases: ['password reuse', 'password sama', 'credential stuffing', 'akun lain', 'kredensial bocor'],
        definition: 'Password reuse adalah memakai password yang sama pada beberapa layanan sehingga satu kebocoran dapat membahayakan akun lain.',
        howItWorks: 'Credential stuffing mencoba pasangan username dan password hasil kebocoran pada banyak layanan secara otomatis.',
        gameConnection: 'Tujuan topic mendorong password yang kuat dan unik sebelum mengaktifkan perlindungan tambahan.',
        realWorldNuance: 'Password yang rumit tetap berbahaya bila dipakai ulang dan kemudian bocor dari layanan lain.',
        safeActions: ['Buat password berbeda untuk setiap layanan dan periksa akun terdampak ketika terjadi kebocoran.'],
        misconceptions: ['Menambahkan satu angka berbeda pada password yang sama bukan pemisahan yang kuat.'],
      },
      {
        id: 'multi_factor_authentication',
        aliases: ['mfa', '2fa', 'two factor', 'multi factor', 'autentikasi dua faktor', 'faktor autentikasi'],
        definition: 'MFA meminta bukti dari lebih dari satu jenis faktor, misalnya sesuatu yang diketahui dan perangkat atau kunci yang dimiliki.',
        howItWorks: 'Password yang bocor saja tidak cukup untuk login karena penyerang masih membutuhkan faktor tambahan.',
        gameConnection: 'Pemain memilih mengaktifkan MFA dan memverifikasi OTP setelah membuat password.',
        realWorldNuance: 'MFA mengurangi risiko tetapi tidak absolut; passkey atau security key lebih tahan phishing daripada OTP biasa.',
        safeActions: ['Aktifkan MFA dan utamakan passkey atau security key bila tersedia.'],
        misconceptions: ['Dua password bukan dua faktor karena keduanya sama-sama sesuatu yang diketahui.'],
      },
      {
        id: 'otp_security',
        aliases: ['otp', 'kode verifikasi', 'one time password', 'kode mfa', 'kode sekali pakai'],
        definition: 'OTP adalah kode berumur pendek yang membuktikan akses ke authenticator tertentu.',
        howItWorks: 'Server mencocokkan kode sementara setelah password benar, tetapi kode masih dapat dicuri melalui phishing real-time.',
        gameConnection: 'Pemain memasukkan OTP untuk menyelesaikan verifikasi MFA.',
        realWorldNuance: 'OTP tidak boleh dibagikan; pihak resmi umumnya tidak meminta pengguna menyebutkan kode tersebut.',
        safeActions: ['Masukkan OTP hanya pada layanan yang domain dan permintaan loginnya sudah diverifikasi.'],
        misconceptions: ['Kode yang hanya berlaku singkat tetap dapat disalahgunakan sebelum kedaluwarsa.'],
      },
    ],
  },
  'password-security': {
    summary: 'Nilai email dari identitas pengirim, domain, konteks, tekanan, tautan, dan permintaan datanya; laporkan phishing tanpa mengabaikan kemungkinan email sah.',
    learningOutcomes: [
      'Mengenali phishing dan impersonation melalui beberapa indikator.',
      'Memeriksa domain dan tujuan tautan sebelum berinteraksi.',
      'Memilih report, delete, atau reply sesuai bukti dan konteks.',
    ],
    concepts: [
      {
        id: 'email_phishing',
        aliases: ['phishing email', 'email phishing', 'email palsu', 'social engineering email'],
        definition: 'Phishing email menyamar sebagai pihak tepercaya untuk mendorong korban membuka situs palsu, memberikan data, atau menjalankan file berbahaya.',
        howItWorks: 'Penyerang menggabungkan branding, impersonation, tekanan waktu, hadiah, atau ancaman agar korban tidak memeriksa bukti.',
        gameConnection: 'Inbox game mencampur email phishing dan sah sehingga pemain harus menilai setiap pesan, bukan sekadar tampilannya.',
        realWorldNuance: 'Tidak ada satu tanda yang selalu membuktikan phishing; gunakan gabungan sender, domain, konteks, link, dan permintaan.',
        safeActions: ['Laporkan pesan mencurigakan melalui mekanisme organisasi dan verifikasi lewat kanal lain.'],
        misconceptions: ['Logo dan tata bahasa profesional bukan bukti email asli.'],
      },
      {
        id: 'sender_and_domain',
        aliases: ['sender', 'pengirim', 'alamat email', 'domain email', 'display name', 'spoofing', 'lookalike'],
        definition: 'Display name dapat dibuat bebas, sedangkan alamat dan domain pengirim memberi bukti yang lebih berguna tentang asal pesan.',
        howItWorks: 'Penyerang memakai typo, angka pengganti huruf, atau domain lain yang terlihat serupa dengan organisasi asli.',
        gameConnection: 'Email BRI, Gojek, Tokopedia, dan bel1jual memakai identitas atau domain yang menyerupai layanan asli.',
        realWorldNuance: 'Alamat yang terlihat benar juga dapat berasal dari akun sah yang sudah dikompromikan, jadi konteks tetap perlu diperiksa.',
        safeActions: ['Periksa alamat lengkap pengirim dan konfirmasi permintaan sensitif melalui kontak resmi.'],
        misconceptions: ['Nama pengirim yang dikenal tidak menjamin mailbox-nya aman.'],
      },
      {
        id: 'urgency_and_sensitive_requests',
        aliases: ['urgency', 'mendesak', 'tekanan waktu', 'ancaman akun', 'hadiah', 'pin', 'data sensitif'],
        definition: 'Urgency, ancaman, hadiah, dan permintaan informasi rahasia adalah teknik untuk mempercepat keputusan sebelum korban memeriksa fakta.',
        howItWorks: 'Emosi seperti takut kehilangan akun atau senang mendapat hadiah mengurangi perhatian terhadap domain dan prosedur.',
        gameConnection: 'Beberapa email meminta PIN, kredensial, atau respons cepat dengan ancaman penutupan akun dan hadiah.',
        realWorldNuance: 'Pesan sah juga bisa mendesak, sehingga urgensi harus dikonfirmasi bersama indikator lain.',
        safeActions: ['Jangan kirim password, PIN, atau OTP melalui email; buka layanan lewat aplikasi atau alamat resmi.'],
        misconceptions: ['Urgensi adalah red flag, bukan bukti tunggal.'],
      },
      {
        id: 'email_actions',
        aliases: ['report', 'laporkan', 'delete', 'hapus', 'reply', 'balas', 'false positive'],
        definition: 'Report memberi tim keamanan kesempatan memblokir kampanye, delete hanya menghilangkan pesan dari inbox, dan reply dapat mengonfirmasi bahwa alamat aktif.',
        howItWorks: 'Tindakan dipilih berdasarkan bukti: phishing sebaiknya dilaporkan, sedangkan email kerja sah ditangani sesuai kebutuhan bisnis.',
        gameConnection: 'Game menilai report sebagai tindakan terbaik untuk phishing dan reply sebagai tindakan tepat untuk pesan kerja sah.',
        realWorldNuance: 'Prosedur organisasi dapat berbeda; email dengan link atau attachment tidak otomatis phishing, dan setelah melapor pesan biasanya dihapus atau dikarantina sesuai kebijakan.',
        safeActions: ['Gunakan tombol report phishing organisasi dan hindari membalas pengirim mencurigakan.'],
        misconceptions: ['Semua email dengan link atau attachment tidak otomatis phishing.'],
      },
    ],
  },
  'malware-awareness': {
    summary: 'Periksa domain sebenarnya dan konteks situs, pahami batas HTTPS, lalu tangani popup melalui kontrol sistem resmi alih-alih mengikuti scareware.',
    learningOutcomes: [
      'Membaca domain utama dan mengenali URL look-alike atau subdomain menipu.',
      'Memahami bahwa HTTPS bukan bukti legitimasi situs.',
      'Membedakan scareware browser dari notifikasi sistem yang sah.',
    ],
    concepts: [
      {
        id: 'url_anatomy',
        aliases: ['url', 'domain', 'main domain', 'domain utama', 'subdomain', 'alamat website'],
        definition: 'Domain utama menunjukkan layanan yang benar-benar dikunjungi; teks sebelum domain utama dapat sekadar subdomain milik pihak lain.',
        howItWorks: 'Penyerang menaruh nama merek pada subdomain atau path agar bagian awal URL terlihat resmi walau registrable domain-nya berbeda.',
        gameConnection: 'Game menampilkan alamat seperti secure-bank pada subdomain yang domain utamanya bukan bank tersebut.',
        realWorldNuance: 'Struktur public suffix dapat berbeda, tetapi pemeriksaan sebaiknya berfokus pada domain yang terdaftar dan ejaannya.',
        safeActions: ['Baca domain dari kanan ke kiri dan buka layanan sensitif melalui bookmark atau aplikasi resmi.'],
        misconceptions: ['Nama merek di bagian awal URL tidak membuktikan kepemilikan situs.'],
      },
      {
        id: 'lookalike_domain',
        aliases: ['lookalike', 'typosquatting', 'typo domain', 'domain mirip', 'angka nol', 'homoglyph'],
        definition: 'Look-alike domain meniru ejaan domain sah dengan typo, angka, karakter serupa, atau tambahan kata.',
        howItWorks: 'Kemiripan visual membuat pengguna melewatkan perbedaan kecil lalu memasukkan kredensial pada situs penyerang.',
        gameConnection: 'go0gle, paypa1, dan faceb00k menggunakan angka yang menyerupai huruf pada merek asli.',
        realWorldNuance: 'Situs palsu dapat memiliki desain rapi dan sertifikat HTTPS valid untuk domain palsunya sendiri.',
        safeActions: ['Periksa setiap karakter domain sebelum login dan gunakan password manager sebagai sinyal tambahan.'],
        misconceptions: ['Tampilan profesional dan ikon gembok tidak menjamin domain tersebut milik merek asli.'],
      },
      {
        id: 'https_limitations',
        aliases: ['https', 'tls', 'sertifikat', 'certificate', 'ikon gembok', 'padlock'],
        definition: 'HTTPS mengenkripsi dan menjaga integritas koneksi antara browser dan domain yang sedang dibuka.',
        howItWorks: 'Sertifikat mengikat koneksi ke domain tertentu, tetapi tidak menilai apakah operator domain itu jujur atau situsnya aman.',
        gameConnection: 'Evaluasi mengajarkan bahwa HTTPS tidak otomatis membuat sebuah website dapat dipercaya.',
        realWorldNuance: 'Peringatan sertifikat harus ditangani serius, tetapi sertifikat valid tetap harus disertai pemeriksaan domain yang tepat dan identitas pemilik situs.',
        safeActions: ['Pastikan HTTPS valid dan domain tepat sebelum memasukkan data sensitif.'],
        misconceptions: ['HTTPS berarti koneksinya terlindungi, bukan isi atau pemilik situs pasti tepercaya.'],
      },
      {
        id: 'scareware',
        aliases: ['scareware', 'popup virus', 'pop up palsu', 'fake support', 'tech support', 'hadiah iphone'],
        definition: 'Scareware adalah pesan menakutkan atau mendesak yang mendorong pengguna mengunduh software, menelepon nomor, atau memberikan akses.',
        howItWorks: 'Popup browser meniru peringatan antivirus atau dukungan teknis untuk membuat korban bertindak di luar kontrol keamanan resmi.',
        gameConnection: 'Game menampilkan fake virus cleanup, fake Defender, hadiah, dan nomor dukungan palsu.',
        realWorldNuance: 'Notifikasi sah tetap dapat muncul, tetapi tindakan aman dilakukan dari Settings, updater, atau aplikasi keamanan yang dibuka sendiri.',
        safeActions: ['Tutup popup dan jalankan pemeriksaan melalui aplikasi keamanan atau pengaturan resmi.'],
        misconceptions: ['Website tidak dapat membuktikan infeksi perangkat hanya melalui popup dramatis.'],
      },
    ],
  },
  'wifi-security': {
    summary: 'Verifikasi identitas jaringan publik, kenali rogue AP dan evil twin, eskalasikan perangkat mencurigakan, serta gunakan HTTPS dan VPN sesuai batas perlindungannya.',
    learningOutcomes: [
      'Membedakan access point resmi, rogue access point, dan evil twin.',
      'Memahami risiko SSID look-alike, auto-connect, dan trafik tidak terenkripsi.',
      'Memahami peran serta keterbatasan HTTPS dan VPN.',
    ],
    scoring: {
      maximumScore: 100,
      bestMandatoryPathScore: 95,
      optionalBonusScore: 5,
      optionalEvents: [
        {
          eventId: 'wifi.clue.bu_dewi',
          label: 'Membantu Bu Dewi setelah ia memakai CafeBean_Free_5G',
          scoreDelta: 5,
          purpose: 'Bonus eksplorasi yang memberi pembelajaran respons awal kepada korban; bukan objective wajib untuk menyelesaikan topic.',
        },
      ],
      interpretation: 'Skor 95/100 dapat berarti seluruh objective wajib dan pilihan utama diselesaikan dengan benar, tetapi bonus eksplorasi Bu Dewi tidak ditemukan. Jangan menyebut selisih lima poin ini sebagai kesalahan keputusan wajib.',
    },
    concepts: [
      {
        id: 'rogue_access_point',
        aliases: ['rogue access point', 'rogue ap', 'access point palsu', 'router palsu', 'wifi ilegal'],
        definition: 'Rogue access point adalah perangkat Wi-Fi yang dipasang tanpa izin pada atau dekat lingkungan organisasi.',
        howItWorks: 'Perangkat tersebut dapat membuka jalur tidak terkelola, meniru layanan jaringan, atau menarik pengguna ke koneksi yang dikendalikan pihak lain.',
        gameConnection: 'Perangkat di ruang karaoke yang dioperasikan Mas Anto menjadi sumber beberapa SSID palsu.',
        realWorldNuance: 'Rogue AP adalah kategori perangkat tidak sah; tidak semuanya meniru SSID resmi, sedangkan evil twin secara khusus melakukan impersonation.',
        safeActions: ['Jangan menyentuh atau mengancam operator; catat temuan dan laporkan kepada staf atau tim IT berwenang.'],
        misconceptions: ['Setiap Wi-Fi asing adalah rogue AP, tetapi belum tentu evil twin.'],
      },
      {
        id: 'evil_twin',
        aliases: ['evil twin', 'wifi kembar', 'ssid palsu', 'wifi mirip', 'impersonasi wifi'],
        definition: 'Evil twin adalah access point berbahaya yang meniru nama atau karakteristik jaringan tepercaya agar korban tersambung kepadanya.',
        howItWorks: 'Penyerang menyiarkan SSID identik atau mirip dan dapat memanfaatkan sinyal kuat, kebiasaan pengguna, atau auto-connect.',
        gameConnection: 'CafeBean_Free_5G dan variasi nama CafeBean dibuat agar terlihat seperti jaringan resmi CafeBean_Free.',
        realWorldNuance: 'Nama SSID bukan autentikasi; password yang sama atau jaringan terbuka dapat meningkatkan peluang perangkat tersambung otomatis.',
        safeActions: ['Konfirmasi nama jaringan dan prosedur koneksi kepada staf resmi sebelum tersambung.'],
        misconceptions: ['SSID yang terlihat resmi tidak membuktikan access point dimiliki kafe.'],
      },
      {
        id: 'mitm_public_wifi',
        aliases: ['mitm', 'man in the middle', 'penyadapan', 'intercept', 'sniffing', 'public wifi', 'wifi publik'],
        definition: 'MITM terjadi ketika pihak lain berada pada jalur komunikasi dan mencoba mengamati atau mengubah trafik antara perangkat dan layanan.',
        howItWorks: 'Jaringan berbahaya dapat melihat trafik yang tidak terenkripsi, memanipulasi resolusi atau tujuan, dan mencoba mengarahkan korban ke layanan palsu.',
        gameConnection: 'Game menggambarkan koneksi tanpa perlindungan sebagai peluang Mas Anto mencegat kredensial.',
        realWorldNuance: 'Public Wi-Fi tidak otomatis berbahaya dan HTTPS valid biasanya melindungi isi kredensial; hasil langsung diretas dalam game adalah penyederhanaan risiko.',
        safeActions: ['Hindari peringatan sertifikat, gunakan HTTPS, nonaktifkan auto-connect, dan pilih jaringan yang sudah diverifikasi.'],
        misconceptions: ['Berada pada Wi-Fi yang sama tidak otomatis membuat penyerang dapat membaca trafik HTTPS valid.'],
      },
      {
        id: 'vpn_protection',
        aliases: ['vpn', 'virtual private network', 'tunnel', 'encrypted tunnel', 'defence in depth'],
        definition: 'VPN membuat tunnel terenkripsi dari perangkat ke VPN gateway untuk melindungi trafik yang diarahkan melalui tunnel pada jaringan lokal yang tidak tepercaya.',
        howItWorks: 'Operator Wi-Fi melihat koneksi ke VPN tetapi tidak isi trafik di dalam tunnel; setelah gateway, trafik tetap bergantung pada keamanan layanan tujuan.',
        gameConnection: 'Pemain diminta mengaktifkan VPN sebelum login sebagai lapisan tambahan pada Wi-Fi publik.',
        realWorldNuance: 'VPN tidak membuat situs phishing aman, tidak menggantikan pemeriksaan domain atau HTTPS, dan memindahkan sebagian kepercayaan kepada provider VPN.',
        safeActions: ['Gunakan VPN organisasi atau provider tepercaya dan tetap periksa domain serta HTTPS.'],
        misconceptions: ['VPN bukan antivirus dan tidak membuat pengguna anonim atau kebal terhadap semua serangan.'],
      },
      {
        id: 'wifi_incident_escalation',
        aliases: ['mas anto', 'pak hendra', 'staff it', 'lapor staff', 'eskalasi', 'konfrontasi'],
        definition: 'Insiden jaringan sebaiknya ditangani oleh person in charge yang memiliki kewenangan, prosedur, dan kemampuan menjaga bukti.',
        howItWorks: 'Eskalasi mengurangi risiko konfrontasi, penghilangan barang bukti, salah tuduh, dan tindakan teknis yang tidak terkoordinasi.',
        gameConnection: 'Raka adalah IT Support dari perusahaan lain; Pak Hendra atau staf kafe adalah pihak yang berwenang menangani Mas Anto.',
        realWorldNuance: 'Indikasi kuat tetap bukan izin untuk mengancam, menyita perangkat, atau melakukan investigasi pribadi.',
        safeActions: ['Jaga jarak, catat lokasi dan indikator, lalu laporkan kepada staf berwenang.'],
        misconceptions: ['Keahlian IT pribadi tidak sama dengan kewenangan incident response di lokasi lain.'],
      },
    ],
  },
  ransomware: {
    summary: 'Ransomware dapat menghentikan akses dan mencuri data; kurangi dampaknya dengan isolasi, pelaporan, serta backup terpisah yang rutin dan teruji.',
    learningOutcomes: [
      'Memahami dampak ransomware dan respons awal yang aman.',
      'Memahami backup sebagai kontrol pemulihan, bukan pencegahan infeksi.',
      'Menyusun backup terpisah, berversi, rutin, dan dapat dipulihkan.',
    ],
    concepts: [
      {
        id: 'ransomware',
        aliases: ['ransomware', 'file terenkripsi', 'file terkunci', 'tebusan', 'data extortion'],
        definition: 'Ransomware adalah malware yang menghambat akses ke sistem atau data, sering melalui enkripsi, dan dapat disertai pencurian data untuk pemerasan.',
        howItWorks: 'Setelah mendapat akses, penyerang dapat menyebar, mengenkripsi file, menghapus backup yang terjangkau, dan menuntut pembayaran.',
        gameConnection: 'Simulasi mengunci file sehingga pemain harus menggunakan backup untuk memulihkan pekerjaan.',
        realWorldNuance: 'Kasus nyata dapat melibatkan banyak perangkat, exfiltration, downtime, forensik, kewajiban pelaporan, dan pemulihan bertahap.',
        safeActions: ['Isolasi perangkat dari jaringan dan segera laporkan sesuai incident response plan.'],
        misconceptions: ['Ransomware tidak selalu hanya mengenkripsi file lokal.'],
      },
      {
        id: 'backup_recovery',
        aliases: ['backup', 'pemulihan', 'recovery', 'restore', 'cadangan data'],
        definition: 'Backup adalah salinan data untuk memulihkan kondisi setelah kehilangan, kerusakan, kesalahan, atau serangan.',
        howItWorks: 'Salinan yang bersih dan cukup baru dipulihkan ke sistem yang sudah diamankan setelah sumber insiden ditangani.',
        gameConnection: 'Pemain memilih apakah backup tersedia dan memulihkan file dari cloud atau external drive.',
        realWorldNuance: 'Backup mengurangi dampak dan waktu pemulihan, tetapi tidak mencegah malware masuk atau data dicuri.',
        safeActions: ['Tentukan data penting, lakukan backup rutin, dan uji proses restore.'],
        misconceptions: ['Adanya ikon cloud atau external drive belum membuktikan backup dapat dipulihkan.'],
      },
      {
        id: 'resilient_backup',
        aliases: ['3-2-1', 'offline backup', 'immutable', 'versioning', 'external drive', 'cloud backup', 'backup terpisah'],
        definition: 'Backup tangguh memiliki beberapa salinan, media atau lokasi terpisah, serta perlindungan agar malware tidak dapat mengubah semuanya.',
        howItWorks: 'Prinsip 3-2-1 memakai tiga salinan, dua jenis media, dan satu salinan offsite; offline, immutable, atau versioned copy memperkuat ketahanan.',
        gameConnection: 'Game menerima external drive atau cloud dengan jadwal harian sebagai konfigurasi yang andal.',
        realWorldNuance: 'External drive yang selalu terhubung dapat ikut terenkripsi dan cloud sync tanpa versioning dapat menyinkronkan perubahan berbahaya.',
        safeActions: ['Simpan setidaknya satu backup terpisah dan lindungi dengan versioning atau immutability bila tersedia.'],
        misconceptions: ['Satu folder sinkronisasi bukan otomatis backup yang tahan ransomware.'],
      },
      {
        id: 'backup_schedule_and_testing',
        aliases: ['jadwal backup', 'backup harian', 'daily backup', 'test restore', 'uji pemulihan', 'rpo', 'rto'],
        definition: 'Frekuensi backup menentukan potensi kehilangan data, sedangkan pengujian restore membuktikan salinan benar-benar dapat digunakan.',
        howItWorks: 'Jadwal disesuaikan dengan perubahan data dan toleransi kehilangan; restore diuji agar masalah format, izin, atau kerusakan diketahui sebelum insiden.',
        gameConnection: 'Pemain memilih lokasi dan jadwal, lalu simulasi menguji apakah konfigurasi backup dapat diandalkan.',
        realWorldNuance: 'Harian bukan nilai universal; sistem kritis mungkin membutuhkan interval lebih pendek dan target pemulihan yang jelas.',
        safeActions: ['Tetapkan jadwal berdasarkan kebutuhan dan lakukan uji restore berkala.'],
        misconceptions: ['Job backup yang berstatus sukses belum menjamin datanya lengkap dan dapat dipulihkan.'],
      },
      {
        id: 'ransom_payment',
        aliases: ['bayar tebusan', 'ransom payment', 'membayar hacker', 'bayar ransomware'],
        definition: 'Pembayaran tebusan tidak menjamin kunci pemulihan bekerja, data dihapus, atau penyerang tidak kembali.',
        howItWorks: 'Korban tetap bergantung pada pelaku dan mungkin menghadapi tuntutan tambahan, risiko hukum, serta data yang sudah terlanjur bocor.',
        gameConnection: 'Game berfokus pada pemulihan dari backup, bukan pembayaran kepada penyerang.',
        realWorldNuance: 'Keputusan insiden nyata melibatkan pimpinan, legal, regulator, insurer, dan penegak hukum sesuai yurisdiksi.',
        safeActions: ['Ikuti incident response plan dan prioritaskan containment serta pemulihan dari backup bersih.'],
        misconceptions: ['Membayar bukan jaminan operasi atau kerahasiaan data kembali normal.'],
      },
    ],
  },
};

export function getTopicKnowledge(stageId) {
  return TOPIC_KNOWLEDGE[stageId] ?? null;
}

export function getPromptKnowledge(stageId) {
  const topic = getTopicKnowledge(stageId);
  if (!topic) return null;
  return {
    knowledgeVersion: TOPIC_KNOWLEDGE_VERSION,
    summary: topic.summary,
    learningOutcomes: topic.learningOutcomes,
    scoring: topic.scoring,
    concepts: topic.concepts.map((concept) => ({
      id: concept.id,
      aliases: concept.aliases,
      definition: concept.definition,
      howItWorks: concept.howItWorks,
      gameConnection: concept.gameConnection,
      realWorldNuance: concept.realWorldNuance,
      safeActions: concept.safeActions,
      misconceptions: concept.misconceptions,
    })),
  };
}

export function getTopicScoringContext(stageId, decisions = []) {
  const scoring = getTopicKnowledge(stageId)?.scoring;
  if (!scoring) return null;
  const recordedEventIds = new Set(
    (Array.isArray(decisions) ? decisions : [])
      .map((decision) => decision?.eventId)
      .filter(Boolean)
  );
  const optionalEvents = scoring.optionalEvents.map((event) => ({
    ...event,
    recorded: recordedEventIds.has(event.eventId),
  }));
  const optionalBonusEarned = optionalEvents
    .filter((event) => event.recorded)
    .reduce((total, event) => total + event.scoreDelta, 0);
  return {
    maximumScore: scoring.maximumScore,
    bestMandatoryPathScore: scoring.bestMandatoryPathScore,
    optionalBonusAvailable: scoring.optionalBonusScore,
    optionalBonusEarned,
    optionalEvents,
    interpretation: scoring.interpretation,
  };
}
