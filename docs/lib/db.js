import { neon } from '@neondatabase/serverless';

// Shared Neon SQL client. Pooled connection used across all serverless functions.
// DATABASE_URL is injected by Vercel ↔ Neon integration.
export const sql = neon(process.env.DATABASE_URL);
