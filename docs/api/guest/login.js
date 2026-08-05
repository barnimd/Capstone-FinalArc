import crypto from 'node:crypto';
import { admin } from '../../lib/auth.js';
import { sql } from '../../lib/db.js';

// Guest login tanpa password. Ini SATU-SATUNYA endpoint yang tidak memanggil
// verifyToken(), karena inilah proses login-nya — belum ada token untuk diverifikasi.
//
// Kenapa custom token, bukan anonymous auth: Firebase anonymous selalu menerbitkan
// UID baru tiap sign-in, jadi callsign yang sama dapat baris users berbeda tiap hari
// dan progress-nya hilang. Di sini server yang menentukan UID ('guest_' + callsign
// lowercase), jadi callsign yang sama selalu memetakan ke baris Neon yang sama.
//
// Aturan "1 callsign 1 hari" dipegang device_hash + last_login:
//   belum ada callsign            -> buat akun, ikat ke device ini      (new)
//   device_hash NULL (data lama)  -> klaim, ikat ke device ini          (claimed)
//   device cocok                  -> resume, kapan saja                 (resumed)
//   device beda, login hari ini   -> ditolak                            (IN_USE_TODAY)
//   device beda, login < hari ini -> resume + ikat ulang ke device baru (rebound)

// Harus sama dengan validasi di Assets/!Script/UI/CallsignController.cs
const CALLSIGN_RE = /^[a-z0-9]{3,16}$/;

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const body = req.body || {};
  const callsign = typeof body.callsign === 'string' ? body.callsign.trim() : '';
  const deviceId = typeof body.deviceId === 'string' ? body.deviceId.trim() : '';

  const normalized = callsign.toLowerCase();
  if (!CALLSIGN_RE.test(normalized)) {
    return res.status(200).json({ success: false, reason: 'INVALID_CALLSIGN' });
  }
  if (!deviceId) {
    return res.status(200).json({ success: false, reason: 'MISSING_DEVICE_ID' });
  }

  const uid = 'guest_' + normalized;
  const deviceHash = crypto.createHash('sha256').update(deviceId).digest('hex');

  try {
    // INSERT dulu, bukan SELECT dulu: ON CONFLICT DO NOTHING ... RETURNING bikin
    // pembuatan akun atomik, jadi dua device yang menembak callsign baru yang sama
    // pada saat bersamaan tidak bisa dua-duanya mengklaim sebagai pemilik.
    const created = await sql`
      INSERT INTO users (user_id, display_name, device_hash, created_at, last_login)
      VALUES (${uid}, ${callsign}, ${deviceHash}, NOW(), NOW())
      ON CONFLICT (user_id) DO NOTHING
      RETURNING user_id
    `;

    let status = 'new';

    if (created.length === 0) {
      // Callsign sudah ada. Keputusan boleh-tidaknya diambil DI DALAM satu statement,
      // bukan SELECT-lalu-UPDATE. Dua alasannya:
      //   1. Tidak ada celah balapan antara membaca dan menulis device_hash.
      //   2. Hasil perbandingan tanggal tidak perlu bolak-balik jadi boolean JS —
      //      versi sebelumnya mengembalikan kolom boolean yang selalu terbaca falsy
      //      di sisi Node, jadi device lain ikut lolos padahal harus ditolak.
      // 0 baris kembali = aturan harian menolak login ini.
      //
      // 'Hari ini' dihitung di Asia/Jakarta, bukan UTC, supaya batas harinya
      // jam 00:00 WIB dan bukan jam 07:00 WIB.
      //
      // last_login WAJIB di-cast ke timestamptz dulu. Kalau kolomnya ternyata
      // 'timestamp without time zone', 'AT TIME ZONE' akan membaca jam UTC yang
      // tersimpan itu seolah-olah jam Jakarta lalu mengembalikan timestamptz —
      // hasil ::date-nya meleset satu hari ke belakang sepanjang pagi/siang WIB,
      // jadi is_stale selalu true dan aturan hariannya tidak pernah menggigit.
      // Cast ini benar untuk kedua tipe kolom: no-op kalau sudah timestamptz.
      const claimed = await sql`
        WITH before AS (
          SELECT device_hash AS old_hash,
                 (last_login::timestamptz AT TIME ZONE 'Asia/Jakarta')::date
                   < (NOW()               AT TIME ZONE 'Asia/Jakarta')::date AS is_stale
          FROM users
          WHERE user_id = ${uid}
        )
        UPDATE users u
           SET device_hash  = ${deviceHash},
               display_name = ${callsign},
               last_login   = NOW()
          FROM before b
         WHERE u.user_id = ${uid}
           AND (
                 b.old_hash IS NULL          -- belum pernah diikat (hasil migrasi data lama)
              OR b.old_hash = ${deviceHash}  -- device yang sama, resume kapan saja
              OR b.is_stale                  -- device lain, tapi login terakhirnya bukan hari ini
           )
        RETURNING b.old_hash
      `;

      if (claimed.length === 0) {
        return res.status(200).json({ success: false, reason: 'IN_USE_TODAY' });
      }

      const oldHash = claimed[0].old_hash;
      status = oldHash === null       ? 'claimed'
             : oldHash === deviceHash ? 'resumed'
             :                          'rebound';
    }

    // Ditandatangani lokal pakai FIREBASE_PRIVATE_KEY — tidak perlu IAM role tambahan.
    const customToken = await admin.auth().createCustomToken(uid, {
      callsign,
      guest: true,
    });

    return res.status(200).json({
      success: true,
      customToken,
      uid,
      callsign,
      status,
      isReturning: status !== 'new',
    });
  } catch (err) {
    return res.status(500).json({ error: 'Guest login failed', detail: err.message });
  }
}
