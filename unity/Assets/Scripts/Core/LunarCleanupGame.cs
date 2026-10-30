// Lunar Cleanup Tycoon – grundlegende Spiellogik für Unity (Mond, Einstieg)
// Übernommen aus prototype/index.html: Reinigen per Finger/Maus, Sammelbehälter (60), Rohstofflager,
// Staubpresse, Raketenverkauf, Reinigungsbuggys, Upgrades, Speichern und Offline-Fortschritt.
//
// Aufbau:
//   GameData   – reine Daten, wird als JSON gespeichert (saveVersion)
//   Simulation – reines C#, keine Unity-Objekte. Tick(dt) ist die EINZIGE Simulation:
//                live jedes Frame und offline in 1-s-Schritten (wie simStep im Prototyp).
//   LunarCleanupGame (MonoBehaviour) – baut die Szene, liest Eingaben, zeigt alles an.
//
// Voraussetzungen: Unity 6, Input System Package, TextMeshPro.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LunarCleanup.Core
{
    public enum Mat { Staub = 0, Metall = 1, Kristall = 2 }
    public enum Prod { StaubBrick = 0, MetallBrick = 1 }

    public static class Balance
    {
        // Werte aus dem Prototyp (MATS, DIRT, VT.buggy, UPG)
        public static readonly string[] MatName = { "Staub", "Metallschrott", "Kristallsplitter" };
        public static readonly string[] ProdName = { "Staub-Brick", "Metall-Brick" };
        public static readonly float[] MatValue = { 1f, 2f, 6f };
        public static readonly float[] ProdValue = { 14f, 26f };
        public static readonly float[] DirtAmount = { 120f, 100f, 60f };   // Menge pro Parzelle
        public static readonly float[] DirtHardness = { 1f, 1.8f, 3.4f };  // langsamer bei harten Sorten
        public static readonly float[] DirtChance = { 0.40f, 0.46f, 0.14f };

        public const float HandCap = 60f;          // Sammelbehälter
        public const float HandRate = 8f;          // Einheiten pro Sekunde beim Wischen
        public const float PressCost = 300f;
        public const float PressIn = 10f;          // 10 Rohstoff -> 1 Brick
        public static readonly float[] PressTime = { 2f, 2.5f };
        public const float RocketTime = 12f;       // Sekunden Flug
        public const float StationRate = 5f;       // Entladen pro Sekunde
        public const float OfflineCapHours = 8f;

        // Reinigungsbuggy
        public const float BuggyCost = 180f, BuggySpeed = 4.4f, BuggyPower = 1.8f, BuggyTank = 40f;
        public const int MaxVehicles = 6;

        // Upgrades: Kosten = base * grow^Stufe
        public static readonly string[] UpName = { "Turbo-Saugkraft", "Größeres Rohstofflager", "Leichtbau-Fahrwerk", "Doppelbürsten", "Größerer Frachtraum" };
        public static readonly float[] UpBase = { 60f, 90f, 200f, 250f, 300f };
        public static readonly float[] UpGrow = { 1.8f, 1.7f, 1.7f, 1.6f, 1.7f };
        public static readonly int[] UpMax = { 10, 14, 20, 30, 15 };
        public const int UpPower = 0, UpRoh = 1, UpVSpeed = 2, UpVPower = 3, UpSell = 4;
    }

    [Serializable]
    public class VehicleData
    {
        public float x, z;       // Position in Metern
        public float tank;       // geladene Menge
        public int tankMat;      // Sorte im Tank (einfach gehalten: eine Sorte pro Fahrt)
        public int target = -1;  // Ziel-Parzelle
        public int mode;         // 0 sucht, 1 fährt hin, 2 reinigt, 3 fährt zur Station, 4 entlädt
    }

    [Serializable]
    public class GameData
    {
        public int saveVersion = 1;
        public double credits = 0;
        public float[] dirt;            // Restschmutz je Parzelle
        public int[] dirtType;          // Mat je Parzelle
        public float[] hand = new float[3];
        public float[] roh = new float[3];
        public float[] prod = new float[2];
        public bool pressBuilt;
        public float pressProgress;
        public int pressRecipe = -1;
        public float[] pressIn = new float[2];
        public float[] rocketCargo = new float[5]; // 3 Rohstoffe + 2 Produkte
        public float rocketFlight = -1f;           // < 0: steht auf der Rampe
        public int[] upgrades = new int[5];
        public List<VehicleData> vehicles = new List<VehicleData>();
        public long lastActiveUnix;
    }

    /// <summary>Reine Spiellogik. Kennt keine GameObjects – dadurch testbar und offline identisch.</summary>
    public class Simulation
    {
        public readonly GameData D;
        public readonly int Size;              // Parzellen pro Seite
        public readonly float CellSize;        // Meter pro Parzelle
        public readonly Vector2 StationPos;    // Entladestation (Mitte)
        public event Action<string> OnMessage; // Meldungen für die UI

        public Simulation(GameData data, int size, float cellSize)
        {
            D = data; Size = size; CellSize = cellSize; StationPos = Vector2.zero;
            if (D.dirt == null || D.dirt.Length != size * size) NewField();
        }

        void NewField()
        {
            var rnd = new System.Random(1337);
            int n = Size * Size; D.dirt = new float[n]; D.dirtType = new int[n];
            for (int i = 0; i < n; i++)
            {
                if (IsBase(i)) { D.dirt[i] = 0; D.dirtType[i] = 0; continue; }
                double r = rnd.NextDouble(); int t = r < Balance.DirtChance[0] ? 0 : r < Balance.DirtChance[0] + Balance.DirtChance[1] ? 1 : 2;
                D.dirtType[i] = t; D.dirt[i] = Balance.DirtAmount[t];
            }
        }

        // ---- Hilfen ----
        public bool IsBase(int i) { Vector2 p = CellPos(i); return p.magnitude < CellSize * 1.6f; }
        public Vector2 CellPos(int i) { int x = i % Size, y = i / Size; float o = (Size - 1) * 0.5f; return new Vector2((x - o) * CellSize, (y - o) * CellSize); }
        public float UpCost(int u) => Balance.UpBase[u] * Mathf.Pow(Balance.UpGrow[u], D.upgrades[u]);
        public float HandPower => 1f + 0.3f * D.upgrades[Balance.UpPower];
        public float RohCap => 150f * Mathf.Pow(1.5f, D.upgrades[Balance.UpRoh]);
        public float ProdCap => 40f;
        public float SellCap => 120f * Mathf.Pow(1.45f, D.upgrades[Balance.UpSell]);
        public float VSpeed => Balance.BuggySpeed * (1f + 0.15f * D.upgrades[Balance.UpVSpeed]);
        public float VPower => Balance.BuggyPower * (1f + 0.25f * D.upgrades[Balance.UpVPower]);
        public float VehicleCost => Balance.BuggyCost * Mathf.Pow(1.4f, D.vehicles.Count);
        public float Sum(float[] a) { float s = 0; foreach (var v in a) s += v; return s; }
        public float HandSum => Sum(D.hand);
        public float RohSum => Sum(D.roh);
        public float ProdSum => Sum(D.prod);
        public int CleanCount { get { int c = 0; for (int i = 0; i < D.dirt.Length; i++) if (D.dirt[i] <= 0.01f) c++; return c; } }

        // ---- Handreinigung (nur live, vom Finger) ----
        public float CleanByHand(int cell, float dt)
        {
            if (cell < 0 || cell >= D.dirt.Length || D.dirt[cell] <= 0) return 0;
            float space = Balance.HandCap - HandSum; if (space <= 0) return 0;
            int t = D.dirtType[cell];
            float a = Mathf.Min(D.dirt[cell], space, Balance.HandRate * HandPower / Balance.DirtHardness[t] * dt);
            D.dirt[cell] -= a; D.hand[t] += a;
            if (D.dirt[cell] <= 0.01f) { D.dirt[cell] = 0; D.credits += 5 * Balance.DirtAmount[t] / 20f; OnMessage?.Invoke("Parzelle sauber!"); }
            return a;
        }

        public bool EmptyHand()
        {
            float space = RohCap - RohSum; if (space <= 0.01f) { OnMessage?.Invoke("Rohstofflager ist voll"); return false; }
            for (int m = 0; m < 3; m++) { float a = Mathf.Min(D.hand[m], space); D.hand[m] -= a; D.roh[m] += a; space -= a; }
            return true;
        }

        // ---- Käufe ----
        public bool Buy(float cost) { if (D.credits < cost) { OnMessage?.Invoke("Zu wenig Credits"); return false; } D.credits -= cost; return true; }
        public bool BuyUpgrade(int u) { if (D.upgrades[u] >= Balance.UpMax[u]) return false; if (!Buy(UpCost(u))) return false; D.upgrades[u]++; return true; }
        public bool BuildPress() { if (D.pressBuilt || !Buy(Balance.PressCost)) return false; D.pressBuilt = true; OnMessage?.Invoke("Staubpresse gebaut"); return true; }
        public bool BuyVehicle()
        {
            if (D.vehicles.Count >= Balance.MaxVehicles) { OnMessage?.Invoke("Alle Fahrzeugplätze belegt"); return false; }
            if (!Buy(VehicleCost)) return false;
            D.vehicles.Add(new VehicleData { x = StationPos.x + 2f, z = StationPos.y }); return true;
        }
        public bool LaunchRocket()
        {
            if (D.rocketFlight >= 0 || Sum(D.rocketCargo) < 1) return false;
            D.rocketFlight = 0; return true;
        }

        /// <summary>Die einzige Simulation. Live: dt = Frame-Zeit. Offline: viele Aufrufe mit dt = 1.</summary>
        public void Tick(float dt)
        {
            for (int i = 0; i < D.vehicles.Count; i++) VehicleStep(D.vehicles[i], dt);
            PressStep(dt);
            RocketStep(dt);
        }

        void VehicleStep(VehicleData v, float dt)
        {
            Vector2 p = new Vector2(v.x, v.z);
            switch (v.mode)
            {
                case 0: // nächste schmutzige Parzelle suchen
                    {
                        int best = -1; float bd = float.MaxValue;
                        for (int i = 0; i < D.dirt.Length; i++)
                        {
                            if (D.dirt[i] <= 0) continue;
                            if (v.tank > 0 && D.dirtType[i] != v.tankMat) continue;
                            if (Claimed(i, v)) continue;
                            float d = (CellPos(i) - p).sqrMagnitude; if (d < bd) { bd = d; best = i; }
                        }
                        if (best < 0) { v.mode = v.tank > 0 ? 3 : 0; break; }
                        v.target = best; v.mode = 1; break;
                    }
                case 1: // hinfahren
                    if (MoveTo(v, CellPos(v.target), dt)) v.mode = 2;
                    if (D.dirt[v.target] <= 0) v.mode = 0;
                    break;
                case 2: // reinigen
                    {
                        int t = D.dirtType[v.target]; if (v.tank <= 0) v.tankMat = t;
                        float a = Mathf.Min(D.dirt[v.target], Balance.BuggyTank - v.tank, VPower / Balance.DirtHardness[t] * dt);
                        D.dirt[v.target] -= a; v.tank += a;
                        if (D.dirt[v.target] <= 0.01f) { D.dirt[v.target] = 0; D.credits += 5 * Balance.DirtAmount[t] / 20f; v.mode = v.tank >= Balance.BuggyTank * 0.7f ? 3 : 0; }
                        else if (v.tank >= Balance.BuggyTank - 0.01f) v.mode = 3;
                        break;
                    }
                case 3: // zur Entladestation
                    if (MoveTo(v, StationPos + new Vector2(2f, 0), dt)) v.mode = 4;
                    break;
                case 4: // entladen in das Rohstofflager
                    {
                        float space = RohCap - RohSum;
                        float a = Mathf.Min(v.tank, space, Balance.StationRate * dt);
                        v.tank -= a; D.roh[v.tankMat] += a;
                        if (v.tank <= 0.01f) { v.tank = 0; v.mode = 0; }
                        break;
                    }
            }
        }

        bool Claimed(int cell, VehicleData self)
        {
            foreach (var o in D.vehicles) if (o != self && o.target == cell && (o.mode == 1 || o.mode == 2)) return true;
            return false;
        }

        bool MoveTo(VehicleData v, Vector2 tgt, float dt)
        {
            Vector2 p = new Vector2(v.x, v.z), d = tgt - p; float step = VSpeed * dt;
            if (d.magnitude <= step) { v.x = tgt.x; v.z = tgt.y; return true; }
            p += d.normalized * step; v.x = p.x; v.z = p.y; return false;
        }

        void PressStep(float dt)
        {
            if (!D.pressBuilt) return;
            // Rohstofflager -> Presse (Staub und Metall; Kristall geht direkt zur Rakete)
            for (int r = 0; r < 2; r++)
            {
                float a = Mathf.Min(D.roh[r], 30f - D.pressIn[r], 6f * dt);
                if (a > 0) { D.roh[r] -= a; D.pressIn[r] += a; }
            }
            if (D.pressRecipe < 0)
            {
                if (ProdSum + 1 > ProdCap) return;
                int pick = D.pressIn[0] >= D.pressIn[1] ? 0 : 1;
                if (D.pressIn[pick] < Balance.PressIn) pick = 1 - pick;
                if (D.pressIn[pick] < Balance.PressIn) return;
                D.pressRecipe = pick; D.pressProgress = 0;
            }
            D.pressProgress += dt;
            if (D.pressProgress >= Balance.PressTime[D.pressRecipe])
            {
                D.pressIn[D.pressRecipe] -= Balance.PressIn; D.prod[D.pressRecipe] += 1;
                D.pressRecipe = -1; D.pressProgress = 0;
            }
        }

        void RocketStep(float dt)
        {
            if (D.rocketFlight >= 0)
            {
                D.rocketFlight += dt;
                if (D.rocketFlight >= Balance.RocketTime)
                {
                    double val = 0;
                    for (int m = 0; m < 3; m++) val += D.rocketCargo[m] * Balance.MatValue[m];
                    for (int p = 0; p < 2; p++) val += D.rocketCargo[3 + p] * Balance.ProdValue[p];
                    D.credits += val; Array.Clear(D.rocketCargo, 0, D.rocketCargo.Length); D.rocketFlight = -1;
                    OnMessage?.Invoke("Rakete verkauft: +" + Mathf.RoundToInt((float)val) + " Cr");
                }
                return;
            }
            // beladen: Produkte immer, Rohstoffe nur, wenn keine Presse sie veredelt (Kristall immer)
            float space = SellCap - Sum(D.rocketCargo), rate = 20f * dt;
            for (int p = 0; p < 2 && space > 0; p++) { float a = Mathf.Min(Mathf.Floor(D.prod[p]), space, rate); D.prod[p] -= a; D.rocketCargo[3 + p] += a; space -= a; }
            for (int m = 0; m < 3 && space > 0; m++)
            {
                if (D.pressBuilt && m < 2 && RohSum < RohCap * 0.97f) continue;
                float a = Mathf.Min(D.roh[m], space, rate); D.roh[m] -= a; D.rocketCargo[m] += a; space -= a;
            }
            if (Sum(D.rocketCargo) >= SellCap - 0.01f) D.rocketFlight = 0; // voll: startet von selbst
        }
    }

    /// <summary>Verbindet die Simulation mit Szene, Eingabe und UI.</summary>
    public class LunarCleanupGame : MonoBehaviour
    {
        [Header("Feld")]
        public int gridSize = 12;
        public float cellSize = 2f;
        public Material parcelMaterial;     // optional, sonst Standard-Material
        public Color dirtyColor = new Color(0.36f, 0.34f, 0.30f);
        public Color cleanColor = new Color(0.85f, 0.87f, 0.90f);

        [Header("Szene")]
        public Camera mainCamera;
        public Transform stationModel;      // optional: Modell der Entladestation
        public GameObject vehiclePrefab;    // optional: Modell des Buggys

        [Header("UI – Texte (TextMeshPro)")]
        public TMP_Text creditsText;
        public TMP_Text handText;
        public TMP_Text rohText;
        public TMP_Text prodText;
        public TMP_Text progressText;
        public TMP_Text messageText;

        [Header("UI – Knöpfe")]
        public Button emptyHandButton;
        public Button launchRocketButton;
        public Button buyVehicleButton;
        public Button buildPressButton;
        public Button upgradePowerButton;
        public Button upgradeRohButton;
        public Button upgradeVehicleButton;

        const string SaveKey = "lunar-cleanup-tycoon-unity-v1";
        Simulation sim;
        Renderer[] parcels;
        readonly List<Transform> vehicleViews = new List<Transform>();
        float messageTimer, saveTimer;

        void Start()
        {
            if (!mainCamera) mainCamera = Camera.main;
            GameData data = Load() ?? new GameData();
            sim = new Simulation(data, gridSize, cellSize);
            sim.OnMessage += ShowMessage;
            BuildField();
            WireButtons();
            CatchUpOffline();
        }

        // ---------- Szene ----------
        void BuildField()
        {
            parcels = new Renderer[gridSize * gridSize];
            var root = new GameObject("Parzellen").transform;
            for (int i = 0; i < parcels.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);   // hat einen BoxCollider für den Raycast
                go.name = "Parzelle " + i; go.transform.SetParent(root);
                Vector2 p = sim.CellPos(i);
                go.transform.position = new Vector3(p.x, -0.1f, p.y);
                go.transform.localScale = new Vector3(cellSize * 0.96f, 0.2f, cellSize * 0.96f);
                var r = go.GetComponent<Renderer>(); if (parcelMaterial) r.sharedMaterial = parcelMaterial;
                parcels[i] = r;
            }
            if (!stationModel)
            {
                var st = GameObject.CreatePrimitive(PrimitiveType.Cylinder); st.name = "Entladestation";
                st.transform.position = new Vector3(sim.StationPos.x, 0.8f, sim.StationPos.y); st.transform.localScale = new Vector3(2.2f, 0.8f, 2.2f);
                Destroy(st.GetComponent<Collider>()); stationModel = st.transform;
            }
        }

        void SyncVehicleViews()
        {
            while (vehicleViews.Count < sim.D.vehicles.Count)
            {
                GameObject go = vehiclePrefab ? Instantiate(vehiclePrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Buggy " + (vehicleViews.Count + 1);
                if (!vehiclePrefab) { go.transform.localScale = new Vector3(0.8f, 0.5f, 1.2f); go.GetComponent<Renderer>().material.color = new Color(1f, 0.75f, 0.3f); Destroy(go.GetComponent<Collider>()); }
                vehicleViews.Add(go.transform);
            }
            for (int i = 0; i < vehicleViews.Count; i++)
            {
                var v = sim.D.vehicles[i]; var t = vehicleViews[i];
                Vector3 np = new Vector3(v.x, 0.35f, v.z), d = np - t.position; d.y = 0;
                if (d.sqrMagnitude > 1e-6f) t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(d), 10f * Time.deltaTime);
                t.position = np;
            }
        }

        void PaintParcels()
        {
            for (int i = 0; i < parcels.Length; i++)
            {
                float full = Balance.DirtAmount[sim.D.dirtType[i]];
                float f = sim.IsBase(i) ? 0 : Mathf.Clamp01(sim.D.dirt[i] / full);
                parcels[i].material.color = Color.Lerp(cleanColor, dirtyColor, f);
            }
        }

        // ---------- Ablauf ----------
        void Update()
        {
            HandleInput();
            sim.Tick(Time.deltaTime);
            SyncVehicleViews();
            PaintParcels();
            UpdateUI();
            saveTimer += Time.deltaTime; if (saveTimer > 10f) { saveTimer = 0; Save(); }
            if (messageTimer > 0) { messageTimer -= Time.deltaTime; if (messageTimer <= 0 && messageText) messageText.text = ""; }
        }

        void HandleInput()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.isPressed || !mainCamera) return;
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return; // Finger auf einem Knopf
            Ray ray = mainCamera.ScreenPointToRay(pointer.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f)) return;
            int cell = Array.IndexOf(parcels, hit.collider.GetComponent<Renderer>());
            if (cell >= 0 && sim.CleanByHand(cell, Time.deltaTime) <= 0 && sim.HandSum >= Balance.HandCap - 0.01f)
                ShowMessage("Sammelbehälter voll – leeren!");
        }

        // ---------- UI ----------
        void WireButtons()
        {
            On(emptyHandButton, () => sim.EmptyHand());
            On(launchRocketButton, () => { if (!sim.LaunchRocket()) ShowMessage("Rakete ist leer oder unterwegs"); });
            On(buyVehicleButton, () => sim.BuyVehicle());
            On(buildPressButton, () => sim.BuildPress());
            On(upgradePowerButton, () => sim.BuyUpgrade(Balance.UpPower));
            On(upgradeRohButton, () => sim.BuyUpgrade(Balance.UpRoh));
            On(upgradeVehicleButton, () => sim.BuyUpgrade(Balance.UpVPower));
        }

        // Unity-Objekte nie mit ?. prüfen (nicht zugewiesene Felder sind im Editor ein "Fake-Null")
        static void On(Button b, UnityAction a) { if (b) b.onClick.AddListener(a); }

        void UpdateUI()
        {
            var D = sim.D;
            Set(creditsText, Fmt(D.credits) + " Cr");
            Set(handText, "Behälter " + Mathf.FloorToInt(sim.HandSum) + "/" + Balance.HandCap);
            Set(rohText, "Roh " + Mathf.FloorToInt(sim.RohSum) + "/" + Mathf.FloorToInt(sim.RohCap));
            Set(prodText, "Bricks " + Mathf.FloorToInt(sim.ProdSum) + (D.rocketFlight >= 0 ? " · Rakete fliegt" : " · Rakete " + Mathf.FloorToInt(sim.Sum(D.rocketCargo)) + "/" + Mathf.FloorToInt(sim.SellCap)));
            Set(progressText, sim.CleanCount + "/" + D.dirt.Length + " Parzellen sauber");
            Label(emptyHandButton, "Behälter leeren", sim.HandSum >= 1);
            Label(launchRocketButton, "Rakete starten", D.rocketFlight < 0 && sim.Sum(D.rocketCargo) >= 1);
            Label(buyVehicleButton, "Buggy kaufen · " + Fmt(sim.VehicleCost), D.credits >= sim.VehicleCost && D.vehicles.Count < Balance.MaxVehicles);
            if (buildPressButton) { buildPressButton.gameObject.SetActive(!D.pressBuilt); Label(buildPressButton, "Staubpresse bauen · " + Fmt(Balance.PressCost), D.credits >= Balance.PressCost); }
            UpLabel(upgradePowerButton, Balance.UpPower);
            UpLabel(upgradeRohButton, Balance.UpRoh);
            UpLabel(upgradeVehicleButton, Balance.UpVPower);
        }

        void UpLabel(Button b, int u)
        {
            bool max = sim.D.upgrades[u] >= Balance.UpMax[u];
            Label(b, Balance.UpName[u] + " " + sim.D.upgrades[u] + (max ? " · max" : " · " + Fmt(sim.UpCost(u))), !max && sim.D.credits >= sim.UpCost(u));
        }

        static void Set(TMP_Text t, string s) { if (t) t.text = s; }
        static void Label(Button b, string s, bool on)
        {
            if (!b) return; b.interactable = on;
            var t = b.GetComponentInChildren<TMP_Text>(); if (t) t.text = s;
        }
        static string Fmt(double v) => v >= 1e9 ? (v / 1e9).ToString("0.0") + " Mrd" : v >= 1e6 ? (v / 1e6).ToString("0.0") + " Mio" : v >= 1e4 ? (v / 1e3).ToString("0.0") + " k" : Math.Floor(v).ToString("0");

        void ShowMessage(string s) { if (messageText) messageText.text = s; messageTimer = 2.5f; }

        // ---------- Speichern und Offline ----------
        void CatchUpOffline()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (sim.D.lastActiveUnix <= 0) { sim.D.lastActiveUnix = now; return; }
            float away = Mathf.Min(now - sim.D.lastActiveUnix, Balance.OfflineCapHours * 3600f);
            if (away < 5) return;
            double before = sim.D.credits; int steps = Mathf.FloorToInt(away);
            for (int i = 0; i < steps; i++) sim.Tick(1f);   // dieselbe Simulation, 1-s-Schritte
            ShowMessage("Während du weg warst: +" + Fmt(sim.D.credits - before) + " Cr");
            Save();
        }

        void Save()
        {
            sim.D.lastActiveUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string json = JsonUtility.ToJson(sim.D);
            PlayerPrefs.SetString(SaveKey + "-bak", PlayerPrefs.GetString(SaveKey, json)); // vorheriger Stand als Sicherung
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        static GameData Load()
        {
            foreach (string key in new[] { SaveKey, SaveKey + "-bak" })
            {
                string json = PlayerPrefs.GetString(key, "");
                if (string.IsNullOrEmpty(json)) continue;
                try { var d = JsonUtility.FromJson<GameData>(json); if (d != null && d.saveVersion >= 1) return Normalize(d); }
                catch (Exception) { /* beschädigt: Sicherung versuchen */ }
            }
            return null;
        }

        // Neue Felder bekommen hier ihren Standardwert, damit alte Stände nie kaputtgehen
        static GameData Normalize(GameData d)
        {
            if (d.hand == null || d.hand.Length < 3) d.hand = new float[3];
            if (d.roh == null || d.roh.Length < 3) d.roh = new float[3];
            if (d.prod == null || d.prod.Length < 2) d.prod = new float[2];
            if (d.pressIn == null || d.pressIn.Length < 2) d.pressIn = new float[2];
            if (d.rocketCargo == null || d.rocketCargo.Length < 5) d.rocketCargo = new float[5];
            if (d.upgrades == null || d.upgrades.Length < 5) { var u = new int[5]; if (d.upgrades != null) Array.Copy(d.upgrades, u, d.upgrades.Length); d.upgrades = u; }
            if (d.vehicles == null) d.vehicles = new List<VehicleData>();
            return d;
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); else if (sim != null) CatchUpOffline(); }
        void OnApplicationQuit() { Save(); }
    }
}
