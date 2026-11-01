# Unity-Version – Was noch zu tun ist

Stand: 31.10.2026 · Unity-Version 0.12 · Branch `claude/prototype-v3.5`
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

## iPhone-Playtest vom 31.10. (Fabian) – Version 0.13
- [x] Gebäude antippen ging auf dem iPhone nicht: beim Loslassen wurde bei (0,0) getippt (keine Maus auf dem Handy), dazu Wackelgrenze/Tippradius in Punkten statt Pixeln (Retina ×3)
- [x] Ampeln als Masten neben den Gebäuden (Ecke, auf dem Boden) statt auf dem Dach
- [x] Basis-Parzellen mit kräftig blauen Rändern
- [x] Fingerlicht beim Putzen nachts: warmweiß, viel heller, größere Reichweite
- [x] Kamera folgt beim Putzen sanft der Parzelle, die gerade gereinigt wird; ist sie sauber, der dreckigen Nachbarparzelle (wandert nicht von selbst weiter, wenn man stillhält)
- [x] Weiter rauszoomen (bis 4,5 × Planetenradius), Atmosphärensaum am Planetenrand, Lichter von Gebäuden/Fahrzeugen/Ereignisort aus der Ferne
- [x] Kometen ziehen ab und zu über den Himmel; Wrack-Absturz/Kometeneinschlag kommt als Feuerball mit Rauchspur vom Himmel, danach Rauchsäule; Wrack antippen zeigt den Stand des Suchtrupps
- [x] Fehler „Billboard“: alle Leuchtscheine (Ampeln, Nachbarplaneten) wurden hinter den Planeten versetzt – jetzt drehen sie sich nur zur Kamera
- [x] Test-Knöpfe in Einstellungen → Testmodus: Wrack-Absturz, Kometeneinschlag, Komet am Himmel
- [x] Simulator-Build: Menü „Lunar Cleanup → iPhone-Version für den Simulator bauen (Mac)“ → Builds/iOS-Simulator, in Xcode Ziel „iPhone 18 Pro“
- [ ] Zwei-Finger-Zoom im Simulator nicht testbar (nur auf dem iPhone prüfen)
- [ ] Auf dem iPhone beurteilen: Absturz-Anflug (Feuerball kommt seitlich ins Bild – im Simulator schwer zu erwischen), Fingerlicht bei Nacht (im Simulator nicht getestet, Nacht kommt erst nach Ausbau)
- Hinweis: Die iPhone-Testversion (Development-Build) spielt im Test-Spielstand (Einstellungen → Testmodus). Für den normalen Spielstand dort ausschalten und App neu starten.
- Xcode-Tipp: vor einem neuen Unity-Build das offene Xcode-Projekt schließen, sonst stürzt Xcode beim Ersetzen ab.

## Version 0.14 (01.11., Nacht) – eigene Modelle, Meteoritenschauer, Spielstand-Code
- [x] 147 eigene Modelle aus `LunarCleanup_Assets.zip` in `Assets/Resources/LC/` (Gebäude, Anlagen, Fahrzeuge, Drohnen, Förderband, Produkte, Schmutz, Ereignisse, Welt); Quelle + Blender-Skript + Liste in `Modelle_Quelle/`
- [x] Kenney-Modelle beiseitegelegt: `Assets/_Archiv/Kenney` (werden nicht mehr geladen, liegen nur noch im Projekt). Reihenfolge: eigenes Modell → sonst Kenney (nur wenn zurückgeholt) → sonst selbst gebaute Form
- [x] Im Spiel: alle Gebäude (Rakete, Kolben, Drehteile animiert), Bagger/Bohranlage, alle 10 Fahrzeuge + Suchtrupp (Räder, Werkzeug drehen), Drohnen + Lastdrohne (Rotoren, Ladung), Sammelwagen, Bauplatz, Ampelköpfe, Wracks, Kometeneinschlag, Felsen/Krater/Meteorit als Deko
- [x] 90 Materialien `LC_*` mit festen Farben; `LC_akzent`/`LC_akzent2` bekommen die Farbe des Gebäudes/Fahrzeugs; leuchtende Teile glühen
- [x] Menü **Lunar Cleanup → Eigene Modelle prüfen** (Größe, Ursprung, Teile-Namen → `Modelle_Quelle/pruefung.txt`): 147 in Ordnung. Fahrzeuge zeigen im Export nach −Z, `LcKit.FrontYaw = 180` dreht sie richtig herum (bei Neuexport mit FRONT_FLIP auf 0 setzen)
- [x] Homescreen-Icon `Assets/Icons/AppIcon.png` (1024 px) wird bei jedem iPhone-Build für alle Größen gesetzt
- [x] Auch eingebaut: Waren `ware_*` auf den Bändern, Bodenteile `schmutz_*` (GPU-Instancing), Laternen, Leuchtkristalle (in Sortenfarbe), Astronauten, Außenlager, Abkippgruben, Erzadern neben den Bohrtürmen, Kometenkern im Kometenschweif, Frachter, der ab und zu vorbeifliegt
- [ ] Noch selbst gebaut statt Modell: Förderbänder (band_*, anschluss_*, weiche, brücke, stütze – die Bänder werden frei verlegt, die festen Teile passen nur zu einem Raster-System; eigener Umbau) und die Pipeline-Trasse
- [x] **Meteoritenschauer-Animation**: Meteore fallen nacheinander mit Glutschweif, Einschlagblitz, Staub, Vibration, Kamera-Ruckeln; glühender Brocken (`meteorit`) bleibt liegen, bis die Parzelle wieder sauber ist. Test-Knopf im Testmodus
- [x] **Spielstand-Code** (Einstellungen → Spielstand): kopieren, einfügen (alter Stand bleibt als Sicherung), Import rückgängig
- [x] Fernsteuerung für Claude (`Editor/RemoteCommands.cs`): Unity liest Befehle aus `Temp/claude_cmd.txt` – nur Editor
- [ ] Auf dem iPhone ansehen: Größenverhältnisse Gebäude ↔ Fahrzeuge, Farben bei Nacht, Bandanschlüsse an den neuen Gebäudewänden

### Morgen: einchecken und aufs iPhone
1. Unity → Fenster **Unity Version Control** → alle Änderungen (Scripts, `Assets/Resources/LC`, `Assets/_Archiv`, `Assets/Icons`, `Modelle_Quelle`, `TODO_Unity.md`) auswählen → Kommentar z. B. „Eigene Modelle + App-Icon (0.13)“ → **Check in**. `Builds/`, `Library/`, `Logs/` werden ignoriert.
2. Xcode-Projekt schließen, dann **Lunar Cleanup → iPhone-Version bauen und aufs iPhone** (iPhone per Kabel, entsperrt).
3. Für TestFlight: **iPhone-Version für TestFlight bauen (ohne Testmodus)** – braucht ein bezahltes Apple-Entwicklerkonto.

## Stand der Spielsysteme (geprüft 01.11.)
Alles aus dem Browser-Prototyp ist in Unity drin: Meteoritenschauer, Ausbaustufen nach Basisstufe 5, Sternenstaub-Neustart mit Boni, Frachtaufträge, Verkaufsautomat, Produktionsplan, Produktions-Ampel, Gebäudestufen, Ausrichtung, Betriebsbonus, Werksaufträge, Planetenziele, tägliche Aufträge, Funde, Erfolge, Statistik, Teamarbeit und Arbeitsbereich der Fahrzeuge, Ton und Vibration, Entladeanlage mit 3 Stufen, alle 57 Hauptziele. Ob alles richtig läuft, zeigt der iPhone-Test (Checkliste).

## Noch offen
### Braucht Fabians iPhone-Test
- [ ] Test-Checkliste durchgehen und Befunde schicken
### Grafik
- [ ] Förderbänder aus Fabians Bandteilen (siehe oben) · Planetenboden mit feinerer Textur/Normal-Map
- [ ] Große Bänder-Prüfung (F11) mit den neuen Gebäudemodellen – Bandanschlüsse an den neuen Wänden
### Braucht ein bezahltes Apple-Entwicklerkonto (99 €/Jahr)
- [ ] TestFlight / App Store · Game Center + iCloud-Speicher (`Core/Platform.cs`) · echte In-App-Käufe (Unity IAP)
- Ohne Konto: die App läuft 7 Tage auf dem iPhone, dann neu aufspielen (Menü „iPhone-Version bauen und aufs iPhone“)
### Später
- [ ] Android-Build · Browser-Prototyp auf den Unity-Stand bringen (Entladeanlage, Rückseiten-Fix) · Webseite mit echten Unity-Bildern
- [ ] Kleinigkeiten: Bohrtürme auf Hängen leicht schief, Seen folgen dem Gelände, Mausrad-Zoom im Editor

## So geht es weiter
Sag „mach mit … weiter“. Jeder Teil wird gebaut, im Simulator getestet und hier abgehakt.
