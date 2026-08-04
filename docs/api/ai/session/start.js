import crypto from 'node:crypto';
import { verifyToken } from '../../../lib/auth.js';
import { sql } from '../../../lib/db.js';
import { buildRunContext, fallbackCoachResponse, validateRunPayload } from '../../../lib/ai-coach.js';
import { requestCoachResponse } from '../../../lib/deepseek.js';

export default async function handler(req, res) {
  res.setHeader('Cache-Control', 'no-store');
  if (req.method !== 'POST') return res.status(405).json({ success: false, error: 'Method not allowed' });
  const { uid, email, name, error: authError } = await verifyToken(req);
  if (authError) return res.status(401).json({ success: false, error: authError });
  const validated = validateRunPayload(req.body);
  if (validated.error) return res.status(400).json({ success: false, error: validated.error });
  const run = validated.value;

  try {
    // Ensure the FK parent exists even when the summary races the normal user-sync call.
    await sql`
      INSERT INTO users (user_id, display_name, email, created_at, last_login)
      VALUES (${uid}, ${name ?? null}, ${email ?? null}, NOW(), NOW())
      ON CONFLICT (user_id) DO UPDATE SET last_login = NOW()
    `;
    await sql`DELETE FROM ai_sessions WHERE expires_at <= NOW()`;
    const existing = await sql`SELECT id, question_count FROM ai_sessions WHERE user_id = ${uid} AND run_id = ${run.runId} AND expires_at > NOW() LIMIT 1`;
    if (existing.length > 0) {
      const messages = await sql`SELECT role, content_text, content_json, created_at FROM ai_messages WHERE session_id = ${existing[0].id} ORDER BY id ASC`;
      return res.status(200).json({ success: true, sessionId: existing[0].id, messages, remainingQuestions: Math.max(0, 3 - existing[0].question_count) });
    }

    const sessionId = crypto.randomUUID();
    await sql`
      INSERT INTO ai_sessions
        (id, run_id, user_id, stage_id, score, max_score, duration_seconds, run_context, prompt_version, question_count, created_at, expires_at)
      VALUES
        (${sessionId}, ${run.runId}, ${uid}, ${run.stageId}, ${run.score}, ${run.maxScore}, ${run.durationSeconds}, ${JSON.stringify(run)}, 'v1', 0, NOW(), NOW() + INTERVAL '7 days')
    `;

    let opening;
    let usage = null;
    try {
      const result = await requestCoachResponse(run.stageId, [{ role: 'user', content: `Create the automatic post-game debrief. Gameplay context JSON: ${buildRunContext(run)}` }]);
      opening = result.response;
      usage = result.usage;
    } catch (error) {
      console.error('[ai/session/start] DeepSeek fallback:', error.message);
      opening = fallbackCoachResponse(run.stageId, run);
    }

    await sql`INSERT INTO ai_messages (session_id, role, content_text, content_json, created_at) VALUES (${sessionId}, 'assistant', ${opening.answer}, ${JSON.stringify(opening)}, NOW())`;
    if (usage) await sql`UPDATE ai_sessions SET prompt_tokens = ${Number(usage.prompt_tokens ?? 0)}, completion_tokens = ${Number(usage.completion_tokens ?? 0)} WHERE id = ${sessionId}`;
    return res.status(200).json({ success: true, sessionId, opening, remainingQuestions: 3 });
  } catch (error) {
    console.error('[ai/session/start]', error);
    return res.status(500).json({ success: false, error: 'Failed to create AI session' });
  }
}
