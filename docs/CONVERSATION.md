# Verlauf: Wünsche und Feedback des Spielers

Zusammenfassung der Entwicklungs-Unterhaltung (Claude-App, 28.–29.10.2026), aus der der Prototyp entstanden ist. Neueste Einträge unten. Jeder Punkt ist umgesetzt, außer es steht etwas anderes dabei.

## 1. Ausgangspunkt
- Master-Prompt für einen Unity-6-Prototyp „Lunar Cleanup Tycoon“ (Reinigung + Idle-Tycoon). Wunsch: **Idle-Funktion** – Staubsauger und Maschinen arbeiten auch offline weiter.
- Daraus entstanden `CLAUDE.md`, `docs/GAME_SPEC.md` (v1, Unity-zentriert) und `docs/START_PROMPT.md`.
- „Fang an einen Prototyp zu bauen, damit ich erst mal sehe, wie das Spiel wäre“ → Browser-Prototyp v1 (Mond, Handreinigung, Fahrzeuge, Offline).

## 2. Industrial-Expansion-Update (großes Spec-Dokument des Spielers)
- Polygonale Planeten mit 250/500/1000/2000+ Parzellen, dauerhaft saubere Parzellen mit Einmalbonus.
- 3 Fahrzeugtypen mit Tank- und Entladelogik, 3 Entladestationen, Förderbänder, Presse/Schmelze/Raffinerie.
- 5 Basisstufen, 10+ Upgrade-Systeme, Planetenkarte, Speichern/LOD.
- Regeln des Spielers: „Lösche keine bestehenden funktionierenden Systeme“, „Erstelle keine bloßen Konzeptbeschreibungen“, „Entscheide selbst“.

## 3. Feedback-Runden
1. **Parzellen sollen nichts kosten** – er hatte kein Geld mehr für die nächste. Vorschlag des Spielers: gereinigte Parzellen können wieder verschmutzen, damit die Automatik etwas zu tun hat. → Kostenlose Freischaltung, Meteoritenstaub.
2. **Gebäude überlappen** (Energiezentrale im Hauptgebäude) → feste Abstände zwischen Gebäuden. Mehrere Fahrzeuge pro Typ erlauben. **10 Planeten**, jeder schöner, mehr Ertrag, eigene Gefahren.
3. **Keine Freischaltung mehr:** Parzellen neben sauberen sind direkt reinigbar, ohne Bestätigung; Fahrzeuge nehmen sich die nächste Parzelle selbst. Neuverschmutzung seltener, aber heftig. Verkaufsautomat bei vollem Lager (später). **Mini-Drohnen** für weit entfernte Fahrzeuge (später). **Mehrere Basislager pro Planet**, verbunden (Außenlager). Jeder Planet soll sich irgendwann selbst reinigen, während man woanders spielt. Eigene Basis pro Planet, alte Maschinen direkt baubar, aber Upgrades nötig für neue Planeten.
4. **Förderbänder brauchen runde Kurven** (Clipping in Kurven). **Nächster Planet erst ab 80 % Reinigung**, keine frühe Mars-Forschung. Eigene Vorschläge erwünscht.
5. „Bau alles ein 1–7“: Planet-Abschlussbonus, Anlagen-Upgrades pro Planet, Sturmwarnung + Schild, Funde, tägliche Aufträge, Statistik + Prognosen, Prestige (Sternenstaub).
6. **Sichtbarkeit:** Parzellenränder grün/gelb/rot (auch bei Neuverschmutzung), grünes Aufleuchten bei Abschluss, Planetenkarte einklappbar. **Nie zwei Parzellen gleichzeitig** mit dem Finger reinigen. Fahrzeuge sollen später immer besser sein als die Hand, am Anfang hilft die Hand.
7. **Handgerät nicht limitieren**, keine Limit-Meldung – lieber Fahrzeuge weiter upgraden können. → Deckel entfernt, Fahrzeug-Upgrades erweitert.
8. **Fahrzeuge dürfen nicht durch Gebäude oder Bänder fahren** (richtige Wege). **Band-Verbindung zur Verkaufsstation**, upgradebar; die Rakete fliegt erst los, wenn ihr Lager voll ist. **Produktionsstau vermeiden**, Bänder und Produktion im Gleichgewicht; bei Stau beschreiben warum, damit man passende Upgrades kaufen kann; kleine **Ampel**.
9. Repository `GhostOFabian/Lunar-Cleanup-Tycon` angelegt; Unterhaltung in Claude Code überführt (dieses Dokument).

## Was der Spieler mag / nicht mag
- Mag: sichtbaren Fortschritt, Automatisierung, die sich selbst trägt, mehrere Planeten parallel, klare Rückmeldungen (Farben, Ampel, Begründungen).
- Mag nicht: Bezahlschranken, die blockieren; künstliche Limits beim Handgerät; Clipping/Überlappen; Dinge, die zu früh kommen (Mars-Forschung).
- Kommunikation auf Deutsch, kurz und konkret.

## Offene Ideen (noch nicht gebaut)
- Balancing der späteren Planeten (Upgrade-Kosten im Verhältnis zu Planetenpreisen; Sternenstaub-Formel eventuell strenger).
- Unity-Umsetzung.
- Echter Gerätetest der 3D-Optik (Rahmen, Kuppel, Drohnen, Pipelines, Raketenflug, Wegfindung) – headless nicht prüfbar.
