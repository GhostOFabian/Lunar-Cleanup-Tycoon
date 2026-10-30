# Lunar Cleanup Tycoon – Spielspezifikation (Stand Prototyp v6.0)

Mobile-3D-Spiel: Reinigungssimulation + Idle-Tycoon + Automatisierung + Planeten-Progression. Sprache der Oberfläche: Deutsch.

**Referenzimplementierung:** `prototype/index.html` (Browser-Prototyp, three.js). Alles in diesem Dokument ist dort spielbar umgesetzt und getestet. Bei Unklarheiten gilt das Verhalten des Prototyps; die Zahlen unten sind aus ihm übernommen. Die Unity-Umsetzung soll dieses Verhalten 1:1 nachbauen und darf es nur gezielt verbessern (Entscheidung in `docs/DECISIONS.md` festhalten).

---

## 1. Kern-Loop

1. Spieler wischt mit dem Finger über verschmutzte Parzellen → Material landet im Rohstofflager.
2. Material verkaufen oder veredeln (Presse → Schmelze/Raffinerie) → Credits.
3. Credits → Basisstufen, Gebäude, Fahrzeuge, Upgrades, Forschung.
4. Fahrzeuge reinigen selbstständig von der sauberen Fläche nach außen, Drohnen und Außenlager verkürzen ihre Wege.
5. Ist ein Planet zu 80 % sauber, wird der nächste erreichbar. Der alte Planet putzt sich mit der aufgebauten Automatik weiter (auch offline).
6. Langzeit: tägliche Aufträge, Funde, Planeten-Abschlussboni, Neustart mit Sternenstaub (Prestige).

## 2. Planet und Parzellen

- Jeder Planet ist ein **Goldberg-Polyeder** (Dual eines geodätischen Ikosaeders mit Frequenz `n`): `N = 10·n² + 2` Zellen, davon immer **12 Fünfecke**, Rest Sechsecke. Radius `R = sqrt(N·140 / 4π)` → jede Zelle hat ~140 m².
- Jede Zelle hat einen lokalen gnomonischen Rahmen (`c`, `e1`, `e2`), ein 32×32-Raster (`inside`-Maske) für die Schmutzmenge und eine 256²-Maskentextur für die Optik.
- **Basis** = alle Zellen mit Ring ≤ 2 um die Basiszelle (19 Zellen). Ring 3 = Startregion (70 % Starttyp). Die **Startparzelle** ist die Ring-3-Zelle in Richtung `base.c − 0,5·e2`.
- Zellarten (`KINDS`): `normal`, `rich` (Ring ≥ 4, 6 %: 2,5× Material + Rohstoffbonus beim Abschluss), `industrial` (Ring ≥ 5, Rauschfeld: 3× Material, Härte ×1,6, nur Sauger/Truck), `anomaly` (Ring ≥ 6, 9 %: braucht Forschungsstation, Abschluss +10 % Forschungstempo dauerhaft).
- Verschmutzungstypen werden per Rauschfunktion nach Planet-Anteilen verteilt (Tabelle Abschnitt 12).
- Render: 20 Chunks (je Ikosaederfläche), Texturatlas für Schmutzarten; ab Kamerahöhe `R·1,15` Planetenkarte (Vertexfarben statt Textur).

### 2.1 Frontier – keine Freischaltung
- Parzellen kosten nichts und müssen nicht freigeschaltet werden.
- **Reinigbar** ist jede nicht saubere Nicht-Basis-Zelle, die an eine saubere Zelle oder die Basis grenzt (Forschungsgebiete zusätzlich nur mit Forschungsstation). Man kann nicht irgendwo anfangen.
- Eine Parzelle wird erst beim ersten Wisch oder bei der ersten Fahrzeugzuweisung „aktiviert“ (Raster + Maske angelegt).
- Fertig, sobald < 3 % Schmutz übrig sind → dauerhaft sauber, einmalige Abschlussprämie `Ertrag × Materialwert × 0,3 × Prämien-Upgrade × Planetenfaktor`.

### 2.2 Rahmen und Feedback
- Jede Parzelle hat einen farbigen Rand (leicht nach innen versetzt, damit Nachbarn beide sichtbar sind):
  grün = sauber · gelb = in Arbeit oder leicht neu verstaubt · rot = verschmutzt und reinigbar oder > 25 % neu verstaubt · dunkelrot = noch nicht erreichbar · blau = Basis.
- Beim Abschluss leuchtet die Parzelle ~1,5 s grün pulsierend auf, dazu Funken, Ton, Toast, schwebender Prämientext.

## 3. Manuelle Reinigung
- Wischen mit einem Finger auf einer reinigbaren Parzelle reinigt; daneben = Kamera drehen. Zwei Finger = Zoom/Drehen.
- **Ein Wisch reinigt immer nur eine Parzelle** – die, auf der er begonnen hat. Erst wenn sie fertig ist, wechselt der Wisch auf die Parzelle unter dem Finger.
- Kein Leistungsdeckel für das Handgerät (ausdrücklicher Wunsch). Stärke: Radius `0,95 m × (1 + 0,15·Stufe)`, Kraft `1 + 0,3·Stufe`, pro Stempel `k = 1 − exp(−0,35·Kraft·Sternenstaubbonus / Härte)`.
- Material geht in den **Sammelbehälter** des Spielers (60 E × 1,5^Stufe, Upgrade „Größerer Sammelbehälter“, max. 12). Ist er voll, stoppt das Wischen mit Hinweis. Entladen per Knopf unten links (Trichter mit dem meisten Platz, sonst Rohstofflager) oder gezielt im Fenster einer Entladestation bzw. des Rohstofflagers.
- Je 30 Einheiten per Hand: 12 % Chance auf einen Fund (Abschnitt 11).
- Verlässt der Finger die gesperrte Parzelle (Rand + 0,6 × Radius), pausiert der Saugkopf (roter Ring), die Kamera bleibt stehen. Nur in den ersten 0,3 s ohne Ertrag wird der Wisch zur Kamerabewegung. Randscrollen 200 px/s erst nach 0,45 s am Rand (äußere 14 %, max. 70 px) und nur über der eigenen Parzelle.
- Arbeitslicht: ab Dämmerung Punktlicht 3,2 m über dem Saugkopf und weicher Lichtfleck (4,2 × Radius).

## 4. Meteoritenstaub (Neuverschmutzung)
- Ab 3 sauberen Parzellen (Staubfaktor = Planet- und Sturmfaktor, Intervall siehe unten).
- Ein Schauer (ab v4.7) trifft ein ganzes Feld: die Zielparzelle plus Nachbarn, 2–4 Parzellen (auf komplett sauberen Planeten 4–7). Je Parzelle 11–15 Einschläge (Radius 3,4–5,2 m, Stärke 0,90–0,98) → meist 80–95 % bedeckt; höchstens 8 Einschläge je Parzelle sind sichtbare Meteore. Parzellen, die schon > 50 % bedeckt sind, werden übersprungen.
- Abstand `max(45 s, 1000/√saubere) / Gefahrfaktor`, auf komplett sauberen Planeten `max(25 s, 520/√saubere)`. Material (ab v5.2, `meteorType`): 45 % Startschmutz, sonst einer der übrigen Schmutztypen des Planeten nach Gewicht, gespeichert als `act[id].t`. Staubmenge je voll bedeckter Parzelle 0,8 × Schmutzmenge (×1,6 auf fertigen Planeten).
- Staub-Parzellen bleiben „sauber“ (Fortschritt zählt), geben 80 % des Starttyp-Ertrags (auf fertigen Planeten 128 %) und werden von Fahrzeugen mitgereinigt (Malus +20/+60 m im Zielscore, max. 2 Fahrzeuge).

## 5. Wirtschaft
- Materialwert = `Basiswert × Händler-Upgrade × (Qualität bei Produkten) × (Raumhafen 1,2) × Planet-Ertragsfaktor × (1 + 0,1·komplette Planeten) × (1 + 0,05·Sternenstaub) × (1 + 0,02·Artefakte)`.
- Rohstoffe: Staub 1, Metallschrott 2, Kristallsplitter 6, Chemieschlamm 4. Produkte: Staub-Brick 14, Metall-Brick 26, Metallbarren 340, Reinkristall 42, Polymer 30.
- Materialmengen sind auf allen Planeten gleich; nur der Wert skaliert mit dem Planet-Ertragsfaktor.

## 6. Basis, Gebäude, Stufen
- Jeder Planet hat seine **eigene Basis**. Technik (Gebäude, Fahrzeuge, Upgrade-Kategorien) ist global: Was auf irgendeinem Planeten freigeschaltet wurde (`S.maxStage`), kann überall gebaut werden. Gebäudekosten × Planetfaktor.
- Basisstufen pro Planet (Kosten × Planetfaktor): 1 frei · 2 = 200 · 3 = 5 000 · 4 = 40 000 · 5 = 220 000. Die Stufe pro Planet gibt Fahrzeugplätze (Stufe 4 +4, Stufe 5 +6).
- Gebäude (Plan-Koordinaten relativ zur Basiszelle, Footprint-Radius `fp`; Abstand zwischen Gebäuden ≥ 2 m; alles liegt auf allen 10 Planeten auf Basiszellen – geprüft):

| Gebäude | Stufe | Kosten | Pos | fp | Funktion |
|---|---|---|---|---|---|
| Hauptstation | 1 | – | -2,16 | 3,6 | 3 Energie/s, Garage |
| Rohstofflager | 1 | – | 4,4 | 3,4 | Rohstoffe |
| Verkaufsstation | 1 | – | -20,2 | 3,3 | Rakete + Frachtraum |
| Produktlager | 2 | – | -9,0 | 2,9 | Produkte |
| Kleine Entladestation | 2 | 120 | 16,9 | 2,6 | 1 Dock, Puffer 60, 5 E/s, nur S |
| Staubpresse | 2 | 300 | 4,-7 | 2,4 | 10 Staub/Metall → 1 Brick |
| Energiezentrale | 2 | 250 | 10,18 | 4,3 | +8 Energie/s, doppelter Speicher |
| Roboterwerkstatt | 3 | 1 500 | 13,-21 | 3,4 | +4 Fahrzeugplätze, Spawn |
| Industrielle Entladestation | 3 | 2 200 | 16,0 | 2,9 | 2 Docks, 250, 14 E/s, S+M |
| Schmelzanlage | 3 | 3 500 | -9,-11 | 2,2 | 10 Metall-Bricks → 1 Barren |
| Forschungsstation | 3 | 2 500 | -14,14 | 2,9 | Forschung |
| Zentrale Entladeanlage | 4 | 12 000 | 16,-10 | 3,4 | 3 Docks, 900, 40 E/s, alle |
| Hochleistungsraffinerie | 4 | 14 000 | 4,-18 | 2,4 | Kristall/Chemie → Reinkristall/Polymer (Forschung) |
| Drohnenhangar | 4 | 20 000 | -9,22 | 3 | Drohnen (Forschung) |
| Raumhafen | 5 | 60 000 | -21,-12 | 3,4 | +20 % Preise überall |

- Docks: `x+2,6`, Abstand 1,8 m; Anfahrpunkt `x+6,4`. Parkplätze `[19 + (i%4)·1,8, 13 + ⌊i/4⌋·1,8]`. Spawn: Werkstatt `[13,-16,2]`, sonst `[2,5,13]`.
- Energie: Speicher 150 (+150), Produktion `(3 + 8) × Solar-Upgrade × Solarfaktor × 2,6 × Tagesfaktor` (siehe 9c); Bedarf × Planet-Energiefaktor. Reicht sie nicht, laufen alle Verbraucher anteilig langsamer.

## 7. Fahrzeuge
| Typ | Stufe | Kosten | m/s | Radius | Kraft | Tank | Energie | Größe |
|---|---|---|---|---|---|---|---|---|
| Staubi | 2 | 150 | 3,2 | 0,8 | 1,4 | 15 | 0,3 | S |
| Reinigungsbuggy | 2 | 180 | 4,4 | 1,0 | 1,8 | 40 | 0,5 | S |
| Industriesauger | 3 | 2 400 | 3,6 | 1,6 | 3,8 | 150 | 1,4 | M |
| Schwerer Truck | 4 | 14 000 | 2,5 | 2,2 | 6,5 | 520 | 3 | L |

- **Spezialfahrzeuge (ab v3.7)** – erst kaufbar, wenn der genannte Planet erreicht ist; auf ihren Heimatplaneten ×2,2 Reinigungskraft und sturmfest (kein Sturm-Tempoabzug):

| Typ | Ab Planet | Stufe | Kosten | m/s | Radius | Kraft | Tank | Energie | Größe | Heimat |
|---|---|---|---|---|---|---|---|---|---|---|
| Eisfräse | Europa | 3 | 4 000 | 3,4 | 1,8 | 5 | 220 | 1,8 | M | Europa, Enceladus, Triton, Pluto |
| Schlammraupe | Titan | 4 | 8 000 | 3 | 2 | 7 | 320 | 2,4 | M | Titan, Venus, Enceladus |
| Lavapanzer | Io | 4 | 16 000 | 2,8 | 2,4 | 11 | 600 | 4 | L | Io, Venus |
| Magnetgleiter | Ganymed | 5 | 25 000 | 7 | 2,2 | 13 | 480 | 3,5 | L | Ganymed, Triton, Europa |
| Kryo-Koloss | Triton | 5 | 40 000 | 2,6 | 3 | 20 | 1 400 | 6 | L | Triton, Pluto |

- Beliebig viele pro Typ, solange Plätze frei sind. Plätze = 4 (ab Technikstufe 2) + Werkstatt 4 + Stufe 4: 4 + Stufe 5: 6 + Fahrzeughalle 2/Stufe + 1 pro kompletter Planet. Kosten × Planetfaktor × 1,22^Anzahl dieses Typs.
- Reinigung pro Tick `k = 1 − exp(−Kraft·2,6·Doppelbürsten·Leistungsfaktor·Sternenstaub·Energie·dt / Härte)`. (Faktor 2,6 = Buff, damit Fahrzeuge mit Upgrades klar besser sind als die Hand.) Gemessen: Buggy ohne Upgrades ≈ 0,64 m²/s inkl. Entladefahrten.
- **Leistungsanforderung pro Planet** (Doppelbürsten-Stufe): Mond 0, Mars 1, Europa 3, Titan 4, Io 5, Venus 6, Ganymed 8, Enceladus 9, Triton 10, Pluto 12. Darunter reinigen Fahrzeuge nur mit 30 % (Warnung im HUD und in der Planetenliste).
- Zustandsmaschine: `seek → drive → clean → toUnload → queue → unload` (+ `wait` auf Drohne). Zielwahl: nächste aktive oder Frontier-Parzelle passender Stufe (Industrieflächen nur Sauger/Truck), max. 3 Fahrzeuge pro Parzelle, reiche Parzellen bevorzugt. Manuelle Zuweisung möglich.
- **Teamarbeit:** Mehrere Fahrzeuge auf einer Parzelle teilen sie in Winkelsektoren, +10 % Reinigung je weiterem Fahrzeug (max. 4). Bei > 35 % Restschmutz zieht eine belegte Parzelle Helfer an (harte Parzellen stärker), sonst wird sie gemieden. Industrieflächen erlauben 4 Fahrzeuge.
- **Akkus:** Staubi 110 s, Buggy 140 s, Industriesauger 220 s, Magnetgleiter 300 s Laufzeit × „Größere Akkus“ (+30 %/Stufe, max. 15). Unter 15 % fahren sie zum nächsten Andockplatz, entladen und laden dort (1/16 Kapazität pro s × Entladetempo, 2,5× Verbrauch aus dem Netz). Unterwegs ziehen sie keinen Netzstrom. Leer: 30 % Tempo/Leistung.
- **Wegfindung:** In der Basis (Radius 31 m) fahren Fahrzeuge per Raster-A* (66×66, 1 m, 8-Nachbarn, String-Pulling) um Gebäude (fp + 0,9 m) und alle Förderbänder (1,15 m) herum. Draußen geradeaus, mit Ausweichpunkt um Außenlager. Raster wird bei jeder Gebäudeänderung neu gebaut, Pfade gecacht.
- Entladen: nächste passende Station oder Außenlager (Außenlager mit > 90 % Puffer werden gemieden).

### 7a. Sammelwagen (ab v4.1)
- Pro Planet einer, ab Basisstufe 1, Kosten `120 × diff^0,85`, belegt keinen Fahrzeugplatz, kein Strom. Ladung `2 × Tragkraft des Sammelbehälters`, Tempo `7 × (1 + 0,15 × Stufe)` m/s (Upgrade „Schneller Sammelwagen“, global).
- Zustände `park → go → wait → back → drop → park`. Auslöser: Sammelbehälter ≥ 95 % voll (oder „Zu mir rufen“) und der Spieler ist auf dem Planeten. Ziel: letzte Wischstelle (sonst Kameramitte); entfernt sich der Finger > 8 m, fährt er hinterher. Ankunft < 3,2 m.
- Im Zustand `wait` leert der Behälter-Knopf alles in den Wagen (zählt als Entladen). Danach fährt er zur Annahme der Entladestation mit dem meisten Platz (sonst Rohstofflager), entlädt 60 E/s, weicht bei vollem Ziel nach 3 s aus und parkt an der Basis. Offline fährt er nur zurück und entlädt.

## 8. Logistik und Produktion
- Förderbänder entstehen automatisch, sobald beide Gebäude stehen. Ihre Enden reichen bis in die Gebäude (≤ halber Footprint-Radius vom Gebäudemittelpunkt, notfalls mit Knick; vom Layout-Test geprüft). An jedem Anschluss steht ein offenes Übergabetor (1,05 m über dem Band), unter dem die Waren durchfahren. Bänder zum selben Ziel rechnen ihre Ware unterwegs gemeinsam gegen den freien Platz. Sie laufen als Item-Listen `[Position, Material, Menge]`, Abstand 1,25 m, Tempo 2,4 m/s × Upgrade, Paket 8 E × Upgrade (Produkte 1 Stück pro 8 E).
- Kurven sind abgerundet (Radius 1,4 m, quadratische Bézier-Kurven). Hochbänder (h > 1) sind Brücken mit Stützen, die nicht auf anderen Bändern stehen.
- Bänder: Stationen → Rohstofflager, Zentrale Entladeanlage → Presse, Rohstofflager → Presse/Raffinerie, Presse → Schmelze/Produktlager, Schmelze/Raffinerie → Produktlager, **Rohstofflager → Verkauf, Produktlager → Verkauf** (ab Technikstufe 2).
- **Stau-Vermeidung (Pull-Prinzip):** Ein Band nimmt nur so viel auf, wie am Ziel nach Abzug der Ware unterwegs noch Platz ist → Bänder blockieren sich nie gegenseitig.
- **Überlauf:** Ab 90 % Füllung von Rohstoff- oder Produktlager geht der Überschuss automatisch zur Verkaufsrakete, auch für Waren mit „Band aus“.
- Maschinen: Presse (Energie 1,5; Eingang 60, Ausgang 8; 10 Staub → Staub-Brick 2 s, 10 Metall → Metall-Brick 2,5 s), Schmelze (4; 20/3; 10 Metall-Bricks → Barren 8 s), Raffinerie (5; 40/6; 5 Kristall → Reinkristall, 5 Chemie → Polymer, je 4 s). Produktionsplan schaltet Rezepte an oder aus.

### 8.0 Förderband-Fenster (ab v5.0)
- Antippen eines Bandes zeigt neben Status und Durchsatz „Gerade auf dem Band“ (Ware, Menge/Stück, Wert, live) und „Befördert“ (welche Waren dieses Band nach den aktuellen Regeln transportiert).

### 8.1 Verkauf per Rakete
- **Wertschöpfungskette (ab v5.0):** Rohwaren gehen nur in den Frachtraum, wenn auf diesem Planeten keine Maschine sie veredelt (`rawRoute`: Staub/Metall → Presse gebaut und im Plan an; Kristall/Chemie → Raffinerie gebaut und an). Sonst laufen sie Presse → (Schmelze) bzw. Raffinerie, und nur das Endprodukt (`endGood`: Staub-Brick, Barren bzw. Metall-Brick ohne Schmelze, Reinkristall, Polymer) kommt in den Frachtraum. Sicherheitsventil: Rohstofflager ≥ 97 % → Überschuss geht unveredelt mit. Produkte haben weiter „Band an/aus“, Rohwaren zeigen nur „wird X“ / „zur Rakete“. Frachtraum 120 E × 1,45^Stufe.
- **Frachtauftrag:** Jede Rakete sucht eines der Endprodukte des Planeten (aus seinen Rohwaren über `endGood`, +60 % auf diese Ware); nach jedem Start ein neuer, nie zweimal derselbe. **Mischfracht:** +8 % je weiterer Warensorte, max. +40 %. Ist der Auftrag eine Rohware ohne Maschine, bedient das Rohstoffband ihn bevorzugt (Schalter „Frachtauftrag automatisch bedienen“).
- **Die Rakete startet erst, wenn der Frachtraum voll ist** (≥ 98 %), verkauft alles und ist 12 s / Raketentempo unterwegs. Knopf „Jetzt starten“ für früher. Direktverkauf per Hand bleibt möglich.
- Verkaufsautomat (Stufe 3, Schalter): Lager ≥ 90 % → kompletter Inhalt wird sofort verkauft (Notfallschutz).

### 8.2 Produktions-Ampel
- HUD-Chip: grün „Produktion läuft“, gelb „Engpass: …“, rot „Stau: …“ mit Ort und Grund. Tippen öffnet die Produktion.
- Bewertet werden Energie, jede Station (Puffer > 97 % rot, > 70 % gelb; Ursache: Band zu langsam oder Rohstofflager voll), Außenlager-Pipelines, Rohstoff- und Produktlager (> 97 % / > 85 %), Maschinen (keine Energie, Ausgang voll, Eingang > 90 %), Rakete (Frachtraum voll und Rakete unterwegs) und Bänder (Stau oder voll belegt).
- Das Produktionsfenster zeigt ganz oben alle gelben und roten Stellen mit Erklärung und bis zu 5 passende Upgrades zum direkten Kaufen. Jedes Produktionsgebäude hat eine Lampe (grün/gelb/rot, grau = wartet).

## 9. Drohnen und Außenlager
- **Drohnen** (Forschung „Drohnentechnik“ + Drohnenhangar, Stufe 4): max. 8 pro Planet, Kosten `2 500 × Planetfaktor × 1,35^n`, Ladung 60 × Upgrade, 9 m/s × Upgrade, Umladen 30 E/s. Ziel: Fahrzeuge > 30 m vom Hangar mit ≥ 40 % Tank (Score = Füllgrad × Entfernung). Volles Fahrzeug mit zugewiesener Drohne wartet („Wartet auf Drohne“). Die Drohne liefert an die Entladestation mit dem meisten freien Platz (`toSt → stdrop`, ab v5.3), nur wenn alle voll sind über den Hangar ins Rohstofflager. Flughöhe 12 m, beim Abholen 3,2 m, beim Abgeben 6,5 m. Energie 0,4/s im Flug.
- **Außenlager** (Forschung „Außenlager-Logistik“, nach Drohnen; Technikstufe 4): auf sauberer Parzelle ab Ring 4, nicht neben einem anderen Außenlager, max. 4 pro Planet, Kosten `20 000 × Planetfaktor × 1,6^n`. 2 Docks, Puffer 400, 20 E/s, alle Fahrzeuggrößen, Pipeline 8 E/s × Bandtempo ins Rohstofflager (sichtbare Röhre, über der Basis erhöht).

- Volle Ziele (ab v5.9): Außenlager > 97 % werden nicht gewählt; angedockt und 4 s kein Platz (außer beim Laden des Akkus) bzw. 8 s ohne Andockplatz → anderes Ziel, das alte wird 40 s bzw. 25 s gemieden. Pipeline vom Silo des Außenlagers (lokal x = −0,9, 3,7 m) zum Silo des Rohstofflagers.
- Pipeline-Leistung (ab v5.8): `8 E/s × Stärkere Pumpen (1 + 0,5 × Stufe)` je Außenlager, unabhängig vom Bandtempo; Puffer über „Größere Trichter“.

## 9a. Lastdrohnen (Schwerlast-Hangar, ab v3.7)
- Forschung „Schwerlastflug“ (nach Außenlager-Logistik), Gebäude Schwerlast-Hangar (Stufe 5, 90 000, Pos 2,24), max. 3 Lastdrohnen (`30 000 × Planetfaktor × 1,6^n`), 12 m/s × Drohnentempo, 2 Energie/s im Flug.
- Wählt ein M- oder L-Fahrzeug eine Parzelle > 45 m entfernt, holt eine freie Lastdrohne es ab und fliegt es direkt hin (Zustände `liftwait` → `lifted` → `drive`). Kleine Fahrzeuge fahren selbst.

## 9b. Energie (ab v3.7)
- Solar (Hauptstation 3 + Energiezentrale 8) × Solarfelder × Sonnenfaktor – leidet unter Planet und Sturm.
- Radioisotopen-Generator (Forschung, Stufe 3, 6 000, Pos -7,5,10): +10/s konstant.
- Geothermie-Kraftwerk (Stufe 4, 25 000, Pos -21,5,10,5): +30/s × Erdwärme (Mond 0 – nicht baubar, Mars 0,4, Europa 0,8, Titan 0,6, Io 2, Venus 1,5, Ganymed 0,6, Enceladus 1,6, Triton 1, Pluto 0,4).
- Kernreaktor (Forschung „Kernspaltung“ nach Radioisotopen, Stufe 5, 150 000, Pos -2,-23): +150/s und +1000 Speicher – Gegenmittel für wenig Sonne und hohen Energiebedarf der äußeren Planeten.
- Akkupark (Upgrade pro Planet): +250 Speicher pro Stufe, max. 10.

## 9c. Tag und Nacht (ab v4.0)
- Jeder Planet hat eine eigene Tageslänge in Sekunden (`DAYLEN`): Mond 480, Mars 420, Europa 540, Titan 600, Io 360, Venus 720, Ganymed 540, Enceladus 420, Triton 600, Pluto 720. Uhr `sp.dayT` pro Planet, läuft in `simStep` (also auch offline); neue Planeten starten am Vormittag (8 % der Tageslänge).
- Sonnenwinkel θ = 2π · Phase, Tagesfaktor `clamp((sin θ + 0,08) / 1,08, 0, 1)^0,8` – erste Hälfte Tag, zweite Hälfte Nacht. Spitze Solar = 2,6 × alter Wert, Tagesmittel ≈ 0,92 × alter Wert.
- Nachts: Solar 0. Strom kommt aus dem Speicher (Akkupark), Radioisotopen-Generator, Geothermie und Kernreaktor; Akku-Fahrzeuge fahren aus dem Akku. Die Ampel nennt bei Strommangel in der Nacht die Nacht als Ursache und schlägt Akkupark vor.
- Optik: Sonne kommt aus der echten Richtung über der Basis, Licht wird nachts gedimmt (Hemisphäre 34 %, kein Sonnenlicht). Fahrzeuge schalten Scheinwerfer, Lichtkegel und Lichtfleck am Boden ein; die Basis hat ein warmes Flutlicht. Straßenlaternen (`lampSpots`): je Gebäude 1 (fp < 2,6) bzw. 2 Laternen als symmetrisches Paar zur Basismitte hin (Versatz ±0,62 rad, sonst weiter nach hinten), Abstand fp + 0,12–0,3 m, ≥ 1,9 m von Bändern – im für Fahrzeuge gesperrten Ring, nicht auf der Andockseite (Osten) der Entladestationen, ≥ 1,5 m von Bändern, ≥ 2,2 m von Parkplätzen und Pipeline-Anschluss, ≥ 4 m zueinander, nur auf Basiszellen. Alle glühen (Kopf, Lichthof, Lichtfleck); die 6 Laternen nächst der Kamera tragen ein echtes Punktlicht (Reichweite 16 m). HUD-Balken heißt „Tag“/„Nacht“, das Energiefenster zeigt die Zeit bis Sonnenunter-/-aufgang.

## 9d. Tiefenabbau (ab v4.4)
- Freigeschaltet pro Planet, sobald er komplett sauber ist (`S.done[pid]`). Bohrtürme auf sauberen Parzellen ab Ring 2, nicht auf Außenlagern, eine pro Parzelle, max. `2 + 2 × Basisstufe`.
- Kosten `3 000 × diff × 1,45^n`. Förderung `0,8 E/s × Vorkommen × Diamantbohrköpfe (1 + 0,3·Stufe, lokal)`, Vorkommen fest pro Parzelle (Hash) 0,6–1,6, Material = 75 % ursprünglicher Schmutz der Parzelle, 25 % gleichmäßig die übrigen Rohstoffe des Planeten (ab v5.2). „Bestes Vorkommen“ ×3 für Materialien ohne Bohrturm, sonst ÷ (1 + Anzahl). Eigenes Lager 300 E (Silo + Andockplatz bei lokal x = 2,7 m). Fahrzeuge (`toRig → rigload → toUnload`) holen ab: sofort, wenn ein Lager ≥ 70 % voll ist, sonst wenn sie nichts zu reinigen haben (ab 15 E); ein Fahrzeug pro Bohrturm, Laden 16 E/s × Schnellentladung. Strom 1,5/s × Maschinen-Effizienz, solange gebohrt wird.
- Basis-Fenster „Tiefenabbau“: Übersicht + „Bohrturm auf dem besten Vorkommen“ (Vorkommen / (1 + 0,05 × Ring)); Parzellenfenster zeigt Vorkommen und baut gezielt. Fahrzeuge umfahren Bohrtürme wie Außenlager. Ampel: Bohrturm-Lager voll rot, ≥ 70 % gelb (Abhilfe: mehr/größere/schnellere Fahrzeuge).

## 9e. Ausbaustufen (ab v4.8)
- Ab Basisstufe 5 und 100 % sauber: Ausbaustufe n+1 (`sp.exp`), unbegrenzt. Kosten `400 000 × diff × 2,2^n` Cr und `4 000 × 2,2^n` E Tiefenförderung auf diesem Planeten seit der letzten Stufe (`sp.deep`, wird beim Ausbau auf 0 gesetzt).
- Wirkung je Stufe: +2 Bohrtürme, Bohrleistung × (1 + 0,25 n), Roh- und Produktlager × (1 + 0,3 n), +1 Fahrzeugplatz, Maximalstufe aller lokalen Anlagen-Upgrades + 2 n.
- Anzeige im Basis-Fenster an Stelle von „Höchste Stufe erreicht“ mit Fortschrittsbalken.

## 9f. Reisen (ab v4.8)
- Nahtlos ohne Ladebild: Phase 1 (1,0 s) Kamera blendet in eine Gesamtansicht (3,4 R, Blick auf die Mitte), der Planet gleitet seitlich weg; Wechsel; Phase 2 (1,25 s) neuer Planet gleitet herein, Kamera blendet in die normale Ansicht (Höhe für 30 m). Eingaben sind währenddessen gesperrt. Beim Wechsel: `renderer.initTexture` + `renderer.compile` vorab, 2 Bilder Pause ohne Animationsfortschritt, Himmel blendet über, Hintergrund-Simulation und Speichern warten bis nach der Ankunft.

## 9g. Gebäudestufen, Bohrturm-Stufen, Erzbagger, Planetenfarben (ab v5.4)
- Gebäudestufe (`sp.blvl[id]`, max. 3 + Ausbaustufe): verfügbar, wenn alle zugehörigen Upgrades auf Maximum sind (`BLV_REQ`: Presse = Hydraulik-Tuning + Größere Presskammer; Schmelze/Raffinerie = Hydraulik-Tuning + Größere Puffer; Stationen = Schnellentladung + Größere Trichter; Lager/Frachtraum = Kapazitäts-Upgrade; Energiezentrale = Solar + Akkupark). Kosten `max(4 000, 15 × Baukosten) × diff × 3^Stufe`. Wirkung `blF = 1 + 0,5/0,4/0,3 × Stufe` auf Tempo und Puffer bzw. Kapazität bzw. Solar und Speicher. Modell: Höhe ×(1 + 0,2 n); Breite ×min(1 + 0,07 n, Platz) – Platz = Abstand zu Nachbargebäuden (−0,5 m), fremden Bändern (−0,9 m) und eigenen Übergabetoren (`growRoom`, aus der Modell-Bounding-Box); Mast mit n leuchtenden Markern auf dem Dach.
- Hochdruckpresse (`pspeed`) ab v5.6 entfernt und erstattet – Tempo kommt nur aus Hydraulik-Tuning (alle Maschinen), Menge aus der Größeren Presskammer (nur Presse). Darstellung: ein Hub alle 1,6 s mit „+N Bricks“, egal wie schnell die Presse rechnet.
- Größere Presskammer (`pvol`, lokal, ab v5.5): bis zu `1 + Stufe` Chargen pro Hub (max. 8), nur so viele wie Material und Ausgangsplatz erlauben; Ein- und Ausgangspuffer der Presse × Chargenzahl.
- Bohrturm-Stufe `r.lv` 0–5: +30 % je Stufe, Kosten `2 500 × diff × 1,9^lv`. Auf Stufe 5 Umbau zum Erzbagger (`r.bag`, `60 000 × diff`): ×2,5 Förderung, alle Planetenrohstoffe zu gleichen Teilen, Lager 600 E, räumt Meteoritenstaub auf eigener und Nachbarparzellen (6 %/s).
- Planetenfarben `PCOL[pid]` = [Akzent, Zweitfarbe]: Akzent ersetzt Gelb an Gebäuden und Fahrzeugen, Weiß wird 16 % zum Akzent getönt, Industriesauger und Sammelwagen in der Zweitfarbe, Spezialfahrzeuge 35 % zum Akzent gemischt.

## 9h. Ausrichtung, Betriebsbonus, Werksaufträge (ab v6.0)
Siehe `docs/LANGZEIT.md`, Abschnitt 4 (Zahlen und Formeln).

## 10. Gefahren pro Planet
- Siehe Tabelle Abschnitt 12. Stürme kommen periodisch (`every`, `dur`) und multiplizieren Fahrzeug-, Band- und Maschinentempo, Solarstrom und Staubrate.
- **Warnung 30 s vorher** (Toast + HUD-Countdown). **Sturmschild** (Forschung): pro Planet einschaltbar, schwächt Sturmeffekte um 80 % (Solar ausgenommen), kostet 4 Energie/s während des Sturms, sichtbar als Kuppel über der Basis. Gefahren-Fenster über den HUD-Chip oder die Energieansicht.

## 11. Langzeitsysteme
- **Planet komplett (100 %)**: dauerhaft +10 % Preise und +1 Fahrzeugplatz überall.
- **Nächster Planet erst ab 80 %** Reinigung des vorherigen (keine Forschung mehr nötig). Freischaltkosten siehe Tabelle.
- **Funde**: beim Abschluss einer Parzelle 7 % (Forschungsgebiet 30 %), per Hand alle 30 E 12 %. Satellitenkern (60 %): Credits `120 × Planetfaktor × Stufe × (1 + 0,5·Stufe)`. Datenkern (28 %): +30 % der laufenden Forschung (sonst 60 % Credits). Alien-Artefakt (12 %): dauerhaft +2 % Preise, +5 % Forschung.
- **Tägliche Aufträge**: 3 pro Kalendertag (seeded), Typen: Material verkaufen, Parzellen fertig reinigen, m² reinigen, Credits verdienen; Ziel und Belohnung skalieren mit Technikstufe und Planetfaktor. Alle 3 = +50 % Bonus. Zählt über alle Planeten und offline. Badge am Auftragsknopf.
- **Statistik**: Gesamtwerte, pro Planet Einnahmen/h, Parzellen/h (gleitender Mittelwert, alle 10 s gemessen), Prognose für 80 % und 100 %.
- **Sternenstaub (Prestige)**: ab Mars möglich. Gewinn `⌊sqrt(Verdienst dieses Laufs / 200 000)⌋ + 3 × komplette Planeten`. Jeder Sternenstaub: +5 % Preise, +3 % Reinigung (Hand und Fahrzeuge). Zurückgesetzt wird alles außer Sternenstaub, Artefakten, Funden, Lebenszeit-Statistik und Toneinstellung.
- **Ziele/Quests** (44, Reihenfolge im Prototyp): Tutorial (inkl. Sammelwagen kaufen) → Stufe 2 → Entladestation → Buggy → Presse → Bricks → Stufe 3 → Schmelze → Barren → Drohnentechnik → Hangar + Drohne → 80 % Mond → Mars → Außenlager → 80 % Mars und Europa → Akkupark 3 → Radioisotopen-Generator → Raffinerie → Eisfräse → Sturmschild → Titan → Schlammraupe → Geothermie → Io → Lavapanzer → Kernspaltung → Kernreaktor → Lastdrohne → Venus → ein Planet komplett → Ganymed → Magnetgleiter → Enceladus → Triton → Kryo-Koloss → Pluto → Pluto 80 % → Neustart mit Sternenstaub.
- **Erfolge** (17, `ACH`, ab v4.0): Stufenziele für Parzellen, Verdienst (Lebenszeit), Raketenstarts, Fahrzeuge, Planeten erreicht/komplett, Bricks, Barren, Hand-m², Meteoritenschauer, Drohnen-Abholungen, eingeflogene Maschinen, Tagesaufträge, Forschung, Funde, Basisstufe, Sternenstaub. Belohnung Stufe n (ab 0): `250 × 5^n` Cr, von Hand abholen. Stand `S.ach` und Zähler `S.ctr` bleiben über den Neustart erhalten.
- **Planetenziele** (ab v5.1, `planetGoals(pid)`): pro Planet 23–24 Ziele, Stand `sp.pg`, Zähler `sp.nRockets`, `sp.nDust`. Reihenfolge: 10 Parzellen, 3 Fahrzeuge, 25 %, Stufe 3, 10 Raketen, 10 Staubparzellen, 50 %, Spezialfahrzeug des Planeten (falls vorhanden), 8 Fahrzeuge, Energie (Kernreaktor bei Energiefaktor ≥ 1,5, sonst Radioisotopen-Generator), Stufe 5, Verkauf 50 000 × diff Cr, 80 %, 100 Raketen, 100 %, 3 Bohrtürme, 10 000 E Tiefenförderung, Ausbaustufen 1/2/3/5/8/12/20. Belohnung `250 × diff × Gewicht` (1 … 1 200), von Hand abholen.
- **Ziel-Übersicht** (Tippen auf den Ziel-Chip): aktuelles Ziel mit „Hinführen“, alle Erfolge (abholbare zuerst), Liste aller Hauptziele. Der Chip zeigt, wie viele Erfolge bereit sind.

## 12. Planeten

| Planet | Parzellen | Ertrag × | Energie × | Freischalten | Vpower-Stufe | Gefahr | Sturm |
|---|---|---|---|---|---|---|---|
| Mond | 252 | 1 | 1 | – | 0 | Meteoritenschauer | – |
| Mars | 492 | 2,2 | 1,5 | 25 000 | 1 | Staubstürme (Staub 1,3) | alle 240 s, 45 s: Fahrzeuge 0,6, Solar 0,5, Staub 3 |
| Europa | 1 002 | 5 | 1,8 | 140 000 | 3 | Kälte und Eisstürme | 300/40: Bänder 0,5, Fahrzeuge 0,8, Staub 2 |
| Titan | 1 442 | 10 | 1,6 | 450 000 | 4 | Säureregen (Solar 0,7) | 260/50: Maschinen 0,5, Staub 2,5 |
| Io | 1 692 | 22 | 1,7 | 1,4 Mio | 5 | Vulkanausbrüche (Staub 1,6) | 200/30: Staub 6, Solar 0,7 |
| Venus | 1 962 | 45 | 2,4 | 4,2 Mio | 6 | Glühende Hitze (Solar 1,5) | 280/45: Fahrzeuge 0,55, Maschinen 0,8 |
| Ganymed | 2 252 | 90 | 1,8 | 12 Mio | 8 | Magnetstürme | 220/50: Bänder 0,3, Maschinen 0,7 |
| Enceladus | 2 562 | 180 | 2 | 34 Mio | 9 | Geysire (Solar 0,6, Staub 2,5) | 180/30: Staub 5 |
| Triton | 2 892 | 360 | 2,2 | 96 Mio | 10 | Kryostürme (Solar 0,5) | 200/60: Fahrzeuge 0,5, Bänder 0,7 |
| Pluto | 3 242 | 700 | 2,4 | 270 Mio | 12 | Ewige Dämmerung (Solar 0,35, Staub 1,4) | 300/45: Fahrzeuge 0,7, Staub 3 |

Verschmutzung (Anteil · Material · Menge pro Parzelle · Härte):
- Mond: Mondstaub 40 % (Staub 120, 1) · Metallschrott 30 % (Metall 100, 1,8) · Alte Satelliten 16 % (Metall 160, 2,6) · Kristallablagerungen 14 % (Kristall 60, 3,4)
- Mars: Roter Staub 45 % (130, 1,6) · Verrostete Maschinen 35 % (Metall 110, 2,6) · Industrierückstände 20 % (Chemie 75, 3,6)
- Europa: Eisablagerungen 45 % (Kristall 40, 2,2) · Gefrorener Weltraumschrott 35 % (Metall 100, 3) · Eiskristalle 20 % (Kristall 70, 3,8)
- Titan: Methanschlamm 45 % (Chemie 60, 2,4) · Organische Ablagerungen 30 % (Chemie 90, 3,6) · Industrieabfall 25 % (Metall 120, 3,2)
- Io: Lavagestein 45 % (Staub 120, 2,2) · Vulkanschrott 30 % (Metall 95, 3,4) · Schwefelkrusten 25 % (Kristall 35, 2,6)
- Venus: Basaltstaub 45 % (130, 2,4) · Säureschlamm 30 % (Chemie 55, 2,8) · Hitzeschrott 25 % (Metall 95, 3,6)
- Ganymed: Eisstaub 45 % (130, 2,4) · Wrackteile 33 % (Metall 90, 3,4) · Kristalladern 22 % (Kristall 30, 4)
- Enceladus: Salzkrusten 45 % (130, 2,4) · Sondenschrott 33 % (Metall 90, 3,6) · Geysireis 22 % (Kristall 30, 3)
- Triton: Stickstoffeis 40 % (Kristall 30, 3,2) · Kryoschlamm 33 % (Chemie 60, 3,4) · Alte Kolonie 27 % (Metall 90, 3,8)
- Pluto: Tholinstaub 45 % (130, 2,8) · Expeditionsschrott 30 % (Metall 90, 4) · Methaneis 25 % (Kristall 30, 3,6)

Jeder Planet hat ein eigenes Farbthema (Himmel, Licht, Boden, Staub, Sterne, Atmosphärenschimmer, großer Begleiter am Himmel: Erde, Phobos, Jupiter, Saturn, Neptun, Charon).

## 13. Upgrades
Global (gelten überall): Handgerät (Radius 10, Kraft 10), Fahrzeuge (Tempo 20 → ×4, Doppelbürsten 30 → ×8,5, Tanks 20 → ×7, Sparmotoren 10, Düsen 15 → ×2,8), Drohnen (Ladung 8, Tempo 8), Rechenzentrum 6, Fahrzeughalle 15 (+30 Plätze), Händler 10 (×2), Abschlussprämie 8 (×3).
**Pro Planet** (beginnen auf jedem Planeten bei 0, Kosten × Planetfaktor): Entladestationen (Tempo, Trichter, Docks +2), Förderbänder (Tempo ×3,5, Paletten ×3,4, Effizienz), Maschinen (Tempo ×3, Puffer ×4,2, Effizienz, Qualität ×1,64), Lager (Rohstoff 150 E × 1,5^Stufe, Produkt 40 × 1,5^Stufe), Solarfelder ×4,5, Verkauf (Frachtraum, Raketentempo).
Kosten `Basis × Wachstum^Stufe` (genaue Werte im Prototyp, `UPG`).

## 14. Forschung (global, eine gleichzeitig, läuft offline)
Effiziente Motoren 2 000/60 s · Langzeitbetrieb I (12 h offline) 4 000/120 s · Kristallchemie (Raffinerie) 6 000/150 s · Sturmschild 15 000/240 s · Drohnentechnik 30 000/300 s · Außenlager-Logistik 90 000/420 s (nach Drohnen) · Langzeitbetrieb II (24 h) 25 000/300 s (nach I). Tempo × Rechenzentrum × (1 + Forschungsboni).

## 15. Idle und Offline (Pflicht)
- **Eine Simulation** `simStep(planet, dt, live)` für live und offline. Offline läuft dieselbe Funktion in 1-s-Schritten für alle Planeten (Bänder in 0,25-s-Teilschritten). Deckel 8 h (12/24 h mit Forschung).
- Hintergrundplaneten laufen auch live weiter (gesammelt im 1-s-Takt).
- Nach der Rückkehr: Zusammenfassung pro Planet (m², fertige Parzellen, Material, Verkäufe, Raketen, Stürme, Schauer, Drohnenflüge, Funde, Planet komplett, volle Lager, Energieknappheit) und sichtbar veränderte Masken (`syncMask`).
- Uhr-Manipulation: negative Zeit = 0, Deckel greift.
- Ab v4.9: Offline-Nachrechnung in Scheiben (≤ 30 ms pro Scheibe, weiterhin 1-s-Schritte) mit Fortschrittsanzeige ab 15 min Abwesenheit – beim Start, nach Cloud-Übernahme und nach Rückkehr in den Tab (Lücke > 5 s). Gedrosselte Bilder (Fenster im Hintergrund, langsames Gerät): verlorene Zeit bis 5 s wird sofort in 1-s-Schritten nachgerechnet. Gespeichert wird beim Verstecken, beim Fensterwechsel (blur) und beim Schließen; `lastActive` berücksichtigt eine noch laufende Nachrechnung. Der Idle-Test in den Einstellungen ist entfernt.

## 16. Speichern
- JSON (Prototyp: localStorage `lunar-cleanup-tycoon-v2`, `S.v=2`, `S.layout=3`, Migrationsflags `locMig`). Pro Planet: saubere/bezahlte Zellen als Base64-Bytes, aktive Zellen als Raster (Base64) + 128²-Masken-Alpha + Staubflag, Fahrzeuge, Drohnen, Außenlager, Stationen, Maschinen, Bänder, Lager, lokale Upgrades.
- Seit v3.6 werden nur Raster gespeichert (Masken werden aus dem Raster rekonstruiert), dazu `…-bak` alle 5 min, Kopie unlesbarer Stände, Cloud-Stand im Claude-Konto (neuerer Stand gewinnt, ein frischer Stand überschreibt nie einen vorhandenen) und Export/Import-Code `LCT1:` (gzip + Base64).
- Unity: `Application.persistentDataPath`, gleiche Struktur als DTOs mit `saveVersion` und Migrationspfad; Cloud-Sync und Code-Export beibehalten.

## 17. Mobile UI
- Herausgezoomt (Kamerahöhe 17 → 30 eingeblendet) trägt jedes Fahrzeug und der Sammelwagen einen orange blinkenden Leuchtpunkt (Größe mitskaliert), damit man die Flotte schnell findet.
- HUD oben: Credits + Rate, Planet-Chip (Fortschritt, Stufe), Aufträge-Knopf mit Badge, Einstellungen; Energie/Roh/Prod-Balken; Materialliste; Ziel-Chip; Sturm-Chip; Produktions-Ampel; Warn-Chip.
- Navigation unten: Reinigen/Kamera, Fahrzeuge, Produktion, Basis, Upgrades, Planeten. Menüs als Bottom-Sheet.
- Planetenkarte beim Herauszoomen mit **einklappbarem** Infofenster (Zustand gespeichert).
- UI-Touches dürfen nie die Welt reinigen.

## 18. Abnahme
Alles oben muss spielbar funktionieren; Headless-Regressionstests des Prototyps (`prototype/tests`) sind die Referenz für das erwartete Verhalten.
