import admin from 'firebase-admin';
import { sql } from './db.js';

// Initialize Firebase Admin sekali aja (per serverless function instance)
if (!admin.apps.length) {
  admin.initializeApp({
    credential: admin.credential.cert({
      projectId: process.env.FIREBASE_PROJECT_ID,
      clientEmail: process.env.FIREBASE_CLIENT_EMAIL,
      privateKey: process.env.FIREBASE_PRIVATE_KEY?.replace(/\\n/g, '\n'),
    }),
  });
}

// Re-exported so handlers that need the Admin SDK directly (mis. guest/login.js
// yang mint custom token) memakai instance yang sudah di-init di atas.
export { admin };

/**
 * Verify Firebase ID token from Authorization header.
 * Returns { uid, email, error } object.
 */
export async function verifyToken(req) {
  const authHeader = req.headers.authorization;
  
  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    return { uid: null, email: null, error: 'Missing or invalid Authorization header' };
  }
  
  const token = authHeader.split('Bearer ')[1];
  
  try {
    const decoded = await admin.auth().verifyIdToken(token);
    return { 
      uid: decoded.uid, 
      email: decoded.email || null,
      name: decoded.name || null,
      error: null 
    };
  } catch (err) {
    return { uid: null, email: null, error: 'Invalid or expired token' };
  }
}

/**
 * Verify the token AND confirm the caller is an admin (users.role = 'admin').
 * Role is never trusted from the client — it's read from Neon every time.
 * Returns { uid, email, name, error, status }.
 */
export async function requireAdmin(req) {
  const auth = await verifyToken(req);
  if (auth.error) return { ...auth, status: 401 };

  try {
    const rows = await sql`SELECT role FROM users WHERE user_id = ${auth.uid}`;
    const role = rows[0]?.role || 'user';
    if (role !== 'admin') {
      return { ...auth, error: 'Admin role required', status: 403 };
    }
    return { ...auth, error: null, status: 200 };
  } catch (err) {
    return { ...auth, error: 'Database error', status: 500 };
  }
}