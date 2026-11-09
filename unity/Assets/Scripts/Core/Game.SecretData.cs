// Fabian 08.11.: „Immer andere Zeichen auf den Planeten und neue Geschichten.“
// Jeder Planet hat ein eigenes Zeichen im Boden (Stil, Farben) und 4 Logbuch-Kapitel (bei 10/40/70/100 % freigelegt).
// Die Kapitel ergeben zusammen eine Geschichte: Die „Wanderer“ haben eine Route durch die Sterne markiert –
// jemand hat sie mit Schleim verdeckt. Jedes Zeichen zeigt zum nächsten Planeten.
using System.Collections.Generic;

namespace LunarCleanup.Core
{
    public class GlyphStyle { public int style, n; public string col, col2, glow, name; }

    public static partial class Game
    {
        // style: 0 Sternkarte (n Strahlen), 1 Dreieck, 2 Wellen, 3 Spirale, 4 Auge, 5 Sechseck, 6 Perlenkreis (n Punkte), 7 Puls, 8 Doppelsichel
        public static readonly Dictionary<string, GlyphStyle> Glyphs = new Dictionary<string, GlyphStyle>
        {
            { "moon",       new GlyphStyle { style = 0, n = 6, col = "#9ff6ff", col2 = "#55c8ff", glow = "#38e8ff", name = "Sternkarte" } },
            { "mars",       new GlyphStyle { style = 1, n = 3, col = "#ffe0a0", col2 = "#ffb050", glow = "#ffb030", name = "Dreieck der Wanderer" } },
            { "europa",     new GlyphStyle { style = 2, n = 4, col = "#c8f4ff", col2 = "#7fd8ff", glow = "#60d8ff", name = "Wellenschrift" } },
            { "titan",      new GlyphStyle { style = 3, n = 3, col = "#ffd0a0", col2 = "#ff9a50", glow = "#ff8a30", name = "Methanspirale" } },
            { "io",         new GlyphStyle { style = 4, n = 1, col = "#c8ffb0", col2 = "#80ff70", glow = "#60ff40", name = "Wächterauge" } },
            { "venus",      new GlyphStyle { style = 5, n = 6, col = "#fff0b0", col2 = "#ffd060", glow = "#ffe060", name = "Siegel der Wolken" } },
            { "ganymed",    new GlyphStyle { style = 0, n = 8, col = "#d0e8ff", col2 = "#90b8ff", glow = "#7aa8ff", name = "Achtstrahl" } },
            { "enceladus",  new GlyphStyle { style = 2, n = 6, col = "#e0fbff", col2 = "#9fe8ff", glow = "#8ff0ff", name = "Geysirschrift" } },
            { "triton",     new GlyphStyle { style = 3, n = 4, col = "#ffd6e8", col2 = "#ff9ac8", glow = "#ff80c0", name = "Rückwärtsspirale" } },
            { "pluto",      new GlyphStyle { style = 4, n = 2, col = "#ffe6d6", col2 = "#ffb898", glow = "#ffa080", name = "Doppelauge" } },
            { "eris",       new GlyphStyle { style = 5, n = 6, col = "#e8e8ff", col2 = "#b0b0ff", glow = "#a0a0ff", name = "Kältesiegel" } },
            { "sedna",      new GlyphStyle { style = 1, n = 3, col = "#ffc8c0", col2 = "#ff8070", glow = "#ff6050", name = "Rotes Dreieck" } },
            { "proxima",    new GlyphStyle { style = 8, n = 2, col = "#ffd8b0", col2 = "#ff9a60", glow = "#ff7a40", name = "Doppelsichel" } }, /* 09.11.: kein Stern – Fabian mag das Symbol nicht */
            { "trappist",   new GlyphStyle { style = 6, n = 7, col = "#b0fff0", col2 = "#50e8c8", glow = "#30ffd0", name = "Kreis der Sieben" } },
            { "kepler",     new GlyphStyle { style = 2, n = 5, col = "#c0ffd0", col2 = "#70e890", glow = "#50ff80", name = "Regenschrift" } },
            { "cancri",     new GlyphStyle { style = 5, n = 6, col = "#ffffff", col2 = "#c8f0ff", glow = "#e0f8ff", name = "Diamantsiegel" } },
            { "gliese",     new GlyphStyle { style = 3, n = 5, col = "#d8e0ff", col2 = "#98a8ff", glow = "#8898ff", name = "Dampfspirale" } },
            { "poltergeist",new GlyphStyle { style = 7, n = 4, col = "#f0d0ff", col2 = "#c080ff", glow = "#c060ff", name = "Puls" } },
        };

        public static readonly float[] LogAt = { .1f, .4f, .7f, 1f };
        public static readonly Dictionary<string, (string title, string text)[]> Logs = new Dictionary<string, (string, string)[]>
        {
            { "moon", new[] {
                ("Linien im Staub", "Unter dem Mondstaub leuchten Linien. Zu gerade für Zufall – jemand hat sie in den Boden gezogen. Und genau auf diesen Linien liegt dieser schwarze Schleim."),
                ("Ein Kreis mit sechs Strahlen", "Es ist ein großer Kreis mit sechs Strahlen, an jedem Ende ein Punkt. Die Punkte stehen genau so, wie du von hier aus die Planeten siehst."),
                ("Der Dreck war Absicht", "Im Schleim steckt dieselbe Leuchtfarbe wie in den Linien. Der Dreck kam nicht zufällig – er sollte das hier verdecken. Die Frage ist nur: vor wem?"),
                ("Die Sternkarte", "Die Karte ist frei. Ein Strahl leuchtet golden – er zeigt zum Mars. Wer immer das gebaut hat, war vor dir dort.") } },
            { "mars", new[] {
                ("Rote Linien", "Auch hier: Linien unter dem Staub. Diesmal keine Kreise, sondern scharfe Kanten. Wer die Sternkarte auf dem Mond gezogen hat, war wirklich hier."),
                ("Ein Dreieck", "Ein Dreieck im Kreis. In den Ecken liegen kleine Abdrücke – wie von Füßen mit drei Zehen. Die Funker auf der Erde nennen sie jetzt „die Wanderer“."),
                ("Gegen den Sturm", "Der Schleim auf dem Mars ist frischer als der auf dem Mond. Jemand verteilt ihn noch – oder er wächst von selbst nach. Beides ist kein schöner Gedanke."),
                ("Ein Weg", "Die Spitze des Dreiecks zeigt nach außen, zum Jupiter. Die Wanderer haben keine Botschaft hinterlassen, sondern einen Weg. Und du folgst ihm gerade.") } },
            { "europa", new[] {
                ("Schrift im Eis", "Unter dem Eisstaub laufen Wellenlinien, eine über der anderen. Es sieht aus wie Schrift – nur liest man sie nicht von links nach rechts, sondern von außen nach innen."),
                ("Wasser", "Die Wellen zeigen den Ozean unter dem Eis. Die Wanderer wussten von ihm. Vielleicht kamen sie wegen des Wassers."),
                ("Kein Wasser für alle", "Zwischen den Wellen steht ein Zeichen, das auf dem Mond schon am Kreis klebte: ein Punkt mit einem Ring. Die Funker meinen, es heißt „bewahren“."),
                ("Zum Ringplaneten", "Die längste Welle endet in einem Pfeil – Richtung Saturn. Der nächste Halt der Wanderer liegt hinter seinen Ringen.") } },
            { "titan", new[] {
                ("Eine Spirale", "Im orangen Dunst von Titan liegt eine Spirale im Boden. Sie dreht sich nach innen, wie ein Strudel im Methansee."),
                ("Jahre in Kreisen", "Die Windungen sind ungleich lang. Jemand hat gezählt: Es sind Saturnjahre. Die Spirale ist ein Kalender – und er reicht sehr weit zurück."),
                ("Die letzte Windung", "Die innerste Windung ist mit Schleim verklebt, dicker als überall sonst. Was immer da stand, sollte niemand lesen."),
                ("Ein Datum", "Unter dem Schleim: ein einziger Strich am Ende der Spirale. Das Datum, an dem die Wanderer weiterzogen. Es ist nicht lange her. Und es zeigt zum Io.") } },
            { "io", new[] {
                ("Es sieht dich an", "Zwischen den Vulkanen schält sich ein Auge aus dem Schwefel. Mandelförmig, mit einem Ring in der Mitte. Unheimlich, wie es dir beim Wischen folgt."),
                ("Ein Wächter", "Die Wanderer haben es nicht als Kunst gemacht. Das Auge liegt genau über dem heißesten Schlot – als Warnung oder als Wache."),
                ("Wer schaut zurück", "Der Schleim auf Io wird warm, wenn du ihn wegwischst. Er reagiert. Vielleicht ist er gar kein Dreck, sondern etwas, das lebt."),
                ("Geschlossen", "Ganz frei sieht das Auge geschlossen aus – friedlich. Die Pupille zeigt zur Sonne, zur Venus. Dorthin, wo es am heißesten ist.") } },
            { "venus", new[] {
                ("Unter den Wolken", "Unter der Hitze und dem Säurenebel: Kanten eines Sechsecks. Sauber wie gegossen, trotz allem, was die Venus mit Metall macht."),
                ("Ein Siegel", "Im Sechseck liegen zwei Dreiecke, ineinander verschränkt. Mars und Venus zusammen. Die Wanderer verbinden ihre Zeichen."),
                ("Gebrochen", "Ein Stück des Siegels ist zerkratzt – unter dem Schleim. Jemand hat versucht, es zu zerstören, und hat es dann nur verdeckt."),
                ("Zurück nach außen", "Das Siegel ist ganz. In seiner Mitte zeigt ein Pfeil weit hinaus, zum größten Mond von allen: Ganymed.") } },
            { "ganymed", new[] {
                ("Acht Strahlen", "Auf Ganymed liegt wieder eine Sternkarte. Aber größer, mit acht Strahlen statt sechs. Die Wanderer kannten mehr Welten als wir."),
                ("Zwei Strahlen zu viel", "Zwei Strahlen zeigen dorthin, wo wir keinen Planeten kennen. Die Funker auf der Erde streiten, ob es Fehler sind oder Ziele."),
                ("Älter als der Mond", "Die Linien hier sind tiefer, verwitterter. Diese Karte ist älter als die auf dem Mond. Ganymed war früher dran."),
                ("Weiter zu den Geysiren", "Der goldene Strahl zeigt zu Enceladus. Unter den Geysiren dort soll das nächste Zeichen liegen.") } },
            { "enceladus", new[] {
                ("Schrift aus Dampf", "Die Wellen auf Enceladus laufen genau zwischen den Geysiren. Wenn sie ausbrechen, leuchten die Linien heller."),
                ("Sie lesen sich anders", "Die Funker haben die Wellen von Europa und Enceladus übereinandergelegt. Zusammen ergeben sie ein Wort – oder einen Namen."),
                ("Der Name", "Der Name passt zu keinem Volk, das wir kennen. Aber er taucht auch im Schleim auf. Die Wanderer und die Verhüller haben denselben Ursprung."),
                ("Flucht", "Die letzte Welle bricht ab, als hätten die Wanderer es eilig gehabt. Sie zeigt zu Neptun – zu Triton, dem Mond, der falsch herum läuft.") } },
            { "triton", new[] {
                ("Eine verkehrte Spirale", "Auch hier eine Spirale – aber sie dreht sich andersherum als auf Titan. Wie Triton selbst, der rückwärts um Neptun zieht."),
                ("Zurückgedreht", "Die Spirale zählt die Zeit nicht vorwärts, sondern rückwärts. Bis zu einem Moment, der noch nicht gekommen ist."),
                ("Ein Countdown", "Der Schleim liegt genau dort, wo der Countdown abläuft. Die Verhüller wollten nicht, dass jemand weiß, wie viel Zeit noch bleibt."),
                ("Es bleibt Zeit", "Frei gelegt zeigt die Spirale: Es bleibt Zeit, aber nicht ewig. Ihr Ende zeigt zum Rand – zu Pluto.") } },
            { "pluto", new[] {
                ("Zwei Augen", "Neben Plutos hellem Herz liegen zwei Augen im Boden, nebeneinander. Das Auge von Io hatte einen Zwilling."),
                ("Sie sehen sich an", "Die beiden Augen blicken einander an, nicht dich. Ein Wanderer und ein Verhüller? Oder zwei, die sich verabschieden?"),
                ("Ein Abschied", "Zwischen den Augen eine Linie, halb verschmiert. Die Funker lesen darin: „Wir gehen weiter, ihr bleibt.“ Jemand hat sich hier getrennt."),
                ("Hinaus", "Beide Blicke wenden sich am Ende nach außen. Hinter Pluto liegen nur noch Eis und Dunkel – und Eris.") } },
            { "eris", new[] {
                ("Ein kaltes Siegel", "Auf Eris wieder ein Sechseck wie auf der Venus. Doch das Eis hat es fast zerdrückt."),
                ("Kein Verschränken", "Die zwei Dreiecke in diesem Siegel berühren sich nicht. Was auf der Venus verbunden war, ist hier getrennt."),
                ("Streit", "Die Verhüller und die Wanderer waren einmal eins. Hier draußen haben sie sich gestritten – über den Weg, über die Karten, über euch."),
                ("Noch weiter", "Ein Dreieck zeigt zurück zur Sonne, eins nach draußen. Du folgst dem nach draußen: zu Sedna, die nie ganz heimkehrt.") } },
            { "sedna", new[] {
                ("Ein rotes Dreieck", "Auf Sedna ist das Dreieck rot statt golden. Gleiche Form wie auf dem Mars – nur umgedreht. Spitze nach unten."),
                ("Eine Warnung", "Ein umgedrehtes Zeichen ist bei den Wanderern eine Warnung, glauben die Funker. Hier haben sie gezögert, weiterzugehen."),
                ("Sie gingen trotzdem", "Unter dem Schleim: dieselben dreizehigen Abdrücke wie auf dem Mars, und sie führen aus dem Dreieck hinaus."),
                ("Zu einem anderen Stern", "Die Spitze zeigt nicht mehr auf einen Planeten unserer Sonne. Sie zeigt auf Proxima Centauri. Die Wanderer haben das Sonnensystem verlassen.") } },
            { "proxima", new[] {
                ("Zwei Sicheln", "Unter einer roten Sonne: zwei Sicheln, die sich den Rücken zukehren. Die erste Spur der Wanderer bei einem fremden Stern."),
                ("Zwei Sonnen", "Die Sicheln stehen für zwei Sonnen – Proxima und ihre großen Geschwister Alpha Centauri. Die Route führt von Stern zu Stern, immer weiter."),
                ("Der Schleim war schneller", "Selbst hier liegt Schleim, obwohl kein Mensch je hier war. Die Verhüller sind den Wanderern gefolgt – oder vorausgeeilt."),
                ("Sieben Geschwister", "Der goldene Punkt zwischen den Sicheln zeigt zu Trappist-1, wo sieben Planeten eng beieinander kreisen.") } },
            { "trappist", new[] {
                ("Ein Kreis aus Perlen", "Sieben leuchtende Punkte im Kreis, einer für jeden Planeten von Trappist-1. Die Wanderer kannten jeden einzelnen."),
                ("Eine Heimat?", "In der Mitte ein Ring, wie auf dem Mond: „bewahren“. Die Funker vermuten, dass die Wanderer hier zu Hause waren."),
                ("Leere Häuser", "Doch unter dem Schleim: nur Spuren von Abreise. Die Wanderer haben ihr Zuhause verlassen, um eine Route zu legen – für jemand anderen."),
                ("Für wen?", "Eine der sieben Perlen ist golden: Sie zeigt zu Kepler-22b, einer warmen, nassen Welt.") } },
            { "kepler", new[] {
                ("Regen auf Stein", "Auf Kepler-22b schreiben die Wellen fast wie Regen. Hier hat sich jemand Zeit gelassen."),
                ("Ein Gruß", "Zum ersten Mal ist es keine Karte und keine Warnung, sondern ein Gruß. Die Funker übersetzen ihn mit „Willkommen, Putztrupp“."),
                ("Sie meinten uns", "Die Wanderer haben die Route für die gelegt, die sauber machen. Der Schleim sollte genau die aufhalten. Euch."),
                ("Fast da", "Die Wellen fließen zu einem einzigen Punkt: 55 Cancri e, die Welt aus Diamant.") } },
            { "cancri", new[] {
                ("Diamantkanten", "Auf der glühenden Diamantwelt ist das Siegel aus purem Licht. Jede Linie funkelt, als wäre sie geschliffen."),
                ("Ganz verbunden", "Hier sind die zwei Dreiecke wieder verschränkt wie auf der Venus. Wanderer und Verhüller – an diesem Ort waren sie einig."),
                ("Eine Prüfung", "Unter dem Schleim steht: Nur wer bis hierher alles sauber gemacht hat, darf weiter. Der Dreck war nie ein Feind – er war eine Prüfung."),
                ("Dampf und Druck", "Das Siegel öffnet sich in Richtung Gliese 1214 b, einer Welt unter dichtem Dampf.") } },
            { "gliese", new[] {
                ("Spirale im Dampf", "Auf Gliese 1214 b dreht sich die Spirale schneller als alle anderen, als wollte sie dich hineinziehen."),
                ("Alle Zeichen", "In den Windungen tauchen alle bisherigen Zeichen auf: Kreis, Dreieck, Welle, Auge, Siegel. Eine Zusammenfassung deiner Reise."),
                ("Der letzte Schleim", "Hier ist der Schleim dünn und müde. Die Verhüller haben fast aufgegeben. Wer bis hierher kommt, hat die Prüfung bestanden."),
                ("Ein Puls", "Das Ende der Spirale zeigt auf einen toten Stern, der noch pocht: den Pulsar. Dort liegt Poltergeist.") } },
            { "poltergeist", new[] {
                ("Ein Herzschlag", "Unter der Strahlung des Pulsars: Ringe im Boden, die im Takt des Sterns aufleuchten. Wie ein Herzschlag."),
                ("Das Signal", "Das ist das Signal, das die Funker auf der Erde seit Jahren hören. Die Wanderer haben es hier angeschaltet, damit jemand kommt."),
                ("Wer die Wanderer sind", "Unter dem letzten Schleim liegt ein Zeichen, das du kennst: das Logo deines eigenen Putztrupps, nur sehr, sehr alt."),
                ("Ihr wart es selbst", "Die Wanderer waren ein Putztrupp wie deiner, vor langer Zeit. Sie haben die Route gelegt, damit es jemand zu Ende bringt. Jetzt ist alles sauber. Danke.") } },
        };
    }
}
