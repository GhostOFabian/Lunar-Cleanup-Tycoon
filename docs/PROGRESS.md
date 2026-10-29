# Fortschritt

## Fertig – Browser-Prototyp v3.6 (`prototype/index.html`)
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
