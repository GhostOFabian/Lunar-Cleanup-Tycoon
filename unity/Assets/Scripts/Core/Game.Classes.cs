// Fabian 05.11.: Klassen im Hard Mode. Jede Klasse hat Stärken, eine Schwäche und eine eigene Ulti (volle Ulti-Leiste).
// Wahl ist Pflicht – auch in alten Hard-Spielständen (Fenster erscheint, bis eine Klasse gewählt ist). Erste Wahl gratis,
// jeder Wechsel kostet Geld. Die Putz-Nuke ist keine Ulti mehr, sondern ein teurer Spezial-Perk im Inventar.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public class ClassDef { public string id, name, icon, plus, minus, ult, ultDesc; }

    public static partial class Game
    {
        public static readonly List<ClassDef> Classes = new List<ClassDef>
        {
            new ClassDef { id = "tech", name = "Techniker", icon = "T", plus = "Anzug verbraucht 30 % weniger, Ladesäulen 30 % billiger", minus = "Wischkraft −10 %",
                ult = "Überlast", ultDesc = "Anzug sofort voll, 60 s ganz ohne Verbrauch, dazu 2 min lang alles auf dem Planeten 50 % schneller" },
            new ClassDef { id = "pro", name = "Putzprofi", icon = "P", plus = "Wischkraft +30 %, Kombos halten 5 s länger", minus = "Anzug verbraucht 20 % mehr",
                ult = "Wischsturm", ultDesc = "30 s lang dreifache Wischkraft und doppelt so breiter Pinsel" },
            new ClassDef { id = "hunt", name = "Schatzsucher", icon = "S", plus = "Perk-Funde doppelt so oft", minus = "Behälter −25 %",
                ult = "Schatzkarte", ultDesc = "Erntet alle Leuchtkristalle des Planeten und gibt dir sofort 3 Perk-Funde, einer davon mindestens selten" },
            new ClassDef { id = "boom", name = "Sprengmeister", icon = "B", plus = "Bombe und Kärcher reinigen 60 % mehr Parzellen, Ulti lädt 25 % schneller", minus = "Perk-Funde 30 % seltener",
                ult = "Bombenteppich", ultDesc = "Eine Kette von Explosionen reinigt bis zu 30 Parzellen rund um die nächste dreckige Stelle" },
            new ClassDef { id = "log", name = "Logistiker", icon = "L", plus = "Behälter +50 %, Sammelshuttle 40 % schneller", minus = "Wischkraft −10 %",
                ult = "Großtransport", ultDesc = "Behälter sofort ins Rohstofflager, danach 2 min Rohrpost und Magnet-Behälter" },
            new ClassDef { id = "crew", name = "Crew-Chef", icon = "C", plus = "2 Helfer-Plätze mehr, Helfer 30 % schneller", minus = "Anzug verbraucht 15 % mehr",
                ult = "Großeinsatz", ultDesc = "3 Staubsauger-Tornados für 25 s und 2 min Espresso für alle Helfer" },
        };
        public static ClassDef Klasse => S == null ? null : Classes.FirstOrDefault(c => c.id == S.hClass);
        /// <summary>Hard Mode ohne gewählte Klasse – dann muss erst gewählt werden.</summary>
        public static bool NeedClass => Hard && S != null && Klasse == null;
        static bool Cl(string id) => Hard && S != null && S.hClass == id;

        // Stärken und Schwächen als Faktoren (1 = neutral)
        public static float ClPower => Cl("pro") ? 1.3f : Cl("tech") || Cl("log") ? .9f : 1;
        public static float ClSuit => Cl("tech") ? .7f : Cl("pro") ? 1.2f : Cl("crew") ? 1.15f : 1;
        public static float ClPylon => Cl("tech") ? .7f : 1;
        public static float ClHand => Cl("log") ? 1.5f : Cl("hunt") ? .75f : 1;
        public static float ClFind => Cl("hunt") ? 2 : Cl("boom") ? .7f : 1;
        public static float ClUlt => Cl("boom") ? 1.25f : 1;
        public static float ClShuttle => Cl("log") ? 1.4f : 1;
        public static float ClHelper => Cl("crew") ? 1.3f : 1;
        public static int ClHelperMax => Cl("crew") ? 2 : 0;
        public static float ClCombo => Cl("pro") ? 5 : 0;
        public static float ClBlast => Cl("boom") ? 1.6f : 1;

        /// <summary>Wechsel kostet Geld: wie zwei Putzbomben-Pakete auf dem aktuellen Planeten (wächst mit Planet und Basisstufe). Erste Wahl gratis.</summary>
        public static double ClassCost() => S == null || string.IsNullOrEmpty(S.hClass) ? 0 : Math.Round(PerkCost("bomb") * 12);
        public static string ClassWhy(string id) { if (!Hard) return "Nur im Hard Mode"; if (S.hClass == id) return "Deine Klasse"; double c = ClassCost(); return S.credits < c ? Lacks(c) : ""; }
        public static bool PickClass(string id)
        {
            var d = Classes.FirstOrDefault(c => c.id == id); if (d == null) return false; string w = ClassWhy(id); if (w != "") return Fail(w);
            bool first = string.IsNullOrEmpty(S.hClass); double c = ClassCost(); S.credits -= c; S.hClass = id; if (!first) S.ult *= .5f; /* Wechsel kostet die halbe Leiste */
            Toast((first ? "Du bist jetzt " : "Neue Klasse: ") + d.name + " – Ulti: " + d.ult, "good"); Save(); return true;
        }

        /// <summary>Volle Ulti-Leiste: die Ulti der eigenen Klasse.</summary>
        static int ClassUlt(PState ps, Vector3 d)
        {
            var k = Klasse; if (k == null) { Fail("Wähle erst eine Klasse"); return 0; }
            switch (k.id)
            {
                case "tech":
                    S.suit = SUIT_MAX; Buff("overload", 60); ps.sv.boostT = Mathf.Max(0, ps.sv.boostT) + 120;
                    PerkFx("overload", d, null); Toast("Überlast! Anzug voll, 60 s ohne Verbrauch, 2 min alles 50 % schneller", "good"); return 1;
                case "pro":
                    Buff("storm", 30); PerkFx("storm", d, null); Toast("Wischsturm! 30 s dreifache Kraft und doppelt so breiter Pinsel", "good"); return 1;
                case "hunt":
                {
                    int kr = 0, nc = 0; foreach (var c in ps.sv.crystals.ToList()) { kr += CrystalGain(c); nc++; S.stats.crystals++; ps.sv.crystals.Remove(c); } S.lk += kr;
                    var got = new List<string>();
                    for (int i = 0; i < 3; i++) { string[] pool = i == 0 ? DropRare : fr.NextDouble() < .3 ? DropRare : DropCommon; var free = pool.Where(p => PerkN(p) < PerkCap(p)).ToArray(); if (free.Length == 0) { AddXp(3); continue; } string pk = free[fr.Next(free.Length)]; PerkAdd(pk, 1); got.Add(Perk(pk).name); }
                    { int fc = ps.pd.CellOf(d); if (fc >= 0 && !Offline) OnDrop?.Invoke(ps.pd.cells[fc].c, 1); } PerkFx("goldmop", d, null);
                    Toast("Schatzkarte! " + (nc > 0 ? nc + " Leuchtkristalle (+" + kr + "), " : "") + (got.Count > 0 ? "gefunden: " + string.Join(", ", got) : "Perks voll – Erfahrung statt Funde"), "good"); return 1;
                }
                case "boom":
                {
                    if (SnapWork(ps, d) < 0) return UltShine(ps, d);
                    PerkAdd("carpet", 1); int n = UseTool("carpet", d); if (n <= 0) PerkAdd("carpet", -1); return n;
                }
                case "log":
                    PipeHand(ps); Buff("rohrpost", 120); Buff("magnet", 120); PerkFx("rohrpost", d, null);
                    Toast("Großtransport! Behälter geleert, 2 min Rohrpost und Magnet-Behälter", "good"); return 1;
                case "crew":
                {
                    var c0 = SnapWork(ps, d); var at = c0 >= 0 ? ps.pd.cells[c0].c.normalized : d.normalized;
                    for (int i = 0; i < 3; i++) ps.sv.helpers.Add(new HelperSave { id = ++ps.sv.helperNext, d = (at + UnityEngine.Random.insideUnitSphere * .05f).normalized, life = 25 });
                    Buff("espresso", 120); PerkFx("tornado", at, null); Toast("Großeinsatz! 3 Tornados und 2 min Espresso für die Helfer", "good"); return 1;
                }
            }
            return 0;
        }
        static void Buff(string id, float t) { var e = S.buffT.Find(x => x.k == id); if (e == null) { e = new FKV { k = id }; S.buffT.Add(e); } e.v = Mathf.Max(0, e.v) + t; }
    }
}
