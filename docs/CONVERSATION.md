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
9. Repository angelegt (heute `GhostOFabian/Lunar-Cleanup-Tycoon`, anfangs mit Tippfehler „Tycon“); Unterhaltung in Claude Code überführt (dieses Dokument). Projekt-Webseite mit echten Screenshots.
10. **Spielstand wurde nach Updates zweimal zurückgesetzt.** Alte Spielstände ließen sich im Test immer laden – wahrscheinliche Ursache: Speicher pro Ansicht (Claude-App, Browser, Vorschau, Webseiten-Kopie haben getrennte Speicher) oder ein stillschweigend fehlgeschlagenes Speichern. → v3.6: Cloud-Spielstand im Claude-Konto, Sicherungskopie, Export/Import-Code, schlanker Speicherstand, sichtbare Fehlermeldung.
11. Website: Link zum Code und Entwickler-Infos entfernt (Sorge vor Ideenklau). Hinweis: Repo ist öffentlich.
12. **Steuerung schwergängig:** Beim Wischen blieb der Finger „kleben“, statt dass sich der Planet mitbewegt. → Verlässt der Wisch die Parzelle, wird er zur Kamerabewegung; Wischen am Rand scrollt mit.
13. **Mehr Fahrzeugtypen**, spätere Planeten brauchen andere Fahrzeuge; **Bänder ohne Lücken** an den Ausgängen; **Transportdrohnen für schwere Maschinen** zu neuen schmutzigen Stellen (spät); **neue Energie bis Kernkraft** für die Nachteile der äußeren Planeten. → v3.7.
14. Fahrzeuge sollen sich **gegenseitig bei der Reinigung unterstützen**; manche nutzen **Ladestationen für ihre Akkus**; der Spieler bekommt ein **ausbaufähiges Tragevolumen**, das er selbst in Trichter oder Entladestation leert. → v3.8.
15. Bandausgänge prüfen – Rohstofflager → Verkauf war nicht verbunden; Bricks clippten durch die schwarz-gelben Übergabekästen. → v3.8.1.
16. Gebäudenamen ausblenden (clippten mit dem Boden), nur beim Antippen zeigen. → v3.8.2.
17. Schild am Bauplatz zeigte nur „uplatz“ (Text abgeschnitten). → v3.8.3.
18. Wert der Frachtraum-Ladung sehen (auch beim manuellen Start); das Band Rohstofflager → Verkauf braucht später einen dauerhaften Nutzen, auch gegen überlastete Bänder. → v3.9: Frachtaufträge + Mischfracht-Bonus.
19. **Viel mehr Ziele** und eine Ziel-Übersicht wie bei den Tageszielen; **Tag und Nacht** mit Scheinwerfern und Lichtkegeln an den Fahrzeugen; nachts laufen Fahrzeuge und Maschinen aus dem am Tag gefüllten Speicher oder aus dem Akkupack. → v4.0.
20. Früh ein **Fahrzeug, das zum Spieler fährt, wenn der Sammelbehälter voll ist**; man leert den ganzen Behälter hinein, es fährt zur Entladestation. → v4.1: Sammelwagen.
21. Beim manuellen Reinigen nachts eine **Lichtquelle**; das **Bild verschiebt sich störend**, wenn man beim Halten außerhalb der Fläche wischt. → v4.2.
22. **Bessere Beleuchtung der Basis**: Laternen, die nachts auch die Gebäude anstrahlen. → v4.3.
23. Laternen **besser positionieren**; **Mond 100 % sauber – was jetzt?** Fertige Planeten brauchen einen Grund, die Basis weiter auszubauen. → v4.4: Tiefenabbau mit Bohrtürmen, Laternen paarweise.
24. Man sieht nicht, dass die Bohrtürme bohren; besser ein **kleines Lager am Bohrturm, das die Fahrzeuge anfahren und abholen**. → v4.5.
25. Silo und Klotz am Bohrturm clippen in die Turmstreben; **Fahrzeuge sollen beim Herauszoomen orange blinken**. → v4.6.
26. **Meteoriten sollen viel größere Flächen und viel mehr verschmutzen**, gern öfter, vor allem auf 100 % sauberen Planeten. → v4.7.
27. Jeder Planet kommt irgendwann an den Punkt, an dem er nicht mehr ausgebaut werden kann – es braucht **immer eine Möglichkeit, nach 100 % weiter auszubauen**; **Reisen soll ein nahtloser Wechsel sein**. → v4.8.
28. Reisen noch **ruckelig**, soll flüssiger laufen. → v4.8.1.
29. **Idle-Test in den Einstellungen muss weg** (macht das Spiel kaputt); dafür soll Idle besser laufen, wenn der Bildschirm aus ist oder das Fenster nicht im Fokus. → v4.9.
30. **Beim Antippen eines Bandes sehen, was befördert wird**; Staub soll ab einem Punkt nicht mehr roh transportiert, sondern erst zu höheren Ressourcen umgewandelt werden – die Kette läuft von klein nach groß, im Frachtraum landet immer das Endprodukt, außer es gibt keine höhere Stufe. → v5.0.
31. **Jeder Planet braucht eigene Ziele**, die abgeschlossen werden können. → v5.1.
32. Auf dem Mond werden nach 100 % **nur noch Bricks** hergestellt, alle anderen Bänder stehen – Produktionskette so korrigieren, dass **alle Bänder dauerhaft zu tun haben**, auf allen Planeten. → v5.2.
33. Fahrzeuge fahren teilweise noch durch Gebäude, **Drohnen fliegen durch Gebäude** und laden nicht korrekt; **Drohnen sollen ihre Ladung bei den Entladestationen abgeben**. → v5.3.
34. **Presse upgraden**, damit sie mit dem Input mithält; **Bohrtürme nach Maximalstufe zu Erzbaggern** umbauen; **Gebäude nach ausgereizten Upgrades eine weitere, sichtbar größere Stufe**; **Gebäude und Fahrzeuge je Planet in eigenen Farben**. → v5.4.
35. Bei der Presse auch das **Volumen** anpassen (mehr pro Durchgang, nicht nur schneller). → v5.5.
36. Die Presse wirkt **viel zu schnell** – sie darf schnell sein, soll aber nicht so aussehen; alles soll **langfristig klar und verständlich** bleiben, **Upgrades sinnvoll**. → v5.6.
37. **Größen prüfen und Bänder anpassen**, wenn Gebäudestufen steigen; **Außenlager-Pipeline geht neben das Rohstofflager** und pumpt scheinbar nicht hinein. → v5.7.
38. **Pumpen der Pipeline upgradebar** machen – das Außenlager ist immer zu schnell voll. → v5.8.
39. Fahrzeuge **stehen fest am Außenlager**, statt weiterzufahren; die **Pipeline sitzt neben dem Außenlager** statt darin. → v5.9.
40. Ausführlicher Auftrag „Gameplay, Progression und Langzeitmotivation verbessern“ (Analyse, Maßnahmenplan, risikoarme Umsetzung, Spielstände erhalten). → v6.0, siehe `docs/LANGZEIT.md`.
41. Ausführlicher Auftrag „Gameplay-Überarbeitung“: 4 feste Außenlager-Plätze, größere Außenlager mit Upgrade und 80 %-Mechanik, Drohnen holen überall ab, Fahrzeug-Lebensdauer mit max. 2 Reparaturen, Störungen bei Bohrtürmen/Baggern mit rotem Licht, sichtbare Naturereignisse. → v6.1.
42. Fahrzeuge **dürfen offline zerstört werden** (mit Meldung); **Drohnen mehr Platz**; Upgrades von alten Planeten sollen nicht auf neue übertragen werden (sonst monoton) – Rückfrage: **pro Planet neu**; **8 weitere Planeten**, **andere Produktionen/Rezepte/Endprodukte je Planet**, ein **extrem verstrahlter Planet**. → v6.2.
43. Jeder Planet soll **vom Aufbau anders aussehen**, nicht nur anders produzieren. → v6.3.
44. Unity: erst ein MonoBehaviour, dann „du legst alles in Unity für mich an“ (direkt auf seinem PC eingerichtet), dann **runde Planeten, gleiches Menü, gleiches Design, bessere Grafik und Modelle**.
45. „Nutz meinen PC und bau alles in Unity so ein, dass ich später eine spielbare Version habe.“ → Unity-Version 0.1.

## Was der Spieler mag / nicht mag
- Mag: sichtbaren Fortschritt, Automatisierung, die sich selbst trägt, mehrere Planeten parallel, klare Rückmeldungen (Farben, Ampel, Begründungen).
- Mag nicht: Bezahlschranken, die blockieren; künstliche Limits beim Handgerät; Clipping/Überlappen; Dinge, die zu früh kommen (Mars-Forschung).
- Kommunikation auf Deutsch, kurz und konkret.

## Offene Ideen (noch nicht gebaut)
- Balancing der späteren Planeten (Upgrade-Kosten im Verhältnis zu Planetenpreisen; Sternenstaub-Formel eventuell strenger).
- Unity: restliche Systeme (Energie, Tag/Nacht, Stürme, Forschung, Drohnen, Außenlager, Tiefenabbau) und Handy-Build.
- Echter Gerätetest der 3D-Optik (Rahmen, Kuppel, Drohnen, Pipelines, Raketenflug, Wegfindung) – headless nicht prüfbar.
