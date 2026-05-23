import { sql } from '../lib/db.js';
import { isValidStage } from '../lib/stages.js';

const DEFAULT_LIMIT = 10;
const MAX_LIMIT = 100;

export default async function handler(req, res) {
  if (req.method !== 'GET') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const stageId = req.query?.stageId;
  if (!isValidStage(stageId)) return res.status(400).json({ error: 'Invalid stageId' });

  let limit = parseInt(req.query?.limit, 10);
  if (!Number.isFinite(limit) || limit <= 0) limit = DEFAULT_LIMIT;
  if (limit > MAX_LIMIT) limit = MAX_LIMIT;

  try {
    // Best score per user for this stage (highest of completion final_score and any replay scores)
    const rows = await sql`
      SELECT
        u.user_id,
        COALESCE(u.display_name, 'Anonymous') AS display_name,
        GREATEST(
          COALESCE(sc.final_score, 0),
          COALESCE((SELECT MAX(score) FROM scores s WHERE s.user_id = u.user_id AND s.stage_id = ${stageId}), 0)
        ) AS best_score
      FROM users u
      LEFT JOIN stage_completions sc ON sc.user_id = u.user_id AND sc.stage_id = ${stageId}
      WHERE sc.user_id IS NOT NULL
         OR EXISTS (SELECT 1 FROM scores s WHERE s.user_id = u.user_id AND s.stage_id = ${stageId})
      ORDER BY best_score DESC, u.user_id ASC
      LIMIT ${limit}
    `;

    return res.status(200).json({
      success: true,
      stageId,
      leaderboard: rows.map((r, i) => ({
        rank: i + 1,
        userId: r.user_id,
        displayName: r.display_name,
        score: r.best_score,
      })),
    });
  } catch (err) {
    return res.status(500).json({ error: 'Database error', detail: err.message });
  }
}
