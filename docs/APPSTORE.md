# App Store – Vorbereitung (Stand 09.11.2026)

Alles ist **vorbereitet, aber ausgeschaltet**. Solange das Scripting-Define `LC_STORE` fehlt, ändert sich am Spiel nichts:
keine nativen Aufrufe, keine neuen Knöpfe, normale Builds wie bisher.

## Was schon im Projekt liegt

| Teil | Datei | Inhalt |
|---|---|---|
| Native Apple-Brücke | `Assets/Plugins/iOS/Prepared/LCAppleServices.mm.txt` | Game-Center-Anmeldung, Erfolge, Bestenlisten, Spielstand im Konto (GKSavedGame/iCloud), „Mit Apple anmelden“. Endet auf `.txt`, damit Xcode sie noch nicht baut. |
| C#-Schicht | `Assets/Scripts/Core/Platform.Apple.cs` | `AppleServices` (Aufrufe), `AppleBridge` (Antworten), `GameCenterCloud` (Konto-Speicher), `AppleIds` (alle IDs). |
| Spielstand | `Platform.cs` | Nutzt automatisch `GameCenterCloud`, sobald eingeschaltet. Regel: Konto-Stand nur übernehmen, wenn neuer; lokal bleibt die Sicherung. |
| Erfolge | `Game.Features.cs`, `Game.Secret.cs` | Jede erreichte Stufe wird an Game Center gemeldet, nach der Anmeldung werden alle alten Stufen nachgemeldet. |
| Bestenlisten | `Platform.Apple.cs` | Sterne gesamt, gereinigte Parzellen, Credits (Mio), Putz-Level im Hard Mode – alle 2 min mit dem Konto-Speichern. |
| Einstellungen | `Sheets.cs` | Zeilen „Erfolge in Game Center“, „Bestenlisten“, „Mit Apple anmelden“ – nur sichtbar mit `LC_STORE`. |
| Xcode-Projekt | `Editor/IosBuild.cs` | Trägt mit `LC_STORE` Game Center, iCloud (Schlüssel-Wert + Dokumente) und „Mit Apple anmelden“ samt GameKit/AuthenticationServices ein. |
| Erfolgsliste | `docs/gamecenter_erfolge.csv` | Alle 95 Erfolge mit ID, Name, Beschreibung, Punkten (zusammen genau 1000). |

## Plan von Fabian (11.11.2026)

- Anmeldung als **Einzelperson** (Anbieter im Store = Fabians Name), Bundle-ID `com.fabian.lunarcleanup`, Name „Lunar Cleanup Tycoon“.
- **Zuerst TestFlight** (Beta für Fabian und Freunde), Store-Eintrag später.
- **Noch kein Gewerbe.** Für TestFlight egal. Vor der Veröffentlichung im EU-Store mit Käufen: Gewerbe anmelden und den EU-Händlerstatus (DSA) in App Store Connect angeben – Adresse, Telefon und E-Mail werden dann öffentlich gezeigt (eigene Geschäftsadresse/-nummer überlegen).
- Shop-Produkte (Stand Build 70): `offline24`, `timeskip`, `cargo2`, `xlic`, `sd50`, `skin_gold` (1,50 €), `hardmode` (3,99 €), `supporter` (4,99 €), `coffee` (1,50 €, Verbrauchsartikel ohne Gegenleistung – Trinkgeld an den Entwickler ist per In-App-Kauf erlaubt). Preise später an Apples Preisstufen anpassen.
- Zum Testen von Käufen (Sandbox) braucht es die Vereinbarung „Paid Apps“ mit Steuer- und Bankdaten in App Store Connect – die gibt Fabian selbst ein.

## Einschalten, wenn es so weit ist

1. **Apple Developer Program** (99 €/Jahr) – ohne geht weder Game Center noch TestFlight noch App Store.
2. **Bundle-ID** `com.fabian.lunarcleanup` im Developer-Portal anlegen (oder in `IosBuild.cs` ändern – dann auch `AppleIds.Prefix`).
   Capabilities einschalten: *Game Center*, *iCloud* (mit Container), *Sign in with Apple*.
3. **App Store Connect**: neue App anlegen, unter *Game Center* die Erfolge aus `docs/gamecenter_erfolge.csv` und die 4 Bestenlisten anlegen
   (IDs genau wie unten). Geheim-Erfolge und Geschichts-Erfolge als „versteckt“ markieren.
4. In Unity: `LCAppleServices.mm.txt` → `LCAppleServices.mm` umbenennen, Player Settings → iOS → *Scripting Define Symbols* `LC_STORE` eintragen.
5. iPhone-Build wie gewohnt, in Xcode Team wählen, auf dem Handy testen: Anmeldebanner von Game Center erscheint, Einstellungen zeigen die neuen Zeilen.
6. **TestFlight** hochladen (Xcode → Product → Archive), dann Prüfung.

## Was vor der Veröffentlichung noch fehlt

- **In-App-Käufe**: Die Angebote stehen in `Platform.Offers`, gekauft werden kann noch nichts. Dafür Unity IAP einbinden und die Produkte in App Store Connect anlegen (`offline24`, `timeskip`, `cargo2`, `xlic`, `sd50`, `skin_gold`, `hardmode`, `supporter`).
  „Käufe wiederherstellen“ ist bei Apple Pflicht.
- **Mit Apple anmelden** ist nur nötig, wenn es eine andere Anmeldung (z. B. Google) gibt. Mit nur Game Center kann man es weglassen – vorbereitet ist es trotzdem.
- **Datenschutz**: Datenschutzerklärung (URL) und die Angaben „App-Datenschutz“ in App Store Connect. Das Spiel sammelt selbst keine Daten; Game Center und iCloud laufen über Apple.
- **Altersfreigabe**, Kategorie (Spiele → Simulation/Strategie), Beschreibung, Schlüsselwörter, Support-URL (die Projekt-Webseite).
- **Screenshots** in den aktuell geforderten iPhone-Größen (vor dem Hochladen in App Store Connect nachsehen) – der Foto-Modus des Spiels eignet sich dafür.
- **Testknöpfe** und der Admin-Zugang sind in normalen Builds versteckt – vor dem Hochladen noch einmal prüfen.
- Version und Build stehen in `IosBuild.Version` und `Assets/Resources/Data/build.txt`.

## Bestenlisten

| ID | Name |
|---|---|
| `com.fabian.lunarcleanup.lb.stars` | Sterne gesamt |
| `com.fabian.lunarcleanup.lb.cells` | Parzellen gereinigt |
| `com.fabian.lunarcleanup.lb.hardlvl` | Putz-Level im Hard Mode |
| `com.fabian.lunarcleanup.lb.earned` | Credits verdient (in Millionen) |

## Erfolge (95, zusammen 1000 Punkte)

| ID | Name | Beschreibung | Punkte |
|---|---|---|---|
| `com.fabian.lunarcleanup.ach.cells.b` | Parzellen gereinigt – Bronze | Parzellen gereinigt: 100 Parzellen | 5 |
| `com.fabian.lunarcleanup.ach.cells.s` | Parzellen gereinigt – Silber | Parzellen gereinigt: 1.000 Parzellen | 10 |
| `com.fabian.lunarcleanup.ach.cells.g` | Parzellen gereinigt – Gold | Parzellen gereinigt: 10.000 Parzellen | 15 |
| `com.fabian.lunarcleanup.ach.hand.b` | Von Hand gewischt – Bronze | Von Hand gewischt: 1.000 m² | 5 |
| `com.fabian.lunarcleanup.ach.hand.s` | Von Hand gewischt – Silber | Von Hand gewischt: 10.000 m² | 10 |
| `com.fabian.lunarcleanup.ach.hand.g` | Von Hand gewischt – Gold | Von Hand gewischt: 100.000 m² | 15 |
| `com.fabian.lunarcleanup.ach.unload.b` | Behälter geleert – Bronze | Behälter geleert: 10 mal | 5 |
| `com.fabian.lunarcleanup.ach.unload.s` | Behälter geleert – Silber | Behälter geleert: 100 mal | 10 |
| `com.fabian.lunarcleanup.ach.unload.g` | Behälter geleert – Gold | Behälter geleert: 1.000 mal | 15 |
| `com.fabian.lunarcleanup.ach.combo.b` | Kombo erreicht – Bronze | Kombo erreicht: 3 × | 5 |
| `com.fabian.lunarcleanup.ach.combo.s` | Kombo erreicht – Silber | Kombo erreicht: 6 × | 10 |
| `com.fabian.lunarcleanup.ach.combo.g` | Kombo erreicht – Gold | Kombo erreicht: 10 × | 15 |
| `com.fabian.lunarcleanup.ach.meteor.b` | Meteoriten-Einschläge erlebt – Bronze | Meteoriten-Einschläge erlebt: 10 Einschläge | 5 |
| `com.fabian.lunarcleanup.ach.meteor.s` | Meteoriten-Einschläge erlebt – Silber | Meteoriten-Einschläge erlebt: 100 Einschläge | 10 |
| `com.fabian.lunarcleanup.ach.meteor.g` | Meteoriten-Einschläge erlebt – Gold | Meteoriten-Einschläge erlebt: 1.000 Einschläge | 15 |
| `com.fabian.lunarcleanup.ach.reach.b` | Planeten erreicht – Bronze | Planeten erreicht: 3 Planeten | 5 |
| `com.fabian.lunarcleanup.ach.reach.s` | Planeten erreicht – Silber | Planeten erreicht: 9 Planeten | 10 |
| `com.fabian.lunarcleanup.ach.reach.g` | Planeten erreicht – Gold | Planeten erreicht: 18 Planeten | 15 |
| `com.fabian.lunarcleanup.ach.done.b` | Planeten komplett sauber – Bronze | Planeten komplett sauber: 1 Planeten | 5 |
| `com.fabian.lunarcleanup.ach.done.s` | Planeten komplett sauber – Silber | Planeten komplett sauber: 6 Planeten | 10 |
| `com.fabian.lunarcleanup.ach.done.g` | Planeten komplett sauber – Gold | Planeten komplett sauber: 18 Planeten | 15 |
| `com.fabian.lunarcleanup.ach.stage.b` | Höchste Basisstufe – Bronze | Höchste Basisstufe: 3 | 5 |
| `com.fabian.lunarcleanup.ach.stage.s` | Höchste Basisstufe – Silber | Höchste Basisstufe: 5 | 10 |
| `com.fabian.lunarcleanup.ach.stage.g` | Höchste Basisstufe – Gold | Höchste Basisstufe: 5 | 15 |
| `com.fabian.lunarcleanup.ach.expand.b` | Ausbaustufen – Bronze | Ausbaustufen: 1 Stufen | 5 |
| `com.fabian.lunarcleanup.ach.expand.s` | Ausbaustufen – Silber | Ausbaustufen: 5 Stufen | 10 |
| `com.fabian.lunarcleanup.ach.expand.g` | Ausbaustufen – Gold | Ausbaustufen: 20 Stufen | 15 |
| `com.fabian.lunarcleanup.ach.fleet.b` | Fahrzeuge gleichzeitig – Bronze | Fahrzeuge gleichzeitig: 3 Fahrzeuge | 5 |
| `com.fabian.lunarcleanup.ach.fleet.s` | Fahrzeuge gleichzeitig – Silber | Fahrzeuge gleichzeitig: 10 Fahrzeuge | 10 |
| `com.fabian.lunarcleanup.ach.fleet.g` | Fahrzeuge gleichzeitig – Gold | Fahrzeuge gleichzeitig: 15 Fahrzeuge | 15 |
| `com.fabian.lunarcleanup.ach.vlvl.b` | Höchste Fahrzeugstufe – Bronze | Höchste Fahrzeugstufe: 2 | 5 |
| `com.fabian.lunarcleanup.ach.vlvl.s` | Höchste Fahrzeugstufe – Silber | Höchste Fahrzeugstufe: 4 | 10 |
| `com.fabian.lunarcleanup.ach.vlvl.g` | Höchste Fahrzeugstufe – Gold | Höchste Fahrzeugstufe: 6 | 15 |
| `com.fabian.lunarcleanup.ach.lifts.b` | Drohnen-Abholungen – Bronze | Drohnen-Abholungen: 25 Flüge | 5 |
| `com.fabian.lunarcleanup.ach.lifts.s` | Drohnen-Abholungen – Silber | Drohnen-Abholungen: 250 Flüge | 10 |
| `com.fabian.lunarcleanup.ach.lifts.g` | Drohnen-Abholungen – Gold | Drohnen-Abholungen: 2.500 Flüge | 15 |
| `com.fabian.lunarcleanup.ach.air.b` | Lastdrohnen-Flüge – Bronze | Lastdrohnen-Flüge: 5 Flüge | 5 |
| `com.fabian.lunarcleanup.ach.air.s` | Lastdrohnen-Flüge – Silber | Lastdrohnen-Flüge: 50 Flüge | 10 |
| `com.fabian.lunarcleanup.ach.air.g` | Lastdrohnen-Flüge – Gold | Lastdrohnen-Flüge: 500 Flüge | 15 |
| `com.fabian.lunarcleanup.ach.bricks.b` | Bricks gepresst – Bronze | Bricks gepresst: 100 Bricks | 5 |
| `com.fabian.lunarcleanup.ach.bricks.s` | Bricks gepresst – Silber | Bricks gepresst: 10.000 Bricks | 10 |
| `com.fabian.lunarcleanup.ach.bricks.g` | Bricks gepresst – Gold | Bricks gepresst: 1 Mio Bricks | 15 |
| `com.fabian.lunarcleanup.ach.barren.b` | Barren gegossen – Bronze | Barren gegossen: 50 Barren | 5 |
| `com.fabian.lunarcleanup.ach.barren.s` | Barren gegossen – Silber | Barren gegossen: 5.000 Barren | 10 |
| `com.fabian.lunarcleanup.ach.barren.g` | Barren gegossen – Gold | Barren gegossen: 500.000 Barren | 15 |
| `com.fabian.lunarcleanup.ach.rockets.b` | Raketen gestartet – Bronze | Raketen gestartet: 10 Starts | 5 |
| `com.fabian.lunarcleanup.ach.rockets.s` | Raketen gestartet – Silber | Raketen gestartet: 100 Starts | 10 |
| `com.fabian.lunarcleanup.ach.rockets.g` | Raketen gestartet – Gold | Raketen gestartet: 1.000 Starts | 15 |
| `com.fabian.lunarcleanup.ach.earned.b` | Credits verdient – Bronze | Credits verdient: 1 Mio Cr | 5 |
| `com.fabian.lunarcleanup.ach.earned.s` | Credits verdient – Silber | Credits verdient: 1 Mrd Cr | 10 |
| `com.fabian.lunarcleanup.ach.earned.g` | Credits verdient – Gold | Credits verdient: 1000 Mrd Cr | 15 |
| `com.fabian.lunarcleanup.ach.sold.b` | Waren verkauft – Bronze | Waren verkauft: 100.000 Cr | 5 |
| `com.fabian.lunarcleanup.ach.sold.s` | Waren verkauft – Silber | Waren verkauft: 100 Mio Cr | 10 |
| `com.fabian.lunarcleanup.ach.sold.g` | Waren verkauft – Gold | Waren verkauft: 100 Mrd Cr | 15 |
| `com.fabian.lunarcleanup.ach.finds.b` | Funde gemacht – Bronze | Funde gemacht: 1 Funde | 5 |
| `com.fabian.lunarcleanup.ach.finds.s` | Funde gemacht – Silber | Funde gemacht: 25 Funde | 10 |
| `com.fabian.lunarcleanup.ach.finds.g` | Funde gemacht – Gold | Funde gemacht: 100 Funde | 15 |
| `com.fabian.lunarcleanup.ach.albums.b` | Fundalben komplett – Bronze | Fundalben komplett: 1 Alben | 5 |
| `com.fabian.lunarcleanup.ach.albums.s` | Fundalben komplett – Silber | Fundalben komplett: 5 Alben | 10 |
| `com.fabian.lunarcleanup.ach.albums.g` | Fundalben komplett – Gold | Fundalben komplett: 18 Alben | 15 |
| `com.fabian.lunarcleanup.ach.wonders.b` | Entdeckungen gesehen – Bronze | Entdeckungen gesehen: 1 Orte | 5 |
| `com.fabian.lunarcleanup.ach.wonders.s` | Entdeckungen gesehen – Silber | Entdeckungen gesehen: 9 Orte | 10 |
| `com.fabian.lunarcleanup.ach.wonders.g` | Entdeckungen gesehen – Gold | Entdeckungen gesehen: 18 Orte | 15 |
| `com.fabian.lunarcleanup.ach.flags.b` | Flaggen gesammelt – Bronze | Flaggen gesammelt: 10 Flaggen | 5 |
| `com.fabian.lunarcleanup.ach.flags.s` | Flaggen gesammelt – Silber | Flaggen gesammelt: 60 Flaggen | 10 |
| `com.fabian.lunarcleanup.ach.flags.g` | Flaggen gesammelt – Gold | Flaggen gesammelt: 250 Flaggen | 15 |
| `com.fabian.lunarcleanup.ach.allflags.b` | Alle Flaggen eines Planeten – Bronze | Alle Flaggen eines Planeten: 1 Planeten | 5 |
| `com.fabian.lunarcleanup.ach.allflags.s` | Alle Flaggen eines Planeten – Silber | Alle Flaggen eines Planeten: 6 Planeten | 10 |
| `com.fabian.lunarcleanup.ach.allflags.g` | Alle Flaggen eines Planeten – Gold | Alle Flaggen eines Planeten: 18 Planeten | 15 |
| `com.fabian.lunarcleanup.ach.crystals.b` | Leuchtkristalle geerntet – Bronze | Leuchtkristalle geerntet: 25 Kristalle | 5 |
| `com.fabian.lunarcleanup.ach.crystals.s` | Leuchtkristalle geerntet – Silber | Leuchtkristalle geerntet: 250 Kristalle | 10 |
| `com.fabian.lunarcleanup.ach.crystals.g` | Leuchtkristalle geerntet – Gold | Leuchtkristalle geerntet: 2.500 Kristalle | 15 |
| `com.fabian.lunarcleanup.ach.wrecks.b` | Wracks und Trümmer geborgen – Bronze | Wracks und Trümmer geborgen: 1 Bergungen | 5 |
| `com.fabian.lunarcleanup.ach.wrecks.s` | Wracks und Trümmer geborgen – Silber | Wracks und Trümmer geborgen: 10 Bergungen | 10 |
| `com.fabian.lunarcleanup.ach.wrecks.g` | Wracks und Trümmer geborgen – Gold | Wracks und Trümmer geborgen: 50 Bergungen | 15 |
| `com.fabian.lunarcleanup.ach.storms.b` | Stürme überstanden – Bronze | Stürme überstanden: 1 Stürme | 5 |
| `com.fabian.lunarcleanup.ach.storms.s` | Stürme überstanden – Silber | Stürme überstanden: 20 Stürme | 10 |
| `com.fabian.lunarcleanup.ach.storms.g` | Stürme überstanden – Gold | Stürme überstanden: 200 Stürme | 15 |
| `com.fabian.lunarcleanup.ach.geysers.b` | Geysir-Ausbrüche gesehen – Bronze | Geysir-Ausbrüche gesehen: 10 Ausbrüche | 5 |
| `com.fabian.lunarcleanup.ach.geysers.s` | Geysir-Ausbrüche gesehen – Silber | Geysir-Ausbrüche gesehen: 100 Ausbrüche | 10 |
| `com.fabian.lunarcleanup.ach.geysers.g` | Geysir-Ausbrüche gesehen – Gold | Geysir-Ausbrüche gesehen: 1.000 Ausbrüche | 15 |
| `com.fabian.lunarcleanup.ach.reset.b` | Sternenstaub-Neustarts – Bronze | Sternenstaub-Neustarts: 1 Neustarts | 5 |
| `com.fabian.lunarcleanup.ach.reset.s` | Sternenstaub-Neustarts – Silber | Sternenstaub-Neustarts: 3 Neustarts | 10 |
| `com.fabian.lunarcleanup.ach.reset.g` | Sternenstaub-Neustarts – Gold | Sternenstaub-Neustarts: 10 Neustarts | 15 |
| `com.fabian.lunarcleanup.ach.daily.b` | Tagesbonus in Folge – Bronze | Tagesbonus in Folge: 3 Tage | 5 |
| `com.fabian.lunarcleanup.ach.daily.s` | Tagesbonus in Folge – Silber | Tagesbonus in Folge: 5 Tage | 10 |
| `com.fabian.lunarcleanup.ach.daily.g` | Tagesbonus in Folge – Gold | Tagesbonus in Folge: 7 Tage | 15 |
| `com.fabian.lunarcleanup.ach.night.b` | Nachts gespielt – Bronze | Nachts gespielt: 10 min | 5 |
| `com.fabian.lunarcleanup.ach.night.s` | Nachts gespielt – Silber | Nachts gespielt: 60 min | 10 |
| `com.fabian.lunarcleanup.ach.night.g` | Nachts gespielt – Gold | Nachts gespielt: 300 min | 15 |
| `com.fabian.lunarcleanup.story.first` | Etwas im Staub | Das erste verborgene Zeichen auf einem Planeten entdeckt | 20 |
| `com.fabian.lunarcleanup.story.moon` | Die Sternkarte | Das Geheimnis des Mondes vollständig freigelegt | 20 |
| `com.fabian.lunarcleanup.story.half` | Auf der Spur | Auf 9 Planeten ein Zeichen freigelegt | 20 |
| `com.fabian.lunarcleanup.story.all` | Die Route der Wanderer | Auf allen 18 Planeten das Zeichen freigelegt | 20 |
| `com.fabian.lunarcleanup.story.stars` | Sternensammler | Alle 54 Sterne gesammelt | 20 |
