import test from 'node:test';
import assert from 'node:assert/strict';
import { buildRunContext, buildSystemPrompt, cleanUserMessage, fallbackCoachResponse, parseCoachResponse, validateRunPayload } from '../lib/ai-coach.js';

const validRun = {
  runId: '123e4567-e89b-12d3-a456-426614174000',
  contentVersion: 'v2-2026-08-04',
  stageId: 'wifi-security',
  score: 80,
  maxScore: 100,
  durationSeconds: 90,
  decisions: [{
    eventId: 'wifi.selection',
    choiceId: 'cafebean_free',
    outcomeId: 'trusted_network',
    scoreDelta: 0,
    elapsedSeconds: 12.3,
    facts: [{ key: 'classification', value: 'legitimate' }],
  }],
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

test('rejects unsupported content versions', () => {
  assert.equal(validateRunPayload({ ...validRun, contentVersion: 'v999' }).error, 'Unsupported contentVersion');
});

test('rejects an unknown choice even when its prefix is allowed', () => {
  const result = validateRunPayload({ ...validRun, decisions: [{ eventId: 'wifi.selection', choiceId: 'invented_network', elapsedSeconds: 1 }] });
  assert.match(result.error, /Unknown event or choice/);
});

test('rejects a tampered score delta for versioned clients', () => {
  const result = validateRunPayload({
    ...validRun,
    decisions: [{ eventId: 'wifi.mas_anto_response', choiceId: 'report_to_staff', scoreDelta: 0, elapsedSeconds: 1 }],
  });
  assert.match(result.error, /Score delta does not match catalog/);
});

test('enriches IDs with scenario, selected choice, and alternatives', () => {
  const validated = validateRunPayload(validRun).value;
  const context = JSON.parse(buildRunContext(validated));
  assert.match(context.contextVersion, /^v2-/);
  assert.equal(context.decisions[0].selectedChoice.label, 'CafeBean_Free');
  assert.equal(context.decisions[0].availableChoices.length, 4);
  assert.equal(context.decisions[0].facts[0].value, 'legitimate');
});

test('keeps legacy WebGL decision IDs compatible during rollout', () => {
  const result = validateRunPayload({
    ...validRun,
    contentVersion: undefined,
    decisions: [{ eventId: 'wifi.selection', choiceId: 'office_secure', elapsedSeconds: 5 }],
  });
  assert.equal(result.error, undefined);
  const context = JSON.parse(buildRunContext(result.value));
  assert.equal(context.decisions[0].selectedChoice.id, 'cafebean_free');
});

test('evaluation context includes selected and correct answers', () => {
  const run = validateRunPayload({
    ...validRun,
    decisions: [{
      eventId: 'evaluation.question_2',
      choiceId: 'choice_a',
      outcomeId: 'incorrect',
      scoreDelta: 0,
      elapsedSeconds: 40,
      facts: [{ key: 'correct_choice', value: 'choice_b' }],
    }],
  }).value;
  const decision = JSON.parse(buildRunContext(run)).decisions[0];
  assert.equal(decision.selectedChoice.label, 'Ya');
  assert.equal(decision.correctChoiceId, 'choice_b');
  assert.match(decision.explanation, /Password jaringan/);
});

test('accepts semantic decisions from all six topics', () => {
  const samples = [
    ['phishing', { eventId: 'evidence.rizal', choiceId: 'refuse_and_verify', outcomeId: 'safe', scoreDelta: 0 }],
    ['2fa', { eventId: 'mfa.verification', choiceId: 'verified', outcomeId: 'otp_verified', scoreDelta: 0 }],
    ['password-security', { eventId: 'email.bri_account_closure', choiceId: 'laporkan', outcomeId: 'correct', scoreDelta: 0 }],
    ['malware-awareness', { eventId: 'popup.fake_virus_cleanup', choiceId: 'correct_action', outcomeId: 'correct', scoreDelta: 0 }],
    ['wifi-security', { eventId: 'wifi.mas_anto_response', choiceId: 'report_to_staff', outcomeId: 'device_secured', scoreDelta: 10 }],
    ['ransomware', { eventId: 'backup.configuration', choiceId: 'reliable', outcomeId: 'simulation_passed', scoreDelta: 20 }],
  ];

  for (const [stageId, decision] of samples) {
    const result = validateRunPayload({
      ...validRun,
      stageId,
      decisions: [{ ...decision, elapsedSeconds: 10, facts: [] }],
    });
    assert.equal(result.error, undefined, `${stageId}: ${result.error}`);
    assert.equal(JSON.parse(buildRunContext(result.value)).decisions.length, 1);
  }
});

test('fallback uses semantic labels instead of opaque IDs', () => {
  const run = validateRunPayload(validRun).value;
  const fallback = fallbackCoachResponse(run.stageId, run);
  assert.match(fallback.evidence[0], /CafeBean_Free/);
  assert.doesNotMatch(fallback.evidence[0], /cafebean_free/);
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
