import { requireAdmin } from '../../../lib/auth.js';
import { sql } from '../../../lib/db.js';
import { deletePosterObject } from '../../../lib/r2.js';

// Admin-only. Removes one poster from the slideshow.
export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const auth = await requireAdmin(req);
  if (auth.error) return res.status(auth.status).json({ error: auth.error });

  const id = Number.parseInt(req.body?.id, 10);
  if (!Number.isInteger(id) || id <= 0) {
    return res.status(400).json({ error: 'Invalid id' });
  }

  // DB row goes first, on purpose. If the order were reversed and this step
  // failed, a row would point at a file that no longer exists and players would
  // see a broken image. This way the worst case is an unreferenced file in the
  // bucket — invisible to players, and recorded below so it can be swept later.
  let objectKey;
  try {
    const rows = await sql`DELETE FROM posters WHERE id = ${id} RETURNING object_key`;
    if (!rows.length) return res.status(404).json({ error: 'Poster not found' });
    objectKey = rows[0].object_key;
  } catch (err) {
    console.error('[poster/delete] delete row failed:', err.message);
    return res.status(500).json({ error: 'Database error' });
  }

  try {
    await deletePosterObject(objectKey);
  } catch (err) {
    console.error('[poster/delete] R2 delete failed:', objectKey, err.message);
    await sql`
      INSERT INTO poster_orphans (object_key, last_error)
      VALUES (${objectKey}, ${err.message})
      ON CONFLICT (object_key) DO UPDATE
        SET failed_at = NOW(), last_error = EXCLUDED.last_error
    `.catch((logErr) => {
      console.error('[poster/delete] orphan log failed:', logErr.message);
    });
    // Deliberately not an error response: from the player's side the poster is
    // already gone, which is what the admin asked for.
  }

  // Close the gap the deleted row left, so sort_order stays 1..N.
  try {
    await sql`
      WITH ranked AS (
        SELECT id, ROW_NUMBER() OVER (ORDER BY sort_order, id) AS n
          FROM posters WHERE is_active = TRUE
      )
      UPDATE posters p SET sort_order = ranked.n
        FROM ranked WHERE p.id = ranked.id AND p.sort_order <> ranked.n
    `;
  } catch (err) {
    // Cosmetic only — gaps in sort_order do not change the display order.
    console.error('[poster/delete] resequence failed:', err.message);
  }

  return res.status(200).json({ success: true, id });
}
