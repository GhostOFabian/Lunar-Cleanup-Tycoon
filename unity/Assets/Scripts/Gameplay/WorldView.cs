// Alles auf und um den Planeten außer den Parzellen: Basisgebäude, Bänder mit Ware, Fahrzeuge, Sammelwagen,
// Rakete, Himmel (Sterne, Sonne, Atmosphäre, Nachbarplanet) und Staubpartikel beim Reinigen.
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public class WorldView : MonoBehaviour
    {
        PState ps; PlanetData pd;
        Transform baseRoot, beltRoot, vehRoot, skyRoot;
        readonly Dictionary<string, Models.BuildingParts> blds = new Dictionary<string, Models.BuildingParts>();
        readonly Dictionary<int, Models.VehicleParts> vehs = new Dictionary<int, Models.VehicleParts>();
        readonly Dictionary<int, Models.DroneParts> drones = new Dictionary<int, Models.DroneParts>(); readonly Dictionary<int, float> droneAlt = new Dictionary<int, float>();
        readonly Dictionary<string, float[]> beltCum = new Dictionary<string, float[]>();
        readonly List<Material> beltMats = new List<Material>();
        GameObject cartGo; int builtVersion = -1; string builtFor;
        public static float NightF; public static bool MapMode; ParticleSystem stormPs; ParticleSystem.Particle[] stormP; Vector3[] stormSt; float stormI, flash; readonly List<Light> lamps = new List<Light>(); Material lampHead, poolMat, beamMat; ParticleSystem dust; Light sun; Color amb0, amb1, amb2; float sunI0;
        float rocketT = -1;
        Mesh itemMesh;
        readonly Dictionary<int, Material> itemMat = new Dictionary<int, Material>();
        readonly Dictionary<int, List<Matrix4x4>> itemMx = new Dictionary<int, List<Matrix4x4>>();

        public void Build(PState state)
        {
            ps = state; pd = ps.pd; builtFor = ps.pid;
            foreach (Transform c in transform) Destroy(c.gameObject);
            blds.Clear(); vehs.Clear(); drones.Clear(); droneAlt.Clear(); beltCum.Clear(); beltMats.Clear(); cartGo = null;
            baseRoot = New("Basis"); beltRoot = New("Bänder"); vehRoot = New("Fahrzeuge"); skyRoot = New("Himmel");
            BuildSky(); BuildBase(); MakeDust(); MakeStorm();
            itemMesh = MeshKit.Cube;
        }
        Transform New(string n) { var g = new GameObject(n); g.transform.SetParent(transform, false); return g.transform; }

        // ---------- Platzierung auf der Kugel ----------
        public Quaternion PlanRot(Vector3 dir, float yaw)
        {
            var c = pd.cells[pd.baseCell]; var fwd = c.e1 * Mathf.Sin(yaw) - c.e2 * Mathf.Cos(yaw);
            fwd = (fwd - dir * Vector3.Dot(fwd, dir)).normalized; return Quaternion.LookRotation(fwd, dir);
        }
        void Place(Transform t, Vector2 plan, float yaw, float lift = 0) { var d = pd.PlanDir(plan); t.position = d * (pd.R + lift); t.rotation = PlanRot(d, yaw); }

        // ---------- Basis ----------
        void BuildBase()
        {
            foreach (Transform c in baseRoot) Destroy(c.gameObject); foreach (Transform c in beltRoot) Destroy(c.gameObject);
            blds.Clear(); beltCum.Clear(); beltMats.Clear();
            Color acc = ps.P.col0, acc2 = ps.P.col1; if (acc.grayscale > .8f) acc = GameData.Hex("#ffbf4d");
            foreach (var id in GameData.BOrder)
            {
                var B = GameData.Bld[id]; if (!ps.lay.pos.ContainsKey(id)) continue; float yaw = ps.lay.yaw.TryGetValue(id, out var y) ? y : 0;
                if (ps.bld.Contains(id))
                {
                    var bp = Models.Building(id, B.fp, acc, acc2); bp.root.transform.SetParent(baseRoot, false); Place(bp.root.transform, ps.lay.pos[id], yaw); blds[id] = bp;
                }
                else if (Game.Gs(ps) >= B.stage && Game.BuildWhy(ps, id) == "")
                {
                    var s = Models.Site(B.fp); s.transform.SetParent(baseRoot, false); Place(s.transform, ps.lay.pos[id], yaw);
                }
            }
            // Laternen: warmes Licht an den wichtigsten Gebäuden, leuchtet nur nachts
            lamps.Clear(); if (!lampHead) lampHead = MeshKit.Mat(new Color(.2f, .18f, .12f), 0, .3f, new Color(1f, .78f, .45f));
            foreach (var id in new[] { "hq", "roh", "u1", "u2", "u3", "prod", "presse", "sell", "werkstatt", "schmelze" })
            {
                if (!ps.bld.Contains(id) || !ps.lay.pos.ContainsKey(id) || lamps.Count >= 6) continue;
                var off = ps.lay.pos[id] + new Vector2(GameData.Bld[id].fp * .7f + .8f, GameData.Bld[id].fp * .7f + .8f);
                var post = new GameObject("Laterne"); post.transform.SetParent(baseRoot, false); Place(post.transform, off, 0);
                MeshKit.Cyl(post.transform, MeshKit.Mat(new Color(.25f, .27f, .3f), .6f, .4f), new Vector3(0, 2.2f, 0), .07f, .09f, 4.4f, 8);
                MeshKit.Ball(post.transform, lampHead, new Vector3(0, 4.5f, 0), .38f);
                var lg = new GameObject("Licht"); lg.transform.SetParent(post.transform, false); lg.transform.localPosition = new Vector3(0, 4.3f, 0);
                var L = lg.AddComponent<Light>(); L.type = LightType.Point; L.range = 16; L.color = new Color(1f, .8f, .55f); L.intensity = 0; L.shadows = LightShadows.None; lamps.Add(L);
                MeshKit.Part(post.transform, MeshKit.Quad, PoolMat(), new Vector3(0, .07f, 0), new Vector3(13, 13, 1), new Vector3(90, 0, 0)).name = "Lichtkegel";
            }
            foreach (var b in ps.lay.belts) if (Game.BeltLive(ps, b)) BuildBelt(b);
            BuildOutposts();
            builtVersion = ps.baseVersion;
        }

        readonly List<(LineRenderer l, StSave o)> pipes = new List<(LineRenderer, StSave)>();
        void BuildOutposts()
        {
            pipes.Clear(); var sites = Game.OutpostSites(ps.pid);
            foreach (int cid in sites)
            {
                var cl = pd.cells[cid]; var o = ps.sv.outposts.Find(x => x.cell == cid); var d = cl.c;
                if (o != null)
                {
                    var bp = Models.Outpost(ps.P.col0); bp.root.transform.SetParent(baseRoot, false); bp.root.transform.position = d * pd.R; bp.root.transform.rotation = Quaternion.LookRotation(cl.e1, d);
                    // Pipeline auf Stelzen bis zum Rohstofflager
                    var a = pd.ToDir(cl, 1.6f, -2.2f); var b = ps.lay.pos.ContainsKey("roh") ? pd.PlanDir(ps.lay.pos["roh"]) : pd.cells[pd.baseCell].c; float arc = pd.Arc(a, b); int n = Mathf.Max(8, Mathf.CeilToInt(arc / 2));
                    var lg = new GameObject("Pipeline"); lg.transform.SetParent(baseRoot, false); var lr = lg.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.positionCount = n + 1; lr.widthMultiplier = .42f; lr.numCapVertices = 4;
                    lr.sharedMaterial = MeshKit.Unlit(GameData.Hex("#8a93a3")); lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var post = MeshKit.Mat(GameData.Hex("#4a525e"), .5f, .4f);
                    for (int i = 0; i <= n; i++)
                    {
                        var p = Vector3.Slerp(a, b, i / (float)n).normalized; float h = i == n ? 4.6f : 1.9f; lr.SetPosition(i, p * (pd.R + h));
                        if (i % 3 == 1 && i < n - 1) { var pg = MeshKit.Cyl(baseRoot, post, Vector3.zero, .07f, .09f, 1.9f, 8); pg.transform.position = p * (pd.R + .95f); pg.transform.rotation = Quaternion.FromToRotation(Vector3.up, p); }
                    }
                    pipes.Add((lr, o));
                }
                else if (Game.ResDone("outpost") || Game.ResDone("drones"))
                {
                    var site = Models.Site(3.2f); site.transform.SetParent(baseRoot, false); site.transform.position = d * (pd.R + .02f); site.transform.rotation = Quaternion.LookRotation(cl.e1, d);
                }
            }
        }

        static Texture2D beltTex;
        static Texture2D BeltTex()
        {
            if (beltTex) return beltTex; int W = 32, H = 64; beltTex = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    float u = Mathf.Abs(x - W / 2f + .5f) / (W / 2f); bool chev = Mathf.Abs(Mathf.Repeat(y + u * 14, 32) - 16) < 3 && u < .8f;
                    byte g = (byte)(chev ? 120 : 40 + (x + y) % 3 * 4); if (u > .9f) g = 160;
                    px[y * W + x] = new Color32(g, g, (byte)(g + 6), 255);
                }
            beltTex.SetPixels32(px); beltTex.Apply(true); return beltTex;
        }

        void BuildBelt(BeltDef b)
        {
            var rp = b.rp; int n = rp.Length; if (n < 2) return; var cum = new float[n]; for (int i = 1; i < n; i++) cum[i] = cum[i - 1] + (rp[i] - rp[i - 1]).magnitude; beltCum[b.id] = cum;
            var v = new List<Vector3>(); var nn = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>(); float w = .55f;
            for (int i = 0; i < n; i++)
            {
                Vector2 dir = (i < n - 1 ? rp[i + 1] - rp[i] : rp[i] - rp[i - 1]).normalized; if (i > 0 && i < n - 1) dir = ((rp[i + 1] - rp[i]).normalized + (rp[i] - rp[i - 1]).normalized).normalized;
                var nrm = new Vector2(-dir.y, dir.x);
                foreach (float s in new[] { -1f, 1f }) { var d = pd.PlanDir(rp[i] + nrm * w * s); v.Add(d * (pd.R + b.h + .12f)); nn.Add(d); uv.Add(new Vector2(s < 0 ? 0 : 1, cum[i] / 1.2f)); }
            }
            for (int i = 0; i < n - 1; i++) { int a = i * 2; t.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
            var mesh = new Mesh { name = b.id }; mesh.SetVertices(v); mesh.SetNormals(nn); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            var tri = mesh.triangles; for (int i = 0; i < tri.Length; i += 3) { var A = v[tri[i]]; var B = v[tri[i + 1]]; var C = v[tri[i + 2]]; if (Vector3.Dot(Vector3.Cross(B - A, C - A), A) < 0) { int x = tri[i + 1]; tri[i + 1] = tri[i + 2]; tri[i + 2] = x; } }
            mesh.triangles = tri; mesh.RecalculateBounds();
            var go = new GameObject(b.id); go.transform.SetParent(beltRoot, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetTexture("_BaseMap", BeltTex()); m.SetFloat("_Smoothness", .3f); m.SetFloat("_Metallic", .2f);
            go.AddComponent<MeshRenderer>().sharedMaterial = m; beltMats.Add(m);
            // Seitenwangen und Stützen
            var rail = MeshKit.Mat(GameData.Hex("#59606c"), .7f, .5f); var post = MeshKit.Mat(GameData.Hex("#3a4150"), .6f, .45f); var gold = MeshKit.Mat(GameData.Hex("#ffbf4d"), .2f, .4f);
            float L = cum[n - 1];
            for (float s = .8f; s < L - .4f; s += 2.6f)
            {
                var p = PointAt(rp, cum, s, out var tan); var d = pd.PlanDir(p); float h = b.h + .1f;
                var g = MeshKit.Box(beltRoot, post, Vector3.zero, new Vector3(.12f, h, .12f)); g.transform.position = d * (pd.R + h / 2); g.transform.rotation = PlanRot(d, 0);
                var nrm = new Vector2(-tan.y, tan.x);
                foreach (float sd in new[] { -1f, 1f }) { var dd = pd.PlanDir(p + nrm * (w + .06f) * sd); var r = MeshKit.Box(beltRoot, sd < 0 ? rail : gold, Vector3.zero, new Vector3(.1f, .18f, 2.6f)); r.transform.position = dd * (pd.R + b.h + .16f); r.transform.rotation = Quaternion.LookRotation(Tangent(d, tan), dd); }
            }
        }
        Vector3 Tangent(Vector3 d, Vector2 tan) { var a = pd.PlanDir(Vector2.zero); var c = pd.cells[pd.baseCell]; var t3 = c.e1 * tan.x + c.e2 * tan.y; return (t3 - d * Vector3.Dot(t3, d)).normalized; }
        static Vector2 PointAt(Vector2[] rp, float[] cum, float s, out Vector2 tan)
        {
            int i = 1; while (i < cum.Length - 1 && cum[i] < s) i++; float seg = Mathf.Max(1e-5f, cum[i] - cum[i - 1]); float t = Mathf.Clamp01((s - cum[i - 1]) / seg);
            tan = (rp[i] - rp[i - 1]).normalized; return Vector2.Lerp(rp[i - 1], rp[i], t);
        }

        // ---------- Himmel ----------
        void BuildSky()
        {
            var th = ps.P.theme; var cam = Camera.main;
            if (cam) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = th.sky; }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = th.hemiSky * th.hemiI * 1.1f; RenderSettings.ambientEquatorColor = Color.Lerp(th.hemiSky, th.hemiGround, .5f) * th.hemiI * .8f; RenderSettings.ambientGroundColor = th.hemiGround * th.hemiI * .7f;
            amb0 = RenderSettings.ambientSkyColor; amb1 = RenderSettings.ambientEquatorColor; amb2 = RenderSettings.ambientGroundColor;
            RenderSettings.fog = false;
            sun = FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            if (!sun) { var lg = new GameObject("Sonne"); sun = lg.AddComponent<Light>(); sun.type = LightType.Directional; }
            sun.color = th.sun; sunI0 = th.sunI * 1.15f; sun.intensity = sunI0; sun.shadows = LightShadows.Soft; sun.shadowStrength = .85f;
            UpdateSun();
            // Sterne
            int nS = Mathf.Max(300, th.stars); var v = new List<Vector3>(); var col = new List<Color>(); var t = new List<int>(); var r = new System.Random(7);
            for (int i = 0; i < nS; i++)
            {
                var d = new Vector3((float)r.NextDouble() * 2 - 1, (float)r.NextDouble() * 2 - 1, (float)r.NextDouble() * 2 - 1).normalized; var p = d * 1800;
                var a = Vector3.Cross(d, Mathf.Abs(d.y) < .9f ? Vector3.up : Vector3.right).normalized; var b = Vector3.Cross(d, a); float s = 1.5f + (float)r.NextDouble() * 3.5f;
                int k = v.Count; v.Add(p - a * s - b * s); v.Add(p + a * s - b * s); v.Add(p + a * s + b * s); v.Add(p - a * s + b * s);
                float br = .5f + (float)r.NextDouble() * .5f; var c = Color.Lerp(Color.white, i % 5 == 0 ? new Color(.7f, .8f, 1) : new Color(1, .9f, .8f), (float)r.NextDouble() * .5f) * br; for (int q = 0; q < 4; q++) col.Add(c);
                t.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            }
            var sm = new Mesh { name = "Sterne", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; sm.SetVertices(v); sm.SetColors(col); sm.SetTriangles(t, 0); sm.bounds = new Bounds(Vector3.zero, Vector3.one * 4000);
            var sg = new GameObject("Sterne"); sg.transform.SetParent(skyRoot, false); sg.AddComponent<MeshFilter>().sharedMesh = sm;
            var smat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default")); smat.SetTexture("_BaseMap", MeshKit.GlowTex()); MeshKit.MakeTransparent(smat, true); smat.SetColor("_BaseColor", Color.white);
            sg.AddComponent<MeshRenderer>().sharedMaterial = smat;
            // Atmosphärenleuchten hinter dem Planeten
            if (th.hasGlow)
            {
                var halo = MeshKit.Part(skyRoot, MeshKit.Quad, null, Vector3.zero, Vector3.one * pd.R * 2.9f); halo.name = "Atmosphäre";
                var hm = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default")); hm.SetTexture("_BaseMap", MeshKit.GlowTex()); MeshKit.MakeTransparent(hm, true);
                var gc = th.glow; gc.a = Mathf.Clamp01(th.glowOp * 2.2f); hm.SetColor("_BaseColor", gc); halo.GetComponent<MeshRenderer>().sharedMaterial = hm; halo.AddComponent<Billboard>();
            }
            // Nachbarkörper am Himmel
            string gi = th.giant; Vector3 gp = Vector3.zero; float gr = 0; Color gcol = Color.white;
            if (ps.pid == "moon") { gp = new Vector3(-900, 500, -1500); gr = 120; gcol = GameData.Hex("#2a6fd6"); }
            else if (gi == "jupiter") { gp = new Vector3(-1500, 700, -1900); gr = 420; gcol = GameData.Hex("#c49060"); }
            else if (gi == "saturn") { gp = new Vector3(1300, 600, -1900); gr = 300; gcol = GameData.Hex("#e8d29a"); }
            else if (gi == "neptune") { gp = new Vector3(-1200, 500, -1700); gr = 260; gcol = GameData.Hex("#3a6ad8"); }
            else if (gi == "phobos" || gi == "charon" || gi == "dysnomia") { gp = new Vector3(900, 400, -1300); gr = 70; gcol = GameData.Hex("#9a9088"); }
            else if (gi == "reddwarf" || gi == "trappist") { gp = new Vector3(1300, 650, -1900); gr = 140; gcol = GameData.Hex("#ff6a3a"); }
            else if (gi == "sunclose") { gp = new Vector3(1100, 500, -1500); gr = 380; gcol = GameData.Hex("#fff0c8"); }
            else if (gi == "pulsar") { gp = new Vector3(-1200, 600, -1700); gr = 30; gcol = GameData.Hex("#e8fff0"); }
            if (gr > 0)
            {
                bool star = gi == "reddwarf" || gi == "trappist" || gi == "sunclose" || gi == "pulsar";
                var gb = MeshKit.Ball(skyRoot, star ? MeshKit.Unlit(gcol) : MeshKit.Mat(gcol, 0, .2f, gcol * .25f), gp, gr * 2); gb.name = "Nachbar";
                if (ps.pid == "moon") { var land = MeshKit.Ball(skyRoot, MeshKit.Mat(GameData.Hex("#4fa45a"), 0, .2f, GameData.Hex("#4fa45a") * .2f), gp + new Vector3(20, 30, 60), gr * 1.55f); land.name = "Kontinente"; }
            }
        }

        // ---------- sichtbares Wetter (wie im Browser: Teilchen um die Kamera, Nebel, Blitze) ----------
        const int SFX_N = 1400;
        void MakeStorm()
        {
            var g = new GameObject("Sturm"); g.transform.SetParent(transform, false); stormPs = g.AddComponent<ParticleSystem>();
            var main = stormPs.main; main.loop = false; main.playOnAwake = false; main.maxParticles = SFX_N; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startLifetime = 1e6f; main.startSpeed = 0;
            var em = stormPs.emission; em.enabled = false; var sh = stormPs.shape; sh.enabled = false;
            var pr = stormPs.GetComponent<ParticleSystemRenderer>(); var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default")); m.SetTexture("_BaseMap", MeshKit.GlowTex()); MeshKit.MakeTransparent(m, false); pr.sharedMaterial = m;
            stormP = new ParticleSystem.Particle[SFX_N]; stormSt = new Vector3[SFX_N]; var r = new System.Random(3);
            for (int i = 0; i < SFX_N; i++) stormSt[i] = new Vector3((float)r.NextDouble() * 2 - 1, (float)r.NextDouble() * 2 - 1, (float)r.NextDouble());
            stormI = 0; flash = 0;
        }
        void UpdateStorm()
        {
            if (!stormPs) return; GameData.Storm.TryGetValue(ps.pid, out var L); var st = ps.P.storm; float dt = Time.deltaTime;
            bool on = L != null && Game.StormOn(ps), soon = L != null && st != null && !on && Game.StormIn(ps) <= 30;
            float tgt = MapMode ? 0 : Mathf.Max(on ? (ps.shOn ? .45f : 1) : soon ? .22f : 0, (L != null && L.rad ? .3f : 0));
            stormI = Mathf.Lerp(stormI, tgt, 1 - Mathf.Exp(-1.5f * dt)); float I = stormI;
            if (L == null || I < .01f) { RenderSettings.fog = false; if (stormPs.particleCount > 0) stormPs.Clear(); flash = 0; return; }
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = L.fog; RenderSettings.fogDensity = L.dens * I * .9f;
            var cam = Camera.main; if (!cam) return; var c = cam.transform.position.normalized; float camAlt = cam.transform.position.magnitude - pd.R;
            var e1 = Vector3.Cross(c, Mathf.Abs(c.y) < .9f ? Vector3.up : Vector3.right).normalized; var e2 = Vector3.Cross(c, e1);
            float S0 = Mathf.Max(25, camAlt * 1.6f), H = Mathf.Max(8, camAlt * .35f), R = pd.R; int n = Mathf.FloorToInt(SFX_N * Mathf.Min(1, .25f + I));
            var col = L.col; col.a = .9f * I; float size = L.size * 2.4f * (1 + camAlt / 40) * .35f;
            for (int i = 0; i < SFX_N; i++)
            {
                var s0 = stormSt[i]; float u = s0.x + L.wind * dt / S0 * (.7f + .6f * ((i * 7919) % 10) / 10f), h = s0.z - L.fall * dt / H; if (u > 1) u -= 2; if (h < 0) h += 1; if (h > 1) h -= 1; stormSt[i] = new Vector3(u, s0.y, h);
                float wx = u * S0, wy = s0.y * S0; var d = (c + (e1 * wx + e2 * wy) / R).normalized;
                stormP[i].position = d * (R + .3f + h * H); stormP[i].startSize = i < n ? size : 0; stormP[i].startColor = col; stormP[i].remainingLifetime = 1e5f; stormP[i].startLifetime = 1e6f;
            }
            stormPs.SetParticles(stormP, SFX_N);
            if (L.lightning && (on || L.rad && Random.value < .04f) && Random.value < dt * .8f) flash = 1;
            if (L.embers && on && Random.value < dt * 10) EmitDust((c + e1 * (Random.value - .5f) * S0 / R).normalized, Random.value < .5f ? new Color(1, .48f, .16f) : new Color(1, .69f, .3f), 2);
            if (L.geysers && on && Random.value < dt * 1.2f) EmitDust((c + e1 * (Random.value - .5f) * S0 / R + e2 * (Random.value - .5f) * S0 / R).normalized, new Color(.92f, .96f, 1), 26);
            if (L.rad && Random.value < dt * 3 * I) EmitDust((c + e1 * (Random.value - .5f) * S0 / R + e2 * (Random.value - .5f) * S0 / R).normalized, new Color(.49f, 1, .42f), 3);
            flash = Mathf.Max(0, flash - dt * 5);
            if (flash > 0) { var k = 1 + 2.2f * flash; RenderSettings.ambientSkyColor *= k; RenderSettings.ambientEquatorColor *= k; RenderSettings.ambientGroundColor *= k; }
        }

        void MakeDust()
        {
            var g = new GameObject("Staub"); g.transform.SetParent(transform, false); dust = g.AddComponent<ParticleSystem>();
            var main = dust.main; main.loop = false; main.playOnAwake = false; main.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1.1f); main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(.12f, .32f); main.gravityModifier = 0; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 600;
            var em = dust.emission; em.rateOverTime = 0; var sh = dust.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = .4f;
            var col = dust.colorOverLifetime; col.enabled = true; var grd = new Gradient(); grd.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(.9f, 0), new GradientAlphaKey(0, 1) }); col.color = grd;
            var pr = dust.GetComponent<ParticleSystemRenderer>(); var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default")); m.SetTexture("_BaseMap", MeshKit.GlowTex()); MeshKit.MakeTransparent(m, false); pr.sharedMaterial = m;
        }
        public void EmitDust(Vector3 dir, Color c, int n)
        {
            if (!dust) return; var ep = new ParticleSystem.EmitParams { position = dir * (pd.R + .2f), startColor = c, applyShapeToPosition = true };
            dust.transform.rotation = Quaternion.LookRotation(dir); dust.Emit(ep, n);
        }

        // ---------- pro Frame ----------
        void LateUpdate()
        {
            if (ps == null) return;
            if (builtFor != Game.S.planet) return;
            if (ps.baseVersion != builtVersion) BuildBase();
            foreach (var m in beltMats) { var off = m.GetTextureOffset("_BaseMap"); off.y -= Time.deltaTime * 2.4f * Game.Uv("bspeed", ps) * Mathf.Max(.25f, ps.ef) / 1.2f; m.SetTextureOffset("_BaseMap", off); }
            foreach (var (l, o) in pipes) if (l) { var c = o.busy ? Color.Lerp(GameData.Hex("#8a93a3"), GameData.Hex("#6ff2d2"), .5f + .5f * Mathf.Sin(Time.time * 6)) : GameData.Hex("#8a93a3"); l.startColor = l.endColor = c; }
            UpdateSun(); UpdateStorm(); DrawItems(); SyncVehicles(); SyncDrones(); SyncCart(); AnimateBuildings();
        }

        /// <summary>Tag und Nacht: die Sonne kreist über der Basis (wie im Browser), nachts wird das Umgebungslicht schwächer.</summary>
        void UpdateSun()
        {
            if (!sun) return; var sd = Game.SunDir(ps); sun.transform.rotation = Quaternion.LookRotation(-sd);
            var cam = Camera.main; var cd = cam ? cam.transform.position.normalized : pd.cells[pd.baseCell].c;
            float k = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.14f, .3f, Vector3.Dot(sd, cd)));
            sun.intensity = sunI0; float a = .34f + .66f * k;
            RenderSettings.ambientSkyColor = amb0 * a; RenderSettings.ambientEquatorColor = amb1 * a; RenderSettings.ambientGroundColor = amb2 * a;
            NightF = Mathf.Lerp(NightF, 1 - k, 1 - Mathf.Exp(-3 * Time.deltaTime)); float nf = Mathf.Clamp01((NightF - .2f) / .6f);
            foreach (var L in lamps) if (L) L.intensity = 2.2f * nf;
            if (lampHead) lampHead.SetColor("_EmissionColor", new Color(1f, .78f, .45f) * (.1f + 2.2f * nf));
            foreach (var vp in vehs.Values) if (vp.head) vp.head.intensity = 3f * nf;
            if (poolMat) poolMat.SetColor("_BaseColor", new Color(1f, .78f, .5f, .5f * nf)); if (beamMat) beamMat.SetColor("_BaseColor", new Color(.8f, .9f, 1f, .45f * nf));
        }

        void DrawItems()
        {
            foreach (var l in itemMx.Values) l.Clear();
            foreach (var b in ps.lay.belts)
            {
                if (!beltCum.TryGetValue(b.id, out var cum) || !ps.belts.TryGetValue(b.id, out var items)) continue;
                foreach (var it in items)
                {
                    var p = PointAt(b.rp, cum, it.s / Mathf.Max(.01f, b.L) * cum[cum.Length - 1], out var tan); var d = pd.PlanDir(p);
                    var M = GameData.Mats[it.m]; Vector3 sc = M.shape == "brick" ? new Vector3(.3f, .2f, .5f) : M.shape == "ingot" ? new Vector3(.22f, .16f, .55f) : M.shape == "gem" ? new Vector3(.3f, .3f, .3f) : M.shape == "barrel" ? new Vector3(.32f, .4f, .32f) : new Vector3(.42f, .34f, .42f);
                    var mx = Matrix4x4.TRS(d * (pd.R + b.h + .14f + sc.y / 2), Quaternion.LookRotation(Tangent(d, tan), d), sc);
                    if (!itemMx.TryGetValue(it.m, out var list)) { list = new List<Matrix4x4>(); itemMx[it.m] = list; }
                    list.Add(mx);
                }
            }
            foreach (var kv in itemMx)
            {
                if (kv.Value.Count == 0) continue;
                if (!itemMat.TryGetValue(kv.Key, out var mat)) { var M = GameData.Mats[kv.Key]; mat = M.shape == "gem" ? MeshKit.Mat(M.col, .1f, .9f, M.col * .5f) : MeshKit.Mat(M.col, M.shape == "ingot" ? .85f : .2f, M.shape == "ingot" ? .75f : .45f); itemMat[kv.Key] = mat; }
                Graphics.DrawMeshInstanced(itemMesh, 0, mat, kv.Value);
            }
        }

        void SyncVehicles()
        {
            var alive = new HashSet<int>();
            Color acc = ps.P.col0;
            foreach (var v in ps.sv.veh)
            {
                alive.Add(v.id);
                if (!vehs.TryGetValue(v.id, out var vp)) { vp = Models.Vehicle(GameData.Veh[v.type], acc); vp.root.transform.SetParent(vehRoot, false); vp.root.transform.position = v.d * pd.R; AddHeadlight(vp); vehs[v.id] = vp; }
                var tr = vp.root.transform; var target = v.d * pd.R; var mv = target - tr.position; var up = v.d;
                var fwd = mv - up * Vector3.Dot(mv, up);
                Quaternion rot = fwd.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(fwd.normalized, up) : Quaternion.LookRotation(Vector3.ProjectOnPlane(tr.forward, up).normalized + up * 1e-4f, up);
                tr.position = target; tr.rotation = Quaternion.Slerp(tr.rotation, rot, 1 - Mathf.Exp(-8 * Time.deltaTime));
                float spin = mv.magnitude / Mathf.Max(.2f, .38f) * Mathf.Rad2Deg; foreach (var w in vp.wheels) w.Rotate(Vector3.up, spin, Space.Self);
                if (vp.tool) vp.tool.Rotate(Vector3.up, (v.mode == "clean" ? 900 : 60) * Time.deltaTime, Space.Self);
                if (v.mode == "clean" && Random.value < Time.deltaTime * 12) EmitDust(v.d, GameData.Dirt[pd.cells[v.cell >= 0 ? v.cell : 0].type].lightC, 2);
            }
            foreach (var id in vehs.Keys.Where(k => !alive.Contains(k)).ToList()) { Destroy(vehs[id].root); vehs.Remove(id); }
        }

        Material PoolMat()
        {
            if (poolMat) return poolMat; poolMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default")); poolMat.SetTexture("_BaseMap", MeshKit.GlowTex()); MeshKit.MakeTransparent(poolMat, true); poolMat.SetFloat("_Cull", 0); poolMat.SetColor("_BaseColor", new Color(1f, .78f, .5f, 0)); return poolMat;
        }
        Material BeamMat()
        {
            if (beamMat) return beamMat; beamMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default")); beamMat.SetTexture("_BaseMap", MeshKit.GlowTex()); MeshKit.MakeTransparent(beamMat, true); beamMat.SetFloat("_Cull", 0); beamMat.SetColor("_BaseColor", new Color(.8f, .9f, 1f, 0)); return beamMat;
        }
        void AddHeadlight(Models.VehicleParts vp)
        {
            MeshKit.Part(vp.root.transform, MeshKit.Quad, BeamMat(), new Vector3(0, .06f, 3.2f), new Vector3(4.2f, 6.5f, 1), new Vector3(90, 0, 0)).name = "Lichtkegel";
            var lg = new GameObject("Scheinwerfer"); lg.transform.SetParent(vp.root.transform, false); lg.transform.localPosition = new Vector3(0, 1.1f, .9f); lg.transform.localRotation = Quaternion.Euler(28, 0, 0);
            var L = lg.AddComponent<Light>(); L.type = LightType.Spot; L.range = 11; L.spotAngle = 70; L.innerSpotAngle = 35; L.color = new Color(.85f, .92f, 1f); L.intensity = 0; L.shadows = LightShadows.None; vp.head = L;
        }

        void SyncDrones()
        {
            var alive = new HashSet<int>();
            foreach (var dr in ps.sv.drones)
            {
                alive.Add(dr.id);
                if (!drones.TryGetValue(dr.id, out var dp)) { dp = Models.Drone(ps.P.col0); dp.root.transform.SetParent(vehRoot, false); dp.root.transform.position = dr.d * (pd.R + 2); drones[dr.id] = dp; droneAlt[dr.id] = 2; }
                bool low = dr.mode == "load" || dr.mode == "stdrop" || dr.mode == "drop" || (dr.mode == "idle" && !dr.busy);
                float goal = dr.mode == "load" ? 3.2f : dr.mode == "stdrop" ? 7.5f : low ? 2.4f : 12f, alt = Mathf.Lerp(droneAlt[dr.id], goal, 1 - Mathf.Exp(-2.5f * Time.deltaTime)); droneAlt[dr.id] = alt;
                var tr = dp.root.transform; var target = dr.d * (pd.R + alt + Mathf.Sin(Time.time * 2 + dr.id) * .15f); var mv = target - tr.position; var up = dr.d;
                var fwd = mv - up * Vector3.Dot(mv, up);
                if (fwd.sqrMagnitude > 1e-5f) tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(fwd.normalized, up) * Quaternion.Euler(Mathf.Min(14, fwd.magnitude / Mathf.Max(1e-3f, Time.deltaTime) * 1.2f), 0, 0), 1 - Mathf.Exp(-5 * Time.deltaTime));
                else tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(tr.forward, up).normalized + up * 1e-4f, up), 1 - Mathf.Exp(-5 * Time.deltaTime));
                tr.position = target;
                foreach (var r in dp.rotors) r.Rotate(Vector3.up, (dr.busy || dr.mode != "idle" ? 2200 : 300) * Time.deltaTime, Space.Self);
                if (dp.cargo) dp.cargo.gameObject.SetActive(Game.Sum(dr.tank) > .05f);
            }
            foreach (var id in drones.Keys.Where(k => !alive.Contains(k)).ToList()) { Destroy(drones[id].root); drones.Remove(id); droneAlt.Remove(id); }
        }

        void SyncCart()
        {
            var c = ps.sv.cart; if (!c.owned) { if (cartGo) Destroy(cartGo); return; }
            if (!cartGo) { cartGo = Models.Cart(); cartGo.transform.SetParent(vehRoot, false); cartGo.transform.position = c.d * pd.R; }
            var tr = cartGo.transform; var target = c.d * pd.R; var mv = target - tr.position; var fwd = mv - c.d * Vector3.Dot(mv, c.d);
            if (fwd.sqrMagnitude > 1e-6f) tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(fwd.normalized, c.d), 1 - Mathf.Exp(-8 * Time.deltaTime));
            else tr.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(tr.forward, c.d).normalized + c.d * 1e-4f, c.d);
            tr.position = target;
        }

        public void LaunchRocket() { rocketT = 0; }

        void AnimateBuildings()
        {
            float t = Time.time;
            if (blds.TryGetValue("presse", out var pr) && pr.piston) { bool on = ps.mach["presse"].cur >= 0; float y = on ? 2.2f - .9f * Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 1.6f)), 3) : 2.2f; pr.piston.localPosition = new Vector3(0, y, 0); }
            if (blds.TryGetValue("research", out var rs) && rs.spin) rs.spin.Rotate(Vector3.up, 20 * Time.deltaTime, Space.World);
            if (blds.TryGetValue("sell", out var sl) && sl.rocket)
            {
                if (rocketT >= 0)
                {
                    rocketT += Time.deltaTime; float h = rocketT < .5f ? 0 : Mathf.Pow(rocketT - .5f, 2.2f) * 14;
                    sl.rocket.localPosition = new Vector3(0, .7f + h, 0);
                    if (rocketT < 3.5f && Random.value < .9f) EmitDust(sl.rocket.position.normalized, new Color(1, .7f, .35f), 3);
                    if (rocketT > 4) { rocketT = -1; sl.rocket.localPosition = new Vector3(0, -6, 0); }
                }
                else if (sl.rocket.localPosition.y < .7f) sl.rocket.localPosition = Vector3.MoveTowards(sl.rocket.localPosition, new Vector3(0, .7f, 0), Time.deltaTime * 3);
            }
        }
    }

    public class Billboard : MonoBehaviour
    {
        void LateUpdate() { var c = Camera.main; if (!c) return; var dir = (c.transform.position).normalized; transform.position = -dir * 40; transform.rotation = Quaternion.LookRotation(transform.position - c.transform.position, c.transform.up); }
    }
}
