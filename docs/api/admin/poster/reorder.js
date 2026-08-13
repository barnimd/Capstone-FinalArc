import { requireAdmin } from '../../../lib/auth.js';
import { sql } from '../../../lib/db.js';

// Admin-only. Rewrites slide order from a full list of ids, in the order the
// admin arranged them. Touches nothing in R2 — this is one integer column.
export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const auth = await requireAdmin(req);
  if (auth.error) return res.status(auth.status).json({ error: auth.error });

  const { ids } = req.body || {};
  if (!Array.isArray(ids) || !ids.length) {
    return res.status(400).json({ error: 'ids harus array yang tidak kosong' });
  }
  if (!ids.every((id) => Number.isInteger(id) && id > 0)) {
    return res.status(400).json({ error: 'ids harus berisi angka' });
  }
  if (new Set(ids).size !== ids.length) {
    return res.status(400).json({ error: 'ids tidak boleh duplikat' });
  }

  try {
    // The list must match the active posters exactly. A partial list would
    // leave the posters it omits holding stale positions, silently colliding
    // with the new ones.
    const rows = await sql`SELECT id FROM posters WHERE is_active = TRUE`;
    const active = new Set(rows.map((r) => r.id));
    if (active.size !== ids.length || !ids.every((id) => active.has(id))) {
      return res.status(400).json({ error: 'ids harus cocok persis dengan poster aktif' });
    }

    // WITH ORDINALITY turns array position into the new sort_order, so the whole
    // reorder lands in one statement instead of N racing updates.
    await sql`
      UPDATE posters p
         SET sort_order = v.ord
        FROM (
          SELECT * FROM UNNEST(${ids}::int[]) WITH ORDINALITY AS t(id, ord)
        ) AS v
       WHERE p.id = v.id
    `;

    return res.status(200).json({ success: true, ids });
  } catch (err) {
    console.error('[poster/reorder] failed:', err.message);
    return res.status(500).json({ error: 'Database error' });
  }
}
