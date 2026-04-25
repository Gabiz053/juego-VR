using System;
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Persistent singleton managing all audio in the game.
    /// Three layers: background music (cross-scene, shuffled, crossfaded),
    /// 2D SFX for UI elements, and 3D spatial SFX instantiated at world positions.
    /// Access via AudioManager.Instance — never use FindObjectOfType.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/AudioManager")]
    public sealed class AudioManager : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[AudioManager]";

        // Max simultaneous 3D sound instances to prevent runaway memory on rapid triggers.
        private const int MAX_CONCURRENT_SFX3D = 16;

        // Slight randomised pitch offset on every SFX for an organic, non-robotic feel.
        private const float PITCH_VARIATION = 0.08f;

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Master Volume")]
        [Tooltip("Overall volume multiplier applied to every audio source (0-1).")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;

        [Header("Music")]
        [Tooltip("Tracks played as shuffled background music. Assign MP3s from Audio/Music/.")]
        [SerializeField] private AudioClip[] _backgroundTracks;

        [Tooltip("Volume for background music (0-1).")]
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.4f;

        [Tooltip("Seconds to fade music in and out at the start and end of each track.")]
        [SerializeField, Range(0.5f, 5f)] private float _musicFadeDuration = 2f;

        [Tooltip("Minimum silence between tracks (seconds). Set both to 0 for continuous play.")]
        [SerializeField, Range(0f, 300f)] private float _minSilenceDuration = 0f;

        [Tooltip("Maximum silence between tracks (seconds). Minecraft default is ~180s.")]
        [SerializeField, Range(0f, 600f)] private float _maxSilenceDuration = 180f;

        [Tooltip("Start shuffled music playback automatically when the game loads.")]
        [SerializeField] private bool _playOnStart = true;

        [Header("UI Sounds (2D)")]
        [Tooltip("Button click / confirm sounds. Assign from Audio/SFX/UI/.")]
        [SerializeField] private AudioClip[] _uiClickSounds;

        [Tooltip("Button hover / pointer-enter sounds.")]
        [SerializeField] private AudioClip[] _uiHoverSounds;

        [Tooltip("Wrist menu open / panel appear sounds.")]
        [SerializeField] private AudioClip[] _uiMenuOpenSounds;

        [Header("Interaction Sounds (3D, spatial)")]
        [Tooltip("Object grab sounds. Assign pickup*.wav from Audio/SFX/Interaction/.")]
        [SerializeField] private AudioClip[] _grabSounds;

        [Tooltip("Object drop / release sounds.")]
        [SerializeField] private AudioClip[] _dropSounds;

        [Tooltip("Object collision / impact sounds. Assign hit*.wav from Audio/SFX/Interaction/.")]
        [SerializeField] private AudioClip[] _impactSounds;

        [Header("Hold Sound (3D, loops while object is held)")]
        [Tooltip("Looping sound while an object is grabbed. Call PlayHoldSound() on grab, StopHoldSound() on release.")]
        [SerializeField] private AudioClip[] _holdSounds;

        [Header("Portal Sounds (3D, spatial)")]
        [Tooltip("Sound when the player approaches a portal (proximity). Short ambient hum.")]
        [SerializeField] private AudioClip[] _portalProximitySounds;

        [Tooltip("Sound when the player teleports through a portal. One-shot whoosh.")]
        [SerializeField] private AudioClip[] _portalTeleportSounds;

        [Header("World Sounds (3D, spatial)")]
        [Tooltip("Planet or asteroid destruction sounds. Assign break.wav from Audio/SFX/Interaction/.")]
        [SerializeField] private AudioClip[] _explosionSounds;

        [Header("Player Sounds (2D)")]
        [Tooltip("Played when the player dies or falls out of bounds. Non-positional.")]
        [SerializeField] private AudioClip[] _playerDeathSounds;

        [Header("SFX Volume")]
        [Tooltip("Volume multiplier applied to all 2D and 3D SFX (0-1).")]
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

        #endregion

        #region Events ----------------------------------------------------------

        /// <summary>Raised when the music track changes. Passes the new clip name.</summary>
        public event Action<string> OnTrackChanged;

        /// <summary>Raised when master volume is changed at runtime.</summary>
        public event Action<float> OnMasterVolumeChanged;

        #endregion

        #region Cached Components -----------------------------------------------

        private static AudioManager _instance;

        // Two music sources allow seamless crossfading without a gap.
        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private AudioSource _sfx2dSource;

        private bool _isMusicSourceAActive = true;
        private int _activeSfx3dCount;
        private int[] _shuffledTrackOrder;
        private int _shufflePosition;
        private Coroutine _musicCoroutine;

        #endregion

        #region Public API ------------------------------------------------------

        /// <summary>Global access point. Non-null after Awake.</summary>
        public static AudioManager Instance => _instance;

        /// <summary>Master volume (0-1). Affects all audio output.</summary>
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                RefreshSourceVolumes();
                OnMasterVolumeChanged?.Invoke(_masterVolume);
                Debug.Log($"{LOG_TAG} Master volume set -- {_masterVolume:F2}.");
            }
        }

        /// <summary>Music volume (0-1).</summary>
        public float MusicVolume
        {
            get => _musicVolume;
            set { _musicVolume = Mathf.Clamp01(value); RefreshSourceVolumes(); }
        }

        /// <summary>SFX volume (0-1).</summary>
        public float SfxVolume
        {
            get => _sfxVolume;
            set { _sfxVolume = Mathf.Clamp01(value); RefreshSourceVolumes(); }
        }

        // ── Music ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Crossfades to <paramref name="clip"/> as the new background music.
        /// Set <paramref name="loop"/> to false for one-shot cinematic pieces.
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null)
            {
                Debug.LogWarning($"{LOG_TAG} _clip is not assigned.", this);
                return;
            }

            RestartMusicCoroutine(CrossfadeToClipRoutine(clip, loop));
        }

        /// <summary>
        /// Starts infinite shuffled playback of all assigned background tracks.
        /// Crossfades between each track automatically.
        /// </summary>
        public void PlayMusicShuffle()
        {
            if (_backgroundTracks == null || _backgroundTracks.Length == 0)
            {
                Debug.LogWarning($"{LOG_TAG} _backgroundTracks is not assigned.", this);
                return;
            }

            BuildShuffledOrder();
            RestartMusicCoroutine(MusicShuffleLoopRoutine());
        }

        /// <summary>Fades out and stops background music over <paramref name="fadeDuration"/> seconds.</summary>
        public void StopMusic(float fadeDuration = 1f)
        {
            RestartMusicCoroutine(FadeOutMusicRoutine(fadeDuration));
        }

        // ── 2D SFX ────────────────────────────────────────────────────────────

        /// <summary>Plays a 2D (non-positional) SFX with slight pitch variation for variety.</summary>
        public void PlaySFX2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            _sfx2dSource.pitch = 1f + UnityEngine.Random.Range(-PITCH_VARIATION, PITCH_VARIATION);
            _sfx2dSource.PlayOneShot(clip, volume * _sfxVolume * _masterVolume);
        }

        /// <summary>Picks a random clip from <paramref name="clips"/> and plays it as 2D SFX.</summary>
        public void PlaySFX2D(AudioClip[] clips, float volume = 1f)
        {
            var clip = PickRandom(clips);
            if (clip != null) PlaySFX2D(clip, volume);
        }

        // ── 3D SFX ────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a temporary AudioSource at <paramref name="position"/>, plays <paramref name="clip"/>,
        /// then destroys the object automatically. Full spatial audio — heard from its world position.
        /// </summary>
        public void PlaySFX3D(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;

            if (_activeSfx3dCount >= MAX_CONCURRENT_SFX3D)
            {
                Debug.Log($"{LOG_TAG} Max 3D SFX reached ({MAX_CONCURRENT_SFX3D}) -- skipping '{clip.name}'.");
                return;
            }

            _activeSfx3dCount++;
            StartCoroutine(Play3DRoutine(clip, position, volume));
        }

        /// <summary>Picks a random clip from <paramref name="clips"/> and plays it as 3D SFX.</summary>
        public void PlaySFX3D(AudioClip[] clips, Vector3 position, float volume = 1f)
        {
            var clip = PickRandom(clips);
            if (clip != null) PlaySFX3D(clip, position, volume);
        }

        // ── Convenience Methods ───────────────────────────────────────────────
        // Callers only need to say WHAT happened, not which clip to use.

        /// <summary>Plays a random UI button click sound (2D).</summary>
        public void PlayUIClick() => PlaySFX2D(_uiClickSounds);

        /// <summary>Plays a random UI pointer-hover sound (2D), at reduced volume.</summary>
        public void PlayUIHover() => PlaySFX2D(_uiHoverSounds, 0.6f);

        /// <summary>Plays a random wrist menu / panel open sound (2D).</summary>
        public void PlayUIMenuOpen() => PlaySFX2D(_uiMenuOpenSounds);

        /// <summary>Plays a random grab sound at <paramref name="position"/> (3D).</summary>
        public void PlayGrabSound(Vector3 position) => PlaySFX3D(_grabSounds, position);

        /// <summary>Plays a random drop/release sound at <paramref name="position"/> (3D).</summary>
        public void PlayDropSound(Vector3 position) => PlaySFX3D(_dropSounds, position);

        /// <summary>Plays a random impact/collision sound at <paramref name="position"/> (3D).</summary>
        public void PlayImpactSound(Vector3 position) => PlaySFX3D(_impactSounds, position);

        /// <summary>
        /// Starts a looping 3D sound at <paramref name="position"/> and returns the AudioSource.
        /// The caller must update <c>source.transform.position</c> each frame to follow the held object,
        /// and call <see cref="StopHoldSound"/> on release.
        /// </summary>
        public AudioSource PlayHoldSound(Vector3 position)
        {
            var clip = PickRandom(_holdSounds);
            if (clip == null) return null;

            var go = new GameObject("SFX3D_Hold");
            go.transform.position = position;

            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.spatialBlend = 1f;
            src.volume = _sfxVolume * _masterVolume * 0.6f;
            src.pitch = 1f + UnityEngine.Random.Range(-PITCH_VARIATION, PITCH_VARIATION);
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 0.3f;
            src.maxDistance = 5f;
            src.Play();

            return src;
        }

        /// <summary>
        /// Stops and destroys a looping hold AudioSource returned by <see cref="PlayHoldSound"/>.
        /// Safe to call with null.
        /// </summary>
        public void StopHoldSound(AudioSource source)
        {
            if (source == null) return;
            source.Stop();
            Destroy(source.gameObject);
        }

        /// <summary>Plays the portal proximity / ambient hum at <paramref name="position"/> (3D).</summary>
        public void PlayPortalProximitySound(Vector3 position) => PlaySFX3D(_portalProximitySounds, position);

        /// <summary>Plays the portal teleport whoosh at <paramref name="position"/> (3D).</summary>
        public void PlayPortalTeleportSound(Vector3 position) => PlaySFX3D(_portalTeleportSounds, position);

        /// <summary>Plays a random explosion sound at <paramref name="position"/> (3D).</summary>
        public void PlayExplosionSound(Vector3 position) => PlaySFX3D(_explosionSounds, position);

        /// <summary>Plays the player death sound (2D, non-positional).</summary>
        public void PlayPlayerDeathSound() => PlaySFX2D(_playerDeathSounds);

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Log($"{LOG_TAG} Duplicate detected -- destroying redundant instance.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            CreateAudioSources();

            Debug.Log($"{LOG_TAG} Initialized.");
        }

        private void Start()
        {
            ValidateReferences();

            if (_playOnStart && _backgroundTracks != null && _backgroundTracks.Length > 0)
                PlayMusicShuffle();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        #endregion

        #region Internals -------------------------------------------------------

        private void CreateAudioSources()
        {
            _musicSourceA = BuildAudioSource("MusicSourceA", spatialBlend: 0f);
            _musicSourceB = BuildAudioSource("MusicSourceB", spatialBlend: 0f);
            _sfx2dSource  = BuildAudioSource("SFX2DSource",  spatialBlend: 0f);
        }

        private AudioSource BuildAudioSource(string goName, float spatialBlend)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform, worldPositionStays: false);
            var src = go.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            src.spatialBlend = spatialBlend;
            src.volume = 0f;
            return src;
        }

        private void RestartMusicCoroutine(IEnumerator routine)
        {
            if (_musicCoroutine != null)
                StopCoroutine(_musicCoroutine);
            _musicCoroutine = StartCoroutine(routine);
        }

        // Crossfades from whatever is currently playing to a specific clip.
        // Used by PlayMusic(clip) for manual transitions.
        private IEnumerator CrossfadeToClipRoutine(AudioClip clip, bool loop)
        {
            var incoming = _isMusicSourceAActive ? _musicSourceB : _musicSourceA;
            var outgoing = _isMusicSourceAActive ? _musicSourceA : _musicSourceB;

            float targetVolume = _musicVolume * _masterVolume;
            float outgoingStart = outgoing.volume;

            incoming.clip = clip;
            incoming.loop = loop;
            incoming.volume = 0f;
            incoming.Play();

            float elapsed = 0f;
            while (elapsed < _musicFadeDuration)
            {
                float t = elapsed / _musicFadeDuration;
                incoming.volume = Mathf.Lerp(0f, targetVolume, t);
                outgoing.volume = Mathf.Lerp(outgoingStart, 0f, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            incoming.volume = targetVolume;
            outgoing.Stop();
            outgoing.volume = 0f;
            _isMusicSourceAActive = !_isMusicSourceAActive;

            OnTrackChanged?.Invoke(clip.name);
            Debug.Log($"{LOG_TAG} Music track -- '{clip.name}'.");
        }

        // Minecraft-style loop: play → fade out → silence → play → repeat.
        private IEnumerator MusicShuffleLoopRoutine()
        {
            // Small random initial delay before the first track, like Minecraft.
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(2f, 8f));

            while (true)
            {
                var clip = _backgroundTracks[_shuffledTrackOrder[_shufflePosition]];
                yield return StartCoroutine(PlayTrackWithFadesRoutine(clip));

                AdvanceShufflePosition();

                // Random silence between tracks.
                float silence = UnityEngine.Random.Range(_minSilenceDuration, _maxSilenceDuration);
                if (silence > 0f)
                {
                    Debug.Log($"{LOG_TAG} Music silence -- {silence:F0}s.");
                    yield return new WaitForSecondsRealtime(silence);
                }
            }
        }

        // Fades in a track, waits for it to finish, fades out. No overlap with next track.
        private IEnumerator PlayTrackWithFadesRoutine(AudioClip clip)
        {
            var source = _isMusicSourceAActive ? _musicSourceA : _musicSourceB;
            float targetVolume = _musicVolume * _masterVolume;

            source.clip = clip;
            source.loop = false;
            source.volume = 0f;
            source.Play();

            // Fade in.
            float elapsed = 0f;
            while (elapsed < _musicFadeDuration)
            {
                source.volume = Mathf.Lerp(0f, targetVolume, elapsed / _musicFadeDuration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            source.volume = targetVolume;

            OnTrackChanged?.Invoke(clip.name);
            Debug.Log($"{LOG_TAG} Music track -- '{clip.name}'.");

            // Wait until near the end to start the fade-out.
            float playDuration = Mathf.Max(0f, clip.length - _musicFadeDuration - 0.1f);
            yield return new WaitForSecondsRealtime(playDuration);

            // Fade out.
            float startVolume = source.volume;
            elapsed = 0f;
            while (elapsed < _musicFadeDuration)
            {
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / _musicFadeDuration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            source.Stop();
            source.volume = 0f;
        }

        private IEnumerator FadeOutMusicRoutine(float duration)
        {
            var active = _isMusicSourceAActive ? _musicSourceA : _musicSourceB;
            float startVolume = active.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                active.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            active.Stop();
            active.volume = 0f;
            Debug.Log($"{LOG_TAG} Music stopped.");
        }

        private IEnumerator Play3DRoutine(AudioClip clip, Vector3 position, float volume)
        {
            var go = new GameObject($"SFX3D_{clip.name}");
            go.transform.position = position;

            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.spatialBlend = 1f;
            src.volume = volume * _sfxVolume * _masterVolume;
            src.pitch = 1f + UnityEngine.Random.Range(-PITCH_VARIATION, PITCH_VARIATION);
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 0.5f;
            src.maxDistance = 20f;
            src.playOnAwake = false;
            src.Play();

            // Wait for the clip to finish (accounting for pitch-shifted duration).
            yield return new WaitForSeconds(clip.length / Mathf.Abs(src.pitch) + 0.05f);

            _activeSfx3dCount--;
            Destroy(go);
        }

        private void RefreshSourceVolumes()
        {
            var activeMusic = _isMusicSourceAActive ? _musicSourceA : _musicSourceB;
            if (activeMusic != null && activeMusic.isPlaying)
                activeMusic.volume = _musicVolume * _masterVolume;
        }

        private void BuildShuffledOrder()
        {
            int count = _backgroundTracks.Length;
            _shuffledTrackOrder = new int[count];

            for (int i = 0; i < count; i++)
                _shuffledTrackOrder[i] = i;

            // Fisher-Yates shuffle — every permutation equally likely.
            for (int i = count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_shuffledTrackOrder[i], _shuffledTrackOrder[j]) =
                    (_shuffledTrackOrder[j], _shuffledTrackOrder[i]);
            }

            _shufflePosition = 0;
        }

        private void AdvanceShufflePosition()
        {
            _shufflePosition++;
            if (_shufflePosition >= _shuffledTrackOrder.Length)
                BuildShuffledOrder();
        }

        private static AudioClip PickRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (_backgroundTracks == null || _backgroundTracks.Length == 0)
                Debug.LogWarning($"{LOG_TAG} _backgroundTracks is not assigned.", this);
            if (_uiClickSounds == null || _uiClickSounds.Length == 0)
                Debug.LogWarning($"{LOG_TAG} _uiClickSounds is not assigned.", this);
            if (_grabSounds == null || _grabSounds.Length == 0)
                Debug.LogWarning($"{LOG_TAG} _grabSounds is not assigned.", this);
            if (_impactSounds == null || _impactSounds.Length == 0)
                Debug.LogWarning($"{LOG_TAG} _impactSounds is not assigned.", this);
        }

        #endregion
    }
}
