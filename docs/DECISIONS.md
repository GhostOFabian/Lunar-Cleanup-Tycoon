# Entscheidungen

Je eine Zeile: Entscheidung – Grund. Neue Einträge unten anfügen.

## Plattform
- Erst Browser-Prototyp (eine HTML-Datei, three.js r128), Unity später – der Spieler wollte das Spiel schnell sehen, bevor viel Aufwand reingeht.
- Prototyp ist Referenz für die Unity-Umsetzung – Verhalten und Zahlen sind dort getestet.

## Welt
- Planeten als Goldberg-Polyeder (12 Fünfecke + Sechsecke), ~140 m² pro Parzelle – gleichmäßige Kugelteilung, Zellzahl wächst mit der Frequenz.
- 10 Planeten Mond → Pluto mit steigendem Ertragsfaktor (1 bis 700) statt steigender Materialmengen – sonst wären Lager sofort voll.
- Basis = Ringe 0–2, Gebäude auf einem festen Plan mit ≥ 2 m Abstand – keine Überlappungen.

## Reinigung
- Keine Freischaltung und keine Kosten für Parzellen; reinigbar ist nur, was an Sauberes grenzt – Wunsch des Spielers, ermöglicht volle Automatik.
- Abschluss bei < 3 % Restschmutz – sonst hängen Parzellen bei 97 %.
- Meteoritenstaub selten, aber 8–11 Einschläge mit 50–90 % Bedeckung – „seltener, aber dann richtig“. (ersetzt in v4.7)
- Ein Wisch reinigt nur die Parzelle, auf der er begonnen hat – Wunsch: nie zwei gleichzeitig.
- Handgerät ohne Leistungsdeckel – ausdrücklicher Wunsch; stattdessen Fahrzeuge ×2,6 und weit ausbaubare Fahrzeug-Upgrades.

## Fahrzeuge und Logistik
- Raster-A* nur im Basisbereich (Radius 31 m), draußen gerade Linien – billig genug für Offline-Simulation; draußen gibt es nur Außenlager als Hindernis.
- Alle Förderbänder (auch Hochbänder) sind Hindernisse für Fahrzeuge – Trucks sind höher als die Brücken.
- Bänder nach dem Pull-Prinzip (nehmen nur, was am Ziel Platz hat) – verhindert gegenseitige Blockaden.
- Überlauf ab 90 % Lagerfüllung zur Verkaufsrakete – es fließt immer etwas, kein Totalstau.
- Verkauf per Rakete, die erst bei vollem Frachtraum startet – Wunsch des Spielers; ersetzt den alten „Autoverkauf“ (Cr/s).
- Drohnen nur für Fahrzeuge > 30 m vom Hangar mit ≥ 40 % Tank – nahe Fahrzeuge fahren selbst schneller.

## Progression
- Technik global (`maxStage`), Basisstufe pro Planet – alte Maschinen sofort baubar, Plätze müssen aber pro Planet ausgebaut werden.
- Anlagen-Upgrades (Stationen, Bänder, Maschinen, Lager, Solar, Verkauf) pro Planet, Rest global – jeder Planet hat wieder etwas aufzubauen.
- Leistungsanforderung (Doppelbürsten-Stufe) pro Planet, sonst 30 % – „man muss alte Maschinen upgraden, um auf neuen Planeten zurechtzukommen“.
- Nächster Planet ab 80 % des vorherigen statt Forschung – Wunsch des Spielers.
- Prestige ab Mars, Formel `√(Verdienst/200 k) + 3 × komplette Planeten` – eventuell zu großzügig, beobachten.

## Speichern
- Spielstand speichert nur noch die 32×32-Raster der aktiven Parzellen, Masken werden daraus neu erzeugt – ca. 95 % kleiner, kein Überlaufen des Browser-Speichers.
- Zusätzlich Cloud-Spielstand über die Claude-Artefakt-Datenbank (`data/users/<id>/save`, privat pro Spieler, in Stücken ≤ 180 000 Zeichen, zwei Slots im Wechsel) – getrennte Speicher je Ansicht waren die wahrscheinlichste Ursache der Resets.
- Ein lokaler Stand ohne Spielstand wird nie über einen vorhandenen Cloud-Stand gelegt; ein neuerer Cloud-Stand wird beim Start übernommen.
- Rolling Backup (`…-bak`, alle 5 min), unlesbare Stände werden als `…-broken-<Zeit>` aufgehoben, Export/Import-Code (gzip + Base64, Präfix `LCT1:`).

## Steuerung
- Ein Wisch, der die gesperrte Parzelle (Rand + 0,6 × Saugradius) oder den Planeten verlässt, wird ohne Absetzen zur Kamerabewegung – sonst „klebt“ der Finger. (ersetzt in v4.2)
- Wischen in den äußeren 14 % (max. 70 px) des Bildschirms scrollt den Planeten mit bis 260 px/s.

## v3.7
- Spezialfahrzeuge an Planeten gebunden (kaufbar ab Erreichen des Planeten) und auf Heimatplaneten ×2,2 + sturmfest – macht jeden neuen Planeten zu einem Grund für neue Fahrzeuge.
- Lastdrohnen nur für M/L-Fahrzeuge und Ziele > 45 m – kurze Wege fährt man schneller selbst.
- Batterien als Upgrade (Akkupark) statt Gebäude – auf der Basis ist nur noch Platz für drei große und ein kleines Gebäude.
- Geothermie ist auf dem Mond nicht baubar (keine Erdwärme), auf Io am stärksten.

## v3.8
- Ladestationen = Andockplätze der Entladestationen und Außenlager (Laden 1/16 der Kapazität pro Sekunde × Entladetempo) – auf der Basis ist kein Platz für ein weiteres Gebäude, und so ist jede Rückfahrt auch eine Ladefahrt.
- Akku-Fahrzeuge ziehen unterwegs keinen Netzstrom, beim Laden 2,5× ihren Verbrauch – Vorteil auf sonnenarmen Planeten. Leerer Akku: 30 % Tempo und Leistung, nie festgefahren.
- Teamarbeit: Parzelle wird nach Winkel in Sektoren je Fahrzeug geteilt, +10 % Reinigung je weiterem Fahrzeug (max. 4); bei > 35 % Restschmutz zieht eine belegte Parzelle Helfer an, sonst wird sie gemieden.
- Sammelbehälter 60 E × 1,5^Stufe; ohne Ziel-Auswahl entlädt der Knopf in den Trichter mit dem meisten Platz, sonst ins Rohstofflager. Neues Tutorial-Ziel 2, alte Spielstände werden per `qv` verschoben.

## v3.9
- Frachtauftrag pro Rakete (eine Rohware des Planeten, +60 % auf sie, nie zweimal hintereinander dieselbe) und Mischfracht (+8 % je weitere Sorte, max. +40 %) – so lohnt ein stetiger Rohstoffstrom zur Rakete dauerhaft, nicht nur als Überlauf.
- Das Rohstoffband bedient den Auftrag automatisch, hält aber 8 % der Lagerkapazität für Presse bzw. Raffinerie zurück, damit die Veredelung nicht leerläuft.
- Toast nur beim manuellen Start, sonst nur die schwebende Zahl – automatische Raketen starten zu oft.

## v4.0
- Tag/Nacht als Uhr pro Planet in `simStep` – offline gleich wie live; Solar-Spitze ×2,6, damit das Tagesmittel ungefähr gleich bleibt und Nächte über Speicher und Grundlast (RTG, Geothermie, Kern) gelöst werden.
- Nachts bleibt die Szene mit 34 % Himmelslicht lesbar – ganz dunkel wäre auf dem Handy nicht spielbar.
- Lichtkegel als additive, transparente Kegel und Bodenflecken statt echter Spotlights – viele Fahrzeuge mit echten Lichtern wären auf Handys zu teuer.
- Erfolge von Hand abholen (Belohnung 250 × 5^Stufe) und über den Neustart behalten – sichtbare Belohnung, kein Verlust beim Prestige.

## v4.1
- Sammelwagen als eigenes Objekt (`sp.cart`) statt Fahrzeugtyp – er reinigt nicht, braucht keinen Platz und keinen Strom und darf von Anfang an da sein.
- Der Spieler leert den Behälter selbst per Tipp in den wartenden Wagen (Wunsch: „bei dem ich meinen Behälter leeren kann“); der Wagen fährt nur los, wenn der Behälter fast voll ist oder gerufen wird.
- Neues Hauptziel 7 „Sammelwagen kaufen“; alte Stände werden per `qv` 3 verschoben.

## v4.2
- Wisch außerhalb der Parzelle pausiert statt die Kamera zu bewegen (Spieler fand das Verschieben störend); nur wenn ein Wisch in den ersten 0,3 s ohne Ertrag die Parzelle verlässt, wird er zur Kamerabewegung – so klebt der Finger trotzdem nicht. Ersetzt die Regel aus v3.6.1.
- Randscrollen nur, wenn der Finger 0,45 s am Rand über der eigenen Parzelle bleibt, 200 px/s.
- Arbeitslicht als ein dauerhaftes Punktlicht (Intensität 0 bei Tag) + Lichtfleck-Textur – keine Shader-Neukompilierung beim Ein-/Ausschalten.

## v4.3
- Laternen stehen im Sperr-Ring direkt um jedes Gebäude – dort fährt nie ein Fahrzeug, die Wegfindung bleibt unverändert.
- Nur 6 echte Punktlichter, die den Laternen nahe der Kamera folgen; alle übrigen leuchten nur optisch – konstante Lichterzahl, keine Shader-Neukompilierung, auf Handys bezahlbar.

## v4.4
- Fertige Planeten bekommen Tiefenabbau statt nur Meteoritenstaub – die Bohrtürme erzeugen einen Dauerstrom, der den weiteren Ausbau von Bändern, Lager, Maschinen, Energie und Verkauf auf diesem Planeten sinnvoll macht.
- Bohrturm-Zahl an die Basisstufe gekoppelt (2 + 2 × Stufe), damit sich auch die Stufen auf alten Planeten noch lohnen.
- Feste Vorkommen pro Parzelle (mager/gut/reich) – eine kleine Standortentscheidung, ohne Zufall beim Neuladen.
- Laternen als Paar an der Front jedes Gebäudes – vorher wirkte die Verteilung zufällig.
- Bohrtürme liefern über unterirdische Leitungen (keine Röhren) – zwölf sichtbare Röhren über der Basis waren zu unruhig.

## v4.5
- Bohrtürme mit eigenem Lager, das die vorhandene Flotte abholt (statt unsichtbarer Leitung) – Wunsch des Spielers; man sieht so, dass gebohrt wird, und die Fahrzeuge haben auf fertigen Planeten wieder Arbeit.
- Abholen hat Vorrang, sobald ein Lager 70 % voll ist; sonst nur, wenn es nichts zu reinigen gibt – Meteoritenstaub bleibt so weiter Aufgabe der Flotte.

## v4.6
- Orange Blinkpunkte als ein gemeinsames Sprite-Material (alle blinken gleichzeitig) – billig und auch auf der Planetenkarte sichtbar.

## v4.7
- Meteoritenschauer als Feld aus Nachbarparzellen statt einer Parzelle; auf fertigen Planeten größer, häufiger und ergiebiger – Wunsch des Spielers, gibt der Flotte dort neben den Bohrtürmen dauerhaft Arbeit. Ersetzt „selten, 8–11 Einschläge“.

## v4.8
- Endloser Ausbau pro Planet über Ausbaustufen, die Credits und Tiefenförderung auf genau diesem Planeten verlangen – jeder Planet bleibt so dauerhaft ausbaufähig, auch wenn Reinigung immer schneller wird.
- Ausbaustufen heben die Maximalstufe der lokalen Anlagen-Upgrades an statt neue Gebäude zu bringen – auf der Basis ist kein Platz mehr, und die vorhandenen Systeme skalieren weiter.
- Reisen als Kamerafahrt (Pose-Blend + seitliches Gleiten) statt Schwarzblende; der Welt-Neuaufbau passiert in dem Moment, in dem kein Planet im Bild ist.
- Ruckler beim Reisen: Neuaufbau, Textur-Upload und Shader-Kompilierung passieren gebündelt im Sternen-Moment, die Animationszeit läuft dabei nicht weiter; Speichern und Hintergrund-Simulation werden verschoben.

## v4.9
- Idle-Test-Knöpfe entfernt – sie haben Zeit doppelt simuliert und den Stand verzerrt („macht das Spiel kaputt“).
- Offline-Nachrechnung asynchron in Scheiben statt in einem Block; die Simulation bleibt dieselbe (1-s-Schritte von `simStep`), nur die Verteilung auf Bilder ändert sich.
- Live-Bilder mit mehr als 0,25 s Abstand holen die verlorene Zeit sofort nach (bis 5 s); darüber greift die Offline-Nachrechnung.

## v5.0
- Rohwaren nur dann zur Rakete, wenn keine Maschine sie hier veredelt – Wunsch des Spielers, maximiert den Verkaufswert; ersetzt die 8-%-Reserve und den rohwarenbasierten Frachtauftrag aus v3.9.
- Sicherheitsventil bei 97 % Rohstofflager bleibt, damit Fahrzeuge und Bänder nie ganz stehen; die Ampel zeigt den Engpass dann an.
- Der „Band an/aus“-Schalter für Rohwaren entfällt (er hätte nach der neuen Regel nichts mehr bewirkt); Produkte behalten ihn.

## v5.1
- Planetenziele aus einer gemeinsamen Vorlage, mit Planetenbezug (Spezialfahrzeug, Energiequelle, Planetenfaktor bei Zielen und Belohnungen) – jeder Planet hat so eigene, abschließbare Ziele, ohne 230 Ziele von Hand zu pflegen.
- Die letzten Planetenziele hängen an den endlosen Ausbaustufen (bis 20), damit auch fertige Planeten Ziele behalten.

## v5.2
- Fertige Planeten liefern sonst fast nur Startschmutz (Staub) – darum gemischter Meteoritenstaub, Nebenprodukte der Bohrtürme und Materialausgleich bei der Bohrturm-Platzierung; so laufen Presse, Schmelze und Raffinerie dauerhaft.
- Stationswahl mit Lastausgleich (Pufferfüllung × 40 m + 14 m je überzähligem Fahrzeug) statt reiner Nähe – damit alle Entladestationen und ihre Bänder arbeiten.
- Das Band Rohstofflager → Verkauf bleibt bewusst meist leer, wenn alles veredelt wird (Regel aus v5.0).

## v5.3
- Wege, deren gerade Linie die Basiszone schneidet, laufen auf einem Kreis knapp außerhalb (Radius 33–37 m) um die Basis – vorher fuhren Fahrzeuge zwischen zwei Außenpunkten quer durch die Gebäude.
- Schmutz unter Gebäudeflächen (fp + 0,6 m) zählt als sauber, damit kein Fahrzeug hineinfahren muss, um eine Randparzelle fertig zu machen.
- Drohnen geben an Entladestationen ab (Wunsch des Spielers) – deren Bänder bekommen so auch Arbeit; Flughöhe 12 m über allen Gebäuden.

## v5.4
- Presse bekommt ein eigenes Upgrade, weil sie mit 10 E pro Charge der Engpass der Kette ist.
- Gebäudestufen erst nach ausgereizten Upgrades („vollständig abgegrast“), mit sichtbarer Größe und Ringen; Obergrenze steigt mit der Ausbaustufe, damit es endlos weitergeht.
- Erzbagger als Endstufe der Bohrtürme (Wunsch des Spielers), räumt zusätzlich Meteoritenstaub – passt zu „holt alles raus, was da rumliegt“.
- Farben pro Planet über die Materialbibliothek, nicht pro Modell – ein Ort für alle Gebäude und Fahrzeuge.

## v5.5
- Presse-Volumen als Chargen pro Hub (ganzzahlig, damit Bricks Stückware bleiben) statt kontinuierlicher Menge; Puffer wachsen mit, sonst passt eine große Charge nicht hinein.

## v5.6
- Animation von der Simulation entkoppelt: die Presse hebt sich in festem Takt und zeigt die Menge pro Hub – schnell ist sie trotzdem, sie wirkt nur nicht hektisch.
- Pro Wirkung genau ein Upgrade: Tempo = Hydraulik-Tuning, Menge = Größere Presskammer; die doppelte Hochdruckpresse entfällt mit voller Erstattung.

## v5.7
- Gebäude wachsen seitlich nur, solange nichts berührt wird – sonst würden Bänder und Tore in die Gebäude ragen; die Stufe zeigt ein Dachmast statt Ringe am Boden.
- Pipeline endet im Silo des Rohstofflagers (senkrechter Einlauf) statt daneben; Kugeln im Rohr zeigen das Pumpen. Die Pump-Logik selbst war korrekt, man hat sie nur nicht gesehen.

## v5.8
- Pipeline bekommt ein eigenes Upgrade statt am Bandtempo zu hängen – eine Wirkung, ein Upgrade; alte Stände erhalten Pumpenstufe = ⌈Bandstufe / 2⌉, damit nichts langsamer wird.
- Die Vollausbau-Tests laufen bei Tag, damit Nachtstrom-Engpässe sie nicht zufällig scheitern lassen.

## v5.9
- Fahrzeuge meiden volle Entladeziele zeitweise, statt angedockt zu warten – sie sollen nur dort stehen, wo sie wirklich abladen können (Wunsch des Spielers).

## v6.0
- Langzeitmotivation über drei kleine, verbundene Systeme statt neuer Inhalte (Ausrichtung, Betriebsbonus, Werksaufträge) – Ursache war fehlende Entscheidung, nicht fehlender Inhalt (Analyse in `docs/LANGZEIT.md`).
- Kein Abbau bestehender Vorteile: globale Technik bleibt global, der Betriebsbonus kann nur steigen oder auf den Grundwert fallen, Werksaufträge zählen normale Raketenverkäufe.
- Planetenmärkte und ausbaubarer Sturmschild bewusst zurückgestellt (verschieben das Preisgefüge bzw. entwerten erworbenen Fortschritt).

## v6.1
- Außenlager-Plätze werden aus der Basisgeometrie berechnet, nicht gespeichert – so sind sie auf allen Ständen gleich und alte Außenlager außerhalb bleiben erhalten (keine Zwangsverschiebung).
- 80 %-Mechanik als Bonus (Druckbetrieb, Volllast), nicht als Strafe – ein volles Lager arbeitet schneller ab, statt zu blockieren.
- Totalschaden nur live und nur nach Warnung; offline wird nie ein Fahrzeug zerstört, damit Idle-Fortschritt sicher bleibt. Offline laufen Verschleiß und Reparatur trotzdem in `simStep`.
- Störungen beheben sich automatisch (Techniker), damit Offline-Produktion nicht dauerhaft steht; sofortige Behebung ist ein bezahlter Komfort.
- Wegfindung fällt nie mehr auf eine gerade Linie zurück; ohne Pfad wird um die Basis herum gefahren.

## v6.2
- Technik und Fahrzeug-Upgrades gelten pro Planet (Wunsch des Spielers, per Rückfrage bestätigt). Alte Planeten behalten ihren Stand, nur neue fangen bei 0 an – so geht kein Fortschritt verloren.
- Planetenlinien über Rezepttabellen in den bestehenden drei Maschinen statt neuer Gebäude: kein Layout-Umbau, alle Layout-Tests bleiben gültig, Bänder und Offline-Simulation funktionieren unverändert.
- Unterschiedliche Gebäudeanordnung pro Planet zurückgestellt: Positionen, Bänder, Laternen, Andockpunkte und Wegfindung hängen am gemeinsamen Layout; ein zweites Layout wäre ein eigener großer Schritt.
- Fahrzeuge dürfen auch offline zerstört werden (Wunsch des Spielers); die Rückkehr-Übersicht meldet es.
- Doppelbürsten-Pflicht pro Planet auf Stufe 2 begrenzt, weil die Stufen jetzt lokal gekauft werden.

## v6.3
- Layouts werden erzeugt statt von Hand gebaut: 17 Handlayouts mit Bändern wären fehleranfällig, der Generator nimmt nur Layouts an, die alle Layout-Regeln erfüllen. Der Mond bleibt beim bekannten Original.
- Layout hängt nur am Planetennamen und steht nicht im Spielstand – alte Stände bekommen auf ihren Planeten (außer dem Mond) das neue Layout; Pakete auf Bändern und Fahrzeuge laufen ohne Umbau weiter.

## Unity – erster Schritt
- Auf Wunsch des Spielers zuerst ein einzelnes MonoBehaviour-Skript statt der vollen Ordner-Architektur; Logik steckt trotzdem in einer reinen `Simulation`-Klasse mit `Tick(dt)` (live und offline), damit sie später in die Systeme aus CLAUDE.md zerlegt werden kann.
- Noch keine `.asmdef`: im Standard-Assembly findet Unity Input System, TextMeshPro und uGUI ohne Verweise – weniger Fehlerquellen beim ersten Einrichten.

## Unity 0.1
- Spieldaten werden aus dem Prototyp exportiert (`tools/export_unity_data.py` → `gamedata.json`), statt sie von Hand abzuschreiben – so bleiben Zahlen, Texte, Layouts und Parzellen 1:1 gleich.
- Modelle prozedural im Code (keine gekauften Assets), Oberfläche zur Laufzeit gebaut mit 390×844 als Referenz (CSS-Pixel des Browsers 1:1), damit Abstände und Schriftgrößen wie im Browser sind.
- Erst die Kernschleife bis zur Planetenreise portiert; spätere Systeme folgen, Ziele dahinter bleiben bis dahin ausgeblendet.
- Test-Tasten nur im Editor (`#if UNITY_EDITOR`), keine sichtbaren Test-Knöpfe im Spiel.
- Unity-Spielstand getrennt vom Browser-Spielstand (eigener Schlüssel), damit nichts verloren geht.
- Unity: Punkte auf der Planetenrückseite behalten in der Basis-Ebene ihre Richtung (statt fester Ecke 1e4/1e4) – sonst kreisen Fahrzeuge um die Basis. Im Browser-Prototyp ist das noch die alte Rechnung.
- Test-Tasten im Editor wirken nur im Testmodus mit eigenem Spielstand (`…-v1-test`), damit der echte Spielstand nie verändert wird.
- `rigRich` im Prototyp: Ergebnis jetzt vorzeichenlos (0,6–1,6), vorher konnten durch `^` negative Werte entstehen.
- 30.10.2026: Planetengröße wächst jetzt mit jedem Planeten (freq 5,7,9 … 24, also 252 bis 5.762 Parzellen). Die Reihenfolge bleibt (Entfernung/Schwierigkeit). Bereits begonnene Planeten behalten ihre alte Größe (`sp.freq`, alte Werte in `OLD_FREQ`), damit kein Spielstand kaputtgeht.
- 30.10.2026: Flüssige Flächen (Wasser, Lava, Säure, Methan) auf 11 Planeten, nur ab Ring 5 und nie auf dem Außenlager-Ring; jede Zelle wird nur geflutet, wenn alles Land erreichbar bleibt. Bodenfahrzeuge planen einen Umweg über Nachbarzellen; reinigen können sie nur Schwebefahrzeuge (neu: Schwebereiniger, Basisstufe 3; Magnetgleiter) oder die Hand.
- 30.10.2026: Neue Inhalte, die Fabian ausdrücklich für die Handy-Version wünscht (Leuchtkristalle, Weltraum-Himmel, Shop-Vorschau, Konto-Speicher), entstehen ab jetzt zuerst in Unity; der Browser-Prototyp bleibt Referenz für die bestehenden Regeln.
- 30.10.2026: Bänder, die sich in den erzeugten Layouts auf gleicher Höhe kreuzen, werden in Unity als Brücke (+1 m mit 3-m-Rampen) gezeichnet; laufen zwei Bänder kurz vor demselben Gebäude zusammen, mündet das spätere als Weiche auf das andere. Reine Optik, die Logik bleibt gleich.
- 30.10.2026: Shop und Konto-Speicher sind Platzhalter auf Fabians Wunsch (Ausnahme von „keine Platzhalter-Knöpfe“): sie erklären, dass noch nichts berechnet wird.
- 30.10.2026: Bandwege in Unity werden neu geroutet (rechtwinklig aus Flächenmitten), die Spiellogik behält die Bandlängen aus dem Browser-Layout – Transportzeiten und Spielstände bleiben gleich. Gebäude werden im Bild achsparallel gedreht (Logik-Ausrichtung der Andockplätze bleibt).
- 31.10.2026: Nur noch eine Entladeanlage je Planet (Fabians Wunsch). Intern bleiben u1/u2/u3 als gekaufte Ausbaustufen bestehen, damit alte Spielstände gültig bleiben; im Bild ein Gebäude (Modell der höchsten Stufe) mit allen Andockplätzen in einer Reihe. Der Browser-Prototyp hat noch drei Stationen und muss nachgezogen werden.
- 31.10.2026: Bandbrücken dürfen nicht in den letzten 4,8 m vor einem Gebäude liegen; kreuzen sich zwei Bänder dort, wird das andere angehoben, und der Router meidet Kreuzungen in Port-Nähe.
- 31.10.2026: Die Unity-Fassung liegt als WebGL-Build unter `play-unity/` im Repo (GitHub Pages). Online liegt der Release-Build (gzip, ~17 MB, ohne Testmodus); zum Aktualisieren Menü „Browser-Version für GitHub bauen“ und `Builds/WebGL-GitHub/Build/*` nach `play-unity/Build/` kopieren.
- 31.10.2026 (Fabians Wunsch): Ein Handwisch reinigt bis zu zwei Parzellen gleichzeitig (die erste und eine, auf die der Pinsel hinüberstreicht). Der Browser-Prototyp bleibt vorerst bei einer Parzelle pro Wisch.
- 31.10.2026: Kamera beim Wischen: am Bildschirmrand dreht sich der Planet mit, Pfeiltasten/WASD verschieben, rechte Maustaste dreht ohne den Wisch zu beenden. Scheinwerfer, Lichtkegel und Laternen-Lichtflecken sind tagsüber aus; durchsichtige Teile werfen keine Schatten.
- 31.10.2026 (Fabians Wunsch): Die Webseite bietet nur noch die Unity-Version an; der Browser-Prototyp bleibt als Referenz im Repo (prototype/), ist aber nirgends mehr verlinkt.
- 31.10.2026 (Fabians Wunsch): Planeten viel größer, bis 17.642 Parzellen (freq 5, 8, 10, 12, 14 … 34, 37, 40, 42). Begonnene Planeten behalten ihre Größe (PREV_FREQ im Prototyp, zusätzliche Größen pid@freq im Export). Unity: Texturauflösung je Parzelle sinkt auf großen Planeten (64/48/32/24 px), weniger Bodenteile je Parzelle.
- 31.10.2026: Die Unity-Version wird die neue Hauptversion; der Browser-Prototyp wird nicht mehr weiterentwickelt (bleibt nur als Nachschlagewerk im Repo).
- 31.10.2026: Spielstand-Code (Export/Import) nur als Übergang, bis die Handy-Version mit Cloud-Speicher kommt – niedrige Priorität.
- 31.10.2026: Claude darf die wichtigsten Tests selbst durchführen und abschließen (Testmodus, eigener Spielstand).
- 31.10.: Ampeln sitzen auf der höchsten breiten Dachfläche (≥ 0,8 m), rücken bei Schornstein/Antenne/Rakete in 35-cm-Ringen nach außen – nie freischwebend über dem Gebäude.
- 01.11.2026: Fabians eigene Modelle ersetzen die Kenney-Modelle (Kenney archiviert). Reihenfolge beim Laden: eigenes Modell → Kenney (nur wenn zurückgeholt) → prozedurale Form.
- 01.11.2026: Förderbänder bleiben frei verlegt (Router), übernehmen aber Profil, Farben, Stützen und Anschlüsse aus Fabians Bandteilen; feste Raster-Bandstücke werden nicht verwendet.
- 01.11.2026 (Fabians Wunsch): Pipelines komplett unsichtbar im Boden, keine Trasse und keine Deckel.
- 01.11.2026: Versionsnummer einheitlich – iPhone-App und Browser-Build heißen beide 0.21.
- 01.11.2026 (Fabian): Meteoritenschauer etwa 3× seltener als im Browser-Prototyp (Unity weicht hier bewusst ab).
- 01.11.2026: Runde Gebäude haben keine geraden Wände – Anschlussrahmen stehen dort davor statt hinein; Gebäude drehen sich nur in 90°-Schritten (Unterteile wie Entladeanlage und Raketenplattform nie).
- 01.11.2026 (Fabian): Flotte je Planet max. 15 Fahrzeuge, mit teuren Flottenplätzen (25 Mio × 6^n) bis 20; Fahrzeug-Upgrades bewusst schwächer als im Prototyp.
- 01.11.2026 (Fabian): Bohrtürme/Erzbagger: 5 frei platzierbar + 3 je Außenlager, abreißbar mit 30 % Erstattung; fünfter Außenlagerplatz auf der Gegenseite der Basis.
- 01.11.2026: Bandwege werden je Basis-Zustand zwischengespeichert (Datei im persistentDataPath + Startdatei im Spiel); bei Router-Änderungen Cache-Version (`RouteCache.Ver`) erhöhen.
- 01.11.2026: Upgrades erscheinen nur im Upgrade-Menü und im jeweiligen Gebäudeblatt, nie zusätzlich als Reiter in anderen Menüs.
- 01.11.2026 (Fabian): Englisch als zweite Sprache. Der Code bleibt deutsch; übersetzt wird beim Anzeigen (`Core/Loc.cs`, `UI/LText.cs`) über das Wörterbuch `Resources/Data/en.json` (ganze Texte + Textbausteine, längster Treffer an Wortgrenzen, Zahlen 1.234,5 → 1,234.5). Neue Texte: Baustein ins Wörterbuch eintragen. Standard: Gerätesprache, umschaltbar in den Einstellungen (PlayerPrefs, nicht im Spielstand).
- 01.11.2026 (Fabian): Bandanschlüsse an einer Gebäudewand mindestens 2 m auseinander (vorher 1,45 m, Rahmen standen Kante an Kante).
- 01.11.2026 (Fabian): Planetenmenü mit Reitern „Reisen“, „Neue Planeten“, „Sternenstaub“; Planeten nach Regionen (innere Planeten, Monde der Gasriesen, Zwergplaneten, Exoplaneten), Sternenstaub-Boni nach Gruppen (Schneller Start, Einnahmen, Funde und Optik).
- 01.11.2026 (Fabian): Vor jedem Anschlussrahmen 1,8 m gerades Band (Router-Startpunkt), damit Kurven nicht durch die Rahmen schneiden.
- 01.11.2026 (Fabian): Versionsnummer steigt mit größeren Updates (0.22 ab Build 17); die Build-Nummer wird im Spiel mit angezeigt.
- 01.11.2026 (Fabian): Keine Andockstationen-Modelle an der Entladeanlage; Fahrzeuge halten an denselben Andockplätzen ohne Grube.
- 01.11.2026 (Fabian): Ziele nicht dauerhaft im HUD; Abholbares zeigt ein Punkt am Ziele-Knopf.
- 01.11.2026 (Fabian): Fahrzeugliste zeigt nur Status; alle Aktionen im Fahrzeug-Fenster. „Zurücksetzen“ stellt ein Fahrzeug an die Garage (Ladung/Zustand bleiben).
- 01.11.2026 (Fabian): Rakete als eigener HUD-Knopf; geerntete Kristalle (kristall) standardmäßig verkaufen.
- 01.11.2026: Kenney-Modelle aus dem Projekt entfernt (eigene Modelle + prozedurale Formen reichen).
- 01.11.2026 (Fabian): Festgefahrene Fahrzeuge werden nach 25 s ohne Fortschritt automatisch an die Werkstatt gesetzt; zusätzlich „Alle zurücksetzen“.
- 01.11.2026 (Fabian): Geysire sind Partikel aus Bodenschloten (kein Modell), Inhalt richtet sich nach der Flüssigkeit des Planeten.
- 01.11.2026: Dateien zum Mac immer unter einem neuen Pfad in /mnt/user-data/outputs ablegen – derselbe Pfad wird beim zweiten Übertragen mit altem Inhalt geschrieben.
- 01.11.2026: Klang bleibt prozedural (Projektregel „keine fremden Assets“), dafür Glocken mit unharmonischen Teiltönen, gefiltertes rosa Rauschen und Akkordflächen je Planetengruppe.
- 01.11.2026 (Fabian): Kamera folgt beim Wischen nicht mehr der Putzstelle (ruhiger), nur der Bildrand schwenkt.
- 01.11.2026 (Fabian): Erfolge mit drei Stufen und Belohnungen zum Abholen; Speicherung als „t:id:stufe“/„c:id:stufe“ in S.achievements.
- 01.11.2026 (Fabian): Reiseprämie bei 100 % + doppelter Tiefenabbau auf sauberen Planeten + Reise auf Kredit, damit niemand festsitzt.
- 01.11.2026 (Fabian): Entdeckungen und Geysire sind Geländeformen (Höhe + Bodentextur + Emission), keine hingestellten Modelle.
- 01.11.2026 (Fabian): Sammelwagen gestrichen; Handsammeln geht direkt ins Rohstofflager, bestehende Wagen werden erstattet.
- 01.11.2026: Gekaufte Upgrades bleiben an ihrer Stelle, erst bei Max wandern sie in „Ausgereizt (Max)“; spontane Geysire erscheinen nur im Blickfeld, damit sie gesehen werden und nichts kosten.
- 01.11.2026 (Fabian): Flaggen erzählen Geschichten; Kapitel nach Öffnungsreihenfolge, Finale in der letzten Truhe – so bleibt es spannend, egal welche Flagge man zuerst findet. Geschichtentexte sind zweisprachig im Code, Loc.Tr lässt sie über ein unsichtbares Zeichen (U+2060) unverändert.
- 01.11.2026 (Fabian): Rakete startet auch teilbeladen, wenn nichts mehr nachkommt – sonst bringt Offline-Zeit nichts ein. Gilt live und offline (eine Simulation), offline nur mit kürzeren Wartezeiten.
- 01.11.2026 (Fabian): Felsen sind prozedural (RockKit) statt der Klotz-Modelle; Fabians übrige Bodenteil-Modelle (Schrott, Fässer, Eis …) bleiben.
- 01.11.2026: Raketenschwelle richtet sich nach dem Zufluss (≈ 4 min), nicht nach der Frachtraumgröße; die Teilstart-Regeln aus Build 26 bleiben als Rückfall.
- 01.11.2026: Die ersten 10 Ziele dienen als Einführung und stehen im Bild; danach Ziele nur im Ziele-Fenster.
- 01.11.2026 (Fabian): Höchstens 24 Fahrzeuge je Planet; die Fahrzeughalle ist der Weg dorthin (+1 je Stufe). Erweiterte Upgrade-Stufen zählen nicht als Bedingung für Gebäudestufen (max0).
- 01.11.2026 (Fabian): Riesenmeteor zerstört einen ganzen Planeten (10 min Sperre, Neustart); nur live, nie offline, nie beim einzigen Planeten. Flaggen und Geschichten bleiben erhalten.
- 01.11.2026: Müllberg-Abtrag wird je Fahrzeug gedeckelt statt über Härte – sonst schaffen voll ausgebaute Fahrzeuge ihn allein in Sekunden.
- 01.11.2026 (Fabian): Handy nutzt standardmäßig die PC-Grafikstufe (SSAO, weiche Schatten); „Sparsam“ in den Einstellungen als Rückfall für ältere Geräte und Akku.
- 01.11.2026: Grafikstufe „Hoch“ auf dem iPhone nur auf Probe mit automatischem Zurückschalten (wie beim Ändern der Bildschirmauflösung), weil sie den Bildschirm schwarz lassen kann.
- 01.11.2026: Nötige Shader-Varianten stehen in einer festen Variantenliste (vorgeladene Shader), nicht nur in Platzhalter-Materialien – die prüft Unity beim Neustart nach und streicht Schlüsselwörter.
- 02.11.2026: Eigene Bandwege sind nur Optik – die Simulation rechnet weiter mit der Bandlänge aus dem Layout, damit Umlegen nicht Balance oder Offline-Ertrag verändert.
- 02.11.2026: Planetenbedarf an Spezialgütern = die zwei Nachbarn in der Planetenreihe (Mond: Mars + Europa) – einfach zu merken, immer erreichbar.
- 02.11.2026: Nicht auf dem Planeten = nur der Schild zählt (kein Abfangen aus der Ferne); dafür Warnband mit „hinfliegen“.
- 02.11.2026: Mehr als die Hälfte der Brocken durch = Planet zerstört; weniger = Teilschaden.
- 02.11.2026: Terraforming-Pflanzen werden zu wenigen Meshes zusammengefasst; Bäume sind je eine Mesh mit zwei Teilen (Stamm, Krone), damit sie einzeln im Wind schwingen, ohne neue Shader-Varianten (kein GPU-Instancing).
- 02.11.2026: Neue Modelle kommen als OBJ aus dem Blender-Skript (läuft auch ohne Blender); FBX erst, wenn Fabian das Skript in Blender exportiert. Materialnamen LC_* bleiben gleich.
- 02.11.2026 (Fabian): Beim Abfangen gehen die unteren Knöpfe weg – Brocken hinter Knöpfen ließen sich nicht antippen.
- 02.11.2026 (Fabian): Alles, was verkauft wird, läuft durchs Produktlager; nur ein Band ins Schiff. Bänder zum selben Gebäude werden zusammengeführt (nur Merger am Ende, nie Verteiler).
- 02.11.2026 (Fabian): Bestellungen gelten für alle Waren aller Planeten, 5–10 gleichzeitig, eigenes Menü wie Herausforderungen. Fracht zwischen Planeten wird direkt aus dem Lager des Herkunftsplaneten zurückgelegt (keine eigene Frachtsimulation).
- 02.11. (Build 38): Offline-Zeit wird beim Start nur für den aktuellen Planeten (plus Forschung/Meteor/Challenges/Bestellungen) nachgerechnet; andere Planeten speichern ihre offene Zeit in `offPend` (Default 0, max. Offline-Grenze) und holen mit 4 ms pro Bild im Hintergrund auf – vor einer Reise wird der Zielplanet sofort fertig gerechnet. Gleiche Simulation (`StepPlanet`).
- 02.11. (Build 38): Reist man zu einem Planeten, der noch Offline-Zeit nachholt, geht die Reise sofort; der Planet holt mit Vorrang (25 ms/Bild) auf, ein kleiner Hinweis zeigt die Restzeit (vorher blockierte die Reise bis 14 s).
- 02.11. (Build 38): Y-Zusammenführungen enden auf der Mitte des Hauptbandes, Brücken nie über das Y-Ende; Hauptband wird vor dem einmündenden Band geplant. Basisprüfung: 847 → ~60 Befunde. Ketten-Y (Merger in Merger) ausprobiert und verworfen – machte es schlechter.
- 02.11. (Build 38): Band-Editor hebt den vorgeschlagenen Weg selbst an, wenn er ein Band kreuzt – der Startzustand ist immer gültig.
- 02.11. (Build 38): Saubere, leere Parzellen werden nicht mehr gespeichert. Der große Spielstand (11 MB) ist echter Meteoritenstaub auf sauberen Parzellen – kompakteres Format später.
- 02.11. (Build 39): Band-Editor „Zeichnen“: Weg mit dem Finger nachziehen; der Strich wird geglättet (max. 12 Punkte, ≥ 2,4 m Abstand), Ein- und Ausgang zeigen in Strichrichtung, Kreuzungen werden automatisch angehoben.
- 02.11.: Riesenmeteor-Zerstörung als Implosion statt Explosion: Kamera weit raus (ganzer Planet), alles wird vom Einschlag aus in einen violett glühenden Kern gesogen, der zum Punkt schrumpft und mit Lichtblitz verlischt (4,2 s). Bleibende Objekte werden nach dem Neuaufbau zurückgesetzt.
- 02.11.: Browser-Admin nur für Fabian: Seite einmal mit `?admin=CODE` öffnen (Code nur als SHA-256 im Spiel), danach merkt sich der Browser das; Admin = Test-Knöpfe + eigener Test-Spielstand. `?admin=aus` oder Einstellungen → „Admin-Modus beenden“ schaltet ab. Nur im WebGL-Release.
- 02.11.: Implosion endet in einer Explosion: Lichtball, zwei Druckwellen-Ringe, 70 Trümmer, Funkenregen; Einsaugen als Strudel, Kern pulsiert immer schneller.
- 02.11.: Browser lief beim Riesenmeteor (voll ausgebaute Planeten) in „OOM“: WebGL-Speicher auf max. 4 GB, nach jedem Planetenaufbau und alle 3 min Resources.UnloadUnusedAssets, Implosions-Materialien werden zerstört.
- 02.11.: Aufgeräumt: 10 leere Platzhalter-Skripte (Cleaning/, Planets/, D3, OrbitCamera, PlanetGame, PlanetSceneBuilder), 14 unbenutzte Modelle (11 alte Bandstücke – Bänder baut das Spiel selbst –, erzader, deko_krater, pipeline_trasse) samt Einträgen im Blender-Skript und manifest.json, doppeltes Blender-Skript (tools/blender → nur noch unity/Modelle_Quelle). Bandwege-Cache neue Version (alte Wege ohne Merger verworfen). Behalten: die 26 neuen, noch nicht eingebauten Modelle (Einbau geplant).
- 02.11. (Build 42) – Claudes Verbesserungsliste, umgesetzt:
  - Riesenmeteor ohne Totalverlust: `Devastate(pid, Stärke)` – Krater (12–34 % der Parzellen, nächst der Basis, schwerer Meteoritenstaub), bis 70 % Fahrzeuge kaputt, alle Bohrtürme gestört, Lager −50 %, 10 min halbe Kraft, Terraforming −1/−2. Stärke: keine Abwehr 1, Schild versagt 0,85, nicht vor Ort 0,6, Abfangen verloren 0,55–1. Bild: Implosion → Explosion → Planet setzt sich wieder zusammen (`Reform`). `Ruin` bleibt nur für alte Spielstände.
  - Export-Frachter je Planet (ab Basisstufe 3, 250 k × 6^(Stufe−3)): nimmt Roh- und Produktlager über 90 % ab (bis 3 %/s), zahlt 40 % des Werts. Ampel bietet ihn im Stau an; volle Bohrtürme nur noch Gelb.
  - Gerettete Welt: Terraforming 5 = Moment „Planet gerettet“ (+25 Sternenstaub, Erklärung). Ruhemodus: Planet liefert dauerhaft sein Einkommen (10-min-Mittel), +5 % Preise überall je ruhendem Planet, keine Simulation, kein Verfall, kein Meteor; beim Besuch läuft er normal.
  - Ruhigerer Bildschirm: „+… entladen“ je Ort alle 2 s gebündelt; Meldungen gehen ins Protokoll (Ziele → Statistik); Nebensächliches (Stufen, Reparaturen, Info) nur, wenn weniger als zwei Meldungen sichtbar sind.
  - Seltene Funde (alle 7–15 min auf Planeten ≥ 50 % sauber mit ≥ 3 Fahrzeugen): Truhe unter Staubhügel, nur mit dem Finger freizuwischen (≈ 2,4 s), 2:30 min Zeit, Belohnung Credits (4–8 min Einkommen), Sternenstaub oder Spezialgüter.
  - Nicht umgesetzt: Wirtschaft entschärfen/Prestige – würde alle Spielstände umwerten; eigener Schritt mit Fabian.
- 03.11.: Parzellenrand umgedreht: nur dreckige Parzellen (und Meteoritenstaub) leuchten umrandet, saubere zeigen den Planeten ohne Rand. Nachtseite heller: Umgebungslicht nachts 55 % statt 34 % plus kühles Gegenlicht („Mondlicht“, 32 % der Sonne, ohne Schatten). Gegen Ruckeln auf großen Planeten: höchstens 2 Boden-Texturen pro Bild hochladen (je ≤ 4×/s), Schmutzsteinchen ab 110 m Höhe nicht zeichnen und ab 40 m ohne Schatten, kein Müll pro Bild beim Zeichnen, Parzellenzahl im HUD zwischengespeichert.
- 03.11.: Fahrzeuge weichen sich aus (nur Darstellung): Abstoßung bei Überlappung (Radius je Typ 0,9–1,7 m), seitlicher Versatz bis 3,2 m, stehende Fahrzeuge (Entladen, Bohrturm, Werkstatt …) bewegen sich nicht und schieben stärker.
- 03.11.: Schildkuppel über der ganzen Basis: Mitte = Schwerpunkt aller Basisparzellen, Radius = weiteste Basisparzelle + Rand (≥ 20 m); Generator steht möglichst mittig. Kuppel zart sichtbar (blau bereit, orange beim Nachladen, aus ohne Energie), heller bei anfliegendem Riesenmeteor, blitzt bei Abwehr.
- 03.11.: Shop neu (noch ohne echte Käufe): Langschläfer, Zeitsprung 4 h, Großer Frachtraum, Export-Lizenz (60 %), Meteor-Versicherung (halber Schaden, Lager bleiben), Sternenstaub-Beutel, Goldflotte (Aussehen), Unterstützer-Paket. Entfernt: Zusatz-Fahrzeugplatz, Saisonpass, Neon-Basis, Werbefrei (es gibt keine Werbung).
- 03.11.: Antippen leichter: Ampeln sind antippbar (Lampe, Mast, Fuß, 52 px) und öffnen das Gebäude; Flaggen 70 px statt 44 px und drei Trefferpunkte; Bäume ducken sich weg, wo gewischt wird (8 m) und über Parzellen mit Staub (40 %).
- 03.11.: Band-Editor: Endgriffe frei (Richtung UND Abstand vor der Wand, `d0/d1` im Spielstand, Default −1 = alt), kein fester 3,4-m-Stummel mehr, Rundung 1,5 m statt 2,2 m – der Weg folgt den Punkten genauer.
- 03.11.: Gebäude verschieben (beide Modi): Gebäude antippen → „Verschieben“, ziehen; nur im Basisgelände, nicht auf Wasser/Lava, ≥ 2 m Abstand zu gebauten Gebäuden. Gespeichert als `bMove` je Planet; Layout-Kopie je Planet, Bandwege und Fahrwege werden neu berechnet (Schlüssel enthält die Plätze), eigene Bandwege am Gebäude verfallen.
- 03.11.: Kombo-Melodien: jede Serie erzeugt eine neue kleine Melodie (A A' B A'', Pentatonik/Dorisch/Dur/Lydisch, zufällige Tonart) – selbst erzeugt, frei nutzbar.
- 03.11.: Hard Mode: eigener Spielstand (`…-hard`), freigeschaltet sobald im normalen Spiel alle Planeten frei sind (dauerhaft gemerkt). Keine Putzfahrzeuge, Drohnen, Bohrtürme, Werkstatt/Hangar/Lifter; je Planet ein kostenloser Sammelwagen (Behälter → Wagen → Station); Upgrades ohne Fahrzeuge/Drohnen/Tiefenabbau/Strahlenschutz/Außenlager/Plätze, „Sammelwagen“ sichtbar; alle Verkaufswerte ×3; Erste-Schritte-Ziele mit Fahrzeugen/Drohnen gelten als erfüllt.
- 03.11.: Shop „Hard Mode sofort“ (2,99 €): schaltet den Hard Mode ohne alle Planeten frei (im Gerät gemerkt, gilt für beide Spielstände); Test-Knopf „Hard Mode freischalten“.
- 03.11.: Bedenken eingearbeitet: (1) neues Gebäude, dessen Standardplatz ein verschobenes belegt, sucht automatisch den nächsten freien Platz; (2) Nachtseite nur über helleres, leicht kühles Umgebungslicht (62 % statt 34 %) – kein zweites Licht (Leistung am Handy); (3) gegen zähes Putzen im Hard Mode der Perk-Shop.
- 03.11.: Perk-Shop (Hard Mode, Credits): Putzbombe (bis 5 Parzellen, Preis 400 × Schwierigkeit × 2,6^(Stufe−1)), Putz-Nuke (bis 60 Parzellen, ×10), Buffs 90 s (Turbo-Wischen ×2,5 Kraft, Breiter Pinsel ×1,7, Magnet-Behälter ×3), Putzhelfer (×6, je weiterem ×1,6, max. 8 je Planet; laufen 2,2 m/s zur nächsten reinigbaren Parzelle, fegen 18 s × Größe × Härte, 60 % des Schmutzes ins Lager, auch offline). Perk-Leiste unten zwischen Behälter und Knöpfen; Werkzeug antippen, dann Stelle antippen.
- 03.11.: Sechs weitere Hard-Mode-Perks, witzig aber nachvollziehbar: Hochdruck-Kärcher (Schneise von 8 Parzellen ab der Basis), Schaumkanone (15 m Umkreis zu 70 % gelöst, Wischen bleibt Spielerarbeit), Staubsauger-Tornado (temporärer Helfer, 25 s, schnell), Espresso (Helfer ×3, 2 min), Goldener Mopp (Parzellenbonus ×3, 90 s), Rohrpost (Behälter alle 3 s ins Lager, 2 min). Tornado nutzt die Helfer-Simulation (`life > 0`), damit live und offline gleich laufen. Die Perk-Leiste zeigt nur, was man besitzt oder was gerade läuft (max. 6 + Shop).
- 03.11.: Perk-Preise gegen „100 000 Atombomben“: Grundpreis = 400 × Schwierigkeit × 1,35^Planetrang × 2,6^(Basisstufe−1), aber nie unter 2 min Einkommen × Perk-Faktor (Nuke ×10 = 20 min Einkommen; Einkommen geglättet: steigt in ~1 min, sinkt in ~30 min). Jeder Kauf auf einem Planeten +30 % (Nuke +80 %), Aufschlag sinkt um eine Stufe je 10 min (in der Simulation, also auch offline). Tragegrenze je Perk (Nuke 2, Tornado 3, Kärcher 5, Rest 4–10). Neue Felder `PlanetSave.perkHeat`, `SaveData.perkInc` mit Default.
- 03.11.: Fracht-Hinweis im Anfangsbereich (vor Basisstufe 2) prominent: Raketenknopf pulsiert golden mit „Laden!“ und lädt beim Antippen alle freigegebenen Waren, daneben eine Sprechblase; im Verkaufsfenster oben ein goldener Kasten „So bekommst du Geld“ mit „Alles laden“.
- 03.11.: Hard Mode steht von Anfang an im Startmenü: gesperrt mit Fortschritt (x/18 Planeten komplett sauber) und Info-Fenster mit Shop-Kauf. Freischalt-Bedingung geändert von „alle Planeten frei“ auf „alle Planeten komplett gesäubert“ (`S.done`); wer ihn schon frei hatte, behält ihn.
- 03.11.: Perk-Preis ohne Einkommens-Mindestpreis (Fabian: wurde ständig teurer, am Anfang nichts kaufbar) – nur noch Planet, Basisstufe, Aufschlag je Kauf (aufgerundet, bis er abgeklungen ist), Tragegrenze. Perks einsetzen: Tipp wird auf die nächste reinigbare Parzelle gelegt, Werkzeug bleibt scharf bis es wirkt, Bomben-/Nuke-Reichweite nach Parzellengröße.
- 03.11.: Hard Mode: Sammelshuttle (fliegt geradeaus, 1,4× Tempo) statt Sammelwagen; Zwischenlager (`PlanetSave.depot`, Platz per `DepotPos` nahe Rohstofflager) nimmt Ladung, wenn Stationen und Rohstofflager voll sind, gibt in der Simulation mit 20 E/s ans Rohstofflager ab; Meteoritenschauer im Hard Mode 3× seltener.
- 03.11.: Planetenkasse (Fabian: man trug zu viel Geld zum neuen Planeten): `S.credits` ist immer die Kasse des aktiven Planeten, die anderen liegen in `PlanetSave.cash` und werden beim Reisen getauscht (`SwapWallet`). Einnahmen während `StepPlanet` gehen in die Kasse des simulierten Planeten (`earnPid`). Von außen darf je Planet insgesamt höchstens `ImportCap` = max(25 000, 2× Freischaltpreis) kommen (Startbudget beim Freischalten oder „Zuschuss schicken“, gezählt in `cashIn`). Alte Spielstände (`walletV` 0): jeder andere Planet bekommt sein Startbudget aus der bisherigen Kasse, der Rest bleibt beim aktuellen Planeten.
- 03.11.: Betreuung gegen AFK: `PlanetSave.careT` zählt in der Simulation (live und offline) hoch; nach 30 min sinkt der Leistungsfaktor `CareF` linear bis 35 % nach 4,5 h und wirkt wie Energie auf Fahrzeuge, Bänder, Maschinen, Bohrtürme. Jedes Antippen oder Wischen (`Attend`) setzt den aktiven Planeten zurück. Ruhende, gerettete Planeten sind ausgenommen.
- 03.11.: Perks ohne Aufschlag je Kauf (auch Putzhelfer nicht mehr teurer je Helfer); Grundpreis gesenkt auf 150 × Schwierigkeit × 1,2^Planetrang × 2^(Basisstufe−1) – Mond am Anfang: Bombe 150, Nuke 1 500, Helfer 900. Es bleiben Tragegrenze und Helfer-Höchstzahl.
- 03.11.: Perks mit einem Tipp: Werkzeug wirkt sofort an der nächsten reinigbaren Parzelle beim Kamerablick (`CartGoal`), die Kamera fliegt zum Ziel. Eigene Effekte je Perk (`WorldView.PerkFx`, Ereignis `Game.OnPerk`), Buffs zeigen Banner und Leuchtrand. Kärcher läuft jetzt Nachbar für Nachbar von der Basis weg (trifft immer).
- 03.11.: Nuke als Ultimate: Faktor 60 statt 10 (Mond 9 000 Cr), Tragegrenze 1. Hard Mode: Freischaltpreis neuer Planeten ×0,4 (`UnlockCost`), weil man ohne Ziele zu wenig verdiente, um weiterzureisen.
- 03.11.: Perk-Shop sortiert: „Kleine Helfer“ (bis 1,6× Bombenpreis), „Große Helfer“, „Ultimate“ (Nuke) – je nach Preis aufsteigend.
- 03.11.: Hard Mode überarbeitet (`Game.HardPlus.cs`): Ultimate-Leiste (`S.ult`, +1/30 je selbst gewischter Parzelle, Kombo mehr; voll = Gratis-Nuke aus dem „Ulti“-Slot links in der Perk-Leiste). 20 Hard-Ziele (`S.hGoals`, je 3 offen, Geld × Schwierigkeit + Perk-Ladungen). Putz-Level aus Erfahrung (`S.hXp`, Bedarf 8+4·Level) mit festem Talentpfad (Pinsel, Behälter, Wischkraft, Helfer, Shuttle, Funde, Ultimate). Perk-Funde: 5 % je selbst gewischter Parzelle (Talent ×2), nur bis zur Tragegrenze, nie die Nuke. Schmutzwellen: nur live auf dem aktuellen Planeten, alle ~10–12 min, 30 s Vorwarnung mit markierter Parzelle; sind ihre dreckigen Nachbarn dann geputzt, ist die Welle gestoppt (+5 EP), sonst werden bis zu 3 saubere Grenzparzellen wieder staubig.
- 04.11.: Perk-Shop mit Reitern (Klein / Groß / Ultimate / Talente), Putz-Level als erste Zeile ganz oben mit nächstem Talent.
- 04.11.: Wischen direkter: Textur-Abschnitte mit Parzellen des laufenden Strichs (`Game.InStroke`) werden jedes Bild sofort hochgeladen, nur der Rest bleibt gedrosselt (2 je Bild, je 0,25 s). Kamera weicher: sichtbare Kamera gleitet der Soll-Position nach (kurze Wege schnell, Sprünge wie ein Flug), nach dem Loslassen läuft das Schieben mit Schwung aus. Perk-Leiste: Ulti + bis 4 Perks; bei mehr zeigt ein Knopf „Mehr +n“, der ein Feld mit den restlichen Perks nach oben aufklappt.
