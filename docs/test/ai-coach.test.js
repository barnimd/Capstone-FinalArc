import test from 'node:test';
import assert from 'node:assert/strict';
import {
  buildFollowupMessages,
  buildRunContext,
  buildSystemPrompt,
  classifyCoachQuestion,
  cleanUserMessage,
  fallbackCoachResponse,
  fallbackQuestionResponse,
  isDuplicateCoachResponse,
  parseCoachResponse,
  questionAllowsRepeat,
  validateRunPayload,
} from '../lib/ai-coach.js';
import { requestCoachResponse } from '../lib/deepseek.js';
import { TOPIC_KNOWLEDGE_VERSION, getTopicKnowledge } from '../lib/topic-knowledge-catalog.js';

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

const topic5Run = validateRunPayload({
  ...validRun,
  decisions: [
    { eventId: 'wifi.mas_anto_response', choiceId: 'threaten_to_report', outcomeId: 'operator_left', scoreDelta: 5, elapsedSeconds: 10, facts: [] },
    { eventId: 'vpn.choice', choiceId: 'skip', outcomeId: 'unprotected_connection', scoreDelta: 0, elapsedSeconds: 20, facts: [] },
    { eventId: 'public_wifi.login', choiceId: 'without_vpn', outcomeId: 'credentials_intercepted', scoreDelta: 0, elapsedSeconds: 30, facts: [] },
  ],
}).value;

const topic5BestCoreRun = validateRunPayload({
  ...validRun,
  score: 95,
  decisions: [
    { eventId: 'wifi.clue.budi', choiceId: 'investigate', outcomeId: 'router_operator_located', scoreDelta: 0, elapsedSeconds: 10, facts: [] },
    { eventId: 'wifi.clue.karaoke', choiceId: 'inspect_device', outcomeId: 'rogue_access_point_confirmed', scoreDelta: 0, elapsedSeconds: 15, facts: [] },
    { eventId: 'wifi.mas_anto_response', choiceId: 'report_to_staff', outcomeId: 'device_secured', scoreDelta: 10, elapsedSeconds: 20, facts: [] },
    { eventId: 'wifi.selection', choiceId: 'cafebean_free', outcomeId: 'trusted_network', scoreDelta: 0, elapsedSeconds: 25, facts: [] },
    { eventId: 'vpn.choice', choiceId: 'enable', outcomeId: 'protected_connection', scoreDelta: 0, elapsedSeconds: 30, facts: [] },
    { eventId: 'public_wifi.login', choiceId: 'with_vpn', outcomeId: 'credentials_protected', scoreDelta: 0, elapsedSeconds: 35, facts: [] },
  ],
}).value;

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
  assert.deepEqual(
    parseCoachResponse(JSON.stringify({ answer: 'Fine.', evidence: [], nextAction: 'Act.', outOfScope: false, extra: true })),
    { answer: 'Fine.', evidence: [], nextAction: 'Act.', outOfScope: false }
  );
});

test('normalizes duration when decisions span a scene transition', () => {
  const result = validateRunPayload({
    ...validRun,
    durationSeconds: 188,
    decisions: [{
      eventId: 'wifi.clue.bu_dewi',
      choiceId: 'assist_victim',
      outcomeId: 'victim_warned',
      scoreDelta: 5,
      elapsedSeconds: 300,
      facts: [{ key: 'fake_network', value: 'cafebean_free_5g' }],
    }],
  });

  assert.equal(result.error, undefined);
  assert.equal(result.value.durationSeconds, 300);
  assert.equal(result.value.decisions[0].eventId, 'wifi.clue.bu_dewi');
});

test('follow-up messages keep the latest question last', () => {
  const history = [
    { role: 'assistant', content: '{"answer":"opening"}' },
    { role: 'user', content: 'pertanyaan lama' },
  ];
  const messages = buildFollowupMessages(topic5Run, history, 'pilihan saya di Mas Anto bagaimana?');
  assert.match(messages[0].content, /Gameplay context JSON/);
  assert.deepEqual(messages.slice(1, 3), history);
  assert.match(messages.at(-1).content, /LATEST PLAYER QUESTION/);
  assert.match(messages.at(-1).content, /Mas Anto/);
});

test('contextual fallback answers Mas Anto instead of repeating the score debrief', () => {
  const response = fallbackQuestionResponse('wifi-security', topic5Run, 'pilihan saya di Mas Anto bagaimana?');
  assert.ok(response);
  assert.match(response.answer, /Matikan sekarang/);
  assert.doesNotMatch(response.answer, /80\/100|keputusan yang tercatat/);
  assert.match(response.nextAction, /panggil staff/i);
});

test('contextual fallback finds VPN and rejects an unrelated ambiguous question', () => {
  const vpn = fallbackQuestionResponse('wifi-security', topic5Run, 'bagaimana pilihan VPN saya?');
  assert.ok(vpn);
  assert.match(vpn.answer, /Lewati VPN/);
  assert.match(vpn.nextAction, /Aktifkan VPN/);
  assert.equal(fallbackQuestionResponse('wifi-security', topic5Run, 'menurut kamu bagaimana?'), null);
});

test('duplicate responses are rejected unless the player asks to repeat', () => {
  const first = fallbackQuestionResponse('wifi-security', topic5Run, 'bagaimana pilihan VPN saya?');
  assert.equal(isDuplicateCoachResponse(first, [first]), true);
  assert.equal(fallbackQuestionResponse('wifi-security', topic5Run, 'bagaimana pilihan VPN saya?', [first]), null);
  assert.ok(fallbackQuestionResponse('wifi-security', topic5Run, 'ulangi pilihan VPN saya', [first], true));
  assert.equal(questionAllowsRepeat('ulangi penjelasannya', 'bagaimana pilihan VPN saya?'), true);
  assert.equal(questionAllowsRepeat('gimana pilihan saya di VPN?', 'bagaimana pilihan VPN saya?'), true);
  assert.equal(questionAllowsRepeat('bagaimana pilihan Mas Anto?', 'bagaimana pilihan VPN saya?'), false);
});

test('DeepSeek retry receives a corrective prompt after a duplicate response', async () => {
  const previous = {
    answer: 'Jawaban lama tentang VPN.',
    evidence: ['VPN dilewati.'],
    nextAction: 'Aktifkan VPN.',
    outOfScope: false,
  };
  const fresh = {
    answer: 'Pilihan Mas Anto memerlukan laporan kepada staf.',
    evidence: ['Perangkat rogue access point ditemukan.'],
    nextAction: 'Laporkan perangkat kepada staf.',
    outOfScope: false,
  };
  const originalFetch = global.fetch;
  const originalKey = process.env.DEEPSEEK_API_KEY;
  const requests = [];
  let call = 0;
  process.env.DEEPSEEK_API_KEY = 'test-key';
  global.fetch = async (_url, options) => {
    requests.push(JSON.parse(options.body));
    const content = JSON.stringify(call++ === 0 ? previous : fresh);
    return { ok: true, json: async () => ({ choices: [{ finish_reason: 'stop', message: { content } }], usage: {} }) };
  };

  try {
    const result = await requestCoachResponse(
      'wifi-security',
      buildFollowupMessages(topic5Run, [], 'bagaimana pilihan Mas Anto?'),
      { previousResponses: [previous], allowRepeat: false }
    );
    assert.deepEqual(result.response, fresh);
    assert.equal(requests.length, 2);
    assert.equal(requests[0].max_tokens, 384);
    assert.match(requests[1].messages.at(-1).content, /REPAIR REQUIRED/);
    assert.match(requests[1].messages.at(-1).content, /duplicate_response/);
  } finally {
    global.fetch = originalFetch;
    if (originalKey == null) delete process.env.DEEPSEEK_API_KEY;
    else process.env.DEEPSEEK_API_KEY = originalKey;
  }
});

test('system prompt contains topic grounding and JSON rules', () => {
  const prompt = buildSystemPrompt('ransomware');
  assert.match(prompt, /Ransomware & Backup/);
  assert.match(prompt, /Return JSON only/);
  assert.match(prompt, /PROMPT VERSION: v4/);
  assert.match(prompt, new RegExp(TOPIC_KNOWLEDGE_VERSION));
});

test('system prompt includes only the selected topic knowledge', () => {
  const wifiPrompt = buildSystemPrompt('wifi-security');
  assert.match(wifiPrompt, /evil twin/i);
  assert.match(wifiPrompt, /HTTPS valid biasanya melindungi isi kredensial/i);
  assert.doesNotMatch(wifiPrompt, /credential stuffing/i);
  assert.doesNotMatch(wifiPrompt, /prinsip 3-2-1/i);

  const ransomwarePrompt = buildSystemPrompt('ransomware');
  assert.match(ransomwarePrompt, /prinsip 3-2-1/i);
  assert.doesNotMatch(ransomwarePrompt, /evil twin/i);
});

test('run context carries an independent topic knowledge version', () => {
  const validated = validateRunPayload(validRun).value;
  const context = JSON.parse(buildRunContext(validated));
  assert.equal(validated.knowledgeVersion, TOPIC_KNOWLEDGE_VERSION);
  assert.equal(context.knowledgeVersion, TOPIC_KNOWLEDGE_VERSION);
  assert.match(context.contextVersion, /^v2-/);
});

test('all six topics expose curated learning outcomes and concepts', () => {
  for (const stageId of ['phishing', '2fa', 'password-security', 'malware-awareness', 'wifi-security', 'ransomware']) {
    const knowledge = getTopicKnowledge(stageId);
    assert.ok(knowledge, stageId);
    assert.ok(knowledge.learningOutcomes.length >= 3, stageId);
    assert.ok(knowledge.concepts.length >= 4, stageId);
    for (const concept of knowledge.concepts) {
      assert.ok(concept.definition && concept.howItWorks && concept.gameConnection, `${stageId}:${concept.id}`);
      assert.ok(concept.realWorldNuance && concept.safeActions.length > 0, `${stageId}:${concept.id}`);
    }
  }
});

test('concept fallback answers material questions across all six topics', () => {
  const samples = [
    ['phishing', 'apa itu verifikasi independen?', /kanal independen|kontak atau sistem tepercaya/i],
    ['2fa', 'apa itu credential stuffing?', /kebocoran|banyak layanan/i],
    ['password-security', 'apa itu phishing email?', /menyamar|situs palsu/i],
    ['malware-awareness', 'apa itu scareware?', /pesan menakutkan|popup browser/i],
    ['wifi-security', 'apa itu evil twin?', /meniru nama|SSID/i],
    ['ransomware', 'kenapa perlu offline backup?', /salinan|terpisah|offline/i],
  ];

  for (const [stageId, question, expected] of samples) {
    const response = fallbackQuestionResponse(stageId, { decisions: [] }, question);
    assert.ok(response, stageId);
    assert.match(`${response.answer} ${response.evidence.join(' ')}`, expected, stageId);
    assert.doesNotMatch(response.answer, /skor|\d+\/100/i, stageId);
  }
});

test('topic 2 explains the modern password-rotation nuance', () => {
  const response = fallbackQuestionResponse('2fa', { decisions: [] }, 'apakah harus ganti password berkala?');
  assert.ok(response);
  assert.match(response.evidence.join(' '), /tidak menganjurkan pergantian arbitrer/i);
  assert.match(response.nextAction, /terindikasi bocor/i);
});

test('topic 3 avoids treating every linked email as phishing', () => {
  const response = fallbackQuestionResponse('password-security', { decisions: [] }, 'apa beda report delete dan reply?');
  assert.ok(response);
  assert.match(response.evidence.join(' '), /tidak otomatis phishing/i);
});

test('topic 4 states that HTTPS protects transport but not site legitimacy', () => {
  const response = fallbackQuestionResponse('malware-awareness', { decisions: [] }, 'kenapa https belum tentu aman?');
  assert.ok(response);
  assert.match(response.answer, /mengenkripsi|integritas koneksi/i);
  assert.match(response.evidence.join(' '), /domain tepat|pemilik situs/i);
});

test('topic 5 distinguishes the simulation from real public Wi-Fi risk', () => {
  const response = fallbackQuestionResponse('wifi-security', { decisions: [] }, 'bagaimana MITM di public wifi?');
  assert.ok(response);
  assert.match(response.evidence.join(' '), /HTTPS valid biasanya melindungi/i);
  assert.match(response.evidence.join(' '), /penyederhanaan risiko/i);
});

test('topic 5 prompt explains that Bu Dewi is an optional five-point bonus', () => {
  const prompt = buildSystemPrompt('wifi-security');
  assert.match(prompt, /bestMandatoryPathScore[^}]*95/i);
  assert.match(prompt, /optionalBonusScore[^}]*5/i);
  assert.match(prompt, /Bu Dewi/i);
  assert.match(prompt, /bukan objective wajib/i);
});

test('topic 5 run context marks the optional Bu Dewi bonus as missing or earned', () => {
  const withoutBonus = JSON.parse(buildRunContext(topic5BestCoreRun)).scoreContext;
  assert.equal(withoutBonus.bestMandatoryPathScore, 95);
  assert.equal(withoutBonus.optionalBonusAvailable, 5);
  assert.equal(withoutBonus.optionalBonusEarned, 0);
  assert.equal(withoutBonus.optionalEvents[0].recorded, false);

  const withBonusRun = validateRunPayload({
    ...validRun,
    score: 100,
    decisions: [
      ...topic5BestCoreRun.decisions,
      { eventId: 'wifi.clue.bu_dewi', choiceId: 'assist_victim', outcomeId: 'victim_warned', scoreDelta: 5, elapsedSeconds: 18, facts: [] },
    ],
  }).value;
  const withBonus = JSON.parse(buildRunContext(withBonusRun)).scoreContext;
  assert.equal(withBonus.optionalBonusEarned, 5);
  assert.equal(withBonus.optionalEvents[0].recorded, true);
});

test('score fallback explains a perfect mandatory Topic 5 run that ends at 95', () => {
  const response = fallbackQuestionResponse('wifi-security', topic5BestCoreRun, 'kenapa skor saya 95 padahal semua pilihan benar?');
  assert.ok(response);
  assert.match(response.answer, /seluruh objective wajib/i);
  assert.match(response.answer, /bonus eksplorasi opsional Bu Dewi/i);
  assert.doesNotMatch(response.answer, /pilihanmu salah|objective wajib gagal/i);
  assert.match(response.nextAction, /temui dan bantu Bu Dewi/i);
  assert.equal(classifyCoachQuestion('wifi-security', topic5BestCoreRun, 'kenapa skor saya 95?'), 'score');
});

test('topic 6 describes backup as recovery rather than prevention', () => {
  const response = fallbackQuestionResponse('ransomware', { decisions: [] }, 'bagaimana backup membantu recovery?');
  assert.ok(response);
  assert.match(response.evidence.join(' '), /tidak mencegah malware/i);
  assert.match(response.nextAction, /uji proses restore/i);
});

test('mixed questions connect a concept to the recorded player decision', () => {
  const response = fallbackQuestionResponse('wifi-security', topic5Run, 'kenapa pilihan VPN saya salah?');
  assert.ok(response);
  assert.match(response.answer, /VPN membuat tunnel/i);
  assert.match(response.answer, /Lewati VPN/i);
  assert.match(response.nextAction, /Aktifkan VPN/i);
  assert.equal(classifyCoachQuestion('wifi-security', topic5Run, 'kenapa pilihan VPN saya salah?'), 'mixed');
  assert.equal(classifyCoachQuestion('wifi-security', topic5Run, 'pilihan VPN saya bagaimana?'), 'decision');
});

test('lesson questions return topic learning material without a score recap', () => {
  const response = fallbackQuestionResponse('wifi-security', topic5Run, 'apa pelajaran yang bisa diambil dari topic ini?');
  assert.ok(response);
  assert.match(response.answer, /Pelajaran utama/i);
  assert.match(response.answer, /rogue AP|evil twin/i);
  assert.doesNotMatch(response.answer, /80\/100|skor/i);
  assert.equal(classifyCoachQuestion('wifi-security', topic5Run, 'apa pelajaran topic ini?'), 'lesson');
});

test('automatic fallback opening remains score-first and adds a topic lesson', () => {
  const opening = fallbackCoachResponse('wifi-security', topic5Run);
  assert.match(opening.answer, /^Kamu menyelesaikan .* skor 80\/100/i);
  assert.match(opening.answer, /Pelajaran utamanya/i);
});

test('structured concept output may contain up to 180 words and five sentences', () => {
  const answer = Array(150).fill('materi').join(' ');
  assert.ok(parseCoachResponse(JSON.stringify({ answer, evidence: [], nextAction: 'Terapkan langkah aman.', outOfScope: false })));
  const tooLong = Array(181).fill('materi').join(' ');
  assert.equal(parseCoachResponse(JSON.stringify({ answer: tooLong, evidence: [], nextAction: 'Bertindak.', outOfScope: false })), null);
  assert.equal(parseCoachResponse(JSON.stringify({ answer: 'Satu. Dua. Tiga. Empat. Lima. Enam.', evidence: [], nextAction: 'Bertindak.', outOfScope: false })), null);
});

test('limits user messages to 400 clean characters', () => {
  assert.equal(cleanUserMessage('  hello\nworld '), 'hello world');
  assert.equal(cleanUserMessage('x'.repeat(401)), null);
});
