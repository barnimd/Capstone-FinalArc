import { sql } from '../../lib/db.js';
import { getPosterObject } from '../../lib/r2.js';

// Public image proxy. Replaces the r2.dev public URL, which Indonesian ISPs
// DNS-hijack to the Internet Positif block page — see Docs/db/schema_posters.sql.
//
// The fetch from R2 happens here, on Vercel's servers, so it never crosses the
// player's ISP. Same origin as the game, so no CORS either.
export default async function handler(req, res) {
  if (req.method !== 'GET') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const id = Number.parseInt(req.query.id, 10);
  if (!Number.isInteger(id) || id <= 0) {
    return res.status(400).json({ error: 'Invalid id' });
  }

  let poster;
  try {
    const rows = await sql`
      SELECT object_key, mime_type
        FROM posters
       WHERE id = ${id} AND is_active = TRUE
    `;
    poster = rows[0];
  } catch (err) {
    console.error('[poster/image] lookup failed:', err.message);
    return res.status(500).json({ error: 'Database error' });
  }

  if (!poster) return res.status(404).json({ error: 'Poster not found' });

  let object;
  try {
    object = await getPosterObject(poster.object_key);
  } catch (err) {
    // Almost always a wrong R2 env var or a token missing Object Read.
    console.error('[poster/image] R2 fetch failed:', poster.object_key, err.message);
    return res.status(502).json({ error: 'Storage fetch failed' });
  }

  // Safe to cache forever: object keys are UUIDs and a poster id is never
  // reused, so one id always means one image. Deleting a poster removes the id
  // from /api/posters, so clients stop asking for it. After the first request
  // Vercel's CDN answers and this function does not run at all.
  res.setHeader('Content-Type', poster.mime_type || object.contentType);
  res.setHeader('Content-Length', object.body.length);
  res.setHeader('Cache-Control', 'public, max-age=31536000, immutable');
  res.setHeader('X-Content-Type-Options', 'nosniff');

  return res.status(200).send(object.body);
}
