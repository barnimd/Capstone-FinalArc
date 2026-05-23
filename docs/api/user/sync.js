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

  try {
    const rows = await sql`
      INSERT INTO users (user_id, display_name, email, created_at, last_login)
      VALUES (${uid}, ${displayName}, ${email}, NOW(), NOW())
      ON CONFLICT (user_id) DO UPDATE
        SET display_name = COALESCE(EXCLUDED.display_name, users.display_name),
            email        = COALESCE(EXCLUDED.email, users.email),
            last_login   = NOW()
      RETURNING user_id, display_name, email, created_at, last_login
    `;
    return res.status(200).json({ success: true, user: rows[0] });
  } catch (err) {
    return res.status(500).json({ error: 'Database error', detail: err.message });
  }
}
