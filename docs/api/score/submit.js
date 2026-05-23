import { verifyToken } from '../../lib/auth.js';
import { sql } from '../../lib/db.js';
import { isValidStage, getMaxScore } from '../../lib/stages.js';

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const { uid, error } = await verifyToken(req);
  if (error) return res.status(401).json({ error });

  const { stageId, score } = req.body || {};
  if (!isValidStage(stageId)) return res.status(400).json({ error: 'Invalid stageId' });

  const value = Number(score);
  if (!Number.isFinite(value) || value < 0) {
    return res.status(400).json({ error: 'score must be a non-negative number' });
  }
  const max = getMaxScore(stageId);
  if (value > max) {
    return res.status(400).json({ error: `score exceeds maxScore (${max}) for stage` });
  }

  try {
    // Anti-cheat: only accept score submissions for stages the user has actually completed
    const completed = await sql`
      SELECT 1 FROM stage_completions WHERE user_id = ${uid} AND stage_id = ${stageId}
    `;
    if (completed.length === 0) {
      return res.status(400).json({ error: 'Stage not completed yet — cannot submit replay score' });
    }

    const rows = await sql`
      INSERT INTO scores (user_id, stage_id, score, created_at)
      VALUES (${uid}, ${stageId}, ${value}, NOW())
      RETURNING id
    `;
    return res.status(200).json({ success: true, scoreId: rows[0].id });
  } catch (err) {
    return res.status(500).json({ error: 'Database error', detail: err.message });
  }
}
