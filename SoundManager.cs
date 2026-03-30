using System;
using System.IO;
using Raylib_cs;

namespace PongGameV2;

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

    private static Music _bgMusic;
    private static bool _musicPlaying;

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

        // Background music
        string musicPath = Path.Combine(_soundDir, "bg_music.wav");
        WriteWav(musicPath, GenerateBgMusic());
        _bgMusic = Raylib.LoadMusicStream(musicPath);
        _bgMusic.Looping = true;
    }

    public static void UpdateMusic()
    {
        if (GameSettings.MusicEnabled)
        {
            Raylib.SetMusicVolume(_bgMusic, GameSettings.Volume / 10f * 0.4f);
            if (!_musicPlaying)
            {
                Raylib.PlayMusicStream(_bgMusic);
                _musicPlaying = true;
            }
            Raylib.UpdateMusicStream(_bgMusic);
        }
        else if (_musicPlaying)
        {
            Raylib.StopMusicStream(_bgMusic);
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
        Raylib.UnloadMusicStream(_bgMusic);
        Raylib.CloseAudioDevice();
    }

    public static void Play(Sound sound)
    {
        Raylib.SetSoundVolume(sound, GameSettings.Volume / 10f);
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

    // --- Background music generator ---

    private static short[] GenerateBgMusic()
    {
        // 16-second energetic loop — upbeat tennis match vibe
        const float duration = 16f;
        const float bpm = 128f;
        float beatLen = 60f / bpm;
        int totalSamples = (int)(SampleRate * duration);
        var data = new float[totalSamples];

        // Driving chord progression: Am - C - F - G
        float[][] chords =
        [
            [220f, 261.63f, 329.63f],   // Am
            [261.63f, 329.63f, 392f],    // C
            [174.61f, 261.63f, 349.23f], // F
            [196f, 246.94f, 392f],       // G
        ];

        // Punchy arpeggio patterns per chord
        float[][] arps =
        [
            [440f, 523.25f, 659.25f, 784f, 659.25f, 523.25f, 440f, 523.25f],
            [523.25f, 659.25f, 784f, 1046.5f, 784f, 659.25f, 523.25f, 659.25f],
            [349.23f, 523.25f, 698.46f, 523.25f, 349.23f, 523.25f, 698.46f, 523.25f],
            [392f, 587.33f, 784f, 587.33f, 392f, 493.88f, 587.33f, 784f],
        ];

        float chordDuration = 4f * beatLen;

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float cycleT = t % (chordDuration * chords.Length);
            int chordIndex = (int)(cycleT / chordDuration) % chords.Length;
            float chordT = cycleT - chordIndex * chordDuration;

            // Staccato pad (short chord stabs on each beat)
            float beatT = (chordT % beatLen) / beatLen;
            float padEnv = beatT < 0.3f ? MathF.Exp(-beatT * 12f) : 0f;
            float pad = 0f;
            foreach (float freq in chords[chordIndex])
            {
                pad += MathF.Sin(2f * MathF.PI * freq * t) * 0.06f;
            }
            pad *= padEnv;

            // Driving bass (eighth note pattern, alternating root and fifth)
            float eighthLen = beatLen / 2f;
            float eighthT = (chordT % eighthLen) / eighthLen;
            int eighthIndex = (int)(chordT / eighthLen) % 2;
            float bassFreq = chords[chordIndex][0] * 0.5f;
            if (eighthIndex == 1) bassFreq *= 1.5f; // fifth
            float bassEnv = MathF.Exp(-eighthT * 8f);
            float bassPhase = (bassFreq * t) % 1f;
            float bass = (MathF.Abs(bassPhase * 4f - 2f) - 1f) * bassEnv * 0.14f;

            // Fast arpeggio (sixteenth notes)
            float sixteenthLen = beatLen / 4f;
            int noteIndex = (int)(chordT / sixteenthLen) % arps[chordIndex].Length;
            float noteT = (chordT % sixteenthLen) / sixteenthLen;
            float arpFreq = arps[chordIndex][noteIndex];
            float arpEnv = MathF.Min(noteT * 30f, 1f) * MathF.Exp(-noteT * 6f);
            float arp = MathF.Sin(2f * MathF.PI * arpFreq * t) * arpEnv * 0.12f;
            arp += MathF.Sin(2f * MathF.PI * arpFreq * 2f * t) * arpEnv * 0.03f;

            // Kick drum on beats 1 and 3
            float kickT = chordT % (beatLen * 2f);
            float kick = 0f;
            if (kickT < 0.08f)
            {
                float kt = kickT / 0.08f;
                float kickFreq = 150f * (1f - kt * 0.7f);
                kick = MathF.Sin(2f * MathF.PI * kickFreq * kickT) * (1f - kt) * 0.18f;
            }

            // Hi-hat on every eighth note
            float hatPhaseT = (chordT % eighthLen) / eighthLen;
            float hat = 0f;
            if (hatPhaseT < 0.04f)
            {
                hat = (Random.Shared.NextSingle() * 2f - 1f) * 0.05f * (1f - hatPhaseT / 0.04f);
            }

            // Snare on beats 2 and 4
            float snarePhase = (chordT + beatLen) % (beatLen * 2f);
            float snare = 0f;
            if (snarePhase < 0.06f)
            {
                float st = snarePhase / 0.06f;
                snare = (Random.Shared.NextSingle() * 2f - 1f) * (1f - st) * 0.1f;
                snare += MathF.Sin(2f * MathF.PI * 200f * snarePhase) * (1f - st) * 0.08f;
            }

            data[i] = pad + bass + arp + kick + hat + snare;
        }

        // Normalize and convert
        float maxVal = 0f;
        for (int i = 0; i < data.Length; i++)
            maxVal = MathF.Max(maxVal, MathF.Abs(data[i]));

        float norm = maxVal > 0 ? 0.7f / maxVal : 1f;
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
