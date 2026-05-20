import admin from 'firebase-admin';

if (!admin.apps.length) {
  try {
    admin.initializeApp({
      credential: admin.credential.cert({
        projectId: process.env.FIREBASE_PROJECT_ID,
        clientEmail: process.env.FIREBASE_CLIENT_EMAIL,
        privateKey: process.env.FIREBASE_PRIVATE_KEY?.replace(/\\n/g, '\n'),
      }),
    });
  } catch (err) {
    console.error('Firebase init error:', err);
  }
}

export default async function handler(req, res) {
  try {
    // Cek env vars
    const hasProjectId = !!process.env.FIREBASE_PROJECT_ID;
    const hasClientEmail = !!process.env.FIREBASE_CLIENT_EMAIL;
    const hasPrivateKey = !!process.env.FIREBASE_PRIVATE_KEY;
    
    // Cek Firebase Admin initialized
    const isInitialized = admin.apps.length > 0;
    
    res.status(200).json({
      success: true,
      env: {
        FIREBASE_PROJECT_ID: hasProjectId ? '✅ set' : '❌ missing',
        FIREBASE_CLIENT_EMAIL: hasClientEmail ? '✅ set' : '❌ missing',
        FIREBASE_PRIVATE_KEY: hasPrivateKey ? '✅ set' : '❌ missing',
      },
      firebaseAdmin: isInitialized ? '✅ initialized' : '❌ not initialized',
      projectId: process.env.FIREBASE_PROJECT_ID || null,
    });
  } catch (err) {
    res.status(500).json({ 
      success: false, 
      error: err.message,
      stack: err.stack
    });
  }
}