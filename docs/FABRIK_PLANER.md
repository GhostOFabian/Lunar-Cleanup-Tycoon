# Fabrik-Planer (Fließbänder, ab Build 64)

Die Bänder werden nicht mehr nachträglich zwischen fertig platzierte Gebäude gelegt. Ein zentraler Planer (`unity/Assets/Scripts/Core/FactoryPlanner.cs`) plant Gebäude, Anschlüsse, Bänder und Y-Stücke gemeinsam. Optik und Simulation nutzen dasselbe Ergebnis.

## Gefundene Ursachen im alten System

| Fehlerbild | Ursache im Code (vorher) |
|---|---|
| Bänder enden im Nichts oder neben dem Eingang | Es gab keine festen Anschlüsse. `Ports()` riet bei jedem Aufbau eine Fläche und einen Platz, die Grundfläche wurde aus Modelldreiecken geschätzt, und den Rahmen setzte das Programm erst dorthin, wo das Band die Wand traf. |
| Bänder laufen ineinander oder kreuzen sich | `AStar` gab Kreuzen nur einen Aufpreis (+4), verboten war es nicht. Die Bänder wurden einzeln und nacheinander verlegt. Am Ende blieb nach 4 Reihenfolgen das „am wenigsten schlechte“ Ergebnis, auch mit Kreuzungen. |
| Unterschiedliche Höhen, schwebende Stücke | `PlanBridges` suchte hinterher nahe Stellen und hob das spätere Band um 1 m an, bei Bedarf noch einmal. Höhen waren also eine Folge der Kreuzungen und nicht geplant. |
| Stützen durch andere Bänder | Stützen wurden nach dem Anheben „irgendwo daneben“ gesucht (`SupportSpots`, ±1,4 m), ohne Teil der Planung zu sein. |
| Y-Merger sehen falsch aus | Kein eigenes Bauteil: Ein Band endete dort, wo es nach Abstandsmessung ein anderes berührte (`mergeEnd`). |
| Gebäude zu eng für Bänder | Die Gebäudeplätze kamen aus dem alten Browser-Layout (1–2 m Abstand). Kein Gebäude rückte je zur Seite. |
| Transport passt nicht zur Optik | Die Simulation rechnete mit der Bandlänge des Browser-Layouts. Die sichtbaren Wege waren anders lang und wurden nur umgerechnet. |
| Fehler werden „repariert“ | Band-Editor, eigene Wege und ein Wege-Cache lagen über dem Ergebnis. Die Basis-Prüfung meldete nur. |

## Neuer Aufbau

1. **Produktionsmodul nach dem Materialfluss.** Entladestation → Rohstofflager → Presse (oben) / Raffinerie (unten) → Schmelze → Produktlager → Verkauf. Die Mittelpunkte liegen fest in Modulkoordinaten (`MOD`).
2. **Einpassen in die echte Basis.** Das Modul wird in 4 Drehungen × 2 Spiegelungen und alle Verschiebungen im 1-m-Raster getestet. Prüfgrundlage ist die echte Basisfläche (Basiszellen ohne Wasser/Lava). Bewertet wird, wie nah die Gebäude an ihren alten Plätzen bleiben. Die besten 25 Lagen werden der Reihe nach versucht.
3. **Feste Anschlüsse.** Jedes Gebäude hat genau einen Eingang (Mitte einer festgelegten Fläche, `FACES`). Die Ausgänge liegen nebeneinander auf einer anderen Fläche, im Abstand von 2 m und nach der Lage des Ziels sortiert, damit nichts direkt am Gebäude kreuzt.
4. **Y-Stücke.** Laufen mehrere Bänder zum selben Ziel, ist das längste das Hauptband. Es muss vor dem Eingang lang genug gerade laufen: 2 m plus 5 m je Y. Die anderen münden auf diesem letzten geraden Stück: Das Zweigende liegt 2 m seitlich und 3 m davor, das Y-Stück läuft schräg bis auf die Mitte des Hauptbandes. Die Y-Stücke wechseln die Seite je nach Lage der Quelle.
5. **Wegsuche ohne Kreuzungen.** A* im 1-m-Raster mit dem Zustand (Zelle, Richtung, gerade Meter seit der letzten Kurve).
   - Zwischen zwei Kurven liegen mindestens 2 m gerade (Radius 1 m), und jedes Band fährt gerade aus seinem Anschluss heraus und gerade hinein.
   - Gebäude, ihre 1-m-Zone, die Andockfläche und fertige Bänder mit 1 m Abstandszone sind harte Hindernisse.
   - Scheitert ein Band, kommt es im nächsten Versuch zuerst dran. Sonst werden die Reihenfolgen gemischt, bis zu 30 Versuche, und gewählt wird das Netz mit der geringsten Länge plus 4 je Kurve.
6. **Eine Höhe, keine Brücken.** Weil nichts kreuzt, liegen alle Bänder auf derselben Höhe. Stützen stehen in festen Abständen unter dem eigenen Band.
7. **Grundflächen aus den echten Modellen.** Die halbe Ausdehnung jedes Modells bis 3,5 m Höhe wurde in Unity gemessen (`call DevFootprints`), zum Beispiel Kraftwerk 7,6 m, Entladestation 4,0 m, Produktlager 3,6 m. Im Raster belegt ein Gebäude alle Zellen, deren Mitte im Modell liegt. Bänder beginnen erst zwei Zellen weiter, die Bandkante bleibt also mindestens 0,6 m von jeder Wand entfernt.
8. **Nebengebäude.** Die Nebengebäude werden der Größe nach gepackt, jeweils an den Platz mit dem meisten Kontakt zu Basisrand, Bändern und Gebäuden. So bleibt die freie Fläche zusammenhängend, und auch das 15 m breite Kraftwerk findet Platz. Bei Gleichstand gewinnt der Platz, der dem alten Ort am nächsten liegt.
   - Abstände: 1 m zu anderen Nebengebäuden (wie im alten Layout), 2 m zum Modul.
   - Unter einem Gebäude und 1 m drumherum liegen keine Bandzonen, kein Andockfeld und keine 4 m Zufahrt.
   - Vom Spieler verschobene Nebengebäude bleiben an ihrem Platz, wenn er frei ist.
   - Das Packen rechnet mit Summentabellen, jede Flächenprüfung kostet also gleich viel. Probiert werden bis zu 400 Modul-Lagen.
   - Die Basis umfasst je nach Planet 2770 bis 3456 m² und ist zu rund 85 % belegt.
9. **Prüfung (`Validate`), unabhängig von der Wegsuche.**
   - Jedes Band beginnt an seinem Ausgang und hat keine Lücken.
   - Keine zu engen Kurven und keine Zelle außerhalb der Basis oder in einem Gebäude.
   - Jedes Y-Stück sitzt auf einem geraden Stück seines Hauptbandes.
   - Bänder sind überall mindestens 2 m auseinander, außer am eigenen Y.
   - Gebäude liegen in der Basis und überlappen nicht.
   - Jeder Eingang hat genau ein Hauptband.

   Bei einem Fehler wird die nächste Modul-Lage geplant. Gibt es keine gültige, bleibt die Vorlage unverändert und der Fehler steht im Log und in der Basis-Prüfung.

## Anschluss ans Spiel

- `Game.PlannedLayout(ps)` (`Core/Game.Factory.cs`) erzeugt das Layout des Planeten aus der Vorlage und dem Plan, gemerkt je Planetengröße und Verschiebungen.
  - Positionen, Ausrichtung der Entladestation (Andockplätze zeigen zur Zufahrt), Start-, Park- und Werkstattplatz.
  - Je Band die sichtbare Linie (1,6 m im Gebäude, Kurven mit Radius 1 m) und `L` als echte Länge.
  - Zweigbänder: eigener Weg + Y-Stück + restliches Hauptband bis in den Eingang. Waren laufen sichtbar bis ins Gebäude, und die Laufzeit stimmt.
- `WorldView.RouteBelts` übernimmt nur noch die Linien. `PlanBridges` bestimmt nur noch, wo ein Zweig sichtbar auf seinem Hauptband endet. Läuft ein Hauptband in einer Ausbaustufe noch nicht, übernimmt der vorderste Zweig seine Strecke.
- Banden an Y-Stücken (Build 65): Wo eine Bande auf der Lauffläche eines verbundenen Bandes liegt, bekommt sie eine Lücke (`WorldView.RailRuns`). So laufen die Banden nur außen wie ein Trichter, und am Y-Ende fällt die Endrolle weg.
- Band antippen zeigt nur den Namen. Der Band-Editor ist abgeschaltet, gespeicherte eigene Wege werden nicht mehr benutzt (sie bleiben im Spielstand, nichts geht verloren).
- Produktionsgebäude lassen sich nicht mehr einzeln verschieben (sie gehören zum Modul). Nebengebäude schon, aber nicht auf Bänder oder die Zufahrt.

## Tests

- `tools/factory/` enthält den Python-Prototyp, mit dem der Algorithmus entwickelt wurde: `python3 runall.py` plant alle 49 Layouts mit der echten Basisfläche (`basemask.json`, aus Unity exportiert) und schreibt je ein Bild nach `all/`.
- In Unity:
  - `call DevPlanAll` plant alle Layouts mit dem C#-Planer. Ergebnis für Build 64: 49/49 gültig, im Mittel 81 ms, höchstens 255 ms (einmal je Planet, danach gemerkt).
  - `call DevPlanInfo` zeigt den Plan des aktuellen Planeten.
  - Die Basis-Prüfung (F11 bzw. `basecheck`) prüft zusätzlich die echte Geometrie mit den Modellen in jeder Ausbaustufe: Bandenden an der Wand, Stützen nicht in Gebäuden oder Bändern, keine Überschneidung, Modelle nicht zu nah, Anschlussrahmen nicht zu dicht. Ergebnis für Build 64: **0 Befunde** über alle 49 Layouts und alle Teilausbauten (`docs/basispruefung_build64.txt`). Der erste Lauf fand einen Befund (Kraftwerk zu nah am Lift). Daraufhin rechnet der Planer mit den echten Modellmaßen.
  - Sichttest im Editor (Mond, Mars): drei Ausgänge des Rohstofflagers nebeneinander, gerades Hauptband ins Produktlager, drei Y-Stücke abwechselnd links und rechts, das Band zum Verkauf liegt an der Raketenplattform an, alles auf einer Höhe, Stützen unter dem eigenen Band.
  - Abgedeckte Testfälle aus der Aufgabe:
    - direkte Verbindung zweier Gebäude: u1→Rohstofflager, Produktlager→Verkauf;
    - viele parallele Bänder: drei Ausgänge des Rohstofflagers;
    - Y-Merger und mehrere hintereinander: drei auf einem Hauptband;
    - enge Platzverhältnisse und problematische Ausgangslage: alle 49 alten Layouts mit 1–2 m Gebäudeabstand werden neu geordnet;
    - Teilausbauten: Hauptband noch nicht da, dann übernimmt der vorderste Zweig.
  - Unterschiedliche Geländehöhen löst die Bandhöhe über dem Boden (eine Höhe überall). Getrennte Ebenen gibt es bewusst nicht, weil nichts kreuzt.
