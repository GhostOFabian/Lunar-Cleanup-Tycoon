// Sichtbare Effekte (nur Optik): Raketenflammen, Staub an reinigenden Fahrzeugen, Absturzwrack und Kometenschweif bei Ereignissen,
// Nordlichter, die ab und zu über den Himmel ziehen, und Ampel-Lampen über den Gebäuden.
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        // ---------- gemeinsame Hilfen ----------
        static Material Glow(Color c, float a = 1)
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"); var m = new Material(sh);
            m.SetTexture("_BaseMap", MeshKit.GlowTex()); MeshKit.MakeTransparent(m, true); m.SetFloat("_Cull", 0); m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, a)); return m;
        }
        static Material Emis(Color c, float k) => MeshKit.Mat(c * .3f, 0, .3f, c * k);

        void UpdateFx()
        {
            if (ps == null) return;
            RocketFlame(); VehicleDust(); EventFx(); TeamFx(); Aurora(); Ampeln();
        }

        // ---------- Raketenflammen beim Start und bei der Landung ----------
        GameObject flame; Light flameLight; Transform flameOf;
        void RocketFlame()
        {
            if (!blds.TryGetValue("sell", out var sl) || !sl.rocket) { if (flame) flame.SetActive(false); return; }
            if (!flame || flameOf != sl.rocket)
            {
                if (flame) Destroy(flame); flameOf = sl.rocket;
                flame = new GameObject("Triebwerksfeuer"); flame.transform.SetParent(sl.rocket, false); flame.transform.localPosition = new Vector3(0, -.2f, 0);
                var outer = MeshKit.Cyl(flame.transform, Glow(new Color(1f, .45f, .1f), .9f), new Vector3(0, -1.4f, 0), .55f, .05f, 2.8f, 12); NoShadow(outer);
                var inner = MeshKit.Cyl(flame.transform, Glow(new Color(1f, .9f, .55f), 1f), new Vector3(0, -.9f, 0), .3f, .03f, 1.8f, 10); NoShadow(inner);
                var lg = new GameObject("Licht"); lg.transform.SetParent(flame.transform, false); lg.transform.localPosition = new Vector3(0, -1.5f, 0);
                flameLight = lg.AddComponent<Light>(); flameLight.type = LightType.Point; flameLight.color = new Color(1f, .6f, .25f); flameLight.range = 18; flameLight.shadows = LightShadows.None;
            }
            float h = sl.rocket.localPosition.y - sl.rocketY; bool on = (rocketT >= .2f && rocketT < 4) || (landT > 2f && h > .05f && h < 40);
            flame.SetActive(on && sl.rocket.gameObject.activeInHierarchy);
            if (on)
            {
                float f = 1 + .25f * Mathf.Sin(Time.time * 40) + .15f * Mathf.Sin(Time.time * 23); float big = rocketT >= 0 ? Mathf.Clamp01(rocketT * 2) * 1.3f : Mathf.Clamp01(h / 6f) * .7f + .4f;
                flame.transform.localScale = new Vector3(big, big * f, big); flameLight.intensity = 6 * big;
            }
        }

        // ---------- Staub an Fahrzeugen, die gerade reinigen ----------
        float vdT;
        void VehicleDust()
        {
            if (MapMode || !dust) return; vdT += Time.deltaTime; if (vdT < .07f) return; vdT = 0; int n = 0;
            foreach (var v in ps.sv.veh) if (v.mode == "boom") /* Selbstzerstörung: Funken und Rauch werden immer heftiger */
                {
                    float k = 1 - Mathf.Clamp01(v.repT / 4f); var p0 = pd.Surf(v.d, .6f);
                    for (int q = 0; q < 2 + (int)(k * 6); q++) dust.Emit(new ParticleSystem.EmitParams { position = p0 + Random.insideUnitSphere * .5f, velocity = v.d * Random.Range(1f, 3f) + Random.insideUnitSphere * (2 + 4 * k), startColor = Random.value < .6f ? new Color(1f, .75f, .3f, 1) : new Color(.25f, .25f, .27f, .7f), startSize = Random.Range(.1f, .35f) * (1 + k), startLifetime = Random.Range(.4f, 1f), applyShapeToPosition = false }, 1);
                }
            foreach (var v in ps.sv.veh)
            {
                if (n > 40) break; bool sweeping = v.mode == "sweep" && v.sweepT > 0; if (v.mode != "clean" && !sweeping) continue; if (v.cell < 0 || v.cell >= pd.N) continue; n++;
                var D = GameData.Dirt[pd.cells[v.cell].type]; var col = Color.Lerp(D.baseC, D.lightC, Random.value); col.a = sweeping ? .35f : .7f;
                var side = Random.onUnitSphere * .5f; var pos = pd.Surf(v.d, .25f) + side;
                dust.Emit(new ParticleSystem.EmitParams { position = pos, velocity = v.d * Random.Range(.6f, 1.6f) + Random.insideUnitSphere * .6f, startColor = col, startSize = Random.Range(.12f, .3f), startLifetime = Random.Range(.6f, 1.2f), applyShapeToPosition = false }, sweeping ? 1 : 2);
            }
        }

        // ---------- Ereignisse: Wrack und Komet ----------
        GameObject wreck, comet; int wreckCell = -1; ParticleSystem cometPs;
        void EventFx()
        {
            var ev = ps.sv.ev; bool wOn = ev != null && (ev.key == "wreck" || ev.key == "impact") && ev.t > 0 && ev.cell >= 0 && ev.cell < pd.N;
            if (wOn && (!wreck || wreckCell != ev.cell)) { if (wreck) Destroy(wreck); wreck = ev.key == "impact" ? BuildImpact(ev.cell) : BuildWreck(ev.cell); wreckCell = ev.cell; }
            if (!wOn && wreck) { EmitDust(wreck.transform.position.normalized, new Color(.8f, .75f, .7f), 20); Destroy(wreck); wreck = null; wreckCell = -1; }
            if (wreck) { wreck.SetActive(!MapMode); if (!MapMode && Random.value < .3f) dust.Emit(new ParticleSystem.EmitParams { position = wreck.transform.position + wreck.transform.up * 1.2f, velocity = wreck.transform.up * 1.5f + Random.insideUnitSphere * .3f, startColor = new Color(.35f, .33f, .32f, .5f), startSize = Random.Range(.5f, 1f), startLifetime = 2.5f, applyShapeToPosition = false }, 1); }

            bool cOn = ev != null && ev.key == "comet" && ev.t > 0;
            if (cOn && !comet) comet = BuildComet();
            if (!cOn && comet) { Destroy(comet); comet = null; }
            if (comet)
            {
                comet.SetActive(true); var cam = Camera.main; if (!cam) return;
                var b = pd.cells[pd.baseCell]; float a = Time.time * .02f;
                var dir = (b.c * 1.0f + b.e1 * Mathf.Cos(a) * .7f + b.e2 * Mathf.Sin(a) * .7f).normalized;
                comet.transform.position = dir * (pd.R + 110); var away = (comet.transform.position - Game.SunDir(ps) * 1e4f).normalized;
                comet.transform.rotation = Quaternion.LookRotation(away, dir);
            }
        }
        GameObject BuildWreck(int cell)
        {
            var cl = pd.cells[cell]; var g = new GameObject("Absturzwrack"); g.transform.SetParent(transform, false);
            var d = cl.c; g.transform.position = pd.Surf(d, -.15f); g.transform.rotation = Quaternion.FromToRotation(Vector3.up, pd.SurfNormal(d)) * Quaternion.Euler(0, cell * 37 % 360, 0);
            var hull = MeshKit.Mat(new Color(.62f, .64f, .68f), .6f, .45f); var dark = MeshKit.Mat(new Color(.16f, .15f, .15f), .2f, .2f); var gold = MeshKit.Mat(new Color(.95f, .72f, .25f), .8f, .7f); var panel = MeshKit.Mat(new Color(.12f, .2f, .45f), .5f, .8f);
            var body = MeshKit.Cyl(g.transform, hull, new Vector3(0, .7f, 0), .9f, 1.1f, 2.2f, 12, new Vector3(68, 0, 12)); // umgekippter Rumpf
            MeshKit.Cyl(g.transform, dark, new Vector3(1.3f, .45f, .2f), .5f, .7f, .9f, 10, new Vector3(70, 0, 20));    // verkohltes Triebwerk
            MeshKit.Box(g.transform, gold, new Vector3(-.4f, .9f, .5f), new Vector3(1.2f, .15f, 1.1f), new Vector3(10, 25, 18)); // Goldfolie
            MeshKit.Box(g.transform, panel, new Vector3(-1.8f, .25f, -.6f), new Vector3(2.4f, .06f, 1f), new Vector3(4, 30, -12)); // abgebrochenes Solarpaneel
            MeshKit.Box(g.transform, panel, new Vector3(.4f, .15f, -1.9f), new Vector3(1.4f, .06f, .9f), new Vector3(-8, -50, 6));
            MeshKit.Cyl(g.transform, hull, new Vector3(.9f, 1.4f, -.6f), .02f, .55f, .35f, 10, new Vector3(-30, 0, 40));   // verbogene Antennenschüssel
            for (int i = 0; i < 6; i++) { float an = i * 1.05f + cell; MeshKit.Box(g.transform, i % 2 == 0 ? dark : hull, new Vector3(Mathf.Cos(an) * (2 + i * .3f), .08f, Mathf.Sin(an) * (2 + i * .3f)), new Vector3(.35f, .12f, .25f), new Vector3(0, i * 40, 0)); } // Trümmer
            var crater = MeshKit.Part(g.transform, MeshKit.Quad, Glow(new Color(.05f, .04f, .03f), .6f), new Vector3(0, .05f, 0), new Vector3(5.5f, 5.5f, 1), new Vector3(90, 0, 0)); NoShadow(crater);
            return g;
        }
        GameObject BuildImpact(int cell)
        {
            var cl = pd.cells[cell]; var g = new GameObject("Kometeneinschlag"); g.transform.SetParent(transform, false);
            var d = cl.c; g.transform.position = pd.Surf(d, -.1f); g.transform.rotation = Quaternion.FromToRotation(Vector3.up, pd.SurfNormal(d)) * Quaternion.Euler(0, cell * 53 % 360, 0);
            var scorch = MeshKit.Part(g.transform, MeshKit.Quad, Glow(new Color(.04f, .03f, .03f), .8f), new Vector3(0, .06f, 0), new Vector3(9, 9, 1), new Vector3(90, 0, 0)); NoShadow(scorch);
            var hot = MeshKit.Part(g.transform, MeshKit.Quad, Glow(new Color(1f, .45f, .15f), .55f), new Vector3(0, .08f, 0), new Vector3(3.5f, 3.5f, 1), new Vector3(90, 0, 0)); NoShadow(hot);
            var ice = MeshKit.Mat(new Color(.55f, .8f, 1f), .1f, .9f, new Color(.3f, .7f, 1f) * 1.6f); var rock = MeshKit.Mat(new Color(.18f, .17f, .2f), .2f, .3f);
            MeshKit.Ball(g.transform, rock, new Vector3(0, .35f, 0), 1.4f).transform.localScale = new Vector3(1.6f, 1.1f, 1.3f); // Kometenkern
            for (int i = 0; i < 9; i++) { float an = i * .7f + cell, r = 1.2f + (i % 4) * .7f; var f = MeshKit.Box(g.transform, i % 3 == 0 ? rock : ice, new Vector3(Mathf.Cos(an) * r, .2f, Mathf.Sin(an) * r), new Vector3(.35f, .5f + (i % 3) * .2f, .3f), new Vector3(20 * (i % 3), i * 41, 15)); NoShadow(f); } // leuchtende Eisbrocken
            var lg = new GameObject("Glut"); lg.transform.SetParent(g.transform, false); lg.transform.localPosition = new Vector3(0, 1.5f, 0); var L = lg.AddComponent<Light>(); L.type = LightType.Point; L.color = new Color(.5f, .8f, 1f); L.range = 10; L.intensity = 2.5f; L.shadows = LightShadows.None;
            return g;
        }
        // ---------- Suchtrupp ----------
        GameObject teamGo; Light teamLight;
        void TeamFx()
        {
            var t = ps.sv.team; if (t == null || t.d == Vector3.zero || MapMode) { if (teamGo) teamGo.SetActive(false); return; }
            if (!teamGo)
            {
                var vp = Models.Vehicle(GameData.Veh["buggy"], new Color(1f, .45f, .1f)); teamGo = vp.root; teamGo.name = "Suchtrupp"; teamGo.transform.SetParent(vehRoot, false);
                var mast = MeshKit.Cyl(teamGo.transform, MeshKit.Mat(new Color(.3f, .3f, .32f), .6f, .4f), new Vector3(0, 1.6f, -.3f), .03f, .03f, 1.2f, 6);
                var lamp = MeshKit.Ball(teamGo.transform, MeshKit.Mat(new Color(1f, .5f, .1f), 0, .5f, new Color(1f, .45f, .1f) * 3), new Vector3(0, 2.25f, -.3f), .22f); NoShadow(lamp); // Rundumleuchte
                var lg = new GameObject("Licht"); lg.transform.SetParent(teamGo.transform, false); lg.transform.localPosition = new Vector3(0, 2.3f, -.3f); teamLight = lg.AddComponent<Light>(); teamLight.type = LightType.Point; teamLight.color = new Color(1f, .5f, .15f); teamLight.range = 7; teamLight.shadows = LightShadows.None;
            }
            teamGo.SetActive(true); var prev = teamGo.transform.position; var pos = pd.Surf(t.d, 0); teamGo.transform.position = pos;
            var mv = Vector3.ProjectOnPlane(pos - prev, t.d); if (mv.sqrMagnitude > 1e-5f) teamGo.transform.rotation = Quaternion.Slerp(teamGo.transform.rotation, Quaternion.LookRotation(mv.normalized, t.d), .2f);
            bool busy = t.mode == "go" || t.mode == "salvage" || t.mode == "back"; teamLight.intensity = busy ? 2f + 1.5f * Mathf.Sin(Time.time * 8) : 0;
            if (t.mode == "salvage" && dust && Random.value < .6f) dust.Emit(new ParticleSystem.EmitParams { position = pos + t.d * .8f + Random.insideUnitSphere * .5f, velocity = t.d * 2 + Random.insideUnitSphere * 2.5f, startColor = new Color(1f, .8f, .4f, 1), startSize = .12f, startLifetime = .5f, applyShapeToPosition = false }, 3); // Schweißfunken
        }
        GameObject BuildComet()
        {
            var g = new GameObject("Komet"); g.transform.SetParent(transform, false);
            var head = MeshKit.Ball(g.transform, Emis(new Color(.75f, .9f, 1f), 3f), Vector3.zero, 2.2f); NoShadow(head);
            var halo = MeshKit.Part(g.transform, MeshKit.Quad, Glow(new Color(.6f, .85f, 1f), .9f), Vector3.zero, new Vector3(14, 14, 1), Vector3.zero); NoShadow(halo); halo.AddComponent<Billboard>();
            for (int i = 0; i < 3; i++) { var tail = MeshKit.Cyl(g.transform, Glow(i == 0 ? new Color(.55f, .8f, 1f) : new Color(.8f, .9f, 1f), .5f - i * .12f), new Vector3(0, 0, 22 + i * 10), 1.4f + i * 1.2f, 3.5f + i * 2.5f, 44 + i * 22, 12, new Vector3(90, 0, 0)); NoShadow(tail); }
            return g;
        }

        // ---------- Nordlichter: ziehen alle paar Minuten für eine Weile über den Himmel ----------
        float auroraClock = 90, auroraOn; GameObject auroraRoot; readonly List<(Mesh m, float ph, float h)> auroraBands = new List<(Mesh, float, float)>(); Material auroraMat; Texture2D auroraTex;
        void Aurora()
        {
            auroraClock -= Time.deltaTime;
            if (auroraClock <= 0) { auroraClock = Random.Range(200f, 380f); auroraOn = 55; }
            if (auroraOn > 0) auroraOn -= Time.deltaTime;
            float vis = Mathf.Clamp01(Mathf.Min(auroraOn / 8f, (55 - auroraOn) / 8f));
            if (vis <= 0 || MapMode) { if (auroraRoot) auroraRoot.SetActive(false); return; }
            if (!auroraRoot) BuildAurora();
            auroraRoot.SetActive(true);
            var cam = Camera.main; if (!cam) return; var up = cam.transform.position.normalized;
            var fwd = Vector3.ProjectOnPlane(cam.transform.forward, up).normalized; if (fwd.sqrMagnitude < .1f) fwd = Vector3.ProjectOnPlane(Vector3.forward, up).normalized; var rgt = Vector3.Cross(up, fwd);
            var basePos = cam.transform.position + up * 40 + fwd * 70;
            auroraRoot.transform.position = basePos; auroraRoot.transform.rotation = Quaternion.LookRotation(fwd, up);
            auroraMat.SetColor("_BaseColor", new Color(1, 1, 1, .85f * vis));
            float t = Time.time;
            foreach (var (m, ph, h) in auroraBands)
            {
                var v = m.vertices; int n = v.Length / 2;
                for (int i = 0; i < n; i++) { float x = (i / (float)(n - 1) - .5f) * 260; float z = 25 * Mathf.Sin(x * .018f + t * .25f + ph) + 10 * Mathf.Sin(x * .05f - t * .4f + ph * 2); float hh = h * (.8f + .3f * Mathf.Sin(x * .03f + t * .6f + ph)); v[i * 2] = new Vector3(x, 0, z); v[i * 2 + 1] = new Vector3(x, hh, z + 6); }
                m.vertices = v; m.RecalculateBounds();
            }
        }
        void BuildAurora()
        {
            auroraRoot = new GameObject("Nordlicht"); auroraRoot.transform.SetParent(transform, false);
            int W = 8, H = 64; auroraTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[W * H]; for (int y = 0; y < H; y++) { float k = y / (H - 1f); Color c = Color.Lerp(new Color(.2f, 1f, .55f), new Color(.65f, .3f, 1f), Mathf.SmoothStep(0, 1, (k - .35f) / .65f)); float a = Mathf.SmoothStep(0, 1, k / .12f) * (1 - Mathf.SmoothStep(0, 1, (k - .5f) / .5f)) * .9f; for (int x = 0; x < W; x++) px[y * W + x] = c * a; }
            auroraTex.SetPixels(px); auroraTex.Apply();
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"); auroraMat = new Material(sh); auroraMat.SetTexture("_BaseMap", auroraTex); MeshKit.MakeTransparent(auroraMat, true); auroraMat.SetFloat("_Cull", 0);
            auroraBands.Clear();
            for (int b = 0; b < 3; b++)
            {
                int n = 70; var m = new Mesh { name = "Nordlichtband" }; var v = new Vector3[n * 2]; var uv = new Vector2[n * 2]; var tr = new List<int>();
                for (int i = 0; i < n; i++) { uv[i * 2] = new Vector2(i / (n - 1f), 0); uv[i * 2 + 1] = new Vector2(i / (n - 1f), 1); if (i < n - 1) tr.AddRange(new[] { i * 2, i * 2 + 1, i * 2 + 2, i * 2 + 1, i * 2 + 3, i * 2 + 2 }); }
                m.vertices = v; m.uv = uv; m.SetTriangles(tr, 0); m.MarkDynamic();
                var go = new GameObject("Band " + b); go.transform.SetParent(auroraRoot.transform, false); go.transform.localPosition = new Vector3(0, b * 6, b * 22);
                go.AddComponent<MeshFilter>().sharedMesh = m; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = auroraMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                auroraBands.Add((m, b * 1.9f, 38 + b * 10));
            }
        }

        // ---------- Ampel-Lampen über den Gebäuden ----------
        readonly Dictionary<string, (GameObject go, Material mat)> ampel = new Dictionary<string, (GameObject, Material)>(); int ampelVer = -1; float ampelT;
        static readonly string[] AmpelIds = { "u1", "roh", "presse", "schmelze", "raff", "prod", "sell", "energy" };
        void Ampeln()
        {
            if (Game.Gs(ps) < 2) { foreach (var a in ampel.Values) if (a.go) a.go.SetActive(false); return; }
            if (ampelVer != builtVersion) { foreach (var a in ampel.Values) if (a.go) Destroy(a.go); ampel.Clear(); ampelVer = builtVersion; ampelT = 0; }
            ampelT -= Time.deltaTime; if (ampelT > 0) { Pulse(); return; } ampelT = 1;
            var rep = Game.FlowReport(ps); var lv = new Dictionary<string, int>();
            foreach (var it in rep) { string id = Game.IsUnit(it.id) ? "u1" : it.id; if (!lv.TryGetValue(id, out int o) || it.lvl > o) lv[id] = it.lvl; }
            foreach (var id in AmpelIds)
            {
                if (!blds.TryGetValue(id, out var bp) || !bp.root) continue;
                if (!ampel.TryGetValue(id, out var a) || !a.go)
                {
                    var rs = bp.root.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue; var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
                    var up = bp.root.transform.up; float top = Vector3.Dot(bb.max - bp.root.transform.position, up); top = Mathf.Max(top, 1.5f);
                    var g = new GameObject("Ampel " + id); g.transform.SetParent(baseRoot, false); g.transform.position = bp.root.transform.position + up * (top - .05f); g.transform.rotation = bp.root.transform.rotation; /* sitzt direkt auf dem Dach */
                    MeshKit.Box(g.transform, MeshKit.Mat(new Color(.2f, .22f, .26f), .6f, .4f), new Vector3(0, .12f, 0), new Vector3(.9f, .24f, .9f)); // Gehäuse
                    var mat = MeshKit.Mat(Color.gray, 0, .6f, Color.gray); mat = new Material(mat);
                    var ball = MeshKit.Ball(g.transform, mat, new Vector3(0, .52f, 0), .75f); NoShadow(ball);
                    var halo = MeshKit.Part(g.transform, MeshKit.Quad, Glow(Color.white, .6f), new Vector3(0, .52f, 0), new Vector3(2.6f, 2.6f, 1), Vector3.zero); NoShadow(halo); halo.AddComponent<Billboard>(); halo.name = "Schein";
                    a = (g, mat); ampel[id] = a;
                }
                int l = lv.TryGetValue(id, out int x) ? x : -1; Color c = l == 2 ? new Color(1f, .25f, .2f) : l == 1 ? new Color(1f, .78f, .2f) : l == 0 ? new Color(.25f, 1f, .55f) : new Color(.5f, .55f, .6f);
                a.mat.SetColor("_BaseColor", c * .5f); a.mat.SetColor("_EmissionColor", c * (l < 0 ? .3f : 2.4f));
                var hr = a.go.transform.Find("Schein"); if (hr) { var mr = hr.GetComponent<MeshRenderer>(); mr.enabled = l >= 0; mr.material.SetColor("_BaseColor", new Color(c.r, c.g, c.b, l == 2 ? .8f : .45f)); }
                a.go.SetActive(!MapMode);
            }
        }
        void Pulse() { }
    }
}
