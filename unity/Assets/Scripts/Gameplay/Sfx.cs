// Prozeduraler Ton (wie im Browser-Prototyp): alle Klänge werden beim Start berechnet, keine Audiodateien.
// Wischen (Rauschen, solange gewischt wird), Parzelle fertig, Kasse, Rakete, Kristall, Fund, Kombo, Fehler.
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public class Sfx : MonoBehaviour
    {
        public static Sfx I;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        AudioSource one, loop, music; float loopGoal, musicT;
        const int SR = 22050;

        void Awake()
        {
            I = this;
            one = gameObject.AddComponent<AudioSource>(); one.playOnAwake = false; one.spatialBlend = 0;
            loop = gameObject.AddComponent<AudioSource>(); loop.loop = true; loop.volume = 0; loop.playOnAwake = false;
            music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.volume = 0; music.playOnAwake = false;
            clips["pop"] = Tone(new[] { 660f, 990f }, .14f, .5f, 0);
            clips["cash"] = Tone(new[] { 1320f, 1760f, 2640f }, .22f, .35f, .04f);
            clips["rocket"] = Noise(1.6f, .5f, 180, true);
            clips["crystal"] = Tone(new[] { 1568f, 2093f, 2637f }, .5f, .3f, .08f);
            clips["find"] = Tone(new[] { 784f, 988f, 1175f, 1568f }, .7f, .35f, .11f);
            clips["combo"] = Tone(new[] { 880f, 1320f }, .12f, .35f, 0);
            clips["bad"] = Tone(new[] { 220f, 180f }, .2f, .35f, .07f);
            clips["wipe"] = Noise(1f, .25f, 2400, false);
            loop.clip = clips["wipe"];
            music.clip = Pad(16f);
        }

        void Start()
        {
            Game.OnFloat += (d, t, k) => { if (k == "big") Play("pop"); else if (t.StartsWith("Kombo")) Play("combo", 1 + .05f * Game.ComboN); else if (t.Contains("Kristall") || t.Contains("kristall")) Play("crystal"); };
            Game.OnRocket += (p, v) => { Play("rocket"); Play("cash", 1, .1f); };
            Game.OnFind += (n, d, c) => Play("find");
            Game.OnToast += (t, k) => { if (k == "bad") Play("bad", 1, .2f); };
        }

        public static void Wipe(bool on) { if (I) I.loopGoal = on ? .18f : 0; }
        public static void Play(string key, float pitch = 1, float vol = .5f) { if (I) I.PlayI(key, pitch, vol); }
        void PlayI(string key, float pitch, float vol)
        {
            if (Game.S == null || !Game.S.sound || Game.Offline) return; if (!clips.TryGetValue(key, out var c)) return;
            one.pitch = pitch; one.PlayOneShot(c, vol * Game.S.volSfx);
        }

        void Update()
        {
            bool snd = Game.S != null && Game.S.sound;
            float g = snd ? loopGoal * Game.S.volSfx : 0; loop.volume = Mathf.MoveTowards(loop.volume, g, Time.unscaledDeltaTime * 1.5f);
            if (loop.volume > .001f && !loop.isPlaying) loop.Play(); else if (loop.volume <= .001f && loop.isPlaying) loop.Pause();
            // Musik: leiser Klangteppich, lauter wenn die Basis arbeitet
            bool mus = snd && Game.S != null && Game.S.music; float mg = 0;
            if (mus) { var ps = Game.Cur; mg = (.05f + .05f * Mathf.Clamp01(ps.ef) * (ps.sv.veh.Count > 0 ? 1 : .4f)) * Game.S.volMusic * 1.6f; }
            music.volume = Mathf.MoveTowards(music.volume, mg, Time.unscaledDeltaTime * .1f);
            if (music.volume > .001f && !music.isPlaying) music.Play(); else if (music.volume <= .001f && music.isPlaying) music.Pause();
        }

        // ---------- Klangerzeugung ----------
        static AudioClip Tone(float[] freqs, float dur, float vol, float stagger)
        {
            int n = Mathf.CeilToInt(SR * (dur + stagger * freqs.Length)); var d = new float[n];
            for (int k = 0; k < freqs.Length; k++)
            {
                int o = Mathf.RoundToInt(stagger * k * SR); int len = Mathf.RoundToInt(dur * SR);
                for (int i = 0; i < len && o + i < n; i++) { float t = i / (float)SR, env = Mathf.Exp(-t * 9 / dur) * Mathf.Clamp01(i / 60f); d[o + i] += Mathf.Sin(2 * Mathf.PI * freqs[k] * t) * env * vol / freqs.Length * 1.6f; }
            }
            var c = AudioClip.Create("t", n, 1, SR, false); c.SetData(d, 0); return c;
        }
        static AudioClip Noise(float dur, float vol, float cut, bool sweep)
        {
            int n = Mathf.CeilToInt(SR * dur); var d = new float[n]; var r = new System.Random(5); float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n, fc = sweep ? Mathf.Lerp(cut, cut * 4, t) : cut, a = Mathf.Clamp01(2 * Mathf.PI * fc / SR);
                lp += a * ((float)r.NextDouble() * 2 - 1 - lp);
                float env = sweep ? Mathf.Sin(Mathf.PI * Mathf.Min(1, t * 1.3f)) : 1; d[i] = lp * vol * env * 2.2f;
            }
            if (!sweep) { int f = SR / 20; for (int i = 0; i < f; i++) { float k = i / (float)f; d[i] *= k; d[n - 1 - i] *= k; } } // nahtlose Schleife
            var c = AudioClip.Create("n", n, 1, SR, false); c.SetData(d, 0); return c;
        }
        static AudioClip Pad(float dur)
        {
            int n = Mathf.CeilToInt(SR * dur); var d = new float[n]; float[] ch = { 110f, 165f, 220f, 277.25f, 329.625f }; /* ganze Schwingungen in 16 s: Schleife ohne Knacken */
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR, s = 0;
                for (int k = 0; k < ch.Length; k++) s += Mathf.Sin(2 * Mathf.PI * ch[k] * t + k) * (.5f + .5f * Mathf.Sin(2 * Mathf.PI * t / dur * (k + 1) + k * 1.7f));
                d[i] = s / ch.Length * .5f;
            }
            var c = AudioClip.Create("pad", n, 1, SR, false); c.SetData(d, 0); return c;
        }
    }
}
