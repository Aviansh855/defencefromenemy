using UnityEngine;

namespace MobRush
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null) _instance = FindAnyObjectByType<AudioManager>();
                return _instance;
            }
            private set => _instance = value;
        }

        private AudioSource sfxSource;
        private AudioSource musicSource;

        private AudioClip shootClip;
        private AudioClip popClip;
        private AudioClip gateChimeClip;
        private AudioClip baseHitClip;
        private AudioClip winFanfareClip;
        private AudioClip defeatClip;
        private AudioClip uiTapClip;
        private AudioClip upgradeClip;
        private AudioClip championClip;
        private AudioClip explosionClip;
        private AudioClip brickCollectClip;

        private int gateComboCount = 0;
        private float lastGateSoundTime = 0f;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                InitializeAudio();
            }
            else if (_instance != this)
            {
                Destroy(this);
            }
        }

        private void InitializeAudio()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            GenerateClips();
            StartAmbientBeat();
        }

        private void GenerateClips()
        {
            shootClip = CreateToneClip("Shoot", 420f, 0.05f, 0.25f, true);
            popClip = CreateToneClip("Pop", 750f, 0.04f, 0.35f, false);
            gateChimeClip = CreateChimeClip("GateChime", 880f, 1320f, 0.18f, 0.4f);
            baseHitClip = CreateToneClip("BaseHit", 220f, 0.08f, 0.4f, true);
            winFanfareClip = CreateFanfareClip("WinFanfare");
            defeatClip = CreateDefeatClip("Defeat");
            uiTapClip = CreateToneClip("UITap", 600f, 0.03f, 0.2f, false);
            upgradeClip = CreateChimeClip("Upgrade", 523f, 1046f, 0.25f, 0.45f);
            championClip = CreateChimeClip("ChampionHorn", 330f, 660f, 0.45f, 0.7f);
            explosionClip = CreateToneClip("Explosion", 90f, 0.35f, 0.8f, true);
            brickCollectClip = CreateToneClip("BrickCollect", 1200f, 0.04f, 0.25f, false);
        }

        private void StartAmbientBeat()
        {
            int sampleRate = 44100;
            int samples = sampleRate * 2; // 2-second looping rhythm
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                // Soft low pulse every 0.5s
                float pulse = Mathf.Sin(t * 8f * Mathf.PI);
                data[i] = (pulse > 0.85f ? (pulse - 0.85f) * 0.15f : 0f);
            }

            AudioClip musicClip = AudioClip.Create("AmbientMusic", samples, 1, sampleRate, false);
            musicClip.SetData(data, 0);

            musicSource.clip = musicClip;
            musicSource.volume = 0.3f;
            if (SaveManager.Data.musicEnabled)
            {
                musicSource.Play();
            }
        }

        public void PlayShoot() => PlayClip(shootClip, 0.2f);
        public void PlayPop() => PlayClip(popClip, 0.3f);

        // Dopamine-inducing musical arpeggio crescendo on crowd gate pass
        public void PlayGate()
        {
            if (Time.time - lastGateSoundTime < 0.5f)
            {
                gateComboCount++;
            }
            else
            {
                gateComboCount = 0;
            }
            lastGateSoundTime = Time.time;

            float pitch = Mathf.Clamp(1.0f + (gateComboCount % 12) * 0.05f, 1.0f, 2.2f);
            PlayClipWithPitch(gateChimeClip, 0.45f, pitch);
        }

        public void PlayBaseHit() => PlayClip(baseHitClip, 0.35f);
        public void PlayWin() => PlayClip(winFanfareClip, 0.6f);
        public void PlayDefeat() => PlayClip(defeatClip, 0.55f);
        public void PlayUITap() => PlayClip(uiTapClip, 0.3f);
        public void PlayUpgrade() => PlayClip(upgradeClip, 0.5f);
        public void PlayChampionSpawn() => PlayClip(championClip, 0.7f);
        public void PlayBoomExplosion() => PlayClip(explosionClip, 0.85f);
        public void PlayBrickCollect() => PlayClip(brickCollectClip, 0.3f);
        public void PlayCoinPickup() => PlayClip(brickCollectClip, 0.45f);

        private void PlayClipWithPitch(AudioClip clip, float vol, float pitch)
        {
            if (clip != null && SaveManager.Data.sfxEnabled)
            {
                sfxSource.pitch = pitch;
                sfxSource.PlayOneShot(clip, vol);
                sfxSource.pitch = 1.0f;
            }
        }

        private void PlayClip(AudioClip clip, float vol)
        {
            if (clip != null && SaveManager.Data.sfxEnabled)
            {
                sfxSource.pitch = 1.0f;
                sfxSource.PlayOneShot(clip, vol);
            }
        }

        public void SetMusicEnabled(bool enabled)
        {
            SaveManager.Data.musicEnabled = enabled;
            SaveManager.Save();

            if (enabled && !musicSource.isPlaying)
                musicSource.Play();
            else if (!enabled && musicSource.isPlaying)
                musicSource.Stop();
        }

        public void SetSfxEnabled(bool enabled)
        {
            SaveManager.Data.sfxEnabled = enabled;
            SaveManager.Save();
        }

        private AudioClip CreateToneClip(string name, float freq, float duration, float volume, bool pitchDrop)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float currentFreq = pitchDrop ? Mathf.Lerp(freq, freq * 0.4f, t) : freq;
                float envelope = 1f - t;
                data[i] = Mathf.Sin(2 * Mathf.PI * currentFreq * i / sampleRate) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateChimeClip(string name, float freq1, float freq2, float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Exp(-t * 9f);
                float wave = 0.5f * Mathf.Sin(2 * Mathf.PI * freq1 * i / sampleRate) +
                             0.5f * Mathf.Sin(2 * Mathf.PI * freq2 * i / sampleRate);
                data[i] = wave * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateFanfareClip(string name)
        {
            int sampleRate = 44100;
            float duration = 0.6f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };

            int noteLength = samples / notes.Length;
            for (int i = 0; i < samples; i++)
            {
                int noteIndex = Mathf.Min(i / noteLength, notes.Length - 1);
                float freq = notes[noteIndex];
                float localT = (float)(i % noteLength) / noteLength;
                float envelope = 1f - localT * 0.5f;
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate) * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateDefeatClip(string name)
        {
            int sampleRate = 44100;
            float duration = 0.5f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            float[] notes = { 440f, 370f, 311f, 261f }; // Descending minor notes

            int noteLength = samples / notes.Length;
            for (int i = 0; i < samples; i++)
            {
                int noteIndex = Mathf.Min(i / noteLength, notes.Length - 1);
                float freq = notes[noteIndex];
                float localT = (float)(i % noteLength) / noteLength;
                float envelope = 1f - localT * 0.5f;
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate) * envelope * 0.35f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
