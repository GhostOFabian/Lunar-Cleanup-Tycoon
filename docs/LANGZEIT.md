# Langzeitmotivation – Analyse, Maßnahmen, Umsetzung (v6.0)

Grundlage ist der Code in `prototype/index.html` (v5.9), nicht nur die Dokumentation. Zahlen sind aus dem Code gelesen.

## 1. Bestandsaufnahme – warum es später monoton wird

| # | Ursache im Code | Wirkung |
|---|---|---|
| 1 | **Jeder Planet ist dieselbe Rechnung × Planetfaktor.** Warenpreise (`pvM`), lokale Upgrade-Kosten (`ucost` × `diff`), Gebäude (`B.cost × diff`) und Freischaltung skalieren alle mit `diff`. | Relativ gesehen ist jeder neue Planet derselbe Aufbau, nur mit größeren Zahlen. |
| 2 | **Technik ist global** (`gs(sp)=max(stage,maxStage)`, Fahrzeug-, Handgerät-, Drohnen-Upgrades global). | Auf einem neuen Planeten ist sofort alles baubar; die einzige Hürde ist Geld. |
| 3 | **Gefahren sind passiv.** Jeder Planet hat `hz` (Energie-, Solar-, Staubfaktor, Sturm alle N s). Der Sturmschild (Forschung) schwächt alle Stürme um 80 %, überall. | Nach einer Forschung spielt die Planetengefahr kaum noch eine Rolle für Entscheidungen. |
| 4 | **Upgrades sind fast nur „mehr vom Gleichen“.** Jedes Upgrade ist strikt besser, kein Upgrade schließt ein anderes aus. | Die Entscheidung ist nur: „Was kann ich mir gerade leisten?“ |
| 5 | **Ziele sind einmalig.** 44 Hauptziele, 23–24 Planetenziele, Forschung (10 Projekte) – alles einmal erledigt. Tagesaufträge sind generisch und planetenunabhängig. | Zwischen zwei Freischaltungen gibt es wenig, worauf man gezielt hinarbeitet. |
| 6 | **Engpässe werden gezeigt (Ampel), aber nicht belohnt.** Eine sauber geplante Fabrik verdient genau so viel wie eine mit Dauerstau, solange die Rakete voll wird. | Gute Planung hat keinen eigenen, sichtbaren Vorteil. |
| 7 | **Leistungsanforderung** (`POWREQ`, Doppelbürsten-Stufe pro Planet) ist die einzige planetenspezifische Technikbedingung – einmal gekauft, erledigt. | – |

Betroffen: `prototype/index.html` (Wirtschaft: `cargoValue`, `sellStep`; Produktion: `machStep`, `rohCap/prodCap`; Fahrzeuge: `vehStep`; Ampel: `flowReport`; Ziele: `planetGoals`, `goalsHTML`; Basis-Fenster).

## 2. Maßnahmen (Vorschläge mit Bewertung)

| Maßnahme | Wirkung Spielspaß | Aufwand | Risiko | Balance |
|---|---|---|---|---|
| **A. Ausrichtung pro Planet** (Bergbau / Industrie / Handel, eine passt je Planet ×1,5) | hoch – echte Entscheidung pro Planet, Planeten unterscheiden sich | klein | niedrig (nur Multiplikatoren auf bestehende Größen) | +25–45 % auf eine Achse, bewusst moderat |
| **B. Betriebsbonus** (bis +30 % Verkauf, wenn die Ampel grün bleibt) | hoch – gute Planung lohnt sich spürbar, nutzt die vorhandene Ampel | klein | niedrig (keine Strafe unter den Grundwert) | bis +30 %, sinkt nur bei Rot |
| **C. Werksaufträge pro Planet** (eine wiederkehrende Lieferaufgabe für ein Endprodukt, Ruf +2 % je Auftrag) | mittel–hoch – Ziele zwischen Freischaltungen, verbindet Produktionsplan und Rakete | mittel | niedrig (zählt nur Raketenverkäufe) | Belohnung 80 % des Auftragswerts, Ruf max. +50 % |
| D. Planetenmärkte (Preisaufschläge je Planet für bestimmte Waren) | mittel | mittel | mittel (Preisgefüge ändert sich) | schwer einzuschätzen |
| E. Sturmschild pro Planet ausbaubar statt global | mittel | mittel | mittel (alte Stände verlieren Schutz) | würde bestehenden Fortschritt entwerten |

## 3. Priorisierung

Umgesetzt: **A, B, C** – sie verbinden vorhandene Systeme (Ampel, Produktionsplan, Rakete, Planetengefahren, Planetenziele), ändern keine bestehende Mechanik und keine Speicherfelder außer neuen Feldern mit Standardwert. D und E zurückgestellt: D verschiebt das Preisgefüge, E würde erworbenen Fortschritt (Sturmschild) entwerten – beides gegen die Grundregeln.

## 4. Umsetzung

- **Ausrichtung** (`SPEC`, `SPEC_FIT`, `specF`, `setSpec`, `specSec`): ab Basisstufe 3 im Basis-Fenster. Erste Wahl kostenlos, Wechsel `3 000 × diff`.
  - Bergbau: Fahrzeuge +25 % (Tempo und Reinigung), Bohrtürme +30 %.
  - Industrie: Maschinen +30 %, Roh- und Produktlager +30 %.
  - Handel: Preise +20 %, Raketentempo +30 %.
  - Passende Ausrichtung ×1,5 (Mond Handel, Mars Bergbau, Europa Industrie, Titan Industrie, Io Bergbau, Venus Handel, Ganymed Industrie, Enceladus Bergbau, Triton Bergbau, Pluto Handel) – begründet aus der jeweiligen Planetengefahr.
- **Betriebsbonus** (`flowTick`, `flowBonus`): alle 5 s Ampelstufe des Planeten (dieselbe `flowReport` wie in der Anzeige). Grün: +1/600 je s (10 min bis voll), Gelb: hält, Rot: −1/120 je s. Bonus = `1 + 0,3 × flowB` auf den Raketenverkauf. Läuft in `simStep`, also auch offline. Anzeige im Ampel-Chip und im Verkaufsfenster.
- **Werksaufträge** (`newWorkOrder`, `woCount`, `woSec`): ab Basisstufe 2 immer ein Auftrag pro Planet für ein Endprodukt des Planeten (`endGood`). Wert `2 500 × diff × 1,55^n`, Belohnung 80 % davon, danach automatisch der nächste (nie dieselbe Ware zweimal hintereinander). Jeder erfüllte Auftrag: Ruf +2 % auf alle Preise dieses Planeten (max. +50 %). Anzeige in „Ziele & Erfolge“, neuer Erfolg „Lieferant“.
- Neue Speicherfelder je Planet mit Standardwert: `spec`, `flowB`, `wo`, `woDone` (über `freshPlanet` → `normalizeSave`).

## 5. Tests (ausgeführt)

- `regression.py` (alle Kernsysteme, Offline 8 h, Speichern/Laden alter Stände) – ALL OK; neuer Test „specialisation / flow bonus / work orders“ (Kosten, ×1,5-Passung, Bonus im Ladungswert, Auftrag zählt und erneuert sich).
- `layout_check.py` – ALL OK.
- Balance-Lauf: kleine Mondbasis 8 × 1 h offline – erster Werksauftrag nach ~1 h, Betriebsbonus erreicht bei grüner Ampel 30 %.
- Echte Darstellung (Software-Rendering): Ausrichtung im Basis-Fenster, Bonus im Ampel-Chip, Werksauftrag in den Zielen.

Nicht geprüft: Spielgefühl über viele Stunden auf echten Geräten.
