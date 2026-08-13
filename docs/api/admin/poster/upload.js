import { requireAdmin } from '../../../lib/auth.js';
import { sql } from '../../../lib/db.js';
import { validateImage, uploadPoster, deletePosterObject } from '../../../lib/r2.js';

// Slideshow stops being a slideshow past a dozen slides, and every extra poster
// is another object nobody will ever scroll to.
const MAX_POSTERS = 12;

// Admin-only. Stores one poster: bytes to R2, metadata to Neon.
export default async function handler(req, res) {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  const auth = await requireAdmin(req);
  if (auth.error) return res.status(auth.status).json({ error: auth.error });

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
