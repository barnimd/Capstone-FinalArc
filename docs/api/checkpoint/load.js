import { verifyToken } from '../../lib/auth.js';
import { sql } from '../../lib/db.js';
import { isValidStage } from '../../lib/stages.js';

export default async function handler(req, res) {
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
