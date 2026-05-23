import { verifyToken } from '../../lib/auth.js';
import { sql } from '../../lib/db.js';
import { isValidStage, getMaxScore } from '../../lib/stages.js';

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const { uid, error } = await verifyToken(req);
  if (error) return res.status(401).json({ error });

  const { stageId, finalScore } = req.body || {};
  if (!isValidStage(stageId)) return res.status(400).json({ error: 'Invalid stageId' });

  const score = Number(finalScore);
  if (!Number.isFinite(score) || score < 0) {
    return res.status(400).json({ error: 'finalScore must be a non-negative number' });
  }
  const max = getMaxScore(stageId);
  if (score > max) {
    return res.status(400).json({ error: `finalScore exceeds maxScore (${max}) for stage` });
  }

  try {
    // Mark stage complete (keep highest score on replay via GREATEST)
    await sql`
      INSERT INTO stage_completions (user_id, stage_id, completed_at, final_score)
      VALUES (${uid}, ${stageId}, NOW(), ${score})
      ON CONFLICT (user_id, stage_id) DO UPDATE
        SET final_score  = GREATEST(stage_completions.final_score, EXCLUDED.final_score),
            completed_at = NOW()
    `;

    // Also drop the active checkpoint (stage done)
    await sql`DELETE FROM checkpoints WHERE user_id = ${uid} AND stage_id = ${stageId}`;

    return res.status(200).json({ success: true, stageId, finalScore: score });
  } catch (err) {
    return res.status(500).json({ error: 'Database error', detail: err.message });
  }
}
