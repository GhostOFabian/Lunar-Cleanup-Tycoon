// Kleine Lebenszeichen: Astronauten laufen zwischen den Gebäuden der Basis hin und her (nur Optik).
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        class Crew { public Transform t, legL, legR, box; public Vector2 a, b, cur, face; public float s, len, wait; public string job = ""; public int leg; public bool work; }
        readonly List<Crew> crew = new List<Crew>(); string crewFor; int crewVer = -1, crewSkin = -1;
        float carryUntil; Vector2 carryA, carryB;

        void BuildCrew()
        {
            foreach (var c in crew) if (c.t) Destroy(c.t.gameObject); crew.Clear(); crewFor = ps.pid; crewVer = ps.baseVersion; crewSkin = Platform.Version; bool gold = Platform.Supporter;
            var ids = ps.bld.Where(id => ps.lay.pos.ContainsKey(id) && !(Game.IsUnit(id) && id != "u1")).ToList(); if (ids.Count < 2) return;
            int n = Mathf.Clamp(ids.Count / 3, 2, 6);
            var suit = gold ? MeshKit.Mat(GameData.Hex("#e3b341"), .7f, .7f) : MeshKit.Mat(new Color(.93f, .94f, .96f), .1f, .4f); /* 11.11.: Unterstützer – goldene Anzüge */ var visor = MeshKit.Mat(new Color(.95f, .7f, .25f), .8f, .9f, new Color(.4f, .25f, .05f)); var pack = MeshKit.Mat(new Color(.55f, .58f, .64f), .4f, .5f);
            for (int i = 0; i < n; i++)
            {
                var g = new GameObject("Astronaut"); g.transform.SetParent(baseRoot, false); var t = g.transform;
                var lca = LcKit.Astronaut(t); if (lca) { lca.transform.localRotation = Quaternion.Euler(0, 180, 0) * lca.transform.localRotation; /* 10.11.: Modell schaut nach −Z – sonst läuft er rückwärts */ if (gold) GoldSuit(lca); var c0 = new Crew { t = t, wait = Random.Range(0f, 3f), cur = Spawn(ids, i) }; AddBox(c0, suit); NewTrip(c0, ids); crew.Add(c0); continue; } /* eigenes Modell (Fabian) */
                MeshKit.Cyl(t, suit, new Vector3(0, .62f, 0), .16f, .18f, .5f, 10);          // Rumpf
                MeshKit.Ball(t, suit, new Vector3(0, 1.02f, 0), .3f);                        // Helm
                MeshKit.Box(t, visor, new Vector3(0, 1.03f, .11f), new Vector3(.2f, .12f, .08f)); // Visier
                MeshKit.Box(t, pack, new Vector3(0, .68f, -.17f), new Vector3(.26f, .34f, .12f)); // Rucksack
                var lL = MeshKit.Cyl(t, suit, new Vector3(-.07f, .2f, 0), .06f, .06f, .38f, 8).transform; var lR = MeshKit.Cyl(t, suit, new Vector3(.07f, .2f, 0), .06f, .06f, .38f, 8).transform;
                var c = new Crew { t = t, legL = lL, legR = lR, wait = Random.Range(0f, 3f), cur = Spawn(ids, i) }; AddBox(c, suit); NewTrip(c, ids); crew.Add(c);
            }
        }
        /// <summary>Helle Anzugteile des eigenen Modells golden färben (Visier und dunkle Teile bleiben).</summary>
        static void GoldSuit(GameObject m)
        {
            var gold = GameData.Hex("#e3b341");
            foreach (var r in m.GetComponentsInChildren<Renderer>()) { var ms = r.materials; foreach (var mt in ms) if (mt.HasProperty("_BaseColor") || mt.HasProperty("_Color")) { var c = mt.color; if (c.r + c.g + c.b > 2.1f) mt.color = gold; } r.materials = ms; }
        }
        Vector2 Spawn(List<string> ids, int i) { var id = ids[i % ids.Count]; var d = Random.insideUnitCircle.normalized; return ps.lay.pos[id] + d * (FactoryPlanner.Half(id) + 1.6f); }
        /// <summary>Kiste vor der Brust – nur beim Verladen sichtbar.</summary>
        void AddBox(Crew c, Material m)
        {
            var g = MeshKit.Box(c.t, MeshKit.Mat(GameData.Hex("#ffbf4d"), .2f, .5f), new Vector3(0, .62f, .3f), new Vector3(.36f, .3f, .3f)); c.box = g.transform; g.SetActive(false);
        }
        /// <summary>10.11.: Besucher holt eine Bestellung ab – Kisten vom Produktlager zum Raumhafen bzw. zur Rakete tragen (30 s).</summary>
        public void CrewCarry()
        {
            if (ps == null) return; string from = ps.bld.Contains("prod") ? "prod" : "roh", to = ps.bld.Contains("sell") ? "sell" : ps.bld.Contains("port") ? "port" : null; /* Rakete steht direkt neben dem Produktlager */
            if (to == null || !ps.lay.pos.ContainsKey(from) || !ps.lay.pos.ContainsKey(to)) return;
            var pa = ps.lay.pos[from]; var pb = ps.lay.pos[to]; var d = (pb - pa).normalized; var side = new Vector2(-d.y, d.x) * 2.6f; /* neben dem Band, nicht darauf */
            carryA = pa + d * (FactoryPlanner.Half(from) + 1.2f) + side; carryB = pb - d * (FactoryPlanner.Half(to) + 1.2f) + side; carryUntil = Time.time + 30f;
        }
        /// <summary>11.11.: Crew trägt Kisten vom Produktlager zum Landeplatz (Händlerschiff wartet dort).</summary>
        public void CrewCarryTo(Vector2 to, float dur)
        {
            if (ps == null) return; string from = ps.bld.Contains("prod") ? "prod" : "roh"; if (!ps.lay.pos.ContainsKey(from)) return;
            var pa = ps.lay.pos[from]; var d = (to - pa).normalized; carryA = pa + d * (FactoryPlanner.Half(from) + 1.2f); carryB = to; carryUntil = Time.time + dur;
        }
        public string CrewInfo() => string.Join(" | ", crew.Select((c, i) => i + ":" + (c.job == "" ? "spaziert" : c.job) + (c.work ? " (arbeitet)" : "") + (c.box && c.box.gameObject.activeSelf ? " (Kiste)" : "")));
        void Go(Crew c, Vector2 to) { c.a = c.cur; c.b = to; c.len = Mathf.Max(.1f, (to - c.cur).magnitude); c.s = 0; c.wait = 0; c.work = false; }
        void NewTrip(Crew c, List<string> ids)
        {
            // vom aktuellen Platz zu einem anderen Gebäude – nur Wege, die kein Band und kein Gebäude kreuzen; sonst kurz stehen bleiben
            for (int k = 0; k < 16; k++)
            {
                string B = ids[Random.Range(0, ids.Count)]; var pb = ps.lay.pos[B]; var d = (pb - c.cur).normalized; var b = pb - d * (FactoryPlanner.Half(B) + 1.6f);
                if ((b - c.cur).magnitude < 2 || !CrewClear(c.cur, b)) continue; Go(c, b); return;
            }
            c.a = c.b = c.cur; c.len = .1f; c.s = 0; c.wait = 4;
        }
        /// <summary>Kreuzt die Strecke a→b ein Band oder ein Gebäude?</summary>
        bool CrewClear(Vector2 a, Vector2 b)
        {
            float L = (b - a).magnitude; int n = Mathf.Max(2, Mathf.CeilToInt(L / .4f));
            for (int i = 0; i <= n; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)n);
                foreach (var rp in ps.visBelt.Values) for (int k = 0; k < rp.Length - 1; k++) { var ab = rp[k + 1] - rp[k]; float t = Mathf.Clamp01(Vector2.Dot(p - rp[k], ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); if ((rp[k] + ab * t - p).magnitude < .95f) return false; }
                foreach (var kv in ps.lay.pos) if (ps.bld.Contains(kv.Key) && GameData.Bld.ContainsKey(kv.Key) && (kv.Value - p).magnitude < GameData.Bld[kv.Key].fp + .3f) return false;
            }
            return true;
        }
        public Vector3? CrewDir(int i) => i >= 0 && i < crew.Count && crew[i].t ? crew[i].t.position.normalized : (Vector3?)null;
        void UpdateCrew()
        {
            if (ps == null || MapMode) { foreach (var c in crew) if (c.t) c.t.gameObject.SetActive(false); return; }
            if (crewFor != ps.pid || crewVer != ps.baseVersion || crewSkin != Platform.Version) BuildCrew();
            var ids = ps.bld.Where(id => ps.lay.pos.ContainsKey(id) && !(Game.IsUnit(id) && id != "u1")).ToList(); if (ids.Count < 2) return;
            // Aufgaben: Techniker an Störungen, Kristallsammler, Kisten tragen – sonst spazieren
            var jobs = new List<(string key, Vector2 at)>();
            foreach (var jb in Game.TechJobs(ps)) jobs.Add(("t:" + jb.k + jb.id, pd.PlanLocal(jb.d)));
            if (ps.sv.crewCr >= 0) { var cx = ps.sv.crystals.Find(x => x.id == ps.sv.crewCr); if (cx != null) jobs.Add(("c:" + cx.id, pd.PlanLocal(Game.CrystalDir(ps, cx)))); }
            bool carry = Time.time < carryUntil; int ji = 0, carriers = 0;
            foreach (var c in crew)
            {
                if (!c.t) continue; c.t.gameObject.SetActive(true);
                string key = ""; Vector2 at = default;
                if (ji < jobs.Count) { key = jobs[ji].key; at = jobs[ji].at; ji++; } else if (carry && carriers < 3) { key = "k"; carriers++; }
                if (key != c.job)
                {
                    c.job = key; if (c.box) c.box.gameObject.SetActive(false);
                    if (key == "") NewTrip(c, ids);
                    else if (key == "k") { c.leg = 0; Go(c, carryA + Random.insideUnitCircle * .8f); }
                    else { var off = (c.cur - at); off = off.sqrMagnitude > 1e-4f ? off.normalized * 1.1f : Vector2.right * 1.1f; Go(c, at + off); c.face = -off; }
                }
                bool walking = c.wait <= 0 && c.s < c.len;
                if (c.wait > 0) c.wait -= Time.deltaTime;
                else if (c.s < c.len) { c.s += Time.deltaTime * (c.job == "k" ? 1.1f : Game.CREW_SPEED); }
                else if (c.job == "") { c.wait = Random.Range(2f, 6f); c.cur = c.b; NewTrip(c, ids); }
                else if (c.job == "k") { c.cur = c.b; if (c.leg == 0) { c.leg = 1; if (c.box) c.box.gameObject.SetActive(true); Go(c, carryB + Random.insideUnitCircle * .8f); } else { c.leg = 0; if (c.box) c.box.gameObject.SetActive(false); Go(c, carryA + Random.insideUnitCircle * .8f); } }
                else c.work = true; /* angekommen: reparieren bzw. ernten */
                if (c.s <= c.len) c.cur = Vector2.Lerp(c.a, c.b, Mathf.Clamp01(c.s / c.len));
                var dir = pd.PlanDir(c.cur); var mv = c.work ? c.face : (c.b - c.a); if (mv.sqrMagnitude < 1e-6f) mv = Vector2.up; var fwd = pd.PlanDir(c.cur + mv.normalized * .5f) - dir;
                float bob = c.work ? Mathf.Abs(Mathf.Sin(Time.time * 5 + c.len)) * .12f : walking && !c.legL ? Mathf.Abs(Mathf.Sin(Time.time * 7 + c.len)) * .08f : 0; /* Arbeiten: kniet und hämmert */
                c.t.position = pd.Surf(dir, bob); c.t.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(fwd, dir).normalized + dir * 1e-4f, dir);
                float sw = walking ? Mathf.Sin(Time.time * 7 + c.len) * 25 : 0; if (c.legL) c.legL.localRotation = Quaternion.Euler(sw, 0, 0); if (c.legR) c.legR.localRotation = Quaternion.Euler(-sw, 0, 0);
            }
        }
    }
}
