# Spielprinzip-Plan: Hard Mode und normaler Modus (Stand 04.11., 08:05)

Leitidee
- **Normaler Modus = Tycoon**: Geld, Fabrik, Automatisierung. Prinzip bleibt, wird gezielt geschärft.
- **Hard Mode = Expedition**: allein mit kleiner Crew auf feindlichen Planeten. Knapp sind Energie, Reichweite und seltene Materialien – nicht Geld. Die Basis ist Lebenserhaltung und Werkstatt.

Legende: ✅ fertig und veröffentlicht · 🟡 geschrieben, noch nicht kompiliert · ⬜ geplant

---

## A. Hard Mode

### A1 Balance 🟡 (Code auf GitHub, Build 54)
- Verkauf ×1 statt ×3.
- Ziele, Erfolge, Aufträge, Tagesbonus, Herausforderungen: 10 % Geld (`GoalF`).
- Hard-Ziele: Erfahrung + Perks statt Geld.

### A2 Perks nur als Funde 🟡
- Nur beim Selbstwischen: 6 % je Parzelle, Pech-Schutz +1,2 % je Parzelle nach 12 ohne Fund.
- 70 % häufig (Bombe, Turbo, Breiter Pinsel, Magnet) · 26 % selten (Schaum, Kärcher, Espresso, Goldener Mopp, Rohrpost) · 4 % sehr selten (Tornado). Nuke nur über die Ulti-Leiste.
- Leuchtende Kiste springt aus der Parzelle (weiß/blau/gold). Über der Tragegrenze: +3 Erfahrung.
- Shop → „Inventar“ mit Reitern Inventar / Talente / Crew (Putzhelfer vorerst kaufbar).

### A3 Anzug-Energie und Ladesäulen 🟡
- Energie 100. Wischen: 1,2/s bis 20 m von Hauptgebäude oder Ladesäule, danach +1 je 30 m. Ohne Wischen +8/s.
- Ladesäulen auf sauberen Parzellen (antippen): 30 E Rohstoffe, +35 % je weitere, höchstens 2 + 2 × Basisstufe.
- Leiste „Anzug %“ über der Perk-Leiste; Säule mit Reichweiten-Kreis.

### A4 Basis wird Werkstatt ⬜
- Im Hard Mode ausgeblendet: Förderbänder, Presse/Schmelze/Raffinerie, Produktlager, Raketenverkauf-Ketten.
- Rohstoffe werden direkt verkauft (Sammelshuttle → Lager → kleine Verkaufsrakete) oder verbaut.
- Neue Gebäude (Rohstoffe + wenig Geld):
  - **Recycler**: 40 E Schmutz → 1 Perk-Kiste; seltene Schmutzarten (z. B. Kristall, Strahlenerz) → nur seltene Perks.
  - **Sensormast**: zeigt Funde, Wracks und Kisten im Umkreis 60 m auf der Karte.
  - **Wetterstation**: Schmutzwelle und Sturm 60 s statt 30 s vorher, Welle trifft 1 Parzelle weniger.
  - **Akku-Labor**: Anzug +25 Energie je Stufe (3 Stufen).
  - **Quartier**: Platz für gerettete Crew (2 + 1 je Stufe).
- Basisstufen kosten im Hard Mode Rohstoffe + Geld und schalten diese Gebäude frei.

### A5 Gründe zu bleiben ⬜
- **3 Sterne je Planet** (siehe C1, gilt für beide Modi).
- **Gestrandete Astronauten**: je Planet 2–4 unter dem Dreck versteckt (Signal-Pieps, wenn nah). Freiwischen = Crew-Mitglied mit Eigenschaft:
  flink (+40 % Tempo) · gründlich (fegt 2 Parzellen zugleich) · Spürnase (+50 % Fundchance in seiner Nähe) · Techniker (Ladesäulen −25 %).
  Ersetzt den Kauf von Putzhelfern.
- **Sprungkern** statt Geld zum Weiterreisen: 3 Kernsplitter je Planet, liegen in entlegenen Regionen (> 60 % Entfernung von der Basis). Mit 3 Splittern + 80 % sauber geht die Reise.
- Planet-Eigenheiten (siehe C2).

### A6 Hard-Talente ergänzen ⬜
- Neue Talente: Akku I/II (+15 Energie), Sparsam (−15 % Verbrauch), Schatzsucher (Kernsplitter auf der Karte).

---

## B. Normaler Modus

### B1 Belohnungen prüfen ⬜
- Messen: Geld aus Zielen/Erfolgen/Aufträgen in % des Einkommens der ersten Stunde je Planet.
- Ziel: Belohnungen ≈ 2–5 min Einkommen des Planeten, nie mehr als 1 Basisstufe. Erfolge mit Sternenstaub statt großer Geldbeträge.
- Wo nötig: Belohnung an Planeten-Einkommen koppeln statt fester Werte.

### B2 Bestehendes bleibt
- Planetenkasse ✅, Crew-Betreuung (Leistung sinkt ohne dich) ✅, Fabrik und Automatisierung unverändert.

---

## C. Für beide Modi

### C1 3 Sterne je Planet ⬜
- ★ Planet 100 % sauber · ★ unter der Zielzeit (Hard: reine Spielzeit) · ★ alle Funde, Flaggen und Wracks.
- Anzeige im Planeten-Fenster und auf der Karte.
- Belohnung je Stern: Sternenstaub + dauerhafter Bonus für alle Planeten (z. B. +2 % Verkauf / +2 % Wischkraft im Hard Mode). Alle 3 Sterne: goldener Planetenring.

### C2 Eigenheiten je Planet ⬜ (nach und nach)
- Mars: Staubsturm verschüttet 2–4 saubere Parzellen (bestehende Stürme erweitern).
- Europa: Eisfelder frieren wieder zu, wenn nicht 30 s nach dem Säubern geschützt (Wärmelampe).
- Io: Lava-Parzellen müssen erst abkühlen (Timer), dann wischen.
- Titan: Methanseen nur mit Schwimmmodul (normal) bzw. Brücken-Säule (Hard).
- Venus: Säure frisst langsam an Gebäuden – Wartung nötig.

---

## Reihenfolge zum Bauen
1. Build 54: A1–A3 kompilieren, testen, veröffentlichen.
2. A4 Werkstatt-Basis (Hard).
3. C1 Sterne (beide Modi) + A5 Sprungkern.
4. A5 Astronauten-Crew + A6 Talente.
5. B1 Belohnungen messen und anpassen.
6. C2 Planeten-Eigenheiten, ein Planet nach dem anderen.
