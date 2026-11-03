// Fabian 03.11.: Putzhelfer (Hard Mode) – kleine Figuren mit Besen, die zur nächsten Parzelle laufen und fegen.
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        readonly Dictionary<int, (Transform root, Transform broom)> helperViews = new Dictionary<int, (Transform, Transform)>(); string helperFor;
        void SyncHelpers()
        {
            if (helperFor != ps.pid) { foreach (var v in helperViews.Values) if (v.root) Destroy(v.root.gameObject); helperViews.Clear(); helperFor = ps.pid; }
            var alive = new HashSet<int>();
            foreach (var h in ps.sv.helpers)
            {
                alive.Add(h.id);
                if (!helperViews.TryGetValue(h.id, out var hv) || !hv.root)
                {
                    var g = new GameObject("Putzhelfer " + h.id); g.transform.SetParent(transform, false);
                    var body = LcKit.Astronaut(g.transform); if (body) body.transform.localScale *= .62f; else { var b0 = MeshKit.Ball(g.transform, MeshKit.Mat(new Color(.95f, .95f, .9f), 0, .5f), new Vector3(0, .5f, 0), .55f); NoShadow(b0); }
                    var broom = new GameObject("Besen").transform; broom.SetParent(g.transform, false); broom.localPosition = new Vector3(.35f, .55f, .25f);
                    MeshKit.Cyl(broom, MeshKit.Mat(new Color(.55f, .38f, .2f), 0, .3f), new Vector3(0, 0, 0), .03f, .03f, 1.1f, 6);
                    MeshKit.Box(broom, MeshKit.Mat(new Color(.9f, .75f, .3f), 0, .2f), new Vector3(0, -.55f, 0), new Vector3(.35f, .12f, .1f));
                    hv = (g.transform, broom); helperViews[h.id] = hv; hv.root.position = pd.Surf(h.d, 0);
                }
                var target = pd.Surf(h.d, 0); var mv = target - hv.root.position; var up = h.d.normalized;
                hv.root.position = Vector3.Lerp(hv.root.position, target, 1 - Mathf.Exp(-10 * Time.deltaTime));
                var fwd = Vector3.ProjectOnPlane(mv.sqrMagnitude > 1e-5f ? mv : hv.root.forward, up); if (fwd.sqrMagnitude > 1e-6f) hv.root.rotation = Quaternion.Slerp(hv.root.rotation, Quaternion.LookRotation(fwd.normalized, up), 1 - Mathf.Exp(-8 * Time.deltaTime));
                bool sweeping = h.cell >= 0 && pd.Arc(h.d, pd.cells[h.cell].c) <= 1.6f;
                hv.broom.localRotation = Quaternion.Euler(sweeping ? Mathf.Sin(Time.time * 9 + h.id) * 35 : 10, 0, 0);
                if (sweeping && dust && Random.value < Time.deltaTime * 6) EmitDust(h.d, GameData.Dirt[pd.cells[h.cell].type].lightC, 2);
            }
            var gone = new List<int>(); foreach (var k in helperViews.Keys) if (!alive.Contains(k)) gone.Add(k); foreach (var k in gone) { if (helperViews[k].root) Destroy(helperViews[k].root.gameObject); helperViews.Remove(k); }
        }
    }
}
