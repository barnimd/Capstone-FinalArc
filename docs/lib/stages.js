// Stage configuration. stage_id must match what Unity sends and what's stored in Neon.
// Naming convention: lowercase + hyphen.
export const STAGES = {
  'phishing':           { name: 'Privasi Data & Social Engineering', maxScore: 1000 },
  '2fa':                { name: 'Keamanan Password & MFA',           maxScore: 1000 },
  'password-security':  { name: 'Phishing Email',                    maxScore: 1000 },
  'malware-awareness':  { name: 'Keamanan Browsing & Malware',       maxScore: 1000 },
  'wifi-security':      { name: 'Keamanan Wi-Fi Publik & Website',   maxScore: 1000 },
  'ransomware':         { name: 'Ransomware & Backup',               maxScore: 1000 },
};

export function isValidStage(stageId) {
  return typeof stageId === 'string' && Object.prototype.hasOwnProperty.call(STAGES, stageId);
}

export function getMaxScore(stageId) {
  return STAGES[stageId]?.maxScore ?? 0;
}

export function listStages() {
  return Object.entries(STAGES).map(([id, cfg]) => ({
    stageId: id,
    name: cfg.name,
    maxScore: cfg.maxScore,
  }));
}
