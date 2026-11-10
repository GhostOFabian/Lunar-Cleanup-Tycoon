// Editor-Hilfe (10.11.): Basisfläche jedes Layouts als Raster (1 m) für den Fabrik-Planer exportieren.
using System.Text;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public static string DevBaseMask()
        {
            var sb = new StringBuilder("{"); bool first = true;
            foreach (var k in GameData.Layouts.Keys)
            {
                string pid = k.Split('@')[0]; if (!GameData.Planets.ContainsKey(pid) || !GameData.Cells.ContainsKey(k)) continue;
                var pd = PlanetData.GetKey(pid, k); var row = new StringBuilder();
                for (int y = -44; y <= 44; y++) for (int x = -44; x <= 44; x++) { int c = pd.CellOf(pd.PlanDir(x, y)); row.Append(c >= 0 && pd.cells[c].IsBase ? '1' : '0'); }
                if (!first) sb.Append(','); first = false; sb.Append('"').Append(k).Append("\":\"").Append(row).Append('"');
            }
            sb.Append('}'); System.IO.File.WriteAllText("Temp/basemask.json", sb.ToString()); return "ok " + GameData.Layouts.Count;
        }
    }
}
