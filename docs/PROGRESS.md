# Fortschritt

## Fertig – Browser-Prototyp v4.3 (`prototype/index.html`)
- v4.3: Straßenlaternen in der Basis (1–2 je Gebäude, bis zu 27 pro Planet), die nachts leuchten und die Gebäude anstrahlen. Layout-Test prüft sie auf allen 10 Planeten.
- v4.2: Arbeitslicht beim Wischen (Lampe + weicher Lichtfleck um den Saugkopf, ab Dämmerung). Verlässt der Finger beim Wischen die Parzelle, pausiert der Saugkopf (roter Ring), das Bild bleibt stehen; nur ein kurzer Wisch ohne Ertrag wird noch zur Kamerabewegung. Randscrollen erst nach 0,45 s am Rand und nur über der eigenen Parzelle.
- v4.1: Sammelwagen ab dem Start (120 Cr × Planetfaktor^0,85, kein Fahrzeugplatz): fährt bei vollem Sammelbehälter zur Stelle, an der du wischst, du leerst den Behälter mit einem Tipp hinein, er bringt die Ladung zur Entladestation mit dem meisten Platz (sonst Rohstofflager) und parkt wieder. Upgrade „Schneller Sammelwagen“, neues Hauptziel 7.
- v4.0: Tag und Nacht auf jedem Planeten (eigene Tageslänge, Solar nur bei Tag, nachts Speicher, Akkus, RTG, Geothermie, Kernkraft), Scheinwerfer mit Lichtkegel an allen Fahrzeugen, Flutlicht an der Basis. 23 neue Hauptziele (jetzt 43, bis Pluto und Neustart), 17 Erfolge mit Stufen und Belohnungen, Ziel-Übersicht über den Ziel-Chip.
- v3.9: Frachtraum zeigt den Wert der Ladung (auch auf „Jetzt starten“), Inhalt, Frachtauftrag und Mischfracht-Bonus. Jede Rakete hat einen Frachtauftrag für eine Rohware des Planeten (+60 %), gemischte Ladung bringt +8 % je weitere Warensorte (max. +40 %). Das Band Rohstofflager → Verkauf bedient den Auftrag laufend (abschaltbar) und lässt 8 % Vorrat für Presse/Raffinerie.
- v3.8.3: Namensschilder passen sich der Textlänge an (nichts wird mehr abgeschnitten), Bauplätze zeigen „BAUPLATZ“ klein über dem Gebäudenamen; Schilder werden nach dem Laden der Schrift neu gezeichnet.
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
- Nachtoptik nur per Software-Rendering geprüft (Scheinwerfer, Lichtkegel sichtbar); auf echten Handys noch ungesehen. Der Wegfindungstest schwankt gelegentlich knapp über der Grenze (zeitabhängig).

## Nächste Schritte (Vorschlag)
1. Feedback des Spielers zur v3.5 einarbeiten (im echten Browser testen lassen).
2. Balancing-Pass für Planeten 3–10.
3. Unity-Projekt unter `unity/` anlegen (siehe `CLAUDE.md`) und Systeme in dieser Reihenfolge portieren: Planet/Parzellen → Reinigung → Wirtschaft/Lager → Fahrzeuge/Stationen → Bänder/Maschinen/Rakete → Idle/Offline → Upgrades/Forschung → Drohnen/Außenlager → Langzeitsysteme → UI.
