import {
  S3Client,
  PutObjectCommand,
  GetObjectCommand,
  DeleteObjectCommand,
} from '@aws-sdk/client-s3';
import { randomUUID } from 'crypto';

// Cloudflare R2 speaks the S3 API. region must be the literal 'auto'; the
// endpoint is built from the account id.
//
// The bucket is PRIVATE on purpose. Indonesian ISPs DNS-hijack the whole r2.dev
// domain to the Internet Positif block page, so a public R2 URL is unreachable
// for our players. Byte reads go through /api/poster/image instead, which calls
// getPosterObject() below from Vercel's servers — outside that block.
//
// Credentials come only from Vercel env vars. They must never reach Unity: a
// WebGL build is just files on a CDN and anyone can unpack it.
const client = new S3Client({
  region: 'auto',
  endpoint: `https://${process.env.R2_ACCOUNT_ID}.r2.cloudflarestorage.com`,
  credentials: {
    accessKeyId: process.env.R2_ACCESS_KEY_ID,
    secretAccessKey: process.env.R2_SECRET_ACCESS_KEY,
  },
});

const BUCKET = process.env.R2_BUCKET;

const ALLOWED = { 'image/jpeg': 'jpg', 'image/png': 'png', 'image/webp': 'webp' };

// Hard ceiling, not the expected size. Unity compresses to roughly 250 KB before
// upload; this only catches a client that skipped that step. Vercel rejects any
// request body over 4.5 MB before our code runs, and base64 inflates by 33%, so
// anything much above 3 MB would never reach us anyway.
const MAX_BYTE = 3 * 1024 * 1024;

/** Magic bytes, so a .png renamed to .jpg is caught instead of stored mislabeled. */
function sniff(buf) {
  if (buf.length >= 3 && buf[0] === 0xff && buf[1] === 0xd8 && buf[2] === 0xff) {
    return 'image/jpeg';
  }
  if (
    buf.length >= 8 &&
    buf[0] === 0x89 && buf[1] === 0x50 && buf[2] === 0x4e && buf[3] === 0x47 &&
    buf[4] === 0x0d && buf[5] === 0x0a && buf[6] === 0x1a && buf[7] === 0x0a
  ) {
    return 'image/png';
  }
  if (
    buf.length >= 12 &&
    buf.toString('ascii', 0, 4) === 'RIFF' &&
    buf.toString('ascii', 8, 12) === 'WEBP'
  ) {
    return 'image/webp';
  }
  return null;
}

/** Returns an error string, or null when the image is acceptable. */
export function validateImage(mimeType, buf) {
  if (!ALLOWED[mimeType]) return 'Format harus JPG, PNG, atau WebP';
  if (!buf?.length) return 'File kosong';
  if (buf.length > MAX_BYTE) {
    return `File terlalu besar (max ${MAX_BYTE / 1024 / 1024}MB)`;
  }

  const actual = sniff(buf);
  if (!actual) return 'Isi file bukan gambar JPG, PNG, atau WebP';
  if (actual !== mimeType) {
    return `Isi file sebenarnya ${actual}, bukan ${mimeType}`;
  }
  return null;
}

/**
 * Upload to R2 under a random key.
 *
 * The key is a UUID rather than the uploaded filename: names from a file picker
 * carry spaces, emoji, and non-ASCII that would break the object key. A random
 * key is also never reused, which is what lets /api/poster/image mark its
 * responses immutable.
 */
export async function uploadPoster(mimeType, buf) {
  const objectKey = `posters/${randomUUID()}.${ALLOWED[mimeType]}`;
  await client.send(
    new PutObjectCommand({
      Bucket: BUCKET,
      Key: objectKey,
      Body: buf,
      ContentType: mimeType,
    }),
  );
  return { objectKey };
}

/** Read an object back so /api/poster/image can stream it to the player. */
export async function getPosterObject(objectKey) {
  const out = await client.send(
    new GetObjectCommand({ Bucket: BUCKET, Key: objectKey }),
  );
  return {
    body: Buffer.from(await out.Body.transformToByteArray()),
    contentType: out.ContentType || 'application/octet-stream',
  };
}

export async function deletePosterObject(objectKey) {
  await client.send(new DeleteObjectCommand({ Bucket: BUCKET, Key: objectKey }));
}
