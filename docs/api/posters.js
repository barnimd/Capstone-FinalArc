import { sql } from '../lib/db.js';

// Public: the dashboard slideshow has to work for guests too, so no auth.
// Returns metadata only — imageUrl points at the proxy, which serves the bytes.
export default async function handler(req, res) {
  if (req.method !== 'GET') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

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
