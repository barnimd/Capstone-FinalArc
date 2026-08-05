-- Guest login (callsign tanpa password). Run AFTER schema_core.sql.
--
--   1. schema_core.sql
--   2. schema_stage_locks.sql
--   3. schema_ai_coach.sql
--   4. schema_guest_login.sql  <- this file
--
-- Tidak ada tabel baru. Guest memakai baris users yang sama seperti user email,
-- bedanya user_id-nya deterministik: 'guest_' + callsign lowercase (mis. guest_ahmad).
-- Itu di-mint sebagai Firebase custom token oleh Docs/api/guest/login.js, jadi
-- callsign yang sama SELALU dapat UID yang sama -> progress ikut terbawa.
-- UID Firebase asli 28 karakter alfanumerik, tidak akan bentrok dengan prefix 'guest_'.

-- ── device_hash ─────────────────────────────────────────────────────────────
-- SHA-256 dari device id yang disimpan di PlayerPrefs (IndexedDB di WebGL).
-- Dipakai untuk aturan "1 callsign 1 hari": device yang sama boleh resume kapan
-- saja, device lain hanya boleh mengambil alih kalau last_login bukan hari ini.
-- NULL untuk akun email/password — mereka tidak lewat jalur ini.
ALTER TABLE users ADD COLUMN IF NOT EXISTS device_hash TEXT;

-- ── verify ──────────────────────────────────────────────────────────────────
-- Expect 1 row.
-- SELECT column_name, data_type FROM information_schema.columns
--  WHERE table_name = 'users' AND column_name = 'device_hash';
