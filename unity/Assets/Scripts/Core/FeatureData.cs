// Daten für geplante Erweiterungen (siehe docs/IDEEN_SPEC.md). Noch nicht angebunden – nur Zahlen und Tabellen,
// damit die Umsetzung später nur noch Logik und Oberfläche braucht. Texte auf Deutsch, Du-Form.
using System.Collections.Generic;

namespace LunarCleanup.Core
{
    public class EventDef { public string key, name, hint; public float dur, w; }
    public class FindDef { public string name, desc; }
    public class SdNode { public string id, name, desc, needs; public int cost; public int[] costs; public System.Func<int, string> lv; public int Max => costs?.Length ?? 1; } /* 11.11.: Werkstatt mit Stufen – lv(n) beschreibt Stufe n */
    public class WeatherDef { public string key, name; public float every, dur; }

    public static class FeatureData
    {
        // ---------- B1 Ereignisse ----------
        public const float EV_MIN = 360, EV_MAX = 720; // Abstand in Sekunden Spielzeit (nur live)
        public static readonly EventDef[] Events =
        {
            new EventDef { key = "wreck", name = "Wrack-Absturz", hint = "Ein Wrack ist abgestürzt – der Suchtrupp ist unterwegs und birgt es", dur = 480, w = 3 },
            new EventDef { key = "impact", name = "Kometeneinschlag", hint = "Ein Kometenbrocken ist eingeschlagen – der Suchtrupp birgt die Trümmer (Credits, Kristalle, vielleicht ein Fund)", dur = 600, w = 2.5f },
            new EventDef { key = "comet", name = "Kometenschweif", hint = "Ein Kometenschweif lässt Leuchtkristalle wachsen", dur = 180, w = 2 },
            new EventDef { key = "solar", name = "Sonnensturm", hint = "Sonnensturm: Solarstrom doppelt, Fahrzeuge etwas langsamer", dur = 300, w = 2 },
            new EventDef { key = "vein", name = "Erzader", hint = "Eine Erzader: Bohrturm und Bagger fördern doppelt", dur = 600, w = 1.5f },
            new EventDef { key = "pile", name = "Riesiger Müllberg", hint = "Ein riesiger Müllberg ist abgeladen worden – nur zusammen schafft ihr ihn: mindestens 3 Fahrzeuge, du zählst beim Mitwischen wie eins. Je mehr, desto schneller. Belohnung: Geld, Kristalle, Sternenstaub – in unter 5 min doppelt so viele Kristalle!", dur = 900, w = 1.5f },
            new EventDef { key = "trader", name = "Händlerschiff", hint = "Ein Händler ist gelandet und zahlt 3 Minuten lang das Dreifache – tippe auf die Meldung oben", dur = 180, w = 1.5f },
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
        /// <summary>Fabian 11.11.: Sternenstaub-Werkstatt mit Stufen – jede Stufe kostet mehr, damit Sternenstaub immer einen Zweck hat.
        /// Alles bleibt bei einem Neustart erhalten. „Glanz“ entfällt (goldene Ränder gibt es längst kostenlos in den Einstellungen).</summary>
        public static readonly int[] CASH = { 0, 1000, 5000, 25000, 100000, 500000 };
        public static readonly SdNode[] Stardust =
        {
            new SdNode { id = "cash", name = "Startkapital", costs = new[] { 1, 3, 8, 15, 30 }, lv = n => "Neustart mit " + (CASH[n] / 1000) + ".000" + " Cr" },
            new SdNode { id = "fast", name = "Schnellstart", costs = new[] { 4 }, lv = n => "Nach dem Neustart gleich Basisstufe 2" },
            new SdNode { id = "buggy", name = "Ewige Flotte", costs = new[] { 5, 8, 12 }, needs = "fast", lv = n => n + (n == 1 ? " Reinigungsbuggy ist" : " Reinigungsbuggys sind") + " nach dem Neustart sofort da" },
            new SdNode { id = "price", name = "Gute Kontakte", costs = new[] { 3, 6, 10, 16, 25, 40, 60, 90 }, lv = n => "+" + (5 * n) + " % Verkaufspreis" },
            new SdNode { id = "hand", name = "Goldene Hand", costs = new[] { 4, 8, 14, 24 }, lv = n => "+" + (25 * n) + " % Saugkraft beim Wischen" },
            new SdNode { id = "offline", name = "Nachtschicht", costs = new[] { 3, 6, 10, 16 }, lv = n => "Offline-Zeit +" + (2 * n) + " Stunden" },
            new SdNode { id = "crystal", name = "Kristallgarten", costs = new[] { 3, 5, 9, 14 }, lv = n => "Leuchtkristalle wachsen " + (15 * n) + " % schneller" },
            new SdNode { id = "find", name = "Sammler", costs = new[] { 2, 3, 6, 10 }, lv = n => "+" + (5 * n) + " % Fundchance" },
            new SdNode { id = "event", name = "Ereignisradar", costs = new[] { 3, 5, 9 }, needs = "find", lv = n => "Ereignisse " + (10 * n) + " % häufiger" },
        };

        // ---------- A2 Wetter (nur Optik) ----------
        public static readonly Dictionary<string, WeatherDef> Weather = new Dictionary<string, WeatherDef>
        {
            { "mars", new WeatherDef { key = "dustdevil", name = "Staubteufel", every = 90, dur = 40 } },
            { "europa", new WeatherDef { key = "icefog", name = "Eisnebel", every = 0, dur = 0 } },
            { "io", new WeatherDef { key = "lavafont", name = "Lavafontänen", every = 30, dur = 6 } },
            { "titan", new WeatherDef { key = "methrain", name = "Methanregen", every = 240, dur = 90 } },
            /* Fabian 01.11.: Wetter auf allen Planeten mit Atmosphäre oder Ausgasung */
            { "ganymed", new WeatherDef { key = "snow", name = "Eisschnee", every = 200, dur = 80 } },
            { "enceladus", new WeatherDef { key = "snow", name = "Geysirschnee", every = 120, dur = 70 } },
            { "triton", new WeatherDef { key = "snow", name = "Stickstoffschnee", every = 180, dur = 90 } },
            { "pluto", new WeatherDef { key = "snow", name = "Tholin-Schnee", every = 220, dur = 90 } },
            { "eris", new WeatherDef { key = "snow", name = "Methanschnee", every = 160, dur = 80 } },
            { "sedna", new WeatherDef { key = "snow", name = "Roter Schnee", every = 240, dur = 90 } },
            { "proxima", new WeatherDef { key = "embers", name = "Sternenfunken", every = 90, dur = 30 } },
            { "trappist", new WeatherDef { key = "mist", name = "Leuchtnebel", every = 0, dur = 0 } },
            { "kepler", new WeatherDef { key = "rain", name = "Regen", every = 180, dur = 100 } },
            { "cancri", new WeatherDef { key = "embers", name = "Hitzeflimmern", every = 60, dur = 25 } },
            { "gliese", new WeatherDef { key = "mist", name = "Dampfschwaden", every = 0, dur = 0 } },
            { "poltergeist", new WeatherDef { key = "wisps", name = "Geisterlichter", every = 0, dur = 0 } },
            { "venus", new WeatherDef { key = "lightning", name = "Blitze", every = 12, dur = .3f } },
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
