// Build 34 (Fabian 02.11.): Abfang-Minispiel, Schild-Abwehr, Lieferraketen und Terraforming (Moos, Gras, Wasser, Bäume im Wind).
// Nur Materialien, die es schon gibt (Lit ohne Transparenz, Partikel-Unlit, Sprites/Default) – keine neuen Shader-Varianten.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        void UpdateBuild34() { UpdateIcp(); UpdateShots(); UpdateSupply(); UpdateTerra(); UpdateEdit(); UpdateDefense(); UpdateOrders(); }

        // =====================================================================
        // Riesenmeteor: Schild lenkt ab / letzter Sturz / Abfangen
        // =====================================================================
        static Texture2D icpRingTex;
        static Texture2D IcpRingTex()
        {
            if (icpRingTex) return icpRingTex; const int S = 128; icpRingTex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "Ring" };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) { float r = new Vector2(x - S / 2f + .5f, y - S / 2f + .5f).magnitude / (S / 2f); float a = Mathf.Clamp01(1 - Mathf.Abs(r - .88f) / .1f); px[y * S + x] = new Color32(255, 255, 255, (byte)(a * a * 255)); }
            icpRingTex.SetPixels32(px); icpRingTex.Apply(true); return icpRingTex;
        }
        /// <summary>Der Meteor wird vom Schild abgelenkt: Schildkuppel leuchtet auf, der Meteor prallt ab und fliegt davon.</summary>
        public void ShieldBlock()
        {
            var bc = pd.cells[pd.baseCell]; var at = shieldGo ? shieldGo.transform.position : pd.Surf(bc.c, 0); var go = doomGo; doomGo = null; doomTo = Vector3.zero; StartCoroutine(Deflect(go, at));
        }
        IEnumerator Deflect(GameObject go, Vector3 baseAt)
        {
            var dome = MeshKit.Part(transform, MeshKit.Sphere, GlowMat(Texture2D.whiteTexture, new Color(.35f, .75f, 1f, .0f)), baseAt, Vector3.one * 70); NoShadow(dome); var dm = dome.GetComponent<MeshRenderer>().sharedMaterial;
            Vector3 p0 = go ? go.transform.position : baseAt + baseAt.normalized * 60, v = Vector3.Reflect((baseAt - p0).normalized, baseAt.normalized) * 60 + Random.onUnitSphere * 10;
            Flash(p0, new Color(.6f, .85f, 1f)); Sfx.Play("big", 1.4f, .8f); Shake = Mathf.Max(Shake, .8f);
            float T = 0;
            while (T < 2.2f)
            {
                float dt = Time.deltaTime; T += dt; float a = T < .25f ? T / .25f : Mathf.Clamp01(1 - (T - .25f) / 1.6f);
                dm.SetColor("_BaseColor", new Color(.35f, .75f, 1f, .32f * a)); dome.transform.localScale = Vector3.one * (70 + 8 * T);
                if (go) { go.transform.position += v * dt; go.transform.localScale *= 1 - dt * .5f; }
                yield return null;
            }
            Destroy(dome); if (go) Destroy(go);
        }
        /// <summary>Ohne Abwehr: der Meteor stürzt das letzte Stück auf die Basis, danach Explode().</summary>
        public IEnumerator DoomDive()
        {
            var go = doomGo; doomGo = null; if (!go) yield break; var p0 = go.transform.position; float T = 0;
            while (T < .7f) { T += Time.deltaTime; go.transform.position = Vector3.Lerp(p0, doomTo, T / .7f); Shake = Mathf.Max(Shake, 1); yield return null; }
            Destroy(go);
        }

        class Chunk { public GameObject go; public Transform ring; public Material ringMat; public Vector3 p0, p1, side; public float t, T, size, ringT, ringP, amp, delay, win; public bool dead, aimed, auto; public int idx; }
        readonly List<Chunk> chunks = new List<Chunk>(); bool icpOn; float icpEnd = -1;
        public bool InterceptActive => icpOn;
        public static event System.Action<Vector3, string, string> OnFeedback; // Weltpunkt, Text, Art – GameRoot zeigt ihn als schwebenden Text
        /// <summary>Test: 1 = jeder Brocken getroffen, 2 = jeder Schuss daneben, 3 = jeder zweite getroffen, Rest durch.</summary>
        public static int AutoPlay;
        /* Ring: 3,2× → 0,4× Brockengröße, dann von vorn. Fabian 02.11.: im grünen Bereich (um 1×) schrumpft er langsam – 40 % der Zeit ist er grün */
        float RingK(Chunk c)
        {
            float f = (c.ringT / c.ringP) % 1f, hi = 1 + c.win, lo = 1 - c.win;
            if (f < .45f) return Mathf.Lerp(3.2f, hi, f / .45f);
            if (f < .85f) return Mathf.Lerp(hi, lo, (f - .45f) / .4f);
            return Mathf.Lerp(lo, .4f, (f - .85f) / .15f);
        }
        public void StartIntercept()
        {
            var I = Game.Icp; if (I == null) return; chunks.Clear(); icpOn = true; icpEnd = -1;
            var bc = pd.cells[pd.baseCell]; Vector3 at = doomGo ? doomGo.transform.position : (bc.e1 * .9f + bc.c * .9f).normalized * pd.R * 2.4f;
            if (doomGo) { Destroy(doomGo); doomGo = null; }
            Flash(at, new Color(1f, .7f, .4f)); Sfx.Play("boom", .8f, 1); Shake = Mathf.Max(Shake, 1.2f);
            float d = I.diff; var rm = RockKit.MeteorMat();
            for (int i = 0; i < I.total; i++)
            {
                float u = (float)i / Mathf.Max(1, I.total - 1);
                var dir = (bc.c * pd.R + bc.e1 * Random.Range(-22f, 22f) + bc.e2 * Random.Range(-22f, 22f)).normalized;
                var c = new Chunk { idx = i, p0 = at + Random.insideUnitSphere * 4, p1 = pd.Surf(dir, .4f), size = pd.R * Random.Range(.07f, .1f) };
                c.T = Mathf.Max(4f, 9f - .3f * d) * Random.Range(.92f, 1.1f) * (1 - .25f * u);         // spätere Brocken schneller
                c.delay = 1.2f + i * Mathf.Max(.8f, 1.7f - .06f * d); /* kurze Pause nach dem Zerbrechen */
                c.amp = (u >= .5f && d >= 1.5f ? 1 : 0) * pd.R * Mathf.Min(.6f, .18f + .03f * d) * Random.Range(.7f, 1f); // spätere fliegen Kurven
                var fwd = (c.p1 - c.p0).normalized; c.side = Vector3.Cross(fwd, Random.onUnitSphere).normalized;
                c.ringP = Mathf.Max(1.6f, 2.6f - .06f * d); c.win = Mathf.Max(.18f, .3f - .01f * d); /* grün etwa 0,6–1 s lang */ /* Fabian 02.11.: großzügigeres Zeitfenster */ c.ringT = Random.value * c.ringP;
                c.go = new GameObject("Brocken " + i); c.go.transform.SetParent(transform, false); c.go.transform.position = c.p0; c.go.SetActive(false);
                var rk = new GameObject("Fels"); rk.transform.SetParent(c.go.transform, false); rk.transform.localScale = Vector3.one * c.size; rk.AddComponent<MeshFilter>().sharedMesh = RockKit.Mesh(i, false); rk.AddComponent<MeshRenderer>().sharedMaterial = rm;
                var gl = MeshKit.Part(c.go.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(1f, .5f, .2f, .9f)), Vector3.zero, Vector3.one * c.size * 3.2f); NoShadow(gl); gl.AddComponent<Billboard>();
                var tr = c.go.AddComponent<TrailRenderer>(); tr.time = .8f; tr.widthMultiplier = c.size * .9f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)); tr.material = GlowMat(MeshKit.GlowTex(), new Color(1f, .55f, .2f, 1)); tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                c.ringMat = GlowMat(IcpRingTex(), new Color(1, 1, 1, .9f)); var rg = MeshKit.Part(c.go.transform, MeshKit.Quad, c.ringMat, Vector3.zero, Vector3.one); NoShadow(rg); rg.AddComponent<Billboard>(); c.ring = rg.transform;
                chunks.Add(c);
            }
        }
        Vector3 ChunkPos(Chunk c) { float u = Mathf.Clamp01(c.t / c.T); return Vector3.Lerp(c.p0, c.p1, u) + c.side * c.amp * Mathf.Sin(Mathf.PI * u); }
        void UpdateIcp()
        {
            if (!icpOn) return;
            if (Game.Icp == null) { EndIcpVisuals(); return; }
            float dt = Time.deltaTime; bool any = false;
            foreach (var c in chunks)
            {
                if (c.dead) continue; any = true;
                if (c.delay > 0) { c.delay -= dt; if (c.delay <= 0) c.go.SetActive(true); else continue; }
                c.t += dt; c.ringT += dt; c.go.transform.position = ChunkPos(c); c.go.transform.Rotate(new Vector3(.4f, 1, .2f), 120 * dt);
                float k = RingK(c); bool inWin = Mathf.Abs(k - 1) <= c.win;
                c.ring.localScale = Vector3.one * c.size * 2.2f * k; c.ringMat.SetColor("_BaseColor", inWin ? new Color(.45f, 1f, .6f, 1) : new Color(1f, .92f, .8f, 1));
                if (!c.aimed && Game.Icp.rockets > 0 && AutoPlay > 0 && c.t > .5f)
                {
                    if (AutoPlay == 1 && inWin) Shoot(c, true);
                    else if (AutoPlay == 2 && k > 2.4f) Shoot(c, false);
                    else if (AutoPlay == 3 && c.idx % 2 == 0 && inWin) Shoot(c, true);
                }
                if (c.t >= c.T)
                {
                    c.dead = true; c.go.SetActive(false); Game.IcpThrough();
                    Flash(c.p1, new Color(1f, .6f, .3f)); Sfx.PlayAt("boom", c.p1, .9f + Random.value * .2f, 1, 400); Shake = Mathf.Max(Shake, 1.4f);
                    if (dust) for (int q = 0; q < 30; q++) dust.Emit(new ParticleSystem.EmitParams { position = c.p1, velocity = (c.p1.normalized + Random.insideUnitSphere) * Random.Range(4f, 14f), startColor = new Color(.5f, .42f, .35f), startSize = Random.Range(.6f, 1.6f), startLifetime = 2.5f }, 1);
                }
            }
            if (!any && icpEnd < 0) icpEnd = Time.time + 1f;
            if (icpEnd > 0 && Time.time >= icpEnd) { EndIcpVisuals(); Game.IcpResolve(); }
        }
        void EndIcpVisuals() { foreach (var c in chunks) if (c.go) Destroy(c.go); chunks.Clear(); icpOn = false; icpEnd = -1; }
        /// <summary>Tipp während des Abfangens: nächster Brocken am Finger. Treffer, wenn der Ring gerade den Brocken umschließt.</summary>
        public bool IcpTap(Camera cam, Vector2 pos, float minPx = 110)
        {
            if (!icpOn || Game.Icp == null) return false; Chunk best = null; float bd = 1e9f;
            foreach (var c in chunks)
            {
                if (c.dead || c.aimed || c.delay > 0) continue; var sp = cam.WorldToScreenPoint(c.go.transform.position); if (sp.z <= 0) continue;
                var ep = cam.WorldToScreenPoint(c.go.transform.position + cam.transform.right * c.size * 2.2f * RingK(c)); float rad = Mathf.Max(minPx, ((Vector2)ep - (Vector2)sp).magnitude * 1.1f);
                float d = ((Vector2)sp - pos).magnitude; if (d < rad && d < bd) { bd = d; best = c; }
            }
            if (best == null) return true; // Tippen ins Leere kostet nichts
            if (Game.Icp.rockets <= 0) { Game.Toast("Keine Abwehrraketen mehr!", "bad"); return true; }
            float k = RingK(best); bool hit = Mathf.Abs(k - 1) <= best.win; Shoot(best, hit);
            OnFeedback?.Invoke(best.go.transform.position, hit ? "Treffer!" : k > 1 ? "Zu früh – warte auf GRÜN" : "Zu spät", hit ? "good" : "bad"); return true;
        }
        class Shot { public Vector3 a; public Chunk c; public float t; public bool hit; public GameObject go; public Vector3 miss; }
        readonly List<Shot> shots = new List<Shot>();
        void Shoot(Chunk c, bool hit)
        {
            if (!Game.IcpShot(hit)) return; if (hit) c.aimed = true;
            var bc = pd.cells[pd.baseCell]; var a = FlakMuzzle(out _) ?? pd.Surf(bc.c, 3); flakAim = c;
            var g = new GameObject("Abwehrrakete"); g.transform.SetParent(transform, false); g.transform.position = a;
            var gl = MeshKit.Part(g.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(.7f, .9f, 1f, 1)), Vector3.zero, Vector3.one * pd.R * .08f); NoShadow(gl); gl.AddComponent<Billboard>();
            var tr = g.AddComponent<TrailRenderer>(); tr.time = .5f; tr.widthMultiplier = pd.R * .025f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)); tr.material = GlowMat(MeshKit.GlowTex(), new Color(.75f, .9f, 1f, 1)); tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shots.Add(new Shot { a = a, c = c, hit = hit, go = g, miss = Random.onUnitSphere * c.size * 4 }); Sfx.Play("rocket", 1.6f, .5f);
        }
        void UpdateShots()
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var s = shots[i]; s.t += Time.deltaTime / .35f; if (!s.go) { shots.RemoveAt(i); continue; }
                var tgt = (s.c.go ? s.c.go.transform.position : s.a) + (s.hit ? Vector3.zero : s.miss);
                s.go.transform.position = Vector3.Lerp(s.a, tgt, Mathf.Clamp01(s.t)) + s.a.normalized * Mathf.Sin(Mathf.PI * Mathf.Clamp01(s.t)) * 6;
                if (s.t < 1) continue;
                if (s.hit && !s.c.dead) { s.c.dead = true; Flash(tgt, new Color(.8f, .95f, 1f)); Sfx.Play("boom", 1.5f, .8f); Sfx.Play("crystal", 1.2f, .5f); if (dust) for (int q = 0; q < 24; q++) dust.Emit(new ParticleSystem.EmitParams { position = tgt, velocity = Random.onUnitSphere * Random.Range(6f, 18f), startColor = Color.Lerp(new Color(1f, .6f, .25f), new Color(.4f, .36f, .33f), Random.value), startSize = Random.Range(.5f, 1.4f), startLifetime = 1.6f }, 1); if (s.c.go) s.c.go.SetActive(false); }
                else if (!s.hit) { Sfx.Play("pop", .7f, .5f); }
                Destroy(s.go, .5f); shots.RemoveAt(i);
            }
        }

        // =====================================================================
        // Lieferrakete: landet kurz an der Basis, wenn Spezialgüter ankommen
        // =====================================================================
        readonly List<(GameObject go, Vector3 a, Vector3 b, float t)> supply = new List<(GameObject, Vector3, Vector3, float)>(); float lastSupply = -99;
        public void SupplyRocket(string gid, int n)
        {
            if (Time.time - lastSupply < 15) return; lastSupply = Time.time; var g0 = Game.Good(gid);
            var bc = pd.cells[pd.baseCell]; var land = pd.PlanDir(ps.lay.pos.ContainsKey("sell") ? ps.lay.pos["sell"] + new Vector2(5, 5) : Vector2.one * 6);
            var b = pd.Surf(land, 1.2f); var a = b + (b.normalized * .8f + bc.e1 * .6f).normalized * pd.R * 1.5f;
            var go = new GameObject("Lieferrakete"); go.transform.SetParent(transform, false);
            var body = MeshKit.Mat(new Color(.92f, .93f, .95f), .3f, .5f); var acc = MeshKit.Mat(g0.col, .2f, .5f, g0.col * .6f);
            if (!LcKit.Has("Ereignisse/lieferrakete") || !LcKit.Spawn("Ereignisse/lieferrakete", go.transform, Vector3.zero, 0, g0.col)) { MeshKit.Cyl(go.transform, body, new Vector3(0, 1.1f, 0), .5f, .55f, 2.2f, 12); MeshKit.Cyl(go.transform, acc, new Vector3(0, 2.6f, 0), 0, .5f, .9f, 12); MeshKit.Cyl(go.transform, acc, new Vector3(0, .5f, 0), .62f, .62f, .4f, 12); } /* Modell aus dem Blender-Skript, Akzent = Farbe des Guts */
            var fl = MeshKit.Part(go.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(1f, .7f, .35f, 1)), new Vector3(0, -.4f, 0), Vector3.one * 2f); NoShadow(fl); fl.AddComponent<Billboard>();
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            supply.Add((go, a, b, 0)); Sfx.Play("rocket", .8f, .4f);
        }
        void UpdateSupply()
        {
            for (int i = supply.Count - 1; i >= 0; i--)
            {
                var s = supply[i]; if (!s.go) { supply.RemoveAt(i); continue; } s.t += Time.deltaTime / 9f; supply[i] = s;
                // 0–0,4 Landeanflug, 0,4–0,6 steht (Ware wird ausgeladen), 0,6–1 Rückflug
                float k = s.t < .4f ? 1 - Mathf.SmoothStep(0, 1, s.t / .4f) : s.t < .6f ? 0 : Mathf.SmoothStep(0, 1, (s.t - .6f) / .4f);
                s.go.transform.position = Vector3.Lerp(s.b, s.a, k * k); s.go.transform.rotation = Quaternion.FromToRotation(Vector3.up, s.b.normalized);
                if (s.t >= 1) { Destroy(s.go); supply.RemoveAt(i); }
            }
        }

        // =====================================================================
        // Terraforming: Grünflächen, Moos, Gras und Sträucher, Teiche, Bäume, die sich im Wind wiegen
        // =====================================================================
        Transform terraRoot; int terraBuilt = -1; string terraFor; Material greenMat;
        class Tree { public Transform t; public Quaternion r0; public float ph, s, full = 1, grow, die = -1; public MeshRenderer crown; public bool young; }
        readonly List<Tree> trees = new List<Tree>();
        public static readonly Color TerraSky = new Color(.45f, .72f, 1f);
        /// <summary>Stufe hat sich geändert (Kauf oder Verfall).</summary>
        public void TerraChanged(int from, int to)
        {
            if (to < from) foreach (var tr in trees) { if (tr.die >= 0) continue; if (to < 4 || (to < 5 && !tr.young)) tr.die = 0; }
            BuildTerra(to > from);
        }
        void BuildTerra(bool grow)
        {
            terraFor = ps.pid; terraBuilt = ps.sv.terra; int st = ps.sv.terra;
            if (terraRoot) foreach (Transform c in terraRoot) if (c.name != "Bäume") Destroy(c.gameObject);
            if (!terraRoot) { terraRoot = New("Terraforming"); }
            var treeRoot = terraRoot.Find("Bäume"); if (!treeRoot) { treeRoot = new GameObject("Bäume").transform; treeRoot.SetParent(terraRoot, false); }
            trees.RemoveAll(t => !t.t); if (st <= 1 && trees.All(t => t.die >= 0)) { }
            if (st < 2) return;
            var rng = new System.Random(ps.pid.GetHashCode() & 0x7fffffff);
            var cells = pd.cells.Where(c => !c.IsBase && !c.Wet && c.ring >= 1 && !ps.sv.rigs.Any(r => r.cell == c.id) && !ps.sv.outposts.Any(o => o.cell == c.id)).OrderBy(c => c.ring).ThenBy(c => c.id).Take(320).ToList();
            if (cells.Count == 0) return;
            float Rad(Cell c) { float m = 1; if (c.poly != null) foreach (var q in c.poly) m = Mathf.Max(m, q.magnitude); return m; }
            Vector3 Spot(Cell c, float k) { float r = Rad(c) * k; return pd.ToDir(c, ((float)rng.NextDouble() * 2 - 1) * r, ((float)rng.NextDouble() * 2 - 1) * r); }
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            // 1) Grüne Flecken auf dem Boden (Sprites/Default, Vertexfarben) – eine Mesh
            {
                var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var t = new List<int>();
                float al = new[] { 0, 0, .4f, .6f, .7f, .8f }[st];
                foreach (var c in cells)
                {
                    var n = c.c.normalized; var p = pd.Surf(n, .06f); var e1 = c.e1 * Rad(c) * 1.25f; var e2 = c.e2 * Rad(c) * 1.25f; int b0 = v.Count;
                    v.Add(p - e1 - e2); v.Add(p + e1 - e2); v.Add(p + e1 + e2); v.Add(p - e1 + e2); uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                    var cc = Color.Lerp(new Color(.25f, .55f, .2f), new Color(.45f, .7f, .25f), (float)rng.NextDouble()); cc.a = al * R(.7f, 1); for (int q = 0; q < 4; q++) col.Add(cc);
                    t.AddRange(new[] { b0, b0 + 2, b0 + 1, b0, b0 + 3, b0 + 2 });
                }
                var m = new Mesh { name = "Grünflächen", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(col); m.SetTriangles(t, 0); m.RecalculateBounds();
                var g = new GameObject("Grünflächen"); g.transform.SetParent(terraRoot, false); g.AddComponent<MeshFilter>().sharedMesh = m; var mr = g.AddComponent<MeshRenderer>(); greenMat = new Material(Shader.Find("Sprites/Default")) { mainTexture = MeshKit.GlowTex() }; mr.sharedMaterial = greenMat; NoShadow(g);
            }
            // Hilfsfunktion: viele kleine Teile zu einer Mesh zusammenfassen
            void Combine(string name, List<CombineInstance> parts, Material mat) { if (parts.Count == 0) return; var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.CombineMeshes(parts.ToArray(), true, true); var g = new GameObject(name); g.transform.SetParent(terraRoot, false); g.AddComponent<MeshFilter>().sharedMesh = m; g.AddComponent<MeshRenderer>().sharedMaterial = mat; NoShadow(g); }
            Matrix4x4 At(Vector3 dir, float lift, float yaw, Vector3 sc) { var p = pd.Surf(dir, lift); var up = pd.SurfNormal(dir); var f = Vector3.Cross(up, Random.onUnitSphere).normalized; return Matrix4x4.TRS(p, Quaternion.LookRotation(f, up) * Quaternion.Euler(0, yaw, 0), sc); }
            // 2) Moos und Flechten: flache, weiche Polster
            {
                var parts = new List<CombineInstance>(); var parts2 = new List<CombineInstance>();
                foreach (var c in cells) for (int q = 0; q < 4; q++) { float s = R(.6f, 1.3f); var ci = new CombineInstance { mesh = RockKit.Mesh(rng.Next(RockKit.Variants), false), transform = At(Spot(c, .75f), -.04f, R(0, 360), new Vector3(s * 1.6f, s * .35f, s * 1.4f)) }; if (q % 3 != 2) parts.Add(ci); else parts2.Add(ci); }
                Combine("Moos", parts, MeshKit.Mat(new Color(.17f, .36f, .13f), 0, .15f)); Combine("Flechten", parts2, MeshKit.Mat(new Color(.5f, .55f, .27f), 0, .15f));
            }
            if (st >= 3)
            {
                // 3) Gras (Büschel aus schmalen Halmen) und Sträucher (runde Blattbälle)
                var blade = MeshKit.CylMesh(0, .09f, 1f, 3); var grass = new List<CombineInstance>(); var bush = new List<CombineInstance>(); var bush2 = new List<CombineInstance>();
                foreach (var c in cells)
                {
                    for (int q = 0; q < 8; q++) { var d = Spot(c, .85f); float hgt = R(.8f, 1.5f); for (int b = 0; b < 3; b++) { var m0 = At(d, .45f * hgt, R(0, 360), new Vector3(1, hgt, 1)); grass.Add(new CombineInstance { mesh = blade, transform = m0 * Matrix4x4.Rotate(Quaternion.Euler(R(-22, 22), 0, R(-22, 22))) }); } }
                    for (int q = 0; q < 2; q++) if (rng.NextDouble() < .6) { float s = R(.7f, 1.3f); var ci = new CombineInstance { mesh = RockKit.Mesh(rng.Next(RockKit.Variants), false), transform = At(Spot(c, .6f), s * .5f, R(0, 360), new Vector3(s, s * .8f, s)) }; if (rng.NextDouble() < .5) bush.Add(ci); else bush2.Add(ci); }
                }
                var lawn = new List<CombineInstance>(); foreach (var c in cells) for (int q = 0; q < 2; q++) { float s = R(1.8f, 2.8f); lawn.Add(new CombineInstance { mesh = RockKit.Mesh(rng.Next(RockKit.Variants), false), transform = At(Spot(c, .6f), -.03f, R(0, 360), new Vector3(s * 1.2f, .09f, s)) }); }
                lawn.Clear(); Combine("Rasen", lawn, MeshKit.Mat(new Color(.3f, .52f, .2f), 0, .2f)); /* beleuchtete grüne Fläche am Boden */
                /* Fabian 02.11.: Grasbüschel und Büsche aus dem Blender-Skript, sonst die einfachen Halme/Ballen */
                var gx = new List<Matrix4x4>(); var bx1 = new List<Matrix4x4>(); var bx2 = new List<Matrix4x4>();
                foreach (var c in cells) { for (int q = 0; q < 7; q++) { float hs = R(.9f, 1.5f); gx.Add(At(Spot(c, .85f), -.02f, R(0, 360), Vector3.one * hs)); } for (int q = 0; q < 2; q++) if (rng.NextDouble() < .55) { float bs = R(.8f, 1.4f); (q == 0 ? bx1 : bx2).Add(At(Spot(c, .7f), -.05f, R(0, 360), Vector3.one * bs)); } }
                if (CombineLc("Gras", "Natur/gras_1", gx) && CombineLc("Busch", "Natur/busch_1", bx1) && CombineLc("Busch", "Natur/busch_2", bx2)) { grass.Clear(); bush.Clear(); bush2.Clear(); }
                Combine("Gras", grass, MeshKit.Mat(new Color(.42f, .68f, .24f), 0, .25f)); Combine("Sträucher", bush, MeshKit.Mat(new Color(.2f, .48f, .2f), 0, .2f)); Combine("Sträucher hell", bush2, MeshKit.Mat(new Color(.36f, .58f, .18f), 0, .2f));
            }
            if (st >= 4)
            {
                // 4) Teiche: flache, glänzende Wasserflächen auf ebenen Parzellen
                var water = MeshKit.Mat(new Color(.16f, .42f, .7f), .1f, .93f); var shore = MeshKit.Mat(new Color(.5f, .45f, .33f), 0, .2f); int nw = 0;
                foreach (var c in cells.Where(c => c.ring >= 3).OrderBy(_ => rng.Next()))
                {
                    if (nw >= 8) break; var n = c.c.normalized; float h0 = pd.Height(n), hmax = h0, hmin = h0; float r = Rad(c) * .45f;
                    for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; float h = pd.Height(pd.ToDir(c, Mathf.Cos(a) * r, Mathf.Sin(a) * r)); hmax = Mathf.Max(hmax, h); hmin = Mathf.Min(hmin, h); }
                    if (hmax - hmin > .5f) continue; nw++;
                    var pond = new GameObject("Teich"); pond.transform.SetParent(terraRoot, false); pond.transform.position = n * (pd.R + hmin + .05f); pond.transform.rotation = Quaternion.FromToRotation(Vector3.up, n);
                    var w = MeshKit.Cyl(pond.transform, water, new Vector3(0, -.05f, 0), r, r, .1f + (hmax - hmin), 28); w.transform.localPosition = new Vector3(0, (hmax - hmin) * .5f - .05f, 0); NoShadow(w);
                    var sh = MeshKit.Cyl(pond.transform, shore, new Vector3(0, 0, 0), r + .5f, r + .7f, .08f + (hmax - hmin), 28); sh.transform.localPosition = new Vector3(0, (hmax - hmin) * .5f - .1f, 0); NoShadow(sh);
                    pond.transform.localScale = new Vector3(1, 1, R(.7f, 1));
                }
                // Bäume: Stufe 4 junge Bäume, Stufe 5 Wald
                int want = st >= 5 ? 170 : 55; int have = trees.Count(t => t.die < 0);
                foreach (var tr in trees) if (tr.die < 0 && st >= 5 && tr.young) { tr.young = false; }
                /* Haine: Bäume stehen in Gruppen, nicht gleichmäßig verstreut */
                var groves = cells.Where(c => c.ring >= 2).OrderBy(c => (c.id * 7919) % 1009).Take(Mathf.Max(4, want / 6)).ToList(); if (groves.Count == 0) groves = cells;
                for (int i = have; i < want; i++) { var c = groves[i % groves.Count]; SpawnTree(treeRoot, Spot(c, .8f), rng, st < 5, grow); }
            }
            if (st >= 4) Sfx.Play("wind", 1, .25f);
        }
        static Mesh[] crownMesh; static Mesh coneMesh;
        static Mesh Crown(int v)
        {
            if (crownMesh == null) crownMesh = new Mesh[3]; if (crownMesh[v]) return crownMesh[v];
            var rnd = new System.Random(77 + v * 13); var parts = new List<CombineInstance>(); int n = 4 + v;
            for (int i = 0; i < n; i++) { float a = i * 6.283f / n, d = i == 0 ? 0 : .55f, s = i == 0 ? .95f : .6f + (float)rnd.NextDouble() * .25f; parts.Add(new CombineInstance { mesh = RockKit.Mesh(i + v * 3, false), transform = Matrix4x4.TRS(new Vector3(Mathf.Cos(a) * d, 2.3f + (i == 0 ? .35f : (float)rnd.NextDouble() * .5f), Mathf.Sin(a) * d), Quaternion.Euler(0, a * 57, 0), Vector3.one * s) }); }
            var m = new Mesh { name = "Baumkrone " + v }; m.CombineMeshes(parts.ToArray(), true, true); m.RecalculateNormals(); crownMesh[v] = m; return m;
        }
        static readonly Dictionary<int, Mesh> treeMesh = new Dictionary<int, Mesh>();
        /// <summary>Ein Baum = eine Mesh mit zwei Teilen (0 Stamm, 1 Krone) – zwei Zeichenaufrufe je Baum. Varianten 0–2 Laubbaum, 3–4 Tanne.</summary>
        static Mesh TreeMesh(int v)
        {
            if (treeMesh.TryGetValue(v, out var m) && m) return m; bool fir = v >= 3;
            var trunk = new Mesh(); trunk.CombineMeshes(new[] { new CombineInstance { mesh = MeshKit.CylMesh(.1f, .2f, fir ? 1.8f : 2.2f, 8), transform = Matrix4x4.Translate(new Vector3(0, fir ? .9f : 1.1f, 0)) } }, true, true);
            var crown = new Mesh();
            if (fir) { var parts = new List<CombineInstance>(); for (int k = 0; k < 4; k++) parts.Add(new CombineInstance { mesh = MeshKit.CylMesh(0, 1.3f - k * .25f, 1.5f, 10), transform = Matrix4x4.Translate(new Vector3(0, 1.3f + k * .75f + (v == 4 ? .2f : 0), 0)) }); crown.CombineMeshes(parts.ToArray(), true, true); }
            else crown.CombineMeshes(new[] { new CombineInstance { mesh = Crown(v), transform = Matrix4x4.identity } }, true, true);
            m = new Mesh { name = "Baum " + v }; m.CombineMeshes(new[] { new CombineInstance { mesh = trunk, transform = Matrix4x4.identity }, new CombineInstance { mesh = crown, transform = Matrix4x4.identity } }, false, false); m.RecalculateBounds(); treeMesh[v] = m; return m;
        }
        void SpawnTree(Transform root, Vector3 dir, System.Random rng, bool young, bool grow)
        {
            var g = new GameObject("Baum"); g.transform.SetParent(root, false); var up = pd.SurfNormal(dir) * .3f + dir.normalized * .7f;
            g.transform.position = pd.Surf(dir, -.05f); g.transform.rotation = Quaternion.FromToRotation(Vector3.up, up.normalized) * Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0);
            bool fir = rng.NextDouble() < .4; MeshRenderer mr; float full;
            /* Fabian 02.11.: Bäume aus dem Blender-Skript (Natur/baum_laub_1–3, baum_tanne_1–2); ohne Modelle der einfache Ersatz */
            var bk = LcKit.Bake(fir ? "Natur/baum_tanne_" + (1 + rng.Next(2)) : "Natur/baum_laub_" + (1 + rng.Next(3)));
            if (bk != null && bk.mesh)
            {
                g.AddComponent<MeshFilter>().sharedMesh = bk.mesh; mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterials = bk.mats; full = (float)(.85 + rng.NextDouble() * .35);
            }
            else
            {
                int v = fir ? 3 + rng.Next(2) : rng.Next(3); var bark = MeshKit.Mat(new Color(.36f, .25f, .16f), 0, .2f);
                var leaf = fir ? MeshKit.Mat(new Color(.12f, .34f, .2f), 0, .25f) : MeshKit.Mat(new[] { new Color(.2f, .5f, .18f), new Color(.3f, .56f, .2f), new Color(.38f, .62f, .2f) }[rng.Next(3)], 0, .25f);
                g.AddComponent<MeshFilter>().sharedMesh = TreeMesh(v); mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { bark, leaf }; full = (float)(1.4 + rng.NextDouble() * .8);
            }
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float s = young ? full * .55f : full;
            var tr = new Tree { t = g.transform, r0 = g.transform.rotation, ph = (float)rng.NextDouble() * 6.283f, s = s, full = full, grow = grow ? 0 : 1, crown = mr, young = young };
            g.transform.localScale = Vector3.one * s * (grow ? .01f : 1); trees.Add(tr);
        }
        /// <summary>Viele kleine Teile aus einem Blender-Modell (je Material eine Mesh).</summary>
        bool CombineLc(string name, string path, List<Matrix4x4> xs)
        {
            var bk = LcKit.Bake(path); if (bk == null || !bk.mesh || xs.Count == 0) return false;
            for (int sm = 0; sm < bk.mesh.subMeshCount && sm < bk.mats.Length; sm++)
            {
                var parts = new List<CombineInstance>(xs.Count); foreach (var x in xs) parts.Add(new CombineInstance { mesh = bk.mesh, subMeshIndex = sm, transform = x });
                var m = new Mesh { name = name + " " + sm, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.CombineMeshes(parts.ToArray(), true, true);
                var g = new GameObject(name); g.transform.SetParent(terraRoot, false); g.AddComponent<MeshFilter>().sharedMesh = m; g.AddComponent<MeshRenderer>().sharedMaterial = bk.mats[sm]; NoShadow(g);
            }
            return true;
        }
        void UpdateTerra()
        {
            if (ps == null) return;
            if (terraFor != ps.pid || !terraRoot || (terraBuilt != ps.sv.terra && trees.All(t => t.die < 0))) { if (terraFor != ps.pid) { trees.Clear(); if (terraRoot) Destroy(terraRoot.gameObject); terraRoot = null; } BuildTerra(false); }
            if (greenMat) { float l = Mathf.Lerp(1f, .18f, NightF); greenMat.color = new Color(l, l, l, 1); } /* Grünflächen sind unbeleuchtet: nachts abdunkeln */
            if (trees.Count == 0) return; float tm = Time.time, wind = .9f + .6f * Mathf.Sin(tm * .13f) + (Game.StormOn(ps) ? 2.5f : 0);
            for (int i = trees.Count - 1; i >= 0; i--)
            {
                var tr = trees[i]; if (!tr.t) { trees.RemoveAt(i); continue; }
                if (tr.die >= 0)
                {
                    tr.die += Time.deltaTime / 4f; // Baum stirbt: Krone wird braun, kippt und versinkt
                    if (tr.crown) { var ms = tr.crown.sharedMaterials; var brown = MeshKit.Mat(new Color(.45f, .32f, .18f), 0, .2f); for (int q = 0; q < ms.Length; q++) if (ms.Length == 2 ? q == 1 : ms[q] && (ms[q].name.Contains("laub") || ms[q].name.Contains("tanne"))) ms[q] = brown; tr.crown.sharedMaterials = ms; tr.crown = null; } /* Krone wird braun */
                    tr.t.rotation = tr.r0 * Quaternion.Euler(tr.die * 70, 0, 0); tr.t.localScale = Vector3.one * tr.s * Mathf.Clamp01(1.4f - tr.die);
                    if (tr.die >= 1.4f) { Destroy(tr.t.gameObject); trees.RemoveAt(i); }
                    continue;
                }
                if (tr.grow < 1) { tr.grow = Mathf.Min(1, tr.grow + Time.deltaTime / 3f); tr.t.localScale = Vector3.one * tr.s * Mathf.SmoothStep(.01f, 1, tr.grow); }
                if (tr.young && ps.sv.terra >= 5) tr.young = false; float target = tr.young ? tr.full * .55f : tr.full; if (tr.s < target) { tr.s = Mathf.MoveTowards(tr.s, target, Time.deltaTime * .1f); tr.t.localScale = Vector3.one * tr.s * Mathf.SmoothStep(.01f, 1, tr.grow); }
                float a = wind * (2.2f + Mathf.Sin(tm * .7f + tr.ph * 2) * .8f);
                tr.t.rotation = tr.r0 * Quaternion.Euler(Mathf.Sin(tm * 1.3f + tr.ph) * a, 0, Mathf.Cos(tm * 1.05f + tr.ph * 1.7f) * a * .6f);
            }
        }
    }
}
