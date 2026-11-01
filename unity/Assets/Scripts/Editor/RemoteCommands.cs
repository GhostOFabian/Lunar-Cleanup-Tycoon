// Fernsteuerung für Claude, wenn der Mac gesperrt ist: Unity prüft jede Sekunde die Datei Temp/claude_cmd.txt
// und führt den Befehl darin aus (eine Zeile, z. B. "refresh", "build-sim", "build-device", "play", "stop", "check").
// Ergebnis steht danach in Temp/claude_out.txt. Reine Editor-Hilfe, kommt nicht ins Spiel.
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LunarCleanup.EditorTools
{
    [InitializeOnLoad]
    public static class RemoteCommands
    {
        const string Cmd = "Temp/claude_cmd.txt", Out = "Temp/claude_out.txt";
        static double next;
        static RemoteCommands() { EditorApplication.update += Poll; }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next) return; next = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(Cmd)) return;
            string c = File.ReadAllText(Cmd).Trim(); File.Delete(Cmd);
            Write("läuft: " + c);
            try
            {
                switch (c)
                {
                    case "refresh": AssetDatabase.Refresh(); Write("ok refresh"); break;
                    case "play": EditorApplication.isPlaying = true; Write("ok play"); break;
                    case "stop": EditorApplication.isPlaying = false; Write("ok stop"); break;
                    case "check": EditorApplication.ExecuteMenuItem("Lunar Cleanup/Eigene Modelle prüfen"); Write("ok check"); break;
                    case "build-sim": EditorApplication.ExecuteMenuItem("Lunar Cleanup/iPhone-Version für den Simulator bauen (Mac)"); Write("ok build-sim"); break;
                    case "build-device": EditorApplication.ExecuteMenuItem("Lunar Cleanup/iPhone-Version nur bauen (Xcode-Projekt)"); Write("ok build-device"); break;
                    /* Spielbefehle (nur im Play-Modus) */
                    case "meteor": LunarCleanup.Core.Game.Meteor(LunarCleanup.Core.Game.Cur); Write("ok meteor"); break;
                    case "maxout": LunarCleanup.Core.Game.DevMaxOut(); Write("ok maxout"); break;
                    case "clean": LunarCleanup.Core.Game.DevCleanTo(.5f); Write("ok clean"); break;
                    case "comet": LunarCleanup.Gameplay.WorldView.CometNow = true; LunarCleanup.Gameplay.WorldView.FreighterNow = false; Write("ok comet"); break;
                    case "freighter": LunarCleanup.Gameplay.WorldView.CometNow = true; LunarCleanup.Gameplay.WorldView.FreighterNow = true; Write("ok freighter"); break;
                    case "night": { var cs = LunarCleanup.Core.Game.Cur.sv; cs.dayT = (cs.dayT < 0 ? 0 : cs.dayT) + LunarCleanup.Core.Game.DayLen(LunarCleanup.Core.Game.Cur.pid) / 4; Write("ok night"); break; }
                    case "wreck": case "impact": { var cs = LunarCleanup.Core.Game.Cur; if (cs.sv.ev != null) { cs.sv.ev.key = ""; cs.sv.ev.t = 0; } Write((LunarCleanup.Core.Game.StartEvent(cs, c) ? "ok " : "kein Platz ") + c); break; }
                    default:
                        if (c.StartsWith("shot")) { string f = "Temp/shot_" + (c.Length > 5 ? c.Substring(5) : "x") + ".png"; ScreenCapture.CaptureScreenshot(f); Write("ok " + f); break; }
                        if (c.StartsWith("look ")) { var ps = LunarCleanup.Core.Game.Cur; var id = c.Substring(5); if (ps.lay.pos.ContainsKey(id)) { LunarCleanup.Gameplay.GameRoot.FocusReq = ps.pd.PlanDir(ps.lay.pos[id]); Write("ok " + c); } else Write("fehlt " + id); break; }
                        if (c.StartsWith("zoom ")) { LunarCleanup.Gameplay.GameRoot.ZoomReq = float.Parse(c.Substring(5), System.Globalization.CultureInfo.InvariantCulture); Write("ok " + c); break; }
                        if (c.StartsWith("travel ")) { var pid = c.Substring(7); if (!LunarCleanup.Core.Game.P.ContainsKey(pid)) { LunarCleanup.Core.Game.S.credits += 1e12; LunarCleanup.Core.Game.Unlock(pid); } else LunarCleanup.Core.Game.Travel(pid); Write("ok " + c); break; }
                        if (c.StartsWith("menu ")) { bool ok = EditorApplication.ExecuteMenuItem(c.Substring(5)); Write((ok ? "ok " : "fehlt ") + c); }
                        else Write("unbekannt: " + c);
                        break;
                }
            }
            catch (System.Exception e) { Write("Fehler: " + e); }
        }
        static void Write(string s) { try { File.WriteAllText(Out, System.DateTime.Now.ToString("HH:mm:ss") + " " + s); } catch { } }
    }
}
