// Bodenteile auf verschmutzten Parzellen (wie im Browser-Prototyp): Steine, Metallschrott, Satellitenteile, Kristalle,
// Fässer, Eis, Lavabrocken, Schleim. Jedes Teil liegt auf einem Feld des 32x32-Rasters und verschwindet, sobald dieses Feld
// sauber ist (mit kleiner Staubwolke). Gezeichnet wird per GPU-Instancing, nur die Abschnitte, die zur Kamera zeigen.
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        class DebItem { public int cell, gi; public Matrix4x4 m; public bool on; }
        class DebBatch { public Mesh mesh; public Material mat; public int chunk; public readonly List<DebItem> items = new List<DebItem>(); public Matrix4x4[] arr = new Matrix4x4[0]; public int n; public bool dirty = true; }
        readonly List<DebBatch> debBatches = new List<DebBatch>();
        readonly Dictionary<int, List<DebItem>> debByCell = new Dictionary<int, List<DebItem>>();
        readonly Dictionary<int, List<DebBatch>> debBatchOfCell = new Dictionary<int, List<DebBatch>>();
        readonly HashSet<int> debWatch = new HashSet<int>(); float debT; string debFor;
        static Mesh octa;

        static Mesh Octa()
        {
            if (octa) return octa;
            var v = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            int[] t = { 0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4, 1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2 };
            var vs = new List<Vector3>(); var tr = new List<int>(); for (int i = 0; i < t.Length; i++) { vs.Add(v[t[i]] * .5f); tr.Add(i); }
            octa = new Mesh { name = "Okta" }; octa.SetVertices(vs); octa.SetTriangles(tr, 0); octa.RecalculateNormals(); octa.RecalculateBounds(); return octa;
        }

        /// <summary>Form (Mesh + Grundmaß) und Material je Bodenteil-Art.</summary>
        (Mesh mesh, Vector3 size, Material mat) DebShape(DirtDef D)
        {
            switch (D.deb)
            {
                case "scrap": return (MeshKit.Cube, new Vector3(.6f, .14f, .32f), MeshKit.Mat(D.lightC, .55f, .5f));
                case "sat": return (MeshKit.Cube, new Vector3(.7f, .06f, .48f), MeshKit.Mat(GameData.Hex("#d9a63e"), .8f, .65f));
                case "crystal": case "sulfur": return (Octa(), new Vector3(.48f, 1.05f, .48f), MeshKit.Mat(D.lightC, .1f, .75f, D.lightC * .35f));
                case "barrel": return (MeshKit.CylMesh(.5f, .5f, 1f, 10), new Vector3(.44f, .5f, .44f), MeshKit.Mat(D.lightC * .8f, .4f, .4f));
                case "ice": return (Octa(), new Vector3(.64f, .64f, .64f), MeshKit.Mat(D.lightC, .1f, .85f));
                case "lava": return (MeshKit.Sphere, new Vector3(.68f, .5f, .62f), MeshKit.Mat(GameData.Hex("#2a2220"), 0, .1f, GameData.Hex("#ff5a1a") * .55f));
                case "goo": return (MeshKit.Sphere, new Vector3(.6f, .21f, .6f), MeshKit.Mat(D.lightC * .9f, .1f, .8f));
                default: return (MeshKit.Sphere, new Vector3(.6f, .42f, .54f), MeshKit.Mat(D.lightC, 0, .1f));
            }
        }

        void BuildDebris()
        {
            debBatches.Clear(); debByCell.Clear(); debBatchOfCell.Clear(); debWatch.Clear(); debFor = ps.pid;
            var keyed = new Dictionary<(int chunk, int type), DebBatch>(); var rnd = new System.Random(pd.N * 31 + 7); int G = GameData.GRID, CELLS = G * G;
            float R() => (float)rnd.NextDouble();
            foreach (var cl in pd.cells)
            {
                if (cl.IsBase || cl.Wet) continue; var D = GameData.Dirt[cl.type]; int n = GameData.Kinds[cl.kind].key == "industrial" ? 9 : 5; if (pd.N > 12000) n = (n + 2) / 3; else if (pd.N > 6000) n = (n + 1) / 2; /* große Planeten: weniger Teile je Parzelle */
                for (int k = 0; k < n; k++)
                {
                    int gi = -1; for (int t = 0; t < 20; t++) { int c = (int)(R() * CELLS); if (c >= CELLS) c = CELLS - 1; if (cl.inside[c]) { gi = c; break; } }
                    if (gi < 0) continue;
                    float x = -cl.H + ((gi % G) + .5f) * cl.cs + (R() - .5f) * .3f, y = -cl.H + ((gi / G) + .5f) * cl.cs + (R() - .5f) * .3f, yaw = R() * 360f, s = .7f + R() * .8f;
                    if (!keyed.TryGetValue((cl.chunk, cl.type), out var bt)) { var sh = DebShape(D); bt = new DebBatch { mesh = sh.mesh, mat = sh.mat, chunk = cl.chunk }; bt.mat.enableInstancing = true; keyed[(cl.chunk, cl.type)] = bt; debBatches.Add(bt); }
                    var dir = pd.ToDir(cl, x, y); var nrm = pd.SurfNormal(dir, cl.id); var size = DebShape(D).size * s;
                    var pos = pd.Surf(dir, size.y * .35f, cl.id); var rot = Quaternion.FromToRotation(Vector3.up, nrm) * Quaternion.Euler(0, yaw, 0);
                    var it = new DebItem { cell = cl.id, gi = gi, m = Matrix4x4.TRS(pos, rot, size) };
                    bt.items.Add(it);
                    if (!debByCell.TryGetValue(cl.id, out var li)) { li = new List<DebItem>(); debByCell[cl.id] = li; } li.Add(it);
                    if (!debBatchOfCell.TryGetValue(cl.id, out var lb)) { lb = new List<DebBatch>(); debBatchOfCell[cl.id] = lb; } if (!lb.Contains(bt)) lb.Add(bt);
                }
            }
            foreach (var id in debByCell.Keys) RefreshDebrisCell(id, true);
        }

        /// <summary>Ein Teil liegt, solange sein Rasterfeld noch verschmutzt ist (wie im Browser: Restschmutz über 30 %).</summary>
        void RefreshDebrisCell(int id, bool silent)
        {
            if (!debByCell.TryGetValue(id, out var L)) return; ps.act.TryGetValue(id, out var a); bool changed = false;
            foreach (var it in L)
            {
                bool on = !ps.clean[id] && (a == null || a.grid[it.gi] > .3f);
                if (on == it.on) continue;
                if (it.on && !on && !silent) EmitDust(it.m.GetColumn(3).normalized, GameData.Dirt[pd.cells[id].type].lightC, 6);
                it.on = on; changed = true;
            }
            if (changed && debBatchOfCell.TryGetValue(id, out var bs)) foreach (var b in bs) b.dirty = true;
        }

        void DrawDebris()
        {
            if (ps == null || MapMode) return;
            if (debFor != ps.pid || debBatches.Count == 0 && debByCell.Count == 0) BuildDebris();
            // Zellen, an denen gerade gearbeitet wird (Hand oder Fahrzeuge), regelmäßig nachsehen; frisch fertige Zellen einmal
            debT += Time.deltaTime;
            if (debT > .2f)
            {
                debT = 0; foreach (var id in ps.act.Keys) debWatch.Add(id);
                var done = new List<int>(); foreach (var id in debWatch) { RefreshDebrisCell(id, false); if (!ps.act.ContainsKey(id)) done.Add(id); } foreach (var id in done) debWatch.Remove(id);
            }
            var cam = Camera.main; if (!cam) return; var cdir = cam.transform.position.normalized;
            foreach (var b in debBatches)
            {
                if (b.chunk >= 0 && b.chunk < pd.faceCenters.Length && Vector3.Dot(pd.faceCenters[b.chunk].normalized, cdir) < -.15f) continue; /* Rückseite */
                if (b.dirty)
                {
                    b.n = 0; if (b.arr.Length < b.items.Count) b.arr = new Matrix4x4[b.items.Count];
                    foreach (var it in b.items) if (it.on) b.arr[b.n++] = it.m;
                    b.dirty = false;
                }
                for (int i = 0; i < b.n; i += 1023)
                {
                    int cnt = Mathf.Min(1023, b.n - i);
                    if (i == 0) Graphics.DrawMeshInstanced(b.mesh, 0, b.mat, b.arr, cnt, null, ShadowCastingMode.On, true);
                    else { var part = new Matrix4x4[cnt]; System.Array.Copy(b.arr, i, part, 0, cnt); Graphics.DrawMeshInstanced(b.mesh, 0, b.mat, part, cnt, null, ShadowCastingMode.On, true); }
                }
            }
        }
    }
}
