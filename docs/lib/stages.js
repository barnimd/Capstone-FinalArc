// Stage configuration. stage_id must match what Unity sends and what's stored in Neon.
// Naming convention: lowercase + hyphen.
export const STAGES = {
  'phishing':           { name: 'Phishing Awareness',            maxScore: 100 },
  '2fa':                { name: 'Two-Factor Authentication',     maxScore: 100 },
  'password-security':  { name: 'Password Security',             maxScore: 100 },
  'malware-awareness':  { name: 'Malware Awareness',             maxScore: 100 },
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
