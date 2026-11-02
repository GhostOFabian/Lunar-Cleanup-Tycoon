// Build 29 (Fabian 01.11.): Müllberg, Händlerschiff, Polarlichter beim Sonnensturm, Putzi-7, Riesenmeteor mit Planetenexplosion.
using System.Collections;
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        void UpdateBuild29() { UpdatePile(); UpdateTrader(); UpdateAurora(); UpdatePutzi(); UpdateDoom(); }

        // ---------------- Müllberg ----------------
        GameObject pileGo; int pileCell = -1; string pileFor;
        void UpdatePile()
        {
            int c = Game.EvOn(ps, "pile") ? ps.sv.ev.cell : -1; if (pileFor != ps.pid) { if (pileGo) Destroy(pileGo); pileGo = null; pileCell = -1; pileFor = ps.pid; }
            if (c != pileCell) { if (pileGo) { EmitDust(pd.cells[pileCell].c, new Color(.6f, .55f, .5f), 60); Destroy(pileGo); } pileGo = null; pileCell = c; if (c >= 0) pileGo = MakePile(c); }
            if (pileGo) { pileGo.SetActive(!MapMode); float k = Mathf.Lerp(.18f, 1f, Mathf.Sqrt(Game.PileLeft(ps))); pileGo.transform.localScale = Vector3.Lerp(pileGo.transform.localScale, Vector3.one * k, Time.deltaTime * 1.5f); }
        }
        GameObject MakePile(int c)
        {
            var cl = pd.cells[c]; var d = cl.c.normalized; var g = new GameObject("Müllberg"); g.transform.SetParent(transform, false);
            g.transform.position = pd.Surf(d, -.2f, c); g.transform.rotation = Quaternion.FromToRotation(Vector3.up, pd.SurfNormal(d, c)); var t = g.transform;
            var rnd = new System.Random(c * 7 + 3); float R() => (float)rnd.NextDouble(); var dc = GameData.Dirt[ps.P.start].lightC; float rad = cl.H * .62f;
            var rust = MeshKit.Mat(new Color(.55f, .3f, .16f), .5f, .3f); var steel = MeshKit.Mat(new Color(.55f, .58f, .62f), .8f, .5f); var blue = MeshKit.Mat(new Color(.2f, .35f, .6f), .4f, .4f); var yel = MeshKit.Mat(new Color(.85f, .65f, .15f), .3f, .4f);
            var core = RockKit.Spawn(t, 3, RockKit.Tint(dc * .75f), rad * 1.6f); core.transform.localPosition = new Vector3(0, -rad * .25f, 0); core.transform.localScale = new Vector3(rad * 1.9f, rad * 1.1f, rad * 1.7f); // Haufen aus Staub und Geröll
            for (int i = 0; i < 46; i++)
            {
                float a = R() * 6.283f, rr = Mathf.Sqrt(R()) * rad, h = Mathf.Max(0, rad * .75f * (1 - rr * rr / (rad * rad))) + R() * .3f; var p = new Vector3(Mathf.Cos(a) * rr, h, Mathf.Sin(a) * rr);
                var rot = new Vector3(R() * 360, R() * 360, R() * 360); int k = rnd.Next(5);
                if (k == 0) MeshKit.Cyl(t, R() < .5f ? blue : yel, p, .35f, .35f, .9f, 10, rot); // Fässer
                else if (k == 1) MeshKit.Box(t, R() < .5f ? rust : steel, p, new Vector3(.6f + R() * 1.4f, .12f + R() * .3f, .4f + R()), rot); // Platten, Schrott
                else if (k == 2) MeshKit.Cyl(t, steel, p, .08f, .08f, 1.2f + R() * 2, 8, rot); // Rohre
                else { var rk = RockKit.Spawn(t, rnd.Next(RockKit.Variants), RockKit.Tint(dc * .8f), .4f + R() * .9f); rk.transform.localPosition = p; rk.transform.localRotation = Quaternion.Euler(rot); }
            }
            var fl = MeshKit.Cyl(t, steel, new Vector3(0, rad * .95f, 0), .04f, .04f, 2.2f, 6); MeshKit.Box(t, MeshKit.Mat(new Color(1f, .35f, .1f), 0, .3f, new Color(1f, .3f, .05f) * .6f), new Vector3(.45f, rad * .95f + .8f, 0), new Vector3(.8f, .5f, .03f)); // Warnfähnchen
            return g;
        }

        // ---------------- Händlerschiff ----------------
        GameObject traderGo; Vector3 traderAt; Material traderBeam;
        void UpdateTrader()
        {
            bool on = Game.EvOn(ps, "trader") && !MapMode;
            if (on && !traderGo)
            {
                var bc = pd.cells[pd.baseCell]; traderAt = (bc.c * pd.R + (bc.e1 * .7f - bc.e2 * .7f).normalized * 22f).normalized;
                traderGo = new GameObject("Händlerschiff"); traderGo.transform.SetParent(transform, false); var t = traderGo.transform;
                var up = pd.SurfNormal(traderAt); t.position = pd.Surf(traderAt, 9); t.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(bc.e2, up).normalized, up);
                var m = LcKit.Has("Ereignisse/frachter") ? LcKit.Spawn("Ereignisse/frachter", t, Vector3.zero, 180, GameData.Hex("#7fe0ff"), null, .9f) : null;
                if (!m) { MeshKit.Box(t, MeshKit.Mat(new Color(.85f, .88f, .92f), .6f, .6f), Vector3.zero, new Vector3(4, 1.4f, 7)); MeshKit.Box(t, MeshKit.Mat(new Color(.2f, .7f, .9f), .3f, .6f), new Vector3(0, .9f, 1.5f), new Vector3(2, .7f, 2.4f)); }
                traderBeam = new Material(MeshKit.Mat(new Color(.4f, .95f, 1f), 0, .2f, new Color(.4f, .95f, 1f) * 1.2f)); MeshKit.MakeTransparent(traderBeam, true); traderBeam.SetColor("_BaseColor", new Color(.4f, .95f, 1f, .08f));
                var beam = MeshKit.Cyl(t, traderBeam, new Vector3(0, -4.5f, 0), 1.2f, 2.6f, 9, 18); NoShadow(beam);
                var L = new GameObject("Licht").AddComponent<Light>(); L.transform.SetParent(t, false); L.transform.localPosition = new Vector3(0, -2, 0); L.type = LightType.Point; L.color = new Color(.5f, .9f, 1f); L.range = 18; L.intensity = 3; L.shadows = LightShadows.None;
                Sfx.PlayAt("rocket", t.position, .6f, .5f, 120);
            }
            if (!on && traderGo) { Destroy(traderGo); traderGo = null; }
            if (traderGo) { var up = traderAt; traderGo.transform.position = pd.Surf(traderAt, 9 + Mathf.Sin(Time.time * 1.3f) * .4f); if (traderBeam) traderBeam.SetColor("_BaseColor", new Color(.4f, .95f, 1f, .06f + .03f * Mathf.Sin(Time.time * 4))); }
        }

        // ---------------- Polarlichter beim Sonnensturm ----------------
        float solarCrackle;
        /// <summary>Beim Sonnensturm ziehen die (schon vorhandenen) Nordlichter dauerhaft über den Himmel, dazu knistert der Funk.</summary>
        void UpdateAurora()
        {
            if (!Game.EvOn(ps, "solar") || MapMode) return;
            if (auroraOn < 12 || auroraOn > 47) auroraOn = 30; /* bleibt im voll sichtbaren Bereich, solange der Sturm tobt */
            solarCrackle -= Time.deltaTime; if (solarCrackle <= 0) { solarCrackle = 3 + Random.value * 5; Sfx.Play("crackle", .6f + Random.value * .5f, .25f); }
        }

        // ---------------- Putzi-7 ----------------
        GameObject putziGo; Vector3 putziD, putziTo; float putziWait, putziSay; Transform putziBrushA, putziBrushB; Material putziEye; string putziFor;
        public bool PutziScreen(Camera cam, out Vector3 sp) { sp = Vector3.zero; if (!putziGo || !putziGo.activeInHierarchy) return false; sp = cam.WorldToScreenPoint(putziGo.transform.position + putziGo.transform.up * .6f); return sp.z > 0; }
        static readonly string[] PutziLines = { "PUTZI-7 AKTIV. Laune: hervorragend.", "Ich habe einen Stein mit Gesicht gesehen. Er grüßt.", "Staub erkannt. Staub wird entfernt.", "Endlich Kollegen!", "Bitte nicht wieder dreckig machen.", "Akku 100 %. Danke für die Solarpanels!", "Ich putze. Also bin ich.", "Hast du den Riesenmeteor gesehen? Ich auch nicht. Zum Glück." };
        public void PutziTalk() { Game.Toast("Putzi-7: „" + PutziLines[Random.Range(0, PutziLines.Length)] + "“", "good"); Sfx.Play("pop", 1.6f, .5f); putziWait = 1.5f; }
        void UpdatePutzi()
        {
            bool on = Game.S.putzi && !MapMode && ps.nClean >= 3; if (putziFor != ps.pid) { if (putziGo) Destroy(putziGo); putziGo = null; putziFor = ps.pid; }
            if (!on) { if (putziGo) putziGo.SetActive(false); return; }
            if (!putziGo) MakePutzi(); putziGo.SetActive(true); float dt = Time.deltaTime;
            if (putziWait > 0) putziWait -= dt;
            else
            {
                float step = 1.8f * dt; var to = putziTo - putziD; if (pd.Arc(putziD, putziTo) < .6f) { putziWait = 2 + Random.value * 4; PickPutziTarget(); }
                else putziD = Vector3.Slerp(putziD, putziTo, Mathf.Min(1, step / Mathf.Max(.01f, pd.Arc(putziD, putziTo)))).normalized;
                if (Random.value < dt * 3) EmitDust(putziD, GameData.Dirt[ps.P.start].lightC, 2);
            }
            int cell = pd.CellOf(putziD); var up = pd.SurfNormal(putziD, cell); var t = putziGo.transform; t.position = pd.Surf(putziD, 0, cell);
            var fwd = Vector3.ProjectOnPlane(putziTo - putziD, up); if (fwd.sqrMagnitude > 1e-6f) t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(fwd.normalized, up), dt * 4);
            float spin = putziWait > 0 ? 90 : 720; if (putziBrushA) putziBrushA.Rotate(Vector3.up, spin * dt, Space.Self); if (putziBrushB) putziBrushB.Rotate(Vector3.up, -spin * dt, Space.Self);
            if (putziEye) putziEye.SetColor("_EmissionColor", new Color(.4f, 1f, .85f) * (1.5f + Mathf.Sin(Time.time * 3) * .6f));
            putziSay -= dt; if (putziSay <= 0) { putziSay = 60 + Random.value * 90; var cam = Camera.main; if (cam && (cam.transform.position - t.position).magnitude < 60) Game.Toast("Putzi-7: „" + PutziLines[Random.Range(0, PutziLines.Length)] + "“", ""); }
        }
        void PickPutziTarget()
        {
            for (int k = 0; k < 30; k++) { int i = Random.Range(0, pd.N); var c = pd.cells[i]; if (!ps.clean[i] || c.Wet || c.IsBase || c.ring > 4) continue; putziTo = pd.ToDir(c, Random.Range(-c.H, c.H) * .5f, Random.Range(-c.H, c.H) * .5f); return; }
            var bc = pd.cells[pd.baseCell]; putziTo = (bc.c * pd.R + bc.e1 * Random.Range(-14f, 14f) + bc.e2 * Random.Range(-14f, 14f)).normalized;
        }
        void MakePutzi()
        {
            putziGo = new GameObject("Putzi-7"); putziGo.transform.SetParent(transform, false); var t = putziGo.transform; t.localScale = Vector3.one * 1.1f;
            var white = MeshKit.Mat(new Color(.92f, .93f, .9f), .2f, .6f); var dark = MeshKit.Mat(new Color(.18f, .2f, .24f), .4f, .4f); var orange = MeshKit.Mat(new Color(1f, .55f, .15f), .2f, .5f);
            putziEye = new Material(MeshKit.Mat(new Color(.4f, 1f, .85f), 0, .8f, new Color(.4f, 1f, .85f) * 1.5f));
            MeshKit.Cyl(t, white, new Vector3(0, .32f, 0), .34f, .38f, .36f, 18); // Körper
            MeshKit.Cyl(t, orange, new Vector3(0, .52f, 0), .36f, .36f, .05f, 18); // Zierring
            MeshKit.Ball(t, white, new Vector3(0, .62f, 0), .24f); // Kopfkuppel
            MeshKit.Box(t, dark, new Vector3(0, .66f, .2f), new Vector3(.3f, .1f, .06f)); var eye = MeshKit.Box(t, putziEye, new Vector3(0, .66f, .235f), new Vector3(.24f, .05f, .02f)); NoShadow(eye);
            MeshKit.Cyl(t, dark, new Vector3(.08f, .95f, 0), .012f, .012f, .3f, 6); var tip = MeshKit.Ball(t, orange, new Vector3(.08f, 1.12f, 0), .04f); tip.AddComponent<Blink>(); // Antenne
            for (int s = -1; s <= 1; s += 2) MeshKit.Cyl(t, dark, new Vector3(s * .36f, .14f, 0), .12f, .12f, .1f, 12, new Vector3(0, 0, 90)); // Räder
            var a = new GameObject("Bürste A").transform; a.SetParent(t, false); a.localPosition = new Vector3(-.2f, .03f, .32f); MeshKit.Cyl(a, orange, Vector3.zero, .14f, .14f, .03f, 10); for (int k = 0; k < 4; k++) MeshKit.Box(a, dark, Vector3.zero, new Vector3(.3f, .02f, .03f), new Vector3(0, k * 45, 0)); putziBrushA = a;
            var b = new GameObject("Bürste B").transform; b.SetParent(t, false); b.localPosition = new Vector3(.2f, .03f, .32f); MeshKit.Cyl(b, orange, Vector3.zero, .14f, .14f, .03f, 10); for (int k = 0; k < 4; k++) MeshKit.Box(b, dark, Vector3.zero, new Vector3(.3f, .02f, .03f), new Vector3(0, k * 45, 0)); putziBrushB = b;
            var bc = pd.cells[pd.baseCell]; putziD = (bc.c * pd.R + bc.e1 * 9).normalized; PickPutziTarget(); putziSay = 8;
        }

        // ---------------- Riesenmeteor ----------------
        GameObject doomGo; Vector3 doomFrom, doomTo; bool doomWhistle; float doomBeep;
        void UpdateDoom()
        {
            bool on = Game.DoomActive && Game.S.doomPid == ps.pid;
            if (!on) { if (doomGo) Destroy(doomGo); doomGo = null; doomWhistle = false; return; }
            if (!doomGo)
            {
                var bc = pd.cells[pd.baseCell]; doomTo = pd.Surf(bc.c.normalized, 0); doomFrom = (bc.e1 * .95f + bc.e2 * .2f + bc.c.normalized * .35f).normalized * pd.R * 7; /* kommt von der Seite – so sieht man ihn heranrasen */
                doomGo = new GameObject("Riesenmeteor"); doomGo.transform.SetParent(transform, false); var t = doomGo.transform;
                var rk = RockKit.Spawn(t, 5, new Color(.25f, .2f, .18f), 1); rk.transform.localScale = Vector3.one * 1;
                var glow = MeshKit.Part(t, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(1f, .55f, .2f, 1)), Vector3.zero, Vector3.one * 3.2f); NoShadow(glow); glow.AddComponent<Billboard>();
                var tr = doomGo.AddComponent<TrailRenderer>(); tr.time = 2.5f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 1.2f), new Keyframe(1, 0)); tr.widthMultiplier = 1; tr.material = GlowMat(MeshKit.GlowTex(), new Color(1f, .6f, .25f, 1)); tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var L = new GameObject("Glut").AddComponent<Light>(); L.transform.SetParent(t, false); L.type = LightType.Point; L.color = new Color(1f, .5f, .2f); L.range = 200; L.intensity = 2; L.shadows = LightShadows.None;
            }
            float k = Mathf.Clamp01(1 - Game.S.doomT / Game.DOOM_COUNT), e = k * k * k; float size = Mathf.Lerp(4, 22, k);
            doomGo.transform.position = Vector3.Lerp(doomFrom, doomTo + (doomFrom - doomTo).normalized * (size * .5f + pd.R * .9f), e); /* Fabian 02.11.: endet über der Basis – dort entscheidet Schild/Abfangen, sonst stürzt er (DoomDive) */ doomGo.transform.localScale = Vector3.one * size;
            doomGo.transform.Rotate(new Vector3(.3f, 1, .2f), 40 * Time.deltaTime);
            var tr2 = doomGo.GetComponent<TrailRenderer>(); if (tr2) tr2.widthMultiplier = size * .8f;
            Shake = Mathf.Max(Shake, k * .35f);
            doomBeep -= Time.deltaTime; if (doomBeep <= 0) { doomBeep = Game.S.doomT > 10 ? 5 : 1; Sfx.Play("bad", .7f, .55f); }
            if (!doomWhistle && Game.S.doomT < 5) { doomWhistle = true; Sfx.Play("whistle", .5f, .9f); }
        }
        /// <summary>Der Planet zerbricht: Bodenabschnitte, Gebäude und alles andere fliegen auseinander. Danach baut GameRoot ihn neu auf.</summary>
        public IEnumerator Explode(Transform planetT)
        {
            var at = doomTo != Vector3.zero ? doomTo : pd.Surf(pd.cells[pd.baseCell].c, 0);
            if (doomGo) { Destroy(doomGo); doomGo = null; }
            for (int i = 0; i < 4; i++) { Sfx.Play("boom", .45f + i * .12f, 1); }
            Flash(at, new Color(1f, .85f, .6f)); Shake = 2.5f;
            // Druckwelle: helle Kugel, die über den Planeten wächst
            var wm = new Material(MeshKit.Mat(new Color(1f, .7f, .4f), 0, .2f, new Color(1f, .6f, .3f) * 2)); MeshKit.MakeTransparent(wm, true);
            var wave = MeshKit.Part(transform, MeshKit.Sphere, wm, at, Vector3.one); NoShadow(wave);
            var parts = new List<(Transform t, Vector3 v, Vector3 w)>();
            void Collect(Transform root) { foreach (Transform c in root) { var dir = (c.position.sqrMagnitude > 1 ? c.position.normalized : Random.onUnitSphere); var away = (dir * .7f + (c.position - at).normalized * .3f).normalized; parts.Add((c, away * Random.Range(20f, 75f) + Random.insideUnitSphere * 10, Random.insideUnitSphere * 90)); } }
            if (planetT) Collect(planetT); Collect(transform);
            parts.RemoveAll(p => p.t == wave.transform);
            float T = 0;
            while (T < 4.5f)
            {
                float dt = Time.deltaTime; T += dt;
                float ws = Mathf.Lerp(1, pd.R * 4.5f, Mathf.Sqrt(Mathf.Clamp01(T / 2.2f))); wave.transform.localScale = Vector3.one * ws; wm.SetColor("_BaseColor", new Color(1f, .7f, .4f, Mathf.Clamp01(.55f - T * .2f)));
                if (T > .35f) foreach (var p in parts) { if (!p.t) continue; p.t.position += p.v * dt * Mathf.Clamp01((T - .35f) * 2); p.t.Rotate(p.w * dt * .3f, Space.World); }
                if (dust && Random.value < .8f) for (int q = 0; q < 12; q++) { var d = Random.onUnitSphere; dust.Emit(new ParticleSystem.EmitParams { position = d * pd.R * Random.Range(.8f, 1.1f), velocity = d * Random.Range(10f, 60f), startColor = Color.Lerp(new Color(1f, .45f, .15f), new Color(.4f, .38f, .36f), Random.value), startSize = Random.Range(1.5f, 6f), startLifetime = Random.Range(1.5f, 3.5f) }, 1); }
                Shake = Mathf.Max(Shake, 1.2f - T * .2f);
                yield return null;
            }
            Destroy(wave);
        }
    }
}
