import { sql } from '../lib/db.js';
import { getPosterObject } from '../lib/r2.js';

/**
 * Public poster endpoints. No auth — the dashboard slideshow has to work for
 * guests too.
 *
 * Two routes share this file because Vercel counts one Serverless Function per
 * file and the Hobby plan caps a deployment at 12:
 *   GET /api/posters               → list metadata      (no rewrite, action absent)
 *   GET /api/poster/image?id=N     → image bytes        (rewritten with action=image)
 */
export default async function handler(req, res) {
  if (req.method !== 'GET') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  if (req.query?.action === 'image') return handleImage(req, res);
  return handleList(req, res);
}

// ── GET /api/posters ────────────────────────────────────────────────────────

async function handleList(req, res) {
  try {
    const rows = await sql`
      SELECT id, title, sort_order
        FROM posters
       WHERE is_active = TRUE
       ORDER BY sort_order ASC, id ASC
    `;

    // Short cache: an admin's change should show up quickly, but opening the
    // menu should not hit the database every time.
    res.setHeader('Cache-Control', 'public, s-maxage=60, stale-while-revalidate=300');

    return res.status(200).json({
      success: true,
      posters: rows.map((r) => ({
        id: r.id,
        title: r.title,
        // Relative on purpose: same origin as the WebGL build, so there is no
        // cross-origin request and nothing for an ISP to block.
        imageUrl: `/api/poster/image?id=${r.id}`,
        sortOrder: r.sort_order,
      })),
    });
  } catch (err) {
    console.error('[posters] list failed:', err.message);
    return res.status(500).json({ error: 'Database error' });
  }
}

// ── GET /api/poster/image?id=N ──────────────────────────────────────────────

// Replaces the r2.dev public URL, which Indonesian ISPs DNS-hijack to the
// Internet Positif block page — see Docs/db/schema_posters.sql. The fetch from
// R2 happens here, on Vercel's servers, so it never crosses the player's ISP.
// Same origin as the game, so no CORS either.
async function handleImage(req, res) {
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
