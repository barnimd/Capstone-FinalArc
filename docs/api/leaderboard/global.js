import { sql } from '../../lib/db.js';
import { STAGES } from '../../lib/stages.js';

const DEFAULT_LIMIT = 10;
const MAX_LIMIT = 100;

/**
 * Global leaderboard: for each user, return their single best (stage, score) pair
 * across all stages — combining stage_completions and replay scores.
 *
 * Use this for an "overall ranking" UI where each row shows the player and
 * the topic they're most proficient at.
 */
export default async function handler(req, res) {
  if (req.method !== 'GET') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  let limit = parseInt(req.query?.limit, 10);
  if (!Number.isFinite(limit) || limit <= 0) limit = DEFAULT_LIMIT;
  if (limit > MAX_LIMIT) limit = MAX_LIMIT;

  try {
    const rows = await sql`
      WITH all_scores AS (
        SELECT user_id, stage_id, final_score AS score
          FROM stage_completions
        UNION ALL
        SELECT user_id, stage_id, score
          FROM scores
      ),
      best_per_user_stage AS (
        SELECT user_id, stage_id, MAX(score) AS score
          FROM all_scores
         GROUP BY user_id, stage_id
      ),
      best_per_user AS (
        SELECT DISTINCT ON (user_id)
               user_id,
               stage_id AS best_stage,
               score    AS best_score
          FROM best_per_user_stage
         ORDER BY user_id, score DESC
      )
      SELECT u.user_id,
             COALESCE(u.display_name, 'Anonymous') AS display_name,
             b.best_stage,
             b.best_score
        FROM best_per_user b
        JOIN users u ON u.user_id = b.user_id
       ORDER BY b.best_score DESC, u.user_id ASC
       LIMIT ${limit}
    `;

    const leaderboard = rows.map((r, i) => ({
      rank:          i + 1,
      userId:        r.user_id,
      displayName:   r.display_name,
      bestStageId:   r.best_stage,
      bestStageName: STAGES[r.best_stage]?.name ?? r.best_stage,
      score:         r.best_score,
    }));

    return res.status(200).json({ success: true, leaderboard });
  } catch (err) {
    return res.status(500).json({ error: 'Database error', detail: err.message });
  }
}
