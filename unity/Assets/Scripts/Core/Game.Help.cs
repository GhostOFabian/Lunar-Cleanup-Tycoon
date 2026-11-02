// Fabian 02.11.: Wer auf etwas Gesperrtes tippt, erfährt warum – und welche Schritte zum Freischalten führen.
// Hud.Explain zeigt das als Fenster mit „Verstanden“. Texte kurz, Du-Form, ein Schritt pro Zeile.
using System;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public const string MoneyHelp = "So kommst du an Geld:\n1. Wische über schmutzige Parzellen oder lass Fahrzeuge putzen – der Schmutz wird zu Rohstoffen.\n2. Fahrzeuge bringen ihn zur Entladeanlage, Bänder bringen Waren zum Verkauf.\n3. Die Rakete fliegt die Waren weg und bringt Credits. Unten links auf die Rakete tippen startet sie sofort.";
        /// <summary>Grund + nächste Schritte zu einem Why-Text (leer = kein Hindernis außer Geld).</summary>
        public static string Steps(string why, double cost = 0)
        {
            if (string.IsNullOrEmpty(why))
            {
                if (cost > 0 && S.credits < cost) return Lacks(cost) + ".\n\n" + MoneyHelp;
                return "";
            }
            string s = why.TrimEnd('.') + ".";
            string w = why;
            if (w.StartsWith("Erst Basisstufe")) s += "\n\nSo geht's: Basis → Reiter „Bauen“ → oben „Basis ausbauen“. Jede Basisstufe kostet Credits.";
            else if (w.StartsWith("Erst erforschen") || (w.StartsWith("Erst ") && w.Contains("erforschen"))) s += "\n\nSo geht's:\n1. Baue eine Forschungsstation (Basis → Bauen).\n2. Starte das Forschungsprojekt (Basis → Ausbau → Forschung).\n3. Warte, bis es fertig ist – auch offline.";
            else if (w.StartsWith("Erst eine Forschungsstation")) s += "\n\nSo geht's: Basis → Reiter „Bauen“ → Forschungsstation.";
            else if (w.Contains("% von") && w.Contains("reinigen")) s += "\n\nSo geht's: Reise zurück (Planeten → Reisen) und reinige weiter – wische über schmutzige Parzellen oder kauf mehr Fahrzeuge. Ab 80 % geht es zum nächsten Planeten.";
            else if (w.StartsWith("Erst ") && w.EndsWith("freischalten")) s += "\n\nSo geht's: Planeten → „Neue Planeten“. Planeten werden der Reihe nach freigeschaltet.";
            else if (w.StartsWith("Erst ") && w.Contains("erreichen")) s += "\n\nDieses Fahrzeug kommt von einem anderen Planeten. Schalte ihn frei (Planeten → Neue Planeten).";
            else if (w.Contains("Fahrzeugplätze") || w.Contains("Fahrzeugplatz")) s += "\n\nMehr Plätze: Upgrades → Fahrzeughalle, oder die Basis ausbauen (Basis → Bauen).";
            else if (w.StartsWith("Es fehlen") || w.Contains("fehlen ")) s += "\n\nSpezialgüter kommen von anderen Planeten: Sobald der genannte Planet zu 100 % sauber ist, stellt er sein Gut von selbst her (40 pro Stunde, auch offline). Raketen bringen es hierher.\nÜbersicht: Planeten → Lieferketten.";
            else if (w.Contains("komplett sauber") || w.Contains("komplett reinigen") || w.Contains("100 % sauber")) s += "\n\nSo geht's: Reinige wirklich jede Parzelle dieses Planeten. Oben in der Mitte siehst du, wie viele noch fehlen.";
            else if (w.StartsWith("Erst die Upgrades ausreizen")) s += "\n\nSo geht's: Upgrades → die genannten Upgrades bis zur angezeigten Stufe kaufen.";
            else if (w.StartsWith("Noch") && w.Contains("Tiefe")) s += "\n\nSo geht's: Bau Bohrtürme auf sauberen Parzellen (Basis → Ausbau → Tiefenabbau). Ihre Förderung zählt hier.";
            else if (w.StartsWith("Erst die Entladeanlage") || w.StartsWith("Erst Stufe 2")) s += "\n\nSo geht's: Basis → Bauen → die Entladeanlage der Reihe nach bauen.";
            else if (w.StartsWith("Es läuft schon ein Projekt")) s += "\n\nEs läuft immer nur ein Forschungsprojekt. Warte, bis es fertig ist.";
            else if (w.StartsWith("Höchste") || w.StartsWith("Steht schon") || w.StartsWith("Erforscht") || w.StartsWith("Gekauft")) { }
            if (cost > 0 && S.credits < cost && !w.StartsWith("Höchste")) s += "\n\nDanach brauchst du noch " + Fmt(cost) + " Cr.";
            return s;
        }
    }
}
