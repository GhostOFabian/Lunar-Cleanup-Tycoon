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
