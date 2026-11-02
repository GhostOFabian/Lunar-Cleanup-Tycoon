// Fernsteuerung für Claude, wenn der Mac gesperrt ist: Unity prüft jede Sekunde die Datei Temp/claude_cmd.txt
// und führt den Befehl darin aus (eine Zeile, z. B. "refresh", "build-sim", "build-device", "play", "stop", "check").
// Ergebnis steht danach in Temp/claude_out.txt. Reine Editor-Hilfe, kommt nicht ins Spiel.
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LunarCleanup.EditorTools
{
    [System.Serializable] public class Wrap { public string o; }
    [InitializeOnLoad]
    public static class RemoteCommands
    {
        const string Cmd = "Temp/claude_cmd.txt", Out = "Temp/claude_out.txt";
        static double next;
        static RemoteCommands()
        {
            EditorApplication.update += Poll;
            /* Fehler/Exceptions aus dem Spiel für Claude in Temp/errlog.txt */
            Application.logMessageReceived += (msg, st, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) try { File.AppendAllText("Temp/errlog.txt", System.DateTime.Now.ToString("HH:mm:ss ") + type + ": " + msg + "\n" + st + "\n"); } catch { } };
            /* Kompilierfehler für Claude in Temp/compile_err.txt (leer = ok) */
            UnityEditor.Compilation.CompilationPipeline.compilationStarted += _ => { try { File.WriteAllText("Temp/compile_err.txt", "compiling\n"); } catch { } };
            UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished += (asm, msgs) => { try { foreach (var m in msgs) if (m.type == UnityEditor.Compilation.CompilerMessageType.Error) File.AppendAllText("Temp/compile_err.txt", m.message + "\n"); } catch { } };
            UnityEditor.Compilation.CompilationPipeline.compilationFinished += _ => { try { File.AppendAllText("Temp/compile_err.txt", "done\n"); } catch { } };
        }

        static System.Collections.IEnumerator ImplodeTest(LunarCleanup.Gameplay.WorldView w, Transform pt) { w.StartCoroutine(ImplodeShots()); yield return w.Implode(pt); yield return new WaitForSeconds(1.5f); w.RestoreImploded(); }
        static System.Collections.IEnumerator ImplodeShots() { for (int i = 0; i < 18; i++) { yield return new WaitForSeconds(.45f); ScreenCapture.CaptureScreenshot("Temp/imp_" + i.ToString("00") + ".png", 1); } }
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
                    case "build-web": EditorApplication.ExecuteMenuItem("Lunar Cleanup/Browser-Version für GitHub bauen"); Write("ok build-web"); break;
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
                    case "flags": { var cs = LunarCleanup.Core.Game.Cur; int k = 0; foreach (var f in cs.sv.flags) { if (k >= 3) break; if (!f.got) { cs.clean[f.cell] = true; LunarCleanup.Core.Game.CollectFlag(cs, f); k++; } } Write("ok " + c); break; }
                    case "doom": { LunarCleanup.Core.Game.StartDoom(LunarCleanup.Core.Game.Cur.pid); LunarCleanup.Core.Game.S.doomT = 20; Write("ok doom"); break; }
                    case "putzi": LunarCleanup.Core.Game.S.putzi = true; Write("ok putzi"); break;
                    case "wreck": case "impact": case "pile": case "trader": case "solar": { var cs = LunarCleanup.Core.Game.Cur; if (cs.sv.ev != null) { cs.sv.ev.key = ""; cs.sv.ev.t = 0; } Write((LunarCleanup.Core.Game.StartEvent(cs, c) ? "ok " : "kein Platz ") + c); break; }
                    default:
                        if (c.StartsWith("gameview ")) { var a2 = c.Substring(9).Split(' '); Write(SetGameView(int.Parse(a2[0]), int.Parse(a2[1]))); break; }
                        if (c.StartsWith("rshot ")) { var cam = Camera.main; var rt = new RenderTexture(720, 1280, 24); var old = cam.targetTexture; float oa = cam.aspect; cam.targetTexture = rt; cam.aspect = 720f / 1280f; cam.Render(); var prev = RenderTexture.active; RenderTexture.active = rt; var tx = new Texture2D(720, 1280, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0); tx.Apply(); RenderTexture.active = prev; cam.targetTexture = old; cam.aspect = oa; cam.ResetAspect(); string f = "Temp/r_" + c.Substring(6) + ".jpg"; File.WriteAllBytes(f, tx.EncodeToJPG(88)); Object.DestroyImmediate(tx); rt.Release(); Write("ok " + f); break; }
                        if (c.StartsWith("shot")) { string f = "Temp/shot_" + (c.Length > 5 ? c.Substring(5) : "x") + ".png"; ScreenCapture.CaptureScreenshot(f, 2); Write("ok " + f); break; }
                        if (c.StartsWith("lookdirty")) { var ps = LunarCleanup.Core.Game.Cur; int want = c.Length > 10 ? int.Parse(c.Substring(10)) : 0; int k = 0; for (int i = 0; i < ps.pd.N; i++) { var cl = ps.pd.cells[i]; if (cl.IsBase || cl.Wet || ps.clean[i] || cl.ring < 2) continue; if (k++ < want) continue; LunarCleanup.Gameplay.GameRoot.FocusReq = cl.c; Write("ok " + c + " " + LunarCleanup.Core.GameData.Dirt[cl.type].key); break; } break; }
                        if (c.StartsWith("look ")) { var ps = LunarCleanup.Core.Game.Cur; var id = c.Substring(5); if (ps.lay.pos.ContainsKey(id)) { LunarCleanup.Gameplay.GameRoot.FocusReq = ps.pd.PlanDir(ps.lay.pos[id]); Write("ok " + c); } else Write("fehlt " + id); break; }
                        if (c.StartsWith("lang ")) { LunarCleanup.Core.Loc.Set(c.Substring(5) == "en"); Write("ok " + c); break; }
                        if (c.StartsWith("sheet ")) { var a = c.Substring(6).Split(' '); Object.FindFirstObjectByType<LunarCleanup.UI.Hud>().OpenSheet(a[0], a.Length > 1 ? a[1] : null); Write("ok " + c); break; }
                        if (c == "loctour") { var h = Object.FindFirstObjectByType<LunarCleanup.UI.Hud>(); int n = 0; foreach (var k in new[] { "vehicles", "production", "base", "upgrades", "planets", "goals", "energy", "research", "hazard", "outpost", "rig", "settings" }) for (int t = 0; t < 5; t++) { try { h.OpenSheet(k, t.ToString()); n++; } catch { } } foreach (var id in new System.Collections.Generic.List<string>(LunarCleanup.Core.Game.Cur.lay.pos.Keys)) { try { h.OpenSheet("building", id); n++; } catch { } } h.CloseSheet(); Write("ok loctour " + n); break; }
                        if (c == "locmiss") { var sb = new System.Text.StringBuilder(); foreach (var kv in LunarCleanup.Core.Loc.Seen) sb.Append(kv.Key.Replace("\n", "\\n")).Append("\t").Append(kv.Value.Replace("\n", "\\n")).Append("\n"); File.WriteAllText("Temp/locmiss.txt", sb.ToString()); Write("ok locmiss " + LunarCleanup.Core.Loc.Seen.Count); break; }
                        if (c == "start") { Object.FindFirstObjectByType<LunarCleanup.UI.Hud>().HideMenu(); Write("ok start"); break; }
                        if (c == "closesheet") { Object.FindFirstObjectByType<LunarCleanup.UI.Hud>().CloseSheet(); Write("ok " + c); break; }
                        if (c.StartsWith("zoom ")) { LunarCleanup.Gameplay.GameRoot.ZoomReq = float.Parse(c.Substring(5), System.Globalization.CultureInfo.InvariantCulture); Write("ok " + c); break; }
                        if (c == "resetsave") { string k = "lunar-cleanup-tycoon-unity-v1" + (LunarCleanup.Core.Game.TestMode ? "-test" : ""); LunarCleanup.Core.Game.Save(); if (!PlayerPrefs.HasKey(k + "-claudebak")) PlayerPrefs.SetString(k + "-claudebak", PlayerPrefs.GetString(k, "")); LunarCleanup.Core.Game.ResetAll(); Write("ok resetsave test=" + LunarCleanup.Core.Game.TestMode); break; }
                        if (c == "restoresave") { string k = "lunar-cleanup-tycoon-unity-v1" + (LunarCleanup.Core.Game.TestMode ? "-test" : ""); string b = PlayerPrefs.GetString(k + "-claudebak", ""); if (b != "") { PlayerPrefs.SetString(k, b); PlayerPrefs.DeleteKey(k + "-claudebak"); PlayerPrefs.Save(); LunarCleanup.Core.Game.S = null; EditorApplication.isPlaying = false; } Write("ok restoresave " + (b != "")); break; }
                        if (c == "oldsave") /* Spielstand wie aus Build 19: alle neueren Felder entfernen, dann neu laden */
                        {
                            string k = "lunar-cleanup-tycoon-unity-v1" + (LunarCleanup.Core.Game.TestMode ? "-test" : ""); LunarCleanup.Core.Game.Save(); string j = PlayerPrefs.GetString(k, "");
                            if (!PlayerPrefs.HasKey(k + "-claudebak")) PlayerPrefs.SetString(k + "-claudebak", j);
                            int n0 = j.Length; foreach (var key in new[] { "flags", "flagsInit", "tFirst", "tDoneAt", "tPlay", "tDone", "prize", "debt", "lineCol", "comboMax", "meteors", "salvage", "storms", "geysers", "nightMin", "achievements", /* Build 29 */ "putzi", "ruin", "ruinUntil", "doomNext", "doomT", "doomPid", "incH", "incHour", "chal", "cellD", "cellDay", "sellRate", "amt0", /* Build 34 */ "msLv", "flakLv", "msCool", "terra", "terraHp", "gAcc", "upAcc", "hitT", "supOk", "bRoutes", "goods", "doomN", "doomHits", /* Build 38 */ "vorders", "voT", "voLv", "voNext", "offPend" }) j = StripKey(j, key);
                            j = j.Replace("\"cart\":{\"owned\":false", "\"cart\":{\"owned\":true");
                            PlayerPrefs.SetString(k, j); PlayerPrefs.Save(); LunarCleanup.Core.Game.S = null; EditorApplication.isPlaying = false; Write("ok oldsave " + n0 + " -> " + j.Length); break;
                        }
                        if (c == "evinfo") { var q = LunarCleanup.Core.Game.Cur; var ev = q.sv.ev; bool has = ev != null && ev.cell >= 0 && q.act.ContainsKey(ev.cell); Write("ok evinfo key=" + ev?.key + " t=" + ev?.t + " cell=" + ev?.cell + " amt0=" + ev?.amt0 + " act=" + has + (has ? " sum=" + q.act[ev.cell].sum + " nIn=" + q.act[ev.cell].cl.nIn : "") + " clean=" + (ev != null && ev.cell >= 0 ? q.clean[ev.cell].ToString() : "")); break; }
                        if (c == "rig") { var ps = LunarCleanup.Core.Game.Cur; LunarCleanup.Core.Game.BuildRig(-1); LunarCleanup.Core.Game.BuildRig(-1); if (ps.sv.rigs.Count > 0) ps.sv.rigs[ps.sv.rigs.Count - 1].bag = true; ps.baseVersion++; Write("ok rig " + ps.sv.rigs.Count); break; }
                        if (c.StartsWith("goods ")) { float n = float.Parse(c.Substring(6), System.Globalization.CultureInfo.InvariantCulture); foreach (var g in LunarCleanup.Core.Game.Goods) LunarCleanup.Core.Game.AddGood(g.id, n); Write("ok " + c); break; }
                        if (c.StartsWith("shield ")) { var ps = LunarCleanup.Core.Game.Cur; ps.sv.msLv = int.Parse(c.Substring(7)); ps.sv.msCool = 0; ps.sv.energy = LunarCleanup.Core.Game.EnergyCap(ps); Write("ok " + c + " chance=" + LunarCleanup.Core.Game.ShieldChance(ps)); break; }
                        if (c.StartsWith("flak ")) { var ps = LunarCleanup.Core.Game.Cur; ps.sv.flakLv = int.Parse(c.Substring(5)); Write("ok " + c + " rockets=" + LunarCleanup.Core.Game.FlakRockets(ps)); break; }
                        if (c.StartsWith("auto ")) { LunarCleanup.Gameplay.WorldView.AutoPlay = int.Parse(c.Substring(5)); Write("ok " + c); break; }
                        if (c.StartsWith("terra ")) { var a = c.Substring(6).Split(' '); LunarCleanup.Core.Game.DevTerra(int.Parse(a[0]), a.Length > 1 ? float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture) : 1); Write("ok " + c); break; }
                        if (c.StartsWith("savebak ")) { string k = "lunar-cleanup-tycoon-unity-v1" + (LunarCleanup.Core.Game.TestMode ? "-test" : ""); LunarCleanup.Core.Game.Save(); PlayerPrefs.SetString(k + "-cb-" + c.Substring(8), PlayerPrefs.GetString(k, "")); PlayerPrefs.Save(); Write("ok " + c + " " + PlayerPrefs.GetString(k, "").Length); break; }
                        if (c.StartsWith("loadbak ")) { string k = "lunar-cleanup-tycoon-unity-v1" + (LunarCleanup.Core.Game.TestMode ? "-test" : ""); string b = PlayerPrefs.GetString(k + "-cb-" + c.Substring(8), ""); if (b == "") { Write("fehlt " + c); break; } PlayerPrefs.SetString(k, b); PlayerPrefs.Save(); LunarCleanup.Core.Game.S = null; EditorApplication.isPlaying = false; Write("ok " + c + " (gestoppt, neu starten)"); break; }
                        if (c == "savesize") { var sb = new System.Text.StringBuilder("ok savesize total=" + JsonUtility.ToJson(LunarCleanup.Core.Game.S).Length); var tot = new System.Collections.Generic.Dictionary<string, long>(); foreach (var pl in LunarCleanup.Core.Game.S.planets) foreach (var f in typeof(LunarCleanup.Core.PlanetSave).GetFields()) { var v = f.GetValue(pl); if (v == null) continue; long n = v is string sv ? sv.Length : (v.GetType().IsPrimitive ? 4 : JsonUtility.ToJson(new LunarCleanup.EditorTools.Wrap { o = v.ToString() }).Length); if (v is System.Collections.IList li) { n = 0; foreach (var e in li) n += e == null ? 4 : (e.GetType().IsPrimitive ? 6 : JsonUtility.ToJson(e).Length); } else if (v is System.Array ar) n = ar.Length * 8; tot[f.Name] = (tot.TryGetValue(f.Name, out var t0) ? t0 : 0) + n; } foreach (var kv in System.Linq.Enumerable.Take(System.Linq.Enumerable.OrderByDescending(tot, x => x.Value), 10)) sb.Append(" " + kv.Key + "=" + kv.Value); Write(sb.ToString()); break; }
                        if (c == "actinfo") { var sb = new System.Text.StringBuilder("ok actinfo"); foreach (var q in LunarCleanup.Core.Game.P.Values) { int n = 0, nc = 0, z = 0, sm = 0; int gl = 0; foreach (var kv in q.act) { n++; gl = kv.Value.grid.Length; if (q.clean[kv.Key]) { nc++; if (kv.Value.sum < 1e-3f) z++; else if (kv.Value.sum < gl * .02f) sm++; } } sb.Append(" " + q.pid + ":" + n + "/c" + nc + "/z" + z + "/s" + sm + "/g" + gl); } Write(sb.ToString()); break; }
                        if (c.StartsWith("beltdraw ")) { var w = Object.FindFirstObjectByType<LunarCleanup.Gameplay.WorldView>(); var a = c.Substring(9).Split(' '); var inv = System.Globalization.CultureInfo.InvariantCulture; var pts = new System.Collections.Generic.List<Vector2>(); for (int i = 0; i + 1 < a.Length; i += 2) pts.Add(new Vector2(float.Parse(a[i], inv), float.Parse(a[i + 1], inv))); w.DrawMode = true; w.DrawBegin(pts[0]); for (int i = 1; i < pts.Count; i++) { var p0 = pts[i - 1]; var p1 = pts[i]; int n = Mathf.CeilToInt((p1 - p0).magnitude / .5f); for (int k = 1; k <= n; k++) w.DrawAdd(Vector2.Lerp(p0, p1, k / (float)n)); } w.DrawEnd(); Write("ok beltdraw n=" + w.EditCount); break; }
                        if (c.StartsWith("bpos ")) { var ps = LunarCleanup.Core.Game.Cur; var sb = new System.Text.StringBuilder("ok bpos"); foreach (var id in c.Substring(5).Split(' ')) if (ps.lay.pos.ContainsKey(id)) sb.Append(" " + id + "=" + ps.lay.pos[id].x.ToString("0.0") + "/" + ps.lay.pos[id].y.ToString("0.0")); Write(sb.ToString()); break; }
                        if (c == "implode") { var w = Object.FindFirstObjectByType<LunarCleanup.Gameplay.WorldView>(); var pv = Object.FindFirstObjectByType<LunarCleanup.Gameplay.PlanetView>(); w.StartCoroutine(ImplodeTest(w, pv.transform)); Write("ok implode"); break; }
                        if (c == "pend") { Write("ok pend n=" + LunarCleanup.Core.Game.PendingPlanets + " " + LunarCleanup.Core.Game.DevPend() + " cr=" + LunarCleanup.Core.Game.S.credits.ToString("0")); break; }
                        if (c.StartsWith("orders")) { if (c.Length > 7) LunarCleanup.Core.Game.DevOrders(int.Parse(c.Substring(7))); Write("ok orders max=" + LunarCleanup.Core.Game.OrderMax() + " " + LunarCleanup.Core.Game.DevOrderInfo()); break; }
                        if (c == "doomhere") { var G = LunarCleanup.Core.Game.S; G.doomT = -1; G.doomPid = ""; LunarCleanup.Core.Game.StartDoom(LunarCleanup.Core.Game.Cur.pid); G.doomT = 13; Write("ok doomhere " + G.doomPid); break; }
                        if (c == "doomother") { var G = LunarCleanup.Core.Game.S; G.doomT = -1; G.doomPid = ""; var o = System.Linq.Enumerable.FirstOrDefault(LunarCleanup.Core.Game.P.Keys, k => k != G.planet); if (o != null) { LunarCleanup.Core.Game.StartDoom(o); G.doomT = 25; } Write("ok doomother " + o); break; }
                        if (c == "icpinfo") { var I = LunarCleanup.Core.Game.Icp; var cs = LunarCleanup.Core.Game.Cur; Write("ok icpinfo " + (I == null ? "keins" : "total=" + I.total + " rockets=" + I.rockets + " hits=" + I.hits + " through=" + I.through) + " doomN=" + LunarCleanup.Core.Game.S.doomN + " doomT=" + LunarCleanup.Core.Game.S.doomT.ToString("0.0") + " pid=" + LunarCleanup.Core.Game.S.doomPid + " ruin=" + LunarCleanup.Core.Game.S.ruin + " planet=" + LunarCleanup.Core.Game.S.planet + " hitT=" + cs.sv.hitT.ToString("0") + " broken=" + System.Linq.Enumerable.Count(cs.sv.veh, v => v.wear >= 1 || v.mode == "broken") + " cool=" + cs.sv.msCool.ToString("0") + " sd=" + LunarCleanup.Core.Game.S.stardust.ToString("0.0")); break; }
                        if (c == "b34") { var cs = LunarCleanup.Core.Game.Cur; var sb = new System.Text.StringBuilder("ok b34 planet=" + cs.pid + " done=" + LunarCleanup.Core.Game.IsDone(cs.pid) + " terra=" + cs.sv.terra + " hp=" + cs.sv.terraHp.ToString("0.000") + " supOk=" + cs.sv.supOk + " up=" + LunarCleanup.Core.Game.UpkeepH(cs) + " rate=" + LunarCleanup.Core.Game.GoodRate(cs) + " ms=" + cs.sv.msLv + " flak=" + cs.sv.flakLv + " goods:"); foreach (var g in LunarCleanup.Core.Game.S.goods ?? new System.Collections.Generic.List<LunarCleanup.Core.FKV>()) sb.Append(" " + g.k + "=" + g.v.ToString("0.0")); Write(sb.ToString()); break; }
                        if (c.StartsWith("supok ")) { LunarCleanup.Core.Game.Cur.sv.supOk = c.EndsWith("1"); Write("ok " + c); break; }
                        if (c.StartsWith("lookdef ")) { var w = Object.FindAnyObjectByType<LunarCleanup.Gameplay.WorldView>(); var p = w.DefPos(int.Parse(c.Substring(8))); if (p.HasValue) LunarCleanup.Gameplay.GameRoot.FocusReq = p.Value.normalized; Write("ok " + c + " " + p.HasValue); break; }
                        if (c == "devall") { var sw = System.Diagnostics.Stopwatch.StartNew(); LunarCleanup.Core.Game.DevAll(); Write("ok devall " + sw.ElapsedMilliseconds + " ms planets=" + LunarCleanup.Core.Game.P.Count + " done=" + LunarCleanup.Core.Game.S.done.Count); break; }
                        if (c == "unruin") { LunarCleanup.Core.Game.S.ruinUntil = 0; Write("ok unruin"); break; }
                        if (c == "belts") { var w = Object.FindAnyObjectByType<LunarCleanup.Gameplay.WorldView>(); var cs = LunarCleanup.Core.Game.Cur; var sb = new System.Text.StringBuilder("ok belts"); foreach (var b in cs.lay.belts) if (LunarCleanup.Core.Game.BeltLive(cs, b)) sb.Append(" " + b.id + "(" + b.from + ">" + b.to + ")"); sb.Append(" custom=" + cs.sv.bRoutes.Count + " edit=" + w.EditBelt + " why=" + w.EditWhy + " pts="); if (w.Editing) foreach (var p in w.EditPts) sb.Append(p.x.ToString("0.0") + "/" + p.y.ToString("0.0") + ";"); Write(sb.ToString()); break; }
                        if (c.StartsWith("beltedit ")) { var w = Object.FindAnyObjectByType<LunarCleanup.Gameplay.WorldView>(); var cs = LunarCleanup.Core.Game.Cur; var id = c.Substring(9); w.BeginEdit(id); if (cs.lay.pos.ContainsKey(cs.lay.belts.Find(x => x.id == id).from)) LunarCleanup.Gameplay.GameRoot.FocusReq = cs.pd.PlanDir(cs.lay.pos[cs.lay.belts.Find(x => x.id == id).from]); Write("ok " + c + " " + w.Editing); break; }
                        if (c.StartsWith("beltmove ")) { var w = Object.FindAnyObjectByType<LunarCleanup.Gameplay.WorldView>(); var a = c.Substring(9).Split(' '); var ci = System.Globalization.CultureInfo.InvariantCulture; int i = int.Parse(a[0]); var p0 = w.EditPts[i]; w.MovePoint(i, p0 + new Vector2(float.Parse(a[1], ci), float.Parse(a[2], ci))); w.SelPt = i; Write("ok " + c); break; }
                        if (c.StartsWith("beltheight ")) { var w = Object.FindAnyObjectByType<LunarCleanup.Gameplay.WorldView>(); var a = c.Substring(11).Split(' '); int i = int.Parse(a[0]); w.SelPt = i; w.SetHeight(i, float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture)); Write("ok " + c); break; }
                        if (c == "beltadd") { Object.FindAnyObjectByType<LunarCleanup.Gameplay.WorldView>().AddPoint(); Write("ok beltadd"); break; }
                        if (c == "beltok") { var w = Object.FindAnyObjectByType<LunarCleanup.Gameplay.WorldView>(); Write("ok beltok " + w.CommitEdit() + " " + w.EditWhy); break; }
                        if (c == "beltreset") { var w = Object.FindAnyObjectByType<LunarCleanup.Gameplay.WorldView>(); w.ResetEdit(); Write("ok beltreset"); break; }
                        if (c == "lookrig") { var ps = LunarCleanup.Core.Game.Cur; if (ps.sv.rigs.Count > 0) { LunarCleanup.Gameplay.GameRoot.FocusReq = ps.pd.cells[ps.sv.rigs[ps.sv.rigs.Count - 1].cell].c.normalized; Write("ok lookrig"); } else Write("fehlt rig"); break; }
                        if (c == "lookev") { var p = LunarCleanup.Core.Game.EventPlace(LunarCleanup.Core.Game.Cur); if (p.HasValue) LunarCleanup.Gameplay.GameRoot.FocusReq = p.Value; Write("ok lookev " + p.HasValue); break; }
                        if (c.StartsWith("quality ")) { QualitySettings.SetQualityLevel(int.Parse(c.Substring(8)), true); var u = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline; Write("ok " + c + " " + QualitySettings.names[QualitySettings.GetQualityLevel()] + " " + (u ? u.name : "")); break; }
                        if (c == "state") { var G = typeof(LunarCleanup.Core.Game); var sb = new System.Text.StringBuilder("ok state cr=" + LunarCleanup.Core.Game.S.credits.ToString("0") + " last=" + LunarCleanup.Core.Game.S.lastActive + " now=" + LunarCleanup.Core.Game.Now());
                            foreach (var q in LunarCleanup.Core.Game.P.Values) { var modes = new System.Collections.Generic.Dictionary<string,int>(); foreach (var v in q.sv.veh) modes[v.mode] = (modes.TryGetValue(v.mode, out var n) ? n : 0) + 1; sb.Append("\n" + q.pid + " clean=" + q.nClean + " roh=" + LunarCleanup.Core.Game.RohSum(q).ToString("0") + " prod=" + LunarCleanup.Core.Game.ProdSum(q).ToString("0") + " rockets=" + q.sv.nRockets + " veh=" + string.Join(",", System.Linq.Enumerable.Select(modes, kv => kv.Key + ":" + kv.Value)) + " ef=" + q.ef.ToString("0.00") + " sell=" + LunarCleanup.Core.Game.Sum(q.sv.sellBuf).ToString("0") + "/" + LunarCleanup.Core.Game.SellCap(q).ToString("0") + " sold=" + q.sv.soldP.ToString("0") + " rk=" + q.sv.rk.ToString("0") + " rigs=" + q.sv.rigs.Count + " cr=" + q.sv.crystals.Count); }
                            Write(sb.ToString()); break; }
                        else if (c.StartsWith("offline ")) { var gr = Object.FindAnyObjectByType<LunarCleanup.Gameplay.GameRoot>(); var mi = typeof(LunarCleanup.Gameplay.GameRoot).GetMethod("Offline", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); gr.StartCoroutine((System.Collections.IEnumerator)mi.Invoke(gr, new object[] { float.Parse(c.Substring(8)) })); Write("ok " + c); break; }
                        if (c.StartsWith("travel ")) { var pid = c.Substring(7); if (!LunarCleanup.Core.Game.P.ContainsKey(pid)) { LunarCleanup.Core.Game.S.credits += 1e12; LunarCleanup.Core.Game.Unlock(pid); } else LunarCleanup.Core.Game.Travel(pid); Write("ok " + c); break; }
                        if (c.StartsWith("menu ")) { bool ok = EditorApplication.ExecuteMenuItem(c.Substring(5)); Write((ok ? "ok " : "fehlt ") + c); }
                        else Write("unbekannt: " + c);
                        break;
                }
            }
            catch (System.Exception e) { Write("Fehler: " + e); }
        }
        static string StripKey(string j, string key)
        {
            string pat = "\"" + key + "\":"; int i;
            while ((i = j.IndexOf(pat)) >= 0)
            {
                int e = i + pat.Length, depth = 0; bool str = false;
                for (; e < j.Length; e++) { char ch = j[e]; if (str) { if (ch == '\\') e++; else if (ch == '"') str = false; continue; } if (ch == '"') str = true; else if (ch == '[' || ch == '{') depth++; else if (ch == ']' || ch == '}') { if (depth == 0) break; depth--; } else if (ch == ',' && depth == 0) break; }
                int st = i; if (e < j.Length && j[e] == ',') e++; else if (st > 0 && j[st - 1] == ',') st--;
                j = j.Remove(st, e - st);
            }
            return j;
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
