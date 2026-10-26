#nullable disable
using System;
using System.IO;
using System.Reflection;
using System.Text;
using GameNetcodeStuff;
using UnityEngine;

namespace RollingGiant.ShipAmbush;

internal static class ShipAmbushAudio {
    private const string BulbResourceName = "RollingGiant.ShipAmbush.ShipBulbShatter.wav";
    private const string ViolinResourceName = "RollingGiant.ShipAmbush.ShipScaryViolin.wav";

    public static AudioClip BulbShatterClip { get; private set; }
    public static AudioClip ScaryViolinClip { get; private set; }

    private static ShipAmbushAudioRunner _runner;
    private static bool _loaded;

    public static void Init() {
        if (_loaded) return;
        _loaded = true;

        try {
            BulbShatterClip = LoadWavFromEmbeddedResource(BulbResourceName, "ShipBulbShatter");
            ScaryViolinClip = LoadWavFromEmbeddedResource(ViolinResourceName, "ShipScaryViolin");
            Plugin.Log.LogInfo($"Loaded ship ambush audio clips (bulb={(BulbShatterClip != null ? $"{BulbShatterClip.length:F2}s" : "null")}, violin={(ScaryViolinClip != null ? $"{ScaryViolinClip.length:F2}s" : "null")}).");
        } catch (Exception ex) {
            Plugin.Log.LogError($"Failed to load embedded ship ambush audio: {ex}");
        }
    }

    private static ShipAmbushAudioRunner EnsureRunner() {
        Init();
        if (_runner != null) return _runner;

        var go = new GameObject("RollingGiant_ShipAmbushAudio");
        UnityEngine.Object.DontDestroyOnLoad(go);
        _runner = go.AddComponent<ShipAmbushAudioRunner>();
        return _runner;
    }

    public static bool IsLocalPlayerInShip(StartOfRound round) {
        if (round == null || GameNetworkManager.Instance == null) return false;
        var local = GameNetworkManager.Instance.localPlayerController;
        if (local == null || !local.IsSpawned || local.isPlayerDead) return false;
        if (ShipAmbushManager.IsHostGhostBody(local)) return false;

        if (local.isInHangarShipRoom) return true;
        if (round.shipInnerRoomBounds != null
            && round.shipInnerRoomBounds.bounds.Contains(local.transform.position + Vector3.up * 0.25f)) {
            return true;
        }

        return false;
    }

    public static void PlayBulbShatterForShipPlayer(StartOfRound round) {
        if (!ShipAmbushSettings.PlayBulbShatterOnBlackout.Value) return;
        if (!IsLocalPlayerInShip(round)) return;

        var runner = EnsureRunner();
        if (BulbShatterClip == null) return;

        var ceilingPos = round.middleOfShipNode != null
            ? round.middleOfShipNode.position + Vector3.up * 2.5f
            : GameNetworkManager.Instance.localPlayerController.transform.position + Vector3.up * 2.2f;

        runner.PlayBulbShatter(BulbShatterClip, ceilingPos, Mathf.Clamp01(ShipAmbushSettings.BulbShatterVolume.Value));

        if (HUDManager.Instance != null) {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
        }
    }

    public static void PlayScaryViolinForShipPlayer(StartOfRound round) {
        if (!ShipAmbushSettings.PlayScaryViolinWhenSeen.Value) return;
        if (!IsLocalPlayerInShip(round)) return;

        var runner = EnsureRunner();
        if (ScaryViolinClip == null) return;

        runner.PlayScaryViolin(ScaryViolinClip, Mathf.Clamp01(ShipAmbushSettings.ScaryViolinVolume.Value));
    }

    public static bool IsViolinPlaying() {
        return _runner != null && _runner.IsViolinPlaying;
    }

    public static void UpdateViolinProximity(float distance, bool isDead) {
        if (_runner == null) return;
        _runner.UpdateProximity(distance, isDead);
    }

    public static void CutOffScaryViolin() {
        if (_runner == null) return;
        _runner.CutOffViolin();
    }

    public static void FadeOutScaryViolin(float fadeSeconds = 1.0f) {
        if (_runner == null) return;
        _runner.BeginFadeOut(fadeSeconds);
    }

    private static AudioClip LoadWavFromEmbeddedResource(string resourceName, string clipName) {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null) {
            Plugin.Log.LogWarning($"Embedded audio resource not found: {resourceName}");
            return null;
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();
        if (bytes.Length < 44) return null;

        var riff = Encoding.ASCII.GetString(bytes, 0, 4);
        var wave = Encoding.ASCII.GetString(bytes, 8, 4);
        if (riff != "RIFF" || wave != "WAVE") {
            Plugin.Log.LogWarning($"Invalid WAV header in {resourceName}");
            return null;
        }

        ushort audioFormat = 0;
        ushort channels = 0;
        int sampleRate = 0;
        ushort bitsPerSample = 0;
        int dataOffset = -1;
        int dataSize = 0;

        int offset = 12;
        while (offset <= bytes.Length - 8) {
            var chunkId = Encoding.ASCII.GetString(bytes, offset, 4);
            var chunkSize = BitConverter.ToInt32(bytes, offset + 4);
            if (chunkSize < 0) break;

            var payloadOffset = offset + 8;
            if (chunkId == "fmt " && payloadOffset + 16 <= bytes.Length) {
                audioFormat = BitConverter.ToUInt16(bytes, payloadOffset);
                channels = BitConverter.ToUInt16(bytes, payloadOffset + 2);
                sampleRate = BitConverter.ToInt32(bytes, payloadOffset + 4);
                bitsPerSample = BitConverter.ToUInt16(bytes, payloadOffset + 14);
            } else if (chunkId == "data") {
                dataOffset = payloadOffset;
                dataSize = Math.Min(chunkSize, bytes.Length - payloadOffset);
                break;
            }

            offset = payloadOffset + chunkSize + (chunkSize & 1);
        }

        if (audioFormat != 1 || channels < 1 || sampleRate <= 0 || bitsPerSample != 16 || dataOffset < 0 || dataSize <= 0) {
            Plugin.Log.LogWarning($"Unsupported WAV format in {resourceName} (fmt={audioFormat}, ch={channels}, rate={sampleRate}, bits={bitsPerSample})");
            return null;
        }

        int totalSamples = dataSize / 2;
        int frameCount = totalSamples / channels;
        if (frameCount <= 0) return null;

        var floatData = new float[frameCount * channels];
        int sampleCountToRead = floatData.Length;
        for (int i = 0; i < sampleCountToRead; i++) {
            short pcm = BitConverter.ToInt16(bytes, dataOffset + i * 2);
            floatData[i] = pcm / 32768f;
        }

        var clip = AudioClip.Create(clipName, frameCount, channels, sampleRate, false);
        clip.SetData(floatData, 0);
        return clip;
    }

    private sealed class ShipAmbushAudioRunner : MonoBehaviour {
        private AudioSource _bulbSource;
        private AudioSource _violinSource;
        private float _baseViolinVolume = 0.95f;
        private float _targetVolume = 0.20f;
        private bool _fadingViolin;
        private float _fadeSpeed = 1f;
        private float _localDeadTimer;

        public bool IsViolinPlaying => _violinSource != null && _violinSource.isPlaying;

        private void Awake() {
            var bulbObj = new GameObject("BulbShatterSource");
            bulbObj.transform.SetParent(transform, false);
            _bulbSource = bulbObj.AddComponent<AudioSource>();
            _bulbSource.playOnAwake = false;
            _bulbSource.loop = false;
            _bulbSource.spatialBlend = 0.25f;
            _bulbSource.dopplerLevel = 0f;
            _bulbSource.minDistance = 4f;
            _bulbSource.maxDistance = 22f;

            var violinObj = new GameObject("ScaryViolinSource");
            violinObj.transform.SetParent(transform, false);
            _violinSource = violinObj.AddComponent<AudioSource>();
            _violinSource.playOnAwake = false;
            _violinSource.loop = false;
            _violinSource.spatialBlend = 0f;
            _violinSource.dopplerLevel = 0f;
            _violinSource.priority = 16;
        }

        public void PlayBulbShatter(AudioClip clip, Vector3 worldPos, float volume) {
            _bulbSource.transform.position = worldPos;
            _bulbSource.volume = volume;
            _bulbSource.PlayOneShot(clip, volume);
        }

        public void PlayScaryViolin(AudioClip clip, float volume) {
            _violinSource.Stop();
            _violinSource.clip = clip;
            _baseViolinVolume = volume;
            // Start at 60% volume when seen at the doorway, will increase up to 100% as Giant approaches
            _violinSource.volume = volume * 0.60f;
            _targetVolume = _violinSource.volume;
            _fadingViolin = false;
            _localDeadTimer = 0f;
            _violinSource.Play();
        }

        public void UpdateProximity(float distance, bool isDead) {
            if (_violinSource == null || !_violinSource.isPlaying || _fadingViolin) return;

            if (isDead) {
                // Keep at maximum intensity during the kill until the monster despawns
                _targetVolume = _baseViolinVolume;
            } else {
                // Scaled from doorway (~8.5m) to melee distance (~1.2m), rising from 60% up to 100%
                float t = Mathf.InverseLerp(8.5f, 1.2f, distance);
                float factor = Mathf.Lerp(0.60f, 1.0f, t);
                _targetVolume = _baseViolinVolume * factor;
            }

            _violinSource.volume = Mathf.MoveTowards(_violinSource.volume, _targetVolume, Time.deltaTime * 2.5f);
        }

        public void CutOffViolin() {
            if (_violinSource == null) return;
            _violinSource.Stop();
            _fadingViolin = false;
            _violinSource.volume = 0f;
        }

        public void BeginFadeOut(float durationSeconds) {
            if (_violinSource == null || !_violinSource.isPlaying) return;
            _fadingViolin = true;
            _fadeSpeed = 1f / Mathf.Max(0.15f, durationSeconds);
        }

        private void Update() {
            if (_violinSource == null || !_violinSource.isPlaying) return;

            var round = StartOfRound.Instance;
            if (round == null || round.inShipPhase || round.shipIsLeaving) {
                CutOffViolin();
                return;
            }

            var local = GameNetworkManager.Instance != null ? GameNetworkManager.Instance.localPlayerController : null;
            if (local != null && local.isPlayerDead) {
                _localDeadTimer += Time.deltaTime;
                // Safety cutoff if for some reason the monster was never despawned after 20s
                if (_localDeadTimer >= 20f) {
                    CutOffViolin();
                    return;
                }
            }

            if (_fadingViolin) {
                _violinSource.volume = Mathf.MoveTowards(_violinSource.volume, 0f, _fadeSpeed * Time.deltaTime);
                if (_violinSource.volume <= 0.001f) {
                    _violinSource.Stop();
                    _fadingViolin = false;
                }
            }
        }
    }
}
