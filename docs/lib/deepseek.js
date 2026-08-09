import { buildSystemPrompt, isDuplicateCoachResponse, parseCoachResponse } from './ai-coach.js';

const DEEPSEEK_URL = 'https://api.deepseek.com/chat/completions';
const MODEL = 'deepseek-v4-flash';
const THINKING = 'enabled';
const EFFORT = 'high';

export async function requestCoachResponse(stageId, messages, options = {}) {
  const apiKey = process.env.DEEPSEEK_API_KEY;
  if (!apiKey) throw new Error('DEEPSEEK_API_KEY not configured');
  let lastError = null;
  let attemptMessages = messages;
  for (let attempt = 0; attempt < 2; attempt++) {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 20000);
    try {
      const upstream = await fetch(DEEPSEEK_URL, { method: 'POST', headers: { Authorization: `Bearer ${apiKey}`, 'Content-Type': 'application/json' }, signal: controller.signal, body: JSON.stringify({ model: MODEL, thinking: { type: THINKING }, reasoning_effort: EFFORT, messages: [{ role: 'system', content: buildSystemPrompt(stageId) }, ...attemptMessages], response_format: { type: 'json_object' }, max_tokens: 1024, temperature: 0.2 }) });
      const data = await upstream.json();
      if (!upstream.ok) throw coachError(`http_${upstream.status}`, data?.error?.message ?? `DeepSeek HTTP ${upstream.status}`);
      const finishReason = data?.choices?.[0]?.finish_reason;
      if (finishReason === 'length') throw coachError('length', 'DeepSeek response reached max_tokens');
      if (finishReason === 'content_filter' || finishReason === 'insufficient_system_resource') {
        throw coachError(finishReason, `DeepSeek stopped with ${finishReason}`);
      }
      const raw = data?.choices?.[0]?.message?.content ?? '';
      if (!raw.trim()) throw coachError('empty_json', 'DeepSeek returned empty JSON content');
      const parsed = parseCoachResponse(raw);
      if (!parsed) throw coachError('schema_invalid', 'DeepSeek returned invalid coach JSON');
      if (isDuplicateCoachResponse(parsed, options.previousResponses, options.allowRepeat)) {
        throw coachError('duplicate_response', 'DeepSeek repeated a previous answer');
      }
      return { response: parsed, usage: data.usage ?? null };
    } catch (error) {
      lastError = normalizeCoachError(error);
      if (attempt === 0) {
        attemptMessages = [
          ...messages,
          {
            role: 'user',
            content: `REPAIR REQUIRED: The previous draft was rejected because ${lastError.code}. Produce fresh JSON that directly answers the latest player question and does not repeat an earlier answer.`,
          },
        ];
      }
    } finally { clearTimeout(timeout); }
  }
  throw lastError ?? new Error('DeepSeek returned an invalid response');
}

function coachError(code, message) {
  const error = new Error(message);
  error.code = code;
  return error;
}

function normalizeCoachError(error) {
  if (error?.name === 'AbortError') return coachError('timeout', 'DeepSeek request timed out');
  if (error?.code) return error;
  return coachError('request_failed', error?.message ?? 'DeepSeek request failed');
}
