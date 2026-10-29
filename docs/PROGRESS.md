# Fortschritt

## Fertig – Browser-Prototyp v3.8 (`prototype/index.html`)
- v3.8.2: Gebäudenamen nur noch beim angetippten Gebäude (immer über allem, kein Clipping mit dem Boden); Webseiten-Screenshots neu.
- v3.8.1: Alle Bandenden reichen bis in ihr Gebäude (Rohstofflager → Verkauf und Rohstofflager → Raffinerie bekamen einen Knick hinein, Layout-Test prüft jetzt jede Lücke). Übergabekästen ersetzt durch offene Tore, unter denen die Waren durchfahren. Bänder zum selben Ziel rechnen ihre Ware gemeinsam (kein Stau mehr am Verkaufsfrachtraum).
- v3.8: Teamarbeit der Fahrzeuge (Sektoren auf einer Parzelle, +10 % je Partner, große/harte Parzellen ziehen Helfer an), Akkus für Staubi, Buggy, Industriesauger und Magnetgleiter (laden an Andockplätzen, Upgrade „Größere Akkus“, ziehen unterwegs keinen Netzstrom), Sammelbehälter des Spielers (Handabbau wird getragen und von Hand in Trichter oder Lager entladen, Upgrade „Größerer Sammelbehälter“), neues Tutorial-Ziel. Tests mit festem Zufallsstartwert.
- v3.7: 5 Spezialfahrzeuge für spätere Planeten, Lastdrohnen (Schwerlast-Hangar) fliegen Maschinen zu entfernten Parzellen, Radioisotopen-Generator, Geothermie, Kernreaktor, Akkupark, Förderbänder ohne Lücke mit Übergabekästen, Zahlen bis Mrd/Bio.
- v3.6.1: Wischen klebt nicht mehr – verlässt der Finger die Parzelle (oder den Planeten), dreht sich ab da der Planet mit; am Bildschirmrand scrollt der Planet beim Wischen mit.
- v3.6: Cloud-Spielstand (Claude-Konto), Sicherungskopie, Export/Import-Code, schlanker Speicherstand, Meldung bei Speicherfehler.
- 10 Planeten als Goldberg-Polyeder (252 bis 3 242 Parzellen), eigene Optik, Gefahren und Stürme.
- Handreinigung (eine Parzelle pro Wisch), Frontier-System ohne Freischaltung, farbige Parzellenränder, grünes Aufleuchten.
- Meteoritenstaub (selten, heftig), 4 Fahrzeugtypen, 3 Entladestationen, Wegfindung um Gebäude und Bänder.
- Förderbänder mit runden Kurven, Hochbänder als Brücken, Presse, Schmelze, Raffinerie, Produktionsplan.
- Verkaufsrakete mit Frachtraum (startet bei vollem Frachtraum), Verkaufsautomat, Pull-Bänder + Überlauf (kein Stau).
- Produktions-Ampel im HUD und im Produktionsfenster, mit Ursache, passenden Upgrades und Gebäudelampen.
- Drohnen + Hangar, Außenlager mit Pipeline, Sturmwarnung + Sturmschild.
- Globale Technik und Basis pro Planet, Anlagen-Upgrades pro Planet, Leistungsanforderung pro Planet.
- 80-%-Regel für den nächsten Planeten, Planet-Abschlussbonus, Funde, tägliche Aufträge, Statistik mit Prognosen, Sternenstaub-Prestige.
- Offline-Simulation (8/12/24 h) über dieselbe `simStep`, Zusammenfassung, Speichern mit Migration.
- Headless-Tests: Kernsysteme, Offline, Speichern/Laden, Layout auf allen Planeten, Wegfindung.

## Bekannte Grenzen
- 3D-Optik ist nur im echten Browser prüfbar. Bisher ungesehen: Rahmen, Kuppel, Drohnen, Pipeline-Röhren, Raketenflug, Lampen, Wegfindung in Bewegung.
- Balancing der späten Planeten ist grob (Upgrade-Kosten im Verhältnis zu Freischaltkosten, Sternenstaub vermutlich zu großzügig).
- An Gebäudeecken liegt die Wegfindung in seltenen Fällen eine Rasterzelle zu nah am Gebäude (Rundung, kein sichtbares Durchfahren).
- Mehrere Fahrzeuge können einander durchfahren (keine Kollision zwischen Fahrzeugen).

## Nächste Schritte (Vorschlag)
1. Feedback des Spielers zur v3.5 einarbeiten (im echten Browser testen lassen).
2. Balancing-Pass für Planeten 3–10.
3. Unity-Projekt unter `unity/` anlegen (siehe `CLAUDE.md`) und Systeme in dieser Reihenfolge portieren: Planet/Parzellen → Reinigung → Wirtschaft/Lager → Fahrzeuge/Stationen → Bänder/Maschinen/Rakete → Idle/Offline → Upgrades/Forschung → Drohnen/Außenlager → Langzeitsysteme → UI.
