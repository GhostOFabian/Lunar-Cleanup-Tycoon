// Daten für geplante Erweiterungen (siehe docs/IDEEN_SPEC.md). Noch nicht angebunden – nur Zahlen und Tabellen,
// damit die Umsetzung später nur noch Logik und Oberfläche braucht. Texte auf Deutsch, Du-Form.
using System.Collections.Generic;

namespace LunarCleanup.Core
{
    public class EventDef { public string key, name, hint; public float dur, w; }
    public class FindDef { public string name, desc; }
    public class SdNode { public string id, name, desc, needs; public int cost; }
    public class WeatherDef { public string key, name; public float every, dur; }

    public static class FeatureData
    {
        // ---------- B1 Ereignisse ----------
        public const float EV_MIN = 360, EV_MAX = 720; // Abstand in Sekunden Spielzeit (nur live)
        public static readonly EventDef[] Events =
        {
            new EventDef { key = "wreck", name = "Wrack-Absturz", hint = "Ein Wrack ist abgestürzt – reinige die Parzelle in 8 Minuten für den dreifachen Erlös", dur = 480, w = 3 },
            new EventDef { key = "comet", name = "Kometenschweif", hint = "Ein Kometenschweif lässt Leuchtkristalle wachsen", dur = 180, w = 2 },
            new EventDef { key = "solar", name = "Sonnensturm", hint = "Sonnensturm: Solarstrom doppelt, Fahrzeuge etwas langsamer", dur = 300, w = 2 },
            new EventDef { key = "vein", name = "Erzader", hint = "Eine Erzader: Bohrturm und Bagger fördern doppelt", dur = 600, w = 1.5f },
            new EventDef { key = "freighter", name = "Frachter in Not", hint = "Ein Frachter zahlt beim nächsten vollen Raketenstart 50 % mehr", dur = 300, w = 1.5f },
        };
        public const float WRECK_AMT = 3f, WRECK_BONUS = 500, SOLAR_MULT = 2f, SOLAR_VEH = .75f, VEIN_MULT = 2f, FREIGHTER_MULT = 1.5f; public const int COMET_MIN = 6, COMET_MAX = 10;

        // ---------- B2 Funde ----------
        public const float FIND_CHANCE = .03f, FIND_CHANCE_ANOMALY = .08f, FIND_DUP_CR = 250, ALBUM_BONUS = .05f;
        public static readonly Dictionary<string, FindDef[]> Finds = new Dictionary<string, FindDef[]>
        {
            { "moon", new[] { F("Apollo-Schraube", "Noch fest angezogen."), F("Alte Flagge", "Ausgeblichen, aber stolz."), F("Luna-Funkgerät", "Rauscht leise."), F("Mondauto-Rad", "Drahtgeflecht, kaum Abrieb."), F("Fußabdruck-Abguss", "Jemand war vor dir hier."), F("Goldene Plakette", "Grüße an alle, die das finden.") } },
            { "mars", new[] { F("Rover-Kamera", "Das letzte Bild zeigt einen Sonnenuntergang."), F("Fallschirm-Rest", "Rot-weiß gemustert."), F("Marsmeteorit", "Kam zurück nach Hause."), F("Hubschrauber-Rotor", "Flog 72 Mal."), F("Bohrkern", "Schichten wie Baumringe."), F("Kolonie-Schild", "Willkommen in Neu-Tharsis.") } },
            { "europa", new[] { F("Eisbohrer-Spitze", "Stumpf vom Ozeaneis."), F("Tiefsee-Sonde", "Hat etwas gesehen."), F("Kryo-Probe", "Noch gefroren."), F("Antennenschüssel", "Zeigte auf Jupiter."), F("Thermo-Anzug", "Größe M."), F("Algen-Fossil", "Grünlich schimmernd.") } },
            { "titan", new[] { F("Landekapsel-Deckel", "Methanfleckig."), F("Ballon-Hülle", "Schwebte jahrelang."), F("Methan-Kompass", "Zeigt zum See."), F("Bootsrumpf", "Für Kohlenwasserstoff-Seen gebaut."), F("Nebelkamera", "Linse beschlagen."), F("Orange Steine", "Riechen nach Kerze.") } },
            { "io", new[] { F("Lava-Thermometer", "Skala endet bei 1.600 °C."), F("Schwefelkristall", "Zitronengelb."), F("Hitzeschild-Kachel", "Angeschmolzen."), F("Vulkan-Seismograf", "Zittert noch."), F("Asbest-Handschuh", "Einer fehlt."), F("Glühender Stein", "Bleibt warm.") } },
        };
        static FindDef F(string n, string d) => new FindDef { name = n, desc = d };

        // ---------- B3 Kombo ----------
        public const float COMBO_WINDOW = 20, COMBO_STEP = .1f; public const int COMBO_MAX = 10;

        // ---------- B4 Tagesbonus ----------
        public static readonly float[] DailyMult = { 1, 2, 3, 4, 6, 8, 12 }; // × Erlös pro Minute
        public const float DAILY_MIN = 100; public const int DAILY_CRYSTALS_DAY7 = 3, DAILY_GRACE_DAYS = 1;

        // ---------- C2 Fahrzeug-Erfahrung ----------
        public static readonly float[] VehXp = { 500, 2000, 6000, 15000 }; // Schwellen für Stufe 1–4
        public const float VEH_LVL_BONUS = .04f;
        public static readonly string[] VehSpec = { "", "schnell", "robust", "sparsam" };
        public static readonly string[] VehSpecName = { "Keine", "Schnell (+15 % Tempo, −5 % Tank)", "Robust (−40 % Verschleiß)", "Sparsam (−30 % Energie)" };
        public static readonly string[] Skins = { "Werksfarbe", "Signalorange", "Polarweiß", "Tiefblau", "Smaragd", "Karminrot", "Graphit", "Gold" };

        // ---------- D1 Sternenstaub ----------
        public static readonly SdNode[] Stardust =
        {
            new SdNode { id = "cash1", name = "Startkapital I", desc = "Neustart mit 1.000 Cr", cost = 1 },
            new SdNode { id = "cash2", name = "Startkapital II", desc = "Neustart mit 5.000 Cr", cost = 3, needs = "cash1" },
            new SdNode { id = "cash3", name = "Startkapital III", desc = "Neustart mit 25.000 Cr", cost = 8, needs = "cash2" },
            new SdNode { id = "fast", name = "Schnellstart", desc = "Basisstufe 2 sofort", cost = 4 },
            new SdNode { id = "buggy", name = "Ewige Flotte", desc = "Ein Reinigungsbuggy ist sofort da", cost = 5, needs = "fast" },
            new SdNode { id = "find", name = "Sammler", desc = "+10 % Fundchance", cost = 3 },
            new SdNode { id = "price", name = "Gute Kontakte", desc = "+10 % Verkaufspreis", cost = 6 },
            new SdNode { id = "hand", name = "Goldene Hand", desc = "+25 % Saugkraft beim Wischen", cost = 4 },
            new SdNode { id = "offline", name = "Nachtschicht", desc = "Offline-Zeit +4 Stunden", cost = 6 },
            new SdNode { id = "event", name = "Ereignisradar", desc = "Ereignisse 20 % häufiger", cost = 5, needs = "find" },
            new SdNode { id = "glow", name = "Glanz", desc = "Goldene Parzellenränder", cost = 2 },
            new SdNode { id = "crystal", name = "Kristallgarten", desc = "Leuchtkristalle wachsen 30 % schneller", cost = 5 },
        };

        // ---------- A2 Wetter (nur Optik) ----------
        public static readonly Dictionary<string, WeatherDef> Weather = new Dictionary<string, WeatherDef>
        {
            { "mars", new WeatherDef { key = "dustdevil", name = "Staubteufel", every = 90, dur = 40 } },
            { "europa", new WeatherDef { key = "icefog", name = "Eisnebel", every = 0, dur = 0 } },
            { "io", new WeatherDef { key = "lavafont", name = "Lavafontänen", every = 30, dur = 6 } },
            { "venus", new WeatherDef { key = "lightning", name = "Blitze", every = 12, dur = .3f } },
            { "titan", new WeatherDef { key = "methrain", name = "Methanregen", every = 240, dur = 90 } },
        };

        // ---------- F2 Automatik-Regeln ----------
        public static readonly string[] RuleIf = { "prodFull", "rohFull", "vehWorn", "night", "storm" };
        public static readonly string[] RuleIfName = { "Produktlager über …", "Rohstofflager über …", "Fahrzeug-Zustand unter …", "Es ist Nacht", "Sturm zieht auf" };
        public static readonly string[] RuleThen = { "sell", "repair", "machOff", "machOn", "beltOff", "beltOn" };
        public static readonly string[] RuleThenName = { "Verkaufen", "Zur Werkstatt", "Maschine aus", "Maschine an", "Band aus", "Band an" };
        public const int RULES_MAX = 5, RULES_STAGE = 4;

        // ---------- F1 Benachrichtigungen ----------
        public static readonly string[] Notify = { "rohFull", "rocket", "crystals", "event", "daily" };
        public static readonly string[] NotifyName = { "Rohstofflager voll", "Rakete bereit", "Kristalle reif", "Ereignis läuft", "Tagesbonus wartet" };
    }
}
