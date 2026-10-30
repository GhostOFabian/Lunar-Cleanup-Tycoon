# Lunar Cleanup Tycoon – Projektregeln für Claude Code

Spielbarer 3D-Mobile-Prototyp: Reinigungssimulation + Idle-Tycoon + Automatisierung + Planeten-Progression. UI-Sprache Deutsch.

## Was hier liegt
- `prototype/index.html` – **Referenzimplementierung** (Browser, three.js r128, eine Datei). Komplett spielbar, Stand v3.5. Alle Spielregeln und Zahlen stammen von hier.
- `prototype/tests/` – Headless-Regressionstests (Playwright + three.js-Mock).
- `docs/GAME_SPEC.md` – vollständige Spezifikation (vor größeren Änderungen lesen).
- `docs/PROGRESS.md` – Stand und nächste Schritte (nach jeder Phase aktualisieren).
- `docs/DECISIONS.md` – getroffene Entscheidungen (neue Entscheidungen mit einem Satz ergänzen).
- `index.html` + `assets/screens/` – Projekt-Webseite (GitHub Pages). Quelle ist `site/page.html`; `python tools/standalone.py site/page.html index.html` erzeugt die fertige Seite.
- `docs/CONVERSATION.md` – Verlauf der Wünsche und des Feedbacks des Spielers (Fabian). Daran erkennst du, was ihm wichtig ist.

## Zwei Arbeitsbereiche
1. **Prototyp weiterentwickeln** (Standard, solange nichts anderes gesagt wird): Änderungen in `prototype/index.html`, danach Tests laufen lassen.
2. **Unity-Umsetzung** (`unity/`, noch nicht angelegt): erst auf ausdrücklichen Wunsch. Das Verhalten des Prototyps wird 1:1 übernommen.

## Prototyp – Regeln
- Eine HTML-Datei, kein Build-Schritt. Die ersten 5 Zeilen (doctype, html, head, charset, viewport) machen sie eigenständig lauffähig (GitHub Pages). Für eine Veröffentlichung als Claude-Artefakt diese 5 Zeilen weglassen. Externe Skripte nur three.js r128 von cdnjs; Schriften Oxanium + Atkinson Hyperlegible (Google Fonts).
- **Eine Simulation für live und offline:** `simStep(pid, dt, live)` ist die einzige Quelle für Reinigung, Fahrzeuge, Drohnen, Bänder, Maschinen, Energie, Verkauf, Stürme und Meteoriten. Offline (`runOffline`) ruft dieselbe Funktion in 1-s-Schritten auf. Nie eine separate Offline-Formel.
- Lokale (pro Planet) Upgrades werden über `CUR` gelesen – `simStep` setzt `CUR=pid`. Neue lokale Upgrades bekommen `loc:1`.
- Spielstand: `localStorage` `lunar-cleanup-tycoon-v2` (+ `-bak`), im Claude-Viewer zusätzlich Cloud über `claude.use('db')` (Artefakt mit `capabilities: {db:{}, user:{}}` veröffentlichen). Funktionen: `serialize`, `save`, `load`, `normalizeSave`, `cloudInit`, `cloudPush`, `adoptSave`, `exportCode`, `importCode`. **Spielstände dürfen nie verloren gehen:** neue Felder nur mit Default, `normalizeSave` erweitern, mit alten Ständen testen. Neue Felder immer mit Default in `freshSave()`/`freshPlanet()` anlegen; `load()` füllt fehlende Felder auf. Bestehende Spielstände dürfen nicht kaputtgehen.
- Gebäudepositionen nur ändern, wenn der Layout-Test (`layout_check.py`) danach ohne Fehler läuft (kein Überlappen, ≥ 2 m Abstand, alles auf Basiszellen aller 18 Planeten). Seit v6.3 hat jeder Planet ein eigenes, erzeugtes Layout (`layoutOf`/`genLayout`); `BLD.pos` und `BELTS` gelten immer für das aktive Layout (`applyLayout`/`withLayout`).
- Keine Platzhalter-Buttons. Jeder sichtbare Knopf tut etwas.
- Texte auf Deutsch, Du-Form, kurz.
- Keine bestehenden funktionierenden Systeme entfernen, außer der Spieler wünscht es (Beispiel: Handgerät-Limit wurde auf Wunsch entfernt).

## Prototyp – prüfen
```bash
pip install playwright && python -m playwright install chromium   # einmalig
cd prototype/tests
python build_test_page.py            # erzeugt test.html mit Test-Hook (__t)
python regression.py                 # Kernsysteme, Offline 8 h, Speichern/Laden
python layout_check.py               # Gebäude/Bänder/Docks auf allen 10 Planeten
# Syntax: JS aus index.html extrahieren und `node --check` (macht build_test_page.py mit)
```
Die Tests mocken three.js. Für echte Screenshots three.js r128 (`git clone --depth 1 --branch r128 --filter=blob:none --sparse https://github.com/mrdoob/three.js`, dann `git sparse-checkout set build`) per Playwright-Route einspielen und Chromium mit `--use-gl=angle --use-angle=swiftshader --enable-unsafe-swiftshader` starten. Visuelle Änderungen klar als „ungetestet im Browser“ melden.

## Unity-Umsetzung (später) – Tech-Stack
- Unity 6 (6000.x LTS), C#, URP, Android + iOS, neues Input System (EnhancedTouch), uGUI + TextMeshPro, Canvas Scaler 1080×1920, Safe Area.
- Keine kostenpflichtigen Assets: prozedurale Meshes, eigene Shader, Partikel, prozedurales Audio.
- Architektur: kleine Systeme mit klarer Verantwortung, Bootstrap/ServiceLocator, C#-Events, Balancing in ScriptableObjects, Logik (reines C#, testbar) getrennt von Darstellung, `ProductionSimulator.Tick(dt)` als einzige Simulation, Save-DTOs mit `saveVersion`.
- Ordner: `unity/Assets/Scripts/{Core,Gameplay,Cleaning,Economy,Automation,Idle,Upgrades,Planets,UI,SaveSystem,Audio,Editor}`, Namespaces `LunarCleanup.<Ordner>`, je Ordner `.asmdef`.
- Menüpunkt `Lunar Cleanup/Build Prototype` baut Szene und verdrahtet alles (idempotent, batchmode-fähig).
- Ist Unity nicht installiert, klar melden und nichts als getestet bezeichnen, was nicht gelaufen ist.

## Arbeitsweise
- Funktionierendes Gameplay vor theoretischer Perfektion.
- Offene Entscheidungen selbst treffen und in `docs/DECISIONS.md` festhalten statt nachzufragen.
- Nach jeder Änderung `docs/PROGRESS.md` aktualisieren.
- Git: Commit-Nachrichten auf Englisch oder Deutsch, kurz; auf `main` nur nach Rückfrage pushen, sonst Feature-Branch.
