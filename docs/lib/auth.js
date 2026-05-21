import admin from 'firebase-admin';

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