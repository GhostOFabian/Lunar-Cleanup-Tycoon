// Prozeduraler Ton (Fabian 01.11.: „Sounds verbessern“) – alles wird beim Start berechnet, keine Audiodateien.
// Wischen: weiches Bürsten-Saugen, das mit dem Tempo heller wird, dazu Knistern; Parzelle fertig: Glocke, bei Serien eine Tonleiter;
// Basis-Geräusche nur aus der Nähe (Summen, Presse); Meteor-Pfeifen und Einschlag, Geysir-Zischen, Sturmwind;
// Entdeckung, Erfolg, Kauf, Klick; Musik je Planetengruppe (warm, eisig, fremd, unheimlich) mit Überblendung.
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public class Sfx : MonoBehaviour
    {
        public static Sfx I;
        /// <summary>Kamerahöhe über dem Boden (setzt GameRoot) – Basis-Geräusche nur aus der Nähe.</summary>
        public static float CamAlt = 60; public static Vector3 CamPos;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, AudioClip> songs = new Dictionary<string, AudioClip>();
        AudioSource one, loop, hum, wind, musA, musB; float loopGoal, wipeSpd, crackT, chainT; int chainN; string song; bool musOnA = true;
        const int SR = 22050;
        static readonly float[] Penta = { 1f, 1.125f, 1.25f, 1.5f, 1.667f, 2f, 2.25f, 2.5f, 3f };

        AudioSource Src(bool looping) { var s = gameObject.AddComponent<AudioSource>(); s.playOnAwake = false; s.loop = looping; s.spatialBlend = 0; s.volume = 0; return s; }
        void Awake()
        {
            I = this; one = Src(false); one.volume = 1; loop = Src(true); hum = Src(true); wind = Src(true); musA = Src(true); musB = Src(true);
            clips["wipe"] = Brush(2f); clips["crackle"] = Crackle(.35f);
            clips["chime"] = Bell(880, .9f, .55f); clips["pop"] = Bell(660, .35f, .45f); clips["combo"] = clips["chime"];
            clips["cash"] = Coins(); clips["buy"] = Arp(new[] { 784f, 988f, 1319f }, .07f, .5f, .4f); clips["click"] = Click();
            clips["crystal"] = Arp(new[] { 1568f, 2093f, 2637f, 3136f }, .06f, .9f, .32f); clips["find"] = Arp(new[] { 659f, 784f, 988f, 1319f, 1568f }, .09f, 1.1f, .4f);
            clips["discover"] = Arp(new[] { 523f, 659f, 784f, 1047f, 1319f, 1568f }, .11f, 1.6f, .42f); clips["medal"] = Fanfare();
            clips["bad"] = Blip(new[] { 330f, 247f }, .12f, .35f);
            clips["rocket"] = Rumble(3f); clips["whistle"] = Whistle(1.1f); clips["boom"] = Boom(1.6f); clips["geyser"] = Hiss(2.6f);
            clips["hum"] = BaseHum(4f); clips["wind"] = Wind(6f);
            loop.clip = clips["wipe"]; hum.clip = clips["hum"]; wind.clip = clips["wind"];
        }

        void Start()
        {
            Game.OnFloat += (d, t, k) => { if (k == "big") Play("pop", 1.2f, .35f); else if (t.Contains("Kristall") || t.Contains("kristall")) Play("crystal", 1, .4f); };
            Game.OnRocket += (p, v) => { Play("rocket", 1, .55f); Play("cash", 1, .25f); Buzz(); };
            Game.OnBoom += d => { PlayAt("boom", Game.Cur.pd.Surf(d, 1), 1, .7f, 140); Buzz(); };
            Game.OnFind += (n, d, c) => { Play("find", 1, .5f); Buzz(); };
            Game.OnToast += (t, k) => { if (k == "bad") Play("bad", 1, .25f); else if (t.StartsWith("Entdeckt")) Play("discover", 1, .5f); else if (t.StartsWith("Erfolg")) Play("medal", 1, .55f); };
            Game.OnCellFlash += id => Chain();
        }

        /// <summary>Parzelle fertig: Glocke; mehrere kurz nacheinander steigen wie eine Tonleiter.</summary>
        void Chain() { if (Time.time - chainT > 3.5f) chainN = 0; chainT = Time.time; PlayI("chime", Penta[Mathf.Min(chainN, Penta.Length - 1)] * .75f, .42f); chainN++; }

        /// <summary>Kurze Vibration am Handy (Raketenstart, Fund, Explosion) – in den Einstellungen abschaltbar.</summary>
        public static void Buzz()
        {
            if (Game.S == null || !Game.S.vibrate || Game.Offline) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
        /// <summary>Wischen an/aus; speed = Tempo in Parzellenradien je Sekunde (macht den Klang heller), dirt = es wurde gerade Schmutz entfernt.</summary>
        public static void Wipe(bool on, float speed = 0, bool dirt = false) { if (!I) return; I.loopGoal = on ? (dirt ? .3f : .12f) : 0; I.wipeSpd = Mathf.Lerp(I.wipeSpd, speed, .2f); if (on && dirt) { I.crackT -= Time.deltaTime; if (I.crackT <= 0) { I.crackT = .12f + Random.value * .25f; I.PlayI("crackle", .8f + Random.value * .5f, .16f); } } }
        public static void Play(string key, float pitch = 1, float vol = .5f) { if (I) I.PlayI(key, pitch, vol); }
        /// <summary>Ton an einer Stelle in der Welt: leiser, je weiter die Kamera weg ist.</summary>
        public static void PlayAt(string key, Vector3 world, float pitch, float vol, float range) { if (!I) return; float d = (world - CamPos).magnitude; float k = Mathf.Clamp01(1 - d / range); if (k > .02f) I.PlayI(key, pitch, vol * k * k); }
        void PlayI(string key, float pitch, float vol)
        {
            if (Game.S == null || !Game.S.sound || Game.Offline) return; if (!clips.TryGetValue(key, out var c)) return;
            var s = one; if (Mathf.Abs(pitch - 1) > .01f) { s = gameObject.AddComponent<AudioSource>(); s.playOnAwake = false; s.pitch = pitch; Destroy(s, c.length / Mathf.Max(.3f, pitch) + .2f); }
            s.PlayOneShot(c, vol * Game.S.volSfx);
        }

        void Update()
        {
            bool snd = Game.S != null && Game.S.sound; float sv = snd ? Game.S.volSfx : 0, dt = Time.unscaledDeltaTime;
            Fade(loop, loopGoal * sv, dt * 2.5f); loop.pitch = Mathf.Lerp(loop.pitch, .85f + Mathf.Clamp01(wipeSpd / 12f) * .5f, dt * 6);
            if (Game.S == null) return; var ps = Game.Cur;
            // Basis-Summen nur nah an der Basis, lauter wenn dort gearbeitet wird
            float near = Mathf.Clamp01(1 - (CamAlt - 18) / 45f), act = ps == null ? 0 : (ps.sv.veh.Count > 0 ? .6f : .25f) + (ps.bld.Contains("presse") ? .4f : 0);
            Fade(hum, .22f * near * act * Mathf.Clamp01(ps?.ef ?? 1) * sv, dt * .8f);
            Fade(wind, (ps != null && Game.StormOn(ps) ? .32f : 0) * sv, dt * .5f);
            // Musik je Planetengruppe, beim Planetenwechsel überblenden
            bool mus = snd && Game.S.music; string want = ps == null ? "warm" : Group(ps.pid);
            if (mus && want != song) { song = want; if (!songs.ContainsKey(want)) songs[want] = Song(want); var nxt = musOnA ? musB : musA; nxt.clip = songs[want]; nxt.volume = 0; nxt.Play(); musOnA = !musOnA; }
            float mg = mus ? (.07f + .05f * Mathf.Clamp01(ps?.ef ?? 1)) * Game.S.volMusic * 1.6f : 0;
            var cur = musOnA ? musA : musB; var old = musOnA ? musB : musA;
            Fade(cur, mg, dt * .12f); Fade(old, 0, dt * .12f);
        }
        static void Fade(AudioSource s, float goal, float step)
        {
            s.volume = Mathf.MoveTowards(s.volume, goal, step);
            if (s.volume > .001f && !s.isPlaying && s.clip) s.Play(); else if (s.volume <= .001f && s.isPlaying) s.Pause();
        }
        static string Group(string pid)
        {
            switch (pid) { case "moon": case "mars": case "venus": case "io": return "warm"; case "europa": case "titan": case "ganymed": case "enceladus": case "triton": case "pluto": case "eris": case "sedna": return "ice"; case "poltergeist": return "eerie"; default: return "alien"; }
        }

        // ================= Klangerzeugung =================
        static AudioClip Make(string name, float[] d) { var c = AudioClip.Create(name, d.Length, 1, SR, false); c.SetData(d, 0); return c; }
        static float[] Loopify(float[] d, int f) { int n = d.Length - f; for (int i = 0; i < f; i++) { float k = i / (float)f; d[i] = d[i] * k + d[n + i] * (1 - k); } System.Array.Resize(ref d, n); return d; } /* Ende weich in den Anfang blenden, dann abschneiden: nahtlose Schleife */
        /// <summary>Glocke: unharmonische Teiltöne mit eigenem Abklingen (klingt nach Metall/Glas statt Piepser).</summary>
        static float[] BellData(float f, float dur, float vol)
        {
            int n = Mathf.CeilToInt(SR * dur); var d = new float[n]; float[] p = { 1, 2.01f, 2.76f, 4.07f, 5.4f }, a = { 1, .45f, .35f, .18f, .1f }, dec = { 3.2f, 5f, 6.5f, 9f, 12f };
            for (int i = 0; i < n; i++) { float t = i / (float)SR, s = 0; for (int k = 0; k < p.Length; k++) s += Mathf.Sin(2 * Mathf.PI * f * p[k] * t) * a[k] * Mathf.Exp(-t * dec[k] / dur); d[i] = s * vol * .45f * Mathf.Clamp01(i / 40f); }
            return d;
        }
        static AudioClip Bell(float f, float dur, float vol) => Make("bell", BellData(f, dur, vol));
        static AudioClip Arp(float[] fs, float step, float dur, float vol)
        {
            int n = Mathf.CeilToInt(SR * (dur + step * fs.Length)); var d = new float[n];
            for (int k = 0; k < fs.Length; k++) { var b = BellData(fs[k], dur, vol * (1 - k * .06f)); int o = Mathf.RoundToInt(step * k * SR); for (int i = 0; i < b.Length && o + i < n; i++) d[o + i] += b[i]; }
            return Make("arp", d);
        }
        static AudioClip Fanfare()
        {
            var a = Arp(new[] { 523f, 659f, 784f }, .1f, 1.4f, .45f); var d = new float[a.samples + SR]; var tmp = new float[a.samples]; a.GetData(tmp, 0); System.Array.Copy(tmp, d, tmp.Length);
            var top = BellData(1047, 1.6f, .5f); int o = Mathf.RoundToInt(.36f * SR); for (int i = 0; i < top.Length && o + i < d.Length; i++) d[o + i] += top[i];
            var r = new System.Random(3); for (int i = o; i < d.Length; i++) { float t = (i - o) / (float)SR; d[i] += ((float)r.NextDouble() * 2 - 1) * .04f * Mathf.Exp(-t * 3) * Mathf.Sin(2 * Mathf.PI * 6000 * t); } // Glitzern
            return Make("medal", d);
        }
        static AudioClip Coins()
        {
            int n = Mathf.CeilToInt(SR * .6f); var d = new float[n];
            foreach (var (f, t0) in new[] { (2637f, 0f), (3520f, .07f), (2960f, .15f) }) { var b = BellData(f, .45f, .4f); int o = Mathf.RoundToInt(t0 * SR); for (int i = 0; i < b.Length && o + i < n; i++) d[o + i] += b[i]; }
            return Make("coins", d);
        }
        static AudioClip Click() { int n = SR / 30; var d = new float[n]; var r = new System.Random(9); float lp = 0; for (int i = 0; i < n; i++) { float t = i / (float)n; lp += .5f * ((float)r.NextDouble() * 2 - 1 - lp); d[i] = (lp * .5f + Mathf.Sin(2 * Mathf.PI * 1800 * i / SR) * .5f) * Mathf.Exp(-t * 9) * .5f; } return Make("click", d); }
        static AudioClip Blip(float[] fs, float dur, float vol)
        {
            int n = Mathf.CeilToInt(SR * dur * fs.Length); var d = new float[n];
            for (int k = 0; k < fs.Length; k++) { int o = Mathf.RoundToInt(dur * k * SR); for (int i = 0; i < dur * SR && o + i < n; i++) { float t = i / (float)SR; d[o + i] += (Mathf.Sin(2 * Mathf.PI * fs[k] * t) + .3f * Mathf.Sin(4 * Mathf.PI * fs[k] * t)) * Mathf.Exp(-t * 14) * Mathf.Clamp01(i / 50f) * vol; } }
            return Make("blip", d);
        }
        /// <summary>Bürste/Sauger: rosa Rauschen durch ein Bandpassfilter, die Borsten flattern leicht (nahtlose Schleife).</summary>
        static AudioClip Brush(float dur)
        {
            int n = Mathf.CeilToInt(SR * dur); var d = new float[n]; var r = new System.Random(5); float b0 = 0, b1 = 0, b2 = 0, lp = 0, hp = 0, prev = 0;
            for (int i = 0; i < n; i++)
            {
                float w = (float)r.NextDouble() * 2 - 1; b0 = .997f * b0 + w * .03f; b1 = .985f * b1 + w * .06f; b2 = .95f * b2 + w * .15f; float pink = b0 + b1 + b2 + w * .05f;
                lp += .22f * (pink - lp); hp = .96f * (hp + lp - prev); prev = lp; float t = i / (float)SR;
                float flut = .75f + .25f * Mathf.Sin(2 * Mathf.PI * 13 * t) * Mathf.Sin(2 * Mathf.PI * 1.5f * t); d[i] = hp * flut * .9f;
            }
            d = Loopify(d, SR / 8); return Make("brush", d);
        }
        static AudioClip Crackle(float dur)
        {
            int n = Mathf.CeilToInt(SR * dur); var d = new float[n]; var r = new System.Random(7);
            for (int k = 0; k < 9; k++) { int o = r.Next(n - 400); float f = 2000 + (float)r.NextDouble() * 3000, a = .2f + (float)r.NextDouble() * .3f; for (int i = 0; i < 300; i++) d[o + i] += Mathf.Sin(2 * Mathf.PI * f * i / SR) * a * Mathf.Exp(-i / 40f) * ((float)r.NextDouble() * .6f + .4f); }
            return Make("crackle", d);
        }
        static AudioClip Rumble(float dur)
        {
            int n = Mathf.CeilToInt(SR * dur); var d = new float[n]; var r = new System.Random(11); float lp = 0, lp2 = 0;
            for (int i = 0; i < n; i++) { float t = i / (float)n, w = (float)r.NextDouble() * 2 - 1, a = Mathf.Lerp(.02f, .09f, t); lp += a * (w - lp); lp2 += .05f * (w - lp2); float env = Mathf.Sin(Mathf.PI * Mathf.Min(1, t * 1.15f)); d[i] = (lp * 2.4f + lp2 * 1.5f + Mathf.Sin(2 * Mathf.PI * 42 * i / SR) * .15f) * env * .6f; }
            return Make("rumble", d);
        }
        static AudioClip Whistle(float dur)
        {
            int n = Mathf.CeilToInt(SR * dur); var d = new float[n]; var r = new System.Random(13); float ph = 0, lp = 0;
            for (int i = 0; i < n; i++) { float t = i / (float)n, f = Mathf.Lerp(2200, 380, t * t); ph += 2 * Mathf.PI * f / SR; lp += .3f * (((float)r.NextDouble() * 2 - 1) - lp); d[i] = (Mathf.Sin(ph) * .35f + lp * .25f) * Mathf.Sin(Mathf.PI * Mathf.Min(1, t * 1.1f)) * .6f; }
            return Make("whistle", d);
        }
        static AudioClip Boom(float dur)
        {
            int n = Mathf.CeilToInt(SR * dur); var d = new float[n]; var r = new System.Random(17); float lp = 0, ph = 0;
            for (int i = 0; i < n; i++) { float t = i / (float)SR, f = Mathf.Lerp(70, 28, Mathf.Clamp01(t * 1.5f)); ph += 2 * Mathf.PI * f / SR; lp += Mathf.Lerp(.5f, .03f, Mathf.Clamp01(t * 3)) * (((float)r.NextDouble() * 2 - 1) - lp); d[i] = (Mathf.Sin(ph) * Mathf.Exp(-t * 3) * .9f + lp * Mathf.Exp(-t * 4) * .8f) * Mathf.Clamp01(i / 30f); }
            return Make("boom", d);
        }
        static AudioClip Hiss(float dur)
        {
            int n = Mathf.CeilToInt(SR * dur); var d = new float[n]; var r = new System.Random(19); float hp = 0, prev = 0, lp = 0;
            for (int i = 0; i < n; i++) { float t = i / (float)n, w = (float)r.NextDouble() * 2 - 1; hp = .9f * (hp + w - prev); prev = w; lp += .4f * (hp - lp); float env = Mathf.Clamp01(t * 6) * Mathf.Pow(1 - t, 1.5f); d[i] = lp * env * .5f + Mathf.Sin(2 * Mathf.PI * 70 * i / SR) * env * .1f; }
            return Make("hiss", d);
        }
        static AudioClip BaseHum(float dur)
        {
            int n = Mathf.CeilToInt(SR * dur); var d = new float[n]; var r = new System.Random(23); float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR; lp += .02f * (((float)r.NextDouble() * 2 - 1) - lp);
                float hum = Mathf.Sin(2 * Mathf.PI * 55 * t) * .35f + Mathf.Sin(2 * Mathf.PI * 110 * t) * .15f + Mathf.Sin(2 * Mathf.PI * 165.5f * t) * .05f; /* ganze Schwingungen in 4 s */
                float beat = t % 1f; float thump = Mathf.Sin(2 * Mathf.PI * 60 * beat) * Mathf.Exp(-beat * 18) * .6f; /* Presse: einmal je Sekunde */
                d[i] = (hum + lp * 1.2f + thump) * .5f;
            }
            d = Loopify(d, SR / 10); return Make("hum", d);
        }
        static AudioClip Wind(float dur)
        {
            int n = Mathf.CeilToInt(SR * dur); var d = new float[n]; var r = new System.Random(29); float lp = 0, lp2 = 0;
            for (int i = 0; i < n; i++) { float t = i / (float)SR, w = (float)r.NextDouble() * 2 - 1, g = .5f + .5f * Mathf.Sin(2 * Mathf.PI * t / dur * 2) * Mathf.Sin(2 * Mathf.PI * t / dur * 3 + 1); lp += (.02f + .05f * g) * (w - lp); lp2 += .3f * (lp - lp2); d[i] = lp2 * (1.2f + g) * .9f; }
            d = Loopify(d, SR / 4); return Make("wind", d);
        }

        /// <summary>Musik: vier Akkorde à 7 s, jeder mit weichem Ein- und Ausblenden, kreisförmig überlappend (nahtlose Schleife von 28 s).</summary>
        static AudioClip Song(string group)
        {
            float[][] chords; float bright, shimmer, detune;
            switch (group)
            {
                case "ice": chords = new[] { new[] { 293.7f, 440f, 554.4f, 659.3f }, new[] { 246.9f, 370f, 493.9f, 587.3f }, new[] { 220f, 329.6f, 440f, 554.4f }, new[] { 261.6f, 392f, 493.9f, 659.3f } }; bright = .5f; shimmer = .35f; detune = 1.004f; break;
                case "alien": chords = new[] { new[] { 196f, 277.2f, 392f, 554.4f }, new[] { 207.7f, 311.1f, 415.3f, 523.3f }, new[] { 185f, 277.2f, 369.9f, 466.2f }, new[] { 220f, 311.1f, 440f, 622.3f } }; bright = .4f; shimmer = .2f; detune = 1.008f; break;
                case "eerie": chords = new[] { new[] { 110f, 130.8f, 155.6f, 233.1f }, new[] { 103.8f, 123.5f, 155.6f, 207.7f }, new[] { 116.5f, 138.6f, 164.8f, 246.9f }, new[] { 98f, 123.5f, 146.8f, 220f } }; bright = .25f; shimmer = .1f; detune = 1.012f; break;
                default: chords = new[] { new[] { 110f, 164.8f, 220f, 277.2f }, new[] { 98f, 146.8f, 196f, 246.9f }, new[] { 87.3f, 130.8f, 174.6f, 220f }, new[] { 98f, 146.8f, 196f, 261.6f } }; bright = .3f; shimmer = .12f; detune = 1.003f; break;
            }
            float seg = 7f; int n = Mathf.CeilToInt(SR * seg * chords.Length); var d = new float[n];
            for (int c = 0; c < chords.Length; c++)
            {
                float start = c * seg - seg * .5f, len = seg * 2; // überlappt die Nachbarn je zur Hälfte
                for (int j = 0; j < (int)(len * SR); j++)
                {
                    float t = j / (float)SR, w = Mathf.Sin(Mathf.PI * t / len); w *= w; int idx = (int)((start * SR + j) % n + n) % n; float s = 0;
                    foreach (var f in chords[c]) { s += Mathf.Sin(2 * Mathf.PI * f * t) + .5f * Mathf.Sin(2 * Mathf.PI * f * detune * t + 1.3f) + bright * .3f * Mathf.Sin(4 * Mathf.PI * f * t); }
                    s += shimmer * Mathf.Sin(2 * Mathf.PI * chords[c][3] * 2 * t) * (.5f + .5f * Mathf.Sin(2 * Mathf.PI * .7f * t));
                    d[idx] += s / chords[c].Length * .16f * w;
                }
            }
            return Make("song " + group, d);
        }
    }
}
