import { verifyToken } from '../../lib/auth.js';
import { sql } from '../../lib/db.js';
import { isValidStage } from '../../lib/stages.js';

const MAX_CHECKPOINT_BYTES = 50 * 1024;

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const { uid, error } = await verifyToken(req);
  if (error) return res.status(401).json({ error });

  const { stageId, checkpointData } = req.body || {};
  if (!isValidStage(stageId)) return res.status(400).json({ error: 'Invalid stageId' });
  if (checkpointData === undefined || checkpointData === null) {
    return res.status(400).json({ error: 'checkpointData required' });
  }

  const json = JSON.stringify(checkpointData);
  if (Buffer.byteLength(json, 'utf8') > MAX_CHECKPOINT_BYTES) {
    return res.status(400).json({ error: 'checkpointData exceeds 50KB' });
  }

  try {
    const completed = await sql`
      SELECT 1 FROM stage_completions WHERE user_id = ${uid} AND stage_id = ${stageId}
    `;
    if (completed.length > 0) {
      // Allow checkpoint on replay (game flow decision: replay allowed)
    }

    const rows = await sql`
      INSERT INTO checkpoints (user_id, stage_id, checkpoint_data, updated_at)
      VALUES (${uid}, ${stageId}, ${json}::jsonb, NOW())
      ON CONFLICT (user_id, stage_id) DO UPDATE
        SET checkpoint_data = EXCLUDED.checkpoint_data,
            updated_at      = NOW()
      RETURNING id, updated_at
    `;
    return res.status(200).json({
      success: true,
      checkpointId: rows[0].id,
      savedAt: rows[0].updated_at,
    });
  } catch (err) {
    return res.status(500).json({ error: 'Database error', detail: err.message });
  }
}
