-- Gender pemain — menentukan sprite MC, voice over, dan portrait VN di seluruh game.
-- Run AFTER schema_core.sql (butuh tabel users).
--
--   1. schema_core.sql
--   2. schema_stage_locks.sql
--   3. schema_ai_coach.sql
--   4. schema_posters.sql
--   5. schema_gender.sql      <- this file
--
-- Dipilih sekali di scene CharacterSelect, lalu PERMANEN. Karakternya:
--   male   = BIMA  (Operative 01)
--   female = AYU   (Operative 02)

-- ── users.gender ────────────────────────────────────────────────────────────
-- NOT NULL DEFAULT 'male' — semua baris yang sudah ada langsung terisi 'male',
-- jadi tidak ada satu pun pemain lama yang tiba-tiba kehilangan karakternya.
ALTER TABLE users ADD COLUMN IF NOT EXISTS gender TEXT NOT NULL DEFAULT 'male';

-- CHECK dipasang terpisah supaya ALTER di atas tetap idempoten. DROP dulu agar
-- file ini aman dijalankan berulang kali.
ALTER TABLE users DROP CONSTRAINT IF EXISTS users_gender_check;
ALTER TABLE users ADD  CONSTRAINT users_gender_check CHECK (gender IN ('male', 'female'));

-- ── users.gender_chosen ─────────────────────────────────────────────────────
-- Kolom gender di atas TIDAK bisa membedakan "memilih male" dari "belum memilih",
-- karena dua-duanya bernilai 'male'. Tanpa flag ini, pemain lama tidak akan
-- pernah ditawari layar pilih karakter — mereka diam-diam terkunci sebagai BIMA.
--
-- FALSE = belum pernah memilih  → tampilkan scene CharacterSelect sekali
-- TRUE  = sudah memilih         → langsung ke All_Menu, dan server menolak
--                                 perubahan gender berikutnya (pilihan permanen)
ALTER TABLE users ADD COLUMN IF NOT EXISTS gender_chosen BOOLEAN NOT NULL DEFAULT FALSE;

-- ── verify ──────────────────────────────────────────────────────────────────
-- Expect 2 rows: gender (text, NOT NULL, default 'male') dan
--                gender_chosen (boolean, NOT NULL, default false).
-- SELECT column_name, data_type, is_nullable, column_default
--   FROM information_schema.columns
--  WHERE table_name = 'users' AND column_name IN ('gender', 'gender_chosen');

-- Cek sebaran setelah migrasi:
-- SELECT gender, gender_chosen, COUNT(*) FROM users GROUP BY 1, 2;
