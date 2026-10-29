# Browser-Prototyp

Eine einzige Datei: `index.html` (three.js r128 von cdnjs, Schriften von Google Fonts – Internet nötig).

## Starten
- Datei im Browser öffnen, oder lokal servieren: `python -m http.server` im Ordner `prototype/` und `http://localhost:8000` aufrufen (am Handy im selben WLAN über die IP des Rechners).
- Spielstand: `localStorage` (`lunar-cleanup-tycoon-v2`). Löschen unter Einstellungen → Spielstand löschen.
- Offline-Zeit testen: Tab schließen oder in den Hintergrund legen und später zurückkommen (Tests rufen `runOffline`/`startOffline` direkt auf).

## Steuerung
Wischen auf einer reinigbaren Parzelle = reinigen (eine Parzelle pro Wisch) · ein Finger daneben = Planet drehen · zwei Finger = zoomen/drehen · tippen = Infos zu Parzelle, Gebäude, Fahrzeug, Band · weit herauszoomen = Planetenkarte.

## Tests
Siehe `tests/` und `CLAUDE.md` (Abschnitt „Prototyp – prüfen“). Die Tests laufen headless mit einem three.js-Mock: sie prüfen die komplette Spiellogik, aber nicht die Grafik.
