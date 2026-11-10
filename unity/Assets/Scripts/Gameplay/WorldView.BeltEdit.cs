// 10.11.: Der Band-Editor ist entfernt – der Fabrik-Planer (Core/FactoryPlanner.cs) verlegt alle Bänder selbst.
// Übrig bleibt: welches Band liegt unter dem Finger (Antippen zeigt seinen Namen).
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        static float SegDist(Vector2[] rp, Vector2 p) { float best = 1e9f; for (int i = 1; i < rp.Length; i++) { var a = rp[i - 1]; var d = rp[i] - a; float l2 = d.sqrMagnitude; float t = l2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, d) / l2) : 0; best = Mathf.Min(best, (a + d * t - p).magnitude); } return best; }
        /// <summary>Band unter diesem Planpunkt (für das Antippen).</summary>
        public bool BeltAt(Vector2 pl, out string id)
        {
            id = null; float best = 1.4f;
            foreach (var b in ps.lay.belts) { if (!Game.BeltLive(ps, b) || Game.HiddenBelt(b) || !routed.TryGetValue(b.id, out var r)) continue; float d = SegDist(r, pl); if (d < best) { best = d; id = b.id; } }
            return id != null;
        }
        public string BeltName(string id) { var b = ps.lay.belts.FirstOrDefault(x => x.id == id); if (b == null) return "Band"; string N(string bid) => GameData.Bld.ContainsKey(bid) ? (bid == "presse" || bid == "schmelze" || bid == "raff" ? GameData.MachName(ps.pid, bid) : GameData.Bld[bid].name) : bid; return N(b.from) + " → " + N(b.to); }
    }
}
