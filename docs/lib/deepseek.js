import { buildSystemPrompt, parseCoachResponse } from './ai-coach.js';

const DEEPSEEK_URL = 'https://api.deepseek.com/chat/completions';
const MODEL = 'deepseek-v4-flash';

export async function requestCoachResponse(stageId, messages) {
  const apiKey = process.env.DEEPSEEK_API_KEY;
  if (!apiKey) throw new Error('DEEPSEEK_API_KEY not configured');
  let lastError = null;
  for (let attempt = 0; attempt < 2; attempt++) {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 20000);
    try {
      const upstream = await fetch(DEEPSEEK_URL, { method: 'POST', headers: { Authorization: `Bearer ${apiKey}`, 'Content-Type': 'application/json' }, signal: controller.signal, body: JSON.stringify({ model: MODEL, thinking: { type: 'disabled' }, messages: [{ role: 'system', content: buildSystemPrompt(stageId) }, ...messages], response_format: { type: 'json_object' }, max_tokens: 256, temperature: 0.2 }) });
      const data = await upstream.json();
      if (!upstream.ok) throw new Error(data?.error?.message ?? `DeepSeek HTTP ${upstream.status}`);
      if (data?.choices?.[0]?.finish_reason === 'length') continue;
      const parsed = parseCoachResponse(data?.choices?.[0]?.message?.content ?? '');
      if (parsed) return { response: parsed, usage: data.usage ?? null };
      lastError = new Error('DeepSeek returned invalid JSON');
    } catch (error) {
      lastError = error;
    } finally { clearTimeout(timeout); }
  }
  throw lastError ?? new Error('DeepSeek returned an invalid response');
}
