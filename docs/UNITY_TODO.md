# Unity-Version – Was noch zu tun ist

Stand: 30.10.2026 · Unity-Version 0.9 · Branch `claude/prototype-v3.5`
Vorbild ist immer der Browser-Prototyp (`prototype/index.html`). Diese Liste wird nach jedem Teil abgehakt.

## Fertig (in Unity getestet, im Editor)
- [x] 0.1 Kernschleife: runde Planeten, Wischen, Behälter, Lager, Verkauf, Basisstufen, Gebäude, Fahrzeuge, Bänder, Presse/Schmelze/Raffinerie, Rakete, Planetenreise, Offline, Speichern
- [x] 0.2 Energie, Akkus, Tag und Nacht mit Nachtbeleuchtung
- [x] 0.3 Forschung (alle 10 Projekte)
- [x] 0.4 Drohnen und Drohnenhangar
- [x] 0.5 Stürme, Sturmschild, sichtbares Wetter, Leistungsanforderung pro Planet
- [x] 0.6 Außenlager mit Pipeline
- [x] 0.7 Lastdrohnen (Schwerlast-Hangar)
- [x] 0.8 Fahrzeugverschleiß, Werkstatt, Totalschaden, Strahlung
- [x] 0.9 Tiefenabbau: Bohrtürme, Stufen 0–5, Erzbagger, Störungen, Planet-Abschluss (+10 % Preis, +1 Fahrzeugplatz)
- [x] Testmodus im Editor (Menü **Lunar Cleanup → Testmodus**): eigener Test-Spielstand, Test-Tasten F5–F10 nur dort

## Als Nächstes (in dieser Reihenfolge)
1. [ ] **Meteoritenschauer** – Meteoritenstaub auf sauberen Parzellen (gemischtes Material), Erzbagger räumt ihn in der Nähe weg, Ziel „Räume Parzellen nach Meteoritenschauern“
2. [ ] **Ausbaustufen** nach Basisstufe 5 (Tiefenförderung als Voraussetzung): +2 Bohrtürme, +Förderung, größere Außenlager, mehr Fahrzeugplätze
3. [ ] **Sternenstaub-Neustart** (Prestige) + Artefakte → schaltet die Hauptziele 44–57 frei
4. [ ] **Frachtaufträge der Rakete** (+60 % für das Auftragsprodukt, Mischfracht bis +40 %), „Band an/aus“ je Produkt, Rohwaren-Routing wie im Browser
5. [ ] **Verkaufsautomat** (Basisstufe 3) und **Produktionsplan** (Rezepte an/aus)
6. [ ] **Produktions-Ampel** im HUD und im Produktionsfenster (Ursache + passende Upgrades, Gebäudelampen)
7. [ ] **Gebäudestufen** (Gebäude wachsen, wenn ihre Upgrades ausgereizt sind; +50 %/+40 %/+30 %)
8. [ ] **Ausrichtung pro Planet** (Bergbau/Industrie/Handel), **Betriebsbonus** bis +30 %, **Werksaufträge** mit Ruf
9. [ ] **Planetenziele** (23–24 eigene Ziele je Planet) und **Tägliche Aufträge**
10. [ ] **Funde, Erfolge, Statistik-Fenster** mit Prognosen
11. [ ] Feinheiten der Fahrzeuge: Teamarbeit auf großen Parzellen (+10 % je Partner), volle Stationen meiden, Arbeitsbereich festlegen, Kollision vermeiden
12. [ ] **Sound** (prozedural wie im Browser) und Vibration
13. [ ] **Spielstand-Code** exportieren/importieren (Übernahme aus dem Browser)

## Für eine echte Handy-Version
14. [ ] Android-Build einrichten (Build Settings, Package Name, Icon, Splash) und auf dem Handy testen: Touch, Pinch-Zoom, Safe Area, Bildrate, Akku
15. [ ] Leistung: Planeten mit 3 000+ Parzellen auf schwachen Handys prüfen (Texturgröße, Schatten, Lichter)
16. [ ] Grafik-Feinschliff: Gebäudemodelle, Fahrzeug-Scheinwerfer, Raketenstart, Übergänge beim Reisen
17. [ ] iOS-Build (braucht einen Mac)

## Offene Kleinigkeiten
- [ ] Mausrad-Zoom im Editor reagiert bei meinen Tests nicht (auf dem Handy zoomt man mit zwei Fingern; prüfen, ob es nur an den simulierten Mausrad-Ereignissen liegt)
- [ ] Hauptziel 1 zählt nur die Startparzelle – wie im Browser, aber eventuell freundlicher machen
- [ ] Im Browser-Prototyp fehlt noch der Fix „Rückseite des Planeten“ für die Wegfindung (in Unity behoben)

## So geht es weiter
Sag einfach „mach mit Punkt X weiter“ (oder „mach weiter“ für den nächsten offenen Punkt). Jeder Teil wird einzeln gebaut, in Unity getestet, gesichert (Git) und hier abgehakt.
