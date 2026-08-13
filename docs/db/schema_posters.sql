-- Dashboard poster slideshow, managed from Menu Admin > Kelola Poster.
-- Run AFTER schema_core.sql — uploaded_by references users(user_id).
--
--   1. schema_core.sql
--   2. schema_stage_locks.sql
--   3. schema_ai_coach.sql
--   4. schema_posters.sql     <- this file
--
-- The image bytes live in Cloudflare R2, not here. This table is metadata plus
-- a pointer, so /api/posters stays a cheap query.
--
-- Note: the bucket is PRIVATE. Indonesian ISPs DNS-hijack the whole r2.dev
-- domain to the Internet Positif block page, so the public r2.dev URL is
-- unreachable for our players. Clients read images through
-- /api/poster/image?id=N instead, which fetches from R2 server-side over the
-- S3 API. That is why there is no public URL column here.

-- ── posters ─────────────────────────────────────────────────────────────────
-- object_key = path inside the R2 bucket; used by both the image proxy and
--              DeleteObject. This is the only pointer to the file.
-- sort_order = 1-based slide position; /api/admin/poster/reorder rewrites it.
CREATE TABLE IF NOT EXISTS posters (
  id          SERIAL      PRIMARY KEY,
  title       TEXT        NOT NULL DEFAULT '',
  object_key  TEXT        NOT NULL UNIQUE,
  mime_type   TEXT        NOT NULL,
  byte_size   INTEGER     NOT NULL CHECK (byte_size > 0),
  sort_order  INTEGER     NOT NULL,
  is_active   BOOLEAN     NOT NULL DEFAULT TRUE,
  uploaded_by TEXT        REFERENCES users(user_id) ON DELETE SET NULL,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Matches the ORDER BY in /api/posters.
CREATE INDEX IF NOT EXISTS idx_posters_order ON posters (is_active, sort_order);

-- ── poster_orphans ──────────────────────────────────────────────────────────
-- /api/admin/poster/delete removes the DB row first so players stop seeing the
-- poster immediately, then deletes the R2 object. If that second step fails the
-- key lands here instead of failing the request. Without this table the file
-- would sit in the bucket forever with nothing pointing at it.
CREATE TABLE IF NOT EXISTS poster_orphans (
  object_key TEXT        PRIMARY KEY,
  failed_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  last_error TEXT
);

-- ── verify ──────────────────────────────────────────────────────────────────
-- Expect 2 rows.
-- SELECT table_name FROM information_schema.tables
--  WHERE table_schema = 'public'
--    AND table_name IN ('posters', 'poster_orphans');
