import test from 'node:test';
import assert from 'node:assert/strict';
import { buildSystemPrompt, cleanUserMessage, parseCoachResponse, validateRunPayload } from '../lib/ai-coach.js';

const validRun = {
  runId: '123e4567-e89b-12d3-a456-426614174000',
  stageId: 'wifi-security',
  score: 80,
  maxScore: 100,
  durationSeconds: 90,
  decisions: [{ eventId: 'wifi.selection', choiceId: 'office_secure', elapsedSeconds: 12.3 }],
};

test('accepts a bounded six-topic run payload', () => {
  const result = validateRunPayload(validRun);
  assert.equal(result.error, undefined);
  assert.equal(result.value.stageId, 'wifi-security');
  assert.equal(result.value.decisions.length, 1);
});

test('rejects unknown stages and impossible scores', () => {
  assert.equal(validateRunPayload({ ...validRun, stageId: 'unknown' }).error, 'Invalid stageId');
  assert.equal(validateRunPayload({ ...validRun, score: 101 }).error, 'Invalid score');
});

test('rejects unsafe decision identifiers', () => {
  const result = validateRunPayload({ ...validRun, decisions: [{ eventId: '<script>', choiceId: 'yes', elapsedSeconds: 1 }] });
  assert.match(result.error, /Invalid decision/);
});

test('rejects a valid-looking event from another topic', () => {
  const result = validateRunPayload({ ...validRun, decisions: [{ eventId: 'backup.exists', choiceId: 'yes', elapsedSeconds: 1 }] });
  assert.match(result.error, /not allowed/);
});

test('validates and sanitizes structured DeepSeek output', () => {
  const parsed = parseCoachResponse(JSON.stringify({ answer: '<b>Good</b> choice.', evidence: ['VPN enabled'], nextAction: 'Keep VPN active.', outOfScope: false }));
  assert.deepEqual(parsed, { answer: 'Good choice.', evidence: ['VPN enabled'], nextAction: 'Keep VPN active.', outOfScope: false });
  assert.equal(parseCoachResponse('{bad json'), null);
  assert.equal(parseCoachResponse(JSON.stringify({ answer: 'Visit https://bad.test', evidence: [], nextAction: 'Open it.', outOfScope: false })), null);
  assert.equal(parseCoachResponse(JSON.stringify({ answer: 'Fine.', evidence: [], nextAction: 'Act.', outOfScope: false, extra: true })), null);
});

test('system prompt contains topic grounding and JSON rules', () => {
  const prompt = buildSystemPrompt('ransomware');
  assert.match(prompt, /Ransomware & Backup/);
  assert.match(prompt, /Return JSON only/);
});

test('limits user messages to 400 clean characters', () => {
  assert.equal(cleanUserMessage('  hello\nworld '), 'hello world');
  assert.equal(cleanUserMessage('x'.repeat(401)), null);
});
