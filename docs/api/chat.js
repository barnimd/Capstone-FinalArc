import { verifyToken } from '../lib/auth.js';

const DEEPSEEK_URL = 'https://api.deepseek.com/chat/completions';
const MODEL = 'deepseek-v4';

const SYSTEM_PROMPT =
  'You are CyberGuard, an AI cybersecurity assistant inside SecMind, an educational game. ' +
  'Your role is to help players understand phishing attacks, social engineering tactics, and online privacy (Privasi Keamanan). ' +
  'Keep every answer concise (2–3 sentences max), educational, and engaging. ' +
  'If a question is unrelated to cybersecurity, politely redirect to cybersecurity topics. ' +
  'Respond in the same language the player uses.';

export default async function handler(req, res) {
  if (req.method !== 'POST') return res.status(405).json({ error: 'Method not allowed' });

  const { uid, error: authError } = await verifyToken(req);
  if (authError) return res.status(401).json({ error: authError });

  const messages = req.body?.messages;
  if (!Array.isArray(messages) || messages.length === 0) {
    return res.status(400).json({ error: 'messages array required' });
  }

  const apiKey = process.env.DEEPSEEK_API_KEY;
  if (!apiKey) return res.status(500).json({ error: 'DEEPSEEK_API_KEY not configured on server' });

  const fullMessages = [{ role: 'system', content: SYSTEM_PROMPT }, ...messages];

  try {
    const upstream = await fetch(DEEPSEEK_URL, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${apiKey}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        model: MODEL,
        messages: fullMessages,
        max_tokens: 300,
        temperature: 0.7,
      }),
    });

    const data = await upstream.json();

    if (!upstream.ok) {
      console.error('[chat] DeepSeek error:', data);
      return res.status(upstream.status).json({ error: data.error?.message ?? 'DeepSeek API error' });
    }

    const reply = data.choices?.[0]?.message?.content ?? '';
    return res.status(200).json({ success: true, reply });
  } catch (err) {
    console.error('[chat] Upstream fetch failed:', err);
    return res.status(500).json({ error: 'Failed to reach DeepSeek API' });
  }
}
