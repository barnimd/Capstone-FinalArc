import admin from 'firebase-admin';

let initError = null;

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
    initError = {
      message: err.message,
      code: err.code,
      stack: err.stack?.split('\n').slice(0, 3).join('\n')
    };
  }
}

export default async function handler(req, res) {
  const privateKey = process.env.FIREBASE_PRIVATE_KEY || '';
  const processedKey = privateKey.replace(/\\n/g, '\n');
  
  res.status(200).json({
    success: !initError,
    env: {
      FIREBASE_PROJECT_ID: !!process.env.FIREBASE_PROJECT_ID ? '✅ set' : '❌ missing',
      FIREBASE_CLIENT_EMAIL: !!process.env.FIREBASE_CLIENT_EMAIL ? '✅ set' : '❌ missing',
      FIREBASE_PRIVATE_KEY: !!process.env.FIREBASE_PRIVATE_KEY ? '✅ set' : '❌ missing',
    },
    firebaseAdmin: admin.apps.length > 0 ? '✅ initialized' : '❌ not initialized',
    projectId: process.env.FIREBASE_PROJECT_ID || null,
    debug: {
      privateKeyLength: privateKey.length,
      privateKeyStartsWith: privateKey.substring(0, 40),
      privateKeyEndsWith: privateKey.substring(privateKey.length - 30),
      hasLiteralBackslashN: privateKey.includes('\\n'),
      hasRealNewline: privateKey.includes('\n'),
      processedKeyStartsCorrectly: processedKey.startsWith('-----BEGIN PRIVATE KEY-----'),
    },
    initError: initError,
  });
}