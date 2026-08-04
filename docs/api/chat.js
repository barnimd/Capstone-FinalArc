export default async function handler(req, res) {
  res.setHeader('Cache-Control', 'no-store');
  return res.status(410).json({ success: false, error: 'Legacy endpoint removed. Use /api/ai/session/start and /api/ai/chat.' });
}
