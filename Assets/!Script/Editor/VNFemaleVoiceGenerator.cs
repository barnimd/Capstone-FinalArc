#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Editor-only offline generator for female Player VN voices.
/// Secrets are read from process environment variables and are never serialized.
/// </summary>
public static class VNFemaleVoiceGenerator
{
    private const string Endpoint = "https://api.minimax.io/v1/t2a_v2";
    private const string ApiKeyEnvironmentVariable = "MINIMAX_API_KEY";
    private const string VoiceIdEnvironmentVariable = "MINIMAX_VOICE_ID";
    private const string RecommendedIndonesianFemaleVoice = "Indonesian_ConfidentWoman";
    private const string PreviewText = "Halo, saya siap mempelajari keamanan siber hari ini.";
    private const string PreviewAssetPath = "Assets/!Script/Editor/VoicePreview/PlayerFemale-Preview.mp3";
    private const int MaxAttempts = 3;
    private const int StarterRequestIntervalMilliseconds = 6200;

    private static readonly string[] TopicFolders =
    {
        "Assets/!Script/Game Topic 1/VN",
        "Assets/!Script/Game Topic 2/VN",
        "Assets/!Script/Game Topic 3/VN",
        "Assets/!Script/Game Topic 4/VN",
        "Assets/!Script/Game Topic 5/VN",
        "Assets/!Script/Game Topic 6/VN",
    };

    [MenuItem("SecMind/VN Female Voice/1. Generate Indonesian Preview")]
    private static async void GeneratePreview()
    {
        if (!TryReadCredentials(out string key, out string voiceId))
            return;

        GenerationResult result = await GenerateClip(PreviewText, key, voiceId);
        if (!result.success)
        {
            Debug.LogError("[VN Female Voice] Preview failed: " + result.status);
            return;
        }

        WriteMp3AndImport(PreviewAssetPath, result.audioBytes);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AudioClip>(PreviewAssetPath);
        Debug.Log("[VN Female Voice] Preview ready. status=created trace=" + SafeTrace(result.traceId));
    }

    [MenuItem("SecMind/VN Female Voice/2. Generate Missing Player Clips")]
    private static async void GenerateMissing()
    {
        await GenerateAll(false);
    }

    [MenuItem("SecMind/VN Female Voice/3. Regenerate All Player Clips")]
    private static async void RegenerateAll()
    {
        if (!EditorUtility.DisplayDialog(
                "Regenerate female VN voices?",
                "All generated female Player MP3 files will be requested again. Existing male and NPC audio is untouched.",
                "Regenerate",
                "Cancel"))
            return;

        await GenerateAll(true);
    }

    [MenuItem("SecMind/VN Female Voice/4. Audit Gendered VN Assets")]
    public static void AuditAssets()
    {
        VNDialogueData[] dialogues = FindDialogues();
        int playerLines = 0;
        int tokenPlayerLines = 0;
        int missingPortraitPairs = 0;
        int missingFemaleClips = 0;
        int femaleClipsOnNpc = 0;
        int legacyNames = 0;
        int legacyIdentityText = 0;

        foreach (VNDialogueData dialogue in dialogues)
        {
            if (dialogue.femalePlayerPortrait == null || dialogue.femalePlayerTalkingPortrait == null)
                missingPortraitPairs++;
            if (!string.Equals(dialogue.playerName, VNPlayerPresentationResolver.DefaultPlayerName, StringComparison.Ordinal))
                legacyNames++;

            if (dialogue.lines == null) continue;
            foreach (VNLine line in dialogue.lines)
            {
                if (line == null) continue;
                if (!string.IsNullOrEmpty(line.text) &&
                    (line.text.Contains("Joji") || line.text.Contains("Raka")))
                    legacyIdentityText++;
                bool isPlayer = line.speaker == VNSpeaker.Player;
                if (isPlayer)
                {
                    playerLines++;
                    if (VNPlayerPresentationResolver.ContainsDynamicPlayerName(line.text))
                        tokenPlayerLines++;
                    else if (line.femalePlayerVoiceClip == null)
                        missingFemaleClips++;
                }
                else if (line.femalePlayerVoiceClip != null)
                {
                    femaleClipsOnNpc++;
                }
            }
        }

        string summary = "dialogues=" + dialogues.Length +
                         " playerLines=" + playerLines +
                         " tokenPlayerLines=" + tokenPlayerLines +
                         " missingPortraitPairs=" + missingPortraitPairs +
                         " missingFemaleClips=" + missingFemaleClips +
                         " femaleClipsOnNpc=" + femaleClipsOnNpc +
                         " nonNormalizedPlayerNames=" + legacyNames +
                         " legacyIdentityText=" + legacyIdentityText +
                         " transparentPortraits=" + PortraitsHaveTransparentCorners();

        if (dialogues.Length == 24 && playerLines == 69 && tokenPlayerLines == 1 &&
            missingPortraitPairs == 0 && missingFemaleClips == 0 &&
            femaleClipsOnNpc == 0 && legacyNames == 0 && legacyIdentityText == 0 &&
            PortraitsHaveTransparentCorners())
            Debug.Log("[VN Gender Audit] PASS " + summary);
        else
            Debug.LogWarning("[VN Gender Audit] INCOMPLETE " + summary);
    }

    [MenuItem("SecMind/VN Female Voice/5. Run Resolver Edit-Mode Checks")]
    public static void RunResolverEditModeChecks()
    {
        AssertEqual("Kamu", VNPlayerPresentationResolver.SanitizeDisplayName(string.Empty), "empty username fallback");
        AssertEqual("Ayu", VNPlayerPresentationResolver.SanitizeDisplayName("\nAyu\t"), "control character cleanup");
        AssertEqual("‹b›Ayu‹/b›", VNPlayerPresentationResolver.SanitizeDisplayName("<b>Ayu</b>"), "TMP markup neutralization");
        AssertEqual("Halo Bima, Raka tetap NPC.",
            VNPlayerPresentationResolver.ResolveLineText("Halo {PLAYER_NAME}, Raka tetap NPC.", "Bima"),
            "exact token replacement");

        AudioClip male = AudioClip.Create("male-test", 32, 1, 32000, false);
        AudioClip female = AudioClip.Create("female-test", 32, 1, 32000, false);
        Texture2D portraitTexture = new Texture2D(2, 2);
        Sprite malePortrait = Sprite.Create(portraitTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
        Sprite femalePortrait = Sprite.Create(portraitTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
        Sprite femaleTalking = Sprite.Create(portraitTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
        VNDialogueData portraitData = ScriptableObject.CreateInstance<VNDialogueData>();
        try
        {
            var playerLine = new VNLine
            {
                speaker = VNSpeaker.Player,
                text = "Baris biasa",
                voiceClip = male,
                femalePlayerVoiceClip = female,
            };
            AssertSame(male, VNPlayerPresentationResolver.ResolveVoiceClip(playerLine, PlayerGender.Male), "male player voice");
            AssertSame(female, VNPlayerPresentationResolver.ResolveVoiceClip(playerLine, PlayerGender.Female), "female player voice");
            playerLine.femalePlayerVoiceClip = null;
            AssertSame(null, VNPlayerPresentationResolver.ResolveVoiceClip(playerLine, PlayerGender.Female), "missing female voice remains silent");
            playerLine.text = "Nama saya {PLAYER_NAME}";
            AssertSame(null, VNPlayerPresentationResolver.ResolveVoiceClip(playerLine, PlayerGender.Male), "token line remains silent");

            var npcLine = new VNLine { speaker = VNSpeaker.NPC, text = "Halo", voiceClip = male, femalePlayerVoiceClip = female };
            AssertSame(male, VNPlayerPresentationResolver.ResolveVoiceClip(npcLine, PlayerGender.Female), "NPC voice is unchanged");

            portraitData.playerPortrait = malePortrait;
            portraitData.femalePlayerPortrait = femalePortrait;
            portraitData.femalePlayerTalkingPortrait = femaleTalking;
            AssertSame(malePortrait, portraitData.GetExpressionSprite(VNSpeaker.Player, VNExpression.Default, PlayerGender.Male), "male portrait");
            AssertSame(femalePortrait, portraitData.GetExpressionSprite(VNSpeaker.Player, VNExpression.Default, PlayerGender.Female), "female portrait");
            AssertSame(femaleTalking, portraitData.GetExpressionSprite(VNSpeaker.Player, VNExpression.Talking, PlayerGender.Female), "female talking portrait");
            AssertSame(femalePortrait, portraitData.GetExpressionSprite(VNSpeaker.Player, VNExpression.Thinking, PlayerGender.Female), "female mood fallback");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(portraitData);
            UnityEngine.Object.DestroyImmediate(malePortrait);
            UnityEngine.Object.DestroyImmediate(femalePortrait);
            UnityEngine.Object.DestroyImmediate(femaleTalking);
            UnityEngine.Object.DestroyImmediate(portraitTexture);
            UnityEngine.Object.DestroyImmediate(male);
            UnityEngine.Object.DestroyImmediate(female);
        }

        Debug.Log("[VN Gender EditMode] PASS username, token, male/female voice, NPC, and silent fallback checks.");
    }

    private static async Task GenerateAll(bool overwrite)
    {
        if (!TryReadCredentials(out string key, out string voiceId))
            return;

        VNDialogueData[] dialogues = FindDialogues();
        int generated = 0;
        int skipped = 0;
        int failed = 0;
        int total = CountEligiblePlayerLines(dialogues);
        int processed = 0;

        try
        {
            foreach (VNDialogueData dialogue in dialogues)
            {
                string dialoguePath = AssetDatabase.GetAssetPath(dialogue);
                int topic = ResolveTopicNumber(dialoguePath);
                if (topic == 0 || dialogue.lines == null) continue;

                for (int index = 0; index < dialogue.lines.Length; index++)
                {
                    VNLine line = dialogue.lines[index];
                    if (!IsEligiblePlayerLine(line)) continue;
                    processed++;

                    string clipPath = BuildClipPath(topic, dialogue.name, index + 1);
                    EditorUtility.DisplayProgressBar(
                        "Generating female VN voices",
                        dialogue.name + " line " + (index + 1),
                        total == 0 ? 1f : (float)processed / total);

                    if (!overwrite && IsValidExistingClip(clipPath))
                    {
                        AssignClip(dialogue, line, clipPath);
                        skipped++;
                        Debug.Log("[VN Female Voice] asset=" + dialogue.name + " line=" + (index + 1) + " status=skipped-existing");
                        continue;
                    }

                    GenerationResult result = await GenerateClip(line.text, key, voiceId);
                    if (!result.success)
                    {
                        failed++;
                        Debug.LogWarning("[VN Female Voice] asset=" + dialogue.name + " line=" + (index + 1) +
                                         " status=" + result.status + " trace=" + SafeTrace(result.traceId));
                        continue;
                    }

                    WriteMp3AndImport(clipPath, result.audioBytes);
                    AssignClip(dialogue, line, clipPath);
                    generated++;
                    Debug.Log("[VN Female Voice] asset=" + dialogue.name + " line=" + (index + 1) +
                              " status=generated trace=" + SafeTrace(result.traceId));

                    // Starter Audio Subscription is limited to 10 requests/minute.
                    // Keep a small buffer above six seconds to avoid HTTP 429 at the window boundary.
                    await Task.Delay(StarterRequestIntervalMilliseconds);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log("[VN Female Voice] Bulk complete. generated=" + generated +
                  " skipped=" + skipped + " failed=" + failed + ".");
        AuditAssets();
    }

    private static async Task<GenerationResult> GenerateClip(string text, string apiKey, string voiceId)
    {
        var payload = new MiniMaxRequest
        {
            model = "speech-2.8-hd",
            text = text,
            stream = false,
            language_boost = "Indonesian",
            output_format = "hex",
            voice_setting = new VoiceSetting
            {
                voice_id = voiceId,
                speed = 1f,
                vol = 1f,
                pitch = 0,
            },
            audio_setting = new AudioSetting
            {
                sample_rate = 32000,
                bitrate = 128000,
                format = "mp3",
                channel = 1,
            },
        };

        byte[] requestBody = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        string lastStatus = "unknown-error";
        string lastTrace = string.Empty;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using (var request = new UnityWebRequest(Endpoint, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(requestBody);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 30;

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Delay(50);

                long responseCode = request.responseCode;
                bool retryable = request.result == UnityWebRequest.Result.ConnectionError ||
                                 responseCode == 408 || responseCode == 429 || responseCode >= 500;

                if (request.result == UnityWebRequest.Result.Success && responseCode >= 200 && responseCode < 300)
                {
                    MiniMaxResponse response;
                    try
                    {
                        response = JsonUtility.FromJson<MiniMaxResponse>(request.downloadHandler.text);
                    }
                    catch (Exception)
                    {
                        response = null;
                    }

                    lastTrace = response != null ? response.trace_id : string.Empty;
                    if (response != null && response.base_resp != null && response.base_resp.status_code != 0)
                        return GenerationResult.Fail("provider-rejected", lastTrace);
                    if (response == null || response.data == null || string.IsNullOrEmpty(response.data.audio))
                        return GenerationResult.Fail("invalid-response", lastTrace);

                    try
                    {
                        byte[] audio = DecodeHex(response.data.audio);
                        if (!LooksLikeMp3(audio))
                            return GenerationResult.Fail("invalid-mp3", lastTrace);
                        return GenerationResult.Ok(audio, lastTrace);
                    }
                    catch (Exception)
                    {
                        return GenerationResult.Fail("invalid-audio-hex", lastTrace);
                    }
                }

                lastStatus = responseCode > 0 ? "http-" + responseCode : "timeout-or-network";
                if (!retryable || attempt == MaxAttempts)
                    break;
            }

            await Task.Delay(500 * attempt);
        }

        return GenerationResult.Fail(lastStatus, lastTrace);
    }

    private static bool TryReadCredentials(out string key, out string voiceId)
    {
        key = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);
        voiceId = Environment.GetEnvironmentVariable(VoiceIdEnvironmentVariable);

        // Local developer fallback. The project .env is gitignored and its values
        // are never copied into assets, source, logs, or player builds.
        if (string.IsNullOrWhiteSpace(key))
            key = ReadDotEnvValue(ApiKeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(voiceId))
            voiceId = ReadDotEnvValue(VoiceIdEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(voiceId))
            voiceId = RecommendedIndonesianFemaleVoice;

        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(voiceId))
            return true;

        Debug.LogError("[VN Female Voice] Set MINIMAX_API_KEY in the Unity Editor process environment or the gitignored project .env. " +
                       "MINIMAX_VOICE_ID is optional and defaults to an Indonesian female system voice.");
        return false;
    }

    private static string ReadDotEnvValue(string requestedKey)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot)) return null;

        string envPath = Path.Combine(projectRoot, ".env");
        if (!File.Exists(envPath)) return null;

        foreach (string rawLine in File.ReadAllLines(envPath))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
            int separator = line.IndexOf('=');
            if (separator <= 0) continue;
            if (!string.Equals(line.Substring(0, separator).Trim(), requestedKey, StringComparison.Ordinal)) continue;

            string value = line.Substring(separator + 1).Trim();
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[value.Length - 1] == '"') ||
                 (value[0] == '\'' && value[value.Length - 1] == '\'')))
                value = value.Substring(1, value.Length - 2);
            return value;
        }

        return null;
    }

    private static VNDialogueData[] FindDialogues()
    {
        var results = new List<VNDialogueData>();
        string[] guids = AssetDatabase.FindAssets("t:VNDialogueData", TopicFolders);
        Array.Sort(guids, StringComparer.Ordinal);
        foreach (string guid in guids)
        {
            VNDialogueData dialogue = AssetDatabase.LoadAssetAtPath<VNDialogueData>(AssetDatabase.GUIDToAssetPath(guid));
            if (dialogue != null) results.Add(dialogue);
        }
        return results.ToArray();
    }

    private static int CountEligiblePlayerLines(VNDialogueData[] dialogues)
    {
        int count = 0;
        foreach (VNDialogueData dialogue in dialogues)
            if (dialogue.lines != null)
                foreach (VNLine line in dialogue.lines)
                    if (IsEligiblePlayerLine(line)) count++;
        return count;
    }

    private static bool IsEligiblePlayerLine(VNLine line)
    {
        return line != null &&
               line.speaker == VNSpeaker.Player &&
               !string.IsNullOrWhiteSpace(line.text) &&
               !VNPlayerPresentationResolver.ContainsDynamicPlayerName(line.text);
    }

    private static int ResolveTopicNumber(string assetPath)
    {
        for (int topic = 1; topic <= 6; topic++)
            if (assetPath.IndexOf("Game Topic " + topic + "/", StringComparison.OrdinalIgnoreCase) >= 0)
                return topic;
        return 0;
    }

    private static string BuildClipPath(int topic, string dialogueName, int oneBasedLineNumber)
    {
        string safeName = SanitizeFileName(dialogueName);
        return "Assets/!Script/Game Topic " + topic + "/VN/VoiceBank/PlayerFemale-Topic" + topic + "/" +
               safeName + "-" + oneBasedLineNumber.ToString("D2") + ".mp3";
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value;
    }

    private static bool IsValidExistingClip(string assetPath)
    {
        string absolute = Path.GetFullPath(assetPath);
        if (!File.Exists(absolute) || new FileInfo(absolute).Length < 128)
            return false;
        return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath) != null;
    }

    private static void WriteMp3AndImport(string assetPath, byte[] bytes)
    {
        string absolute = Path.GetFullPath(assetPath);
        string directory = Path.GetDirectoryName(absolute);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllBytes(absolute, bytes);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer == null) return;
        importer.forceToMono = true;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.7f;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }

    private static void AssignClip(VNDialogueData dialogue, VNLine line, string assetPath)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip == null) return;
        Undo.RecordObject(dialogue, "Assign female VN voice");
        line.femalePlayerVoiceClip = clip;
        EditorUtility.SetDirty(dialogue);
    }

    private static byte[] DecodeHex(string hex)
    {
        if (hex.Length % 2 != 0) throw new FormatException("Odd hex length.");
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    private static bool LooksLikeMp3(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 128) return false;
        bool id3 = bytes.Length >= 3 && bytes[0] == (byte)'I' && bytes[1] == (byte)'D' && bytes[2] == (byte)'3';
        bool frameSync = bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0;
        return id3 || frameSync;
    }

    private static string SafeTrace(string traceId)
    {
        return string.IsNullOrWhiteSpace(traceId) ? "none" : traceId;
    }

    private static bool PortraitsHaveTransparentCorners()
    {
        return HasTransparentCorners("Assets/Asset game/VN/Topic5VN/PlayerFemaleDefault.png") &&
               HasTransparentCorners("Assets/Asset game/VN/Topic5VN/PlayerFemaleTalking.png");
    }

    private static bool HasTransparentCorners(string assetPath)
    {
        string absolute = Path.GetFullPath(assetPath);
        if (!File.Exists(absolute)) return false;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!texture.LoadImage(File.ReadAllBytes(absolute), false)) return false;
            int maxX = texture.width - 1;
            int maxY = texture.height - 1;
            return texture.width == 1024 && texture.height == 1024 &&
                   texture.GetPixel(0, 0).a == 0f &&
                   texture.GetPixel(maxX, 0).a == 0f &&
                   texture.GetPixel(0, maxY).a == 0f &&
                   texture.GetPixel(maxX, maxY).a == 0f;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void AssertEqual(string expected, string actual, string check)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            throw new InvalidOperationException(check + " failed. expected='" + expected + "' actual='" + actual + "'");
    }

    private static void AssertSame(UnityEngine.Object expected, UnityEngine.Object actual, string check)
    {
        if (expected != actual)
            throw new InvalidOperationException(check + " failed.");
    }

    [Serializable]
    private class MiniMaxRequest
    {
        public string model;
        public string text;
        public bool stream;
        public string language_boost;
        public string output_format;
        public VoiceSetting voice_setting;
        public AudioSetting audio_setting;
    }

    [Serializable]
    private class VoiceSetting
    {
        public string voice_id;
        public float speed;
        public float vol;
        public int pitch;
    }

    [Serializable]
    private class AudioSetting
    {
        public int sample_rate;
        public int bitrate;
        public string format;
        public int channel;
    }

    [Serializable]
    private class MiniMaxResponse
    {
        public ResponseData data;
        public BaseResponse base_resp;
        public string trace_id;
    }

    [Serializable]
    private class ResponseData
    {
        public string audio;
    }

    [Serializable]
    private class BaseResponse
    {
        public int status_code;
    }

    private struct GenerationResult
    {
        public bool success;
        public byte[] audioBytes;
        public string status;
        public string traceId;

        public static GenerationResult Ok(byte[] bytes, string trace)
        {
            return new GenerationResult { success = true, audioBytes = bytes, status = "ok", traceId = trace };
        }

        public static GenerationResult Fail(string status, string trace)
        {
            return new GenerationResult { success = false, audioBytes = null, status = status, traceId = trace };
        }
    }
}
#endif
