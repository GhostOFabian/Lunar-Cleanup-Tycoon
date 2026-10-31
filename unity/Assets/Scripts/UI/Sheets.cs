// Inhalte der Fenster (Fahrzeuge, Produktion, Basis, Upgrades, Planeten, Ziele, Einstellungen, Gebäude) – Texte wie im Browser.
using System.Collections.Generic;
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
                case "hazard": title = "Gefahren"; Hazard(h); break;
                case "outpost": title = "Außenlager"; Outpost(h, int.Parse(arg)); BuildingUpgrades(h, "outpost"); break;
                case "rig": title = "Tiefenabbau"; Rig(h, int.Parse(arg)); BuildingUpgrades(h, "rig"); break;
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
        /// <summary>Upgrades für Systeme, die in Unity noch fehlen (Tiefenabbau), bleiben bis dahin ausgeblendet – kein Knopf ohne Wirkung.</summary>
        static readonly string[] NotPorted = { };
        static bool UpVisible(UpgDef u) => !u.hide && System.Array.IndexOf(NotPorted, u.id) < 0 && (!u.done || Game.CleanPct(ps) >= .999f) && (!u.rad || ps.P.rad > 0) && (u.stage == 0 || Game.Gs(ps) >= u.stage) && (string.IsNullOrEmpty(u.res) || Game.ResDone(u.res));

        static readonly Color AccVeh = new Color(.43f, .95f, .82f), AccBld = new Color(1f, .75f, .3f), AccDrone = new Color(.5f, .7f, 1f);
        static void Vehicles(Hud h)
        {
            h.Tabs("Kaufen", "Flotte " + ps.sv.veh.Count + "/" + Game.VehSlots(ps), "Upgrades");
            int tab = h.Tab;
            if (tab == 0)
            {
                var c = ps.sv.cart;
                if (!c.owned) h.Card("Sammelwagen", () => "Kommt zu dir, wenn dein Behälter voll ist, und bringt die Ladung zur Entladestation. Danach leerst du den Behälter nur noch in den Wagen.", AccVeh, () => Cr(Game.CartCost(ps)), () => Game.S.credits >= Game.CartCost(ps), () => Game.BuyCart());
                h.Section("Fahrzeuge · " + ps.sv.veh.Count + " von " + Game.VehSlots(ps) + " Plätzen (höchstens " + Game.VehMax(ps) + ")");
                foreach (var t in GameData.VehOrder)
                {
                    var T = GameData.Veh[t]; if (!string.IsNullOrEmpty(T.planet) && T.planet != ps.pid && !(T.home ?? new string[0]).Contains(ps.pid) && !Game.P.ContainsKey(T.planet)) continue;
                    string key = t; int have = ps.sv.veh.Count(v => v.type == key);
                    h.Card(T.name, () => { string w = Game.VehWhy(ps, key); return (w != "" ? w + " · " : "") + T.desc + " Tank " + Game.Fmt(T.tank) + " E."; }, AccVeh,
                        () => Cr(Game.VehCost(ps, key)), () => Game.VehWhy(ps, key) == "" && Game.S.credits >= Game.VehCost(ps, key), () => Game.BuyVehicle(key), have > 0 ? have + "× im Einsatz" : null);
                }
                return;
            }
            if (tab == 1)
            {
                var c = ps.sv.cart;
                if (c.owned) h.Row("Sammelwagen", () => (c.mode == "park" ? "Wartet in der Basis" : c.mode == "go" ? "Kommt zu dir" : c.mode == "wait" ? "Ist da – tippe auf deinen Behälter" : c.mode == "back" ? "Bringt die Ladung weg" : "Entlädt") + " · Kapazität " + Game.Fmt(Game.CartCap()) + " E");
                if (ps.sv.veh.Count == 0) { h.Note("Noch keine Fahrzeuge. Unter „Kaufen“ findest du alle, die hier fahren können."); return; }
                h.Note("Zu viele kleine Fahrzeuge? Mit der Selbstzerstörung machst du Platz für größere – 30 % des Kaufpreises kommen zurück.");
                foreach (var v in ps.sv.veh)
                {
                    var vv = v;
                    h.Row(Game.VehName(v), () => Game.VehStatus(vv) + " · Tank " + Game.Fmt(Game.Sum(vv.tank)) + "/" + Game.Fmt(GameData.Veh[vv.type].tank * Game.Uv("vtank")) + " · Stufe " + Game.VehLvl(vv) + (Game.Gs(ps) >= 3 ? " · Zustand " + Mathf.RoundToInt(Mathf.Max(0, 1 - vv.wear) * 100) + " %" : ""),
                        () => Game.Gs(ps) < 3 || !ps.sv.veh.Contains(vv) ? "–" : vv.mode == "toShop" || vv.mode == "repair" ? "In Werkstatt" : Game.RepsLeft(vv) <= 0 ? "Ersetzen " + Cr(Game.ReplaceCost(ps, vv)) : "Warten " + Cr(Game.RepCost(ps, vv)),
                        () => Game.Gs(ps) >= 3 && ps.sv.veh.Contains(vv) && vv.mode != "toShop" && vv.mode != "repair" && vv.mode != "boom" && (Game.RepsLeft(vv) <= 0 ? Game.S.credits >= Game.ReplaceCost(ps, vv) : ps.bld.Contains("werkstatt") && vv.wear > .05f && Game.S.credits >= Game.RepCost(ps, vv)),
                        () => { if (Game.RepsLeft(vv) <= 0) Game.ReplaceVeh(ps, vv, false); else if (Game.SendToShop(ps, vv, false)) Game.Toast("Fährt zur Werkstatt", "good"); }, false);
                    h.Row("   Spezialisierung / Selbstzerstörung", () => Game.VehLvl(vv) >= 2 ? "Spezialisierung: " + FeatureData.VehSpecName[vv.spec] : "Spezialisierung ab Stufe 2",
                        () => vv.mode == "boom" ? "Läuft …" : "Sprengen +" + Cr(Game.BoomRefund(ps, vv)), () => ps.sv.veh.Contains(vv) && vv.mode != "boom", () => Game.SelfDestruct(vv), false);
                    if (Game.VehLvl(vv) >= 2) h.Row("   Spezialisierung wechseln", () => "", () => "Wechseln", () => true, () => Game.VehSetSpec(vv, (vv.spec + 1) % FeatureData.VehSpec.Length), false);
                    h.Row("   Arbeitsbereich", () => Game.Zones[vv.zone] + " · Teamarbeit: mehrere Fahrzeuge auf einer Parzelle reinigen je Partner 10 % stärker", () => "Ändern", () => true, () => vv.zone = (vv.zone + 1) % Game.Zones.Length, false);
                }
                if (Game.Gs(ps) >= 3) h.Row("Automatische Wartung", () => "Ab 75 % Verschleiß fährt jedes Fahrzeug selbst zur Werkstatt; nach zwei Reparaturen wird es automatisch ersetzt.", () => ps.sv.autoMaint ? "An" : "Aus", () => true, () => ps.sv.autoMaint = !ps.sv.autoMaint, false);
                return;
            }
            foreach (var u in GameData.Upg.Where(x => (x.cat == "Fahrzeuge" || x.cat == "Drohnen") && UpVisible(x))) UpRow(h, u.id);
        }

        static void Production(Hud h)
        {
            h.Tabs("Ampel", "Verkauf", "Plan", "Upgrades");
            int tab = h.Tab;
            if (tab == 0) { ProductionOverview(h); return; }
            if (tab == 1) { Selling(h); return; }
            if (tab == 2) { if (!ps.bld.Contains("presse")) h.Note("Baue die Staubpresse (Basisstufe 2), um mit der Veredelung zu beginnen."); Plan(h); return; }
            foreach (var cat in new[] { "Entladestationen", "Förderbänder", "Maschinen", "Lager", "Verkauf", "Außenlager" })
            {
                var list = GameData.Upg.Where(u => u.cat == cat && UpVisible(u)).ToList(); if (list.Count == 0) continue;
                h.Section(cat); foreach (var u in list) UpRow(h, u.id);
            }
        }
        static void ProductionOverview(Hud h)
        {
            Ampel(h);
            h.Section("Kette");
            h.Note("Fahrzeuge → Entladeanlage → Band → Rohstofflager → Band → " + GameData.MachName(ps.pid, "presse") + " → " + GameData.MachName(ps.pid, "schmelze") + " → Produktlager → Rakete");
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
        }
        /// <summary>Verkauf: nur freigegebene Waren gehen in den Frachtraum der Rakete – verkauft wird erst beim Start.</summary>
        static void Selling(Hud h)
        {
            h.Section("Rakete");
            h.Row("Frachtraum", () => ps.sv.rk > 0 ? "Unterwegs – zurück in " + Mathf.CeilToInt(ps.sv.rk) + " s" : Game.Fmt(Game.Sum(ps.sv.sellBuf)) + " / " + Game.Fmt(Game.SellCap(ps)) + " E · Wert " + Cr(Game.CargoValue(ps)) + " · startet von selbst, wenn sie voll ist",
                () => "Starten", () => ps.sv.rk <= 0 && Game.Sum(ps.sv.sellBuf) > 0, () => Game.LaunchRocket(), false);
            if (ps.bld.Contains("sell"))
            {
                h.Row("Frachtauftrag", () => { if (ps.sv.order < 0) Game.NewOrder(ps); return ps.sv.order >= 0 ? GameData.Mats[ps.sv.order].name + " bringt +60 % – jede Rakete hat einen neuen Auftrag" : "–"; });
                h.Row("Mischfracht und Betriebsbonus", () => { int k = Enumerable.Range(0, ps.sv.sellBuf.Length).Count(m => ps.sv.sellBuf[m] > 1e-6f); return k + " Warensorten · +" + Mathf.RoundToInt((Mathf.Min(1.4f, 1 + .08f * Mathf.Max(0, k - 1)) - 1) * 100) + " % · Betriebsbonus +" + Mathf.RoundToInt((float)(Game.FlowBonus(ps) - 1) * 100) + " %"; });
                if (Game.Gs(ps) >= 2) h.Row("Frachtauftrag bevorzugen", () => "Ist der Auftrag eine freigegebene Rohware, schickt das Band sie zuerst.", () => ps.sv.autoOrder ? "An" : "Aus", () => true, () => ps.sv.autoOrder = !ps.sv.autoOrder, false);
                if (Game.Gs(ps) >= 3) h.Row("Verkaufsautomat", () => "Sind die Lager zu 90 % voll, startet die Rakete schon halb beladen.", () => ps.sv.autoDump ? "An" : "Aus", () => true, () => Game.ToggleAutoDump(), false);
            }
            h.Section("Waren");
            h.Note(Game.Gs(ps) < 2 ? "Gib eine Ware frei und lade sie in den Frachtraum. Verkauft wird beim Raketenstart. Ab Basisstufe 2 fahren freigegebene Waren übers Band von selbst hin." : "Nur freigegebene Waren fahren übers Band in den Frachtraum. Gesperrte bleiben im Lager – zum Beispiel, weil eine Maschine sie noch veredeln soll.");
            for (int i = 0; i < GameData.Mats.Count; i++)
            {
                int m = i; var M = GameData.Mats[m]; var st = M.raw ? ps.sv.roh : ps.sv.prod; if (st[m] < (M.raw ? .5f : 1) && !Game.PlanetRaws(ps).Contains(m)) continue;
                h.Row(M.name, () => Game.Fmt((M.raw ? ps.sv.roh : ps.sv.prod)[m]) + " auf Lager · je " + Game.MatValue(m, ps).ToString(Game.MatValue(m, ps) < 10 ? "0.0" : "0") + " Cr · " + (Game.SellOn(ps, m) ? "freigegeben" : "gesperrt"),
                    () => Game.SellOn(ps, m) ? "Sperren" : "Freigeben", () => true, () => Game.ToggleAuto(m), false);
                if (Game.Gs(ps) < 2) h.Row("   In den Frachtraum laden", () => "", () => "Laden", () => Game.SellOn(ps, m) && ps.sv.rk <= 0 && (M.raw ? ps.sv.roh : ps.sv.prod)[m] >= (M.raw ? .5f : 1), () => Game.LoadRocket(m), false);
            }
        }
        // Produktions-Ampel: Engpässe mit Ursache und den passenden Upgrades
        static void Ampel(Hud h)
        {
            var r = Game.FlowReport(ps); var bad = r.Where(n => n.lvl > 0).OrderByDescending(n => n.lvl).ToList();
            h.Section("Ampel");
            if (bad.Count == 0) { h.Note("", () => { var rr = Game.FlowReport(ps); return Game.FlowLevel(rr) == 0 ? "Grün: alles im Fluss, kein Stau." : "Ampel hat sich geändert – Fenster neu öffnen."; }); return; }
            foreach (var n in bad) h.Row((n.lvl == 2 ? "Rot: " : "Gelb: ") + n.name, () => n.why);
            var fixes = bad.SelectMany(n => n.fix).Distinct().Where(id => GameData.Up.ContainsKey(id) && UpVisible(GameData.Up[id])).Take(5).ToList();
            if (fixes.Count > 0) { h.Section("Hilft gegen den Stau"); foreach (var f in fixes) UpRow(h, f); }
        }
        // Produktionsplan: Rezepte an/aus
        static void Plan(Hud h)
        {
            if (!ps.bld.Contains("presse")) return;
            var raws = Game.PlanetRaws(ps).ToList();
            h.Section("Produktionsplan");
            h.Note("Veredeln bringt mehr pro Einheit, kostet aber Zeit, Energie und Maschinen. Was du nicht veredelst, bleibt im Rohstofflager und wird roh verkauft. Endprodukte hier: " + string.Join(", ", raws.Select(m => GameData.Mats[Game.EndGood(ps, m)].name).Distinct()) + ".");
            string Pct(Recipe rc) => "+" + Mathf.RoundToInt((GameData.Mats[rc.outp].v * rc.k / Mathf.Max(.01f, GameData.Mats[rc.inp].v * rc.n) - 1) * 100) + " % Wert";
            var keys = new List<(string key, string label, string desc)>();
            foreach (var rc in ps.line.presse.Where(x => raws.Contains(x.inp))) keys.Add((rc.plan, GameData.MachName(ps.pid, "presse") + ": " + GameData.Mats[rc.inp].name, rc.n + " " + GameData.Mats[rc.inp].name + " → " + rc.k + " " + GameData.Mats[rc.outp].name + " (" + Pct(rc) + ")"));
            if (ps.bld.Contains("schmelze")) { var sm = ps.line.schmelze.Where(x => ps.line.presse.Any(q => q.outp == x.inp && raws.Contains(q.inp))).ToList(); if (sm.Count > 0) keys.Add(("smelt", GameData.MachName(ps.pid, "schmelze"), string.Join(" · ", sm.Select(x => x.n + " " + GameData.Mats[x.inp].name + " → " + x.k + " " + GameData.Mats[x.outp].name + " (" + Pct(x) + ")")))); }
            if (ps.bld.Contains("raff")) { var rf = ps.line.raff.Where(x => raws.Contains(x.inp)).ToList(); if (rf.Count > 0) keys.Add(("refine", GameData.MachName(ps.pid, "raff"), string.Join(" · ", rf.Select(x => x.n + " " + GameData.Mats[x.inp].name + " → " + x.k + " " + GameData.Mats[x.outp].name + " (" + Pct(x) + ")")))); }
            foreach (var (key, label, desc) in keys.GroupBy(k => k.key).Select(g => g.First())) { var kk = key; h.Row(label, () => desc, () => Game.PlanOn(ps, kk) ? "An" : "Aus", () => true, () => Game.TogglePlan(kk), false); }
        }
        // Gebäudestufe
        static void BuildingLevel(Hud h, string id)
        {
            if (!Game.BLV_REQ.ContainsKey(id) || !ps.bld.Contains(id)) return;
            h.Section("Gebäudestufe");
            h.Row("Stufe " + Game.Blvl(ps, id) + " von " + Game.BlMax(ps), () => { string w = Game.BlWhy(ps, id); return "Sind alle Upgrades dieses Gebäudes ausgereizt, wächst es eine Stufe: sichtbar größer und +" + Game.BlPct(id) + " % " + Game.BlEffect(id) + " je Stufe." + (Game.Blvl(ps, id) > 0 ? " Jetzt ×" + Dec(Game.BlF(ps, id), 1) + "." : "") + (w != "" ? " " + w + "." : ""); },
                () => Game.BlWhy(ps, id) == "" ? "Ausbauen · " + Cr(Game.BlCost(ps, id)) : "Gesperrt", () => Game.BlWhy(ps, id) == "" && Game.S.credits >= Game.BlCost(ps, id), () => Game.BuyBl(id), true, () => "Stufe " + Game.Blvl(ps, id) + " von " + Game.BlMax(ps));
        }
        static string Mats(float[] a) { var l = Enumerable.Range(0, a.Length).Where(i => a[i] >= .5f).Select(i => GameData.Mats[i].name + " " + Game.Fmt(a[i])).ToList(); return l.Count == 0 ? "leer" : string.Join(", ", l); }

        static void Base(Hud h)
        {
            h.Tabs("Bauen", "Ausbau", "Upgrades");
            int tab = h.Tab; var st = GameData.Stages; int s = ps.sv.stage;
            if (tab == 0)
            {
                h.Section("Basisstufe " + s + " · " + st[s - 1].name);
                if (s < st.Count) { var nx = st[s]; h.Card("Stufe " + nx.n + ": " + nx.name, () => nx.txt, AccBld, () => Cr(Game.StageCost(ps)), () => Game.S.credits >= Game.StageCost(ps), () => Game.BuyStage(), "Basis ausbauen"); }
                else h.Card("Ausbaustufe " + (ps.sv.exp + 1), () => { string w = Game.ExpWhy(ps); return "+2 Bohrtürme, +25 % Bohrleistung, +30 % Lager, +1 Fahrzeugplatz, +1 Gebäudestufe. Es gibt kein Ende – jede Stufe kostet mehr." + (w != "" ? " " + w + "." : " Tiefe: " + Game.Fmt(Mathf.Min(ps.sv.deep, Game.ExpNeed(ps))) + " / " + Game.Fmt(Game.ExpNeed(ps)) + " E."); }, AccBld,
                    () => Cr(Game.ExpCost(ps)), () => Game.ExpWhy(ps) == "" && Game.S.credits >= Game.ExpCost(ps), () => Game.BuyExp(), ps.sv.exp > 0 ? "Ausbaustufe " + ps.sv.exp : null);
                h.Section("Gebäude");
                int n = 0;
                foreach (var id in GameData.BOrder)
                {
                    var B = GameData.Bld[id]; if (ps.bld.Contains(id) || B.stage > s + 1) continue; string key = id; n++;
                    h.Card(B.name, () => { string w = Game.BuildWhy(ps, key); return (w != "" ? w + " · " : "") + B.desc; }, AccBld, () => Cr(Game.BuildCost(ps, key)), () => Game.BuildWhy(ps, key) == "" && Game.S.credits >= Game.BuildCost(ps, key), () => Game.Build(key));
                }
                if (n == 0) h.Note("Alles gebaut, was diese Basisstufe erlaubt.");
                return;
            }
            if (tab == 1)
            {
                Specialize(h); WorkOrder(h);
                BaseMore(h);
                h.Section("Steht schon");
                h.Note(string.Join(" · ", GameData.BOrder.Where(ps.bld.Contains).Select(id => id == "presse" || id == "schmelze" || id == "raff" ? GameData.MachName(ps.pid, id) : GameData.Bld[id].name)));
                return;
            }
            foreach (var u in GameData.Upg.Where(x => (x.cat == "Basis" || x.cat == "Tiefenabbau" || x.cat == "Strahlenschutz") && UpVisible(x))) UpRow(h, u.id);
        }
        static void Specialize(Hud h)
        {
            h.Section("Ausrichtung");
            if (Game.Gs(ps) < 3) { h.Note("Ab Basisstufe 3 gibst du jedem Planeten eine Ausrichtung: Bergbau, Industrie oder Handel."); return; }
            string fit = Game.SpecFitOf(ps.pid); h.Note(ps.P.name + " passt am besten zu " + Game.Specs.First(x => x.id == fit).name + " – dort wirkt sie 1,5-mal so stark. " + (string.IsNullOrEmpty(ps.sv.spec) ? "Die erste Wahl ist kostenlos." : "Wechseln kostet " + Cr(Game.SpecCost(ps)) + "."));
            foreach (var d in Game.Specs) { var dd = d; h.Row(d.name + (d.id == fit ? " (passt)" : ""), () => dd.desc, () => ps.sv.spec == dd.id ? "Aktiv" : string.IsNullOrEmpty(ps.sv.spec) ? "Wählen" : Cr(Game.SpecCost(ps)), () => ps.sv.spec != dd.id && Game.S.credits >= Game.SpecCost(ps), () => Game.SetSpec(dd.id), ps.sv.spec != dd.id && !string.IsNullOrEmpty(ps.sv.spec)); }
        }
        static void WorkOrder(Hud h)
        {
            if (Game.Gs(ps) < 2) return; if (ps.sv.wo == null || ps.sv.wo.n <= 0) Game.NewWorkOrder(ps); var w = ps.sv.wo; if (w == null || w.m < 0) return;
            h.Section("Werksauftrag");
            h.Row("Liefere " + Game.Fmt(w.n) + " " + GameData.Mats[w.m].name + " per Rakete", () => Game.Fmt(Mathf.Min(ps.sv.wo.got, ps.sv.wo.n)) + " / " + Game.Fmt(ps.sv.wo.n) + " · Belohnung " + Cr(ps.sv.wo.r) + " · erfüllt: " + ps.sv.woDone + " (Ruf +" + Mathf.RoundToInt((float)(Game.WoRep(ps) - 1) * 100) + " % auf alle Preise hier)");
            h.Note("Gib die Ware zum Verkauf frei und stell den Produktionsplan so ein, dass sie entsteht.");
        }
        static void BaseMore(Hud h)
        {
            if (Game.IsDone(ps.pid))
            {
                h.Section("Tiefenabbau");
                h.Note(ps.P.name + " ist komplett sauber – jetzt geht es in die Tiefe. Bohrtürme auf sauberen Parzellen fördern dauerhaft Rohstoffe in ihr eigenes Lager, deine Fahrzeuge holen sie dort ab. Jede Basisstufe erlaubt 2 Bohrtürme mehr. Tippe eine saubere Parzelle an, um gezielt zu bauen.");
                h.Row("Bohrtürme", () => ps.sv.rigs.Count + " / " + Game.RigMax(ps) + " · Förderung " + Dec(ps.sv.rigs.Sum(r => Game.RigRate(ps, r)) * 60, 0) + " E pro Minute",
                    () => ps.sv.rigs.Count >= Game.RigMax(ps) ? "Voll" : "Bauen " + Cr(Game.RigCost(ps)), () => ps.sv.rigs.Count < Game.RigMax(ps) && Game.S.credits >= Game.RigCost(ps), () => Game.BuildRig(-1));
                foreach (var r in ps.sv.rigs) { var rr = r; h.Row((r.bag ? "Erzbagger" : "Bohrturm Stufe " + r.lv) + " · " + GameData.Mats[Game.RigMat(ps, r.cell)].name, () => Game.RigStatus(ps, rr), () => "Öffnen", () => true, () => h.OpenSheet("rig", rr.cell.ToString()), false); }
            }
            if (Game.HasLab()) h.Row("Forschung", () => Game.ResActive != null ? "Läuft: " + Game.ResActive.name + " · " + Mathf.FloorToInt(Game.S.resT / Game.ResActive.time * 100) + " %" : "Kein Projekt aktiv", () => "Öffnen", () => true, () => h.OpenSheet("research"), false);
        }

        static void Upgrades(Hud h)
        {
            h.Tabs("Hand", "Fahrzeuge", "Anlagen", "Wirtschaft");
            string[][] groups = { new[] { "Handgerät", "Basis" }, new[] { "Fahrzeuge", "Drohnen" }, new[] { "Entladestationen", "Förderbänder", "Maschinen", "Lager", "Außenlager" }, new[] { "Wirtschaft", "Verkauf", "Tiefenabbau", "Strahlenschutz" } };
            int tab = Mathf.Clamp(h.Tab, 0, groups.Length - 1);
            if (tab == 0) h.Note("Handgerät und Wirtschaft gelten überall. Fahrzeug- und Anlagen-Upgrades baust du auf jedem Planeten neu auf.");
            foreach (var cat in groups[tab])
            {
                var list = GameData.Upg.Where(u => u.cat == cat && UpVisible(u)).ToList(); if (list.Count == 0) continue;
                h.Section(cat); foreach (var u in list) UpRow(h, u.id);
            }
        }

        static void Planets(Hud h)
        {
            if (Game.S.done.Count > 0 || Game.S.stardust > 0) Stardust(h);
            h.Section("Planetenziele " + ps.P.name + " · " + ps.sv.pgDone.Count + "/" + Game.PlanetGoals.Length);
            for (int i = 0; i < Game.PlanetGoals.Length; i++)
            {
                int k = i; if (Game.PGoalClaimed(ps, k)) continue;
                h.Row(Game.PlanetGoals[k].text, () => Mathf.RoundToInt(Game.PGoalProg(ps, k) * 100) + " % · Belohnung " + Cr(Game.PGoalReward(ps, k)), () => Game.PGoalProg(ps, k) >= 1 ? "Abholen" : "Offen", () => Game.PGoalProg(ps, k) >= 1, () => Game.ClaimPGoal(k));
            }
            h.Section("Reiseziele");
            foreach (var pid in GameData.POrder)
            {
                var P = GameData.Planets[pid]; bool have = Game.P.ContainsKey(pid), cur = Game.S.planet == pid; string key = pid;
                string dirt = string.Join(", ", P.types.Select(t => GameData.Dirt[t.Key].name));
                if (GameData.PLiq.TryGetValue(pid, out var lq)) { var LD = GameData.Liq.Find(x => x.key == lq); if (LD != null) dirt += " · " + LD.name + "-Flächen: nur Schwebefahrzeuge oder Hand";}
                int nP = (have ? Game.P[pid].pd.N : GameData.Cells[pid].N); string size = nP.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) + " Parzellen";
                if (cur) h.Row(P.name, () => "Du bist hier · " + Mathf.FloorToInt(Game.CleanPct(Game.P[key]) * 100) + " % sauber · " + size + " · " + P.sub);
                else if (have) h.Row(P.name, () => Mathf.FloorToInt(Game.CleanPct(Game.P[key]) * 100) + " % sauber · " + size + " · " + P.sub, () => "Reisen", () => true, () => { Game.Travel(key); h.CloseSheet(); }, false);
                else
                {
                    int i = System.Array.IndexOf(GameData.POrder, pid); string prev = i > 0 ? GameData.POrder[i - 1] : null;
                    if (prev != null && Game.P.ContainsKey(prev)) h.Row(P.name, () => { string w = Game.UnlockWhy(key); return (w != "" ? w + " · " : "") + size + " · " + P.sub + " · " + dirt + (P.hzName != "" ? " · Gefahr: " + P.hzName : ""); }, () => Cr(P.unlockCost), () => Game.UnlockWhy(key) == "" && Game.S.credits >= P.unlockCost, () => { if (Game.Unlock(key)) h.CloseSheet(); });
                    else h.Row(P.name, () => size + " · " + P.sub + " · noch nicht erreichbar");
                }
            }
        }

        static float prestigeT = -99;
        static void Stardust(Hud h)
        {
            h.Section("Sternenstaub · " + Game.StardustFree() + " frei");
            h.Note("Jeder komplett saubere Planet bringt Sternenstaub (später in der Reihe = mehr), dazu 1 je 10 Funde. Mit einem Neustart beginnst du wieder auf dem Mond – Sternenstaub, Boni, Funde und Tagesbonus bleiben.");
            h.Row("Neustart", () => Time.unscaledTime - prestigeT < 4 ? "Wirklich? Noch einmal tippen setzt Planeten, Credits, Upgrades und Forschung zurück." : "Bringt jetzt +" + Game.StardustPending() + " Sternenstaub" + (Game.S.resets > 0 ? " · bisher " + Game.S.resets + " Neustarts" : ""),
                () => Time.unscaledTime - prestigeT < 4 ? "Ja, neu starten" : "Neu starten", () => Game.StardustPending() > 0, () => { if (Time.unscaledTime - prestigeT < 4) { Game.Prestige(); h.CloseSheet(); } else prestigeT = Time.unscaledTime; }, false);
            foreach (var n in FeatureData.Stardust) { var nn = n; h.Row(n.name, () => n.desc + (Game.SdWhy(nn) != "" && Game.SdWhy(nn) != "Gekauft" ? " · " + Game.SdWhy(nn) : ""), () => Game.S.sdNodes.Contains(nn.id) ? "Gekauft" : nn.cost + " Staub", () => Game.SdWhy(nn) == "", () => Game.SdBuy(nn)); }
        }

        static void Goals(Hud h)
        {
            Game.DailyContracts(); var cs = Game.S.contracts;
            h.Section("Tägliche Aufträge" + (cs.bonus ? " · alle geschafft" : " · alle drei: +50 % Bonus"));
            for (int i = 0; i < cs.list.Count; i++) { int k = i; var it = cs.list[k]; h.Row(Game.ContractText(it), () => it.done ? "Erledigt" : Game.Fmt(Mathf.Min((float)Game.ContractProg(it), (float)it.target)) + " / " + Game.Fmt(it.target) + " · Belohnung " + Cr(it.reward), () => it.done ? "Erledigt" : Game.ContractProg(it) >= it.target ? "Abholen" : "Offen", () => !it.done && Game.ContractProg(it) >= it.target, () => Game.ClaimContract(k)); }
            h.Section("Statistik");
            h.Row("Einnahmen", () => Cr(Game.IncomePerMin()) + " pro Minute (letzte 5 min) · insgesamt " + Cr(Game.S.stats.earned));
            h.Row(ps.P.name, () => { float cpm = Game.CellsPerMin(); int tot = ps.pd.cells.Count(x => !x.IsBase); int left80 = Mathf.Max(0, Mathf.CeilToInt(tot * .8f) - ps.nClean); return Mathf.RoundToInt(Game.CleanPct(ps) * 100) + " % sauber · " + Dec(cpm, 1) + " Parzellen pro Minute" + (left80 > 0 && cpm > .05f ? " · 80 % in etwa " + Mathf.CeilToInt(left80 / cpm) + " min" : ""); });
            h.Row("Gesamt", () => Game.S.stats.cells + " Parzellen gereinigt · " + Game.S.planets.Sum(p => p.nRockets) + " Raketen · " + Game.S.stats.crystals + " Kristalle · " + Cr(Game.S.stats.sold) + " verkauft");
            var ev = ps.sv.ev;
            if (ev != null && ev.key != "")
            {
                var E = Game.EvDef(ev.key); h.Section("Ereignis");
                h.Row(E != null ? E.name : "Ereignis", () => (E != null ? E.hint : "") + " · noch " + Mathf.CeilToInt(Mathf.Max(0, ps.sv.ev.t) / 60f) + " min", () => ps.sv.ev.cell >= 0 ? "Hinfliegen" : "OK", () => true, () => { h.CloseSheet(); h.OnFocusEvent?.Invoke(); }, false);
            }
            h.Section("Funde");
            foreach (var kv in FeatureData.Finds)
            {
                if (!Game.P.ContainsKey(kv.Key)) continue; string pid = kv.Key; var al = kv.Value;
                h.Row(GameData.Planets[pid].name + " · " + Game.FindsOn(pid) + "/" + al.Length, () => string.Join(" · ", al.Select((f, i) => Game.HasFind(pid, i) ? f.name : "???")) + (Game.FindsOn(pid) >= al.Length ? " · Album komplett: +5 % Erlös" : " · Funde gibt es beim Reinigen (3 %, auf seltsamen Parzellen 8 %)"));
            }
            h.Section("Erfolge · " + Game.S.achievements.Count + "/" + Game.Achievements.Count);
            foreach (var a in Game.Achievements) { var aa = a; h.Row(a.name, () => aa.desc + (Game.S.achievements.Contains(aa.id) ? " · geschafft" : "")); }
            if (Game.S.daily != null && Game.S.daily.streak > 0) h.Note("Tagesbonus: Tag " + Game.S.daily.streak + " von 7 – komm morgen wieder, die Belohnung steigt.");
            h.Section("Hauptziele");
            for (int i = 0; i < Mathf.Min(Game.QuestsSupported, GameData.Quests.Count); i++)
            {
                int q = i; var Q = GameData.Quests[i];
                h.Row((q == Game.S.quest ? "Jetzt: " : "") + Q.t, () => q < Game.S.quest ? "Erledigt" : q == Game.S.quest ? Mathf.FloorToInt(Mathf.Clamp01(Game.QuestProgress(q)) * 100) + " % · Belohnung " + Cr(Q.r) : "Belohnung " + Cr(Q.r));
            }
            if (GameData.Quests.Count > Game.QuestsSupported) h.Note("Die übrigen " + (GameData.Quests.Count - Game.QuestsSupported) + " Ziele kommen mit den nächsten Systemen in der Unity-Version (Neustart mit Sternenstaub).");
        }

        static string Dec(float x, int d) => x.ToString(d == 0 ? "0" : d == 1 ? "0.0" : "0.00");
        static string FmtTime(float s) { int t = Mathf.Max(0, Mathf.CeilToInt(s)); return t >= 60 ? t / 60 + " min " + (t % 60).ToString("00") + " s" : t + " s"; }
        static void Rig(Hud h, int cell)
        {
            var r = ps.sv.rigs.Find(x => x.cell == cell); int m = Game.RigMat(ps, cell); float rich = Game.RigRich(ps.pid, cell); var raws = Game.PlanetRaws(ps);
            if (r != null)
            {
                h.Section(r.bag ? "Erzbagger" : "Bohrturm · Stufe " + r.lv + " / " + Game.RIG_MAXLV);
                h.Note((r.bag ? "Gräbt alle Rohstoffe des Planeten zu gleichen Teilen aus. " : "") + "Fördert " + GameData.Mats[m].name + " aus der Tiefe und sammelt es im eigenen Lager. Deine Fahrzeuge holen es ab, sobald sie nichts zu reinigen haben oder das Lager zu 70 % voll ist. Braucht Strom – auch nachts.");
                h.Row("Status", () => Game.RigStatus(ps, r));
                h.Row("Vorkommen", () => Game.RichTxt(rich) + " · ×" + Dec(rich, 2) + (raws.Count > 1 && !r.bag ? " · 75 % " + GameData.Mats[m].name + ", 25 % " + string.Join(", ", raws.Where(q => q != m).Select(q => GameData.Mats[q].name)) : ""));
                h.Row("Förderung", () => Dec(Game.RigRate(ps, r) * 60, 0) + " pro Minute · Lager " + Game.Fmt(Game.Sum(r.buf)) + " / " + Game.RigBufCap(r).ToString("0") + " E · gefördert " + Game.Fmt(r.mined));
                h.Row("Störungen", () => r.faultK != "" ? Game.FaultName(r) + " – Techniker in " + Mathf.CeilToInt(r.faultT) + " s" : "keine · bisher " + r.fixedN, () => "Beheben " + Cr(Game.FaultFixCost(ps, r)), () => r.faultK != "" && Game.S.credits >= Game.FaultFixCost(ps, r), () => Game.FixFault(cell));
                if (!r.bag && r.lv < Game.RIG_MAXLV) h.Row("Bohrturm verbessern", () => "Stufe " + r.lv + " → " + (r.lv + 1) + " · +30 % Förderung. Auf Stufe " + Game.RIG_MAXLV + " kannst du ihn zum Erzbagger umbauen.", () => Cr(Game.RigUpCost(ps, r)), () => Game.S.credits >= Game.RigUpCost(ps, r), () => Game.RigUp(cell));
                else if (!r.bag) h.Row("Zum Erzbagger umbauen", () => "×2,5 Förderung, alle Rohstoffe zu gleichen Teilen, doppeltes Lager.", () => Cr(Game.BagCost(ps)), () => Game.S.credits >= Game.BagCost(ps), () => Game.RigToBagger(cell));
                foreach (var id in new[] { "rcare", "rigrate" }) if (UpVisible(GameData.Up[id])) UpRow(h, id);
                return;
            }
            h.Section("Tiefenabbau");
            string w = Game.RigWhy(ps, cell);
            h.Note(w != "" ? w + "." : "Tiefenvorkommen: " + Game.RichTxt(rich) + " (×" + Dec(rich, 2) + ") " + GameData.Mats[m].name + ". Ein Bohrturm fördert hier dauerhaft, deine Fahrzeuge holen die Ladung ab.");
            if (w == "") h.Row("Bohrturm bauen", () => ps.sv.rigs.Count + " von " + Game.RigMax(ps), () => Cr(Game.RigCost(ps)), () => Game.S.credits >= Game.RigCost(ps), () => { if (Game.BuildRig(cell)) h.OpenSheet("rig", cell.ToString()); });
        }

        static void Outpost(Hud h, int cell)
        {
            var o = ps.sv.outposts.Find(x => x.cell == cell);
            if (o != null)
            {
                h.Note("Entladestation weit draußen. Alle Fahrzeugtypen docken hier an, eine Pipeline pumpt alles ins Rohstofflager der Basis.");
                h.Row("Status", () => Game.OutpostStatus(ps, o));
                h.Row("Puffer", () => Game.Fmt(Game.Sum(o.buf)) + " / " + Game.Fmt(Game.StBuf(ps, o.id)));
                h.Row("Andockplätze", () => (o.docks == null ? 0 : o.docks.Count(x => x != 0)) + " belegt von " + Game.StDocks(ps, o.id));
                h.Row("Pipeline", () => { float pf = Game.PressF(Game.Sum(o.buf) / Game.StBuf(ps, o.id)); return Dec(Game.PipeRate(ps, o), 0) + " E/s" + (pf > 1.01f ? " · Druckbetrieb ×" + Dec(pf, 1) : ""); });
                h.Note("Ab 80 % Füllung arbeitet die Pipeline im Druckbetrieb – bis doppelt so schnell. Drohnen holen volle Silos ab.");
                foreach (var id in new[] { "obuf", "pipe", "urate", "udock" }) if (UpVisible(GameData.Up[id])) UpRow(h, id);
                return;
            }
            h.Section("Außenlager-Platz");
            if (!Game.ResDone("outpost")) { h.Note("Mit Außenlager-Logistik (Forschung) kannst du hier später ein Außenlager mit Pipeline zur Basis bauen."); return; }
            h.Note("Einer von vier festen Plätzen rund um die Basis. Fahrzeuge, die hier draußen reinigen, entladen dann direkt hier – eine Pipeline pumpt alles zur Basis.");
            h.Row("Außenlager bauen", () => { string w = Game.OutpostWhy(ps, cell); return (w != "" ? w + " · " : "") + ps.sv.outposts.Count + " von " + GameData.OutMax + " gebaut"; },
                () => Cr(Game.OutpostCost(ps)), () => Game.OutpostWhy(ps, cell) == "" && Game.S.credits >= Game.OutpostCost(ps), () => Game.BuildOutpost(cell));
        }

        static void Hazard(Hud h)
        {
            var P = ps.P; var st = P.storm;
            h.Section(string.IsNullOrEmpty(P.hzName) ? "Keine besonderen Gefahren" : P.hzName);
            if (!string.IsNullOrEmpty(P.hzDesc)) h.Note(P.hzDesc);
            if (st != null)
            {
                h.Row("Sturm", () => st.name + " · alle " + FmtTime(st.every) + " · Dauer " + FmtTime(st.dur));
                h.Row("Nächster", () => Game.StormOn(ps) ? "tobt gerade · noch " + Mathf.CeilToInt(ps.sv.hzOn) + " s" : "in " + Mathf.CeilToInt(Game.StormIn(ps)) + " s");
                h.Row("Wirkung", () => Game.StormFx(st));
                h.Section("Sturmschild");
                if (Game.ResDone("shield")) h.Row("Schild automatisch aktivieren", () => "Schwächt jeden Sturm um 80 %, kostet währenddessen 4 Energie/s. Gilt für diesen Planeten, auch offline.", () => ps.sv.shield ? "An" : "Aus", () => true, () => Game.ToggleShield(), false);
                else h.Note("Erforsche den Sturmschild in der Forschungsstation, dann kannst du Stürme fast komplett abwehren.");
            }
            if (P.rad > 0) { h.Section("Strahlung"); h.Row("Verschleiß durch Strahlung", () => "Fahrzeuge verschleißen hier ×" + Dec(1 + 3 * Game.RadF(ps), 1) + " so schnell" + (Game.StormOn(ps) ? " – der Sturm verstärkt die Strahlung" : "") + "."); if (UpVisible(GameData.Up["rshield"])) UpRow(h, "rshield"); }
            if (ps.sv.veh.Count > 0 || Game.PowReq(ps.pid) > 0) h.Row("Fahrzeugleistung", () => Game.PowOK(ps) ? "Deine Fahrzeuge sind stark genug für " + P.name : "Doppelbürsten Stufe " + Game.PowReq(ps.pid) + " nötig – sonst reinigen Fahrzeuge hier nur mit 30 %");
        }

        static void Energy(Hud h)
        {
            var P = ps.P;
            if (P.storm != null) h.BigButton("Gefahren & Sturmschild", null, () => h.OpenSheet("hazard"));
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

        static void Lifter(Hud h)
        {
            h.Section("Schwerlast-Hangar");
            h.Note("Sucht sich ein Industriesauger, Truck oder Spezialfahrzeug eine Parzelle, die weiter als " + GameData.LiftMinDist.ToString("0") + " m entfernt ist, holt eine Lastdrohne die Maschine ab und fliegt sie direkt hin. Kleine Fahrzeuge fahren selbst.");
            h.Row("Lastdrohne kaufen", () => ps.sv.lifters.Count + " von " + GameData.LiftMax + " · " + Dec(GameData.LiftSpeed * Game.Uv("dspeed"), 1) + " m/s",
                () => ps.sv.lifters.Count >= GameData.LiftMax ? "Voll" : Cr(Game.LiftCost(ps)), () => ps.sv.lifters.Count < GameData.LiftMax && Game.S.credits >= Game.LiftCost(ps), () => Game.BuyLifter());
            h.Row("Eingeflogene Fahrzeuge", () => Game.S.stats.airlifts + " insgesamt");
            foreach (var L in ps.sv.lifters) { var l = L; h.Row("Lastdrohne #" + l.id, () => Game.LifterStatus(ps, l)); }
            if (UpVisible(GameData.Up["dspeed"])) UpRow(h, "dspeed");
        }

        static void Hangar(Hud h)
        {
            h.Section("Drohnenhangar");
            h.Note("Drohnen fliegen zu Fahrzeugen, die weiter als " + GameData.DroneMinDist.ToString("0") + " m von hier arbeiten und mindestens 40 % Ladung haben, übernehmen die Ladung und bringen sie zur Entladestation mit dem meisten Platz. Die Fahrzeuge müssen nicht mehr zurückfahren – auch offline.");
            h.Row("Transportdrohne kaufen", () => ps.sv.drones.Count + " von " + GameData.DroneMax + " · Ladung " + Game.Fmt(Game.DroneCap(ps)) + " · " + Dec(Game.DroneSpeed(ps), 1) + " m/s",
                () => ps.sv.drones.Count >= GameData.DroneMax ? "Voll" : Cr(Game.DroneCost(ps)), () => ps.sv.drones.Count < GameData.DroneMax && Game.S.credits >= Game.DroneCost(ps), () => Game.BuyDrone());
            h.Row("Übernommene Ladungen", () => Game.S.stats.lifts + " insgesamt");
            foreach (var dr in ps.sv.drones) { var d = dr; h.Row("Drohne #" + d.id, () => Game.DroneStatus(ps, d) + " · " + Game.Fmt(Game.Sum(d.tank)) + "/" + Game.Fmt(Game.DroneCap(ps))); }
            foreach (var id in new[] { "dcap", "dspeed" }) if (UpVisible(GameData.Up[id])) UpRow(h, id);
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
            h.Section("Foto-Modus");
            h.Row("Oberfläche ausblenden", () => "Für schöne Bilder deiner Basis. Taste P oder ein Tipp oben links blendet sie wieder ein.", () => "Foto-Modus", () => true, () => { h.CloseSheet(); h.OnPhoto?.Invoke(); }, false);
            h.Section("Ton");
            h.Row("Geräusche", () => "Wischen, Kasse, Rakete, Kristalle, Funde", () => Game.S.sound ? "An" : "Aus", () => true, () => Game.S.sound = !Game.S.sound, false);
            h.Row("Musik", () => "Leiser Klangteppich, wird lauter, wenn deine Basis arbeitet", () => Game.S.music ? "An" : "Aus", () => true, () => Game.S.music = !Game.S.music, false);
            h.Slider("Lautstärke Geräusche", () => Game.S.volSfx, v => Game.S.volSfx = v);
            h.Row("Vibration", () => "Kurz bei Raketenstart, Funden und Explosionen (nur am Handy)", () => Game.S.vibrate ? "An" : "Aus", () => true, () => Game.S.vibrate = !Game.S.vibrate, false);
            h.Slider("Lautstärke Musik", () => Game.S.volMusic, v => Game.S.volMusic = v);
            h.Section("Spielstand");
            h.Row("Jetzt speichern", () => "Das Spiel speichert alle 10 Sekunden und beim Schließen von selbst.", () => "Speichern", () => true, () => { Game.Save(); Game.Toast("Gespeichert", "good"); }, false);
            h.Row("Neu anfangen", () => Time.unscaledTime - resetT < 4 ? "Wirklich? Noch einmal tippen löscht deinen Unity-Spielstand." : "Löscht den Spielstand dieser Unity-Version und startet auf dem Mond.", () => Time.unscaledTime - resetT < 4 ? "Ja, löschen" : "Neu anfangen", () => true,
                () => { if (Time.unscaledTime - resetT < 4) { Game.ResetAll(); h.CloseSheet(); } else resetT = Time.unscaledTime; }, false);
            if (Game.TestMode)
            {
                h.Section("Testmodus");
                h.Row("Alles maximal ausbauen", () => "Basisstufe, alle Gebäude, Upgrades, Forschungen, Außenlager und Fahrzeuge auf diesem Planeten (Editor: F12).", () => "Ausbauen", () => true, () => { Game.DevMaxOut(); h.CloseSheet(); }, true);
                h.Row("+1 Mio Credits", () => "Geld zum Ausprobieren.", () => "+1 Mio", () => true, () => Game.Earn(1e6), false);
                h.Row("Planet säubern", () => "Erst auf 81 %, beim zweiten Mal komplett sauber.", () => "Säubern", () => true, () => Game.DevCleanTo(Game.CleanPct(Game.Cur) >= .8f ? 1.01f : .81f), false);
                h.Row("Vierteltag vorspulen", () => "Tag und Nacht testen.", () => "Vorspulen", () => true, () => { var cs = Game.Cur.sv; cs.dayT = (cs.dayT < 0 ? 0 : cs.dayT) + Game.DayLen(Game.Cur.pid) / 4; }, false);
                h.Row("Sturm auslösen", () => Game.Cur.P.storm != null ? "Der nächste Sturm kommt in 25 Sekunden." : "Auf diesem Planeten gibt es keine Stürme.", () => "Sturm", () => Game.Cur.P.storm != null, () => { var cs = Game.Cur.sv; cs.hzOn = 0; cs.hzWarned = false; cs.hzT = Game.Cur.P.storm.every - 25; }, false);
            }
            if (Debug.isDebugBuild && !Application.isEditor)
                h.Row("Testmodus " + (Game.TestMode ? "an" : "aus"), () => Game.TestMode ? "Du spielst mit einem eigenen Test-Spielstand. Ausschalten und die Seite neu laden = normaler Spielstand." : "Normaler Spielstand. Einschalten und die Seite neu laden = Test-Spielstand mit Test-Knöpfen.",
                    () => Game.TestMode ? "Ausschalten" : "Einschalten", () => true, () => { Game.Save(); PlayerPrefs.SetInt("LunarCleanup.TestMode", Game.TestMode ? 0 : 1); PlayerPrefs.Save(); Game.Toast("Gespeichert – jetzt die Seite neu laden", "good"); }, false);
            h.Section("Konto-Speicher");
            h.Row(Platform.Cloud.Name, () => Platform.Cloud.SignedIn ? "Verbunden – dein Spielstand wird zusätzlich in deinem Konto gesichert." : "Noch nicht verbunden. Auf dem Handy sicherst du hier deinen Spielstand in deinem " + Platform.Cloud.Name + "-Konto.",
                () => Platform.Cloud.SignedIn ? "Verbunden" : "Anmelden", () => !Platform.Cloud.SignedIn, () => Platform.Cloud.SignIn(ok => Game.Toast(ok ? "Angemeldet – Spielstand wird gesichert" : "Anmeldung kommt mit der Handy-Version", ok ? "good" : "")), false);
            h.Section("Shop (Vorschau)");
            h.Note("Vorschau der späteren Angebote. Noch wird nichts berechnet und nichts vergeben.");
            foreach (var o in Platform.Offers) { var oo = o; h.Row(o.name, () => oo.desc, () => oo.price, () => true, () => Platform.Buy(oo, ok => Game.Toast(ok ? oo.name + " gekauft – danke!" : "Käufe sind noch nicht freigeschaltet – es wurde nichts berechnet", ok ? "good" : "")), true); }
            h.Section("Über");
            h.Note("Lunar Cleanup Tycoon · Unity-Version 0.10. Planeten, Parzellen, Zahlen und Texte stammen 1:1 aus dem Browser-Prototyp. Noch nicht übertragen: Meteoritenschauer, Ausbaustufen, Sternenstaub-Neustart.");
        }

        /// <summary>Welche Upgrades zu welchem Gebäude gehören (Antippen eines Gebäudes zeigt sie oben im Fenster).</summary>
        static readonly Dictionary<string, string[]> BldUpg = new Dictionary<string, string[]>
        {
            { "u1", new[] { "urate", "ubuf", "udock" } }, { "u2", new[] { "urate", "ubuf", "udock" } }, { "u3", new[] { "urate", "ubuf", "udock" } },
            { "roh", new[] { "rohcap", "bspeed", "bcap", "beff" } }, { "prod", new[] { "prodcap", "bspeed", "bcap", "beff" } },
            { "presse", new[] { "pvol", "pspeed", "mspeed", "mcap", "meff", "mqual" } }, { "schmelze", new[] { "mspeed", "mcap", "meff", "mqual" } }, { "raff", new[] { "mspeed", "mcap", "meff", "mqual" } },
            { "sell", new[] { "sellcap", "rspeed", "price", "autosell" } }, { "port", new[] { "sellcap", "rspeed", "price", "autosell" } },
            { "energy", new[] { "energy", "ecap" } }, { "rtg", new[] { "ecap" } }, { "geo", new[] { "ecap" } }, { "reactor", new[] { "ecap" } },
            { "research", new[] { "research" } }, { "hq", new[] { "slots" } },
            { "werkstatt", new[] { "vdur", "vspeed", "vpower", "vtank", "veff", "vbatt", "vradius", "slots" } },
            { "hangar", new[] { "dcap", "dspeed" } }, { "lifter", new[] { "dcap", "dspeed" } },
            { "outpost", new[] { "obuf", "pipe" } }, { "rig", new[] { "rigrate", "rcare", "rshield" } },
        };
        static void BuildingUpgrades(Hud h, string key)
        {
            if (!BldUpg.TryGetValue(key, out var ids)) return;
            var list = ids.Where(x => GameData.Up.ContainsKey(x) && UpVisible(GameData.Up[x])).ToList(); if (list.Count == 0) return;
            h.Section("Upgrades für dieses Gebäude"); foreach (var x in list) UpRow(h, x);
        }

        static void Building(Hud h, string id)
        {
            var B = GameData.Bld[id]; h.Note(B.desc);
            BuildingUpgrades(h, id); BuildingLevel(h, id);
            if (id == "roh" || id == "prod" || id == "sell" || GameData.Stations.Contains(id) || id == "presse" || id == "schmelze" || id == "raff") { ProductionOverview(h); if (id == "sell" || id == "roh" || id == "prod") Selling(h); return; }
            if (id == "energy" || id == "rtg" || id == "geo" || id == "reactor") { Energy(h); return; }
            if (id == "research") { Research(h); return; }
            if (id == "hangar") { Hangar(h); return; }
            if (id == "lifter") { Lifter(h); return; }
            if (id == "hq") { BaseMore(h); return; }
            if (id == "werkstatt") { h.Note("Fahrzeuge und Wartung findest du unten unter „Fahrzeuge“."); return; }
        }
    }
}
