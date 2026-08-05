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
      // Callsign sudah ada. 'Hari ini' dihitung di Asia/Jakarta, bukan UTC, supaya
      // batas harinya jam 00:00 WIB dan bukan jam 07:00 WIB.
      const rows = await sql`
        SELECT device_hash,
               (last_login AT TIME ZONE 'Asia/Jakarta')::date
                 = (NOW()   AT TIME ZONE 'Asia/Jakarta')::date AS logged_in_today
        FROM users
        WHERE user_id = ${uid}
      `;

      const row = rows[0];
      if (!row) {
        return res.status(500).json({ error: 'Database error', detail: 'user row missing after upsert' });
      }

      if (row.device_hash === null) {
        // Baris ada tapi belum pernah diikat ke device mana pun — hasil migrasi data
        // lama. Pemain pertama yang mengetik callsign ini mengklaimnya; memblokir
        // sampai besok justru mengunci pemiliknya dari progress-nya sendiri.
        status = 'claimed';
      } else if (row.device_hash === deviceHash) {
        status = 'resumed';
      } else if (row.logged_in_today) {
        return res.status(200).json({ success: false, reason: 'IN_USE_TODAY' });
      } else {
        status = 'rebound';
      }

      // display_name ikut ditulis ulang supaya kapitalisasi terbaru yang diketik
      // pemain ("Ahmad") yang tampil di profil dan leaderboard.
      await sql`
        UPDATE users
           SET device_hash  = ${deviceHash},
               display_name = ${callsign},
               last_login   = NOW()
         WHERE user_id = ${uid}
      `;
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
