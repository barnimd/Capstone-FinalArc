import { verifyToken } from '../../lib/auth.js';
import { sql } from '../../lib/db.js';
import { isValidStage } from '../../lib/stages.js';

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const { uid, error } = await verifyToken(req);
  if (error) return res.status(401).json({ error });

  const { stageId } = req.body || {};
  if (!isValidStage(stageId)) return res.status(400).json({ error: 'Invalid stageId' });

  try {
    const result = await sql`
      DELETE FROM checkpoints WHERE user_id = ${uid} AND stage_id = ${stageId}
      RETURNING id
    `;
    return res.status(200).json({ success: true, checkpointDeleted: result.length > 0 });
  } catch (err) {
    return res.status(500).json({ error: 'Database error', detail: err.message });
  }
}
