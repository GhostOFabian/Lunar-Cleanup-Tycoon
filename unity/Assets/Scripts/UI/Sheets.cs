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
                case "flag": { int fi = int.TryParse(arg, out var q) ? q : -1; title = "Flagge"; FlagSheet(h, fi); break; }
                case "story": title = "Geschichte"; StorySheet(h, arg); break;
                case "vehicle": { var v = ps.sv.veh.FirstOrDefault(x => x.id.ToString() == arg); if (v == null) { title = "Fahrzeuge"; Vehicles(h); } else { title = Game.VehName(v); Vehicle(h, v); } break; }
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
                case "perks": title = Game.Hard ? "Inventar" : "Perks"; PerkShop(h); break;
                case "hard": title = "Hard Mode"; HardSheet(h); break;
                case "crystals": title = "Kristalle"; CrystalSheet(h); break;
                case "pylon": { int pc = int.TryParse(arg, out var q2) ? q2 : -1; bool has = ps.sv.pylons != null && ps.sv.pylons.Contains(pc); title = has ? "Ladesäule" : "Saubere Parzelle"; PylonSheet(h, pc, has); break; }
                case "building": title = GameData.Bld[arg].id == "presse" || GameData.Bld[arg].id == "schmelze" || GameData.Bld[arg].id == "raff" ? GameData.MachName(ps.pid, arg) : GameData.Bld[arg].name; Building(h, arg); break;
            }
        }

        /// <summary>Fabian 03.11.: Hard Mode aus dem Startmenü – was er ist und wie man ihn freischaltet.</summary>
        static void HardSheet(Hud h)
        {
            h.Note("Ein eigener, neuer Spielstand ab dem Mond. Keine Putzfahrzeuge, keine Drohnen, keine Bohrtürme – du putzt alles selbst. Ein fliegendes Sammelshuttle bringt deine Ladung zur Basis, Perks findest du beim Wischen, und dein Anzug braucht Energie – Ladesäulen bringen dich weiter hinaus. Dein normaler Spielstand bleibt unberührt.");
            if (Game.Hard || Game.HardOpen)
            {
                h.Row(Game.Hard ? "Hard Mode (aktiv)" : "Hard Mode ist frei", () => Game.Hard ? "Du spielst gerade den Hard Mode." : "Du kannst jederzeit wechseln – auch später in den Einstellungen.",
                    () => Game.Hard ? "Zum normalen Spiel" : "Hard Mode spielen", () => true, () => { h.CloseSheet(); h.HideMenu(); Game.SwitchHard(!Game.Hard); }, !Game.Hard);
                return;
            }
            h.Callout("So schaltest du ihn frei", "Säubere im normalen Spiel alle " + GameData.POrder.Length + " Planeten komplett – oder schalte ihn direkt im Shop frei.");
            int n = Game.HardDoneN, all = GameData.POrder.Length;
            h.Row("Alle Planeten säubern", () => n + " von " + all + " Planeten komplett sauber" + (n < all ? " · als Nächstes: " + NextDirty() : ""));
            var o = Platform.Offers.FirstOrDefault(x => x.id == "hardmode");
            if (o != null) h.Row(o.name, () => o.desc, () => o.price, () => true, () => Platform.Buy(o, ok => Game.Toast(ok ? o.name + " gekauft – danke!" : "Käufe sind noch nicht freigeschaltet – es wurde nichts berechnet", ok ? "good" : "")), true);
        }
        static string NextDirty() { foreach (var p in GameData.POrder) if (!Game.S.done.Contains(p)) return GameData.Planets[p].name + (Game.P.ContainsKey(p) ? " (" + Mathf.FloorToInt(Game.CleanPct(Game.P[p]) * 100) + " %)" : " (noch nicht freigeschaltet)"); return "–"; }
        /// <summary>Fabian 03.11.: Perk-Shop (Hard Mode).</summary>
        /// <summary>Fabian 04.11.: Ladesäule bauen oder abbauen – je näher an einer Ladestelle, desto länger hält der Anzug.</summary>
        static void PylonSheet(Hud h, int c, bool has)
        {
            var d = ps.pd.cells[c].c.normalized;
            h.Note("Wischen kostet Anzug-Energie. Im Umkreis von " + Mathf.RoundToInt(Game.SUIT_NEAR) + " m um das Hauptgebäude oder eine Ladesäule verbrauchst du am wenigsten, je weiter weg, desto mehr. Hier gerade: " + Game.SuitDrain(ps, d).ToString("0.0").Replace('.', ',') + " je Sekunde (Abstand " + Mathf.RoundToInt(Game.ChargeDist(ps, d)) + " m).");
            if (has) { h.Row("Ladesäule", () => "Steht hier · " + (ps.sv.pylons?.Count ?? 0) + "/" + Game.PylonMax(ps) + " Ladesäulen auf " + ps.P.name, () => "Abbauen", () => true, () => { Game.RemovePylon(c); h.CloseSheet(); }, false); return; }
            h.Row("Ladesäule bauen", () => "Kostet " + Game.Fmt(Game.PylonCost(ps)) + " E Rohstoffe aus dem Lager · " + (ps.sv.pylons?.Count ?? 0) + "/" + Game.PylonMax(ps) + " gebaut", () => "Bauen", () => Game.PylonWhy(ps, c) == "", () => { if (Game.BuildPylon(c)) h.CloseSheet(); }, true, null, true, () => Game.PylonWhy(ps, c));
        }
        /// <summary>Fabian 04.11.: Kristalle ausgeben.</summary>
        static void CrystalSheet(Hud h)
        {
            h.Row(Game.Kristalle + " Kristalle", () => "Leuchtkristalle wachsen auf sauberen Parzellen. Antippen oder von Fahrzeugen ernten lassen – größere und seltenere Kristalle bringen mehr." + (Game.Hard ? " Im Hard Mode gibt jeder selbst geernteter Kristall +25 Anzug-Energie und +3 % Ulti." : ""), titleLive: () => Game.Kristalle + " Kristalle");
            h.Section("Ausgeben");
            h.Row("Kristall-Schub", () => "Alle Fahrzeuge, Bänder und Maschinen auf " + ps.P.name + " 5 min lang 50 % schneller" + (Game.BoostLeft(ps) > 0 ? " · läuft noch " + Mathf.CeilToInt(Game.BoostLeft(ps) / 60f) + " min (verlängert sich)" : ""), () => Game.BOOST_COST + " Kristalle", () => Game.Kristalle >= Game.BOOST_COST, () => Game.CrystalBoost(), true, null, true, () => "Es fehlen Kristalle – " + Game.BOOST_COST + " nötig");
            if (Game.ResActive != null) h.Row("Forschung sofort fertig", () => Game.ResActive.name, () => Game.ResInstantCost() + " Kristalle", () => Game.Kristalle >= Game.ResInstantCost(), () => Game.ResInstant(), true, null, true, () => "Es fehlen Kristalle – " + Game.ResInstantCost() + " nötig");
            if (Game.S.ruin != "" && Game.Ruined(Game.S.ruin)) h.Row("Wiederaufbau sofort", () => GameData.Planets[Game.S.ruin].name + " ist sofort wieder bereit", () => Game.RuinInstantCost() + " Kristalle", () => Game.Kristalle >= Game.RuinInstantCost(), () => Game.RuinInstant(), true, null, true, () => "Es fehlen Kristalle – " + Game.RuinInstantCost() + " nötig");
            if (Game.Hard)
            {
                h.Row("Anzug sofort voll", () => "Volle Anzug-Energie, auch weit draußen", () => "2 Kristalle", () => Game.Kristalle >= 2, () => Game.SuitFill(), true, null, true, () => "Es fehlen Kristalle – 2 nötig");
                h.Row("Ulti +20 %", () => "Lädt die Ultimate-Leiste um ein Fünftel", () => "5 Kristalle", () => Game.Kristalle >= 5 && Game.Ult < 1, () => Game.UltBoost(), true, null, true, () => Game.Ult >= 1 ? "Die Ulti ist schon voll" : "Es fehlen Kristalle – 5 nötig");
            }
        }
        static string Rarity(string id) => id == "tornado" ? "sehr selten" : id == "nuke" ? "nur über die Ulti-Leiste" : new[] { "foam", "kaercher", "espresso", "goldmop", "rohrpost" }.Contains(id) ? "selten" : "häufig";
        /// <summary>Fabian 04.11.: im Hard Mode gibt es nichts mehr zu kaufen – Perks findest du beim Selbstwischen. Das Fenster ist dein Inventar.</summary>
        static void PerkShop(Hud h)
        {
            if (!Game.Hard) { h.Note("Perks gibt es im Hard Mode (Einstellungen → Spielmodus)."); return; }
            h.Row("Putz-Level " + Game.HardLevel, () => { var nx = Game.Talents.FirstOrDefault(t => t.lv > Game.HardLevel); return Mathf.FloorToInt(Game.LevelXp) + " / " + Mathf.FloorToInt(Game.LevelNeed(Game.HardLevel)) + " Erfahrung bis Level " + (Game.HardLevel + 1) + (nx != null ? " · nächstes Talent: " + nx.name + " (" + nx.desc + ")" : "") + " · Ultimate " + Mathf.FloorToInt(Game.Ult * 100) + " %"; }, titleLive: () => "Putz-Level " + Game.HardLevel);
            h.Tabs("Inventar", "Talente", "Crew"); int tab = h.Tab;
            if (tab == 1)
            {
                h.Note("Selbst wischen gibt Erfahrung (1 je Parzelle, Kombos mehr). Jedes Level schaltet ein Talent frei – es wirkt sofort.");
                foreach (var T in Game.Talents) { var tt = T; h.Row(tt.name, () => (Game.HardLevel >= tt.lv ? "Aktiv · " : "Ab Level " + tt.lv + " · ") + tt.desc); }
                return;
            }
            if (tab == 2)
            {
                h.Note("Putzhelfer laufen mit einem Besen über den Planeten und fegen Parzelle für Parzelle – auch wenn du nicht da bist.");
                foreach (var P in Game.Perks.Where(x => x.kind == 2)) { var pp = P; h.Row(P.name, () => pp.desc + " · auf " + ps.P.name + ": " + ps.sv.helpers.Count(x => x.life <= 0) + "/" + Game.HELPER_MAX, () => Cr(Game.PerkCost(pp.id)), () => Game.PerkWhy(pp.id) == "", () => Game.BuyPerk(pp.id), true, null, true, () => Game.PerkWhy(pp.id)); }
                return;
            }
            h.Note("Perks findest du nur beim Selbstwischen – manchmal springt eine Kiste aus der Parzelle. Häufig: Bombe, Turbo, Breiter Pinsel, Magnet · selten: Schaum, Kärcher, Espresso, Goldener Mopp, Rohrpost · sehr selten: Tornado. Die Nuke lädst du über die Ulti-Leiste.");
            foreach (var P in Game.Perks.Where(x => x.kind != 2))
            {
                var pp = P;
                h.Row(P.name, () => pp.desc + " · " + Rarity(pp.id) + " · dabei: " + Game.PerkN(pp.id) + "/" + Game.PerkCap(pp.id) + (pp.kind == 1 && Game.BuffLeft(pp.id) > 0 ? " · läuft noch " + Mathf.CeilToInt(Game.BuffLeft(pp.id)) + " s" : ""),
                    () => Game.PerkN(pp.id) > 0 ? "Einsetzen" : "–", () => Game.PerkN(pp.id) > 0, () => { if (pp.kind == 0) { Hud.PerkReq = pp.id; h.CloseSheet(); } else Game.UseBuff(pp.id); }, false, null, true, () => pp.id == "nuke" ? "Die Nuke bekommst du, wenn die Ulti-Leiste voll ist" : "Noch nicht gefunden – wisch weiter selbst, " + Rarity(pp.id) + "er Fund");
            }
        }
        static void UpRow(Hud h, string id)
        {
            var u = GameData.Up[id];
            h.Row(Game.Hard && id == "cartspd" ? "Schnelles Sammelshuttle" : u.name, () => { int l = Game.Lvl(id); return "Stufe " + l + "/" + u.max + (u.loc ? " · nur " + ps.P.name : "") + " · " + u.label + ": " + Game.UShow(id, l) + (l < u.max ? " → " + Game.UShow(id, l + 1) : "") + (Game.UGoods(id).Length > 0 ? " · Top-Stufe braucht " + Game.GoodsTxt(Game.UGoods(id)) + " (Lager: " + Mathf.FloorToInt(Game.GoodAmt(Game.UGoods(id)[0].g)) + ")" : ""); },
                () => Game.Lvl(id) >= u.max ? "Max" : Cr(Game.UCost(id)), () => Game.Lvl(id) < u.max && Game.S.credits >= Game.UCost(id) && Game.HasGoods(Game.UGoods(id)),
                () => { Game.BuyUpgrade(id); if (Game.Lvl(id) >= u.max) h.Rebuild(); }, true, null, false, () => { var ps0 = Game.Cur; string w = u.stage > 0 && Game.Gs(ps0) < u.stage ? "Erst Basisstufe " + u.stage : !Game.HasGoods(Game.UGoods(id)) ? "Es fehlen " + Game.GoodsTxt(Game.UGoods(id)) + " (von " + GameData.Planets[Game.Needs(ps0.pid)[0]].name + ")" : ""; return Game.Steps(w, Game.UCost(id)); }); /* Fabian 01.11.: Zeile bleibt stehen, erst bei Max wandert sie nach unten */
        }
        static bool IsMax(string id) => Game.Lvl(id) >= GameData.Up[id].max;
        static void MaxedRows(Hud h, List<string> ids)
        {
            if (ids.Count == 0) return; h.Section("Ausgereizt (Max)"); foreach (var x in ids) UpRow(h, x);
        }
        /// <summary>Upgrades für Systeme, die in Unity noch fehlen (Tiefenabbau), bleiben bis dahin ausgeblendet – kein Knopf ohne Wirkung.</summary>
        static readonly string[] NotPorted = { };
        static bool UpVisible(UpgDef u) => Game.HardUpgOk(u) && (!u.hide || (Game.Hard && u.id == "cartspd")) && /* 03.11.: Hard Mode */ System.Array.IndexOf(NotPorted, u.id) < 0 && (!u.done || Game.CleanPct(ps) >= .999f) && (!u.rad || ps.P.rad > 0) && (u.stage == 0 || Game.Gs(ps) >= u.stage) && (string.IsNullOrEmpty(u.res) || Game.ResDone(u.res));

        static readonly Color AccVeh = new Color(.43f, .95f, .82f), AccBld = new Color(1f, .75f, .3f), AccDrone = new Color(.5f, .7f, 1f);
        static void Vehicles(Hud h)
        {
            if (Game.Hard) /* Fabian 03.11.: Hard Mode – nur das Sammelshuttle */
            {
                h.Section("Hard Mode · Sammelshuttle");
                h.Note("Im Hard Mode putzt du alles selbst. Ist dein Behälter voll, tippe unten links auf ihn: Das Sammelshuttle fliegt zu dir, du lädst um, und es bringt die Ladung zur Entladestation oder ins Rohstofflager. Ist dort alles voll, legt es im Zwischenlager ab – von dort geht es von selbst ins Lager, sobald Platz ist. Ladesäulen baust du, indem du eine saubere Parzelle antippst.");
                h.Row("Sammelshuttle", () => { var c = ps.sv.cart; return (c.mode == "go" ? "Fliegt zu dir" : c.mode == "wait" ? "Wartet bei dir – tippe auf deinen Behälter" : c.mode == "back" ? (c.to == "depot" ? "Fliegt ins Zwischenlager (alles voll)" : "Bringt die Ladung weg") : c.mode == "drop" ? "Lädt ab" : "Steht bereit") + " · fasst " + Game.Fmt(Game.CartCap()) + " E (doppelter Behälter) · schneller mit dem Upgrade „Schnelles Sammelshuttle“"; }, () => "Rufen", () => ps.sv.cart.mode == "park", () => { ps.sv.cart.mode = "go"; });
                h.Row("Zwischenlager", () => Game.DepotSum(ps) < .5f ? "Leer – hier landet die Ladung, wenn Entladestation und Rohstofflager voll sind" : Game.Fmt(Game.DepotSum(ps)) + " E warten – gehen von selbst ins Rohstofflager, sobald dort Platz ist (größeres Lager hilft)");
                return;
            }
            h.Tabs("Kaufen", "Flotte " + ps.sv.veh.Count + "/" + Game.VehSlots(ps)); /* Upgrades nur noch im Upgrades-Menü (Fabian 01.11.: keine doppelte Menüführung) */
            int tab = h.Tab;
            if (tab == 0)
            {
                h.Section("Fahrzeuge · " + ps.sv.veh.Count + " von " + Game.VehSlots(ps) + " Plätzen (höchstens " + Game.VehMax(ps) + ")");
                if (Game.VehMax(ps) < Game.VEH_CAP) h.Row("Flottenplatz", () => "Erhöht die Obergrenze auf allen Planeten um 1 (höchstens " + Game.VEH_CAP + ")", () => Cr(Game.FleetPlusCost()), () => Game.S.credits >= Game.FleetPlusCost(), () => Game.BuyFleetPlus(), false);
                foreach (var t in GameData.VehOrder)
                {
                    var T = GameData.Veh[t]; if (!string.IsNullOrEmpty(T.planet) && T.planet != ps.pid && !(T.home ?? new string[0]).Contains(ps.pid) && !Game.P.ContainsKey(T.planet)) continue;
                    string key = t; int have = ps.sv.veh.Count(v => v.type == key);
                    h.Card(T.name, () => { string w = Game.VehWhy(ps, key); return (w != "" ? w + " · " : "") + T.desc + " Tank " + Game.Fmt(T.tank) + " E."; }, AccVeh,
                        () => Cr(Game.VehCost(ps, key)), () => Game.VehWhy(ps, key) == "" && Game.S.credits >= Game.VehCost(ps, key), () => Game.BuyVehicle(key), have > 0 ? have + "× im Einsatz" : null, () => Game.Steps(Game.VehWhy(ps, key), Game.VehCost(ps, key)));
                }
                return;
            }
            if (tab == 1)
            {
                if (ps.sv.veh.Count > 0) h.Row("Alle zurücksetzen", () => "Hängen Fahrzeuge fest? Alle fahren sofort zurück an die Werkstatt und suchen sich neue Arbeit. (Wer 25 s feststeckt, wird ohnehin zurückgesetzt.)", () => "Zurücksetzen", () => true, () => Game.ResetAllVeh(), false);
                if (ps.sv.veh.Count == 0) { h.Note("Noch keine Fahrzeuge. Unter „Kaufen“ findest du alle, die hier fahren können."); return; }
                /* Fabian 01.11.: übersichtliche Liste – Optionen erst nach dem Antippen eines Fahrzeugs */
                h.Section("Deine Fahrzeuge · " + ps.sv.veh.Count);
                foreach (var v in ps.sv.veh)
                {
                    var vv = v;
                    h.Row(Game.VehName(v), () => Game.VehStatus(vv) + " · Stufe " + Game.VehLvl(vv) + " · Tank " + Mathf.RoundToInt(Game.Sum(vv.tank) / Mathf.Max(.01f, GameData.Veh[vv.type].tank * Game.Uv("vtank")) * 100) + " %" + (Game.Gs(ps) >= 3 ? " · Zustand " + Mathf.RoundToInt(Mathf.Max(0, 1 - vv.wear) * 100) + " %" : ""),
                        () => "Optionen", () => ps.sv.veh.Contains(vv), () => h.OpenSheet("vehicle", vv.id.ToString()), false);
                }
                if (Game.Gs(ps) >= 3) h.Row("Automatische Wartung", () => "Ab 75 % Verschleiß fährt jedes Fahrzeug selbst zur Werkstatt; nach zwei Reparaturen wird es automatisch ersetzt.", () => ps.sv.autoMaint ? "An" : "Aus", () => true, () => ps.sv.autoMaint = !ps.sv.autoMaint, false);
                return;
            }
        }

        static void Vehicle(Hud h, VehSave vv)
        {
            var T = GameData.Veh[vv.type];
            h.Note(T.desc);
            h.Row("Zustand", () => Game.VehStatus(vv) + " · Tank " + Game.Fmt(Game.Sum(vv.tank)) + "/" + Game.Fmt(T.tank * Game.Uv("vtank")) + " E · Stufe " + Game.VehLvl(vv) + (Game.Gs(ps) >= 3 ? " · Zustand " + Mathf.RoundToInt(Mathf.Max(0, 1 - vv.wear) * 100) + " %" : ""),
                () => "Zeigen", () => ps.sv.veh.Contains(vv), () => { LunarCleanup.Gameplay.GameRoot.FocusReq = vv.d; LunarCleanup.Gameplay.GameRoot.ZoomReq = 24; h.CloseSheet(); }, false);
            h.Row("Zurücksetzen", () => "Steckt das Fahrzeug fest? Es fährt sofort zurück an die Basis und sucht sich neue Arbeit. Ladung und Zustand bleiben.", () => "Zurücksetzen", () => ps.sv.veh.Contains(vv) && vv.mode != "boom", () => Game.ResetVeh(vv), false);
            h.Row("Arbeitsbereich", () => Game.Zones[vv.zone] + " · mehrere Fahrzeuge auf einer Parzelle reinigen je Partner 10 % stärker", () => "Ändern", () => true, () => vv.zone = (vv.zone + 1) % Game.Zones.Length, false);
            h.Row("Spezialisierung", () => Game.VehLvl(vv) >= 2 ? FeatureData.VehSpecName[vv.spec] : "ab Stufe 2", () => "Wechseln", () => Game.VehLvl(vv) >= 2, () => Game.VehSetSpec(vv, (vv.spec + 1) % FeatureData.VehSpec.Length), false);
            if (Game.Gs(ps) >= 3)
                h.Row("Werkstatt", () => Game.RepsLeft(vv) <= 0 ? "Nach zwei Reparaturen wird es ersetzt" : "Reparieren in der Roboterwerkstatt",
                    () => vv.mode == "toShop" || vv.mode == "repair" ? "In Werkstatt" : Game.RepsLeft(vv) <= 0 ? "Ersetzen " + Cr(Game.ReplaceCost(ps, vv)) : "Warten " + Cr(Game.RepCost(ps, vv)),
                    () => ps.sv.veh.Contains(vv) && vv.mode != "toShop" && vv.mode != "repair" && vv.mode != "boom" && (Game.RepsLeft(vv) <= 0 ? Game.S.credits >= Game.ReplaceCost(ps, vv) : ps.bld.Contains("werkstatt") && vv.wear > .05f && Game.S.credits >= Game.RepCost(ps, vv)),
                    () => { if (Game.RepsLeft(vv) <= 0) Game.ReplaceVeh(ps, vv, false); else if (Game.SendToShop(ps, vv, false)) Game.Toast("Fährt zur Werkstatt", "good"); }, false);
            h.Row("Selbstzerstörung", () => "Macht Platz für ein größeres Fahrzeug – 30 % des Kaufpreises kommen zurück", () => vv.mode == "boom" ? "Läuft …" : "Sprengen +" + Cr(Game.BoomRefund(ps, vv)), () => ps.sv.veh.Contains(vv) && vv.mode != "boom", () => Game.SelfDestruct(vv), false);
            h.BigButton("Zurück zur Flotte", () => true, () => h.OpenSheet("vehicles", "1"));
        }

        static void Production(Hud h)
        {
            h.Tabs("Ampel", "Verkauf", "Plan", "Lieferketten"); /* 04.11.: Lieferketten aus dem Planeten-Fenster hierher */
            int tab = h.Tab;
            if (tab == 0) { ProductionOverview(h); return; }
            if (tab == 1) { Selling(h); return; }
            if (tab == 3) { Supply(h); return; }
            if (!ps.bld.Contains("presse")) h.Note("Baue die Staubpresse (Basisstufe 2), um mit der Veredelung zu beginnen."); Plan(h);
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
            if (Game.Gs(ps) < 2)
            { /* Fabian 03.11.: im Anfangsbereich gut sichtbar, wie man Geld bekommt */
                h.Callout("So bekommst du Geld", "1. Ware freigeben\n2. In die Rakete laden\n3. Rakete starten\nOhne Laden fliegt nichts weg – erst ab Basisstufe 2 bringt ein Band die Ware von selbst.");
                h.Row("Alles in die Rakete laden", () => Game.CargoHint(ps) ? "Lädt alle freigegebenen Waren auf einmal ein" : ps.sv.rk > 0 ? "Die Rakete ist unterwegs" : "Gerade nichts Freigegebenes auf Lager", () => "Alles laden", () => Game.CargoHint(ps), () => Game.LoadAllCargo(), true, null, false, () => ps.sv.rk > 0 ? "Die Rakete ist unterwegs" : "Erst Ware reinigen, ins Lager bringen und freigeben");
            }
            h.Section("Rakete");
            h.Row("Frachtraum", () => ps.sv.rk > 0 ? "Unterwegs – zurück in " + Mathf.CeilToInt(ps.sv.rk) + " s" : Game.Fmt(Game.Sum(ps.sv.sellBuf)) + " / " + Game.Fmt(Game.SellCap(ps)) + " E · Wert " + Cr(Game.CargoValue(ps)) + " · startet von selbst ab " + Game.Fmt(Game.SellTarget(ps)) + " E",
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
                if (Game.Gs(ps) < 2) h.Row("   In die Rakete laden", () => "", () => "Laden", () => Game.SellOn(ps, m) && ps.sv.rk <= 0 && (M.raw ? ps.sv.roh : ps.sv.prod)[m] >= (M.raw ? .5f : 1), () => Game.LoadRocket(m), true);
            }
        }
        // Produktions-Ampel: Engpässe mit Ursache und den passenden Upgrades
        static string Lamp(int lvl) => lvl >= 2 ? "<color=#ff6a5c>●</color> Rot" : lvl == 1 ? "<color=#ffd24d>●</color> Gelb" : lvl == 0 ? "<color=#6ff2a0>●</color> Grün" : "<color=#8a93a6>●</color> Pause";
        /// <summary>Fabian 01.11.: Ampel zeigt den Zustand jeder Station; bei Gelb/Rot steht das Problem dabei und direkt darunter die Upgrades, die es beheben.</summary>
        static bool xportShown;
        /// <summary>Fabian 02.11.: Ausweg aus dem Stau im späten Spiel.</summary>
        static void XportRow(Hud h) => h.Row("Export-Frachter", () => ps.sv.xport ? "Nimmt alles über 90 % Lagerfüllung ab und zahlt " + Game.XportPct + " des Werts · bisher " + Game.Fmt(ps.sv.xEarn) + " Cr" : "Volle Lager laufen nicht mehr über: Überschuss ab 90 % wird für " + Game.XportPct + " des Werts verkauft. Die Basis steht nie mehr still.",
            () => ps.sv.xport ? "Aktiv" : Game.Fmt(Game.XportCost(ps)) + " Cr", () => !ps.sv.xport && Game.XportWhy(ps) == "", () => Game.BuyXport(ps), true, null, true, () => ps.sv.xport ? "" : Game.XportWhy(ps));
        static void Ampel(Hud h)
        {
            xportShown = false;
            var r = Game.FlowReport(ps); int lv = Game.FlowLevel(r); var bad = r.Where(n => n.lvl > 0).OrderByDescending(n => n.lvl).ToList();
            h.Section("Ampel · " + (lv >= 2 ? "Rot" : lv == 1 ? "Gelb" : "Grün"));
            h.Note(lv == 0 ? "Alles im Fluss – der Betriebsbonus wächst bis +30 %." : lv == 1 ? "Ein Engpass bremst die Produktion. Der Betriebsbonus hält." : "Stau – der Betriebsbonus sinkt. Behebe das Problem mit einem der Upgrades darunter.");
            foreach (var n in bad) FlowRows(h, n);
            h.Section("Alle Stationen");
            foreach (var n in r.Where(n => n.lvl <= 0)) h.Row(n.name, () => Lamp(n.lvl) + " · " + n.why);
            if (Game.XportOpen(ps) && (ps.sv.xport || !xportShown)) { h.Section("Überschuss"); XportRow(h); }
        }
        static void FlowRows(Hud h, Game.FlowItem n)
        {
            h.Row(n.name, () => Lamp(n.lvl) + " · " + n.why);
            var fixes = (n.fix ?? new string[0]).Where(id => GameData.Up.ContainsKey(id) && UpVisible(GameData.Up[id]) && Game.Lvl(id) < GameData.Up[id].max).Take(3).ToList();
            if (fixes.Count == 0 && n.fix != null && n.fix.Length > 0) h.Note(n.name == "Bohrtürme" ? "   Die passenden Upgrades sind schon ausgereizt – mehr Fahrzeuge holen mehr ab. Volle Bohrtürme bremsen nur den Abbau, sonst nichts." : ps.sv.xport ? "   Die passenden Upgrades sind schon ausgereizt – der Export-Frachter nimmt den Überschuss ab." : "   Die passenden Upgrades sind schon ausgereizt – baue die Basis weiter aus oder kauf den Export-Frachter.");
            foreach (var f in fixes) UpRow(h, f);
            if (!ps.sv.xport && n.lvl >= 2 && Game.XportOpen(ps) && (n.id == "roh" || Game.IsUnit(n.id)) && n.name != "Bohrtürme" && !xportShown) { xportShown = true; XportRow(h); }
        }
        /// <summary>Ampel eines Gebäudes oben im Gebäudefenster.</summary>
        static void BuildingAmpel(Hud h, string id)
        {
            var r = Game.FlowReport(ps).Where(n => n.id == id || (Game.IsUnit(id) && Game.IsUnit(n.id))).ToList(); if (r.Count == 0) return;
            h.Section("Ampel"); foreach (var n in r) { if (n.lvl > 0) FlowRows(h, n); else h.Row(n.name, () => Lamp(n.lvl) + " · " + n.why); }
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
                () => Game.BlWhy(ps, id) == "" ? "Ausbauen · " + Cr(Game.BlCost(ps, id)) : "Gesperrt", () => Game.BlWhy(ps, id) == "" && Game.S.credits >= Game.BlCost(ps, id), () => Game.BuyBl(id), true, () => "Stufe " + Game.Blvl(ps, id) + " von " + Game.BlMax(ps), true, () => Game.Steps(Game.BlWhy(ps, id), Game.BlCost(ps, id)));
        }
        static string Mats(float[] a) { var l = Enumerable.Range(0, a.Length).Where(i => a[i] >= .5f).Select(i => GameData.Mats[i].name + " " + Game.Fmt(a[i])).ToList(); return l.Count == 0 ? "leer" : string.Join(", ", l); }

        static void Base(Hud h)
        {
            h.Tabs("Bauen", "Ausbau", "Schutz"); /* Fabian 02.11.: Abwehr und Terraforming */
            int tab = h.Tab; var st = GameData.Stages; int s = ps.sv.stage;
            if (tab == 2) { Protect(h); return; }
            if (tab == 0)
            {
                h.Section("Basisstufe " + s + " · " + st[s - 1].name);
                if (s < st.Count) { var nx = st[s]; h.Card("Stufe " + nx.n + ": " + nx.name, () => nx.txt, AccBld, () => Cr(Game.StageCost(ps)), () => Game.S.credits >= Game.StageCost(ps), () => Game.BuyStage(), "Basis ausbauen", () => Game.Steps("", Game.StageCost(ps))); }
                else h.Card("Ausbaustufe " + (ps.sv.exp + 1), () => { string w = Game.ExpWhy(ps); return "+25 % Bohrleistung, +30 % Lager, +1 Fahrzeugplatz, +1 Gebäudestufe. Es gibt kein Ende – jede Stufe kostet mehr." + (w != "" ? " " + w + "." : " Tiefe: " + Game.Fmt(Mathf.Min(ps.sv.deep, Game.ExpNeed(ps))) + " / " + Game.Fmt(Game.ExpNeed(ps)) + " E."); }, AccBld,
                    () => Cr(Game.ExpCost(ps)), () => Game.ExpWhy(ps) == "" && Game.S.credits >= Game.ExpCost(ps), () => Game.BuyExp(), ps.sv.exp > 0 ? "Ausbaustufe " + ps.sv.exp : null, () => Game.Steps(Game.ExpWhy(ps), Game.ExpCost(ps)));
                h.Section("Gebäude");
                int n = 0;
                foreach (var id in GameData.BOrder)
                {
                    var B = GameData.Bld[id]; if (ps.bld.Contains(id) || B.stage > s + 1) continue; string key = id; n++;
                    h.Card(B.name, () => { string w = Game.BuildWhy(ps, key); return (w != "" ? w + " · " : "") + B.desc; }, AccBld, () => Cr(Game.BuildCost(ps, key)), () => Game.BuildWhy(ps, key) == "" && Game.S.credits >= Game.BuildCost(ps, key), () => Game.Build(key), null, () => Game.Steps(Game.BuildWhy(ps, key), Game.BuildCost(ps, key)));
                }
                if (n == 0) h.Note("Alles gebaut, was diese Basisstufe erlaubt.");
                return;
            }
            if (tab == 1)
            {
                WorkOrder(h); /* 04.11.: Ausrichtung ausgeblendet (wirkte kaum spürbar) – gewählte Ausrichtungen wirken weiter */
                BaseMore(h);
                h.Section("Steht schon");
                h.Note(string.Join(" · ", GameData.BOrder.Where(ps.bld.Contains).Select(id => id == "presse" || id == "schmelze" || id == "raff" ? GameData.MachName(ps.pid, id) : GameData.Bld[id].name)));
                return;
            }
        }
        static string Pc(float f) => Mathf.RoundToInt(f * 100) + " %";
        /// <summary>Fabian 02.11.: Meteorschild, Abwehrraketen, Terraforming und Lieferungen dieses Planeten.</summary>
        static void Protect(Hud h)
        {
            var sv = ps.sv; string pn = ps.P.name;
            h.Section("Riesenmeteor");
            h.Note("Ab und zu trifft ein Riesenmeteor einen deiner Planeten. Ohne Abwehr wird die Basis schwer verwüstet (Krater, kaputte Fahrzeuge, gedrosselte Produktion) – gebaut bleibt alles. Keine Abwehr hält immer.", () => Game.DoomActive ? "RIESENMETEOR im Anflug auf " + GameData.Planets[Game.S.doomPid].name + " – Einschlag in " + Mathf.CeilToInt(Game.S.doomT) + " s" : "Ab und zu trifft ein Riesenmeteor einen deiner Planeten. Ohne Abwehr wird die Basis schwer verwüstet (Krater, kaputte Fahrzeuge, gedrosselte Produktion) – gebaut bleibt alles. Keine Abwehr hält immer. Bisher: " + Game.S.doomN + " Riesenmeteore, " + Game.S.doomHits + " Brocken abgefangen.");
            h.Row("Meteorschild", () => "Stufe " + sv.msLv + "/" + Game.MS_MAX + " · " + Game.ShieldState(ps) + " · lenkt den Meteor mit bis zu " + Pc(Game.ShieldMax(ps)) + " ab. Schwächer bei Sturm, wenig Energie und fehlenden Lieferungen, aus bei leeren Batterien. Nach jedem abgelenkten Meteor 30 min Nachladen. Kostet " + Dec(.5f * sv.msLv, 1) + " Energie/s" + (sv.msLv < Game.MS_MAX ? " · nächste Stufe (" + Pc(.85f * (sv.msLv + 1) / Game.MS_MAX) + "): " + (Game.MsGoods(ps).Length > 0 ? Game.GoodsTxt(Game.MsGoods(ps)) : "nur Credits") : ""),
                () => sv.msLv >= Game.MS_MAX ? "Max" : Cr(Game.MsCost(ps)), () => Game.MsWhy(ps) == "", () => Game.BuyShield(), why: () => Game.Steps(Game.MsWhy(ps).StartsWith("Dir fehlen") ? "" : Game.MsWhy(ps), Game.MsCost(ps)));
            h.Row("Abwehrraketen", () => "Stufe " + sv.flakLv + "/" + Game.FLAK_MAX + " · " + Game.FlakRockets(ps) + " Raketen je Riesenmeteor. Hält der Schild nicht, zerbricht der Meteor in 4–8 Brocken: Tippe auf jeden Brocken, wenn sein Ring ihn genau umschließt. Daneben kostet eine Rakete. Nur wenn du vor Ort bist – sonst zählt nur der Schild." + (sv.flakLv < Game.FLAK_MAX && Game.FlakGoods(ps).Length > 0 ? " · nächste Stufe: " + Game.GoodsTxt(Game.FlakGoods(ps)) : ""),
                () => sv.flakLv >= Game.FLAK_MAX ? "Max" : Cr(Game.FlakCost(ps)), () => Game.FlakWhy(ps) == "", () => Game.BuyFlak(), why: () => Game.Steps(Game.FlakWhy(ps).StartsWith("Dir fehlen") ? "" : Game.FlakWhy(ps), Game.FlakCost(ps)));
            h.Note("Jeder Riesenmeteor wird schwerer: mehr, schnellere Brocken, die später auch in Kurven fliegen. Terraforming macht ihn ebenfalls schwerer. Kommen mehr als zwei Drittel durch, ist die Basis verloren – sonst gibt es Schmutz, kaputte Fahrzeuge und gestörte Anlagen.");
            h.Section("Terraforming");
            if (!Game.IsDone(ps.pid)) h.Note("Sobald " + pn + " zu 100 % sauber ist, kannst du den Planeten in 5 Stufen begrünen – bis zum Wald. Jede Stufe gibt einen dauerhaften Bonus.");
            else
            {
                h.Row("Stufe " + sv.terra + "/" + Game.TERRA_MAX + " · " + Game.TerraName[sv.terra], () => sv.terra == 0 ? "Noch kahl." : "Zustand " + Pc(Mathf.Clamp01(sv.terraHp)) + " · " + (Game.TerraTrouble(ps) is var tr && tr != "" ? "sinkt: " + tr : "stabil") + " · Unterhalt " + Dec(Game.UpkeepH(ps), 1) + " je Gut pro Stunde. Bei 0 % fällt eine Stufe weg.");
                for (int k = 1; k <= Game.TERRA_MAX; k++) { int kk = k; h.Row(k + ". " + Game.TerraName[k], () => (sv.terra >= kk ? "Erreicht · " : "") + Game.TerraBonus[kk]); }
                if (sv.terra < Game.TERRA_MAX) h.Row("Nächste Stufe: " + Game.TerraName[sv.terra + 1], () => { string w = Game.TerraWhy(ps); return Game.GoodsTxt(Game.TerraGoods(ps)) + (w != "" ? " · " + w : ""); }, () => Cr(Game.TerraCost(ps)), () => Game.TerraWhy(ps) == "", () => Game.BuyTerra(), why: () => Game.Steps(Game.TerraWhy(ps).StartsWith("Dir fehlen") ? "" : Game.TerraWhy(ps), Game.TerraCost(ps)));
                h.Note("Ohne Lieferungen, bei Stürmen und wenn Schmutz liegen bleibt, verfällt das Terraforming. Ein Riesenmeteor wirft das Terraforming ein bis zwei Stufen zurück.");
            }
            /* Fabian 02.11.: gerettete Welt ruhen lassen */
            h.Section("Gerettete Welt");
            h.Row(sv.rest ? pn + " ruht" : "Planet ruhen lassen", () => sv.rest ? "Liefert dauerhaft " + Game.Fmt(sv.restRate * 3600) + " Cr/h (bisher " + Game.Fmt(sv.restEarn) + " Cr), braucht keine Pflege, kein Meteor trifft ihn. Alle Preise +" + Mathf.RoundToInt(Game.REST_PRICE * 100 * Game.RestCount) + " %. Wenn du hier bist, läuft er ganz normal."
                    : "Ein geretteter Planet (Stufe " + Game.TERRA_MAX + ", Wald) kann ruhen: Er liefert dauerhaft sein jetziges Einkommen (≈ " + Game.Fmt(sv.rateEma * 3600) + " Cr/h), jeder ruhende Planet hebt alle Preise um " + Mathf.RoundToInt(Game.REST_PRICE * 100) + " %, und du musst dich nicht mehr kümmern.",
                () => sv.rest ? "Aufwecken" : "Ruhen lassen", () => sv.rest || Game.RestWhy(ps) == "", () => Game.SetRest(ps, !sv.rest), !sv.rest, null, true, () => Game.RestWhy(ps));
            h.Section("Lieferungen für " + pn);
            foreach (var q in Game.Needs(ps.pid)) { var g = Game.GoodOf(q); string qq = q; h.Row(g.name, () => "von " + GameData.Planets[qq].name + " · im Lager: " + Mathf.FloorToInt(Game.GoodAmt(g.id)) + (Game.P.ContainsKey(qq) && Game.IsDone(qq) ? " · wird hergestellt" : " · erst wenn " + GameData.Planets[qq].name + " 100 % sauber ist")); }
            h.Note("Raketen bringen die Güter automatisch. Übersicht aller Planeten: Planeten → Lieferketten.");
        }
        /// <summary>Fabian 02.11.: wer stellt was her, wer braucht was.</summary>
        static void Supply(Hud h)
        {
            h.Note("Jeder komplett saubere Planet stellt ein Spezialgut her (40 pro Stunde, auch offline). Raketen bringen es automatisch dorthin, wo es gebraucht wird: Top-Stufen der Upgrades, Meteorschild, Abwehrraketen und Terraforming.");
            h.Section("Galaxie-Lager");
            foreach (var pid in GameData.POrder) { var g = Game.GoodOf(pid); if (!Game.P.ContainsKey(pid) && Game.GoodAmt(g.id) <= 0) continue; string pp = pid;
                h.Row(g.name, () => Mathf.FloorToInt(Game.GoodAmt(g.id)) + " / " + Game.GOOD_CAP + " · von " + GameData.Planets[pp].name + (Game.P.ContainsKey(pp) && Game.GoodRate(Game.P[pp]) > 0 ? " · +" + Game.GoodRate(Game.P[pp]) + " pro Stunde" : " · noch keine Herstellung")); }
            h.Section("Planeten");
            foreach (var pid in GameData.POrder) { if (!Game.P.ContainsKey(pid)) continue; var q = Game.P[pid];
                h.Row(GameData.Planets[pid].name, () => "liefert " + Game.GoodOf(pid).name + (Game.GoodRate(q) > 0 ? "" : " (erst bei 100 % sauber)") + " · braucht " + string.Join(" und ", Game.Needs(pid).Select(x => Game.GoodOf(x).name)) + (Game.UpkeepH(q) > 0 ? (q.sv.supOk ? " · versorgt" : " · UNVERSORGT") : "") + (q.sv.terra > 0 ? " · Terraforming " + q.sv.terra : "") + (q.sv.msLv > 0 ? " · Schild " + q.sv.msLv : "")); }
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
                    () => ps.sv.rigs.Count >= Game.RigMax(ps) ? "Voll" : "Bauen " + Cr(Game.RigCost(ps)), () => ps.sv.rigs.Count < Game.RigMax(ps) && Game.S.credits >= Game.RigCost(ps), () => Game.BuildRig(-1), why: () => Game.Steps("", Game.RigCost(ps)));
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
            var maxed = new List<string>();
            foreach (var cat in groups[tab])
            {
                var list = GameData.Upg.Where(u => u.cat == cat && UpVisible(u)).ToList(); if (list.Count == 0) continue;
                foreach (var u in list) if (IsMax(u.id)) maxed.Add(u.id); list.RemoveAll(u => IsMax(u.id)); if (list.Count == 0) continue;
                h.Section(cat); foreach (var u in list) UpRow(h, u.id);
            }
            MaxedRows(h, maxed);
        }

        /* Fabian 01.11.: Planetenwechsel und Sternenstaub in Reiter und Gruppen gliedern */
        static readonly (string name, string[] ids)[] Regions =
        {
            ("Innere Planeten", new[] { "moon", "mars", "venus" }),
            ("Monde der Gasriesen", new[] { "europa", "io", "ganymed", "titan", "enceladus", "triton" }),
            ("Zwergplaneten", new[] { "pluto", "eris", "sedna" }),
            ("Exoplaneten", new[] { "proxima", "trappist", "kepler", "cancri", "gliese", "poltergeist" }),
        };
        static readonly (string name, string[] ids)[] SdGroups =
        {
            ("Schneller Start", new[] { "cash1", "cash2", "cash3", "fast", "buggy" }),
            ("Einnahmen", new[] { "price", "hand", "offline", "crystal" }),
            ("Funde und Optik", new[] { "find", "event", "glow" }),
        };
        static void Planets(Hud h)
        {
            bool sd = Game.S.done.Count > 0 || Game.S.stardust > 0;
            int owned = GameData.POrder.Count(x => Game.P.ContainsKey(x));
            if (sd) h.Tabs("Reisen " + owned, "Neue Planeten", "Sternenstaub " + Game.StardustFree()); else h.Tabs("Reisen " + owned, "Neue Planeten");
            int tab = h.Tab;
            if (sd && tab == 2) { Stardust(h); return; }
            bool travel = tab == 0; int shown = 0;
            foreach (var (rname, ids) in Regions)
            {
                var list = ids.Where(x => GameData.Planets.ContainsKey(x) && Game.P.ContainsKey(x) == travel).OrderBy(x => System.Array.IndexOf(GameData.POrder, x)).ToList(); if (list.Count == 0) continue;
                h.Section(rname); foreach (var pid in list) { PlanetRow(h, pid); shown++; }
            }
            foreach (var pid in GameData.POrder) if (!Regions.Any(r => r.ids.Contains(pid)) && Game.P.ContainsKey(pid) == travel) { PlanetRow(h, pid); shown++; } /* Sicherheitsnetz für neue Planeten */
            if (shown == 0) h.Note(travel ? "Noch keine Planeten." : "Alle Planeten sind freigeschaltet.");
            if (travel) h.Note("Jeder Planet hat seine eigene Kasse: Was er verdient, bleibt bei ihm. Von außen darf nur ein Startbudget kommen (2× Freischaltpreis). Ohne dich lassen Maschinen und Fahrzeuge nach 30 min langsam nach – kurz vorbeischauen und antippen bringt sie zurück.");
            if (Game.S.debt > 0) h.Note("Reisekredit offen: " + Cr(Game.S.debt) + " – wird mit der Hälfte jeder Einnahme abbezahlt.");
            if (!travel && Game.S.debt > 0 && Game.StardustPending() > 0) h.Note("Tipp: Ein Sternenstaub-Neustart (Reiter Sternenstaub) bringt jetzt +" + Game.StardustPending() + " Sternenstaub für dauerhafte Boni.");
            else if (!travel) h.Note("Freischalten kannst du immer den nächsten Planeten der Reihe.");
        }
        static void PlanetRow(Hud h, string pid)
        {
            var P = GameData.Planets[pid]; bool have = Game.P.ContainsKey(pid), cur = Game.S.planet == pid; string key = pid;
            string dirt = string.Join(", ", P.types.Select(t => GameData.Dirt[t.Key].name));
            if (GameData.PLiq.TryGetValue(pid, out var lq)) { var LD = GameData.Liq.Find(x => x.key == lq); if (LD != null) dirt += " · " + LD.name + "-Flächen: nur Schwebefahrzeuge oder Hand"; }
            int nP = (have ? Game.P[pid].pd.N : GameData.Cells[pid].N); string size = Game.Num(nP) + " Parzellen";
            if (cur) h.Row(P.name, () => "Du bist hier · Kasse " + Cr(Game.S.credits) + " · " + Mathf.FloorToInt(Game.CleanPct(Game.P[key]) * 100) + " % sauber · " + size + " · " + P.sub);
            else if (have) h.Row(P.name, () => Game.Ruined(key) ? "Zerstört vom Riesenmeteor – wird neu aufgebaut, noch " + Mathf.CeilToInt(Game.RuinLeft(key) / 60f) + " min" : Game.P[key].sv.rest ? "Gerettet · ruht · " + Game.Fmt(Game.P[key].sv.restRate * 3600) + " Cr/h" : "Kasse " + Cr(Game.P[key].sv.cash) + " · " + Mathf.FloorToInt(Game.CleanPct(Game.P[key]) * 100) + " % sauber · " + size + " · " + P.sub, () => Game.Ruined(key) ? "Gesperrt" : "Reisen", () => !Game.Ruined(key), () => { Game.Travel(key); h.CloseSheet(); }, false);
            if (have && !cur && Game.ImportLeft(Game.P[key]) >= 1) h.Row("   Zuschuss schicken", () => "Noch bis " + Cr(Game.ImportLeft(Game.P[key])) + " von außen erlaubt – danach muss " + P.name + " selbst verdienen", () => "Schicken", () => Game.S.credits >= 1, () => Game.SendCash(key), false); /* 03.11.: Planetenkasse */
            if (!have)
            {
                int i = System.Array.IndexOf(GameData.POrder, pid); string prev = i > 0 ? GameData.POrder[i - 1] : null;
                if (prev != null && Game.P.ContainsKey(prev)) h.Row(P.name, () => { string w = Game.UnlockWhy(key); return (w != "" ? w + " · " : "") + size + " · " + P.sub + " · " + dirt + (P.hzName != "" ? " · Gefahr: " + P.hzName : "") + " · Startbudget: bis " + Cr(Game.ImportCap(key)) + " aus dieser Kasse, der Rest bleibt hier"; }, () => Cr(Game.UnlockCost(key)), () => Game.UnlockWhy(key) == "" && Game.S.credits >= Game.UnlockCost(key), () => { if (Game.Unlock(key)) h.CloseSheet(); }, why: () => Game.Steps(Game.UnlockWhy(key), Game.UnlockCost(key)) + (Game.UnlockWhy(key) == "" && Game.CanCredit(key) ? "\n\nOder: „Auf Kredit reisen“ direkt darunter." : ""));
                if (Game.CanCredit(key)) h.Row("   Auf Kredit reisen", () => "Dir fehlen " + Cr(Game.UnlockCost(key) - Game.S.credits) + ". Der Rest wird geliehen (+10 %) und mit der Hälfte deiner Einnahmen abbezahlt.", () => "Kredit", () => Game.CanCredit(key), () => { if (Game.UnlockOnCredit(key)) h.CloseSheet(); }, false);
                else h.Row(P.name, () => size + " · " + P.sub + " · noch nicht erreichbar");
            }
        }

        static float cartBoomT = -99;
        static float prestigeT = -99, importT = -99, demolishT = -99;
        static void Stardust(Hud h)
        {
            h.Section("Neustart · " + Game.StardustFree() + " Sternenstaub frei");
            h.Note("Jeder komplett saubere Planet bringt Sternenstaub (später in der Reihe = mehr), dazu 1 je 10 Funde. Mit einem Neustart beginnst du wieder auf dem Mond – Sternenstaub, Boni, Funde und Tagesbonus bleiben.");
            h.Row("Neustart", () => Time.unscaledTime - prestigeT < 4 ? "Wirklich? Noch einmal tippen setzt Planeten, Credits, Upgrades und Forschung zurück." : "Bringt jetzt +" + Game.StardustPending() + " Sternenstaub" + (Game.S.resets > 0 ? " · bisher " + Game.S.resets + " Neustarts" : ""),
                () => Time.unscaledTime - prestigeT < 4 ? "Ja, neu starten" : "Neu starten", () => Game.StardustPending() > 0, () => { if (Time.unscaledTime - prestigeT < 4) { Game.Prestige(); h.CloseSheet(); } else prestigeT = Time.unscaledTime; }, false);
            var done = new HashSet<string>();
            foreach (var (gname, ids) in SdGroups)
            {
                var list = FeatureData.Stardust.Where(n => ids.Contains(n.id)).ToList(); if (list.Count == 0) continue;
                h.Section(gname + " · " + list.Count(n => Game.S.sdNodes.Contains(n.id)) + "/" + list.Count); foreach (var n in list) { SdRow(h, n); done.Add(n.id); }
            }
            var rest = FeatureData.Stardust.Where(n => !done.Contains(n.id)).ToList();
            if (rest.Count > 0) { h.Section("Weitere"); foreach (var n in rest) SdRow(h, n); }
        }
        static void SdRow(Hud h, SdNode n) { var nn = n; h.Row(n.name, () => nn.desc + (Game.SdWhy(nn) != "" && Game.SdWhy(nn) != "Gekauft" ? " · " + Game.SdWhy(nn) : ""), () => Game.S.sdNodes.Contains(nn.id) ? "Gekauft" : nn.cost + " Staub", () => Game.SdWhy(nn) == "", () => Game.SdBuy(nn)); }

        static string Dur(double sec) { if (sec <= 0) return "–"; long t = (long)sec; long d = t / 86400, hh = t % 86400 / 3600, mm = t % 3600 / 60; return d > 0 ? d + " T " + hh + " h" : hh > 0 ? hh + " h " + mm + " min" : Mathf.Max(1, mm) + " min"; }
        const string Raw = "\u2060"; /* Texte, die schon in der richtigen Sprache sind */
        static void FlagSheet(Hud h, int i)
        {
            if (i < 0 || i >= ps.sv.flags.Count) return; var f = ps.sv.flags[i];
            if (!f.got) { h.Note("Die Schatztruhe an dieser Flagge ist noch verschlossen. Reinige die Parzelle und tippe die Truhe an."); return; }
            /* Fabian 01.11.: jede Flagge erzählt ein Stück Geschichte, darunter eine Randnotiz – und dann kommt dein Kommentar */
            var e = Game.FlagStory(ps, f); Game.CheckPutzi(ps, e);
            h.Section(Raw + FlagStories.ArcTitle(ps.pid) + " · " + e.title);
            h.Note(Raw + "<b>" + e.who + "</b>"); h.Note(Raw + e.body); h.Note(Raw + "<i><color=#9fb0c8>" + e.note + "</color></i>");
            if (e.finale) h.Note("<color=#ffd24d>Das war das letzte Kapitel dieser Geschichte.</color>");
            h.Section("Dein Kommentar");
            h.Note("Schreib, was du dazu denkst – es steht danach auf der Fahne (höchstens 24 Zeichen).");
            h.TextInput(f.text, "Dein Kommentar", t => Game.SetFlagText(f, t));
            h.Row("Fahnenfarbe", () => "Jetzt: " + Game.FlagCols[Mathf.Clamp(f.fcol, 0, Game.FlagCols.Length - 1)].name, () => "Wechseln", null, () => Game.NextFlagCol(f), false);
            h.Row("Schriftfarbe", () => "Jetzt: " + Game.TextCols[Mathf.Clamp(f.tcol, 0, Game.TextCols.Length - 1)].name, () => "Wechseln", null, () => Game.NextTextCol(f), false);
            h.Section("Ideen");
            foreach (var idea in new[] { "Gänsehaut!", "Ich les weiter!", "Unglaublich …", "Hier war ich!", "Wie geht's weiter?", "Gruß an alle" }) { var t = idea; h.Row(t, null, () => f.text == t ? "Steht drauf" : "Nehmen", () => f.text != t, () => { Game.SetFlagText(f, t); Game.Toast("Flagge beschriftet: „" + t + "“", "good"); }, false); }
            h.Row("Geschichte", () => Game.FlagsGot(ps) + " von " + ps.sv.flags.Count + " Truhen auf " + ps.P.name + " geöffnet", () => "Alles lesen", null, () => h.OpenSheet("story", ps.pid), false);
        }
        /// <summary>Alle gefundenen Teile einer Planeten-Geschichte in der richtigen Reihenfolge.</summary>
        static void StorySheet(Hud h, string pid)
        {
            if (!Game.P.TryGetValue(pid, out var q) || q.sv.flags == null) return; Game.EnsureSlots(q);
            var got = q.sv.flags.Where(f => f.got).OrderBy(f => f.slot).ToList();
            h.Note(Game.FlagsGot(q) + " von " + q.sv.flags.Count + " Truhen geöffnet. Das letzte Kapitel liegt immer in der letzten Truhe.");
            if (got.Count == 0) { h.Note("Noch keine Truhe geöffnet."); return; }
            foreach (var f in got)
            {
                var e = Game.FlagStory(q, f); h.Section(Raw + e.title);
                h.Note(Raw + "<b>" + e.who + "</b> – " + e.body); h.Note(Raw + "<i><color=#9fb0c8>" + e.note + "</color></i>");
                if (!string.IsNullOrEmpty(f.text)) h.Note(Raw + "<color=#5cf2b0>" + (Loc.En ? "You: " : "Du: ") + "„" + f.text + "“</color>");
            }
            if (got.Count < q.sv.flags.Count) h.Note("<i>Fortsetzung folgt – in der nächsten Truhe.</i>");
        }
        static void Codex(Hud h)
        {
            h.Section("Geschichten");
            foreach (var pid in GameData.POrder) { if (!Game.P.ContainsKey(pid)) continue; var q = Game.P[pid]; if (q.sv.flags == null || q.sv.flags.Count == 0) continue; string key = pid;
                h.Row(GameData.Planets[pid].name, () => Raw + "„" + FlagStories.ArcTitle(key) + "“ · " + Game.FlagsGot(Game.P[key]) + "/" + Game.P[key].sv.flags.Count + (Loc.En ? " chests opened" : " Truhen geöffnet"), () => "Lesen", () => Game.FlagsGot(Game.P[key]) > 0, () => h.OpenSheet("story", key), false); }
            h.Section("Flaggen");
            foreach (var pid in GameData.POrder) { if (!Game.P.ContainsKey(pid)) continue; var q = Game.P[pid]; if (q.sv.flags == null) continue; h.Row(q.P.name, () => Game.FlagsGot(q) + " / " + q.sv.flags.Count + " gefunden" + (q.sv.flags.Any(f => f.got && f.text != "") ? " · " + string.Join(", ", q.sv.flags.Where(f => f.got && f.text != "").Select(f => "„" + f.text + "“").Take(4)) : "")); }
            h.Section("Entdeckungen · " + GameData.POrder.Count(pid => LunarCleanup.Gameplay.WorldView.WonderKnown(pid)) + "/" + GameData.POrder.Count(pid => LunarCleanup.Gameplay.WorldView.TryWonder(pid, out _, out _)));
            foreach (var pid in GameData.POrder)
            {
                if (!LunarCleanup.Gameplay.WorldView.TryWonder(pid, out var wn, out var wt)) continue; bool known = LunarCleanup.Gameplay.WorldView.WonderKnown(pid);
                h.Row(GameData.Planets[pid].name + " · " + (known ? wn : "???"), () => known ? wt : Game.P.ContainsKey(pid) ? "Noch nicht entdeckt – schau dich auf dem Planeten um." : "Planet noch nicht erreicht.");
            }
            h.Section("Funde");
            foreach (var kv in FeatureData.Finds)
            {
                if (!Game.P.ContainsKey(kv.Key)) continue; string pid = kv.Key; var al = kv.Value;
                h.Row(GameData.Planets[pid].name + " · " + Game.FindsOn(pid) + "/" + al.Length, () => string.Join(" · ", al.Select((f, i) => Game.HasFind(pid, i) ? f.name : "???")) + (Game.FindsOn(pid) >= al.Length ? " · Album komplett: +5 % Erlös" : " · Funde gibt es beim Reinigen (3 %, auf seltsamen Parzellen 8 %)"));
            }
            if (Game.S.daily != null && Game.S.daily.streak > 0) h.Note("Tagesbonus: Tag " + Game.S.daily.streak + " von 7 – komm morgen wieder, die Belohnung steigt.");
        }
        static string Medal(int t) => t >= 3 ? "<color=#ffd24d>●●●</color>" : t == 2 ? "<color=#ffd24d>●●</color><color=#4a5266>●</color>" : t == 1 ? "<color=#ffd24d>●</color><color=#4a5266>●●</color>" : "<color=#4a5266>●●●</color>";
        /// <summary>Fabian 01.11.: Erfolge mit Bronze/Silber/Gold, Belohnung zum Abholen, nach Kategorien.</summary>
        /// <summary>Fabian 02.11.: Bestellungen von Besuchern – wer was möchte, was schon zurückgelegt ist, wie lange noch.</summary>
        static void OrdersSheet(Hud h)
        {
            if (!Game.OrdersOpen) { h.Note("Sobald ein Planet Basisstufe 2 hat, kommen Besucher und bestellen Waren."); return; }
            h.Note("Besucher landen auf deinen Planeten und bestellen Waren – auch von anderen Planeten. Sobald die Ware in einem Lager liegt, wird sie automatisch im Bestelllager zurückgelegt (die Rakete verkauft sie dann nicht). Ist alles da, holt der Besucher sie ab und zahlt deutlich mehr als die Rakete. Läuft auch offline. Ist die Zeit um, reist er ab – Zurückgelegtes wird normal verkauft.");
            h.Section("Bestelllager");
            h.Row("Stufe " + Game.S.voLv + "/" + Game.ORDER_MAXLV, () => Game.Orders.Count + " von " + Game.OrderMax() + " Bestellungen gleichzeitig" + (Game.S.voLv < Game.ORDER_MAXLV ? " · nächste Stufe: +1 Platz" : ""),
                () => Game.S.voLv >= Game.ORDER_MAXLV ? "Max" : Cr(Game.OrderLvCost()), () => Game.OrderLvWhy() == "", () => Game.BuyOrderLv(), why: () => Game.Steps("", Game.OrderLvCost()));
            h.Section("Offene Bestellungen");
            if (Game.Orders.Count == 0) { h.Note("Gerade keine Bestellungen. Neue Besucher kommen alle 10–25 Minuten."); return; }
            foreach (var o in Game.Orders.ToList())
            {
                var oo = o;
                h.Row(o.who + " · " + GameData.Planets[o.pid].name, () =>
                {
                    var parts = oo.items.Select(it => Game.ItemName(it) + " " + Game.Fmt(Mathf.Min(it.got, it.n)) + "/" + Game.Fmt(it.n) + (it.src != oo.pid || it.g != "" ? " (von " + Game.ItemFrom(it) + ")" : ""));
                    return string.Join(" · ", parts) + "\nNoch " + Game.TimeLeft(oo.until) + " · zahlt " + Cr(oo.reward) + " + " + oo.sd + " Sternenstaub · " + Mathf.RoundToInt(Game.OrderProgress(oo) * 100) + " % zurückgelegt";
                }, () => "Absagen", () => true, () => Game.CancelOrder(oo.id), false);
            }
            h.Note("Tipp: Fehlt eine Ware, schau, ob der Planet sie herstellt (Produktion → Plan) und ob genug ins Lager kommt. Spezialgüter kommen erst, wenn ihr Planet zu 100 % sauber ist.");
        }
        static void Achievements(Hud h)
        {
            int got = Game.Achievements.Sum(a => Game.AchTier(a)), all = Game.Achievements.Count * 3;
            h.Note(got + " von " + all + " Stufen erreicht" + (Game.S.title != "" ? " · Dein Titel: " + Game.S.title : "") + ". Bronze bringt Credits, Silber zusätzlich Sternenstaub, Gold Sternenstaub und manchmal einen Titel.");
            var pend = new List<(Game.AchDef a, int t)>(); foreach (var a in Game.Achievements) for (int t = 0; t < Game.AchTier(a); t++) if (!Game.AchClaimed(a, t)) pend.Add((a, t));
            if (pend.Count > 0)
            {
                h.Section("Jetzt abholen");
                foreach (var (a, t) in pend) { var aa = a; int tt = t; h.Row(Game.TierName[t] + ": " + a.name, () => Game.Fmt(aa.tiers[tt]) + (aa.unit != "" ? " " + aa.unit : "") + " · " + Game.AchRewardText(aa, tt), () => Game.AchClaimed(aa, tt) ? "Abgeholt" : "Abholen", () => !Game.AchClaimed(aa, tt), () => Game.ClaimAch(aa, tt)); }
            }
            foreach (var cat in Game.AchCats)
            {
                var list = Game.Achievements.Where(a => a.cat == cat).ToList(); if (list.Count == 0) continue;
                h.Section(cat + " · " + list.Sum(a => Game.AchTier(a)) + "/" + list.Count * 3);
                foreach (var a in list)
                {
                    var aa = a;
                    h.Row(a.name, () => { int t = Game.AchTier(aa); double v; try { v = aa.val(); } catch { v = 0; } string u = aa.unit != "" ? " " + aa.unit : "";
                        return Medal(t) + "  " + (t >= 3 ? "Gold geschafft · " + Game.Fmt(v) + u : Game.Fmt(System.Math.Min(v, aa.tiers[t])) + " / " + Game.Fmt(aa.tiers[t]) + u + " bis " + Game.TierName[t] + " · Belohnung " + Game.AchRewardText(aa, t)); });
                }
            }
        }
        static void Stats(Hud h)
        {
            /* Fabian 02.11.: alle Meldungen zum Nachlesen – auf dem Bildschirm erscheint nur noch das Wichtige */
            h.Section("Protokoll · letzte Meldungen");
            if (Game.Log.Count == 0) h.Note("Noch keine Meldungen.");
            foreach (var e in Enumerable.Reverse(Game.Log).Take(12)) { var ee = e; long ago = System.Math.Max(0, Game.Now() - ee.at); h.Note((ee.k == "bad" ? "<color=#ff8a7a>●</color> " : ee.k == "good" ? "<color=#6ff2a0>●</color> " : "<color=#8a93a6>●</color> ") + (ago < 60 ? "gerade eben" : ago < 3600 ? "vor " + (ago / 60) + " min" : "vor " + (ago / 3600) + " h") + " · " + ee.t); }
            h.Section("Statistik");
            h.Row("Einnahmen", () => Cr(Game.IncomePerMin()) + " pro Minute (letzte 5 min) · insgesamt " + Cr(Game.S.stats.earned));
            h.Row(ps.P.name, () => { float cpm = Game.CellsPerMin(); int tot = ps.pd.cells.Count(x => !x.IsBase); int left80 = Mathf.Max(0, Mathf.CeilToInt(tot * .8f) - ps.nClean); return Mathf.RoundToInt(Game.CleanPct(ps) * 100) + " % sauber · " + Dec(cpm, 1) + " Parzellen pro Minute" + (left80 > 0 && cpm > .05f ? " · 80 % in etwa " + Mathf.CeilToInt(left80 / cpm) + " min" : ""); });
            h.Row("Gesamt", () => Game.S.stats.cells + " Parzellen gereinigt · " + Game.S.planets.Sum(p => p.nRockets) + " Raketen · " + Game.S.stats.crystals + " Kristalle · " + Cr(Game.S.stats.sold) + " verkauft");
            h.Section("Verlauf");
            h.BarChart("Einnahmen je Stunde", Game.IncomeHours(24), Pal.Credit, "vor 24 h", "jetzt", v => Game.Fmt(v) + " Cr");
            foreach (var pid in GameData.POrder) { if (!Game.P.ContainsKey(pid)) continue; var cd = Game.CellDays(Game.P[pid], 7); if (cd.Sum() <= 0) continue; h.BarChart(GameData.Planets[pid].name + " · Parzellen je Tag", cd, Pal.Clean, "vor 7 Tagen", "heute", v => Game.Fmt(v)); }
            h.Section("Planeten");
            foreach (var pid in GameData.POrder)
            {
                if (!Game.P.ContainsKey(pid)) continue; var q = Game.P[pid]; string key = pid;
                h.Row(q.P.name, () => { var sv = Game.P[key].sv; int pct = Mathf.FloorToInt(Game.CleanPct(Game.P[key]) * 100); string t = pct + " % sauber · Spielzeit " + Dur(sv.tPlay);
                    if (sv.tDone > 0) t += " · komplett sauber nach " + Dur(sv.tDone) + " Spielzeit" + (sv.tDoneAt > 0 && sv.tFirst > 0 ? " (" + Dur(sv.tDoneAt - sv.tFirst) + " seit dem ersten Besuch)" : "");
                    else if (Game.IsDone(key)) t += " · komplett sauber"; return t; });
            }
        }
        /// <summary>Fabian 01.11.: Was ist auf welchem Planeten schon fertig – und was fehlt noch?</summary>
        static void Completion(Hud h)
        {
            const string ok = "<color=#5cf2b0>●</color>", no = "<color=#ff9a6a>●</color>";
            var parts = new Dictionary<string, List<(string what, bool done)>>(); int full = 0;
            foreach (var pid in GameData.POrder)
            {
                if (!Game.P.ContainsKey(pid)) continue; var q = Game.P[pid]; var l = new List<(string, bool)>();
                int pct = Mathf.FloorToInt(Game.CleanPct(q) * 100); l.Add(("Sauber " + pct + " %", Game.IsDone(pid)));
                if (q.sv.flags != null && q.sv.flags.Count > 0) l.Add(("Flaggen " + Game.FlagsGot(q) + "/" + q.sv.flags.Count, Game.FlagsGot(q) >= q.sv.flags.Count));
                if (FeatureData.Finds.TryGetValue(pid, out var al)) l.Add(("Funde " + Game.FindsOn(pid) + "/" + al.Length, Game.FindsOn(pid) >= al.Length));
                if (LunarCleanup.Gameplay.WorldView.TryWonder(pid, out _, out _)) l.Add(("Entdeckung", LunarCleanup.Gameplay.WorldView.WonderKnown(pid)));
                int pg = Enumerable.Range(0, Game.PlanetGoals.Length).Count(k => Game.PGoalClaimed(q, k)); l.Add(("Planetenziele " + pg + "/" + Game.PlanetGoals.Length, pg >= Game.PlanetGoals.Length));
                parts[pid] = l; if (l.All(x => x.Item2)) full++;
            }
            int reached = parts.Count, total = GameData.POrder.Length;
            h.Note(full + " von " + total + " Planeten komplett abgeschlossen · " + reached + " erreicht. Grün ist fertig, Orange fehlt noch.");
            foreach (var pid in GameData.POrder)
            {
                string nm = GameData.Planets[pid].name;
                if (!parts.TryGetValue(pid, out var l)) { h.Row(nm + " · " + no, () => "Noch nicht erreicht – reise im Planetenmenü dorthin."); continue; }
                bool all = l.All(x => x.done); var miss = l.Where(x => !x.done).Select(x => x.what).ToList();
                string d = string.Join(" · ", l.Select(x => (x.done ? ok : no) + " " + x.what)) + (miss.Count > 0 ? "\nFehlt noch: " + string.Join(", ", miss) : "");
                h.Row(nm + " · " + (all ? ok + " komplett" : (l.Count - miss.Count) + "/" + l.Count), () => d);
            }
        }
        static void Goals(Hud h)
        {
            /* Fabian 04.11.: kompakter – 4 statt 6 Reiter (Statistik und Abschluss stecken im Kodex), keine doppelten Abschnitte, Aufträge in einem Abschnitt */
            int ap = Game.AchPending(); h.Tabs("Ziele", "Erfolge" + (ap > 0 ? " · " + ap : ""), "Besucher" + (Game.Orders.Count > 0 ? " · " + Game.Orders.Count : ""), "Kodex"); int gtab = h.Tab;
            if (gtab == 1) { Achievements(h); return; }
            if (gtab == 2) { OrdersSheet(h); return; }
            if (gtab == 3) { Completion(h); Stats(h); Codex(h); return; }
            if (Game.Hard) /* 03.11.: eigene Hard-Ziele, immer drei offen */
            {
                h.Section("Hard-Ziele · " + (Game.S.hGoals?.Count ?? 0) + "/" + Game.HardGoals.Length);
                foreach (int gi in Game.HardGoalsOpen()) { int g = gi; var G = Game.HardGoals[g]; h.Row(G.t, () => Mathf.FloorToInt(Game.HardGoalProg(g) * 100) + " % · Belohnung " + Game.HardGoalXp(g) + " Erfahrung" + (G.perk != "" ? " + " + G.n + "× " + Game.Perk(G.perk).name : ""), () => Game.HardGoalProg(g) >= 1 ? "Abholen" : Mathf.FloorToInt(Game.HardGoalProg(g) * 100) + " %", () => Game.HardGoalProg(g) >= 1, () => Game.ClaimHardGoal(g), true, null, true, () => "Noch nicht geschafft – " + G.t); }
                if (Game.HardGoalsOpen().Count == 0) h.Note("Alle Hard-Ziele geschafft – Respekt!");
            }
            Game.DailyContracts(); var cs = Game.S.contracts;
            int qn = Mathf.Min(Game.QuestsSupported, GameData.Quests.Count);
            if (Game.S.quest < qn)
            {
                int qi = Game.S.quest; var Q = GameData.Quests[qi]; bool qr = Game.QuestProgress(qi) >= 1; h.Section("Hauptziel " + (qi + 1) + " von " + qn);
                h.Row(Q.t, () => (qr ? "Geschafft" : Mathf.RoundToInt(Game.QuestProgress(qi) * 100) + " %") + " · Belohnung " + Cr(System.Math.Round(Q.r * Game.GoalF)), () => Game.QuestProgress(qi) >= 1 ? "Abholen" : "Zeigen", () => true, () => { if (Game.QuestProgress(qi) >= 1) Game.ClaimQuest(); else { h.CloseSheet(); h.OnFocusQuest?.Invoke(); } }, Game.QuestProgress(qi) >= 1);
            }
            h.Section("Aufträge" + (cs.bonus ? " · alle Tagesaufträge geschafft" : " · alle drei Tagesaufträge: +50 % Bonus"));
            /* fertige zuerst */
            var order = Enumerable.Range(0, cs.list.Count).Where(k => !cs.list[k].done).OrderBy(k => Game.ContractProg(cs.list[k]) >= cs.list[k].target ? 0 : 1).ToList();
            foreach (int k0 in order) { int i = k0;
    int k = i; var it = cs.list[k]; h.Row(Game.ContractText(it), () => it.done ? "Erledigt" : Game.Fmt(Mathf.Min((float)Game.ContractProg(it), (float)it.target)) + " / " + Game.Fmt(it.target) + " · Belohnung " + Cr(it.reward), () => it.done ? "Erledigt" : Game.ContractProg(it) >= it.target ? "Abholen" : "Offen", () => !it.done && Game.ContractProg(it) >= it.target, () => Game.ClaimContract(k));
            }
            { Game.ChalRefresh(); var ch = Game.S.chal; 
              if (ch.act != "") { var c = Game.Chal(ch.act); if (c != null) h.Row(c.name + " · läuft", () => string.Format(Loc.Tr(c.desc), Game.Fmt(Game.ChalTarget(c.id))) + " · " + Mathf.Min(100, Mathf.FloorToInt((float)Game.ChalProgress() * 100)) + " % · noch " + Dur(Mathf.Max(0, Game.S.chal.left)), () => "+" + Cr(Game.ChalReward(c.id)), () => false, null, false); }
              else foreach (var id in ch.offer) { var c = Game.Chal(id); if (c == null) continue; var cid = id; h.Row(c.name, () => string.Format(Loc.Tr(c.desc), Game.Fmt(Game.ChalTarget(cid))) + " in " + Mathf.RoundToInt(c.time / 60) + " min · Belohnung " + Cr(Game.ChalReward(cid)) + (c.sd > 0 ? " + " + c.sd + " Sternenstaub" : ""), () => "Starten", () => Game.S.chal.act == "", () => Game.ChalStart(cid), false); }
              if (ch.act == "" && ch.offer.Count == 0) h.Note("Neue Herausforderungen in " + Dur(System.Math.Max(0L, ch.refresh - Game.Now())) + ".");
              if (ch.done > 0) h.Note("Geschafft: " + ch.done); }
            var pl = Enumerable.Range(0, Game.PlanetGoals.Length).Where(k => !Game.PGoalClaimed(ps, k)).OrderBy(k => Game.PGoalProg(ps, k) >= 1 ? 0 : 1).ToList();
            if (pl.Count > 0)
            {
                h.Section("Planetenziele " + ps.P.name + " · " + ps.sv.pgDone.Count + "/" + Game.PlanetGoals.Length);
                foreach (int k0 in pl.Take(4)) { int k = k0; h.Row(Game.PlanetGoals[k].text, () => Mathf.RoundToInt(Game.PGoalProg(ps, k) * 100) + " % · Belohnung " + Cr(Game.PGoalReward(ps, k)), () => Game.PGoalProg(ps, k) >= 1 ? "Abholen" : "Offen", () => Game.PGoalProg(ps, k) >= 1, () => Game.ClaimPGoal(k)); }
                if (pl.Count > 4) h.Note("… und " + (pl.Count - 4) + " weitere.");
            }
            var ev = ps.sv.ev;
            if (ev != null && ev.key != "")
            {
                var E = Game.EvDef(ev.key); h.Section("Ereignis");
                h.Row(E != null ? E.name : "Ereignis", () => (E != null ? E.hint : "") + " · noch " + Mathf.CeilToInt(Mathf.Max(0, ps.sv.ev.t) / 60f) + " min", () => ps.sv.ev.cell >= 0 ? "Hinfliegen" : "OK", () => true, () => { h.CloseSheet(); h.OnFocusEvent?.Invoke(); }, false);
            }
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
                h.Row(r.bag ? "Erzbagger abreißen" : "Bohrturm abreißen", () => Time.unscaledTime - demolishT < 4 ? "Wirklich? Noch einmal tippen reißt ihn ab, das Lager geht verloren." : "Macht den Platz frei · " + Cr(Game.RigRefund(ps, r)) + " zurück", () => Time.unscaledTime - demolishT < 4 ? "Ja, abreißen" : "Abreißen", () => true,
                    () => { if (Time.unscaledTime - demolishT < 4) { Game.DemolishRig(cell); demolishT = -99; h.CloseSheet(); } else demolishT = Time.unscaledTime; }, false);
                return;
            }
            h.Section("Tiefenabbau");
            string w = Game.RigWhy(ps, cell);
            h.Note(w != "" ? w + "." : "Tiefenvorkommen: " + Game.RichTxt(rich) + " (×" + Dec(rich, 2) + ") " + GameData.Mats[m].name + ". Ein Bohrturm fördert hier dauerhaft, deine Fahrzeuge holen die Ladung ab.");
            if (w == "") h.Row("Bohrturm bauen", () => { int oc = Game.RigOutpost(ps, cell); return oc >= 0 ? "Am Außenlager: " + Game.RigsAt(ps, oc) + " von " + Game.RIG_PER_OUTPOST : "Frei: " + ps.sv.rigs.Count(x => Game.RigOutpost(ps, x.cell) < 0) + " von " + Game.RIG_FREE; }, () => Cr(Game.RigCost(ps)), () => Game.S.credits >= Game.RigCost(ps), () => { if (Game.BuildRig(cell)) h.OpenSheet("rig", cell.ToString()); });
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
                () => Cr(Game.OutpostCost(ps)), () => Game.OutpostWhy(ps, cell) == "" && Game.S.credits >= Game.OutpostCost(ps), () => Game.BuildOutpost(cell), why: () => Game.Steps(Game.OutpostWhy(ps, cell), Game.OutpostCost(ps)));
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
                () => Game.ResWhy(dd) == "" && Game.S.credits >= dd.cost, () => Game.StartResearch(dd.id), why: () => Game.Steps(Game.ResWhy(dd), dd.cost));
            }
            if (UpVisible(GameData.Up["research"])) UpRow(h, "research");
        }

        static float resetT;
        static void Settings(Hud h)
        {
            /* Fabian 04.11.: Reiter statt einer langen Liste */
            if (Game.TestMode) h.Tabs("Allgemein", "Ton", "Spielstand", "Test", "Shop"); else h.Tabs("Allgemein", "Ton", "Spielstand", "Shop");
            int stab = h.Tab; if (!Game.TestMode && stab == 3) stab = 4;
            if (stab == 0) {
            if (!h.MenuOpen) h.Row("Hauptmenü", () => "Zurück zum Startmenü (Spiel starten, Einstellungen, Beenden)", () => "Öffnen", () => true, () => h.ShowMenu(), false);
            h.Section("Darstellung");
            h.Row("Farbe der Parzellenränder", () => "Parzellen, die noch dreckig sind, bekommen einen leuchtenden Rand (saubere zeigen den Planeten ohne Rand) – jetzt: " + Game.LineColors[Mathf.Clamp(Game.S.lineCol, 0, Game.LineColors.Length - 1)].name, () => Game.LineColors[Mathf.Clamp(Game.S.lineCol, 0, Game.LineColors.Length - 1)].name, () => true, () => Game.NextLineColor(), false);
            h.Section("Sprache · Language");
            h.Row("Sprache · Language", () => Loc.En ? "English · tap to switch to German" : "Deutsch · tippen für Englisch", () => Loc.En ? "Deutsch" : "English", () => true, () => { Loc.Set(!Loc.En); h.OpenSheet("settings", null); }, false);
            h.Section("Foto-Modus");
            h.Row("Oberfläche ausblenden", () => "Für schöne Bilder deiner Basis – mach dann einfach einen Screenshot. Der Knopf oben links holt die Oberfläche zurück.", () => "Foto-Modus", () => true, () => { h.CloseSheet(); h.OnPhoto?.Invoke(); }, false);
            } if (stab == 1) {
            h.Section("Ton");
            h.Row("Geräusche", () => "Wischen, Kasse, Rakete, Kristalle, Funde", () => Game.S.sound ? "An" : "Aus", () => true, () => Game.S.sound = !Game.S.sound, false);
            h.Row("Musik", () => "Leiser Klangteppich, wird lauter, wenn deine Basis arbeitet", () => Game.S.music ? "An" : "Aus", () => true, () => Game.S.music = !Game.S.music, false);
            h.Slider("Lautstärke Geräusche", () => Game.S.volSfx, v => Game.S.volSfx = v);
            h.Row("Vibration", () => "Kurz bei Raketenstart, Funden und Explosionen (nur am Handy)", () => Game.S.vibrate ? "An" : "Aus", () => true, () => Game.S.vibrate = !Game.S.vibrate, false);
            h.Slider("Lautstärke Musik", () => Game.S.volMusic, v => Game.S.volMusic = v);
            } if (stab == 2) {
            h.Section("Spielstand");
            h.Row("Jetzt speichern", () => "Das Spiel speichert alle 10 Sekunden und beim Schließen von selbst.", () => "Speichern", () => true, () => { Game.Save(); Game.Toast("Gespeichert", "good"); }, false);
            h.Row("Spielstand-Code kopieren", () => "Kopiert deinen ganzen Spielstand als Text in die Zwischenablage – in Notizen sichern oder auf ein anderes Gerät übertragen.", () => "Kopieren", () => true, () => { GUIUtility.systemCopyBuffer = Game.ExportCode(); Game.Toast("Spielstand-Code kopiert (" + (GUIUtility.systemCopyBuffer.Length / 1024 + 1) + " KB)", "good"); }, false);
            h.Row("Spielstand-Code einfügen", () => Time.unscaledTime - importT < 4 ? "Wirklich? Noch einmal tippen ersetzt deinen Spielstand durch den Code aus der Zwischenablage (der alte bleibt als Sicherung)." : "Lädt einen kopierten Spielstand-Code aus der Zwischenablage.", () => Time.unscaledTime - importT < 4 ? "Ja, laden" : "Einfügen", () => true,
                () => { if (Time.unscaledTime - importT < 4) { string err = Game.ImportCode(GUIUtility.systemCopyBuffer); Game.Toast(err == "" ? "Spielstand geladen" : err, err == "" ? "good" : "bad"); importT = -99; if (err == "") h.CloseSheet(); } else importT = Time.unscaledTime; }, false);
            if (Game.HasImportBackup) h.Row("Import rückgängig", () => "Holt den Spielstand von vor dem letzten Einfügen zurück.", () => "Zurück", () => true, () => { if (Game.UndoImport()) { Game.Toast("Alter Spielstand wiederhergestellt", "good"); h.CloseSheet(); } }, false);
            h.Row("Neu anfangen", () => Time.unscaledTime - resetT < 4 ? "Wirklich? Noch einmal tippen löscht deinen Unity-Spielstand." : "Löscht den Spielstand dieser Unity-Version und startet auf dem Mond.", () => Time.unscaledTime - resetT < 4 ? "Ja, löschen" : "Neu anfangen", () => true,
                () => { if (Time.unscaledTime - resetT < 4) { Game.ResetAll(); h.CloseSheet(); } else resetT = Time.unscaledTime; }, false);
            } if (stab == 3) {
            if (Game.TestMode)
            {
                h.Section("Testmodus");
                h.Row("Alle Planeten voll ausgebaut", () => "Schaltet alle " + GameData.POrder.Length + " Planeten frei, macht sie komplett sauber und baut alles: Gebäude, Upgrades, Fahrzeuge, Bohrtürme, Schild, Raketen, Terraforming (Wald), Spezialgüter voll. Dauert ein paar Sekunden.", () => "Alles", () => true, () => { Game.DevAll(); h.CloseSheet(); }, true);
                h.Row("Alles maximal ausbauen", () => "Basisstufe, alle Gebäude, Upgrades, Forschungen, Außenlager und Fahrzeuge auf diesem Planeten (Editor: F12).", () => "Ausbauen", () => true, () => { Game.DevMaxOut(); h.CloseSheet(); }, true);
                h.Row("+1 Mio Credits", () => "Geld zum Ausprobieren.", () => "+1 Mio", () => true, () => Game.Earn(1e6), false);
                h.Row("Planet säubern", () => "Erst auf 81 %, beim zweiten Mal komplett sauber.", () => "Säubern", () => true, () => Game.DevCleanTo(Game.CleanPct(Game.Cur) >= .8f ? 1.01f : .81f), false);
                h.Row("Wrack-Absturz", () => "Ein Wrack stürzt sichtbar vom Himmel, der Suchtrupp birgt es.", () => "Absturz", () => true, () => { var cs = Game.Cur; if (cs.sv.ev != null) { cs.sv.ev.key = ""; cs.sv.ev.t = 0; } if (!Game.StartEvent(cs, "wreck")) Game.Toast("Kein freier Platz für ein Wrack (erst ein paar Parzellen säubern)"); h.CloseSheet(); }, false);
                h.Row("Kometeneinschlag", () => "Ein Kometenbrocken schlägt ein.", () => "Einschlag", () => true, () => { var cs = Game.Cur; if (cs.sv.ev != null) { cs.sv.ev.key = ""; cs.sv.ev.t = 0; } if (!Game.StartEvent(cs, "impact")) Game.Toast("Kein freier Platz (erst ein paar Parzellen säubern)"); h.CloseSheet(); }, false);
                h.Row("Komet am Himmel", () => "Ein Komet zieht in wenigen Sekunden über den Himmel (am schönsten weit rausgezoomt).", () => "Komet", () => true, () => { LunarCleanup.Gameplay.WorldView.CometNow = true; LunarCleanup.Gameplay.WorldView.FreighterNow = false; h.CloseSheet(); }, false);
                h.Row("Frachter am Himmel", () => "Ein Frachter fliegt in wenigen Sekunden vorbei (weit rausgezoomt am besten zu sehen).", () => "Frachter", () => true, () => { LunarCleanup.Gameplay.WorldView.CometNow = true; LunarCleanup.Gameplay.WorldView.FreighterNow = true; h.CloseSheet(); }, false);
                h.Row("Meteoritenschauer", () => "Ein paar Meteore schlagen auf sauberen Parzellen ein (erst ein paar Parzellen säubern).", () => "Schauer", () => Game.Cur.nClean >= 3, () => { Game.Meteor(Game.Cur); h.CloseSheet(); }, false);
                h.Row("Vierteltag vorspulen", () => "Tag und Nacht testen.", () => "Vorspulen", () => true, () => { var cs = Game.Cur.sv; cs.dayT = (cs.dayT < 0 ? 0 : cs.dayT) + Game.DayLen(Game.Cur.pid) / 4; }, false);
                /* Fabian 02.11.: Test-Knöpfe für Build 34 */
                h.Row("Riesenmeteor hier", () => Game.S.ruin != "" ? "Gerade ist " + GameData.Planets[Game.S.ruin].name + " zerstört – erst „Zerstörung aufheben“." : Game.P.Count < 2 ? "Braucht mindestens zwei freigeschaltete Planeten." : "Schlägt in 15 Sekunden auf diesem Planeten ein. Mit Raketen (und ohne Schild) startet das Abfangen.", () => "Meteor", () => Game.S.ruin == "" && Game.P.Count >= 2 && !Game.DoomActive && Game.Icp == null,
                    () => { Game.StartDoom(Game.Cur.pid); if (Game.DoomActive) Game.S.doomT = 15; h.CloseSheet(); }, false);
                h.Row("Riesenmeteor anderswo", () => "Trifft in 30 Sekunden einen anderen Planeten – das Warnband bietet „hinfliegen“ an.", () => "Meteor", () => Game.S.ruin == "" && Game.P.Count >= 2 && !Game.DoomActive && Game.Icp == null,
                    () => { var o = Game.P.Keys.FirstOrDefault(k => k != Game.S.planet); if (o != null) { Game.StartDoom(o); if (Game.DoomActive) Game.S.doomT = 30; } h.CloseSheet(); }, false);
                h.Row("Abwehrraketen voll, Schild aus", () => "Raketen Stufe 6 (14 Raketen), Schild Stufe 0 – so kommst du sicher ins Abfang-Minispiel.", () => "Setzen", () => true, () => { var cs = Game.Cur.sv; cs.flakLv = Game.FLAK_MAX; cs.msLv = 0; Game.Toast("14 Abwehrraketen, kein Schild", "good"); }, false);
                h.Row("Meteorschild voll", () => "Schild Stufe 6 (85 %), Batterie voll, Nachladen fertig.", () => "Setzen", () => true, () => { var cs = Game.Cur; cs.sv.msLv = Game.MS_MAX; cs.sv.msCool = 0; cs.sv.energy = Game.EnergyCap(cs); cs.sv.supOk = true; Game.Toast("Schild bereit", "good"); }, false);
                h.Row("Zerstörung aufheben", () => Game.S.ruin != "" ? GameData.Planets[Game.S.ruin].name + " ist zerstört – sofort wieder aufbauen." : "Kein Planet zerstört.", () => "Aufheben", () => Game.S.ruin != "", () => { Game.S.ruinUntil = 0; }, false);
                h.Row("Hard Mode freischalten", () => Game.HardOpen ? "Ist schon frei – Einstellungen → Spielmodus." : "Schaltet den Hard Mode zum Testen frei (wie der Shop-Kauf).", () => "Frei", () => !Game.HardOpen, () => Game.UnlockHard(), false);
                h.Row("Seltener Fund", () => "Lässt sofort einen Fund zum Freiwischen erscheinen (in der Nähe der Kamera).", () => "Fund", () => true, () => { Game.DevFind(); h.CloseSheet(); }, false);
                h.Row("Export-Frachter geschenkt", () => ps.sv.xport ? "Hat dieser Planet schon." : "Schaltet den Überschuss-Export auf diesem Planeten frei.", () => "Schenken", () => !ps.sv.xport, () => { ps.sv.xport = true; Game.Toast("Export-Frachter aktiv", "good"); }, false);
                h.Row("Spezialgüter +200", () => "200 von jedem der 18 Güter ins Galaxie-Lager.", () => "+200", () => true, () => { foreach (var g in Game.Goods) Game.AddGood(g.id, 200); Game.Toast("+200 von jedem Gut", "good"); }, false);
                h.Row("Terraforming +1", () => "Stufe jetzt " + Game.Cur.sv.terra + " (" + Game.TerraName[Game.Cur.sv.terra] + ") – zum Anschauen, auch wenn der Planet noch nicht sauber ist.", () => "+1", () => Game.Cur.sv.terra < Game.TERRA_MAX, () => Game.DevTerra(Game.Cur.sv.terra + 1), false);
                h.Row("Terraforming komplett (Wald)", () => "Springt direkt auf Stufe 5 – so sieht der Planet voll wiederhergestellt aus.", () => "Wald", () => Game.Cur.sv.terra < Game.TERRA_MAX, () => { Game.DevTerra(Game.TERRA_MAX); h.CloseSheet(); }, false);
                h.Row("Terraforming -1 (Verfall)", () => "Eine Stufe zurück – so sterben die Bäume.", () => "-1", () => Game.Cur.sv.terra > 0, () => { Game.DevTerra(Game.Cur.sv.terra - 1); h.CloseSheet(); }, false);
                h.Row("Sturm auslösen", () => Game.Cur.P.storm != null ? "Der nächste Sturm kommt in 25 Sekunden." : "Auf diesem Planeten gibt es keine Stürme.", () => "Sturm", () => Game.Cur.P.storm != null, () => { var cs = Game.Cur.sv; cs.hzOn = 0; cs.hzWarned = false; cs.hzT = Game.Cur.P.storm.every - 25; }, false);
            }
            } if (stab == 0) {
            h.Section("Spielmodus");
            h.Row(Game.Hard ? "Hard Mode (aktiv)" : "Hard Mode", () => Game.Hard ? "Du spielst den Hard Mode: keine Putzfahrzeuge, Sammelshuttle bringt deine Ladung, Anzug-Energie und Ladesäulen. Dein normaler Spielstand wartet unberührt." : "Eigener, neuer Spielstand ab dem Mond: keine Putzfahrzeuge, keine Drohnen und Bohrtürme – du putzt selbst, ein Sammelshuttle bringt deine Ladung zur Basis. Perks findest du beim Wischen, dein Anzug braucht Energie, Ladesäulen bringen dich weiter hinaus. Dein normaler Spielstand bleibt erhalten, du kannst jederzeit zurück.",
                () => Game.Hard ? "Zum normalen Spiel" : "Hard Mode spielen", () => Game.Hard || Game.HardOpen, () => { h.CloseSheet(); Game.SwitchHard(!Game.Hard); }, !Game.Hard, null, true, () => Game.Hard ? "" : Game.HardWhy);
            if (Game.AdminWeb) h.Row("Admin-Modus", () => "Nur für dich: Test-Knöpfe an, eigener Test-Spielstand (dein normaler Spielstand bleibt unberührt). Beenden und neu laden = normales Spiel.", () => "Beenden", () => true, () => { Game.Save(); PlayerPrefs.DeleteKey("LunarCleanup.Admin"); PlayerPrefs.Save(); Game.Toast("Admin-Modus aus – Seite neu laden", "good"); }, false);
            if (Debug.isDebugBuild && !Application.isEditor)
                h.Row("Testmodus " + (Game.TestMode ? "an" : "aus"), () => Game.TestMode ? "Du spielst mit einem eigenen Test-Spielstand. Ausschalten und die Seite neu laden = normaler Spielstand." : "Normaler Spielstand. Einschalten und die Seite neu laden = Test-Spielstand mit Test-Knöpfen.",
                    () => Game.TestMode ? "Ausschalten" : "Einschalten", () => true, () => { Game.Save(); PlayerPrefs.SetInt("LunarCleanup.TestMode", Game.TestMode ? 0 : 1); PlayerPrefs.Save(); Game.Toast("Gespeichert – jetzt die Seite neu laden", "good"); }, false);
            } if (stab == 2) {
            h.Section("Konto-Speicher");
            h.Row(Platform.Cloud.Name, () => Platform.Cloud.SignedIn ? "Verbunden – dein Spielstand wird zusätzlich in deinem Konto gesichert." : "Noch nicht verbunden. Auf dem Handy sicherst du hier deinen Spielstand in deinem " + Platform.Cloud.Name + "-Konto.",
                () => Platform.Cloud.SignedIn ? "Verbunden" : "Anmelden", () => !Platform.Cloud.SignedIn, () => Platform.Cloud.SignIn(ok => Game.Toast(ok ? "Angemeldet – Spielstand wird gesichert" : "Anmeldung kommt mit der Handy-Version", ok ? "good" : "")), false);
            } if (stab == 4) {
            h.Section("Shop (Vorschau)");
            h.Note("Vorschau der späteren Angebote. Noch wird nichts berechnet und nichts vergeben.");
            foreach (var o in Platform.Offers) { var oo = o; h.Row(o.name, () => oo.desc, () => !oo.consumable && Platform.Owned(oo.id) ? "Gekauft" : oo.price, () => oo.consumable || !Platform.Owned(oo.id), () => Platform.Buy(oo, ok => Game.Toast(ok ? oo.name + " gekauft – danke!" : "Käufe sind noch nicht freigeschaltet – es wurde nichts berechnet", ok ? "good" : "")), true); }
            } if (stab == 0) {
            h.Section("Über");
            h.Note("Lunar Cleanup Tycoon · " + Game.VersionText + ". Planeten, Parzellen, Zahlen und Texte stammen 1:1 aus dem Browser-Prototyp; Modelle von Fabian.");
            }
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
            var open = list.Where(x => !IsMax(x)).ToList(); if (open.Count > 0) { h.Section("Upgrades für dieses Gebäude"); foreach (var x in open) UpRow(h, x); } MaxedRows(h, list.Where(IsMax).ToList());
        }

        static void Building(Hud h, string id)
        {
            var B = GameData.Bld[id]; h.Note(B.desc);
            if (ps.bld.Contains(id)) h.Row("Verschieben", () => (Game.Moved(ps, id) ? "Steht an deinem eigenen Platz. " : "") + "Stell das Gebäude frei an einen anderen Platz im Basisgelände – die Bänder werden neu verlegt.", () => "Verschieben", () => true, () => { h.CloseSheet(); LunarCleanup.Gameplay.GameRoot.MoveReq = id; }, false); /* Fabian 03.11. */
            BuildingAmpel(h, id);
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
