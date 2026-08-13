import { verifyToken } from '../lib/auth.js';
import { sql } from '../lib/db.js';
import { isValidStage } from '../lib/stages.js';

const MAX_CHECKPOINT_BYTES = 50 * 1024;

/**
 * Single handler behind /api/checkpoint/load and /api/checkpoint/save.
 *
 * Merged for the same reason as api/ai/coach.js: Vercel counts one Serverless
 * Function per file and the Hobby plan caps a deployment at 12. The public URLs
 * are preserved by the `rewrites` block in vercel.json, so Unity is unchanged.
 */
export default async function handler(req, res) {
  switch (req.query?.action) {
    case 'load': return handleLoad(req, res);
    case 'save': return handleSave(req, res);
    default:     return res.status(404).json({ error: 'Not found' });
  }
}

// ── GET /api/checkpoint/load ────────────────────────────────────────────────

async function handleLoad(req, res) {
  if (req.method !== 'GET') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const { uid, error } = await verifyToken(req);
  if (error) return res.status(401).json({ error });

  const stageId = req.query?.stageId;
  if (!isValidStage(stageId)) return res.status(400).json({ error: 'Invalid stageId' });

  try {
    const [completionRows, checkpointRows] = await Promise.all([
      sql`SELECT final_score, completed_at FROM stage_completions
          WHERE user_id = ${uid} AND stage_id = ${stageId}`,
      sql`SELECT checkpoint_data, updated_at FROM checkpoints
          WHERE user_id = ${uid} AND stage_id = ${stageId}`,
    ]);

    const isCompleted = completionRows.length > 0;
    const hasCheckpoint = checkpointRows.length > 0;

    return res.status(200).json({
      success: true,
      stageId,
      isCompleted,
      bestScore: isCompleted ? completionRows[0].final_score : null,
      completedAt: isCompleted ? completionRows[0].completed_at : null,
      hasCheckpoint,
      checkpoint: hasCheckpoint ? checkpointRows[0].checkpoint_data : null,
      checkpointUpdatedAt: hasCheckpoint ? checkpointRows[0].updated_at : null,
    });
  } catch (err) {
    return res.status(500).json({ error: 'Database error', detail: err.message });
  }
}

// ── POST /api/checkpoint/save ───────────────────────────────────────────────

async function handleSave(req, res) {
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
