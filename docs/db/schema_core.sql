-- Core tables for SecMind. Run this FIRST, before the other two schema files.
--
--   1. schema_core.sql         <- this file (users, scores, checkpoints, stage_completions)
--   2. schema_stage_locks.sql  (stage_locks)
--   3. schema_ai_coach.sql     (ai_sessions, ai_messages)
--
-- stage_locks.updated_by and ai_sessions.user_id both reference users(user_id),
-- so those two files fail if this one has not run yet.
--
-- stage_id is plain TEXT on purpose: the list of stages lives in Docs/lib/stages.js
-- and is validated by isValidStage() before any query runs. There is no stages table.

-- ── users ───────────────────────────────────────────────────────────────────
-- user_id is the Firebase UID. role is server-owned: requireAdmin() in
-- Docs/lib/auth.js re-reads it from here on every admin request, so a client
-- can never claim to be an admin.
CREATE TABLE IF NOT EXISTS users (
  user_id      TEXT        PRIMARY KEY,
  display_name TEXT,
  email        TEXT,
  role         TEXT        NOT NULL DEFAULT 'user',
  created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  last_login   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ── stage_completions ───────────────────────────────────────────────────────
-- One row per user per stage. /api/stage/complete upserts with
-- GREATEST(existing, new), so a worse replay never lowers the score.
CREATE TABLE IF NOT EXISTS stage_completions (
  user_id      TEXT        NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
  stage_id     TEXT        NOT NULL,
  final_score  INTEGER     NOT NULL CHECK (final_score >= 0),
  completed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  PRIMARY KEY (user_id, stage_id)
);

CREATE INDEX IF NOT EXISTS idx_stage_completions_leaderboard
  ON stage_completions (stage_id, final_score DESC);

-- ── checkpoints ─────────────────────────────────────────────────────────────
-- Mid-stage progress, max 50 KB of JSON (enforced in Docs/api/checkpoint/save.js).
-- UNIQUE(user_id, stage_id) is what the ON CONFLICT in that handler targets.
-- Deleted by /api/stage/restart and by /api/stage/complete.
CREATE TABLE IF NOT EXISTS checkpoints (
  id              BIGSERIAL   PRIMARY KEY,
  user_id         TEXT        NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
  stage_id        TEXT        NOT NULL,
  checkpoint_data JSONB       NOT NULL,
  updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  UNIQUE (user_id, stage_id)
);

-- ── scores ──────────────────────────────────────────────────────────────────
-- Append-only history of replay scores, one row per submission.
-- Both leaderboards read it; /api/score/submit is the only writer.
CREATE TABLE IF NOT EXISTS scores (
  id         BIGSERIAL   PRIMARY KEY,
  user_id    TEXT        NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
  stage_id   TEXT        NOT NULL,
  score      INTEGER     NOT NULL CHECK (score >= 0),
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_scores_leaderboard
  ON scores (stage_id, score DESC);

CREATE INDEX IF NOT EXISTS idx_scores_user_stage
  ON scores (user_id, stage_id);

-- ── verify ──────────────────────────────────────────────────────────────────
-- Expect 4 rows.
-- SELECT table_name FROM information_schema.tables
--  WHERE table_schema = 'public'
--    AND table_name IN ('users', 'scores', 'checkpoints', 'stage_completions');
