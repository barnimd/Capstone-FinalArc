import { sql } from '../../lib/db.js';

export default async function handler(req, res) {
  if (req.headers.authorization !== `Bearer ${process.env.CRON_SECRET}`) return res.status(401).json({ error: 'Unauthorized' });
  try {
    const deleted = await sql`DELETE FROM ai_sessions WHERE expires_at <= NOW() RETURNING id`;
    return res.status(200).json({ success: true, deleted: deleted.length });
  } catch (error) {
    console.error('[cron/cleanup-ai-sessions]', error);
    return res.status(500).json({ error: 'Cleanup failed' });
  }
}
