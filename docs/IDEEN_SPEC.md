# Ausarbeitung der Ideen (Vorbereitung für Unity) – 31.10.2026

Zu jeder Idee aus `IDEEN.md`: Regeln mit Zahlen, Spielstand-Felder, Oberfläche, betroffene Dateien, Aufwand.
Grundregeln bleiben: eine Simulation für live und offline (`Game.SimStep`), neue Spielstand-Felder nur mit Standardwert,
Texte auf Deutsch in Du-Form, keine Knöpfe ohne Funktion.

Vorbereitet im Code (noch nicht übersetzt, noch nicht angebunden):
- `Core/FeatureData.cs` – alle Zahlen und Tabellen unten als Daten (Ereignisse, Funde, Kombo, Tagesbonus, Fahrzeug-Stufen, Sternenstaub-Baum, Wetter, Saisonplanet, Automatik-Regeln, Benachrichtigungen).
- `Core/SaveData.cs` – Felder für alle Ideen mit Standardwerten (alte Spielstände laden unverändert).

Reihenfolge-Vorschlag: A1 → A2 → B1 → B2 → C1 → D1 → … (siehe Ende).

---

## A. Sichtbarkeit und Gefühl

### A1 ★ Waren sichtbar (zuerst – behebt auch „Produkte nicht auf den Bändern“)
- **Bänder**: Jedes Paket aus `ps.belts` wird als Kiste/Brick/Barren/Fass auf der sichtbaren Bandlinie gezeichnet (`WorldView.DrawItems`). Zuerst den Fehler suchen (Logik oder Darstellung), dann:
- **Lager-Füllstand**: Rohstofflager-Silos und Produktlager zeigen den Füllstand (Silos: Füllring/Höhe, Produktlager: Kistenstapel 0–12 je nach `ProdSum/ProdCap`).
- **Rakete beladen**: Kisten stapeln sich an der Rampe je nach `sellBuf / SellCap`; beim Start verschwinden sie.
- Aufwand: klein–mittel. Dateien: `WorldView.cs`, `Kit.cs` (Stapel-Modelle).

### A2 Wetter je Planet
| Planet | Effekt | Darstellung |
|---|---|---|
| Mars | Staubteufel | 1–3 wandernde Staubwirbel (Partikel-Säule), heben etwas Staub auf saubere Parzellen (+2 % Schmutz, nur Optik bis zur Meteor-Regel) |
| Europa | Eisnebel | Bodennebel (Partikel, flach) morgens/abends |
| Io | Lavafontänen | Fontänen an Lavafeldern, kurze Glut |
| Venus | Blitze | Blitz + Aufhellen bei Sturm (Sturm gibt es schon) |
| Titan | Methanregen | feiner Regen, Pfützen-Glanz |
| alle anderen | Staubschleier/Sternschnuppen | leicht |
- Nur Optik, keine Regeländerung. Aufwand: mittel. Datei: neue `Gameplay/Weather.cs`, Daten `FeatureData.Weather`.

### A3 Kleine Lebenszeichen
- 2–6 Astronauten (einfache Figuren) laufen zwischen Gebäuden der Basis auf festen Wegen (nicht über Bänder). Drohnen mit Blinklicht (rot/grün). Fahrzeuge hupen (Ton) bei Stau.
- Aufwand: mittel. Dateien: `WorldView` (neu `WorldView.Crew.cs`), `Kit.Astronaut()`.

### A4 Sound und Musik
- Prozedural wie im Browser (`SFX` im Prototyp): Wischen, Parzelle fertig, Kasse, Raketenstart, Kristall.
- Musik in Schichten: Grundton immer, + Takt, wenn > 50 % Produktion läuft, + Melodie bei Ereignis/Kombo.
- Einstellungen: Ton an/aus (gibt es), Musik an/aus, Lautstärke.
- Aufwand: mittel. Neuer Ordner `Audio/` (laut CLAUDE.md vorgesehen).

### A5 Vibration (Handy)
- Kurz (20 ms): Parzelle fertig, Kristall geerntet. Mittel (40 ms): Rakete startet, Ziel erreicht. Abschaltbar.
- `Handheld.Vibrate()` ist zu lang → Android: `VibrationEffect` über JNI, iOS: `UIImpactFeedbackGenerator` über kleines Plugin. Im Browser: `navigator.vibrate`.
- Aufwand: klein (Android/Web), mittel (iOS).

---

## B. Kurzfristige Ziele

### B1 ★ Ereignisse auf dem Planeten
- Alle 6–12 Minuten Spielzeit (zufällig, nur wenn kein Ereignis läuft, nicht offline):
| Ereignis | Dauer | Wirkung | Belohnung |
|---|---|---|---|
| **Wrack-Absturz** | 8 min | Eine saubere Parzelle (Ring ≥ 3) wird zu einer Wrack-Parzelle: Schmutzart „Wrackteile“, 3× Menge | ×3 Erlös dieser Parzelle, Bonus 500 × Planetenstufe Cr, wenn rechtzeitig sauber |
| **Kometenschweif** | 3 min | 6–10 Leuchtkristalle wachsen sofort reif auf sauberen Parzellen | Kristalle (wie bisher) |
| **Sonnensturm** | 5 min | Solarstrom ×2, Fahrzeuge −25 % Tempo | – |
| **Erzader** | 10 min | Ein Bohrturm/Bagger fördert ×2 | – |
| **Frachter in Not** | 5 min | Rakete zahlt +50 % für den nächsten vollen Start | – |
- Hinweis oben (Toast + Symbol), Tipp → Kamera fliegt hin. Offline laufen Ereignisse nicht an, laufende enden normal.
- Spielstand: `PlanetSave.ev` (EventSave: key, t, cell, done) und `SaveData.evNext`.
- Aufwand: mittel. Dateien: `Core/Game.cs` (EventStep im SimStep, nur live), `Hud` (Symbol), `FeatureData.Events`.

### B2 ★ Schatzfunde und Sammelalbum
- Beim Fertigreinigen einer Parzelle: Chance 3 % (anomale Parzellen 8 %) auf einen Fund aus dem Album des Planeten (6 Funde pro Planet, je ein Name + kurze Beschreibung, z. B. Mond: „Apollo-Schraube“, „Alte Flagge“, „Luna-Funkgerät“…).
- Doppelte Funde geben 250 Cr × Planetenstufe. Album komplett: dauerhaft +5 % Erlös auf diesem Planeten.
- Fenster „Funde“ (in Ziele oder eigenes): Raster mit Silhouetten, gefundene in Farbe.
- Spielstand: `SaveData.finds` (Liste „pid:index“).
- Aufwand: klein–mittel. Dateien: `Game.CellDone()`, `Sheets.Finds`, `FeatureData.Finds`.

### B3 Kombo beim Wischen
- Jede selbst fertig gewischte Parzelle innerhalb von 20 s nach der letzten: Kombo +1 (max 10). Prämie der Parzelle × (1 + 0,1 × Kombo). Anzeige „Kombo ×3“ am Pinsel, verfällt nach 20 s.
- Nur Handreinigung, nur live. Spielstand: nichts (flüchtig).
- Aufwand: klein. Dateien: `Game.Stroke/CellDone`, `Hud`.

### B4 Tagesbonus (7-Tage-Serie)
- Einmal pro Kalendertag (lokale Zeit) beim Öffnen: Belohnung Tag 1–7: 1, 2, 3, 4, 6, 8, 12 × (Erlös pro Minute, mind. 100 Cr); Tag 7 zusätzlich 3 Leuchtkristalle. Ein ausgelassener Tag setzt auf Tag 1 zurück (1 Tag Kulanz).
- Spielstand: `SaveData.daily` (lastDay als yyyymmdd, streak).
- Aufwand: klein. Dateien: `Game.Load`, `Hud` (Fenster beim Start), `FeatureData.Daily`.

---

## C. Mittelfristiger Fortschritt

### C1 ★ Forschungsbaum
- Die 10 bestehenden Projekte bleiben, werden aber auf 4 Äste verteilt (Reinigung, Logistik, Energie, Raumfahrt) und um 8 neue ergänzt (z. B. „Kombo-Sensor“ +5 s Kombozeit, „Fundscanner“ +2 % Fundchance, „Ereignisradar“ Ereignisse 20 % häufiger, „Schnellfrachter“).
- Voraussetzungen als Linien; Entscheidung: auf jeder Ebene 1 von 2 „Spezialisierungen“ (umschaltbar gegen Kosten).
- Spielstand: bleibt `resDone`; neu `SaveData.resSpec` (Liste „ast:ebene:wahl“).
- Aufwand: mittel–groß (Oberfläche!). Dateien: `Sheets.Research` → Baum-Ansicht.

### C2 Fahrzeuge: Erfahrung, Spezialisierung, Name, Lackierung
- Erfahrung: +1 je gereinigtem m² Anteil; Stufen bei 500/2.000/6.000/15.000 → je +4 % Tempo und Saugkraft.
- Ab Stufe 2 eine Spezialisierung: **Schnell** (+15 % Tempo, −5 % Tank), **Robust** (−40 % Verschleiß), **Sparsam** (−30 % Energie/Akku).
- Name (frei, max. 16 Zeichen) und Lackierung (8 Farben; „Goldflotte“ aus dem Shop).
- Spielstand: `VehSave.xp, spec, name, skin`.
- Aufwand: mittel. Dateien: `Game` (Faktoren), `Sheets.Vehicles`, `Models.Vehicle` (Farbe).

### C3 Basis selbst gestalten
- Bauen-Modus: Gebäude auf freie Basisplätze (Basiszellen) ziehen; erlaubt nur ohne Überlappung (Layout-Test-Regeln: ≥ 2 m Abstand). Bänder werden mit dem Unity-Router neu verlegt, Logiklänge = verlegte Länge.
- Achtung: Bandlängen bestimmen Transportzeit → Logik nutzt dann die neue Länge (ändert Spielbalance minimal). Spielstand: `PlanetSave.layoutOverride` (id → x,y,yaw).
- Aufwand: groß.

### C4 Handelsrouten zwischen Planeten
- Ab Basisstufe 5 auf zwei Planeten: Frachter-Route A → B für ein Produkt (z. B. Eis von Europa nach Io: Kühlung, Maschinen +20 % Tempo). 1 Route je 2 Planeten, Frachter fliegt alle 4 min (sichtbar als Rakete, die abhebt/landet).
- Spielstand: `SaveData.routes` (from, to, mat, t).
- Aufwand: groß.

### C5 Raumstation im Orbit
- Eigenes Ziel nach Planet 3: Station mit Modulen (Dock, Werft, Labor, Lager), je 5 Stufen; Boni planetenübergreifend (Werft: Fahrzeuge −10 % Preis, Labor: Forschung −15 % Zeit …). Sichtbar am Himmel jedes Planeten.
- Spielstand: `SaveData.station` (Liste Modul/Stufe).
- Aufwand: mittel–groß.

---

## D. Langzeit

### D1 ★ Sternenstaub-Neustart mit Bonus-Baum
- Wie im Prototyp geplant: Ein komplett sauberer Planet gibt Sternenstaub; Neustart (Planeten zurück, Credits weg, Funde/Erfolge/Kosmetik bleiben) gegen dauerhafte Boni.
- Baum mit 12 Knoten (z. B. „Startkapital“ 1.000/5.000/25.000 Cr, „Schnellstart“ Basisstufe 2 sofort, „Sammler“ +10 % Fundchance, „Ewige Flotte“ 1 Buggy gratis, „Glanz“ goldene Parzellenränder).
- Spielstand: `SaveData.stardust, sdSpent, sdNodes, resets`.
- Aufwand: groß (mit Hauptzielen 44–57 verknüpft).

### D2 Saisonplaneten
- Alle 4–6 Wochen ein Sonderplanet (eigene Schmutzart, z. B. „Kürbis-Mond“ im Oktober, „Eis-Komet“ im Dezember), 252 Parzellen, eigene Belohnung (Kosmetik + Sternenstaub). Endet nach der Saison; der Fortschritt verfällt, die Belohnung bleibt.
- Rangliste über Play Spiele / Game Center (E3).
- Spielstand: `SaveData.season` (key, clean, claimed).
- Aufwand: mittel (Daten), groß mit Rangliste.

### D3 Foto-Modus und Erfolge mit Titeln
- Foto-Modus: Oberfläche aus, freie Kamera, Tag/Nacht-Regler, Filter; Speichern/Teilen (Handy: Galerie/Teilen-Dialog).
- Erfolge (30 Stück, z. B. „1.000 Parzellen“, „Erster Barren“, „Album Mond komplett“) mit Titeln, die im Profil stehen.
- Spielstand: `SaveData.achievements`, `title`.
- Aufwand: mittel.

### D4 Sauberer Planet erwacht
- Bei 100 %: über 60 s wachsen Gewächshauskuppeln, Wohnmodule, Lichter und kleiner Verkehr (Fahrzeuge ohne Aufgabe fahren Runden). Rein optisch, zeigt „geschafft“.
- Aufwand: mittel. Datei: `WorldView.Settlement.cs`.

---

## E. Gemeinsam (später, braucht Konto-Dienste)
- E1 Freundesbasen besuchen (Spielstand-Schnappschuss ansehen, „Gefällt mir“ = kleiner Bonus für beide).
- E2 Gemeinsame Ziele aller Spieler (Server nötig – Unity Gaming Services oder eigener Dienst).
- E3 Ranglisten über Google Play Spiele / Game Center (Platzhalter `Platform.cs` erweitern).

## F. Komfort
- F1 **Benachrichtigungen** (lokal): „Rohstofflager voll“, „Rakete bereit“, „Kristalle reif“, „Ereignis läuft“, „Tagesbonus wartet“. Je einzeln abschaltbar. Unity „Mobile Notifications“-Paket (kostenlos, nur nach Rückfrage installieren).
- F2 **Automatik-Regeln** (ab Basisstufe 4): bis zu 5 Regeln „Wenn … dann …“ aus Bausteinen:
  - Wenn: Produktlager > X %, Rohstofflager > X %, Fahrzeug-Zustand < X %, Nacht, Sturm.
  - Dann: Verkaufen (Produkt), Fahrzeug zur Werkstatt, Maschine an/aus, Band an/aus.
  - Spielstand: `PlanetSave.rules`.
- F3 **Schnellreise-Leiste**: oben ausklappbar, alle Planeten mit Sauberkeit und Ampel, 1 Tipp = Reise.

---

## Reihenfolge und Aufwand (Vorschlag)
| # | Idee | Aufwand | Warum jetzt |
|---|---|---|---|
| 1 | A1 Waren sichtbar | klein–mittel | behebt Fehler, sofort spürbar |
| 2 | B3 Kombo | klein | mehr Spaß beim Wischen |
| 3 | B2 Funde + Album | klein–mittel | Sammeln motiviert lange |
| 4 | B1 Ereignisse | mittel | Abwechslung |
| 5 | B4 Tagesbonus | klein | Grund zum Wiederkommen |
| 6 | A5 Vibration, A4 Sound | klein/mittel | Gefühl |
| 7 | C2 Fahrzeug-Stufen | mittel | Bindung an die Flotte |
| 8 | A2 Wetter, A3 Astronauten | mittel | Atmosphäre |
| 9 | D1 Sternenstaub | groß | Langzeit |
| 10 | C1 Forschungsbaum | mittel–groß | Entscheidungen |
| 11 | F1–F3 Komfort | klein–mittel | Handy-Version |
| 12 | D3, D4 | mittel | Belohnung |
| 13 | C5, C4, C3 | groß | später |
| 14 | D2, E1–E3 | groß | mit Konto-Diensten |
