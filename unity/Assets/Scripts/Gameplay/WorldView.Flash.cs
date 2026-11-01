// Fertig gereinigte Parzelle leuchtet kurz grün auf (Rand + leichte Fläche), dann blendet es aus (Fabian 01.11.).
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        class CellFlash { public GameObject go; public Material ring, fill; public float t; }
        readonly List<CellFlash> flashes = new List<CellFlash>();
        static readonly Color FlashC = new Color(.35f, 1f, .55f);
        const float FlashDur = 1.4f;

        /// <summary>Grünes Aufleuchten über einer gerade fertig gereinigten Parzelle.</summary>
        public void FlashCell(int id)
        {
            if (pd == null || id < 0 || id >= pd.N || MapMode) return;
            var cl = pd.cells[id]; var poly = cl.poly; int K = poly.Length; if (K < 3) return;
            float w = Mathf.Max(.25f, cl.H * .045f); // Randbreite in Metern
            var cen = Vector2.zero; foreach (var p in poly) cen += p; cen /= K;
            var v = new List<Vector3>(); var tr = new List<int>(); var fv = new List<Vector3>(); var ft = new List<int>();
            Vector3 P(Vector2 q, float lift) => pd.Surf(pd.ToDir(cl, q.x, q.y), lift, id);
            fv.Add(P(cen, .06f));
            for (int k = 0; k < K; k++)
            {
                var o = poly[k]; var dir = (o - cen); float L = dir.magnitude; var i = L > w ? cen + dir * ((L - w) / L) : cen;
                v.Add(P(o, .07f)); v.Add(P(i, .07f)); fv.Add(P(i, .06f));
                int a = k * 2, b = ((k + 1) % K) * 2; tr.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                ft.AddRange(new[] { 0, 1 + k, 1 + (k + 1) % K });
            }
            var root = new GameObject("Aufleuchten"); root.transform.SetParent(transform, false);
            Mesh Make(List<Vector3> vs, List<int> ts) { var m = new Mesh(); m.SetVertices(vs); m.SetTriangles(ts, 0); m.RecalculateBounds(); return m; }
            var ring = new Material(MeshKit.Unlit(new Color(FlashC.r, FlashC.g, FlashC.b, 1), true)); ring.SetFloat("_Cull", 0);
            var fill = new Material(MeshKit.Unlit(new Color(FlashC.r, FlashC.g, FlashC.b, .3f), true)); fill.SetFloat("_Cull", 0);
            void Add(Mesh m, Material mat, string n) { var g = new GameObject(n); g.transform.SetParent(root.transform, false); g.AddComponent<MeshFilter>().sharedMesh = m; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
            Add(Make(fv, ft), fill, "Fläche"); Add(Make(v, tr), ring, "Rand");
            var lg = new GameObject("Licht"); lg.transform.SetParent(root.transform, false); lg.transform.position = pd.Surf(cl.c, 2f, id); var L0 = lg.AddComponent<Light>(); L0.type = LightType.Point; L0.color = FlashC; L0.range = cl.H * 2.2f; L0.intensity = 3; L0.shadows = LightShadows.None;
            flashes.Add(new CellFlash { go = root, ring = ring, fill = fill, t = 0 });
            if (flashes.Count > 12) { if (flashes[0].go) Destroy(flashes[0].go); flashes.RemoveAt(0); }
        }

        void UpdateFlashes()
        {
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                var f = flashes[i]; f.t += Time.deltaTime; float k = f.t / FlashDur;
                if (k >= 1 || !f.go) { if (f.go) Destroy(f.go); flashes.RemoveAt(i); continue; }
                float a = k < .15f ? k / .15f : 1 - (k - .15f) / .85f; a = Mathf.Clamp01(a); // schnell an, langsam aus
                f.ring.SetColor("_BaseColor", new Color(FlashC.r, FlashC.g, FlashC.b, a));
                f.fill.SetColor("_BaseColor", new Color(FlashC.r, FlashC.g, FlashC.b, .3f * a));
                var L = f.go.GetComponentInChildren<Light>(); if (L) L.intensity = 3 * a;
            }
        }
    }
}
