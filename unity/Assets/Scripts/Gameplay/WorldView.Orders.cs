// Fabian 02.11.: Bestelllager und gelandete Besucher-Shuttles auf dem Planeten, auf dem bestellt wurde.
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        Transform ordRoot; string ordFor; int ordVer = -1, ordCount = -1; GameObject ordDepot; Vector2 ordAt; readonly List<Transform> ordShips = new List<Transform>(); Transform ordCrates;
        void UpdateOrders()
        {
            if (ps == null) return; int n = Game.Orders.Count(o => o.pid == ps.pid);
            if (ordFor != ps.pid || ordVer != ps.baseVersion || ordCount != Mathf.Min(n, 3) || !ordRoot || (n > 0 && !ordDepot)) { if (builtVersion != ps.baseVersion) return; BuildOrders(n); }
            float t = Time.time;
            for (int i = 0; i < ordShips.Count; i++) if (ordShips[i]) { var up = ordShips[i].parent ? ordShips[i].parent.up : Vector3.up; ordShips[i].localPosition = new Vector3(ordShips[i].localPosition.x, 3.2f + .35f * Mathf.Sin(t * 1.3f + i * 2), ordShips[i].localPosition.z); }
            if (ordCrates) { float pr = 0; int k = 0; foreach (var o in Game.Orders) if (o.pid == ps.pid) { pr += Game.OrderProgress(o); k++; } float f = k > 0 ? pr / k : 0; ordCrates.localScale = Vector3.one * Mathf.Lerp(.15f, 1f, f); }
        }
        void BuildOrders(int n)
        {
            ordFor = ps.pid; ordVer = ps.baseVersion; ordCount = Mathf.Min(n, 3); ordShips.Clear(); ordCrates = null;
            if (ordRoot) Destroy(ordRoot.gameObject); ordRoot = New("Bestellungen"); ordDepot = null; if (n <= 0) return;
            Color acc = ps.P.col0; if (acc.grayscale > .8f) acc = GameData.Hex("#ffbf4d");
            Vector2 hq = ps.lay.pos.ContainsKey("hq") ? ps.lay.pos["hq"] : Vector2.zero, prod = ps.lay.pos.ContainsKey("prod") ? ps.lay.pos["prod"] : hq + Vector2.one * 8;
            var av = new List<Vector2>(); if (shieldGo) av.Add(shieldAt); if (flakGo) av.Add(flakAt);
            var sp = FreeSpot(prod + (prod - hq).normalized * 8, av.ToArray()); if (!sp.HasValue) return; ordAt = sp.Value;
            ordDepot = LcKit.Has("Welt/bestelllager") ? LcKit.Spawn("Welt/bestelllager", ordRoot, Vector3.zero, 0, acc) : null;
            if (!ordDepot)
            {
                ordDepot = new GameObject("Bestelllager"); ordDepot.transform.SetParent(ordRoot, false);
                var wall = MeshKit.Mat(new Color(.82f, .85f, .9f), .2f, .5f); var roof = MeshKit.Mat(acc, .3f, .45f);
                MeshKit.Box(ordDepot.transform, MeshKit.Mat(new Color(.29f, .31f, .38f), .2f, .3f), new Vector3(0, .08f, 0), new Vector3(4.4f, .16f, 4.4f));
                MeshKit.Box(ordDepot.transform, wall, new Vector3(0, 1.2f, 1.2f), new Vector3(4f, 2.2f, .2f)); MeshKit.Box(ordDepot.transform, wall, new Vector3(-1.9f, 1.2f, 0), new Vector3(.2f, 2.2f, 2.6f)); MeshKit.Box(ordDepot.transform, wall, new Vector3(1.9f, 1.2f, 0), new Vector3(.2f, 2.2f, 2.6f));
                MeshKit.Box(ordDepot.transform, roof, new Vector3(0, 2.4f, 0), new Vector3(4.2f, .2f, 2.8f));
            }
            var d = pd.PlanDir(ordAt); float hmax = pd.Height(d); for (int k = 0; k < 8; k++) { float a = k * Mathf.PI / 4; hmax = Mathf.Max(hmax, pd.Height(pd.PlanDir(ordAt + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * DEF_FP))); }
            ordDepot.transform.position = d * (pd.R + hmax); ordDepot.transform.rotation = PlanRot(d, 0) * ordDepot.transform.localRotation;
            // zurückgelegte Kisten: wachsen mit dem Fortschritt
            var cr = new GameObject("Kisten"); cr.transform.SetParent(ordDepot.transform, false); cr.transform.localPosition = new Vector3(0, .16f, -.2f); ordCrates = cr.transform;
            var cols = new[] { new Color(.85f, .64f, .25f), new Color(.36f, .56f, .85f), new Color(.6f, .82f, .77f) };
            for (int i = 0; i < 6; i++) MeshKit.Box(cr.transform, MeshKit.Mat(cols[i % 3], .1f, .4f), new Vector3((i % 3 - 1) * .8f, .35f + (i / 3) * .7f, 0), Vector3.one * .65f);
            // Besucher-Shuttles schweben über dem Lager
            for (int i = 0; i < ordCount; i++)
            {
                var holder = new GameObject("Besucher " + i); holder.transform.SetParent(ordDepot.transform, false); holder.transform.localPosition = new Vector3((i - (ordCount - 1) / 2f) * 4.5f, 3.2f, -4.5f);
                var shipModel = LcKit.Has("Ereignisse/haendlerschiff") ? LcKit.Spawn("Ereignisse/haendlerschiff", holder.transform, Vector3.zero, 90, acc, null, .45f) : (LcKit.Has("Ereignisse/lieferrakete") ? LcKit.Spawn("Ereignisse/lieferrakete", holder.transform, Vector3.zero, 0, acc, null, .9f) : null);
                if (!shipModel) MeshKit.Ball(holder.transform, MeshKit.Mat(new Color(.9f, .9f, .95f), .3f, .6f), Vector3.zero, 1.6f);
                var glow = MeshKit.Part(holder.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(.6f, .85f, 1f, .7f)), new Vector3(0, -.9f, 0), Vector3.one * 2.2f, new Vector3(90, 0, 0)); NoShadow(glow);
                ordShips.Add(holder.transform);
            }
        }
    }
}
