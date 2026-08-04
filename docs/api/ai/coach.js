import crypto from 'node:crypto';
import { verifyToken } from '../../lib/auth.js';
import { sql } from '../../lib/db.js';
import {
  buildFollowupMessages,
  buildRunContext,
  cleanUserMessage,
  fallbackCoachResponse,
  fallbackQuestionResponse,
  questionAllowsRepeat,
  validateRunPayload,
  MAX_QUESTIONS,
} from '../../lib/ai-coach.js';
import { requestCoachResponse } from '../../lib/deepseek.js';

/**
 * Single handler behind every /api/ai/* URL.
 *
 * Vercel counts one Serverless Function per file, and the Hobby plan caps a
 * deployment at 12. The public URLs are mapped onto this one file by the
 * `rewrites` block in vercel.json, which appends the `action` query param.
 * Bracket catch-all filenames ([...route].js) are a Next.js convention and do
 * NOT resolve on this project's plain filesystem API routing, so the rewrites
 * are what makes multi-segment paths like /api/ai/session/start work.
 */
export default async function handler(req, res) {
  res.setHeader('Cache-Control', 'no-store');

  switch (req.query?.action) {
    case 'session-start': return handleSessionStart(req, res);
    case 'chat':          return handleChat(req, res);
    case 'session':       return handleSession(req, res);
    case 'cleanup':       return handleCleanup(req, res);
    default:              return res.status(404).json({ success: false, error: 'Not found' });
  }
}

// ── POST /api/ai/session/start ──────────────────────────────────────────────

async function handleSessionStart(req, res) {
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
      return res.status(200).json({ success: true, sessionId: existing[0].id, messages, remainingQuestions: Math.max(0, MAX_QUESTIONS - existing[0].question_count) });
    }

    const sessionId = crypto.randomUUID();
    await sql`
      INSERT INTO ai_sessions
        (id, run_id, user_id, stage_id, score, max_score, duration_seconds, run_context, prompt_version, question_count, created_at, expires_at)
      VALUES
        (${sessionId}, ${run.runId}, ${uid}, ${run.stageId}, ${run.score}, ${run.maxScore}, ${run.durationSeconds}, ${JSON.stringify(run)}, 'v3', 0, NOW(), NOW() + INTERVAL '7 days')
    `;

    let opening;
    let usage = null;
    try {
      const result = await requestCoachResponse(run.stageId, [{ role: 'user', content: `Create the automatic post-game debrief. Gameplay context JSON: ${buildRunContext(run)}` }]);
      opening = result.response;
      usage = result.usage;
    } catch (error) {
      console.error('[ai/session/start] DeepSeek fallback:', error.code ?? 'request_failed');
      opening = fallbackCoachResponse(run.stageId, run);
    }

    await sql`INSERT INTO ai_messages (session_id, role, content_text, content_json, created_at) VALUES (${sessionId}, 'assistant', ${opening.answer}, ${JSON.stringify(opening)}, NOW())`;
    if (usage) await sql`UPDATE ai_sessions SET prompt_tokens = ${Number(usage.prompt_tokens ?? 0)}, completion_tokens = ${Number(usage.completion_tokens ?? 0)} WHERE id = ${sessionId}`;
    return res.status(200).json({ success: true, sessionId, opening, remainingQuestions: MAX_QUESTIONS });
  } catch (error) {
    console.error('[ai/session/start]', error);
    return res.status(500).json({ success: false, error: 'Failed to create AI session' });
  }
}

// ── POST /api/ai/chat ───────────────────────────────────────────────────────

async function handleChat(req, res) {
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
    const historyRows = await sql`SELECT role, content_text, content_json FROM ai_messages WHERE session_id = ${sessionId} ORDER BY id DESC LIMIT 7`;
    const chronologicalRows = [...historyRows].reverse();
    const history = chronologicalRows.map((row) => {
      const storedResponse = row.role === 'assistant' ? parseStoredCoachResponse(row) : null;
      return { role: row.role, content: storedResponse ? JSON.stringify(storedResponse) : row.content_text };
    });
    const previousResponses = chronologicalRows.filter((row) => row.role === 'assistant').map(parseStoredCoachResponse).filter(Boolean);
    const previousUserMessage = historyRows.find((row) => row.role === 'user')?.content_text ?? '';
    const allowRepeat = questionAllowsRepeat(message, previousUserMessage);
    const run = typeof session.run_context === 'string' ? JSON.parse(session.run_context) : session.run_context;

    let response;
    let usage = null;
    try {
      const result = await requestCoachResponse(
        session.stage_id,
        buildFollowupMessages(run, history, message),
        { previousResponses, allowRepeat }
      );
      response = result.response;
      usage = result.usage;
    } catch (error) {
      console.error('[ai/chat] DeepSeek fallback:', error.code ?? 'request_failed');
      response = fallbackQuestionResponse(session.stage_id, run, message, previousResponses, allowRepeat);
      if (!response) {
        const rolledBack = await sql`
          UPDATE ai_sessions
          SET question_count = GREATEST(question_count - 1, 0)
          WHERE id = ${sessionId} AND user_id = ${uid}
          RETURNING question_count
        `;
        const restoredCount = Number(rolledBack[0]?.question_count ?? Math.max(0, Number(reserved[0].question_count) - 1));
        console.error('[ai/chat] No confident local fallback:', error.code ?? 'request_failed');
        return res.status(200).json({
          success: false,
          error: 'AI coach belum dapat menjawab. Coba kirim lagi—kuota tidak berkurang.',
          retryable: true,
          remainingQuestions: Math.max(0, MAX_QUESTIONS - restoredCount),
        });
      }
      console.warn('[ai/chat] Served contextual local fallback:', error.code ?? 'request_failed');
    }

    await sql`INSERT INTO ai_messages (session_id, role, content_text, created_at) VALUES (${sessionId}, 'user', ${message}, NOW())`;
    await sql`INSERT INTO ai_messages (session_id, role, content_text, content_json, created_at) VALUES (${sessionId}, 'assistant', ${response.answer}, ${JSON.stringify(response)}, NOW())`;
    if (usage) await sql`UPDATE ai_sessions SET prompt_tokens = prompt_tokens + ${Number(usage.prompt_tokens ?? 0)}, completion_tokens = completion_tokens + ${Number(usage.completion_tokens ?? 0)} WHERE id = ${sessionId}`;
    const count = Number(reserved[0].question_count);
    return res.status(200).json({ success: true, response, retryable: false, remainingQuestions: Math.max(0, MAX_QUESTIONS - count) });
  } catch (error) {
    console.error('[ai/chat]', error);
    return res.status(500).json({ success: false, error: 'Failed to process AI message' });
  }
}

function parseStoredCoachResponse(row) {
  if (!row?.content_json) return null;
  if (typeof row.content_json === 'object') return row.content_json;
  try { return JSON.parse(row.content_json); } catch { return null; }
}

// ── GET /api/ai/session?runId=<uuid> ────────────────────────────────────────

async function handleSession(req, res) {
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

// ── GET /api/ai/cleanup (cron only) ─────────────────────────────────────────

async function handleCleanup(req, res) {
  if (req.headers.authorization !== `Bearer ${process.env.CRON_SECRET}`) return res.status(401).json({ error: 'Unauthorized' });
  try {
    const deleted = await sql`DELETE FROM ai_sessions WHERE expires_at <= NOW() RETURNING id`;
    return res.status(200).json({ success: true, deleted: deleted.length });
  } catch (error) {
    console.error('[ai/cleanup]', error);
    return res.status(500).json({ error: 'Cleanup failed' });
  }
}
