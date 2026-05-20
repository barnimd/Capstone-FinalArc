import { neon } from '@neondatabase/serverless';

const sql = neon(process.env.DATABASE_URL);

export default async function handler(req, res) {
  try {
    const result = await sql`SELECT NOW() as current_time, version() as pg_version`;
    res.status(200).json({
      success: true,
      time: result[0].current_time,
      pgVersion: result[0].pg_version,
      message: 'Neon connected!'
    });
  } catch (err) {
    res.status(500).json({
      success: false,
      error: err.message
    });
  }
}