import { requireAdmin } from '../../lib/auth.js';
import { sql } from '../../lib/db.js';
import { validateImage, uploadPoster, deletePosterObject } from '../../lib/r2.js';

// Slideshow stops being a slideshow past a dozen slides, and every extra poster
// is another object nobody will ever scroll to.
const MAX_POSTERS = 12;

/**
 * Single handler behind every /api/admin/poster/* URL.
 *
 * Same reason as api/ai/coach.js: Vercel counts one Serverless Function per file
 * and the Hobby plan caps a deployment at 12. The public URLs are mapped onto
 * this file by the `rewrites` block in vercel.json, which appends `action`.
 * Unity still calls /api/admin/poster/upload and friends, unchanged.
 *
 * Auth runs once here rather than in each branch — every action is admin-only.
 */
export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const auth = await requireAdmin(req);
  if (auth.error) return res.status(auth.status).json({ error: auth.error });

  switch (req.query?.action) {
    case 'upload':  return handleUpload(req, res, auth);
    case 'delete':  return handleDelete(req, res);
    case 'reorder': return handleReorder(req, res);
    default:        return res.status(404).json({ error: 'Not found' });
  }
}

// ── POST /api/admin/poster/upload ───────────────────────────────────────────

async function handleUpload(req, res, auth) {
  const { title, mimeType, imageBase64 } = req.body || {};
  if (typeof mimeType !== 'string' || typeof imageBase64 !== 'string' || !imageBase64) {
    return res.status(400).json({ error: 'mimeType dan imageBase64 wajib diisi' });
  }

  // The WebGL file picker hands back a FileReader data URL
  // ("data:image/jpeg;base64,..."). Unity strips the prefix, but strip it here
  // too — a stray prefix would otherwise be decoded as image data and fail the
  // magic-byte check with a confusing message.
  const payload = imageBase64.includes(',')
    ? imageBase64.slice(imageBase64.indexOf(',') + 1)
    : imageBase64;

  const buf = Buffer.from(payload, 'base64');
  const invalid = validateImage(mimeType, buf);
  if (invalid) return res.status(400).json({ error: invalid });

  try {
    const [{ count }] = await sql`
      SELECT COUNT(*)::int AS count FROM posters WHERE is_active = TRUE
    `;
    if (count >= MAX_POSTERS) {
      return res.status(409).json({ error: `Maksimal ${MAX_POSTERS} poster` });
    }
  } catch (err) {
    console.error('[poster/upload] count failed:', err.message);
    return res.status(500).json({ error: 'Database error' });
  }

  // R2 first. If this fails nothing has changed anywhere, so there is no
  // half-written state to undo.
  let objectKey;
  try {
    ({ objectKey } = await uploadPoster(mimeType, buf));
  } catch (err) {
    console.error('[poster/upload] R2 put failed:', err.message);
    return res.status(502).json({ error: 'Upload ke storage gagal' });
  }

  try {
    const [row] = await sql`
      INSERT INTO posters (title, object_key, mime_type, byte_size, sort_order, uploaded_by)
      VALUES (
        ${typeof title === 'string' ? title.slice(0, 200) : ''},
        ${objectKey},
        ${mimeType},
        ${buf.length},
        (SELECT COALESCE(MAX(sort_order), 0) + 1 FROM posters),
        ${auth.uid}
      )
      RETURNING id, title, sort_order
    `;
    return res.status(200).json({
      success: true,
      poster: {
        id: row.id,
        title: row.title,
        imageUrl: `/api/poster/image?id=${row.id}`,
        sortOrder: row.sort_order,
      },
    });
  } catch (err) {
    // The object is in the bucket but nothing points at it. Undo the upload so
    // it does not become an orphan nobody can find.
    console.error('[poster/upload] insert failed:', err.message);
    await deletePosterObject(objectKey).catch((cleanupErr) => {
      console.error('[poster/upload] rollback failed:', objectKey, cleanupErr.message);
    });
    return res.status(500).json({ error: 'Database error' });
  }
}

// ── POST /api/admin/poster/delete ───────────────────────────────────────────

async function handleDelete(req, res) {
  const id = Number.parseInt(req.body?.id, 10);
  if (!Number.isInteger(id) || id <= 0) {
    return res.status(400).json({ error: 'Invalid id' });
  }

  // DB row goes first, on purpose. If the order were reversed and this step
  // failed, a row would point at a file that no longer exists and players would
  // see a broken image. This way the worst case is an unreferenced file in the
  // bucket — invisible to players, and recorded below so it can be swept later.
  let objectKey;
  try {
    const rows = await sql`DELETE FROM posters WHERE id = ${id} RETURNING object_key`;
    if (!rows.length) return res.status(404).json({ error: 'Poster not found' });
    objectKey = rows[0].object_key;
  } catch (err) {
    console.error('[poster/delete] delete row failed:', err.message);
    return res.status(500).json({ error: 'Database error' });
  }

  try {
    await deletePosterObject(objectKey);
  } catch (err) {
    console.error('[poster/delete] R2 delete failed:', objectKey, err.message);
    await sql`
      INSERT INTO poster_orphans (object_key, last_error)
      VALUES (${objectKey}, ${err.message})
      ON CONFLICT (object_key) DO UPDATE
        SET failed_at = NOW(), last_error = EXCLUDED.last_error
    `.catch((logErr) => {
      console.error('[poster/delete] orphan log failed:', logErr.message);
    });
    // Deliberately not an error response: from the player's side the poster is
    // already gone, which is what the admin asked for.
  }

  // Close the gap the deleted row left, so sort_order stays 1..N.
  try {
    await sql`
      WITH ranked AS (
        SELECT id, ROW_NUMBER() OVER (ORDER BY sort_order, id) AS n
          FROM posters WHERE is_active = TRUE
      )
      UPDATE posters p SET sort_order = ranked.n
        FROM ranked WHERE p.id = ranked.id AND p.sort_order <> ranked.n
    `;
  } catch (err) {
    // Cosmetic only — gaps in sort_order do not change the display order.
    console.error('[poster/delete] resequence failed:', err.message);
  }

  return res.status(200).json({ success: true, id });
}

// ── POST /api/admin/poster/reorder ──────────────────────────────────────────

async function handleReorder(req, res) {
  const { ids } = req.body || {};
  if (!Array.isArray(ids) || !ids.length) {
    return res.status(400).json({ error: 'ids harus array yang tidak kosong' });
  }
  if (!ids.every((id) => Number.isInteger(id) && id > 0)) {
    return res.status(400).json({ error: 'ids harus berisi angka' });
  }
  if (new Set(ids).size !== ids.length) {
    return res.status(400).json({ error: 'ids tidak boleh duplikat' });
  }

  try {
    // The list must match the active posters exactly. A partial list would
    // leave the posters it omits holding stale positions, silently colliding
    // with the new ones.
    const rows = await sql`SELECT id FROM posters WHERE is_active = TRUE`;
    const active = new Set(rows.map((r) => r.id));
    if (active.size !== ids.length || !ids.every((id) => active.has(id))) {
      return res.status(400).json({ error: 'ids harus cocok persis dengan poster aktif' });
    }

    // WITH ORDINALITY turns array position into the new sort_order, so the whole
    // reorder lands in one statement instead of N racing updates.
    await sql`
      UPDATE posters p
         SET sort_order = v.ord
        FROM (
          SELECT * FROM UNNEST(${ids}::int[]) WITH ORDINALITY AS t(id, ord)
        ) AS v
       WHERE p.id = v.id
    `;

    return res.status(200).json({ success: true, ids });
  } catch (err) {
    console.error('[poster/reorder] failed:', err.message);
    return res.status(500).json({ error: 'Database error' });
  }
}
