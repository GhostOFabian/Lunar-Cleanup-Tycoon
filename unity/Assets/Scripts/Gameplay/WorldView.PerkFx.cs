// Fabian 03.11.: Perks sollen man sehen – jeder Perk hat seinen eigenen Effekt:
// Bombe: Feuerball + Druckwelle, die Parzellen springen nacheinander sauber. Nuke: greller Blitz, Pilz-Säule, riesige Druckwelle, starkes Beben.
// Kärcher: Wasserstrahl, der die Schneise Parzelle für Parzelle entlangläuft. Schaumkanone: weiße Schaumblasen auf allen eingeweichten Parzellen.
// Tornado: Staubwirbel beim Start. Buffs: farbiger Ring und Funken an der Kamera, dazu Banner und Leuchtrand im Bild (Hud).
using System.Collections;
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        static Texture2D fxRingTex, blobTex;
        static Texture2D FxRingTex()
        {
            if (fxRingTex) return fxRingTex; int n = 128; fxRingTex = new Texture2D(n, n, TextureFormat.RGBA32, false); fxRingTex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float r = new Vector2(x - n / 2f + .5f, y - n / 2f + .5f).magnitude / (n / 2f); float a = Mathf.Clamp01(1 - Mathf.Abs(r - .82f) / .16f); a = a * a; a += Mathf.Clamp01(1 - r) * .12f; fxRingTex.SetPixel(x, y, new Color(1, 1, 1, r > 1 ? 0 : a)); }
            fxRingTex.Apply(); return fxRingTex;
        }
        static Texture2D BlobTex()
        {
            if (blobTex) return blobTex; int n = 64; blobTex = new Texture2D(n, n, TextureFormat.RGBA32, false); blobTex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) { float r = new Vector2(x - n / 2f + .5f, y - n / 2f + .5f).magnitude / (n / 2f); float a = Mathf.Clamp01(1 - r); blobTex.SetPixel(x, y, new Color(1, 1, 1, a * a)); }
            blobTex.Apply(); return blobTex;
        }
        public static Color PerkColor(string id)
        {
            switch (id)
            {
                case "bomb": return new Color(1f, .62f, .25f); case "nuke": return new Color(1f, .95f, .7f); case "kaercher": return new Color(.35f, .75f, 1f); case "foam": return new Color(.95f, .97f, 1f);
                case "tornado": return new Color(.85f, .78f, .6f); case "turbo": return new Color(1f, .45f, .3f); case "wide": return new Color(.43f, .95f, .82f); case "magnet": return new Color(.7f, .55f, 1f);
                case "espresso": return new Color(.75f, .5f, .3f); case "goldmop": return new Color(1f, .8f, .3f); case "rohrpost": return new Color(.5f, .9f, 1f);
            }
            return Color.white;
        }
        float CellSize(Vector3 d) { int c = pd.CellOf(d); if (c < 0) return 10; float s = 0; int n = 0; foreach (int b in pd.cells[c].nb) { s += pd.Arc(pd.cells[b].c, pd.cells[c].c); n++; } return n > 0 ? s / n : 10; }

        public void PerkFx(string id, Vector3 d, List<int> cells) { if (pd == null || MapMode) return; StartCoroutine(PerkFxCo(id, d, cells)); }
        IEnumerator PerkFxCo(string id, Vector3 d, List<int> cells)
        {
            var P = Game.Perk(id); var col = PerkColor(id); float cs = CellSize(d);
            if (P != null && P.kind == 1) { LunarCleanup.UI.Hud.I?.BuffPop(P.name, col, P.dur); Sfx.Play("pop", 1.4f, .8f); StartCoroutine(Shock(d, cs * 1.6f, col, .9f, .4f)); EmitDust(d, col, 60); yield break; }
            yield return new WaitForSeconds(.25f); /* Kamera fliegt kurz hin */
            switch (id)
            {
                case "bomb":
                    Sfx.Play("boom", 1.1f, .8f); Shake = Mathf.Max(Shake, .9f);
                    StartCoroutine(Fireball(d, cs * .55f, col, .8f)); StartCoroutine(Shock(d, cs * 2.6f, col, .9f, .55f));
                    if (cells != null) foreach (int c in cells) { var cd = pd.cells[c].c.normalized; EmitDust(cd, new Color(1, .54f, .23f), 25); EmitDust(cd, new Color(.4f, .4f, .4f), 20); FlashCell(c); yield return new WaitForSeconds(.07f); }
                    break;
                case "nuke":
                    Sfx.Play("boom", .55f, 1f); Shake = Mathf.Max(Shake, 2.4f); StartCoroutine(ScreenFlash());
                    StartCoroutine(Fireball(d, cs * 1.4f, col, 1.6f)); StartCoroutine(Column(d, cs * .5f, cs * 3.5f, new Color(1f, .75f, .4f), 2.2f));
                    StartCoroutine(Shock(d, cs * 7f, new Color(1f, .9f, .7f), 1.6f, .8f)); StartCoroutine(Shock(d, cs * 5f, new Color(1f, .55f, .2f), 1.2f, .6f));
                    if (cells != null) { int k = 0; foreach (int c in cells) { var cd = pd.cells[c].c.normalized; EmitDust(cd, new Color(1, .6f, .3f), 18); FlashCell(c); if (++k % 4 == 0) yield return new WaitForSeconds(.03f); } }
                    break;
                case "kaercher":
                    Sfx.Play("geyser", 1f, .8f);
                    if (cells != null) foreach (int c in cells) { var cd = pd.cells[c].c.normalized; StartCoroutine(Shock(cd, cs * .9f, col, .45f, .5f)); EmitDust(cd, new Color(.6f, .85f, 1f), 45); EmitDust(cd, Color.white, 20); FlashCell(c); Sfx.Play("pop", 1.6f, .35f); yield return new WaitForSeconds(.12f); }
                    break;
                case "foam":
                    Sfx.Play("pop", .8f, .8f); StartCoroutine(Shock(d, cs * 3.2f, col, .8f, .45f));
                    if (cells != null) foreach (int c in cells) { StartCoroutine(Foam(pd.cells[c].c.normalized, cs * .35f)); yield return new WaitForSeconds(.04f); }
                    break;
                case "pylon":
                    Sfx.Play("buy", 1f, .7f); StartCoroutine(Shock(d, Game.SUIT_NEAR, new Color(.43f, .95f, .82f), 1.2f, .5f)); EmitDust(d, new Color(.6f, 1f, .9f), 40);
                    break;
                case "wave":
                    Sfx.Play("bad", .7f, .8f); StartCoroutine(Shock(d, cs * 2.2f, new Color(.55f, .4f, .25f), 1.1f, .6f));
                    if (cells != null) foreach (int c in cells) { var cd = pd.cells[c].c.normalized; EmitDust(cd, new Color(.45f, .35f, .25f), 60); yield return new WaitForSeconds(.15f); }
                    break;
                case "tornado":
                    Sfx.Play("geyser", .7f, 1f); StartCoroutine(Shock(d, cs * 1.4f, col, .7f, .5f)); for (int i = 0; i < 4; i++) { EmitDust(d, col, 50); yield return new WaitForSeconds(.1f); }
                    break;
            }
        }
        GameObject FxQuad(Vector3 d, float lift, Texture tex, Color c, out Material m)
        {
            m = GlowMat(tex, c); var g = MeshKit.Part(transform, MeshKit.Quad, m, Vector3.zero, Vector3.one); NoShadow(g);
            g.transform.position = pd.Surf(d, lift); g.transform.rotation = Quaternion.FromToRotation(Vector3.forward, d) ; return g;
        }
        /// <summary>Druckwelle: flacher Leuchtring, wächst und blendet aus.</summary>
        IEnumerator Shock(Vector3 d, float r, Color c, float dur, float a0)
        {
            var g = FxQuad(d, .4f, FxRingTex(), c, out var m); float t = 0;
            while (t < dur) { t += Time.deltaTime; float e = t / dur, k = 1 - (1 - e) * (1 - e); g.transform.localScale = Vector3.one * Mathf.Max(.01f, r * 2 * k); m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, a0 * (1 - e))); yield return null; }
            Destroy(g); Destroy(m);
        }
        /// <summary>Feuerball: leuchtende Kugel, bläht sich auf und verglüht.</summary>
        IEnumerator Fireball(Vector3 d, float r, Color c, float dur)
        {
            var m = GlowMat(BlobTex(), c); var g = MeshKit.Part(transform, MeshKit.Sphere, m, Vector3.zero, Vector3.one); NoShadow(g); float t = 0;
            while (t < dur) { t += Time.deltaTime; float e = t / dur; g.transform.position = pd.Surf(d, r * (.3f + e)); g.transform.localScale = Vector3.one * r * 2 * (.3f + .9f * Mathf.Sqrt(e)); m.SetColor("_BaseColor", new Color(c.r, c.g * (1 - .4f * e), c.b * (1 - .6f * e), .85f * (1 - e))); yield return null; }
            Destroy(g); Destroy(m);
        }
        /// <summary>Pilz-Säule der Nuke: steigt auf und wird durchsichtig.</summary>
        IEnumerator Column(Vector3 d, float r, float h, Color c, float dur)
        {
            var m = GlowMat(BlobTex(), c); var g = MeshKit.Part(transform, MeshKit.Sphere, m, Vector3.zero, Vector3.one); NoShadow(g);
            var mh = GlowMat(BlobTex(), c); var hd = MeshKit.Part(transform, MeshKit.Sphere, mh, Vector3.zero, Vector3.one); NoShadow(hd); float t = 0; var rot = Quaternion.FromToRotation(Vector3.up, d);
            while (t < dur) { t += Time.deltaTime; float e = t / dur, hh = h * Mathf.Sqrt(Mathf.Min(1, e * 1.6f)); float a = .7f * (1 - e);
                g.transform.position = pd.Surf(d, hh * .5f); g.transform.rotation = rot; g.transform.localScale = new Vector3(r, Mathf.Max(.1f, hh), r);
                hd.transform.position = pd.Surf(d, hh); hd.transform.rotation = rot; hd.transform.localScale = new Vector3(r * 3.2f, r * 1.6f, r * 3.2f) * (.6f + .6f * e);
                m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, a)); mh.SetColor("_BaseColor", new Color(c.r, c.g * .85f, c.b * .7f, a)); yield return null; }
            Destroy(g); Destroy(m); Destroy(hd); Destroy(mh);
        }
        /// <summary>Schaumberg auf einer Parzelle: weiße Blasen ploppen auf, bleiben kurz und vergehen.</summary>
        IEnumerator Foam(Vector3 d, float r)
        {
            var m = GlowMat(BlobTex(), new Color(1, 1, 1, .9f)); var bl = new List<(Transform t, Vector3 p, float s)>(); var rot = Quaternion.FromToRotation(Vector3.up, d);
            var e1 = Vector3.Cross(d, Vector3.up); if (e1.sqrMagnitude < .01f) e1 = Vector3.Cross(d, Vector3.right); e1.Normalize(); var e2 = Vector3.Cross(d, e1);
            for (int i = 0; i < 9; i++) { var g = MeshKit.Part(transform, MeshKit.Sphere, m, Vector3.zero, Vector3.one); NoShadow(g); var off = (e1 * Random.Range(-1f, 1f) + e2 * Random.Range(-1f, 1f)) * r; bl.Add((g.transform, pd.Surf((pd.Surf(d, 0) + off).normalized, 0), Random.Range(.5f, 1.1f) * r * .6f)); }
            float t = 0, dur = 3f;
            while (t < dur) { t += Time.deltaTime; float grow = Mathf.Clamp01(t / .35f), fade = Mathf.Clamp01((dur - t) / .8f);
                foreach (var b in bl) { b.t.position = b.p + d * b.s * .4f; b.t.rotation = rot; b.t.localScale = Vector3.one * b.s * grow * (.85f + .15f * fade) * (1 + .05f * Mathf.Sin(t * 6 + b.s * 10)); }
                m.SetColor("_BaseColor", new Color(1, 1, 1, .85f * fade)); yield return null; }
            foreach (var b in bl) Destroy(b.t.gameObject); Destroy(m);
        }
        /* Markierung der kommenden Schmutzwelle: pulsierender brauner Ring über der Parzelle */
        GameObject waveMark; Material waveMat; int waveFor = -2;
        public void UpdateWaveMark()
        {
            int wc = Game.Hard && ps != null ? Game.WaveCell(ps) : -1;
            if (wc != waveFor) { if (waveMark) Destroy(waveMark); if (waveMat) Destroy(waveMat); waveMark = null; waveFor = wc; if (wc >= 0) { var d = pd.cells[wc].c.normalized; waveMark = FxQuad(d, .5f, FxRingTex(), new Color(.75f, .5f, .3f), out waveMat); } }
            if (waveMark) { float k = .5f + .5f * Mathf.Sin(Time.time * 4); waveMark.transform.localScale = Vector3.one * CellSize(pd.cells[wc].c.normalized) * (1.4f + .25f * k); waveMat.SetColor("_BaseColor", new Color(.8f, .5f, .25f, .35f + .35f * k)); }
        }
        /// <summary>Fund: kleine leuchtende Kiste springt aus der Parzelle (weiß = häufig, blau = selten, gold = sehr selten).</summary>
        public void DropFx(Vector3 d, int tier) { if (pd == null || MapMode) return; StartCoroutine(DropCo(d.normalized, tier)); }
        IEnumerator DropCo(Vector3 d, int tier)
        {
            var c = tier == 2 ? new Color(1f, .8f, .3f) : tier == 1 ? new Color(.4f, .7f, 1f) : new Color(.9f, .95f, 1f);
            Sfx.Play(tier >= 1 ? "discover" : "find", 1f, .6f); EmitDust(d, c, tier == 2 ? 70 : 35);
            var box = MeshKit.Part(transform, MeshKit.Cube, MeshKit.Glow(c, tier == 2 ? 3f : 2f), Vector3.zero, Vector3.one); NoShadow(box);
            var m = box.GetComponent<MeshRenderer>().material; var rot = Quaternion.FromToRotation(Vector3.up, d); float t = 0, dur = 1.3f, s0 = tier == 2 ? 1.1f : .8f;
            while (t < dur) { t += Time.deltaTime; float e = t / dur, h = Mathf.Sin(Mathf.Min(1, e * 1.4f) * Mathf.PI) * 3f + e * 2;
                box.transform.position = pd.Surf(d, .5f + h); box.transform.rotation = rot * Quaternion.Euler(e * 400, e * 520, 0); box.transform.localScale = Vector3.one * s0 * (e < .15f ? e / .15f : e > .8f ? (1 - e) / .2f : 1); yield return null; }
            Destroy(box); Destroy(m);
        }
        /* Ladesäulen: schlanker Mast mit leuchtendem Ring, darum ein schwacher Kreis für die Spar-Reichweite */
        readonly List<GameObject> pylonGos = new List<GameObject>(); int pylonVer = -1; string pylonFor;
        public void SyncPylons()
        {
            if (pd == null) return; bool show = Game.Hard; if (pylonFor == ps.pid && pylonVer == ps.baseVersion && (pylonGos.Count > 0) == (show && ps.sv.pylons != null && ps.sv.pylons.Count > 0)) return;
            foreach (var g in pylonGos) if (g) Destroy(g); pylonGos.Clear(); pylonFor = ps.pid; pylonVer = ps.baseVersion; if (!show || ps.sv.pylons == null) return;
            var mastM = MeshKit.Mat(new Color(.82f, .85f, .9f), .3f, .6f); var glowM = MeshKit.Glow(new Color(.43f, .95f, .82f), 2.5f);
            foreach (int c in ps.sv.pylons)
            {
                var d = pd.cells[c].c.normalized; var g = new GameObject("Ladesäule " + c); g.transform.SetParent(transform, false); g.transform.position = pd.Surf(d, 0); g.transform.rotation = Quaternion.FromToRotation(Vector3.up, d);
                MeshKit.Cyl(g.transform, mastM, new Vector3(0, 1.6f, 0), .12f, .2f, 3.2f, 10); var ring = MeshKit.Cyl(g.transform, glowM, new Vector3(0, 3.3f, 0), .45f, .45f, .12f, 16); NoShadow(ring);
                MeshKit.Cyl(g.transform, mastM, new Vector3(0, .1f, 0), .6f, .7f, .2f, 16);
                var area = FxQuad(d, .35f, FxRingTex(), new Color(.43f, .95f, .82f), out var am); am.SetColor("_BaseColor", new Color(.43f, .95f, .82f, .12f)); area.transform.localScale = Vector3.one * Game.SUIT_NEAR * 2; area.transform.SetParent(g.transform, true);
                pylonGos.Add(g);
            }
        }
        IEnumerator ScreenFlash() { LunarCleanup.UI.Hud.I?.Flash(new Color(1, .97f, .85f), .9f); yield break; }
    }
}
