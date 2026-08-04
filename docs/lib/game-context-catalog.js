export const GAME_CONTEXT_VERSION = 'v2-2026-08-04';

const choice = (label, assessment, outcome) => ({ label, assessment, outcome });

const RISK_INDICATOR_LABELS = {
  lookalike_domain: 'Domain memakai karakter yang menyerupai nama brand resmi.',
  account_pressure: 'Ancaman akun diblokir atau kedaluwarsa mendorong keputusan terburu-buru.',
  reply_for_password_change: 'Perubahan password diarahkan melalui balasan email.',
  urgency: 'Batas waktu atau tekanan mendesak dipakai untuk menurunkan kewaspadaan.',
  unofficial_domain: 'Alamat pengirim tidak memakai domain resmi organisasi.',
  sensitive_data_request: 'Pesan meminta data pribadi, rekening, PIN, atau informasi sensitif.',
  prize_lure: 'Hadiah tak terduga dipakai sebagai umpan.',
  credential_request: 'Pesan meminta username atau password.',
  credential_form: 'Halaman meminta credential login.',
  missing_https: 'Koneksi tidak memakai HTTPS.',
  malformed_domain: 'Alamat tidak memiliki struktur domain resmi yang benar.',
  misspelled_path: 'Bagian alamat login salah eja.',
  deceptive_subdomain: 'Nama tepercaya hanya muncul sebagai subdomain, bukan domain utama.',
  unrelated_domain: 'Domain utama tidak berkaitan dengan organisasi yang diklaim.',
  scare_tactic: 'Pesan menakut-nakuti player agar segera bertindak.',
  urgent_cleanup_action: 'Popup mendorong aksi pembersihan instan yang tidak diverifikasi.',
  impersonated_security_tool: 'Popup meniru antivirus atau alat keamanan resmi.',
  unexpected_popup: 'Popup muncul tanpa konteks atau tindakan user yang relevan.',
  impersonated_vendor: 'Popup menyamar sebagai vendor teknologi resmi.',
  phone_support_scam: 'Popup mengarahkan korban untuk menghubungi dukungan palsu.',
};

const EXPECTED_SCORE_DELTAS = {
  'evidence.rizal:comply': -25,
  'installer.untrusted:install': -25,
  'file_request.computer:send': -25,
  'password.creation:weak': 10,
  'password.creation:medium': 25,
  'password.creation:strong': 50,
  'mfa.choice:enable': 50,
  'wifi.clue.bu_dewi:assist_victim': 5,
  'wifi.mas_anto_response:report_to_staff': 10,
  'wifi.mas_anto_response:threaten_to_report': 5,
  'file.organization:important_project_moved': 15,
  'file.recovery:important_project': 15,
  'file.recovery:work_report.pdf': 15,
  'file.recovery:presentation.pptx': 15,
  'file.recovery:image_pribadi.jpg': 15,
  'file.recovery:vacation.png': 15,
  'file.recovery:random.zip': 15,
  'file.recovery:temp_file.tmp': 15,
  'file.recovery:unknown_file': 15,
  'backup.exists:yes': 20,
  'backup.recovery_source:cloud': 20,
  'backup.recovery_source:external_drive': 20,
  'backup.configuration:reliable': 20,
};

const evaluation = (question, choiceA, choiceB, correctChoiceId, explanation) => ({
  question,
  choices: {
    choice_a: choice(choiceA, correctChoiceId === 'choice_a' ? 'correct' : 'incorrect', explanation),
    choice_b: choice(choiceB, correctChoiceId === 'choice_b' ? 'correct' : 'incorrect', explanation),
  },
  correctChoiceId,
  explanation,
});

const TOPIC_EVALUATIONS = {
  '2fa': [
    evaluation('Apakah kata sandi memiliki minimal 12 karakter?', 'Ya', 'Tidak', 'choice_a', 'Password yang lebih panjang lebih sulit ditebak.'),
    evaluation('Apakah kata sandi memakai huruf besar, huruf kecil, angka, dan simbol?', 'Ya', 'Tidak', 'choice_a', 'Variasi karakter meningkatkan kompleksitas password.'),
    evaluation('Apakah setiap akun menggunakan password yang berbeda?', 'Ya', 'Tidak', 'choice_a', 'Password unik mencegah satu kebocoran membuka akun lain.'),
    evaluation('Apakah password akun penting diganti secara rutin?', 'Ya', 'Tidak', 'choice_a', 'Materi game mengajarkan penggantian berkala untuk akun penting.'),
    evaluation('Apakah MFA sudah diaktifkan?', 'Ya', 'Tidak', 'choice_a', 'MFA menambah lapisan keamanan selain password.'),
  ],
  'password-security': [
    evaluation('Apakah phishing sering menyamar sebagai pihak resmi?', 'Ya', 'Tidak', 'choice_a', 'Phishing memakai identitas tepercaya untuk menurunkan kewaspadaan.'),
    evaluation('Apakah email dengan logo perusahaan pasti aman?', 'Ya', 'Tidak', 'choice_b', 'Logo dan tampilan dapat dipalsukan.'),
    evaluation('Apakah link email dapat menuju website palsu walau tampilannya meyakinkan?', 'Ya', 'Tidak', 'choice_a', 'Teks link dapat menyembunyikan tujuan sebenarnya.'),
    evaluation('Apakah URL harus diperiksa sebelum memasukkan data pribadi?', 'Ya', 'Tidak', 'choice_a', 'Domain membantu memeriksa keaslian website.'),
    evaluation('Apakah permintaan data sensitif yang mendesak patut dicurigai?', 'Ya', 'Tidak', 'choice_a', 'Urgency sering dipakai untuk mendorong keputusan terburu-buru.'),
  ],
  'malware-awareness': [
    evaluation('Apakah URL look-alike seperti go0gle.com dapat menjadi phishing?', 'Ya', 'Tidak', 'choice_a', 'Penyerang mengganti karakter agar domain menyerupai domain resmi.'),
    evaluation('Apakah website dengan tampilan profesional pasti aman?', 'Ya', 'Tidak', 'choice_b', 'Desain dapat ditiru dan bukan bukti keaslian.'),
    evaluation('Apakah HTTPS menjamin website 100% aman dan asli?', 'Ya', 'Tidak', 'choice_b', 'HTTPS melindungi koneksi, bukan membuktikan pemilik situs.'),
    evaluation('Apakah link dari iklan atau pesan dapat menuju situs palsu?', 'Ya', 'Tidak', 'choice_a', 'Link dapat disamarkan agar terlihat tepercaya.'),
    evaluation('Apakah domain utama perlu diperiksa sebelum login?', 'Ya', 'Tidak', 'choice_a', 'Domain utama membantu memastikan tujuan yang sebenarnya.'),
  ],
  'wifi-security': [
    evaluation('Apakah Wi-Fi publik berisiko untuk data sensitif?', 'Ya', 'Tidak', 'choice_a', 'Jaringan publik dapat dipantau pihak lain.'),
    evaluation('Apakah Wi-Fi publik berpassword otomatis aman?', 'Ya', 'Tidak', 'choice_b', 'Password jaringan tidak menjamin operatornya tepercaya.'),
    evaluation('Apakah data tanpa enkripsi dapat dibaca di Wi-Fi publik?', 'Ya', 'Tidak', 'choice_a', 'Data tanpa perlindungan dapat disadap.'),
    evaluation('Apakah nama Wi-Fi yang terlihat resmi selalu tepercaya?', 'Ya', 'Tidak', 'choice_b', 'Penyerang dapat membuat SSID look-alike.'),
    evaluation('Apakah VPN atau koneksi aman penting di Wi-Fi publik?', 'Ya', 'Tidak', 'choice_a', 'Perlindungan tambahan mengurangi risiko penyadapan.'),
  ],
  ransomware: [
    evaluation('Apakah backup dapat mengembalikan file hilang atau rusak?', 'Ya', 'Tidak', 'choice_a', 'Backup menyimpan salinan data untuk recovery.'),
    evaluation('Apakah aman menghapus file penting tanpa backup?', 'Ya', 'Tidak', 'choice_b', 'Tanpa cadangan, data dapat hilang permanen.'),
    evaluation('Apakah ransomware dapat membuat data tidak dapat diakses?', 'Ya', 'Tidak', 'choice_a', 'Ransomware dapat mengenkripsi atau mengunci file.'),
    evaluation('Apakah menyimpan password di file sembarangan merupakan backup aman?', 'Ya', 'Tidak', 'choice_b', 'Informasi sensitif harus disimpan dengan perlindungan yang sesuai.'),
    evaluation('Apakah cloud storage dapat digunakan sebagai backup?', 'Ya', 'Tidak', 'choice_a', 'Cloud dapat menjadi lokasi salinan data yang terpisah.'),
  ],
};

const EVENTS = {
  phishing: {
    'evidence.rizal': {
      scenario: 'Rizal yang belum terverifikasi meminta file Finance Q3 secara mendesak di lift.',
      evidence: {
        unverified_identity: 'Identitas Rizal belum diverifikasi.',
        sensitive_financial_file: 'Permintaan menyangkut file keuangan sensitif.',
        time_pressure: 'Ada tekanan agar permintaan segera dipenuhi.',
        claimed_finance_role: 'Klaim bekerja di Finance bukan bukti kewenangan dan bukan red flag tunggal.',
      },
      choices: {
        refuse_and_verify: choice('Tolak dan verifikasi melalui supervisor', 'best', 'Permintaan ditahan sampai identitas dan kewenangan diverifikasi.'),
        comply: choice('Kirim file Finance Q3', 'dangerous', 'Data sensitif diberikan kepada peminta yang belum terverifikasi.'),
      },
    },
    'evidence.pak_budi': {
      scenario: 'Pak Budi meminta software akuntansi dari attachment email segera dipasang.',
      evidence: {
        unofficial_software_source: 'Software berasal dari email, bukan sistem IT resmi.',
        unverified_sender_claim: 'Sumber hanya disebut sebagai teman di Finance.',
        time_pressure: 'Ada tekanan waktu sebelum meeting.',
        known_employee: 'Pak Budi memang karyawan, tetapi itu tidak memvalidasi attachment.',
      },
      choices: {
        refuse_and_verify: choice('Tolak dan verifikasi file', 'best', 'Software ditahan sampai sumber dan integritasnya diperiksa.'),
        comply: choice('Instal software tersebut', 'dangerous', 'Attachment tidak terverifikasi diteruskan ke installer.'),
      },
    },
    'evidence.computer': {
      scenario: 'Aplikasi chat kantor meminta pengiriman file finansial sensitif tanpa jalur resmi.',
      evidence: {
        sensitive_financial_file: 'File berisi informasi finansial sensitif.',
        unverified_authority: 'Identitas dan kewenangan peminta belum diverifikasi.',
        bypasses_official_channel: 'Permintaan melewati jalur pengiriman resmi.',
        office_chat_channel: 'Aplikasi chat kantor bukan bukti bahwa permintaan aman.',
      },
      choices: {
        refuse: choice('Tolak pengiriman', 'best', 'Pengiriman ditahan untuk verifikasi.'),
        send: choice('Kirim file', 'dangerous', 'Informasi sensitif berpotensi bocor.'),
      },
    },
    'installer.untrusted': {
      scenario: 'Installer berasal dari attachment yang belum diverifikasi.',
      choices: {
        cancel: choice('Batalkan instalasi', 'best', 'Software tidak tepercaya tidak dijalankan.'),
        install: choice('Pasang software', 'dangerous', 'Perangkat terkena risiko malware.'),
      },
    },
    'file_request.computer': {
      scenario: 'Komputer meminta player mengirim file finansial kepada peminta yang belum diverifikasi.',
      choices: {
        refuse: choice('Tolak pengiriman', 'best', 'Data tetap terlindungi sambil menunggu verifikasi.'),
        send: choice('Kirim file', 'dangerous', 'Data sensitif dikirim tanpa verifikasi.'),
      },
    },
  },
  '2fa': {
    'password.creation': {
      scenario: 'Player membuat password baru. Password asli tidak direkam; hanya kriterianya.',
      choices: {
        weak: choice('Password diklasifikasikan Weak', 'dangerous', 'Password tidak memenuhi panjang atau kompleksitas dasar.'),
        medium: choice('Password diklasifikasikan Medium', 'needs_improvement', 'Password memenuhi sebagian kriteria tetapi belum mencapai standar Strong.'),
        strong: choice('Password diklasifikasikan Strong', 'best', 'Password memiliki 12+ karakter, huruf besar, angka, dan simbol.'),
      },
    },
    'mfa.choice': {
      scenario: 'Player memilih apakah akun diberi faktor autentikasi tambahan.',
      choices: {
        enable: choice('Aktifkan MFA', 'best', 'Akun mendapat perlindungan tambahan.'),
        skip: choice('Lewati MFA', 'dangerous', 'Akun hanya bergantung pada password.'),
      },
    },
    'mfa.verification': {
      scenario: 'Player menyelesaikan atau melewati verifikasi OTP. Kode OTP tidak pernah direkam.',
      choices: {
        verified: choice('OTP berhasil diverifikasi', 'best', 'MFA berhasil diaktifkan.'),
        skipped: choice('Verifikasi OTP dilewati', 'dangerous', 'MFA tidak diaktifkan.'),
      },
    },
    'password.outcome': {
      scenario: 'Simulasi menunjukkan dampak gabungan kekuatan password dan MFA.',
      choices: {
        success: choice('Akun bertahan', 'best', 'Kombinasi perlindungan berhasil.'),
        brute_force: choice('Terkena brute-force', 'dangerous', 'Perlindungan belum cukup menahan serangan.'),
        instant_hack: choice('Instant hack', 'dangerous', 'Password lemah tanpa MFA menyebabkan kompromi cepat.'),
      },
    },
  },
  'password-security': {
    'email.belijual_password_expiry': emailEvent('Email security@bel1jual.com menyatakan password akan kedaluwarsa dan meminta perubahan melalui balasan', true, ['lookalike_domain', 'account_pressure', 'reply_for_password_change']),
    'email.lapakkomputer_promotion': emailEvent('Email info@lapakkomputer.com menawarkan promosi produk tanpa meminta data sensitif', false, []),
    'email.bri_account_closure': emailEvent('Email alert@bri-bank-id.com mengancam rekening ditutup dalam 48 jam dan meminta nomor rekening, nama, serta PIN', true, ['urgency', 'unofficial_domain', 'sensitive_data_request']),
    'email.gojek_prize': emailEvent('Email promo@g0jek-hadiah.com menawarkan hadiah GoPay dan meminta data pribadi serta rekening dalam 24 jam', true, ['lookalike_domain', 'prize_lure', 'sensitive_data_request', 'urgency']),
    'email.ahmad_meeting': emailEvent('Email internal ahmad@perusahaan.com mengingatkan meeting dan laporan mingguan', false, []),
    'email.hr_salary_slip': emailEvent('Email internal hr@perusahaan.com memberi tahu slip gaji tersedia melalui portal HRIS', false, []),
    'email.tokopedia_account_verification': emailEvent('Email noreply@t0kopedia.com mengancam akun diblokir dan meminta username serta password melalui balasan', true, ['lookalike_domain', 'account_pressure', 'credential_request']),
    'email.budi_proposal_revision': emailEvent('Email internal budi@perusahaan.com meminta pengecekan revisi proposal', false, []),
  },
  'malware-awareness': {
    'website.google_lookalike': websiteEvent('https://go0gle.com/login', true, ['lookalike_domain', 'credential_form']),
    'website.microsoft_missing_tld': websiteEvent('http://microsoft/logn', true, ['missing_https', 'malformed_domain', 'misspelled_path']),
    'website.secure_bank_deceptive_subdomain': websiteEvent('http://secure-bank.tk.com', true, ['missing_https', 'deceptive_subdomain', 'unrelated_domain']),
    'website.paypal_lookalike': websiteEvent('http://paypa1.com', true, ['missing_https', 'lookalike_domain']),
    'website.facebook_lookalike': websiteEvent('http://faceb00k.com', true, ['missing_https', 'lookalike_domain']),
    'website.google_official': websiteEvent('https://google.com', false, []),
    'website.github_official': websiteEvent('https://github.com', false, []),
    'website.microsoft_official': websiteEvent('https://microsoft.com', false, []),
    'popup.fake_virus_cleanup': popupEvent('Warning - Virus Terdeteksi', true, 'Tutup', 'Bersihkan Sekarang', ['scare_tactic', 'urgent_cleanup_action']),
    'popup.fake_defender_scan': popupEvent('Peringatan keamanan kritis palsu', true, 'Tutup', 'Pindai Sekarang', ['impersonated_security_tool', 'scare_tactic']),
    'popup.fake_iphone_prize': popupEvent('Hadiah iPhone palsu', true, 'Tutup', 'Klaim Hadiah', ['prize_lure', 'unexpected_popup']),
    'popup.fake_microsoft_support': popupEvent('Microsoft Security Alert palsu', true, 'Tutup', 'Hubungi Sekarang', ['impersonated_vendor', 'phone_support_scam', 'scare_tactic']),
    'popup.windows_update': popupEvent('Windows Update legitimate', false, 'Restart Sekarang', 'Tolak'),
    'popup.storage_low': popupEvent('Penyimpanan hampir penuh legitimate', false, 'Bersihkan', 'Abaikan'),
    'popup.windows_firewall': popupEvent('Windows Firewall legitimate', false, 'Izinkan Akses', 'Blokir'),
  },
  'wifi-security': {
    'wifi.clue.bu_dewi': {
      scenario: 'Bu Dewi terhubung ke CafeBean_Free_5G dan baru mengirim dokumen kontrak.',
      choices: { assist_victim: choice('Peringatkan dan bantu Bu Dewi', 'best', 'Korban diminta disconnect, mengganti password, dan memeriksa aktivitas login.') },
    },
    'wifi.clue.budi': {
      scenario: 'Budi melihat pria dengan laptop, kabel, dan router portable di ruang karaoke.',
      choices: { investigate: choice('Tindak lanjuti petunjuk Budi', 'best', 'Lokasi operator rogue access point ditemukan.') },
    },
    'wifi.clue.karaoke': {
      scenario: 'Player melihat router portable aktif di meja Mas Anto.',
      choices: { inspect_device: choice('Periksa perangkat', 'best', 'Sumber jaringan palsu dikonfirmasi.') },
    },
    'wifi.mas_anto_response': {
      scenario: 'Mas Anto mengoperasikan FREE_CAFE_WIFI dan CafeBean_Free_5G untuk mengarahkan korban ke login palsu.',
      choices: {
        report_to_staff: choice('Saya sudah panggil staff.', 'best', 'Security datang dan perangkat diamankan.'),
        threaten_to_report: choice('Matikan sekarang, atau saya panggil staff.', 'partial', 'Router dimatikan, tetapi pelaku pergi tanpa perangkat diamankan.'),
        ask_motive: choice('Kenapa kamu lakuin ini?', 'weak', 'Router dimatikan tanpa laporan atau konsekuensi resmi.'),
      },
    },
    'wifi.selection': {
      scenario: 'Player harus memilih SSID resmi kafe dan menghindari jaringan yang dibuat Mas Anto.',
      choices: {
        cafebean_free: choice('CafeBean_Free', 'best', 'Terhubung ke jaringan resmi kafe.'),
        cafebean_free_5g: choice('CafeBean_Free_5G', 'dangerous', 'Terhubung ke rogue access point look-alike.'),
        free_cafe_wifi: choice('FREE_CAFE_WIFI', 'dangerous', 'Terhubung ke rogue access point dengan nama generik.'),
        unknown_network: choice('Jaringan tidak dikenal', 'dangerous', 'Keaslian jaringan tidak dapat diverifikasi.'),
      },
    },
    'vpn.choice': {
      scenario: 'Koneksi Wi-Fi publik meminta player memilih perlindungan VPN.',
      choices: {
        enable: choice('Aktifkan VPN', 'best', 'Koneksi diberi perlindungan tambahan.'),
        skip: choice('Lewati VPN', 'dangerous', 'Login dilakukan melalui koneksi yang tidak terlindungi.'),
      },
    },
    'public_wifi.login': {
      scenario: 'Player melakukan login setelah menentukan status VPN.',
      choices: {
        with_vpn: choice('Login dengan VPN', 'best', 'Simulasi menyatakan credential terlindungi.'),
        without_vpn: choice('Login tanpa VPN', 'dangerous', 'Simulasi menyatakan credential disadap.'),
      },
    },
  },
  ransomware: {
    'file.organization': {
      scenario: 'Tutorial sengaja memindahkan Important_Project ke Trash ketika file penting di-drag. Ini adalah mekanik scenario, bukan kesalahan bebas player.',
      choices: { important_project_moved: choice('Important_Project terpicu masuk Trash', 'scenario', 'Player diminta melakukan recovery.') },
    },
    'file.recovery': {
      scenario: 'Player memilih file dari Trash untuk dipulihkan sebelum crash.',
      choices: {
        important_project: choice('Important_Project', 'best', 'File penting dipulihkan sebelum crash.'),
        'work_report.pdf': choice('work_report.pdf', 'contextual', 'File kerja biasa dipulihkan.'),
        'presentation.pptx': choice('presentation.pptx', 'contextual', 'File presentasi dipulihkan.'),
        'image_pribadi.jpg': choice('image_pribadi.jpg', 'contextual', 'File pribadi dipulihkan.'),
        'vacation.png': choice('vacation.png', 'contextual', 'File pribadi dipulihkan.'),
        'random.zip': choice('random.zip', 'contextual', 'File lain dipulihkan.'),
        'temp_file.tmp': choice('temp_file.tmp', 'contextual', 'File sementara dipulihkan.'),
        unknown_file: choice('File tidak dikenal', 'contextual', 'Identitas file tidak tercatat.'),
      },
    },
    'backup.exists': {
      scenario: 'Setelah ransomware, player menyatakan apakah backup tersedia.',
      choices: {
        yes: choice('Ada backup', 'best', 'Recovery dapat dilanjutkan.'),
        no: choice('Tidak ada backup', 'dangerous', 'Data tidak dapat dipulihkan.'),
      },
    },
    'backup.recovery_source': {
      scenario: 'Player memilih sumber backup yang sudah tersedia.',
      choices: {
        cloud: choice('Cloud Backup', 'safe', 'Data dipulihkan dari cloud.'),
        external_drive: choice('External Drive', 'safe', 'Data dipulihkan dari drive eksternal.'),
      },
    },
    'backup.configuration': {
      scenario: 'Player mengatur lokasi dan jadwal backup baru. Lokasi aman: External Drive atau Cloud. Jadwal yang diterima: Daily.',
      choices: {
        reliable: choice('Konfigurasi reliable', 'best', 'Simulasi backup berhasil.'),
        unreliable: choice('Konfigurasi unreliable', 'dangerous', 'Simulasi backup gagal.'),
      },
    },
  },
};

function emailEvent(name, isPhishing, riskIndicators) {
  return {
    scenario: `${name}. Klasifikasi sebenarnya: ${isPhishing ? 'phishing' : 'legitimate'}.`,
    riskIndicators,
    choices: isPhishing
      ? {
          balas: choice('Balas', 'dangerous', 'Wrong: player berinteraksi dengan phishing.'),
          hapus: choice('Hapus', 'neutral', 'Neutral: ancaman dibuang tetapi tidak dilaporkan.'),
          laporkan: choice('Laporkan', 'best', 'Correct: phishing dilaporkan.'),
        }
      : {
          balas: choice('Balas', 'best', 'Correct: email legitimate ditangani normal.'),
          hapus: choice('Hapus', 'dangerous', 'Wrong: email legitimate dihapus.'),
          laporkan: choice('Laporkan', 'neutral', 'Neutral: email legitimate salah dilaporkan.'),
        },
  };
}

function websiteEvent(url, isPhishing, riskIndicators) {
  return {
    scenario: `URL yang tampil: ${url}. Klasifikasi sebenarnya: ${isPhishing ? 'phishing' : 'legitimate'}.`,
    riskIndicators,
    choices: isPhishing
      ? {
          login: choice('Login', 'dangerous', 'Credential dimasukkan ke website phishing dan run gagal.'),
          cancel: choice('Cancel', 'best', 'Login phishing dibatalkan.'),
        }
      : {
          login: choice('Login', 'best', 'Website legitimate dipilih dan challenge dilanjutkan.'),
          cancel: choice('Cancel', 'weak', 'Website legitimate dilewatkan.'),
        },
  };
}

function popupEvent(name, isPhishing, correctLabel, wrongLabel, riskIndicators = []) {
  return {
    scenario: `${name}. Klasifikasi sebenarnya: ${isPhishing ? 'popup phishing/fake' : 'notifikasi legitimate'}.`,
    riskIndicators,
    choices: {
      correct_action: choice(correctLabel, 'best', 'Aksi yang benar untuk popup ini.'),
      wrong_action: choice(wrongLabel, 'dangerous', 'Aksi yang salah untuk popup ini.'),
    },
  };
}

export function resolveDecisionContext(stageId, decision) {
  if (!decision || typeof decision.eventId !== 'string' || typeof decision.choiceId !== 'string') return null;

  const legacy = resolveLegacyDecisionContext(stageId, decision);
  if (legacy) return legacy;

  if (decision.eventId.startsWith('vn.')) {
    if (!['accept', 'reject'].includes(decision.choiceId)) return null;
    return formatDecision(decision, {
      scenario: 'Pilihan cabang pada visual novel.',
      choices: {
        accept: choice('Accept', 'contextual', 'Cabang accept dipilih.'),
        reject: choice('Reject', 'contextual', 'Cabang reject dipilih.'),
      },
    });
  }

  const evaluationMatch = /^evaluation\.question_([1-5])$/.exec(decision.eventId);
  if (evaluationMatch) {
    const item = TOPIC_EVALUATIONS[stageId]?.[Number(evaluationMatch[1]) - 1];
    if (!item || !item.choices[decision.choiceId]) return null;
    return formatDecision(decision, {
      scenario: item.question,
      choices: item.choices,
      correctChoiceId: item.correctChoiceId,
      explanation: item.explanation,
    });
  }

  const event = EVENTS[stageId]?.[decision.eventId];
  if (!event) return null;
  if (!event.choices?.[decision.choiceId]) return null;
  return formatDecision(decision, event);
}

function resolveLegacyDecisionContext(stageId, decision) {
  const remap = (eventId, choiceId, outcomeId = decision.outcomeId) =>
    resolveDecisionContext(stageId, { ...decision, eventId, choiceId, outcomeId });

  if (decision.eventId.startsWith('evaluation.question_') && ['correct', 'incorrect'].includes(decision.choiceId)) {
    const match = /^evaluation\.question_([1-5])$/.exec(decision.eventId);
    const item = match ? TOPIC_EVALUATIONS[stageId]?.[Number(match[1]) - 1] : null;
    if (!item) return null;
    const selectedChoiceId = decision.choiceId === 'correct'
      ? item.correctChoiceId
      : item.correctChoiceId === 'choice_a' ? 'choice_b' : 'choice_a';
    return formatDecision({ ...decision, choiceId: selectedChoiceId, outcomeId: decision.choiceId }, {
      scenario: item.question,
      choices: item.choices,
      correctChoiceId: item.correctChoiceId,
      explanation: item.explanation,
    });
  }

  if (stageId === 'phishing') {
    const evidence = {
      'evidence.topic1evidence_rizal': 'evidence.rizal',
      'evidence.topic1evidence_pakbudi': 'evidence.pak_budi',
      'evidence.topic1evidence_computer': 'evidence.computer',
    }[decision.eventId];
    if (evidence && ['safe', 'unsafe'].includes(decision.choiceId)) {
      const newChoice = evidence === 'evidence.computer'
        ? decision.choiceId === 'safe' ? 'refuse' : 'send'
        : decision.choiceId === 'safe' ? 'refuse_and_verify' : 'comply';
      return remap(evidence, newChoice, decision.choiceId);
    }
    if (decision.eventId === 'installer.untrusted' && ['installed', 'cancelled'].includes(decision.choiceId))
      return remap(decision.eventId, decision.choiceId === 'installed' ? 'install' : 'cancel');
    if (decision.eventId === 'file_request.computer' && ['refused', 'sent'].includes(decision.choiceId))
      return remap(decision.eventId, decision.choiceId === 'refused' ? 'refuse' : 'send');
  }

  if (stageId === '2fa') {
    if (decision.eventId === 'password.strength') return remap('password.creation', decision.choiceId);
    if (decision.eventId === 'mfa.choice' && ['enabled', 'skipped'].includes(decision.choiceId))
      return remap('mfa.choice', decision.choiceId === 'enabled' ? 'enable' : 'skip');
  }

  if (stageId === 'password-security' && ['email.phishing', 'email.legitimate'].includes(decision.eventId)) {
    const [action, recordedOutcome = null] = decision.choiceId.split('.');
    const isPhishing = decision.eventId.endsWith('.phishing');
    return formatDecision({ ...decision, choiceId: action, outcomeId: recordedOutcome }, emailEvent(
      isPhishing ? 'Email phishing lama tanpa template ID' : 'Email legitimate lama tanpa template ID',
      isPhishing,
      [],
    ));
  }

  if (stageId === 'malware-awareness') {
    const website = /^website\.round_\d+\.(phishing|legitimate)$/.exec(decision.eventId);
    if (website && ['login', 'cancel'].includes(decision.choiceId))
      return formatDecision(decision, websiteEvent('URL lama tanpa template ID', website[1] === 'phishing', []));
    const popup = /^popup\.round_\d+\.(phishing|legitimate)$/.exec(decision.eventId);
    if (popup && ['correct', 'incorrect'].includes(decision.choiceId)) {
      const correct = decision.choiceId === 'correct';
      return formatDecision({ ...decision, choiceId: correct ? 'correct_action' : 'wrong_action', outcomeId: decision.choiceId },
        popupEvent('Popup lama tanpa template ID', popup[1] === 'phishing', 'Aksi benar', 'Aksi salah'));
    }
  }

  if (stageId === 'wifi-security') {
    if (decision.eventId === 'wifi.selection' && ['office_secure', 'lookalike_network'].includes(decision.choiceId))
      return remap('wifi.selection', decision.choiceId === 'office_secure' ? 'cafebean_free' : 'unknown_network');
    if (decision.eventId === 'vpn.choice' && ['enabled', 'skipped'].includes(decision.choiceId))
      return remap('vpn.choice', decision.choiceId === 'enabled' ? 'enable' : 'skip');
    if (decision.eventId === 'wifi.investigation_response' && ['choice_a', 'choice_b', 'choice_c'].includes(decision.choiceId)) {
      const mapped = { choice_a: 'report_to_staff', choice_b: 'threaten_to_report', choice_c: 'ask_motive' }[decision.choiceId];
      return remap('wifi.mas_anto_response', mapped);
    }
  }

  if (stageId === 'ransomware') {
    if (decision.eventId === 'file.organization' && decision.choiceId === 'important_file_lost')
      return remap('file.organization', 'important_project_moved', 'scenario_forced_loss');
    if (decision.eventId === 'file.recovery' && decision.choiceId === 'attempted')
      return remap('file.recovery', 'unknown_file');
  }

  return null;
}

function formatDecision(decision, event) {
  const selected = event.choices[decision.choiceId];
  return {
    eventId: decision.eventId,
    scenario: event.scenario,
    selectedChoice: { id: decision.choiceId, ...selected },
    availableChoices: Object.entries(event.choices).map(([id, value]) => ({ id, ...value })),
    scoreDelta: EXPECTED_SCORE_DELTAS[`${decision.eventId}:${decision.choiceId}`] || 0,
    elapsedSeconds: decision.elapsedSeconds,
    riskIndicators: (event.riskIndicators || []).map((id) => ({ id, meaning: RISK_INDICATOR_LABELS[id] || id })),
    evidenceCatalog: event.evidence || undefined,
    facts: Array.isArray(decision.facts) ? decision.facts : [],
    correctChoiceId: event.correctChoiceId,
    explanation: event.explanation,
  };
}
