// Geschichten der Flaggen (Fabian 01.11.): Jede Flagge erzählt ein Stück einer Geschichte – wer sie dort hingesteckt hat und was passiert ist.
// Je Planet läuft eine von sechs Geschichten (8 Kapitel). Die Kapitel kommen in der Reihenfolge, in der du die Truhen öffnest;
// das Finale liegt immer in der letzten Truhe. Dazwischen finden sich Notizen anderer Besucher.
// Unter jedem Eintrag steht eine Randnotiz von jemandem, der es vor dir gelesen hat – und darunter dein eigener Kommentar (der Text auf der Flagge).
// Platzhalter: {A} {B} {C} Personen, {S} Schiff, {P} Planet, {O} Ort, {Y} Jahr, {Y2} spätere Jahreszahl, {N} weitere Person.
using System;
using System.Collections.Generic;

namespace LunarCleanup.Core
{
    public static class FlagStories
    {
        public struct Entry { public string title, who, body, note; public bool finale; }

        static readonly string[] First = { "Mara", "Jonas", "Ilka", "Theo", "Noor", "Kasimir", "Lea", "Ruben", "Saga", "Emil", "Yara", "Ole", "Fenja", "Milan", "Ada", "Bastian", "Zoe", "Linus", "Ines", "Tamo" };
        static readonly string[] Last = { "Venn", "Okafor", "Lindqvist", "Brandt", "Sato", "Haldane", "Moreau", "Kowalski", "Reyes", "Falk", "Ivanova", "Nakamura", "Abara", "Quist", "Delacroix", "Wendt" };
        static readonly string[] Ships = { "Albatros", "Morgenstern", "Kiebitz", "Halcyon", "Nordlicht", "Kranich", "Odyssee", "Seeschwalbe", "Lotte", "Pfefferminz", "Ikarus II", "Fröhliche Wanda" };

        static readonly Dictionary<string, (string de, string en)> Places = new Dictionary<string, (string, string)>
        {
            ["moon"] = ("dem Meer der Ruhe", "the Sea of Tranquility"), ["mars"] = ("den Valles Marineris", "Valles Marineris"), ["europa"] = ("dem Conamara-Chaos", "the Conamara Chaos"),
            ["titan"] = ("dem Kraken Mare", "Kraken Mare"), ["io"] = ("der Loki Patera", "Loki Patera"), ["venus"] = ("den Maxwell-Bergen", "the Maxwell Mountains"),
            ["ganymed"] = ("der Galileo Regio", "Galileo Regio"), ["enceladus"] = ("den Tigerstreifen", "the Tiger Stripes"), ["triton"] = ("der Melonenhaut-Ebene", "the Cantaloupe Terrain"),
            ["pluto"] = ("der Sputnik-Ebene", "Sputnik Planitia"), ["eris"] = ("der Dysnomia-Senke", "the Dysnomia Basin"), ["sedna"] = ("der Roten Ebene", "the Red Plain"),
            ["proxima"] = ("der Dämmerzone", "the twilight zone"), ["trappist"] = ("der Siebenfach-Bucht", "Sevenfold Bay"), ["kepler"] = ("der endlosen Küste", "the endless coast"),
            ["cancri"] = ("dem Diamantgrat", "the Diamond Ridge"), ["gliese"] = ("den Dampfbecken", "the steam basins"), ["poltergeist"] = ("dem Pulsarfeld", "the pulsar field"),
        };

        // ---------- Die sechs Geschichten (Titel, dann 8 Kapitel: Überschrift | Absender | Text) ----------
        struct Ch { public string tDe, tEn, wDe, wEn, de, en; public Ch(string tDe, string tEn, string wDe, string wEn, string de, string en) { this.tDe = tDe; this.tEn = tEn; this.wDe = wDe; this.wEn = wEn; this.de = de; this.en = en; } }
        struct Arc { public string de, en; public Ch[] ch; }

        static readonly Arc[] Arcs =
        {
            new Arc { de = "Das Signal", en = "The Signal", ch = new[] {
                new Ch("Landung", "Landing", "Logbuch · {A}", "Log · {A}", "Die {S} steht sicher bei {O}. Alles läuft nach Plan. Nur das Funkgerät knackt seltsam: drei kurze Töne, ein langer. Immer wieder.", "The {S} has landed safely near {O}. Everything is going to plan. Only the radio crackles strangely: three short beeps, one long. Over and over."),
                new Ch("Von unten", "From below", "Notiz · {B}", "Note · {B}", "Ich habe das Signal angepeilt. Es kommt nicht aus dem All. Es kommt von unten – direkt aus dem Boden von {P}.", "I triangulated the signal. It isn't coming from space. It's coming from below – straight out of the ground of {P}."),
                new Ch("Die Platte", "The plate", "Logbuch · {A}", "Log · {A}", "Zwei Meter tief lag eine Metallplatte. Sie ist warm, obwohl hier alles andere kalt ist. Ich habe angeordnet, sie liegen zu lassen.", "Two metres down we found a metal plate. It is warm, though everything else here is cold. I ordered that it be left alone."),
                new Ch("Unsere Namen", "Our names", "Zettel · {C}", "Scrap of paper · {C}", "Ich hab sie nicht liegen lassen. Nachts hab ich die Platte umgedreht. Darauf sind Zeichen eingeritzt. Und darunter, ganz klein: unsere drei Namen.", "I didn't leave it alone. At night I turned the plate over. Symbols are carved into it. And beneath them, very small: our three names."),
                new Ch("Leere Stiefel", "Empty boots", "Notiz · {B}", "Note · {B}", "{C} ist weg. Der Anzug hängt im Schrank, die Stiefel stehen ordentlich davor. Keine Spuren draußen. Das Signal ist jetzt schneller.", "{C} is gone. The suit hangs in the locker, the boots stand neatly in front of it. No tracks outside. The signal is faster now."),
                new Ch("Der Fahrplan", "The timetable", "Logbuch · {A}", "Log · {A}", "Ich habe die Zeichen entschlüsselt. Es ist ein Fahrplan. Für Ankünfte. Die letzte war {C}. Die nächste ist in sehr, sehr vielen Jahren.", "I've decoded the symbols. It's a timetable. For arrivals. The last one was {C}. The next one is many, many years from now."),
                new Ch("Abflug", "Departure", "Notiz · {B}", "Note · {B}", "Ich fliege zurück. {A} bleibt und will warten, wer als Nächstes kommt. Ich stecke Flaggen in den Boden, damit jemand diesen Weg findet.", "I'm flying home. {A} is staying to wait for whoever comes next. I'm planting flags so that someone will find this trail."),
                new Ch("Pünktlich", "Right on time", "Letzter Eintrag · {A}, {Y2}", "Last entry · {A}, {Y2}", "Wenn du das liest, bist du pünktlich. Das Signal hat in dem Moment aufgehört, als deine Basis gelandet ist. Auf der Platte steht jetzt noch ein Name. Deiner. Keine Angst – es ist eine Einladung.", "If you're reading this, you're right on time. The signal stopped the moment your base touched down. There's one more name on the plate now. Yours. Don't be afraid – it's an invitation."),
            } },
            new Arc { de = "Der Schatz der {S}", en = "The Treasure of the {S}", ch = new[] {
                new Ch("Wer das findet", "Whoever finds this", "Brief · {A}", "Letter · {A}", "Wer das findet: Ich habe geschmuggelt, und ich bin nicht stolz darauf. Die {S} hatte etwas an Bord, das niemand finden sollte. Ich habe es hier auf {P} versteckt.", "Whoever finds this: I used to smuggle things, and I'm not proud of it. The {S} carried something nobody was supposed to find. I hid it here on {P}."),
                new Ch("Erster Hinweis", "First clue", "Rätsel · {A}", "Riddle · {A}", "Folge dem Staub, nicht dem Licht. Wo es am dreckigsten ist, liegt der Weg. – Klingt komisch? Wart's ab.", "Follow the dust, not the light. Where it's dirtiest, that's the way. – Sounds odd? Just wait."),
                new Ch("Verfolger", "Pursuers", "Tagebuch · {A}", "Diary · {A}", "Die Zollkreuzer von {B} sind mir auf den Fersen. Ich vergrabe die Truhen schneller, als ich denken kann. Eine davon ist leer – nur zur Ablenkung.", "{B}'s customs cruisers are on my tail. I'm burying the chests faster than I can think. One of them is empty – just a decoy."),
                new Ch("Zweiter Hinweis", "Second clue", "Rätsel · {A}", "Riddle · {A}", "Der Schatz ist nicht aus Gold. Er ist größer als jede Truhe. Du stehst schon fast drauf.", "The treasure isn't gold. It's bigger than any chest. You're almost standing on it."),
                new Ch("Erwischt", "Caught", "Protokoll · {B}", "Report · {B}", "{A} bei {O} wegen Schmuggels festgenommen. Ladung nicht gefunden. {A} lacht nur und sagt, wir würden sie nie finden, weil wir nie putzen.", "{A} arrested near {O} for smuggling. Cargo not found. {A} just laughs and says we'll never find it because we never clean."),
                new Ch("Aus der Zelle", "From the cell", "Brief · {A}", "Letter · {A}", "{B} besucht mich jede Woche und fragt nach dem Versteck. Ich sage immer dasselbe: unter dem Dreck. Niemand glaubt mir.", "{B} visits me every week and asks where it's hidden. I always say the same thing: under the dirt. Nobody believes me."),
                new Ch("Fast", "Almost", "Rätsel · {A}", "Riddle · {A}", "Wenn du so weit gekommen bist, hast du schon mehr geputzt als alle Zöllner zusammen. Eine Truhe noch.", "If you've come this far, you've already cleaned more than all the customs officers put together. One more chest."),
                new Ch("Der Schatz", "The treasure", "Letzter Brief · {A}, {Y2}", "Last letter · {A}, {Y2}", "Die {S} hatte Saatgut an Bord – Samen für einen ganzen Planeten. Ich habe sie unter den Staub von {P} gestreut. Sie brauchen nur eins: Licht. Du hast den Dreck weggeräumt. Schau genau hin, bald wird es hier grün.", "The {S} was carrying seeds – enough for a whole planet. I scattered them under the dust of {P}. They only need one thing: light. You cleared the dirt away. Look closely – soon it will turn green here."),
            } },
            new Arc { de = "Putzi-7", en = "Scrubby-7", ch = new[] {
                new Ch("Einschalten", "Power on", "Speicher · Putzi-7", "Memory · Scrubby-7", "PUTZI-7 AKTIV. Auftrag: Reinigung bei {O}. Besitzerin: Firma Glanz & {B}. Laune: hervorragend.", "SCRUBBY-7 ONLINE. Task: cleaning near {O}. Owner: Shine & {B} Ltd. Mood: excellent."),
                new Ch("Tag 400", "Day 400", "Speicher · Putzi-7", "Memory · Scrubby-7", "Tag 400. Niemand hat angerufen. Vielleicht sind alle beschäftigt. Ich putze weiter. Ich habe einen Stein gefunden, der aussieht wie ein Gesicht. Ich nenne ihn {A}.", "Day 400. Nobody has called. Maybe everyone is busy. I keep cleaning. I found a rock that looks like a face. I'm calling it {A}."),
                new Ch("Pleite", "Bankrupt", "Funkspruch · {B}", "Radio message · {B}", "An alle Einheiten: Glanz & {B} ist pleite. Alle Roboter abschalten. … Putzi-7? Putzi-7, hörst du mich? Ach, egal.", "To all units: Shine & {B} has gone bust. Shut down all robots. … Scrubby-7? Scrubby-7, can you hear me? Oh, never mind."),
                new Ch("Nicht abgeschaltet", "Not shut down", "Speicher · Putzi-7", "Memory · Scrubby-7", "Ich habe den Funkspruch absichtlich nicht gehört. {A} der Stein findet, ich mache das richtig. {P} ist noch nicht sauber.", "I didn't hear that message. On purpose. {A} the rock agrees with me. {P} isn't clean yet."),
                new Ch("Sturm", "Storm", "Speicher · Putzi-7", "Memory · Scrubby-7", "Ein Sturm hat alles wieder zugedeckt. Achtzig Jahre Arbeit. Ich habe kurz überlegt, traurig zu sein. Dann habe ich weitergeputzt.", "A storm covered everything again. Eighty years of work. I briefly considered being sad. Then I kept cleaning."),
                new Ch("Akku 3 %", "Battery 3%", "Speicher · Putzi-7", "Memory · Scrubby-7", "Akku bei 3 %. Ich stecke diese Flaggen, damit die Nächsten wissen, wo ich schon war. Bitte nicht wieder dreckig machen.", "Battery at 3%. I'm planting these flags so the next ones know where I've been. Please don't make it dirty again."),
                new Ch("Besuch", "A visitor", "Notiz · {C}", "Note · {C}", "Ich habe Putzi-7 gefunden, eingestaubt neben einem Stein mit Gesicht. Er hat noch einmal kurz geblinkt und „Danke“ gesagt. Ich weiß nicht, wofür.", "I found Scrubby-7, covered in dust next to a rock with a face. It blinked once and said “thank you”. I don't know what for."),
                new Ch("Neustart", "Reboot", "Speicher · Putzi-7, {Y2}", "Memory · Scrubby-7, {Y2}", "PUTZI-7 AKTIV. Solarstrom erkannt – jemand hat die Panels freigeputzt! Laune: hervorragend. Ich glaube, eins deiner Fahrzeuge zwinkert mir zu. Endlich Kollegen.", "SCRUBBY-7 ONLINE. Solar power detected – someone cleaned the panels! Mood: excellent. I think one of your vehicles just winked at me. Colleagues at last."),
            } },
            new Arc { de = "Briefe über den Horizont", en = "Letters across the horizon", ch = new[] {
                new Ch("Hallo, Station Ost", "Hello, East Station", "Brief · {A}", "Letter · {A}", "Hallo, wer immer in Station Ost sitzt. Unser Funk ist kaputt, also stecke ich Briefe an Flaggen und hoffe, dein Rover kommt vorbei. Hier bei {O} ist es sehr still.", "Hello, whoever is in East Station. Our radio is broken, so I'm pinning letters to flags and hoping your rover passes by. It's very quiet here near {O}."),
                new Ch("Antwort", "Reply", "Brief · {B}", "Letter · {B}", "Ich habe deinen Brief gefunden! Hier ist es auch still. Ich koche jeden Abend Nudeln und rede mit der Kaffeemaschine. Sie heißt Herbert.", "I found your letter! It's quiet here too. I cook pasta every evening and talk to the coffee machine. Its name is Herbert."),
                new Ch("Sternschnuppen", "Shooting stars", "Brief · {A}", "Letter · {A}", "Heute Nacht fielen Meteore über {P}. Ich habe mir bei jedem gewünscht, dass dein nächster Brief länger ist. Grüß Herbert.", "Meteors fell over {P} tonight. With every one I wished your next letter would be longer. Say hi to Herbert."),
                new Ch("Eine Karte", "A map", "Brief · {B}", "Letter · {B}", "Ich habe ausgerechnet, wo genau du wohnst. Es sind nur 40 Kilometer. Ich habe eine Karte gezeichnet, mit einem Herz in der Mitte. Zu kitschig?", "I've worked out exactly where you live. It's only 40 kilometres. I drew a map with a heart in the middle. Too cheesy?"),
                new Ch("Funkstille", "Radio silence", "Brief · {A}", "Letter · {A}", "Seit drei Wochen kein Brief. Der Sturm war schlimm bei euch, oder? Ich lasse jeden Abend das Außenlicht an. Bitte melde dich.", "No letter for three weeks. The storm was bad on your side, wasn't it? I leave the outside light on every night. Please write back."),
                new Ch("Unterwegs", "On the way", "Brief · {B}", "Letter · {B}", "Der Rover ist hin. Ich gehe zu Fuß. Herbert habe ich dabei, er ist schwerer als gedacht. Ich folge deinen Flaggen.", "The rover is wrecked. I'm walking. I brought Herbert, he's heavier than expected. I'm following your flags."),
                new Ch("Ein Licht", "A light", "Brief · {A}", "Letter · {A}", "Draußen am Horizont bewegt sich ein Licht. Ganz langsam. Und es riecht nach Kaffee, obwohl das auf {P} unmöglich ist.", "Out on the horizon a light is moving. Very slowly. And I can smell coffee, which is impossible on {P}."),
                new Ch("Angekommen", "Arrived", "Gemeinsam · {A} & {B}, {Y2}", "Together · {A} & {B}, {Y2}", "Wir wohnen jetzt zusammen in Station West. Herbert steht in der Mitte des Tischs. Danke, dass du unsere Flaggen gefunden und ihre Wege freigeputzt hast – so findet uns jetzt jeder Besuch.", "We live together in West Station now. Herbert has the place of honour in the middle of the table. Thank you for finding our flags and cleaning their paths – now any visitor can find us."),
            } },
            new Arc { de = "Die große Wette", en = "The Great Bet", ch = new[] {
                new Ch("Die Wette", "The bet", "Logbuch · {A}", "Log · {A}", "{B} hat gewettet, dass ich es nicht schaffe, auf ganz {P} Flaggen zu stecken, bevor die {S} wieder abfliegt. Einsatz: ein Jahr lang Geschirr spülen. Ich habe angenommen.", "{B} bet me I couldn't plant flags all over {P} before the {S} takes off again. Stake: a whole year of washing dishes. I said yes."),
                new Ch("Vorsprung", "In the lead", "Logbuch · {A}", "Log · {A}", "Sechs Flaggen bis Mittag. {B} sitzt in der Kantine und isst Pudding. Ich glaube, ich gewinne das locker.", "Six flags by noon. {B} is sitting in the canteen eating pudding. I think I'll win this easily."),
                new Ch("Sabotage", "Sabotage", "Logbuch · {A}", "Log · {A}", "Jemand hat meinen Rover mit Rasierschaum gefüllt. Auf {P}! Woher hat {B} hier Rasierschaum?", "Someone filled my rover with shaving foam. On {P}! Where did {B} even get shaving foam here?"),
                new Ch("Gegenangriff", "Counter-attack", "Notiz · {B}", "Note · {B}", "Ich gebe zu: der Rasierschaum war ich. Aber jemand hat dafür meinen Pudding gegen Mörtel getauscht. Das geht zu weit.", "I admit it: the shaving foam was me. But someone swapped my pudding for mortar. That's going too far."),
                new Ch("Unentschieden?", "A draw?", "Logbuch · {A}", "Log · {A}", "Ein Sturm hat die Hälfte meiner Flaggen unter Dreck begraben. Niemand kann sie mehr zählen. Ist die Wette jetzt ungültig?", "A storm buried half my flags in dirt. Nobody can count them anymore. Is the bet off now?"),
                new Ch("Schiedsrichter", "Referee", "Protokoll · {C}", "Report · {C}", "Ich leite jetzt offiziell das Schiedsgericht. Gewonnen hat, wer alle Flaggen wiederfindet. Ich habe ein Klemmbrett. Ich liebe dieses Klemmbrett.", "I'm officially the referee. Whoever finds all the flags again wins. I have a clipboard. I love this clipboard."),
                new Ch("Abflug", "Take-off", "Notiz · {B}", "Note · {B}", "Die {S} fliegt ab. Keiner von uns hat alle gefunden. Wir spülen jetzt beide Geschirr. Das Klemmbrett von {C} bleibt hier, für den nächsten Versuch.", "The {S} is leaving. Neither of us found them all. Now we both wash dishes. {C}'s clipboard stays here, for the next attempt."),
                new Ch("Sieger", "Winner", "Urkunde · {C}, {Y2}", "Certificate · {C}, {Y2}", "Hiermit erkläre ich dich zum Sieger der großen Wette von {P}. {A} und {B} müssen dir je ein Jahr lang das Geschirr spülen. Sie wissen es noch nicht. Bitte sag es ihnen schonend.", "I hereby declare you the winner of the Great Bet of {P}. {A} and {B} each owe you a year of washing dishes. They don't know yet. Please break it to them gently."),
            } },
            new Arc { de = "Die blinde Passagierin", en = "The Stowaway", ch = new[] {
                new Ch("Im Frachtraum", "In the cargo hold", "Heft · {A}, 11 Jahre", "Notebook · {A}, age 11", "Ich bin heimlich auf die {S} gestiegen. Mama arbeitet auf {P}, bei {O}, und ich will sie überraschen. Hier im Frachtraum riecht es nach Socken.", "I snuck aboard the {S}. Mum works on {P}, near {O}, and I want to surprise her. The cargo hold smells like socks."),
                new Ch("Entdeckt", "Discovered", "Heft · {A}", "Notebook · {A}", "{B} aus der Technik hat mich gefunden. Kein Schimpfen – nur Kekse und der Rat, Flaggen zu stecken, damit wir uns nicht verlaufen.", "{B} from engineering found me. No telling off – just biscuits and the advice to plant flags so we don't get lost."),
                new Ch("Die Station ist leer", "The station is empty", "Heft · {A}", "Notebook · {A}", "Mamas Station ist leer. Das Licht ist an, auf dem Tisch steht eine Tasse. Der Tee darin ist gefroren.", "Mum's station is empty. The light is on, there's a cup on the table. The tea in it is frozen."),
                new Ch("Spuren", "Tracks", "Notiz · {B}", "Note · {B}", "Spuren führen von der Station weg. Ein Fahrzeug, kein Mensch. {A} will sofort hinterher. Ich auch, aber ich tue so, als wäre ich ruhig.", "Tracks lead away from the station. A vehicle, not a person. {A} wants to follow them right away. So do I, but I'm pretending to be calm."),
                new Ch("Ein Funkspruch", "A radio call", "Heft · {A}", "Notebook · {A}", "Im Funk war eine Stimme. Ganz leise, aber ich kenne sie: „Bleib wo du bist, Spatz.“ So nennt mich nur Mama.", "There was a voice on the radio. Very faint, but I know it: “Stay where you are, sparrow.” Only Mum calls me that."),
                new Ch("Nicht bleiben", "Not staying", "Heft · {A}", "Notebook · {A}", "Ich bleibe nicht, wo ich bin. {B} sagt, das ist Unsinn – und wirft dann seufzend den Rover an.", "I'm not staying where I am. {B} says that's nonsense – then sighs and starts the rover."),
                new Ch("In der Höhle", "In the cave", "Notiz · {B}", "Note · {B}", "Wir haben den Rover von {C} gefunden, in einer Höhle, festgefahren. Daneben ein warmes Zelt. Daneben Fußspuren. Sie sind ganz frisch.", "We found {C}'s rover in a cave, stuck. Next to it, a warm tent. Next to that, footprints. Very fresh ones."),
                new Ch("Spatz", "Sparrow", "Heft · {A}, {Y2}", "Notebook · {A}, {Y2}", "Ich hab sie gefunden! Mama hat geschimpft und geweint und gelacht, alles gleichzeitig. Jetzt bin ich erwachsen und arbeite selbst hier. Wenn du diese Flagge liest: Pass auf die Leute auf, die nachts noch Licht brennen lassen.", "I found her! Mum scolded me and cried and laughed, all at once. I'm grown up now and I work here myself. If you're reading this flag: look after the people who leave a light on at night."),
            } },
        };

        // ---------- Notizen anderer Besucher (für die Flaggen zwischen den Kapiteln) ----------
        static readonly (string wDe, string wEn, string de, string en)[] Frags =
        {
            ("Postkarte · {N}", "Postcard · {N}", "Liebe Oma, {P} ist toll, nur das Essen schmeckt nach Staub. Der Ausblick bei {O} ist den ganzen Flug wert. Bitte gieß meine Kakteen.", "Dear Gran, {P} is great, only the food tastes of dust. The view near {O} is worth the whole flight. Please water my cacti."),
            ("Beschwerde · {N}", "Complaint · {N}", "Hiermit beschwere ich mich offiziell: Im Prospekt stand „sauberer Planet“. Hier liegt Dreck bis zum Knie. Ich will mein Geld zurück.", "I hereby officially complain: the brochure said “clean planet”. The dirt here is knee-deep. I want my money back."),
            ("Rezept · {N}", "Recipe · {N}", "Weltraum-Pfannkuchen: 2 Tüten Pulver-Ei, 1 Tüte Mehl, Wasser aus dem Recycler (nicht fragen). Bei niedriger Schwerkraft beim Wenden festhalten!", "Space pancakes: 2 packets of powdered egg, 1 packet of flour, water from the recycler (don't ask). In low gravity, hold on when flipping!"),
            ("Geologie · {N}", "Geologist · {N}", "Probe 117: eindeutig ein Stein. Probe 118: auch ein Stein. Probe 119: Moment … nein, Stein. Ich liebe meinen Job.", "Sample 117: definitely a rock. Sample 118: also a rock. Sample 119: wait … no, rock. I love my job."),
            ("Kinderzeichnung · {N}, 7", "Child's drawing · {N}, 7", "Auf dem Zettel: ein Raumschiff, eine riesige Sonne und ein Strichmännchen mit Besen. Darunter steht: „Wenn ich groß bin, putze ich Planeten.“", "On the paper: a spaceship, a huge sun and a stick figure with a broom. Underneath: “When I grow up, I'll clean planets.”"),
            ("Bergleute · {N}", "Miners · {N}", "Schicht 4 war hier. Wir haben 12 Tonnen gefördert und einen Socken verloren. Wer ihn findet: Er ist blau mit Raketen drauf.", "Shift 4 was here. We mined 12 tonnes and lost a sock. Whoever finds it: it's blue with rockets on it."),
            ("Forschungsnotiz · {N}", "Research note · {N}", "Seltsam: Nachts summt der Boden bei {O} ganz leise. Immer dieselbe Melodie. Ich glaube, es ist ein Wiegenlied.", "Strange: at night the ground near {O} hums very softly. Always the same tune. I think it's a lullaby."),
            ("Heiratsantrag · {N}", "Proposal · {N}", "Willst du mich heiraten? (Bitte ankreuzen: [ ] Ja [ ] Nein [ ] Frag mich nach dem Sturm.) Das dritte Kästchen ist angekreuzt.", "Will you marry me? (Tick one: [ ] Yes [ ] No [ ] Ask me after the storm.) The third box is ticked."),
            ("Tourist · {N}", "Tourist · {N}", "Ich habe 400 Fotos gemacht. Auf allen ist nur grauer Staub. Auf einem bin ich zu sehen. Ich sehe auch grau aus.", "I took 400 photos. They all show grey dust. I'm in one of them. I look grey too."),
            ("Funk · {N}", "Radio operator · {N}", "Habe heute eine Nachricht von der Erde empfangen. Sie ist 30 Jahre alt und lautet: „Bitte Milch mitbringen.“", "Received a message from Earth today. It's 30 years old and says: “Please bring milk.”"),
            ("Notfallplan · {N}", "Emergency plan · {N}", "Bei Sturm: 1. Ruhe bewahren. 2. Helm auf. 3. Nicht die Flaggen ausreißen, die halten den Boden fest. (Stimmt nicht, aber es beruhigt.)", "In a storm: 1. Stay calm. 2. Helmet on. 3. Don't pull up the flags, they hold the ground together. (Not true, but it's soothing.)"),
            ("Gedicht · {N}", "Poem · {N}", "Hier ist es staubig, hier ist es kalt, / ich bin noch jung, doch fühl mich alt. / Doch wenn die Sonne über {P} steht, / weiß ich, warum man so weit geht.", "It's dusty here, it's cold and grey, / I'm young but feel old every day. / But when the sun climbs over {P}, / I know why I came so far to see."),
            ("Liste · {N}", "List · {N}", "Mitgebracht: Zahnbürste, Lieblingstasse, 3 Bücher. Vergessen: Zahnpasta. Vermisst: Regen.", "Brought: toothbrush, favourite mug, 3 books. Forgot: toothpaste. Missing: rain."),
            ("Warnung · {N}", "Warning · {N}", "Achtung! Bei {O} wohnt etwas Kleines, Neugieriges unter den Steinen. Es klaut Schrauben. Es ist harmlos. Glaube ich.", "Warning! Near {O} something small and curious lives under the rocks. It steals screws. It's harmless. I think."),
            ("Abschied · {N}", "Farewell · {N}", "Mein letzter Tag hier. Ich stecke diese Flagge, damit ein Stück von mir auf {P} bleibt. Pass gut drauf auf.", "My last day here. I'm planting this flag so a little piece of me stays on {P}. Take good care of it."),
            ("Bauplan · {N}", "Blueprint · {N}", "Skizze einer Maschine, die Staub in Pfannkuchen verwandelt. Daneben: „Funktioniert noch nicht. Schmeckt trotzdem besser als die Kantine.“", "Sketch of a machine that turns dust into pancakes. Next to it: “Doesn't work yet. Still tastes better than the canteen.”"),
        };

        // ---------- Randnotizen: jemand hat es vor dir gelesen ----------
        static readonly (string de, string en)[] Notes =
        {
            ("Darunter, mit anderem Stift: „Ich war Jahre später hier. Es stimmt alles.“ – {N}", "Below, in a different pen: “I came here years later. It's all true.” – {N}"),
            ("Am Rand: „Hab das dreimal gelesen. Gänsehaut.“ – {N}", "In the margin: “Read it three times. Goosebumps.” – {N}"),
            ("Klein in die Ecke gekritzelt: „Wer hat das letzte Kapitel? Ich suche seit Wochen.“ – {N}", "Scribbled in the corner: “Who has the last chapter? I've been searching for weeks.” – {N}"),
            ("Darunter: „Ich glaube kein Wort. Aber ich lege die Flagge trotzdem zurück.“ – {N}", "Underneath: “I don't believe a word. But I'm putting the flag back anyway.” – {N}"),
            ("Mit Bleistift: „Die nächste Flagge liegt weiter draußen. Viel Glück.“ – {N}", "In pencil: “The next flag is further out. Good luck.” – {N}"),
            ("Ein Fleck, der nach Kaffee aussieht, und daneben: „Sorry.“ – {N}", "A stain that looks like coffee, and next to it: “Sorry.” – {N}"),
            ("Am Rand: „Ich hab hier geweint. Im Helm. Sehr unpraktisch.“ – {N}", "In the margin: “I cried here. Inside my helmet. Very impractical.” – {N}"),
            ("Darunter: „Hallo an alle, die das noch lesen. Ihr seid nicht allein.“ – {N}", "Underneath: “Hello to everyone still reading this. You're not alone.” – {N}"),
            ("Kaum lesbar: „Es fehlen Seiten. Jemand sammelt sie ein …“ – {N}", "Barely legible: “Pages are missing. Someone is collecting them …” – {N}"),
            ("Mit Glitzerstift: „Beste Flagge auf ganz {P}!!“ – {N}", "In glitter pen: “Best flag on all of {P}!!” – {N}"),
            ("Darunter: „Ich habe die Geschichte meiner Tochter vorgelesen. Sie will jetzt Astronautin werden.“ – {N}", "Underneath: “I read this story to my daughter. Now she wants to be an astronaut.” – {N}"),
            ("Am Rand, ganz ordentlich: „Gelesen, abgelegt, Staub entfernt.“ – {N}", "In the margin, very neatly: “Read, filed, dust removed.” – {N}"),
        };

        static int H(string pid, int k) { unchecked { int h = 17; foreach (char c in pid) h = h * 31 + c; return Math.Abs(h * 7919 + k * 104729); } }
        static string Person(string pid, int k) => First[H(pid, k) % First.Length] + " " + Last[H(pid, k + 50) % Last.Length];

        public static string ArcTitle(string pid) { var a = Arcs[ArcIndex(pid)]; return Fill(pid, Loc.En ? a.en : a.de, 0); }
        static int ArcIndex(string pid) { int i = Array.IndexOf(GameData.POrder, pid); return (i < 0 ? H(pid, 1) : i) % Arcs.Length; }

        static string Fill(string pid, string s, int k)
        {
            var P = GameData.Planets.TryGetValue(pid, out var pl) ? pl.name : pid;
            string a = Person(pid, 1), b = Person(pid, 2), c = Person(pid, 3); if (b == a) b = Person(pid, 7); if (c == a || c == b) c = Person(pid, 9);
            int yr = 2090 + H(pid, 4) % 120;
            string o = Places.TryGetValue(pid, out var pp) ? (Loc.En ? pp.en : pp.de) : P;
            return s.Replace("{A}", a).Replace("{B}", b).Replace("{C}", c).Replace("{S}", Ships[H(pid, 5) % Ships.Length]).Replace("{P}", Loc.Tr(P)).Replace("{O}", o)
                    .Replace("{Y2}", (yr + 60 + H(pid, 6) % 140).ToString()).Replace("{Y}", yr.ToString()).Replace("{N}", Person(pid, 20 + k));
        }

        /// <summary>Was in der Truhe an Platz <paramref name="slot"/> (0 = erste geöffnete) von <paramref name="count"/> Flaggen liegt.</summary>
        public static Entry Get(string pid, int slot, int count)
        {
            var arc = Arcs[ArcIndex(pid)]; int n = arc.ch.Length; var e = new Entry(); bool en = Loc.En;
            int ci = slot >= count - 1 ? n - 1 : slot < n - 1 ? slot : -1;
            if (ci >= 0) { var c = arc.ch[ci]; e.title = Fill(pid, (en ? "Chapter " : "Kapitel ") + (ci + 1) + " · " + (en ? c.tEn : c.tDe), slot); e.who = Fill(pid, en ? c.wEn : c.wDe, slot); e.body = Fill(pid, en ? c.en : c.de, slot); e.finale = ci == n - 1; }
            else { var f = Frags[(H(pid, 11) + slot) % Frags.Length]; e.title = en ? "Found item" : "Fundstück"; e.who = Fill(pid, en ? f.wEn : f.wDe, slot); e.body = Fill(pid, en ? f.en : f.de, slot); }
            var nt = Notes[(H(pid, 12) + slot * 5) % Notes.Length]; e.note = Fill(pid, en ? nt.en : nt.de, slot + 40);
            return e;
        }
        public static int Chapters => Arcs[0].ch.Length;
    }
}
