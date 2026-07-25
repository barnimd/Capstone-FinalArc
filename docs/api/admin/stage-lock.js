import { requireAdmin } from '../../lib/auth.js';
import { sql } from '../../lib/db.js';
import { isValidStage } from '../../lib/stages.js';

// Admin-only. Locks or unlocks one class level for every player.
// The result is read back by all clients through GET /api/stages.
export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const auth = await requireAdmin(req);
  if (auth.error) return res.status(auth.status).json({ error: auth.error });

  const { stageId, locked } = req.body || {};
  if (!isValidStage(stageId)) {
    return res.status(400).json({ error: 'Invalid stageId' });
  }
  if (typeof locked !== 'boolean') {
    return res.status(400).json({ error: 'locked must be a boolean' });
  }

  try {
    await sql`
      INSERT INTO stage_locks (stage_id, is_locked, updated_by, updated_at)
      VALUES (${stageId}, ${locked}, ${auth.uid}, NOW())
      ON CONFLICT (stage_id) DO UPDATE
        SET is_locked  = EXCLUDED.is_locked,
            updated_by = EXCLUDED.updated_by,
            updated_at = NOW()
    `;
    return res.status(200).json({ success: true, stageId, locked });
  } catch (err) {
    return res.status(500).json({ error: 'Database error', detail: err.message });
  }
}
