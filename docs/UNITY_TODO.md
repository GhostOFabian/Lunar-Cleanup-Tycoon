# Unity-Version – Was noch zu tun ist

Stand: 30.10.2026 · Unity-Version 0.12 · Branch `claude/prototype-v3.5`
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
- [x] 0.10 Wünsche vom 30.10.: Bänder (Banden genau am Band, höher, enden an der Gebäudewand), Pipeline unterirdisch, schlauere Fahrzeuge (Werkstatt, nie dumm herumstehen), Planet in alle Richtungen drehen, Kenney-Modelle (CC0) für Gebäude/Fahrzeuge/Drohnen/Rakete/Deko, Upgrade-Fenster behält die Scrollposition, Planeten werden immer größer (252 → 5.762 Parzellen; alte Stände behalten ihre Größe), Seen aus Wasser/Lava/Säure/Methan mit Umwegen und Schwebereiniger, Hügel und Krater
- [x] 0.10.1 Bandanschlüsse an der echten Modellwand, Ladeterminal an der Rakete, neues Rohstofflager und Außenlager
- [x] 0.11 Basis-Prüfung (F11 im Testmodus, alle 18 Planeten + alte Größen, Bericht + Fotos in `BasisCheck/`), Brücken und Weichen für Bänder, glattere Kurven, Raketenlandung von oben, neue Entladestationen mit Abkippgruben, Fahrzeuge verteilen sich und lösen sich, wenn sie hängen, Leuchtkristalle (5 Farben, wachsen nach, Antippen oder Fahrzeuge), Weltraum-Himmel je Planet mit farbigem Sternlicht, Konto-Speicher- und Shop-Platzhalter, Zoom per Tastatur (R/F)
- [x] 0.12 Bänder rechtwinklig aus geraden Flächen mit Y-Zusammenführungen, Pipeline unterirdisch, Test-Knopf „Alles maximal ausbauen“ (F12), Fahrzeuge sammeln Kristalle unterwegs
- [x] Testmodus im Editor (Menü **Lunar Cleanup → Testmodus**): eigener Test-Spielstand, Test-Tasten F5–F10 nur dort

## Morgen zuerst: Playtest-Befunde vom 30.10. (Fabian)
Reihenfolge so gewählt, dass jeder Punkt einzeln in Unity geprüft werden kann (Basis-Prüfung F11 + Fotos).

1. [ ] **Bänder ohne Anfang / Ende in der Luft (Mars: roh→Rakete, roh→Raffinerie; Io: Band oben aus dem Rohstofflager)**
   - Ursache: Vom Rohstofflager gehen auf Mars drei Bänder ab (→ Presse, → Raffinerie, → Rakete). Zwei davon werden als „Abzweig“ (Y) gekappt und beginnen dort, wo sie das erste Band berühren – liegt dieses Band dort aber angehoben (Brücke) oder ist selbst gekappt, hängt der Abzweig in der Luft. Bei Io startet ein Band schon angehoben („Brücke ab Start“), deshalb kommt es oben aus dem Lager.
   - Lösung: **Jedes Band bekommt seinen eigenen Ausgang und Eingang** (bis 3 Plätze je Gebäudefläche, notfalls Flächen um die Ecke). Y nur noch, wenn alle Plätze belegt sind – und nur, wenn der Treffpunkt auf einem sichtbaren, gleich hohen Bandstück liegt (wird geprüft, sonst eigener Port). Brücken nie an Bandenden: Rampe frühestens 2 m nach dem Ausgang, die Bandenden liegen immer auf Normalhöhe.
   - Basis-Prüfung erweitern: „Bandende hängt in der Luft“ und „Band beginnt/endet nicht an einem Gebäude oder Band“.

2. [ ] **Bänder laufen ineinander (Europa: Produktlager → Rakete; Titan: Bänder mit mehreren Wegen)**
   - Ursache: Y-Zusammenführungen und Brücken überlagern sich, wenn mehrere Bänder zum selben Ziel wollen.
   - Lösung wie 1 (eigene Ports statt Y). Dazu Router: Bänder mit gleichem Ziel nebeneinander mit festem Abstand (Kanal-Prinzip) statt übereinander; weniger Brücken durch eine Strafe für Kreuzungen, die sich mit einem längeren Weg vermeiden lassen.

3. [ ] **Unnötige Bänder entfernen**
   - Heute: 12 Bänder je Basis (3 Entladestationen → Lager, u3 → Presse, Lager → Presse/Raffinerie/Rakete, Presse → Schmelze/Produktlager, Schmelze → Produktlager, Raffinerie → Produktlager, Produktlager → Rakete).
   - Mit Punkt 4 (eine Entladeanlage) fallen 3 Bänder weg (u2→Lager, u3→Lager, u3→Presse); übrig bleiben 9, jedes mit klarer Aufgabe.

4. [ ] **Nur noch eine Entladeanlage je Planet** (Vorschlag, morgen kurz bestätigen)
   - Die drei Stationen werden zu **einer Anlage mit 3 Ausbaustufen** am Platz der heutigen kleinen Station: Stufe 1 = heutige Kleine (1 Andockplatz, klein), Stufe 2 = + Industrielle (2 weitere Plätze, auch mittel), Stufe 3 = + Zentrale (3 weitere Plätze, auch groß). Kosten, Puffer und Raten bleiben wie bisher.
   - Spielstände bleiben gültig: intern bleiben u1/u2/u3 bestehen (gekaufte Stufen), nur der Bauplatz, die Optik und das eine Band zum Lager sind gemeinsam. Fahrzeuge fahren alle zur selben Anlage, jede Größe zu ihren passenden Plätzen.
   - Auch im Browser-Prototyp nachziehen (Regeln/Texte), Hauptziele mit „Baue die … Entladestation“ werden zu „Baue die Entladeanlage aus (Stufe 2/3)“.

5. [ ] **Andockplätze der Entladestationen kreuz und quer**
   - Ursache: Die Gebäude stehen im Bild jetzt gerade (auf 90° gedreht), die Andockplätze und Gruben folgen aber noch der alten schrägen Ausrichtung aus dem Layout (z. B. 83° auf Mars, 52° auf Kepler).
   - Lösung: Andockplätze in einer Reihe an einer geraden Seite der Anlage (gleiche Drehung wie das Gebäude), Gruben direkt davor, Anfahrspur frei von Bändern. Mit Punkt 4 eine gemeinsame Andockseite mit bis zu 6 Plätzen.

6. [ ] **Gebäudenamen im Fenster abgeschnitten**
   - Lösung: Titel in den Fenstern verkleinern sich automatisch (Auto-Size, 2 Zeilen erlaubt), lange Namen wie „Industrielle Entladestation“ passen dann ganz.

7. [ ] **Bohrturm und Erzbagger neu gestalten**
   - Bohrturm: richtiger Gittermast (4 Beine mit Querstreben), Drehtisch, Bohrgestänge, das sich beim Bohren dreht und auf/ab fährt, Rohrlager, Warnleuchte oben, Fundament.
   - Erzbagger: klar erkennbarer Bagger – Kettenlaufwerk, drehbarer Oberwagen mit Kabine, Ausleger + Stiel + Schaufel, die beim Fördern gräbt und schwenkt, dazu ein kleiner Erzhaufen.

8. [ ] Danach: Basis-Prüfung über alle Planeten, Fotos ansehen, Browser-Version neu bauen.

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

## Für die Handy-Version (neu)
- [ ] Google Play Spiele (Android) und Game Center/iCloud (iOS) wirklich anbinden: `Core/Platform.cs` – Klassen PlayGamesCloud/GameCenterCloud füllen
- [ ] Unity IAP einrichten und die Shop-Angebote (Platform.Offers) mit echten Store-Produkten verbinden; Belohnungen erst nach bestätigtem Kauf
- [ ] Shader „Skybox/Panoramic“ in „Always Included Shaders“ aufnehmen (sonst fehlt der Himmel im Handy-Build)

## Offene Kleinigkeiten
- [ ] Hügel/Krater gibt es nur in Unity (reine Optik). Bohrtürme auf Hängen stehen leicht schief; Seen folgen dem Gelände statt flach zu liegen
- [ ] Schwebereiniger im Spiel kaufen und auf einem See-Planeten (z. B. Europa) im Editor beobachten (bisher nur im Browser-Test geprüft)
- [ ] Mausrad-Zoom im Editor reagiert bei meinen Tests nicht (auf dem Handy zoomt man mit zwei Fingern; prüfen, ob es nur an den simulierten Mausrad-Ereignissen liegt)
- [ ] Hauptziel 1 zählt nur die Startparzelle – wie im Browser, aber eventuell freundlicher machen
- [ ] Im Browser-Prototyp fehlt noch der Fix „Rückseite des Planeten“ für die Wegfindung (in Unity behoben)

## So geht es weiter
Sag einfach „mach mit Punkt X weiter“ (oder „mach weiter“ für den nächsten offenen Punkt). Jeder Teil wird einzeln gebaut, in Unity getestet, gesichert (Git) und hier abgehakt.
