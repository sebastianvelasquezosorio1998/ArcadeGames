using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Música y efectos. Los clips salen del SkinProfile activo (marca blanca),
    /// el código solo pide por ID: AudioManager.Play(SfxId.Coin).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const int SfxVoices = 10;

        public static AudioManager Instance { get; private set; }

        private AudioSource _music;
        private AudioSource[] _sfx;
        private int _nextVoice;
        private MusicId _currentMusic = MusicId.None;
        private float _musicBaseVolume = 1f;

        public static void Play(SfxId id)
        {
            if (Instance != null) Instance.PlaySfx(id);
        }

        public static void PlayMusic(MusicId id)
        {
            if (Instance != null) Instance.SetMusic(id);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;

            _sfx = new AudioSource[SfxVoices];
            for (int i = 0; i < SfxVoices; i++)
            {
                _sfx[i] = gameObject.AddComponent<AudioSource>();
                _sfx[i].playOnAwake = false;
            }
        }

        public void PlaySfx(SfxId id)
        {
            var entry = SkinManager.GetSfx(id);
            var clip = entry?.RandomClip;
            if (clip == null) return;

            var source = _sfx[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _sfx.Length;
            source.pitch = 1f + Random.Range(-entry.pitchVariation, entry.pitchVariation);
            source.PlayOneShot(clip, entry.volume * SfxVolume);
        }

        public void SetMusic(MusicId id)
        {
            if (id == _currentMusic && _music.isPlaying) return;
            _currentMusic = id;
            var entry = SkinManager.GetMusic(id);
            if (entry == null) { _music.Stop(); return; }
            _music.clip = entry.clip;
            _musicBaseVolume = entry.volume;
            ApplyVolumes();
            _music.Play();
        }

        public void ApplyVolumes()
        {
            if (_music != null) _music.volume = _musicBaseVolume * MusicVolume;
        }

        public static float MusicVolume => SaveSystem.Data != null ? SaveSystem.Data.settings.musicVolume : 1f;
        public static float SfxVolume => SaveSystem.Data != null ? SaveSystem.Data.settings.sfxVolume : 1f;

        /// <summary>Activa/desactiva todo el sonido (botón de ajustes del menú).</summary>
        public static void SetMuted(bool muted)
        {
            if (SaveSystem.Data == null) return;
            SaveSystem.Data.settings.musicVolume = muted ? 0f : 0.7f;
            SaveSystem.Data.settings.sfxVolume = muted ? 0f : 1f;
            SaveSystem.Save();
            if (Instance != null) Instance.ApplyVolumes();
        }

        public static bool IsMuted => MusicVolume <= 0f && SfxVolume <= 0f;
    }
}
