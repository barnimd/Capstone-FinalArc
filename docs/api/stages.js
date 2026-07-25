import { sql } from '../lib/db.js';
import { listStages } from '../lib/stages.js';

export default async function handler(req, res) {
  if (req.method !== 'GET') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  let locked = new Set();
  try {
    const rows = await sql`SELECT stage_id FROM stage_locks WHERE is_locked = TRUE`;
    locked = new Set(rows.map((r) => r.stage_id));
  } catch (err) {
    // Lock lookup failed — serve the stage list unlocked rather than breaking
    // the whole Class page.
    console.error('[stages] lock lookup failed:', err.message);
  }

  // Never cache: an admin lock has to show up the next time a player opens the menu.
  res.setHeader('Cache-Control', 'no-store');

  return res.status(200).json({
    success: true,
    stages: listStages().map((s) => ({ ...s, locked: locked.has(s.stageId) })),
  });
}
