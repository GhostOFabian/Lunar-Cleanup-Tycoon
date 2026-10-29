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
- Meteoritenstaub selten, aber 8–11 Einschläge mit 50–90 % Bedeckung – „seltener, aber dann richtig“.
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
- Ein Wisch, der die gesperrte Parzelle (Rand + 0,6 × Saugradius) oder den Planeten verlässt, wird ohne Absetzen zur Kamerabewegung – sonst „klebt“ der Finger.
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
