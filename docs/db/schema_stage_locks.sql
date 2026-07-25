-- Admin-controlled lock state for the 6 class levels.
-- One row per stage, written only by Menu Admin > Manage Class.
-- No row = unlocked (the default shipped in the Unity LessonData asset).
CREATE TABLE IF NOT EXISTS stage_locks (
  stage_id   TEXT PRIMARY KEY,
  is_locked  BOOLEAN     NOT NULL DEFAULT FALSE,
  updated_by TEXT        REFERENCES users(user_id) ON DELETE SET NULL,
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Promote yourself to admin so the Menu Admin sidebar button appears.
-- Requires logging in once first (that creates the users row via /api/user/sync).
-- UPDATE users SET role = 'admin' WHERE email = 'harvezmine@gmail.com';
