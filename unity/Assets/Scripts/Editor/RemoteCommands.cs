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
                    case "flash": { var ps = LunarCleanup.Core.Game.Cur; var w = Object.FindFirstObjectByType<LunarCleanup.Gameplay.WorldView>(); int best = 0; float bd = -2; for (int i = 0; i < ps.pd.N; i++) { if (ps.pd.cells[i].IsBase) continue; float d = Vector3.Dot(ps.pd.cells[i].c.normalized, LunarCleanup.Core.Game.ViewDir); if (d > bd) { bd = d; best = i; } } w.FlashCell(best); Write("ok flash " + best); break; }
                    case "basecheck": LunarCleanup.Gameplay.GameRoot.CheckNoPhotos = true; LunarCleanup.Gameplay.GameRoot.CheckReq = true; Write("ok basecheck"); break;
                    case "seedroutes": { int n = LunarCleanup.Gameplay.RouteCache.ExportSeed("Assets/Resources/Data/bandwege.txt"); AssetDatabase.Refresh(); Write("ok seedroutes " + n); break; }
                    case "wonder": { var w = Object.FindFirstObjectByType<LunarCleanup.Gameplay.WorldView>(); LunarCleanup.Gameplay.GameRoot.FocusReq = w.WonderDir; Write("ok wonder"); break; }
                    case "cleanall": LunarCleanup.Core.Game.DevCleanTo(1.01f); Write("ok cleanall"); break;
                    case "meteor": LunarCleanup.Core.Game.Meteor(LunarCleanup.Core.Game.Cur); Write("ok meteor"); break;
                    case "maxout": LunarCleanup.Core.Game.DevMaxOut(); Write("ok maxout"); break;
                    case "clean": LunarCleanup.Core.Game.DevCleanTo(.5f); Write("ok clean"); break;
                    case "comet": LunarCleanup.Gameplay.WorldView.CometNow = true; LunarCleanup.Gameplay.WorldView.FreighterNow = false; Write("ok comet"); break;
                    case "freighter": LunarCleanup.Gameplay.WorldView.CometNow = true; LunarCleanup.Gameplay.WorldView.FreighterNow = true; Write("ok freighter"); break;
                    case "night": { var cs = LunarCleanup.Core.Game.Cur.sv; cs.dayT = (cs.dayT < 0 ? 0 : cs.dayT) + LunarCleanup.Core.Game.DayLen(LunarCleanup.Core.Game.Cur.pid) / 4; Write("ok night"); break; }
                    case "wreck": case "impact": { var cs = LunarCleanup.Core.Game.Cur; if (cs.sv.ev != null) { cs.sv.ev.key = ""; cs.sv.ev.t = 0; } Write((LunarCleanup.Core.Game.StartEvent(cs, c) ? "ok " : "kein Platz ") + c); break; }
                    default:
                        if (c.StartsWith("gameview ")) { var a2 = c.Substring(9).Split(' '); Write(SetGameView(int.Parse(a2[0]), int.Parse(a2[1]))); break; }
                        if (c.StartsWith("rshot ")) { var cam = Camera.main; var rt = new RenderTexture(720, 1280, 24); var old = cam.targetTexture; float oa = cam.aspect; cam.targetTexture = rt; cam.aspect = 720f / 1280f; cam.Render(); var prev = RenderTexture.active; RenderTexture.active = rt; var tx = new Texture2D(720, 1280, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0); tx.Apply(); RenderTexture.active = prev; cam.targetTexture = old; cam.aspect = oa; cam.ResetAspect(); string f = "Temp/r_" + c.Substring(6) + ".jpg"; File.WriteAllBytes(f, tx.EncodeToJPG(88)); Object.DestroyImmediate(tx); rt.Release(); Write("ok " + f); break; }
                        if (c.StartsWith("shot")) { string f = "Temp/shot_" + (c.Length > 5 ? c.Substring(5) : "x") + ".png"; ScreenCapture.CaptureScreenshot(f, 2); Write("ok " + f); break; }
                        if (c.StartsWith("look ")) { var ps = LunarCleanup.Core.Game.Cur; var id = c.Substring(5); if (ps.lay.pos.ContainsKey(id)) { LunarCleanup.Gameplay.GameRoot.FocusReq = ps.pd.PlanDir(ps.lay.pos[id]); Write("ok " + c); } else Write("fehlt " + id); break; }
                        if (c.StartsWith("lang ")) { LunarCleanup.Core.Loc.Set(c.Substring(5) == "en"); Write("ok " + c); break; }
                        if (c.StartsWith("sheet ")) { var a = c.Substring(6).Split(' '); Object.FindFirstObjectByType<LunarCleanup.UI.Hud>().OpenSheet(a[0], a.Length > 1 ? a[1] : null); Write("ok " + c); break; }
                        if (c == "loctour") { var h = Object.FindFirstObjectByType<LunarCleanup.UI.Hud>(); int n = 0; foreach (var k in new[] { "vehicles", "production", "base", "upgrades", "planets", "goals", "energy", "research", "hazard", "outpost", "rig", "settings" }) for (int t = 0; t < 5; t++) { try { h.OpenSheet(k, t.ToString()); n++; } catch { } } foreach (var id in new System.Collections.Generic.List<string>(LunarCleanup.Core.Game.Cur.lay.pos.Keys)) { try { h.OpenSheet("building", id); n++; } catch { } } h.CloseSheet(); Write("ok loctour " + n); break; }
                        if (c == "locmiss") { var sb = new System.Text.StringBuilder(); foreach (var kv in LunarCleanup.Core.Loc.Seen) sb.Append(kv.Key.Replace("\n", "\\n")).Append("\t").Append(kv.Value.Replace("\n", "\\n")).Append("\n"); File.WriteAllText("Temp/locmiss.txt", sb.ToString()); Write("ok locmiss " + LunarCleanup.Core.Loc.Seen.Count); break; }
                        if (c == "start") { Object.FindFirstObjectByType<LunarCleanup.UI.Hud>().HideMenu(); Write("ok start"); break; }
                        if (c == "closesheet") { Object.FindFirstObjectByType<LunarCleanup.UI.Hud>().CloseSheet(); Write("ok " + c); break; }
                        if (c.StartsWith("zoom ")) { LunarCleanup.Gameplay.GameRoot.ZoomReq = float.Parse(c.Substring(5), System.Globalization.CultureInfo.InvariantCulture); Write("ok " + c); break; }
                        if (c.StartsWith("travel ")) { var pid = c.Substring(7); if (!LunarCleanup.Core.Game.P.ContainsKey(pid)) { LunarCleanup.Core.Game.S.credits += 1e12; LunarCleanup.Core.Game.Unlock(pid); } else LunarCleanup.Core.Game.Travel(pid); Write("ok " + c); break; }
                        if (c.StartsWith("menu ")) { bool ok = EditorApplication.ExecuteMenuItem(c.Substring(5)); Write((ok ? "ok " : "fehlt ") + c); }
                        else Write("unbekannt: " + c);
                        break;
                }
            }
            catch (System.Exception e) { Write("Fehler: " + e); }
        }
        /// <summary>Game-Ansicht auf eine feste Auflösung stellen (für Hochformat-Screenshots mit Oberfläche).</summary>
        static string SetGameView(int w, int h)
        {
            var asm = typeof(Editor).Assembly; var gvs = asm.GetType("UnityEditor.GameViewSizes"); var single = typeof(ScriptableSingleton<>).MakeGenericType(gvs);
            var inst = single.GetProperty("instance").GetValue(null); var grpType = (GameViewSizeGroupType)gvs.GetProperty("currentGroupType").GetValue(inst);
            var group = gvs.GetMethod("GetGroup").Invoke(inst, new object[] { (int)grpType }); var gType = group.GetType();
            int total = (int)gType.GetMethod("GetTotalCount").Invoke(group, null); int idx = -1;
            for (int i = 0; i < total; i++) { var sz = gType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i }); var st = sz.GetType(); if ((int)st.GetProperty("width").GetValue(sz) == w && (int)st.GetProperty("height").GetValue(sz) == h) { idx = i; break; } }
            if (idx < 0)
            {
                var szType = asm.GetType("UnityEditor.GameViewSize"); var enumType = asm.GetType("UnityEditor.GameViewSizeType");
                var ctor = szType.GetConstructor(new[] { enumType, typeof(int), typeof(int), typeof(string) }); var sz = ctor.Invoke(new object[] { System.Enum.Parse(enumType, "FixedResolution"), w, h, "LC " + w + "x" + h });
                gType.GetMethod("AddCustomSize").Invoke(group, new[] { sz }); idx = total;
            }
            var gvType = asm.GetType("UnityEditor.GameView"); var gv = EditorWindow.GetWindow(gvType);
            var prop = gvType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (prop != null) prop.SetValue(gv, idx); else gvType.GetMethod("SizeSelectionCallback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(gv, new object[] { idx, null });
            gv.Repaint(); return "ok gameview " + w + "x" + h + " #" + idx;
        }
        static void Write(string s) { try { File.WriteAllText(Out, System.DateTime.Now.ToString("HH:mm:ss") + " " + s); } catch { } }
    }
}
