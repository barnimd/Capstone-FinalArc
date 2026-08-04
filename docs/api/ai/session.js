import { verifyToken } from '../../lib/auth.js';
import { sql } from '../../lib/db.js';
import { MAX_QUESTIONS } from '../../lib/ai-coach.js';

export default async function handler(req, res) {
  res.setHeader('Cache-Control', 'no-store');
  if (req.method !== 'GET') return res.status(405).json({ success: false, error: 'Method not allowed' });
  const { uid, error: authError } = await verifyToken(req);
  if (authError) return res.status(401).json({ success: false, error: authError });
  const runId = typeof req.query?.runId === 'string' ? req.query.runId : '';
  if (!/^[a-f0-9-]{32,36}$/i.test(runId)) return res.status(400).json({ success: false, error: 'Invalid runId' });
  try {
    const sessions = await sql`SELECT id, question_count FROM ai_sessions WHERE run_id = ${runId} AND user_id = ${uid} AND expires_at > NOW() LIMIT 1`;
    if (sessions.length === 0) return res.status(404).json({ success: false, error: 'Session not found or expired' });
    const messages = await sql`SELECT role, content_text, content_json, created_at FROM ai_messages WHERE session_id = ${sessions[0].id} ORDER BY id ASC`;
    return res.status(200).json({ success: true, sessionId: sessions[0].id, messages, remainingQuestions: Math.max(0, MAX_QUESTIONS - sessions[0].question_count) });
  } catch (error) {
    console.error('[ai/session]', error);
    return res.status(500).json({ success: false, error: 'Failed to load AI session' });
  }
}
