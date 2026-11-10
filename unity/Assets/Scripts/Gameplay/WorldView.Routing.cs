// 10.11.: Bandführung kommt aus dem Fabrik-Planer (Core/FactoryPlanner.cs, docs/FABRIK_PLANER.md).
// Die alte Wegsuche (A* mit Kreuzungs-Aufpreis), die Brücken und der Wege-Cache sind entfernt.
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        readonly Dictionary<string, Vector2[]> routed = new Dictionary<string, Vector2[]>();
        readonly Dictionary<string, string> sharedEnd = new Dictionary<string, string>(); // Zweigband → Band, auf dem es als Y endet
        string routedKey;
        const float GOFF = 39f; // Planfläche ±39 m (Verschieben)

        /// <summary>10.11.: Wege kommen aus dem Fabrik-Planer (Game.PlannedLayout) – hier nur noch übernehmen und die Y-Stücke den sichtbaren Bändern zuordnen.</summary>
        void RouteBelts()
        {
            routed.Clear(); sharedEnd.Clear(); routedKey = null;
            var live = ps.lay.belts.Where(x => Game.BeltLive(ps, x) && !Game.HiddenBelt(x) && x.rp != null && x.rp.Length >= 2).ToList(); var liveIds = new HashSet<string>(live.Select(x => x.id));
            foreach (var b in live) routed[b.id] = b.rp;
            if (ps.plan == null) return;
            // Zweige je Hauptband; läuft das Hauptband noch nicht, übernimmt der vorderste Zweig seine Strecke (er läuft ohnehin bis in den Eingang)
            foreach (var g in live.Where(b => ps.plan.merge.ContainsKey(b.id)).GroupBy(b => ps.plan.merge[b.id].trunk))
            {
                if (liveIds.Contains(g.Key)) { foreach (var b in g) sharedEnd[b.id] = g.Key; continue; }
                if (!ps.plan.cells.TryGetValue(g.Key, out var tc)) continue;
                var byUp = g.OrderBy(b => tc.IndexOf(ps.plan.merge[b.id].m)).ToList();
                for (int i = 1; i < byUp.Count; i++) sharedEnd[byUp[i].id] = byUp[0].id;
            }
        }

    }
}
