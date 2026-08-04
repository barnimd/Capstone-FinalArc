import { verifyToken } from '../../lib/auth.js';
import { sql } from '../../lib/db.js';
import { buildRunContext, cleanUserMessage, fallbackCoachResponse, MAX_QUESTIONS } from '../../lib/ai-coach.js';
import { requestCoachResponse } from '../../lib/deepseek.js';

export default async function handler(req, res) {
  res.setHeader('Cache-Control', 'no-store');
  if (req.method !== 'POST') return res.status(405).json({ success: false, error: 'Method not allowed' });
  const { uid, error: authError } = await verifyToken(req);
  if (authError) return res.status(401).json({ success: false, error: authError });
  const sessionId = typeof req.body?.sessionId === 'string' ? req.body.sessionId : '';
  const message = cleanUserMessage(req.body?.message);
  if (!/^[a-f0-9-]{36}$/i.test(sessionId)) return res.status(400).json({ success: false, error: 'Invalid sessionId' });
  if (!message) return res.status(400).json({ success: false, error: 'Message must be 1-400 characters' });

  try {
    const sessions = await sql`SELECT id, stage_id, run_context, question_count FROM ai_sessions WHERE id = ${sessionId} AND user_id = ${uid} AND expires_at > NOW() LIMIT 1`;
    if (sessions.length === 0) return res.status(404).json({ success: false, error: 'Session not found or expired' });
    const session = sessions[0];
    if (session.question_count >= MAX_QUESTIONS) return res.status(429).json({ success: false, error: 'Question limit reached', remainingQuestions: 0 });

    const reserved = await sql`UPDATE ai_sessions SET question_count = question_count + 1 WHERE id = ${sessionId} AND user_id = ${uid} AND question_count < ${MAX_QUESTIONS} RETURNING question_count`;
    if (reserved.length === 0) return res.status(429).json({ success: false, error: 'Question limit reached', remainingQuestions: 0 });
    await sql`INSERT INTO ai_messages (session_id, role, content_text, created_at) VALUES (${sessionId}, 'user', ${message}, NOW())`;

    const historyRows = await sql`SELECT role, content_text, content_json FROM ai_messages WHERE session_id = ${sessionId} ORDER BY id DESC LIMIT 7`;
    const history = historyRows.reverse().map((row) => ({ role: row.role, content: row.role === 'assistant' && row.content_json ? JSON.stringify(row.content_json) : row.content_text }));
    const run = typeof session.run_context === 'string' ? JSON.parse(session.run_context) : session.run_context;

    let response;
    let usage = null;
    try {
      const result = await requestCoachResponse(session.stage_id, [{ role: 'user', content: `Gameplay context JSON: ${buildRunContext(run)}` }, ...history]);
      response = result.response;
      usage = result.usage;
    } catch (error) {
      console.error('[ai/chat] DeepSeek fallback:', error.message);
      response = fallbackCoachResponse(session.stage_id, run);
    }

    await sql`INSERT INTO ai_messages (session_id, role, content_text, content_json, created_at) VALUES (${sessionId}, 'assistant', ${response.answer}, ${JSON.stringify(response)}, NOW())`;
    if (usage) await sql`UPDATE ai_sessions SET prompt_tokens = prompt_tokens + ${Number(usage.prompt_tokens ?? 0)}, completion_tokens = completion_tokens + ${Number(usage.completion_tokens ?? 0)} WHERE id = ${sessionId}`;
    const count = Number(reserved[0].question_count);
    return res.status(200).json({ success: true, response, remainingQuestions: Math.max(0, MAX_QUESTIONS - count) });
  } catch (error) {
    console.error('[ai/chat]', error);
    return res.status(500).json({ success: false, error: 'Failed to process AI message' });
  }
}
