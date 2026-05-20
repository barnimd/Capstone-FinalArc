import { neon } from '@neondatabase/serverless';

const sql = neon(process.env.DATABASE_URL);

export default async function handler(req, res) {
  try {
    const tables = await sql`
      SELECT table_name 
      FROM information_schema.tables 
      WHERE table_schema = 'public'
      ORDER BY table_name
    `;
    
    res.status(200).json({
      success: true,
      tableCount: tables.length,
      tables: tables.map(t => t.table_name)
    });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
}