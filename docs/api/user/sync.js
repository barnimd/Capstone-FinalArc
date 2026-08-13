import { verifyToken } from '../../lib/auth.js';
import { sql } from '../../lib/db.js';

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const { uid, email, name, error } = await verifyToken(req);
  if (error) return res.status(401).json({ error });

  const displayName = (req.body && typeof req.body.displayName === 'string')
    ? req.body.displayName.slice(0, 100)
    : (name || null);

  // Gender is written here and nowhere else. The player picks it in the
  // CharacterSelect scene, which runs after auth on all three entry points
  // (guest callsign, email login, signup), so guest/login.js never has to
  // know about it.
  //
  // Anything other than 'male'/'female' is dropped rather than rejected: an
  // unrecognised value should not fail a login the player did nothing wrong in.
  const raw = req.body?.gender;
  const gender = (raw === 'male' || raw === 'female') ? raw : null;

  try {
    const rows = await sql`
      INSERT INTO users (user_id, display_name, email, gender, gender_chosen, created_at, last_login)
      VALUES (
        ${uid}, ${displayName}, ${email},
        ${gender ?? 'male'},
        ${gender !== null},
        NOW(), NOW()
      )
      ON CONFLICT (user_id) DO UPDATE
        SET display_name = COALESCE(EXCLUDED.display_name, users.display_name),
            email        = COALESCE(EXCLUDED.email, users.email),
            last_login   = NOW(),
            -- The choice is permanent. Enforced here, not only in the UI, because
            -- a client can post this endpoint directly.
            gender = CASE
                       WHEN users.gender_chosen THEN users.gender
                       ELSE COALESCE(${gender}, users.gender)
                     END,
            gender_chosen = users.gender_chosen OR ${gender !== null}
      RETURNING user_id, display_name, email, role, gender, gender_chosen, created_at, last_login
    `;
    return res.status(200).json({ success: true, user: rows[0] });
  } catch (err) {
    return res.status(500).json({ error: 'Database error', detail: err.message });
  }
}
