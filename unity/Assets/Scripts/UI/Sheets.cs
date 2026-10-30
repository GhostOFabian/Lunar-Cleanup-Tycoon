// Inhalte der Fenster (Fahrzeuge, Produktion, Basis, Upgrades, Planeten, Ziele, Einstellungen, Gebäude) – Texte wie im Browser.
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.UI
{
    public static class Sheets
    {
        static string Cr(double c) => Game.Fmt(c) + " Cr";
        static PState ps => Game.Cur;

        public static void Fill(Hud h, string kind, string arg, out string title)
        {
            title = "";
            switch (kind)
            {
                case "vehicles": title = "Fahrzeuge"; Vehicles(h); break;
                case "production": title = "Produktion"; Production(h); break;
                case "base": title = "Basis"; Base(h); break;
                case "upgrades": title = "Upgrades"; Upgrades(h); break;
                case "planets": title = "Planeten"; Planets(h); break;
                case "goals": title = "Ziele"; Goals(h); break;
                case "energy": title = "Energie"; Energy(h); break;
                case "research": title = "Forschung"; Research(h); break;
                case "settings": title = "Einstellungen"; Settings(h); break;
                case "building": title = GameData.Bld[arg].id == "presse" || GameData.Bld[arg].id == "schmelze" || GameData.Bld[arg].id == "raff" ? GameData.MachName(ps.pid, arg) : GameData.Bld[arg].name; Building(h, arg); break;
            }
        }

        static void UpRow(Hud h, string id)
        {
            var u = GameData.Up[id];
            h.Row(u.name, () => { int l = Game.Lvl(id); return "Stufe " + l + "/" + u.max + (u.loc ? " · nur " + ps.P.name : "") + " · " + u.label + ": " + Game.UShow(id, l) + (l < u.max ? " → " + Game.UShow(id, l + 1) : ""); },
                () => Game.Lvl(id) >= u.max ? "Max" : Cr(Game.UCost(id)), () => Game.Lvl(id) < u.max && Game.S.credits >= Game.UCost(id), () => Game.BuyUpgrade(id));
        }
        static bool UpVisible(UpgDef u) => !u.hide && (!u.done || Game.CleanPct(ps) >= .999f) && (!u.rad || ps.P.rad > 0) && (u.stage == 0 || Game.Gs(ps) >= u.stage) && (string.IsNullOrEmpty(u.res) || Game.ResDone(u.res) && u.res != "drones" && u.res != "outpost");

        static void Vehicles(Hud h)
        {
            h.Section("Sammelwagen");
            var c = ps.sv.cart;
            if (!c.owned) h.Row("Sammelwagen", () => "Holt deinen vollen Behälter bei dir ab und bringt die Ladung zur Entladestation mit dem meisten Platz.", () => Cr(Game.CartCost(ps)), () => Game.S.credits >= Game.CartCost(ps), () => Game.BuyCart());
            else h.Row("Sammelwagen", () => (c.mode == "park" ? "Wartet in der Basis" : c.mode == "go" ? "Kommt zu dir" : c.mode == "wait" ? "Ist da – tippe auf deinen Behälter" : c.mode == "back" ? "Bringt die Ladung weg" : "Entlädt") + " · Kapazität " + Game.Fmt(Game.CartCap()) + " E");
            h.Section("Fahrzeuge kaufen · " + ps.sv.veh.Count + "/" + Game.VehSlots(ps) + " Plätze");
            foreach (var t in GameData.VehOrder)
            {
                var T = GameData.Veh[t]; if (!string.IsNullOrEmpty(T.planet) && T.planet != ps.pid && !(T.home ?? new string[0]).Contains(ps.pid) && !Game.P.ContainsKey(T.planet)) continue;
                string key = t;
                h.Row(T.name, () => { string w = Game.VehWhy(ps, key); return (w != "" ? w + " · " : "") + T.desc + " Tank " + Game.Fmt(T.tank) + " E."; },
                    () => Cr(Game.VehCost(ps, key)), () => Game.VehWhy(ps, key) == "" && Game.S.credits >= Game.VehCost(ps, key), () => Game.BuyVehicle(key));
            }
            if (ps.sv.veh.Count > 0)
            {
                h.Section("Im Einsatz");
                foreach (var v in ps.sv.veh) { var vv = v; h.Row(GameData.Veh[v.type].name + " #" + v.id, () => Game.VehStatus(vv) + " · Tank " + Game.Fmt(Game.Sum(vv.tank)) + "/" + Game.Fmt(GameData.Veh[vv.type].tank * Game.Uv("vtank")) + (GameData.Veh[vv.type].batt > 0 ? " · Akku " + Mathf.RoundToInt((vv.bat < 0 ? 1 : vv.bat / Game.BattCap(ps, vv)) * 100) + " %" : "")); }
            }
            h.Section("Upgrades für Fahrzeuge");
            foreach (var u in GameData.Upg.Where(x => x.cat == "Fahrzeuge" && UpVisible(x))) UpRow(h, u.id);
        }

        static void Production(Hud h)
        {
            h.Section("Kette");
            h.Note("Fahrzeuge → Entladestation → Band → Rohstofflager → Band → " + GameData.MachName(ps.pid, "presse") + " → " + GameData.MachName(ps.pid, "schmelze") + " → Produktlager → Rakete");
            h.Section("Lager");
            h.Row(GameData.Bld["roh"].name, () => Game.Fmt(Game.RohSum(ps)) + " / " + Game.Fmt(Game.RohCap(ps)) + " E · " + Mats(ps.sv.roh));
            if (ps.bld.Contains("prod")) h.Row(GameData.Bld["prod"].name, () => Game.Fmt(Game.ProdSum(ps)) + " / " + Game.Fmt(Game.ProdCap(ps)) + " · " + Mats(ps.sv.prod));
            foreach (var s in GameData.Stations) if (ps.bld.Contains(s)) { var ss = s; h.Row(GameData.Bld[s].name, () => "Trichter " + Game.Fmt(Game.Sum(ps.st[ss].buf)) + " / " + Game.Fmt(Game.StBuf(ps, ss)) + " E"); }
            if (ps.bld.Contains("presse") || ps.bld.Contains("schmelze") || ps.bld.Contains("raff"))
            {
                h.Section("Maschinen");
                foreach (var m in new[] { "presse", "schmelze", "raff" }) if (ps.bld.Contains(m))
                    {
                        var mm = m; var rec = ps.line.Of(m);
                        h.Row(GameData.MachName(ps.pid, m), () => Game.MachStatus(ps, mm) + " · hergestellt " + ps.mach[mm].made + " · " + string.Join(", ", rec.Select(r => r.n + " " + GameData.Mats[r.inp].name + " → " + GameData.Mats[r.outp].name)));
                    }
            }
            h.Section("Rakete");
            h.Row("Frachtraum", () => ps.sv.rk > 0 ? "Unterwegs – zurück in " + Mathf.CeilToInt(ps.sv.rk) + " s" : Game.Fmt(Game.Sum(ps.sv.sellBuf)) + " / " + Game.Fmt(Game.SellCap(ps)) + " E · Wert " + Cr(Game.CargoValue(ps)) + " · startet von selbst, wenn sie voll ist",
                () => "Starten", () => ps.sv.rk <= 0 && Game.Sum(ps.sv.sellBuf) > 0, () => Game.LaunchRocket(), false);
            if (Game.Gs(ps) < 2) h.Note("Ab Basisstufe 2 verbinden Förderbänder die Lager mit der Rakete – dann verkauft sie automatisch.");
            h.Section("Waren");
            h.Row("Alles verkaufen", () => "Wert: " + Cr(Enumerable.Range(0, GameData.Mats.Count).Sum(m => (GameData.Mats[m].raw ? ps.sv.roh[m] : Mathf.Floor(ps.sv.prod[m] + 1e-6f)) * Game.MatValue(m, ps))), () => "Verkaufen",
                () => Game.RohSum(ps) >= .5f || Game.ProdSum(ps) >= 1, () => Game.SellAll(), false);
            for (int i = 0; i < GameData.Mats.Count; i++)
            {
                int m = i; var M = GameData.Mats[m]; var st = M.raw ? ps.sv.roh : ps.sv.prod; if (st[m] < (M.raw ? .5f : 1)) continue;
                h.Row(M.name, () => Game.Fmt((M.raw ? ps.sv.roh : ps.sv.prod)[m]) + " auf Lager · je " + Game.MatValue(m, ps).ToString(Game.MatValue(m, ps) < 10 ? "0.0" : "0") + " Cr", () => "Verkaufen",
                    () => (M.raw ? ps.sv.roh : ps.sv.prod)[m] >= (M.raw ? .5f : 1), () => Game.SellMat(m), false);
            }
            h.Section("Upgrades");
            foreach (var u in GameData.Upg.Where(x => (x.cat == "Förderbänder" || x.cat == "Maschinen" || x.cat == "Entladestationen" || x.cat == "Verkauf" || x.cat == "Lager") && UpVisible(x))) UpRow(h, u.id);
        }
        static string Mats(float[] a) { var l = Enumerable.Range(0, a.Length).Where(i => a[i] >= .5f).Select(i => GameData.Mats[i].name + " " + Game.Fmt(a[i])).ToList(); return l.Count == 0 ? "leer" : string.Join(", ", l); }

        static void Base(Hud h)
        {
            var st = GameData.Stages; int s = ps.sv.stage;
            h.Section("Basisstufe " + s + " · " + st[s - 1].name);
            if (s < st.Count) { var nx = st[s]; h.Row("Stufe " + nx.n + ": " + nx.name, () => nx.txt, () => Cr(Game.StageCost(ps)), () => Game.S.credits >= Game.StageCost(ps), () => Game.BuyStage()); }
            else h.Note("Höchste Basisstufe erreicht.");
            h.Section("Gebäude");
            foreach (var id in GameData.BOrder)
            {
                var B = GameData.Bld[id]; if (ps.bld.Contains(id) || B.stage > s + 1) continue; string key = id;
                h.Row(B.name, () => { string w = Game.BuildWhy(ps, key); return (w != "" ? w + " · " : "") + B.desc; }, () => Cr(Game.BuildCost(ps, key)), () => Game.BuildWhy(ps, key) == "" && Game.S.credits >= Game.BuildCost(ps, key), () => Game.Build(key));
            }
            if (Game.HasLab()) h.Row("Forschung", () => Game.ResActive != null ? "Läuft: " + Game.ResActive.name + " · " + Mathf.FloorToInt(Game.S.resT / Game.ResActive.time * 100) + " %" : "Kein Projekt aktiv", () => "Öffnen", () => true, () => h.OpenSheet("research"), false);
            h.Section("Steht schon");
            h.Note(string.Join(" · ", GameData.BOrder.Where(ps.bld.Contains).Select(id => id == "presse" || id == "schmelze" || id == "raff" ? GameData.MachName(ps.pid, id) : GameData.Bld[id].name)));
            h.Section("Upgrades");
            foreach (var u in GameData.Upg.Where(x => x.cat == "Basis" && UpVisible(x))) UpRow(h, u.id);
        }

        static void Upgrades(Hud h)
        {
            h.Note("Handgerät, Drohnen und Wirtschaft gelten überall. Fahrzeug- und Anlagen-Upgrades und die Technikstufe baust du auf jedem Planeten neu auf – sie kosten dort auch mehr.");
            foreach (var cat in GameData.Upg.Select(u => u.cat).Distinct())
            {
                var list = GameData.Upg.Where(u => u.cat == cat && UpVisible(u)).ToList(); if (list.Count == 0) continue;
                h.Section(cat); foreach (var u in list) UpRow(h, u.id);
            }
        }

        static void Planets(Hud h)
        {
            h.Section("Reiseziele");
            foreach (var pid in GameData.POrder)
            {
                var P = GameData.Planets[pid]; bool have = Game.P.ContainsKey(pid), cur = Game.S.planet == pid; string key = pid;
                string dirt = string.Join(", ", P.types.Select(t => GameData.Dirt[t.Key].name));
                if (cur) h.Row(P.name, () => "Du bist hier · " + Mathf.FloorToInt(Game.CleanPct(Game.P[key]) * 100) + " % sauber · " + P.sub);
                else if (have) h.Row(P.name, () => Mathf.FloorToInt(Game.CleanPct(Game.P[key]) * 100) + " % sauber · " + P.sub, () => "Reisen", () => true, () => { Game.Travel(key); h.CloseSheet(); }, false);
                else
                {
                    int i = System.Array.IndexOf(GameData.POrder, pid); string prev = i > 0 ? GameData.POrder[i - 1] : null;
                    if (prev != null && Game.P.ContainsKey(prev)) h.Row(P.name, () => { string w = Game.UnlockWhy(key); return (w != "" ? w + " · " : "") + P.sub + " · " + dirt + (P.hzName != "" ? " · Gefahr: " + P.hzName : ""); }, () => Cr(P.unlockCost), () => Game.UnlockWhy(key) == "" && Game.S.credits >= P.unlockCost, () => { if (Game.Unlock(key)) h.CloseSheet(); });
                    else h.Row(P.name, () => P.sub + " · noch nicht erreichbar");
                }
            }
        }

        static void Goals(Hud h)
        {
            h.Section("Hauptziele");
            for (int i = 0; i < Mathf.Min(Game.QuestsSupported, GameData.Quests.Count); i++)
            {
                int q = i; var Q = GameData.Quests[i];
                h.Row((q < Game.S.quest ? "✓ " : q == Game.S.quest ? "▶ " : "") + Q.t, () => q < Game.S.quest ? "Erledigt" : q == Game.S.quest ? Mathf.FloorToInt(Mathf.Clamp01(Game.QuestProgress(q)) * 100) + " % · Belohnung " + Cr(Q.r) : "Belohnung " + Cr(Q.r));
            }
            h.Note("Die übrigen " + (GameData.Quests.Count - Game.QuestsSupported) + " Ziele kommen mit den nächsten Systemen in der Unity-Version (Drohnen, Außenlager, Stürme, Tiefenabbau).");
        }

        static string Dec(float x, int d) => x.ToString(d == 0 ? "0" : d == 1 ? "0.0" : "0.00");
        static string FmtTime(float s) { int t = Mathf.Max(0, Mathf.CeilToInt(s)); return t >= 60 ? t / 60 + " min " + (t % 60).ToString("00") + " s" : t + " s"; }
        static void Energy(Hud h)
        {
            var P = ps.P;
            h.Section("Energie");
            h.Note("Fahrzeuge, Maschinen und Bänder brauchen Strom" + (P.energy > 1 ? " – auf dem " + P.name + " das " + Dec(P.energy, 1) + "-fache" : "") + ". Reicht er nicht, läuft alles langsamer.");
            h.Row("Speicher", () => Game.Fmt(ps.sv.energy) + " / " + Game.Fmt(Game.EnergyCap(ps)) + " gespeichert · Produktion " + Dec(Game.EnergyProd(ps), 1) + "/s · Verbrauch " + Dec(ps.demand, 1) + "/s" + (ps.ef < .99f ? " · reicht nicht" : ""));
            h.Row("Tageszeit", () => (Game.IsDay(ps) ? "Tag" : "Nacht") + " · noch " + FmtTime(Game.DayLeft(ps)));
            h.Row("Solar", () => Dec(Game.SolarProd(ps), 1) + "/s · Sonne " + Mathf.RoundToInt(ps.solar * 100) + " %");
            h.Row("Radioisotopen", () => ps.bld.Contains("rtg") ? "10/s" : "–");
            float gf = Game.GeoF(ps.pid);
            h.Row("Geothermie", () => ps.bld.Contains("geo") ? Dec(30 * gf, 0) + "/s" : gf > 0 ? "möglich: " + Dec(30 * gf, 0) + "/s" : "keine Erdwärme");
            h.Row("Kernreaktor", () => ps.bld.Contains("reactor") ? "150/s" : "–");
            UpRow(h, "energy"); if (UpVisible(GameData.Up["ecap"])) UpRow(h, "ecap");
            h.Note("Solarfelder liefern nur tagsüber. Nachts laufen Maschinen aus dem Speicher (Akkupark), Akku-Fahrzeuge aus ihrem Akku – oder du baust Radioisotopen-Generator (Stufe 3), Geothermie (Stufe 4) und Kernreaktor (Stufe 5), die rund um die Uhr liefern.");
        }

        static void Research(Hud h)
        {
            h.Section("Forschung");
            h.Note("", () => "Ein Projekt zur Zeit, läuft auch offline. Tempo ×" + Dec(Game.ResSpeed(), 2) + (Game.HasLab() ? "" : " · Erst eine Forschungsstation bauen (Basisstufe 3)."));
            foreach (var d in GameData.Res)
            {
                var dd = d;
                h.Row(d.name, () =>
                {
                    bool act = Game.S.resAct == dd.id; string st = Game.ResDone(dd.id) ? "Erforscht · " : act ? "Läuft · " + Mathf.FloorToInt(Game.S.resT / dd.time * 100) + " % · noch " + FmtTime((dd.time - Game.S.resT) / Mathf.Max(.01f, Game.ResSpeed())) + " · " : !string.IsNullOrEmpty(dd.req) && !Game.ResDone(dd.req) ? "Später · " : "";
                    return st + dd.desc + " · " + FmtTime(dd.time);
                },
                () => Game.ResDone(dd.id) ? "Erforscht" : Game.S.resAct == dd.id ? "Läuft" : !string.IsNullOrEmpty(dd.req) && !Game.ResDone(dd.req) ? "Später" : Cr(dd.cost),
                () => Game.ResWhy(dd) == "" && Game.S.credits >= dd.cost, () => Game.StartResearch(dd.id));
            }
            if (UpVisible(GameData.Up["research"])) UpRow(h, "research");
        }

        static float resetT;
        static void Settings(Hud h)
        {
            h.Section("Spielstand");
            h.Row("Jetzt speichern", () => "Das Spiel speichert alle 10 Sekunden und beim Schließen von selbst.", () => "Speichern", () => true, () => { Game.Save(); Game.Toast("Gespeichert", "good"); }, false);
            h.Row("Neu anfangen", () => Time.unscaledTime - resetT < 4 ? "Wirklich? Noch einmal tippen löscht deinen Unity-Spielstand." : "Löscht den Spielstand dieser Unity-Version und startet auf dem Mond.", () => Time.unscaledTime - resetT < 4 ? "Ja, löschen" : "Neu anfangen", () => true,
                () => { if (Time.unscaledTime - resetT < 4) { Game.ResetAll(); h.CloseSheet(); } else resetT = Time.unscaledTime; }, false);
            h.Section("Über");
            h.Note("Lunar Cleanup Tycoon · Unity-Version 0.3. Planeten, Parzellen, Zahlen und Texte stammen 1:1 aus dem Browser-Prototyp. Noch nicht übertragen: Stürme, Drohnen, Außenlager, Tiefenabbau.");
        }

        static void Building(Hud h, string id)
        {
            var B = GameData.Bld[id]; h.Note(B.desc);
            if (id == "roh" || id == "prod" || id == "sell" || GameData.Stations.Contains(id) || id == "presse" || id == "schmelze" || id == "raff") { Production(h); return; }
            if (id == "energy" || id == "rtg" || id == "geo" || id == "reactor") { Energy(h); return; }
            if (id == "research") { Research(h); return; }
            if (id == "hq") { Base(h); return; }
            if (id == "werkstatt") { Vehicles(h); return; }
        }
    }
}
