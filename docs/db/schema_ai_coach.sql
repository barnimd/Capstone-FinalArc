CREATE TABLE IF NOT EXISTS ai_sessions (
  id UUID PRIMARY KEY,
  run_id UUID NOT NULL,
  user_id TEXT NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
  stage_id TEXT NOT NULL,
  score INTEGER NOT NULL CHECK (score >= 0),
  max_score INTEGER NOT NULL CHECK (max_score > 0),
  duration_seconds DOUBLE PRECISION NOT NULL CHECK (duration_seconds >= 0),
  run_context JSONB NOT NULL,
  prompt_version TEXT NOT NULL,
  question_count SMALLINT NOT NULL DEFAULT 0 CHECK (question_count BETWEEN 0 AND 3),
  prompt_tokens INTEGER NOT NULL DEFAULT 0,
  completion_tokens INTEGER NOT NULL DEFAULT 0,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  expires_at TIMESTAMPTZ NOT NULL,
  UNIQUE (user_id, run_id)
);
CREATE INDEX IF NOT EXISTS idx_ai_sessions_user_created ON ai_sessions(user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_ai_sessions_expiry ON ai_sessions(expires_at);

CREATE TABLE IF NOT EXISTS ai_messages (
  id BIGSERIAL PRIMARY KEY,
  session_id UUID NOT NULL REFERENCES ai_sessions(id) ON DELETE CASCADE,
  role TEXT NOT NULL CHECK (role IN ('user', 'assistant')),
  content_text TEXT NOT NULL,
  content_json JSONB,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_ai_messages_session_id ON ai_messages(session_id, id);
