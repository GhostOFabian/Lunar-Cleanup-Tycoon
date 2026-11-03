// Fabian 03.11.: Gebäude frei verschieben (normales Spiel und Hard Mode) – überall im Basisgelände, mit Abstand zu anderen Gebäuden.
// Gespeichert je Planet (PlanetSave.bMove); beim Laden überschreibt das die Plätze aus dem Layout. Bänder werden danach neu verlegt.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        /// <summary>Eigene Gebäudeplätze ins Layout dieses Planeten übernehmen (eigene Kopie, das gemeinsame Layout bleibt unverändert).</summary>
        static void ApplyMoves(PState ps)
        {
            if (ps.sv.bMove == null) ps.sv.bMove = new List<BPosSave>(); if (ps.sv.bMove.Count == 0) return;
            var L = ps.lay; ps.lay = new LayoutDef { pos = new Dictionary<string, Vector2>(L.pos), yaw = new Dictionary<string, float>(L.yaw), shop = L.shop, start = L.start, park = L.park, belts = L.belts };
            foreach (var m in ps.sv.bMove)
            {
                if (!ps.lay.pos.ContainsKey(m.id)) continue; var p = new Vector2(m.x, m.y);
                if (Units.Contains(m.id)) { foreach (var u in Units) if (ps.lay.pos.ContainsKey(u)) ps.lay.pos[u] = p; } else ps.lay.pos[m.id] = p;
            }
        }
        public static string MoveKey(PState ps) => ps.sv.bMove == null || ps.sv.bMove.Count == 0 ? "" : "|mv" + string.Join(";", ps.sv.bMove.Select(m => m.id + ":" + m.x.ToString("0.0") + "," + m.y.ToString("0.0")));
        static string GroupId(string id) => Units.Contains(id) ? "u1" : id;
        /// <summary>Darf das Gebäude hierhin? "" = ja, sonst der Grund.</summary>
        public static string MoveWhy(PState ps, string id, Vector2 p)
        {
            if (!GameData.Bld.ContainsKey(id)) return "Unbekanntes Gebäude"; float fp = GameData.Bld[id].fp; var pd = ps.pd;
            for (int k = 0; k < 12; k++) { float a = k * Mathf.PI / 6; foreach (float rr in new[] { 0f, fp * .5f, fp }) { var q = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr; int c = pd.CellOf(pd.PlanDir(q)); if (c < 0 || !pd.cells[c].IsBase) return "Nur im Basisgelände (blau umrandet)"; if (pd.cells[c].Wet) return "Nicht auf Wasser oder Lava"; } }
            string g = GroupId(id);
            foreach (var kv in ps.lay.pos)
            {
                if (!GameData.Bld.ContainsKey(kv.Key) || GroupId(kv.Key) == g) continue; if (!ps.bld.Contains(kv.Key) && !(Units.Contains(kv.Key) && ps.bld.Contains("u1"))) continue; /* nur gebaute Gebäude */
                float min = fp + GameData.Bld[kv.Key].fp + 2f; if ((kv.Value - p).magnitude < min) return "Zu nah an " + GameData.Bld[kv.Key].name + " (mindestens 2 m Abstand)";
            }
            return "";
        }
        public static bool MoveBuilding(PState ps, string id, Vector2 p)
        {
            string w = MoveWhy(ps, id, p); if (w != "") return Fail(w); string g = GroupId(id);
            ps.sv.bMove.RemoveAll(m => GroupId(m.id) == g); ps.sv.bMove.Add(new BPosSave { id = g, x = p.x, y = p.y });
            ApplyMovesFresh(ps);
            ps.sv.bRoutes?.RemoveAll(r => { var b = ps.lay.belts.FirstOrDefault(x => x.id == r.id); return b != null && (GroupId(b.from) == g || GroupId(b.to) == g); }); /* eigene Bandwege an diesem Gebäude passen nicht mehr */
            ps.baseVersion++; Save(); Toast(GameData.Bld[id].name + " versetzt – die Bänder werden neu verlegt", "good"); return true;
        }
        public static void ResetBuilding(PState ps, string id)
        {
            string g = GroupId(id); if (ps.sv.bMove.RemoveAll(m => GroupId(m.id) == g) == 0) return; ApplyMovesFresh(ps);
            ps.sv.bRoutes?.RemoveAll(r => { var b = ps.lay.belts.FirstOrDefault(x => x.id == r.id); return b != null && (GroupId(b.from) == g || GroupId(b.to) == g); });
            ps.baseVersion++; Save(); Toast(GameData.Bld[id].name + " wieder am Standardplatz", "");
        }
        /// <summary>Neues Gebäude: steht an seinem Standardplatz schon ein verschobenes Gebäude, wird der nächste freie Platz im Basisgelände gesucht.</summary>
        static void FreeSpotIfBlocked(PState ps, string id)
        {
            if (ps.sv.bMove == null || ps.sv.bMove.Count == 0 || !ps.lay.pos.ContainsKey(id)) return; var p0 = ps.lay.pos[id]; if (MoveWhy(ps, id, p0) == "") return;
            for (float r = 2; r <= 40; r += 1.5f) for (int k = 0; k < 24; k++) { float a = k * Mathf.PI / 12; var p = p0 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; if (MoveWhy(ps, id, p) != "") continue; string g = GroupId(id); ps.sv.bMove.RemoveAll(m => GroupId(m.id) == g); ps.sv.bMove.Add(new BPosSave { id = g, x = p.x, y = p.y }); ApplyMovesFresh(ps); Toast(GameData.Bld[id].name + ": Standardplatz war belegt – steht jetzt daneben (Gebäude antippen → Verschieben)", ""); return; }
        }
        public static bool Moved(PState ps, string id) => ps.sv.bMove != null && ps.sv.bMove.Any(m => GroupId(m.id) == GroupId(id));
        /// <summary>Layout neu aus der Vorlage aufbauen und alle eigenen Plätze anwenden.</summary>
        static void ApplyMovesFresh(PState ps) { ps.lay = OneStation(GameData.Layouts[GameData.SizeKey(ps.pid, ps.sv.freq)]); ApplyMoves(ps); }
    }
}
