using System;
using System.IO;
using Raylib_cs;
using PongGameV2.Core;

namespace PongGameV2.Audio;

public static class SoundManager
{
    public static Sound MenuMove { get; private set; }
    public static Sound MenuSelect { get; private set; }
    public static Sound PaddleHit { get; private set; }
    public static Sound WallBounce { get; private set; }
    public static Sound Score { get; private set; }
    public static Sound GameOver { get; private set; }
    public static Sound CountdownTick { get; private set; }
    public static Sound CountdownGo { get; private set; }
    public static Sound Swing { get; private set; }
    public static Sound SwingHit { get; private set; }
    public static Sound FireworkBurst { get; private set; }
    public static Sound FireworkCrackle { get; private set; }

    private static Music _menuMusic;
    private static Music _gameMusic;
    private static bool _musicPlaying;
    private static bool _inGame;

    private static string _soundDir = null!;

    public static void Init()
    {
        Raylib.InitAudioDevice();

        _soundDir = Path.Combine(Path.GetTempPath(), "PongV2Sounds");
        Directory.CreateDirectory(_soundDir);

        MenuMove = GenerateAndLoad("menu_move", SineWave(880, 0.05f, 0.3f));
        MenuSelect = GenerateAndLoad("menu_select", SineWave(1100, 0.08f, 0.4f));
        PaddleHit = GenerateAndLoad("paddle_hit", TennisHit());
        WallBounce = GenerateAndLoad("wall_bounce", SineWave(330, 0.04f, 0.2f));
        Score = GenerateAndLoad("score", DescendingTone(600, 200, 0.3f, 0.4f));
        GameOver = GenerateAndLoad("game_over", Fanfare());
        CountdownTick = GenerateAndLoad("countdown_tick", SineWave(600, 0.1f, 0.3f));
        CountdownGo = GenerateAndLoad("countdown_go", SineWave(900, 0.15f, 0.45f));
        Swing = GenerateAndLoad("swing", Whoosh());
        SwingHit = GenerateAndLoad("swing_hit", SwingImpact());
        FireworkBurst = GenerateAndLoad("firework_burst", GenerateFireworkBurst());
        FireworkCrackle = GenerateAndLoad("firework_crackle", GenerateFireworkCrackle());

        // Menu music (chill ambient)
        string menuMusicPath = Path.Combine(_soundDir, "menu_music.wav");
        WriteWav(menuMusicPath, GenerateMenuMusic());
        _menuMusic = Raylib.LoadMusicStream(menuMusicPath);
        _menuMusic.Looping = true;

        // Game music (upbeat, driving)
        string gameMusicPath = Path.Combine(_soundDir, "game_music.wav");
        WriteWav(gameMusicPath, GenerateGameMusic());
        _gameMusic = Raylib.LoadMusicStream(gameMusicPath);
        _gameMusic.Looping = true;
    }

    /// <summary>Call to switch between menu and in-game music.</summary>
    public static void SetInGame(bool inGame)
    {
        if (_inGame == inGame) return;
        _inGame = inGame;

        // Stop current, let UpdateMusic start the right one
        Raylib.StopMusicStream(_menuMusic);
        Raylib.StopMusicStream(_gameMusic);
        _musicPlaying = false;
    }

    private static Music ActiveMusic => _inGame ? _gameMusic : _menuMusic;

    public static void UpdateMusic()
    {
        var music = ActiveMusic;
        if (GameSettings.MusicEnabled)
        {
            Raylib.SetMusicVolume(music, GameSettings.EffectiveMusicVolume * 0.4f);
            if (!_musicPlaying)
            {
                Raylib.PlayMusicStream(music);
                _musicPlaying = true;
            }
            Raylib.UpdateMusicStream(music);
        }
        else if (_musicPlaying)
        {
            Raylib.StopMusicStream(music);
            _musicPlaying = false;
        }
    }

    public static void Shutdown()
    {
        Raylib.UnloadSound(MenuMove);
        Raylib.UnloadSound(MenuSelect);
        Raylib.UnloadSound(PaddleHit);
        Raylib.UnloadSound(WallBounce);
        Raylib.UnloadSound(Score);
        Raylib.UnloadSound(GameOver);
        Raylib.UnloadSound(CountdownTick);
        Raylib.UnloadSound(CountdownGo);
        Raylib.UnloadSound(Swing);
        Raylib.UnloadSound(SwingHit);
        Raylib.UnloadSound(FireworkBurst);
        Raylib.UnloadSound(FireworkCrackle);
        Raylib.UnloadMusicStream(_menuMusic);
        Raylib.UnloadMusicStream(_gameMusic);
        Raylib.CloseAudioDevice();
    }

    public static void Play(Sound sound)
    {
        Raylib.SetSoundVolume(sound, GameSettings.EffectiveSfxVolume);
        Raylib.PlaySound(sound);
    }

    // --- Wave generators ---

    private const int SampleRate = 44100;

    private static short[] SineWave(float freq, float duration, float volume)
    {
        int samples = (int)(SampleRate * duration);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = 1f - (float)i / samples; // linear fade out
            float value = MathF.Sin(2f * MathF.PI * freq * t) * envelope * volume;
            data[i] = (short)(value * short.MaxValue);
        }
        return data;
    }

    private static short[] SquareWave(float freq, float duration, float volume)
    {
        int samples = (int)(SampleRate * duration);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = 1f - (float)i / samples;
            float sin = MathF.Sin(2f * MathF.PI * freq * t);
            float value = (sin >= 0 ? 1f : -1f) * envelope * volume;
            data[i] = (short)(value * short.MaxValue);
        }
        return data;
    }

    private static short[] DescendingTone(float startFreq, float endFreq, float duration, float volume)
    {
        int samples = (int)(SampleRate * duration);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / samples;
            float freq = startFreq + (endFreq - startFreq) * progress;
            float envelope = 1f - progress;
            float value = MathF.Sin(2f * MathF.PI * freq * t) * envelope * volume;
            data[i] = (short)(value * short.MaxValue);
        }
        return data;
    }

    private static short[] Fanfare()
    {
        // Three ascending notes: C5 -> E5 -> G5 with a final sustain
        float[] notes = [523.25f, 659.25f, 783.99f];
        float noteDuration = 0.12f;
        float sustainDuration = 0.25f;
        float totalDuration = notes.Length * noteDuration + sustainDuration;
        int totalSamples = (int)(SampleRate * totalDuration);
        var data = new short[totalSamples];
        float volume = 0.35f;

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            int noteIndex = Math.Min((int)(t / noteDuration), notes.Length - 1);
            float freq = notes[noteIndex];

            // Envelope: each note fades in quickly, final note sustains and fades
            float localT;
            float envelope;
            if (noteIndex < notes.Length - 1)
            {
                localT = (t - noteIndex * noteDuration) / noteDuration;
                envelope = MathF.Min(localT * 8f, 1f); // quick attack
            }
            else
            {
                float finalStart = (notes.Length - 1) * noteDuration;
                localT = (t - finalStart) / (totalDuration - finalStart);
                envelope = MathF.Min(localT * 4f, 1f) * (1f - MathF.Max(0, localT - 0.3f) / 0.7f);
            }

            float value = MathF.Sin(2f * MathF.PI * freq * t) * envelope * volume;
            data[i] = (short)(value * short.MaxValue);
        }
        return data;
    }

    private static short[] TennisHit()
    {
        // Tennis racket pop: short noise burst + low resonance, softer than a square wave
        int samples = (int)(SampleRate * 0.1f);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / samples;
            // Quick attack, smooth decay
            float envelope = MathF.Exp(-progress * 12f);
            // Mid-frequency body (racket resonance)
            float body = MathF.Sin(2f * MathF.PI * 220f * t) * 0.35f;
            // String snap (higher, fades fast)
            float snap = MathF.Sin(2f * MathF.PI * 580f * t) * MathF.Exp(-progress * 25f) * 0.25f;
            // Soft noise (ball felt impact)
            float noise = (Random.Shared.NextSingle() * 2f - 1f) * MathF.Exp(-progress * 18f) * 0.15f;
            float value = (body + snap + noise) * envelope * 0.5f;
            data[i] = (short)(value * short.MaxValue);
        }
        return data;
    }

    private static short[] Whoosh()
    {
        // Quick noise sweep — sounds like a fast swing
        int samples = (int)(SampleRate * 0.12f);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float envelope = MathF.Sin(t * MathF.PI) * 0.3f;
            // Filtered noise with descending pitch
            float freq = 800f + (1f - t) * 1200f;
            float noise = (Random.Shared.NextSingle() * 2f - 1f) * 0.5f;
            float tone = MathF.Sin(2f * MathF.PI * freq * (float)i / SampleRate) * 0.5f;
            data[i] = (short)((noise * 0.4f + tone * 0.6f) * envelope * short.MaxValue);
        }
        return data;
    }

    private static short[] SwingImpact()
    {
        // Hard impact: low thud + bright crack
        int samples = (int)(SampleRate * 0.15f);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / samples;
            float envelope = MathF.Exp(-progress * 8f);
            // Low thud
            float low = MathF.Sin(2f * MathF.PI * 120f * t) * 0.5f;
            // Bright crack
            float high = MathF.Sin(2f * MathF.PI * 800f * t) * MathF.Exp(-progress * 20f) * 0.4f;
            // Noise burst
            float noise = (Random.Shared.NextSingle() * 2f - 1f) * MathF.Exp(-progress * 15f) * 0.2f;
            float value = (low + high + noise) * envelope * 0.5f;
            data[i] = (short)(value * short.MaxValue);
        }
        return data;
    }

    // --- Firework sound generators ---

    private static short[] GenerateFireworkBurst()
    {
        // Rising whistle (0-0.25s) → big explosion burst (0.25-0.8s) → sparkle tail
        int samples = (int)(SampleRate * 0.8f);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / samples;
            float value = 0f;

            // Phase 1: Rising whistle (first 0.25s)
            if (t < 0.25f)
            {
                float wp = t / 0.25f;
                float whistleFreq = 400f + wp * wp * 2800f; // accelerating rise
                float whistleEnv = wp * 0.4f;
                value += MathF.Sin(2f * MathF.PI * whistleFreq * t) * whistleEnv;
                // Slight noise underneath
                value += (Random.Shared.NextSingle() * 2f - 1f) * wp * 0.05f;
            }

            // Phase 2: Explosion burst (at 0.25s mark)
            if (t >= 0.22f)
            {
                float ep = (t - 0.22f);
                float boomEnv = MathF.Exp(-ep * 5f);

                // Deep boom
                value += MathF.Sin(2f * MathF.PI * 65f * t) * boomEnv * 0.5f;
                value += MathF.Sin(2f * MathF.PI * 100f * t) * boomEnv * 0.3f;

                // Noise burst (the main crackle)
                float crackleEnv = MathF.Exp(-ep * 4f);
                value += (Random.Shared.NextSingle() * 2f - 1f) * crackleEnv * 0.25f;

                // High frequency sparkle
                float sparkleEnv = MathF.Max(0, ep - 0.05f) * MathF.Exp(-ep * 2.5f);
                value += MathF.Sin(2f * MathF.PI * 1800f * t) * sparkleEnv * 0.1f;
                value += MathF.Sin(2f * MathF.PI * 3200f * t) * sparkleEnv * 0.06f;

                // Crackle pops (random high-frequency pops in the tail)
                if (ep > 0.15f && Random.Shared.NextSingle() < 0.03f)
                    value += (Random.Shared.NextSingle() * 2f - 1f) * MathF.Exp(-(ep - 0.15f) * 3f) * 0.15f;
            }

            data[i] = (short)(Math.Clamp(value * 0.55f, -1f, 1f) * short.MaxValue);
        }
        return data;
    }

    private static short[] GenerateFireworkCrackle()
    {
        // Rapid crackle pop — lighter, shorter firework
        int samples = (int)(SampleRate * 0.45f);
        var data = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / samples;

            // Quick pop
            float popEnv = MathF.Exp(-progress * 10f);
            float pop = MathF.Sin(2f * MathF.PI * 200f * t) * popEnv * 0.2f;

            // Scattered crackle (random clicks)
            float crackle = 0f;
            float crackleEnv = (1f - progress) * MathF.Exp(-progress * 3f);
            if (Random.Shared.NextSingle() < 0.08f)
                crackle = (Random.Shared.NextSingle() * 2f - 1f) * crackleEnv * 0.3f;

            // Gentle noise bed
            float noise = (Random.Shared.NextSingle() * 2f - 1f) * crackleEnv * 0.08f;

            // Sparkle tinkle
            float sparkle = MathF.Sin(2f * MathF.PI * 5000f * t) * MathF.Exp(-progress * 6f) * 0.05f;
            sparkle += MathF.Sin(2f * MathF.PI * 3500f * t) * MathF.Exp(-progress * 5f) * 0.04f;

            float value = (pop + crackle + noise + sparkle) * 0.6f;
            data[i] = (short)(Math.Clamp(value, -1f, 1f) * short.MaxValue);
        }
        return data;
    }

    // --- Menu music generator ---

    private static short[] GenerateMenuMusic()
    {
        // 24-second chill ambient loop — unobtrusive menu atmosphere
        const float duration = 24f;
        const float bpm = 85f;
        float beatLen = 60f / bpm;
        int totalSamples = (int)(SampleRate * duration);
        var data = new float[totalSamples];

        // Warm, dreamy chord progression: Cmaj7 - Am7 - Fmaj7 - G
        float[][] chords =
        [
            [130.81f, 164.81f, 196f, 246.94f],   // Cmaj7
            [110f, 130.81f, 164.81f, 196f],       // Am7
            [87.31f, 110f, 130.81f, 164.81f],     // Fmaj7
            [98f, 123.47f, 146.83f, 185f],        // G
        ];

        // Gentle arpeggio notes (slow, sparse)
        float[][] arps =
        [
            [523.25f, 659.25f, 784f, 987.77f],
            [440f, 523.25f, 659.25f, 784f],
            [349.23f, 440f, 523.25f, 659.25f],
            [392f, 493.88f, 587.33f, 784f],
        ];

        float chordDuration = 6f * beatLen; // longer chords = more relaxed

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float cycleT = t % (chordDuration * chords.Length);
            int chordIndex = (int)(cycleT / chordDuration) % chords.Length;
            float chordT = cycleT - chordIndex * chordDuration;
            float chordProgress = chordT / chordDuration;

            // Smooth pad — sustained sine chords with slow crossfade
            float padEnv = MathF.Min(chordProgress * 6f, 1f) * MathF.Min((1f - chordProgress) * 6f, 1f);
            float pad = 0f;
            foreach (float freq in chords[chordIndex])
            {
                // Pure sine tones, warm and soft
                pad += MathF.Sin(2f * MathF.PI * freq * t) * 0.04f;
                // Subtle detuned layer for warmth
                pad += MathF.Sin(2f * MathF.PI * (freq * 1.003f) * t) * 0.015f;
            }
            pad *= padEnv;

            // Sub bass — gentle sine, just the root note
            float bassFreq = chords[chordIndex][0] * 0.5f;
            float bassEnv = padEnv * 0.6f;
            float bass = MathF.Sin(2f * MathF.PI * bassFreq * t) * bassEnv * 0.10f;

            // Slow arpeggio — one note per beat, soft sine plucks
            float beatProgress = (chordT % beatLen) / beatLen;
            int arpNote = (int)(chordT / beatLen) % arps[chordIndex].Length;
            float arpFreq = arps[chordIndex][arpNote];
            // Soft pluck envelope: quick attack, long decay
            float arpEnv = MathF.Exp(-beatProgress * 3f) * padEnv;
            float arp = MathF.Sin(2f * MathF.PI * arpFreq * t) * arpEnv * 0.06f;
            // Octave shimmer (very quiet)
            arp += MathF.Sin(2f * MathF.PI * arpFreq * 2f * t) * arpEnv * 0.015f;

            // Gentle kick — soft thump on beat 1 only, felt more than heard
            float kickPhase = chordT % (beatLen * 3f);
            float kick = 0f;
            if (kickPhase < 0.05f)
            {
                float kt = kickPhase / 0.05f;
                kick = MathF.Sin(2f * MathF.PI * 60f * kickPhase) * (1f - kt) * 0.08f;
            }

            // Soft hi-hat — sparse, on every other beat
            float halfBeat = beatLen * 2f;
            float hatPhase = (chordT % halfBeat) / halfBeat;
            float hat = 0f;
            if (hatPhase < 0.015f)
            {
                hat = (Random.Shared.NextSingle() * 2f - 1f) * 0.02f * (1f - hatPhase / 0.015f);
            }

            data[i] = pad + bass + arp + kick + hat;
        }

        // Normalize and convert — keep it quiet
        float maxVal = 0f;
        for (int i = 0; i < data.Length; i++)
            maxVal = MathF.Max(maxVal, MathF.Abs(data[i]));

        float norm = maxVal > 0 ? 0.6f / maxVal : 1f;
        var pcm = new short[totalSamples];
        for (int i = 0; i < totalSamples; i++)
            pcm[i] = (short)(data[i] * norm * short.MaxValue);

        return pcm;
    }

    // --- Game music generator ---

    private static short[] GenerateGameMusic()
    {
        // 16-second upbeat loop — subtle but driving energy for gameplay
        const float duration = 16f;
        const float bpm = 110f;
        float beatLen = 60f / bpm;
        int totalSamples = (int)(SampleRate * duration);
        var data = new float[totalSamples];

        // Minor progression: Am - F - C - G (classic tension/release)
        float[][] chords =
        [
            [110f, 130.81f, 164.81f],    // Am
            [87.31f, 110f, 130.81f],     // F
            [130.81f, 164.81f, 196f],    // C
            [98f, 123.47f, 146.83f],     // G
        ];

        float[][] arps =
        [
            [440f, 523.25f, 659.25f, 523.25f],
            [349.23f, 440f, 523.25f, 440f],
            [523.25f, 659.25f, 784f, 659.25f],
            [392f, 493.88f, 587.33f, 493.88f],
        ];

        float chordDuration = 4f * beatLen;

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float cycleT = t % (chordDuration * chords.Length);
            int chordIndex = (int)(cycleT / chordDuration) % chords.Length;
            float chordT = cycleT - chordIndex * chordDuration;

            // Warm sine pad — sustained, gentle
            float chordProgress = chordT / chordDuration;
            float padEnv = MathF.Min(chordProgress * 8f, 1f) * MathF.Min((1f - chordProgress) * 8f, 1f);
            float pad = 0f;
            foreach (float freq in chords[chordIndex])
            {
                pad += MathF.Sin(2f * MathF.PI * freq * t) * 0.03f;
                pad += MathF.Sin(2f * MathF.PI * freq * 1.002f * t) * 0.01f;
            }
            pad *= padEnv;

            // Pulsing bass — eighth notes with gentle saw
            float eighthLen = beatLen / 2f;
            float eighthT = (chordT % eighthLen) / eighthLen;
            float bassFreq = chords[chordIndex][0] * 0.5f;
            float bassEnv = MathF.Exp(-eighthT * 6f) * padEnv;
            float bass = MathF.Sin(2f * MathF.PI * bassFreq * t) * bassEnv * 0.10f;

            // Arpeggio — quarter notes, soft plucks
            float beatProgress = (chordT % beatLen) / beatLen;
            int arpNote = (int)(chordT / beatLen) % arps[chordIndex].Length;
            float arpFreq = arps[chordIndex][arpNote];
            float arpEnv = MathF.Exp(-beatProgress * 4f) * padEnv;
            float arp = MathF.Sin(2f * MathF.PI * arpFreq * t) * arpEnv * 0.06f;
            arp += MathF.Sin(2f * MathF.PI * arpFreq * 2f * t) * arpEnv * 0.015f;

            // Soft kick on 1 and 3
            float kickPhase = chordT % (beatLen * 2f);
            float kick = 0f;
            if (kickPhase < 0.05f)
            {
                float kt = kickPhase / 0.05f;
                kick = MathF.Sin(2f * MathF.PI * 70f * kickPhase) * (1f - kt) * 0.12f;
            }

            // Subtle hi-hat on every beat
            float hatPhase = (chordT % beatLen) / beatLen;
            float hat = 0f;
            if (hatPhase < 0.02f)
            {
                hat = (Random.Shared.NextSingle() * 2f - 1f) * 0.03f * (1f - hatPhase / 0.02f);
            }

            data[i] = pad + bass + arp + kick + hat;
        }

        float maxVal = 0f;
        for (int i = 0; i < data.Length; i++)
            maxVal = MathF.Max(maxVal, MathF.Abs(data[i]));

        float norm = maxVal > 0 ? 0.65f / maxVal : 1f;
        var pcm = new short[totalSamples];
        for (int i = 0; i < totalSamples; i++)
            pcm[i] = (short)(data[i] * norm * short.MaxValue);

        return pcm;
    }

    // --- WAV file creation ---

    private static Sound GenerateAndLoad(string name, short[] pcmData)
    {
        string path = Path.Combine(_soundDir, $"{name}.wav");
        WriteWav(path, pcmData);
        return Raylib.LoadSound(path);
    }

    private static void WriteWav(string path, short[] pcmData)
    {
        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        int dataSize = pcmData.Length * 2; // 16-bit = 2 bytes per sample

        // RIFF header
        bw.Write("RIFF"u8);
        bw.Write(36 + dataSize);
        bw.Write("WAVE"u8);

        // fmt chunk
        bw.Write("fmt "u8);
        bw.Write(16);           // chunk size
        bw.Write((short)1);     // PCM format
        bw.Write((short)1);     // mono
        bw.Write(SampleRate);   // sample rate
        bw.Write(SampleRate * 2); // byte rate (sampleRate * channels * bitsPerSample/8)
        bw.Write((short)2);     // block align
        bw.Write((short)16);    // bits per sample

        // data chunk
        bw.Write("data"u8);
        bw.Write(dataSize);
        foreach (short sample in pcmData)
            bw.Write(sample);
    }
}
