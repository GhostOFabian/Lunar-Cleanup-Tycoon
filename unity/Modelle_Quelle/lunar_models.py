# -*- coding: utf-8 -*-
"""
Lunar Cleanup Tycoon – Modellbaukasten für Blender
===================================================

Baut alle Gebäude, Fahrzeuge, Drohnen, Bohrturm/Erzbagger und die Rakete des
Spiels als detaillierte Low-Poly-Modelle (Mobile-tauglich, ca. 2–12 k Dreiecke
pro Modell) mit PBR-Materialien.

So benutzt du es
----------------
Kurz (Fabian 02.11.): Blender öffnen → Reiter „Scripting“ → „Open“ → diese Datei → ▶ „Run Script“.
Dann „Datei → Speichern unter …“ (z. B. LunarCleanup_neu.blend) – fertig. NUR_NEUE = True zeigt nur die
37 neuen Modelle; auf False stellen, um alle zu bauen.

1. Blender (4.2 oder neuer, 3.6 geht auch) öffnen → Reiter „Scripting“.
2. „Open“ → diese Datei wählen → „Run Script“ (▶).
3. Alle Modelle stehen nebeneinander in der Sammlung „LunarCleanup“
   (Reihen: Gebäude, Anlagen, Fahrzeuge, Drohnen).
4. Zum Export nach Unity: unten EXPORT = True setzen, Datei speichern und
   nochmal ausführen. Jedes Modell landet als eigene FBX im Ordner EXPORT_DIR.

Einstellungen stehen direkt unter diesem Kommentar.

Maße und Ausrichtung (wichtig für Unity)
----------------------------------------
- 1 Blender-Einheit = 1 m = 1 Unity-Einheit. Der Ursprung jedes Modells liegt
  in der Mitte am Boden.
- Vorderseite zeigt nach −Y (Blender-Ansicht „Front“, Ziffernblock 1). Mit den
  Export-Einstellungen unten (Forward −Z, Up Y, Apply Transform) zeigt sie in
  Unity nach +Z – so wie die Fahrzeuge im Spiel fahren. Falls ein Modell in
  Unity verkehrt herum steht: FRONT_FLIP = True.
- Gebäude bleiben innerhalb ihres Grundkreises (Radius fp aus gamedata.json),
  damit sie nicht in Bänder und Nachbarn ragen.

Bewegliche Teile
----------------
Teile, die das Spiel bewegt, sind eigene Objekte mit dem Drehpunkt an der
richtigen Stelle. Die Namen entsprechen den Feldern im Unity-Code
(Models.BuildingParts / VehicleParts / DroneParts):
  <id>_Rakete (sell), <id>_Kolben (presse, fährt auf und ab),
  <id>_Dreh (research Schüssel, rig Bohrgestänge, bagger Schaufelrad,
            lifter Haken, hangar Radar, werkstatt Roboterarm),
  <id>_Rad_0..n (Fahrzeuge, drehen um X), <id>_Werkzeug (Bürste/Düse/Fräse/
  Pflug/Pumpenarm vorne), <id>_Rotor_0..3 (Drohnen, drehen um Z),
  <id>_Ladung (Drohnenkorb bzw. Greifer). Alles andere steckt in <id>_Koerper.
Leuchtende Teile (Lampen, Glut, Mint-Lichter) haben Emissions-Materialien
(LC_lampe, LC_mint, LC_warn, LC_glut, LC_glas).
"""

import math
import os

try:
    import bpy  # in Blender vorhanden
    HAVE_BPY = True
except ImportError:  # außerhalb von Blender (Test/Vorschau)
    HAVE_BPY = False

# ============================== Einstellungen ===============================
EXPORT = False                    # True: jedes Modell als FBX exportieren
EXPORT_DIR = "//LunarCleanup_FBX"  # "//" = neben der gespeicherten .blend
ONLY = []                         # leer = alle, sonst z. B. ["presse", "buggy"]
NUR_NEUE = True                   # Fabian 02.11.: True = nur die neuen Modelle (Build 34–38) in die Szene, False = alle
HQ = True                         # Blender-Qualität: doppelt so viele Rundungen, mehr Details, weiche Felsen, schönere Materialien.
                                  # Für den Export ins Spiel (EXPORT = True) auf False stellen – sonst wird es fürs Handy zu schwer.
ACCENT = "#ffbf4d"                # Akzentfarbe (Mond: Orange/Gelb wie im Spiel)
ACCENT2 = "#58cdb4"               # zweite Akzentfarbe (Türkis)
BEVEL = True                      # weiche Kanten (Bevel-Modifier)
BEVEL_WIDTH = 0.025               # m
BEVEL_SEGMENTS = 2                # 1 = sparsamer fürs Handy
SPACING = 13.0                    # Abstand der Modelle in der Szene
SETUP_SCENE = True                # Licht, Boden, Kamera für schöne Vorschau
FRONT_FLIP = False                # Modelle beim Export um 180° drehen
CLEAR_OLD = True                  # vorherige LunarCleanup-Modelle löschen

def _hq():
    return HAVE_BPY and HQ


SEGQ = 2 if (HAVE_BPY and HQ) else 1   # Rundungen ×2 in Blender (nur runde Teile ab 10 Segmenten; Kanten wie Obelisk/Kristall bleiben)
DETAIL = HAVE_BPY and HQ               # zusätzliche Kleinteile nur in Blender


def _sq(seg):
    return seg * SEGQ if seg >= 10 else seg


# ============================== Materialien =================================
# name: (Farbe, Metallic, Rauheit, Leuchtfarbe oder None, Leuchtstärke)
MATS = {
    "weiss":   ("#dfe5ee", 0.10, 0.55, None, 0),
    "hell":    ("#c4ccd8", 0.20, 0.50, None, 0),
    "dunkel":  ("#3a4150", 0.50, 0.45, None, 0),
    "stahl":   ("#8a93a3", 0.85, 0.35, None, 0),
    "platte":  ("#4a5060", 0.20, 0.70, None, 0),
    "gummi":   ("#1a1d22", 0.00, 0.85, None, 0),
    "akzent":  (ACCENT,    0.30, 0.45, None, 0),
    "akzent2": (ACCENT2,   0.30, 0.45, None, 0),
    "rot":     ("#e2553a", 0.30, 0.45, None, 0),
    "orange":  ("#ff731a", 0.30, 0.45, None, 0),
    "kupfer":  ("#b87333", 0.90, 0.35, None, 0),
    "solar":   ("#26447f", 0.70, 0.15, None, 0),
    "glas":    ("#7fd8ff", 0.10, 0.05, "#7fd8ff", 0.6),
    "eis":     ("#bfe8ff", 0.10, 0.20, None, 0),
    "lampe":   ("#fff0c8", 0.00, 0.30, "#fff0c8", 6.0),
    "mint":    ("#6ff2d2", 0.00, 0.30, "#6ff2d2", 5.0),
    "warn":    ("#ff5a4a", 0.00, 0.30, "#ff5a4a", 6.0),
    "glut":    ("#ff7a1a", 0.00, 0.40, "#ff7a1a", 8.0),
    "kiste1":  ("#d9a441", 0.10, 0.70, None, 0),
    "kiste2":  ("#5b8fd9", 0.30, 0.50, None, 0),
    "kiste3":  ("#9ad1c4", 0.20, 0.30, None, 0),
    "fels":    ("#8a8378", 0.00, 0.90, None, 0),
    "felsdkl": ("#4d4943", 0.00, 0.90, None, 0),
    "rost":    ("#8a4a2a", 0.40, 0.80, None, 0),
    "schleim": ("#7fbf3a", 0.00, 0.20, "#7fbf3a", 0.4),
    "schwefel": ("#e8d040", 0.00, 0.70, None, 0),
    "lavastein": ("#2a2422", 0.00, 0.85, None, 0),
    "russ":    ("#15130f", 0.00, 0.95, None, 0),
    "kaltlicht": ("#eaf4ff", 0.00, 0.30, "#eaf4ff", 8.0),
    "kristall": ("#b98cff", 0.10, 0.10, "#b98cff", 2.5),
    "visier":  ("#ffb347", 0.90, 0.10, None, 0),
    "kometeis": ("#dfefff", 0.05, 0.25, "#9fdcff", 0.25),
    "band":    ("#23272e", 0.10, 0.75, None, 0),
    # Build 34 (02.11.2026): Abwehr und Natur
    "schildfeld": ("#7fe8ff", 0.00, 0.20, "#7fe8ff", 6.0),
    "laub1":   ("#4c9a3a", 0.00, 0.75, None, 0),
    "laub2":   ("#6fb24a", 0.00, 0.75, None, 0),
    "laub3":   ("#2f7a33", 0.00, 0.80, None, 0),
    "tanne":   ("#24593a", 0.00, 0.80, None, 0),
    "rinde":   ("#6b4a2f", 0.00, 0.90, None, 0),
    "gras":    ("#7cb94c", 0.00, 0.80, None, 0),
    "bluete":  ("#f4d35e", 0.00, 0.60, None, 0),
    "wasser":  ("#2c6fa8", 0.10, 0.05, None, 0),
    "pilz":    ("#c9493b", 0.00, 0.55, None, 0),
    "pilzstiel": ("#e8dcc8", 0.00, 0.70, None, 0),
    "trockengras": ("#c2a95a", 0.00, 0.85, None, 0),
}

# Grundkreis-Radien aus gamedata.json (bld.fp)
FP = {"hq": 3.6, "roh": 3.4, "sell": 3.3, "prod": 2.9, "u1": 2.6, "presse": 2.4,
      "energy": 4.3, "werkstatt": 3.4, "u2": 2.9, "schmelze": 2.2, "research": 2.9,
      "u3": 3.4, "raff": 2.4, "hangar": 3.0, "rtg": 1.9, "geo": 2.8, "reactor": 2.8,
      "lifter": 2.8, "port": 3.4}

# ======================= Geometrie (reines Python) ==========================
def _rot(rx=0.0, ry=0.0, rz=0.0):
    """Drehmatrix aus Euler-Winkeln in Grad (Reihenfolge XYZ wie in Blender)."""
    a, b, c = math.radians(rx), math.radians(ry), math.radians(rz)
    ca, sa, cb, sb, cc, sc = math.cos(a), math.sin(a), math.cos(b), math.sin(b), math.cos(c), math.sin(c)
    Rx = ((1, 0, 0), (0, ca, -sa), (0, sa, ca))
    Ry = ((cb, 0, sb), (0, 1, 0), (-sb, 0, cb))
    Rz = ((cc, -sc, 0), (sc, cc, 0), (0, 0, 1))
    return _mm(Rz, _mm(Ry, Rx))


def _mm(A, B):
    return tuple(tuple(sum(A[i][k] * B[k][j] for k in range(3)) for j in range(3)) for i in range(3))


def _mv(R, v):
    return (R[0][0] * v[0] + R[0][1] * v[1] + R[0][2] * v[2],
            R[1][0] * v[0] + R[1][1] * v[1] + R[1][2] * v[2],
            R[2][0] * v[0] + R[2][1] * v[1] + R[2][2] * v[2])


def _sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def _add(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def _mul(a, s): return (a[0] * s, a[1] * s, a[2] * s)
def _dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def _cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def _len(a): return math.sqrt(_dot(a, a))


def _norm(a):
    l = _len(a)
    return (a[0] / l, a[1] / l, a[2] / l) if l > 1e-12 else (0.0, 0.0, 1.0)


def _align_z(d):
    """Drehmatrix, die +Z auf Richtung d dreht (Rodrigues)."""
    d = _norm(d)
    z = (0.0, 0.0, 1.0)
    c = _dot(z, d)
    if c > 0.999999:
        return ((1, 0, 0), (0, 1, 0), (0, 0, 1))
    if c < -0.999999:
        return ((1, 0, 0), (0, -1, 0), (0, 0, -1))
    v = _cross(z, d)
    s = _len(v)
    k = _mul(v, 1 / s)
    K = ((0, -k[2], k[1]), (k[2], 0, -k[0]), (-k[1], k[0], 0))
    K2 = _mm(K, K)
    return tuple(tuple((1 if i == j else 0) + s * K[i][j] + (1 - c) * K2[i][j] for j in range(3)) for i in range(3))


class Mesh:
    """Ein Objekt: Eckpunkte, Flächen, Material je Fläche. pivot = Drehpunkt (Objektursprung)."""

    def __init__(self, name, pivot=(0.0, 0.0, 0.0)):
        self.name, self.pivot = name, tuple(pivot)
        self.v, self.f, self.fm, self.mats = [], [], [], []
        self.parts = []  # (erste Fläche, letzte Fläche) je geschlossenem Teil – für den Test

    def _mi(self, mat):
        if mat not in MATS:
            raise KeyError("Unbekanntes Material: " + mat)
        if mat not in self.mats:
            self.mats.append(mat)
        return self.mats.index(mat)

    def add(self, verts, faces, mat, pos=(0, 0, 0), rot=None, R=None, closed=True):
        """verts lokal; rot = (rx,ry,rz) Grad oder R = 3×3-Matrix; pos = Weltposition (Modellraum)."""
        if R is None:
            R = _rot(*rot) if rot else None
        base = len(self.v)
        for p in verts:
            q = _mv(R, p) if R else p
            self.v.append((q[0] + pos[0] - self.pivot[0], q[1] + pos[1] - self.pivot[1], q[2] + pos[2] - self.pivot[2]))
        mi = self._mi(mat)
        f0 = len(self.f)
        for fc in faces:
            self.f.append(tuple(base + i for i in fc))
            self.fm.append(mi)
        if closed:
            self.parts.append((f0, len(self.f)))
        return self

    # ---------- Grundformen ----------
    def box(self, mat, pos, size, rot=None, R=None):
        """Quader, pos = Mitte."""
        x, y, z = size[0] / 2, size[1] / 2, size[2] / 2
        v = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z), (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
        f = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
        return self.add(v, f, mat, pos, rot, R)

    def lathe(self, mat, pos, prof, seg=24, rot=None, R=None, cap0=True, cap1=True, closed_loop=False, a0=0.0, a1=360.0):
        """Drehkörper um die lokale Z-Achse. prof = [(Radius, Höhe), …] von unten nach oben (außen).
        closed_loop: Profil ist eine geschlossene Schleife (z. B. Wand mit Innenseite) – keine Deckel."""
        seg = _sq(seg)
        full = abs(a1 - a0) >= 359.999
        n = seg if full else seg + 1
        rings, v, f = [], [], []
        for (r, h) in prof:
            if r <= 1e-6:
                rings.append([len(v)])
                v.append((0.0, 0.0, h))
            else:
                ring = []
                for i in range(n):
                    a = math.radians(a0 + (a1 - a0) * i / seg)
                    ring.append(len(v))
                    v.append((r * math.cos(a), r * math.sin(a), h))
                rings.append(ring)
        cnt = len(rings) if not closed_loop else len(rings) + 1
        for k in range(cnt - 1):
            A, B = rings[k % len(rings)], rings[(k + 1) % len(rings)]
            m = seg if full else seg
            for i in range(m):
                i1 = (i + 1) % n if full else i + 1
                if len(A) == 1 and len(B) == 1:
                    continue
                if len(A) == 1:
                    f.append((A[0], B[i1], B[i]))
                elif len(B) == 1:
                    f.append((A[i], A[i1], B[0]))
                else:
                    f.append((A[i], A[i1], B[i1], B[i]))
        if not closed_loop and full:
            if cap0 and len(rings[0]) > 1:
                f.append(tuple(reversed(rings[0])))
            if cap1 and len(rings[-1]) > 1:
                f.append(tuple(rings[-1]))
        return self.add(v, f, mat, pos, rot, R, closed=full and (closed_loop or ((cap0 or len(rings[0]) == 1) and (cap1 or len(rings[-1]) == 1))))

    def cyl(self, mat, pos, r, h, seg=24, rot=None, R=None, r2=None):
        """Zylinder/Kegelstumpf, pos = Mitte der Unterseite (lokal), wächst entlang lokal +Z."""
        return self.lathe(mat, pos, [(r, 0), (r if r2 is None else r2, h)], seg, rot, R)

    def sphere(self, mat, pos, r, seg=16, rings=8, zcut=-1.0):
        """Kugel; zcut = -1 ganz, 0 = Halbkugel (Kuppel)."""
        rings = rings * SEGQ if seg >= 10 else rings
        prof = []
        a_start = math.asin(max(-1.0, min(1.0, zcut))) if zcut > -1 else -math.pi / 2
        for i in range(rings + 1):
            a = a_start + (math.pi / 2 - a_start) * i / rings
            prof.append((r * math.cos(a) if i < rings else 0.0, r * math.sin(a)))
        if zcut <= -1:
            prof[0] = (0.0, -r)
        return self.lathe(mat, pos, prof, seg)

    def torus(self, mat, pos, R0, r, seg=24, seg2=8, rot=None):
        seg = _sq(seg)
        v, f = [], []
        for i in range(seg):
            a = 2 * math.pi * i / seg
            for j in range(seg2):
                b = 2 * math.pi * j / seg2
                rr = R0 + r * math.cos(b)
                v.append((rr * math.cos(a), rr * math.sin(a), r * math.sin(b)))
        for i in range(seg):
            for j in range(seg2):
                a0, a1 = i * seg2 + j, ((i + 1) % seg) * seg2 + j
                b0, b1 = i * seg2 + (j + 1) % seg2, ((i + 1) % seg) * seg2 + (j + 1) % seg2
                f.append((a0, a1, b1, b0))
        return self.add(v, f, mat, pos, rot)

    def extrude(self, mat, pos, prof, depth, plane="YZ", rot=None):
        """Profil (a,b) als Prisma. plane 'YZ': Seitenansicht (a=y, b=z), Dicke entlang X.
        'XZ': Frontansicht (a=x, b=z), Dicke entlang Y. 'XY': Draufsicht (a=x, b=y), Höhe entlang Z (0..depth)."""
        if plane == "YZ":
            mp = lambda a, b, t: (t, a, b); ax = (1, 0, 0); t0, t1 = -depth / 2, depth / 2
        elif plane == "XZ":
            mp = lambda a, b, t: (a, t, b); ax = (0, 1, 0); t0, t1 = -depth / 2, depth / 2
        else:
            mp = lambda a, b, t: (a, b, t); ax = (0, 0, 1); t0, t1 = 0.0, depth
        pts = list(prof)
        # Newell-Normale der Kappe bei t0; sie muss nach −ax zeigen
        cap = [mp(a, b, t0) for a, b in pts]
        nx = ny = nz = 0.0
        for i in range(len(cap)):
            p, q = cap[i], cap[(i + 1) % len(cap)]
            nx += (p[1] - q[1]) * (p[2] + q[2]); ny += (p[2] - q[2]) * (p[0] + q[0]); nz += (p[0] - q[0]) * (p[1] + q[1])
        if _dot((nx, ny, nz), ax) > 0:
            pts.reverse()
        n = len(pts)
        v = [mp(a, b, t0) for a, b in pts] + [mp(a, b, t1) for a, b in pts]
        f = [tuple(range(n)), tuple(range(2 * n - 1, n - 1, -1))]
        for i in range(n):
            j = (i + 1) % n
            f.append((i, n + i, n + j, j))
        return self.add(v, f, mat, pos, rot)

    def tube(self, mat, p1, p2, r, seg=10, r2=None):
        """Rohr/Strebe zwischen zwei Punkten."""
        d = _sub(p2, p1)
        return self.cyl(mat, p1, r, _len(d), seg, R=_align_z(d), r2=r2)

    def pipe(self, mat, pts, r, seg=10, joints=True):
        for a, b in zip(pts, pts[1:]):
            self.tube(mat, a, b, r, seg)
        if joints:
            for p in pts[1:-1]:
                self.sphere(mat, (p[0], p[1], p[2]), r * 1.08, seg, 4)
        return self

    def sweep(self, mat, path, prof):
        """Profil (u = seitlich, v = nach oben) entlang eines Pfades (3D-Punkte). Seite = Tangente × Z."""
        n = len(path)
        frames = []
        for i in range(n):
            a, b = path[max(0, i - 1)], path[min(n - 1, i + 1)]
            t = _norm(_sub(b, a))
            sd = _norm(_cross(t, (0, 0, 1)))
            up = _cross(sd, t)
            frames.append((sd, up))
        m = len(prof)
        v, f = [], []
        for i, p in enumerate(path):
            sd, up = frames[i]
            for (u, w) in prof:
                v.append(_add(p, _add(_mul(sd, u), _mul(up, w))))
        for i in range(n - 1):
            for k in range(m):
                k1 = (k + 1) % m
                f.append((i * m + k, i * m + k1, (i + 1) * m + k1, (i + 1) * m + k))
        f.append(tuple(range(m - 1, -1, -1)))
        f.append(tuple((n - 1) * m + k for k in range(m)))
        # Wicklung prüfen: Volumen muss positiv sein
        vol = 0.0
        for fc in f:
            p0 = v[fc[0]]
            for j in range(1, len(fc) - 1):
                vol += _dot(p0, _cross(v[fc[j]], v[fc[j + 1]]))
        if vol < 0:
            f = [tuple(reversed(fc)) for fc in f]
        return self.add(v, f, mat)

    def blob(self, mat, pos, r, seed, scale=(1, 1, 0.7), rough=0.3, seg=10, rings=6, rot=None, facet=0.12):
        """Unregelmäßiger Brocken (Fels, Eis, Komet): Kugel, deren Oberfläche mit weichem Rauschen
        (Summe von Wellen in zufälliger Richtung) ein- und ausgebeult wird – keine Stacheln."""
        import random
        rnd = random.Random(seed)
        waves = []
        for i in range(5):
            d = _norm((rnd.gauss(0, 1), rnd.gauss(0, 1), rnd.gauss(0, 1)))
            waves.append((d, 1.5 + i * 1.3, rnd.random() * 6.283, 1.0 / (1 + i * 0.8)))
        tot = sum(w[3] for w in waves)
        tmp = Mesh("tmp")
        tmp.sphere(mat, (0, 0, 0), 1.0, seg, rings)
        v = []
        for p in tmp.v:
            n = _norm(p)
            h = sum(a * math.sin(_dot(n, d) * f + ph) for d, f, ph, a in waves) / tot
            k = r * (1.0 + rough * h + facet * (rnd.random() - 0.5))  # weiche Form + kantige Facetten
            v.append((n[0] * k * scale[0], n[1] * k * scale[1], n[2] * k * scale[2]))
        return self.add(v, tmp.f, mat, pos, rot)


class Model:
    """Ein exportierbares Modell: Wurzel + statischer Körper + bewegliche Teile."""

    def __init__(self, key, name, group, fp=None):
        self.key, self.name, self.group, self.fp = key, name, group, fp
        self.meshes = [Mesh("Koerper")]

    @property
    def body(self):
        return self.meshes[0]

    def part(self, name, pivot=(0, 0, 0)):
        m = Mesh(name, pivot)
        self.meshes.append(m)
        return m


# =========================== Detail-Bausteine ===============================
def pad(M, fp, h=0.16):
    """Bodenplatte jedes Gebäudes mit Akzentring, Schrauben und Warnstreifen."""
    M.lathe("platte", (0, 0, 0), [(fp + 0.15, 0), (fp, h), (0, h)], 40)
    M.lathe("akzent", (0, 0, 0), [(fp - 0.02, h), (fp - 0.02, h + 0.035), (fp - 0.22, h + 0.035), (fp - 0.22, h)], 40, closed_loop=True)
    for i in range(16):
        a = 2 * math.pi * (i + 0.5) / 16
        M.cyl("stahl", ((fp - 0.42) * math.cos(a), (fp - 0.42) * math.sin(a), h), 0.05, 0.035, 6)
    return h


def railing(M, pts, h=0.9, post=0.75, mat="akzent", r=0.025):
    """Geländer entlang einer Punktfolge."""
    top = [(p[0], p[1], p[2] + h) for p in pts]
    mid = [(p[0], p[1], p[2] + h * 0.5) for p in pts]
    M.pipe(mat, top, r, 8)
    M.pipe("stahl", mid, r * 0.7, 6, joints=False)
    for a, b in zip(pts, pts[1:]):
        L = _len(_sub(b, a))
        k = max(1, int(L / post))
        for i in range(k + 1):
            if i == k and b != pts[-1]:
                continue
            p = _add(a, _mul(_sub(b, a), i / k))
            M.tube("stahl", p, (p[0], p[1], p[2] + h), r * 0.8, 6)


def ladder(M, p, h, facing=(0, -1, 0), w=0.45):
    """Leiter am Punkt p (Fuß, Mitte) nach oben, Sprossen alle 30 cm."""
    s = _norm(_cross(facing, (0, 0, 1)))
    l, r = _add(p, _mul(s, w / 2)), _sub(p, _mul(s, w / 2))
    M.tube("stahl", l, (l[0], l[1], l[2] + h), 0.025, 6)
    M.tube("stahl", r, (r[0], r[1], r[2] + h), 0.025, 6)
    for i in range(1, int(h / 0.3)):
        z = i * 0.3
        M.tube("stahl", (l[0], l[1], l[2] + z), (r[0], r[1], r[2] + z), 0.016, 5)


def beacon(M, p, h=1.2, mat="warn"):
    """Mast mit Warnlicht."""
    M.cyl("stahl", p, 0.05, h, 8, r2=0.03)
    M.sphere(mat, (p[0], p[1], p[2] + h + 0.08), 0.1, 10, 5)


def vents(M, pos, w, h, n=5, facing="y", depth=0.08):
    """Lüftungsgitter: Rahmen + Lamellen (facing 'y' = Fläche zeigt nach −Y, 'x' = nach −X)."""
    if facing == "y":
        M.box("dunkel", pos, (w, depth, h))
        for i in range(n):
            z = pos[2] - h / 2 + h * (i + 0.5) / n
            M.box("stahl", (pos[0], pos[1] - depth / 2 - 0.01, z), (w * 0.92, 0.03, h / n * 0.35), rot=(25, 0, 0))
    else:
        M.box("dunkel", pos, (depth, w, h))
        for i in range(n):
            z = pos[2] - h / 2 + h * (i + 0.5) / n
            M.box("stahl", (pos[0] - depth / 2 - 0.01, pos[1], z), (0.03, w * 0.92, h / n * 0.35), rot=(0, -25, 0))


def windows(M, x0, x1, y, z, h, n, mat="glas"):
    """Fensterband an einer Wand bei y (zeigt nach −Y)."""
    w = (x1 - x0) / n
    for i in range(n):
        cx = x0 + w * (i + 0.5)
        M.box("dunkel", (cx, y, z), (w * 0.86, 0.06, h + 0.08))
        M.box(mat, (cx, y - 0.025, z), (w * 0.74, 0.04, h))


def stripes(M, pos, length, axis="x", w=0.12, h=0.05, n=None):
    """Warnstreifen (Akzent/Dunkel im Wechsel)."""
    n = n or max(2, int(length / 0.25))
    for i in range(n):
        t = -length / 2 + length * (i + 0.5) / n
        p = (pos[0] + t, pos[1], pos[2]) if axis == "x" else (pos[0], pos[1] + t, pos[2])
        s = (length / n * 0.98, w, h) if axis == "x" else (w, length / n * 0.98, h)
        M.box("akzent" if i % 2 == 0 else "dunkel", p, s)


def solar_panel(M, pos, w, d, tilt=25.0, yaw=0.0, post=0.9):
    """Solarmodul auf Ständer, Zellenraster als Leisten."""
    x, y, z = pos
    M.cyl("stahl", (x, y, z), 0.06, post, 8)
    R = _rot(tilt, 0, yaw)
    top = (x, y, z + post + 0.05)
    M.box("dunkel", top, (w + 0.08, d + 0.08, 0.06), R=R)
    M.box("solar", _add(top, _mv(R, (0, 0, 0.035))), (w, d, 0.02), R=R)
    for i in range(1, 4):
        M.box("hell", _add(top, _mv(R, (-w / 2 + w * i / 4, 0, 0.05))), (0.02, d, 0.012), R=R)
    for j in range(1, 3):
        M.box("hell", _add(top, _mv(R, (0, -d / 2 + d * j / 3, 0.05))), (w, 0.02, 0.012), R=R)


def tank_h(M, pos, r, L, mat="weiss", yaw=0.0, bands=2):
    """Liegender Tank mit gewölbten Enden (Kapsel) entlang X (yaw dreht)."""
    prof = [(0.0, -L / 2 - r * 0.55)]
    for i in range(1, 5):
        a = math.pi / 2 * (1 - i / 4)
        prof.append((r * math.sin(math.pi / 2 - a), -L / 2 - r * 0.55 * math.sin(a)))
    prof += [(r, L / 2)]
    for i in range(1, 5):
        a = math.pi / 2 * i / 4
        prof.append((r * math.cos(a), L / 2 + r * 0.55 * math.sin(a)))
    prof[-1] = (0.0, L / 2 + r * 0.55)
    R = _mm(_rot(0, 0, yaw), _rot(0, 90, 0))
    M.lathe(mat, pos, prof, 20, R=R)
    for k in range(bands):
        t = -L / 2 + L * (k + 1) / (bands + 1)
        M.lathe("akzent", _add(pos, _mv(_rot(0, 0, yaw), (t, 0, 0))), [(r + 0.03, -0.05), (r + 0.03, 0.05)], 20, R=R)


def lattice(M, base, top, w0, w1, levels, mat="stahl", r=0.04):
    """Gittermast (4 Eckstreben + Kreuzverbände), base/top = Mittelpunkte."""
    corners = lambda c, w: [(c[0] + sx * w / 2, c[1] + sy * w / 2, c[2]) for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    lv = []
    for i in range(levels + 1):
        t = i / levels
        c = _add(base, _mul(_sub(top, base), t))
        lv.append(corners(c, w0 + (w1 - w0) * t))
    for k in range(4):
        M.pipe(mat, [l[k] for l in lv], r, 6, joints=False)
    for i in range(levels):
        for k in range(4):
            a, b = lv[i][k], lv[i + 1][(k + 1) % 4]
            M.tube(mat, a, b, r * 0.6, 5)
            M.tube(mat, lv[i + 1][k], lv[i + 1][(k + 1) % 4], r * 0.6, 5)


def dish(M, pos, r, depth, mat="weiss", tilt=35.0, yaw=0.0):
    """Parabolschüssel (doppelwandig) mit Empfängerstab."""
    prof = []
    for i in range(7):
        t = i / 6
        prof.append((r * t, depth * t * t))
    inner = [(rr * 0.97, z + 0.03) for rr, z in reversed(prof)]
    prof = prof + inner
    prof[0] = (0.0, 0.0)
    prof[-1] = (0.0, 0.03)
    R = _rot(tilt, 0, yaw)
    M.lathe(mat, pos, prof, 24, R=R, cap0=False, cap1=False)
    tip = _add(pos, _mv(R, (0, 0, depth + r * 0.55)))
    M.tube("stahl", _add(pos, _mv(R, (0, 0, 0.03))), tip, 0.025, 6)
    M.sphere("dunkel", tip, 0.06, 8, 4)


def hopper(M, pos, r_top, r_bot, h, mat="hell", t=0.06):
    """Trichter (doppelwandig, oben offen, unten Auslauf)."""
    M.lathe(mat, pos, [(r_bot, 0), (r_top, h), (r_top - t, h), (r_bot - t, t)], 24, closed_loop=True)


def crate(M, pos, s, mat):
    M.box(mat, pos, (s, s, s))
    M.box("dunkel", (pos[0], pos[1], pos[2] + s / 2 - 0.03), (s * 1.02, s * 1.02, 0.05))
    M.box("dunkel", (pos[0], pos[1], pos[2] - s / 2 + 0.03), (s * 1.02, s * 1.02, 0.05))


# ================================ Gebäude ===================================
def b_hq():
    m = Model("hq", "Hauptstation", "Gebaeude", FP["hq"]); M = m.body
    h0 = pad(M, FP["hq"])
    # Wohnmodul mit abgeschrägten Kanten (Frontprofil, Tiefe entlang Y)
    prof = [(-2.3, h0), (2.3, h0), (2.45, h0 + 0.35), (2.45, h0 + 1.9), (2.2, h0 + 2.25), (-2.2, h0 + 2.25), (-2.45, h0 + 1.9), (-2.45, h0 + 0.35)]
    M.extrude("weiss", (0, 0.2, 0), prof, 3.0, "XZ")
    for y in (-1.25, 0.2, 1.65):  # Paneelfugen
        M.box("hell", (0, y, h0 + 1.15), (5.0, 0.08, 2.0))
    M.box("akzent", (0, -1.31, h0 + 1.45), (4.9, 0.04, 0.22))
    windows(M, -2.1, 0.4, -1.33, h0 + 1.85, 0.45, 4)
    # Garage mit Lamellentor
    M.box("dunkel", (1.45, -1.32, h0 + 0.75), (1.5, 0.06, 1.45))
    for i in range(7):
        M.box("hell", (1.45, -1.36, h0 + 0.12 + i * 0.2), (1.38, 0.03, 0.14))
    stripes(M, (1.45, -1.75, h0 + 0.02), 1.6, "x", 0.5, 0.03)
    for x in (0.62, 2.28):
        M.sphere("lampe", (x, -1.4, h0 + 1.6), 0.07, 8, 4)
    # Luftschleuse (Zylinder links)
    M.cyl("hell", (-2.45, -0.4, h0 + 0.05), 0.75, 1.9, 24, rot=(0, 0, 0))
    M.lathe("dunkel", (-2.45, -0.4, h0 + 1.95), [(0.78, 0), (0.6, 0.18), (0, 0.22)], 24)
    M.box("dunkel", (-2.75, -1.05, h0 + 0.95), (0.7, 0.1, 1.5))
    M.box("glas", (-2.75, -1.11, h0 + 1.35), (0.35, 0.04, 0.3))
    # Dach: Solarmodule, Kuppel, Klimageräte, Mast
    roof = h0 + 2.25
    for x in (-1.25, 1.0):
        solar_panel(M, (x, 0.75, roof), 1.9, 1.15, -22, 0, 0.35)
    M.cyl("hell", (-1.3, -0.55, roof), 0.75, 0.15, 24)
    M.sphere("glas", (-1.3, -0.55, roof + 0.15), 0.68, 20, 6, zcut=0.0)
    for x in (0.6, 1.4):
        M.box("hell", (x, -0.6, roof + 0.25), (0.6, 0.6, 0.5))
        M.cyl("dunkel", (x, -0.6, roof + 0.5), 0.22, 0.03, 16)
    beacon(M, (2.0, 1.4, roof), 1.4)
    M.cyl("stahl", (-2.0, 1.3, roof), 0.04, 1.0, 6)
    dish(M, (-2.0, 1.3, roof + 1.0), 0.35, 0.12, "weiss", 40, 200)
    railing(M, [(-2.15, 1.6, roof), (2.15, 1.6, roof)], 0.6)
    ladder(M, (2.3, 1.72, h0), 2.25, (0, 1, 0))
    # Rohre zur Erde und Kabelkanal
    M.pipe("stahl", [(2.45, 1.2, h0 + 0.5), (2.9, 1.2, h0 + 0.5), (2.9, 1.2, h0 + 0.02)], 0.07)
    M.box("dunkel", (0, 1.82, h0 + 0.12), (4.4, 0.3, 0.24))
    return m


def b_roh():
    m = Model("roh", "Rohstofflager", "Gebaeude", FP["roh"]); M = m.body
    h0 = pad(M, FP["roh"])
    for i, x in enumerate((-1.25, 1.25)):
        # Stützen und Trichterboden
        for a in range(4):
            ang = math.radians(45 + a * 90)
            M.tube("stahl", (x + 1.0 * math.cos(ang), 1.0 * math.sin(ang), h0), (x + 1.0 * math.cos(ang), 1.0 * math.sin(ang), h0 + 1.2), 0.07, 8)
        M.lathe("hell", (x, 0, h0 + 0.35), [(0.22, 0), (0.22, 0.2), (1.15, 0.95), (1.15, 1.0)], 28, cap1=False)
        M.lathe("weiss", (x, 0, h0 + 1.35), [(1.15, 0), (1.15, 3.2), (1.05, 3.45), (0.3, 3.9), (0.3, 4.0), (0, 4.0)], 28)
        for k in range(4):
            M.lathe("akzent" if k % 2 == 0 else "hell", (x, 0, h0 + 1.6 + k * 0.8), [(1.19, 0), (1.19, 0.1)], 28)
        # Füllstandsfenster + Leiter
        M.box("dunkel", (x, -1.15, h0 + 2.9), (0.16, 0.06, 2.4))
        M.box("glas", (x, -1.18, h0 + 2.9), (0.09, 0.04, 2.3))
        ladder(M, (x + (0.95 if i else -0.95), -0.75, h0), 4.6, (0.6 if i else -0.6, -0.8, 0))
        # Auslauf unten
        M.cyl("dunkel", (x, 0, h0 + 0.15), 0.2, 0.25, 12)
    # Laufsteg oben zwischen den Silos mit Geländer
    top = h0 + 5.35
    M.box("stahl", (0, 0, top), (2.1, 0.7, 0.08))
    railing(M, [(-1.0, -0.33, top), (1.0, -0.33, top)], 0.55)
    railing(M, [(-1.0, 0.33, top), (1.0, 0.33, top)], 0.55)
    # Einfüllrohr vom Band (vorne) nach oben
    M.pipe("stahl", [(0, -1.9, h0 + 0.6), (0, -1.9, h0 + 5.0), (-0.9, -0.2, h0 + 5.45)], 0.12, 12)
    M.pipe("stahl", [(0, -1.9, h0 + 5.0), (0.9, -0.2, h0 + 5.45)], 0.1, 12)
    M.box("dunkel", (0, -2.0, h0 + 0.5), (1.3, 0.8, 1.0))
    vents(M, (0, -2.41, h0 + 0.55), 1.0, 0.6, 4)
    stripes(M, (0, -2.55, h0 + 0.02), 1.5, "x", 0.3, 0.03)
    beacon(M, (0, 0.2, top), 0.6)
    return m


def b_sell():
    m = Model("sell", "Verkaufsstation", "Gebaeude", FP["sell"]); M = m.body
    h0 = pad(M, FP["sell"])
    # Startplattform mit Flammrost
    M.lathe("dunkel", (0, 0, h0), [(2.75, 0), (2.6, 0.45), (0, 0.45)], 36)
    M.lathe("akzent", (0, 0, h0 + 0.45), [(1.65, 0), (1.65, 0.04), (1.45, 0.04), (1.45, 0)], 36, closed_loop=True)
    for i in range(8):
        M.box("stahl", (0, -1.1 + i * 0.32, h0 + 0.47), (2.2, 0.06, 0.04))
    for k in range(12):
        a = 2 * math.pi * k / 12
        M.sphere("lampe", (2.3 * math.cos(a), 2.3 * math.sin(a), h0 + 0.46), 0.06, 8, 4)
    # Startturm (Gitter) mit Versorgungsarm
    lattice(M, (-2.05, 0, h0 + 0.45), (-2.05, 0, h0 + 5.6), 0.7, 0.5, 8)
    M.box("dunkel", (-2.05, 0, h0 + 5.7), (0.8, 0.8, 0.2))
    beacon(M, (-2.05, 0, h0 + 5.8), 0.7)
    for z in (2.4, 4.3):
        M.box("stahl", (-1.45, 0, h0 + z), (1.0, 0.18, 0.12))
        M.box("akzent", (-0.95, 0, h0 + z), (0.12, 0.4, 0.3))
    M.pipe("kupfer", [(-2.4, -0.5, h0 + 0.5), (-2.4, -0.5, h0 + 4.3), (-1.45, -0.1, h0 + 4.3)], 0.05, 8)
    # Tanks hinten rechts
    for y in (-0.9, 0.9):
        tank_h(M, (1.95, y * 1.35, h0 + 0.85), 0.38, 0.9, "weiss", 90, 1)
    # Rakete (beweglich)
    rk = m.part("Rakete", (0, 0, h0 + 0.45))
    z = h0 + 0.45
    rk.lathe("dunkel", (0, 0, z), [(0.28, 0), (0.42, 0.05), (0.36, 0.45), (0.3, 0.5)], 20, cap1=False)
    rk.lathe("weiss", (0, 0, z + 0.5), [(0.56, 0), (0.6, 0.1), (0.6, 2.9), (0.55, 3.3), (0.38, 3.9), (0.15, 4.35), (0, 4.45)], 28)
    for k in range(3):
        rk.lathe("akzent" if k != 1 else "dunkel", (0, 0, z + 0.9 + k * 0.85), [(0.62, 0), (0.62, 0.12)], 28)
    for k in range(4):
        a = k * 90
        R = _rot(0, 0, a)
        fin = [(0.0, 0.0), (0.0, 1.2), (0.25, 1.0), (0.55, 0.0), (0.55, -0.2), (0.1, -0.2)]
        rk.extrude("akzent", _add((0, 0, z + 0.55), _mv(R, (0.55, 0, 0))), fin, 0.07, "XZ", rot=(0, 0, a))
    rk.box("dunkel", (0, -0.6, z + 2.95), (0.34, 0.06, 0.34))
    rk.box("glas", (0, -0.62, z + 2.95), (0.26, 0.04, 0.26))
    rk.sphere("warn", (0, 0, z + 4.47), 0.05, 8, 4)
    return m


def b_prod():
    m = Model("prod", "Produktlager", "Gebaeude", FP["prod"]); M = m.body
    h0 = pad(M, FP["prod"])
    W, D = 3.9, 2.6
    M.box("hell", (0, 0, h0 + 0.12), (W + 0.2, D + 0.2, 0.24))
    xs = [-W / 2 + W * i / 4 for i in range(5)]
    for x in xs:
        for y in (-D / 2, D / 2):
            M.box("stahl", (x, y, h0 + 1.95), (0.12, 0.12, 3.6))
    cols = ["kiste1", "kiste2", "kiste3"]
    for s in range(3):
        z = h0 + 0.35 + s * 1.15
        M.box("akzent", (0, -D / 2, z), (W, 0.1, 0.1))
        M.box("akzent", (0, D / 2, z), (W, 0.1, 0.1))
        M.box("stahl", (0, 0, z + 0.03), (W, D, 0.05))
        for i in range(4):
            for j in range(2):
                if (i + j + s) % 5 == 4:
                    continue
                c = cols[(i * 2 + j + s) % 3]
                crate(M, (xs[i] + W / 8, -0.55 + j * 1.1, z + 0.42), 0.7, c)
    # Dach + Lichtband
    M.box("weiss", (0, 0, h0 + 3.85), (W + 0.4, D + 0.4, 0.14))
    M.box("dunkel", (0, 0, h0 + 3.97), (W - 0.4, 0.5, 0.1))
    M.box("glas", (0, 0, h0 + 4.0), (W - 0.6, 0.35, 0.06))
    # Rückwand + Seitenwände geschlossen, Front offen (Regalgasse sichtbar)
    M.box("weiss", (0, D / 2 + 0.14, h0 + 1.95), (W + 0.3, 0.06, 3.6))
    for x in (-W / 2 - 0.15, W / 2 + 0.15):
        M.box("weiss", (x, 0, h0 + 1.95), (0.06, D + 0.3, 3.6))
        for k in range(4):
            M.box("hell", (x * 1.012, 0, h0 + 0.6 + k * 0.9), (0.03, D + 0.32, 0.06))
    # Regalbediengerät (Kran) vorne
    M.box("akzent", (0, -D / 2 - 0.45, h0 + 3.6), (W + 0.3, 0.14, 0.16))
    M.box("dunkel", (-0.6, -D / 2 - 0.45, h0 + 2.0), (0.14, 0.14, 3.2))
    M.box("akzent", (-0.6, -D / 2 - 0.6, h0 + 1.3), (0.7, 0.35, 0.12))
    beacon(M, (W / 2, D / 2, h0 + 3.92), 0.4, "mint")
    return m


def b_unload(key, tier):
    """Entladeanlage Stufe 1–3: Trichter, Andockplätze, Steuerkabine."""
    fp = FP[key]
    names = {1: "Kleine Entladestation", 2: "Industrielle Entladestation", 3: "Zentrale Entladeanlage"}
    m = Model(key, names[tier], "Gebaeude", fp); M = m.body
    h0 = pad(M, fp)
    s = 0.85 + 0.22 * tier
    # Trichter auf Gestell
    for a in range(4):
        ang = math.radians(45 + 90 * a)
        p = (0.9 * s * math.cos(ang), 0.35 + 0.9 * s * math.sin(ang), h0)
        M.tube("stahl", p, (p[0], p[1], h0 + 1.5 * s), 0.07, 8)
    M.box("dunkel", (0, 0.35, h0 + 0.5 * s), (0.9 * s, 0.9 * s, 0.9 * s))
    hopper(M, (0, 0.35, h0 + 1.0 * s), 1.35 * s, 0.35 * s, 1.0 * s, "hell")
    M.lathe("akzent", (0, 0.35, h0 + 2.0 * s), [(1.4 * s, 0), (1.4 * s, 0.1), (1.3 * s, 0.1), (1.3 * s, 0)], 24, closed_loop=True)
    for k in range(6):
        a = math.radians(k * 60)
        M.tube("stahl", (1.2 * s * math.cos(a), 0.35 + 1.2 * s * math.sin(a), h0 + 1.5 * s), (0.4 * s * math.cos(a), 0.35 + 0.4 * s * math.sin(a), h0 + 1.05 * s), 0.04, 6)
    # Andockplätze vorne (Plattformen mit Markierung und Prellpfosten)
    n = tier
    wdock = min(1.6, (2 * fp - 0.8) / n)
    for i in range(n):
        x = (i - (n - 1) / 2) * wdock
        xe = abs(x) + wdock / 2  # äußere Ecke muss im Grundkreis bleiben
        y = max(-fp + 1.0, -math.sqrt(max(0.0, (fp - 0.12) ** 2 - xe ** 2)) + 0.63)
        M.box("platte", (x, y, h0 + 0.04), (wdock - 0.12, 1.25, 0.08))
        stripes(M, (x, y - 0.6, h0 + 0.09), wdock - 0.2, "x", 0.08, 0.02)
        M.box("akzent", (x - wdock / 2 + 0.14, y, h0 + 0.09), (0.06, 1.2, 0.02))
        M.box("akzent", (x + wdock / 2 - 0.14, y, h0 + 0.09), (0.06, 1.2, 0.02))
        for sx in (-1, 1):
            M.cyl("akzent", (x + sx * (wdock / 2 - 0.2), y + 0.55, h0 + 0.08), 0.07, 0.55, 10)
            M.box("dunkel", (x + sx * (wdock / 2 - 0.2), y + 0.55, h0 + 0.68), (0.16, 0.16, 0.1))
        # Saugstutzen zum Fahrzeug
        M.pipe("stahl", [(x, y + 0.45, h0 + 0.9), (x, 0.0, h0 + 1.2 * s), (0, 0.35, h0 + 1.6 * s)], 0.08, 10)
        M.sphere("mint", (x, y + 0.62, h0 + 0.95), 0.07, 8, 4)
    # Steuerkabine seitlich
    cx = fp - 0.9
    M.box("weiss", (cx, 0.9, h0 + 0.7), (0.9, 0.9, 1.4))
    M.box("akzent", (cx, 0.9, h0 + 1.45), (0.95, 0.95, 0.1))
    M.box("glas", (cx - 0.46, 0.9, h0 + 1.0), (0.04, 0.6, 0.4))
    vents(M, (cx, 0.44, h0 + 0.5), 0.5, 0.4, 3)
    beacon(M, (cx, 0.9, h0 + 1.5), 0.6)
    if tier >= 3:  # Anschluss an die Presse (Förderrohr nach hinten)
        M.pipe("stahl", [(0, 0.35, h0 + 0.5), (0, fp - 0.3, h0 + 0.5)], 0.18, 12)
    return m


def b_presse():
    m = Model("presse", "Staubpresse", "Gebaeude", FP["presse"]); M = m.body
    h0 = pad(M, FP["presse"])
    M.box("dunkel", (0, 0, h0 + 0.4), (2.2, 1.6, 0.8))
    M.box("hell", (0, 0, h0 + 0.85), (1.9, 1.4, 0.1))
    for x in (-0.8, 0.8):
        M.box("weiss", (x, 0, h0 + 2.0), (0.38, 1.0, 2.3))
        M.box("akzent", (x, -0.51, h0 + 2.0), (0.4, 0.03, 0.2))
        M.cyl("stahl", (x * 1.45, -0.3, h0 + 0.9), 0.1, 1.6, 12)  # Hydraulik
        M.cyl("kupfer", (x * 1.45, -0.3, h0 + 2.5), 0.06, 0.5, 8)
    M.box("weiss", (0, 0, h0 + 3.35), (2.1, 1.2, 0.5))
    M.box("akzent", (0, -0.61, h0 + 3.35), (2.1, 0.03, 0.18))
    M.cyl("dunkel", (0, 0, h0 + 3.6), 0.45, 0.25, 20)
    # Pressform unten
    M.box("stahl", (0, 0, h0 + 1.0), (0.95, 0.95, 0.28))
    M.box("dunkel", (0, 0, h0 + 1.15), (0.7, 0.7, 0.04))
    # Kolben (beweglich, Drehpunkt oben)
    k = m.part("Kolben", (0, 0, h0 + 3.1))
    k.cyl("stahl", (0, 0, h0 + 1.6), 0.18, 1.5, 16)
    k.box("dunkel", (0, 0, h0 + 1.45), (0.8, 0.8, 0.3))
    k.box("akzent", (0, 0, h0 + 1.62), (0.82, 0.82, 0.05))
    # Einfülltrichter hinten, Auswurf vorne
    hopper(M, (0, 1.05, h0 + 1.0), 0.6, 0.2, 0.8, "hell")
    M.box("stahl", (0, -1.05, h0 + 0.55), (0.8, 0.6, 0.06), rot=(-20, 0, 0))
    stripes(M, (0, -0.83, h0 + 0.82), 1.8, "x", 0.08, 0.05)
    beacon(M, (0.9, 0.5, h0 + 3.6), 0.5, "warn")
    return m


def b_energy():
    m = Model("energy", "Energiezentrale", "Gebaeude", FP["energy"]); M = m.body
    h0 = pad(M, FP["energy"])
    # Solarfeld 3×2
    for i in range(3):
        for j in range(2):
            x, y = -2.2 + i * 2.2, 0.4 + j * 1.75
            if i == 1 and j == 0:
                continue
            solar_panel(M, (x, y, h0), 1.9, 1.3, -28, 0, 0.8)
    # Batteriespeicher (Container) vorne Mitte
    M.box("weiss", (0, -1.6, h0 + 0.7), (2.4, 1.1, 1.4))
    for k in range(5):
        M.box("hell", (-1.0 + k * 0.5, -2.16, h0 + 0.7), (0.04, 0.03, 1.3))
    vents(M, (-0.6, -2.16, h0 + 0.95), 0.7, 0.4, 4)
    vents(M, (0.6, -2.16, h0 + 0.95), 0.7, 0.4, 4)
    for k in range(4):
        M.box("mint", (-0.75 + k * 0.5, -2.17, h0 + 0.35), (0.3, 0.02, 0.06))
    M.box("akzent", (0, -1.6, h0 + 1.42), (2.45, 1.15, 0.06))
    # Umspanner + Isolatoren
    M.box("dunkel", (0, -0.3, h0 + 0.5), (0.9, 0.7, 1.0))
    for x in (-0.25, 0.0, 0.25):
        M.lathe("kupfer", (x, -0.3, h0 + 1.0), [(0.06, 0), (0.09, 0.08), (0.06, 0.16), (0.09, 0.24), (0.04, 0.4), (0, 0.4)], 10)
    # Kabelkanäle zu den Paneelen
    M.box("dunkel", (0, 0.4, h0 + 0.06), (6.4, 0.18, 0.12))
    M.box("dunkel", (0, 2.15, h0 + 0.06), (6.4, 0.18, 0.12))
    M.box("dunkel", (0, 1.2, h0 + 0.06), (0.18, 2.0, 0.12))
    beacon(M, (1.25, -1.2, h0 + 1.45), 0.7, "mint")
    return m


def b_werkstatt():
    m = Model("werkstatt", "Roboterwerkstatt", "Gebaeude", FP["werkstatt"]); M = m.body
    h0 = pad(M, FP["werkstatt"])
    # Halle mit Tonnendach (Frontprofil entlang Y)
    prof = [(-2.0, h0), (2.0, h0), (2.0, h0 + 1.7)]
    for i in range(1, 12):
        a = math.pi * i / 12
        prof.append((2.0 * math.cos(a), h0 + 1.7 + 1.0 * math.sin(a)))
    prof.append((-2.0, h0 + 1.7))
    M.extrude("weiss", (0, 0.3, 0), prof, 3.6, "XZ")
    for y in (-1.2, -0.2, 0.8, 1.8):  # Dachrippen
        M.extrude("hell", (0, y + 0.3, 0), [(x * 1.03, z if z <= h0 + 1.7 else h0 + 1.7 + (z - h0 - 1.7) * 1.03) for x, z in prof], 0.1, "XZ")
    # Großes Tor
    M.box("dunkel", (0, -1.52, h0 + 1.05), (2.6, 0.06, 2.0))
    for i in range(9):
        M.box("hell", (0, -1.56, h0 + 0.15 + i * 0.22), (2.5, 0.03, 0.15))
    M.box("akzent", (0, -1.55, h0 + 2.12), (2.9, 0.08, 0.14))
    for x in (-1.45, 1.45):
        M.box("akzent", (x, -1.55, h0 + 1.05), (0.12, 0.08, 2.1))
        M.sphere("lampe", (x, -1.62, h0 + 2.3), 0.08, 8, 4)
    stripes(M, (0, -2.0, h0 + 0.02), 2.6, "x", 0.6, 0.03)
    # Roboterarm vor der Halle (Dreh = Arm schwenkt)
    arm = m.part("Dreh", (2.35, -1.7, h0))
    arm.cyl("dunkel", (2.35, -1.7, h0), 0.35, 0.3, 16)
    arm.cyl("akzent", (2.35, -1.7, h0 + 0.3), 0.25, 0.35, 16)
    a1 = (2.35, -1.7, h0 + 0.7); a2 = (2.35, -2.0, h0 + 1.8); a3 = (2.0, -2.5, h0 + 1.5)
    arm.tube("akzent", a1, a2, 0.12, 12)
    arm.sphere("dunkel", a2, 0.16, 10, 5)
    arm.tube("akzent", a2, a3, 0.09, 10)
    arm.cyl("stahl", a3, 0.08, 0.25, 8, rot=(180, 0, 0))
    # Dachaufbauten
    M.box("hell", (-1.0, 1.3, h0 + 2.75), (0.7, 0.7, 0.35))
    M.cyl("stahl", (0.9, 1.4, h0 + 2.5), 0.15, 0.8, 12)
    beacon(M, (0, 1.9, h0 + 2.7), 0.6)
    M.box("dunkel", (-2.1, 0.6, h0 + 0.6), (0.25, 1.6, 1.0))  # Werkzeugschrank
    vents(M, (-2.24, 0.6, h0 + 0.7), 1.2, 0.5, 4, "x")
    return m


def b_schmelze():
    m = Model("schmelze", "Schmelzanlage", "Gebaeude", FP["schmelze"]); M = m.body
    h0 = pad(M, FP["schmelze"])
    M.lathe("weiss", (0, 0.1, h0), [(1.45, 0), (1.45, 1.6), (1.2, 2.0), (0.75, 2.4), (0.75, 2.55), (0, 2.55)], 28)
    for k in range(3):
        M.lathe("akzent" if k == 1 else "hell", (0, 0.1, h0 + 0.35 + k * 0.5), [(1.49, 0), (1.49, 0.1)], 28)
    # Glühende Ofentür vorne
    M.box("dunkel", (0, -1.32, h0 + 0.75), (0.9, 0.3, 0.9))
    M.box("glut", (0, -1.48, h0 + 0.7), (0.6, 0.04, 0.5))
    for x in (-0.25, 0, 0.25):
        M.box("stahl", (x, -1.51, h0 + 0.7), (0.04, 0.03, 0.55))
    # Gießrinne + Tiegel
    M.box("stahl", (0.0, -1.85, h0 + 0.35), (0.25, 0.6, 0.08), rot=(-15, 0, 0))
    M.lathe("dunkel", (0, -2.0, h0), [(0.22, 0), (0.3, 0.35), (0.26, 0.35), (0.18, 0.05)], 16, closed_loop=True)
    M.cyl("glut", (0, -2.0, h0 + 0.05), 0.2, 0.25, 16, r2=0.27)
    # Schornstein mit Kühlrippen
    M.cyl("hell", (0.7, 0.9, h0 + 1.5), 0.3, 3.4, 16, r2=0.24)
    for k in range(4):
        M.lathe("dunkel", (0.7, 0.9, h0 + 2.3 + k * 0.6), [(0.36, 0), (0.36, 0.08)], 16)
    M.lathe("akzent", (0.7, 0.9, h0 + 4.9), [(0.3, 0), (0.3, 0.15), (0.2, 0.15), (0.2, 0.0)], 16, closed_loop=True)
    # Brennerrohre, Gasflaschen
    M.pipe("kupfer", [(-1.4, 0.4, h0 + 0.4), (-1.75, 0.4, h0 + 0.4), (-1.75, 0.4, h0 + 1.3), (-1.3, 0.4, h0 + 1.3)], 0.06, 8)
    for y in (0.9, 1.3):
        M.cyl("rot", (-1.55, y, h0), 0.15, 0.9, 12)
        M.sphere("rot", (-1.55, y, h0 + 0.9), 0.15, 12, 4, zcut=0)
    beacon(M, (-0.3, 0.6, h0 + 2.55), 0.5, "glut")
    return m


def b_research():
    m = Model("research", "Forschungsstation", "Gebaeude", FP["research"]); M = m.body
    h0 = pad(M, FP["research"])
    M.cyl("hell", (0, 0.2, h0), 1.7, 0.7, 32)
    M.lathe("akzent", (0, 0.2, h0 + 0.7), [(1.74, 0), (1.74, 0.08)], 32)
    M.sphere("weiss", (0, 0.2, h0 + 0.78), 1.6, 32, 8, zcut=0.0)
    for k in range(10):  # Fensterring
        a = 2 * math.pi * k / 10
        R = _rot(0, 0, math.degrees(a) + 90)
        M.box("glas", (1.62 * math.cos(a), 0.2 + 1.62 * math.sin(a), h0 + 0.45), (0.5, 0.05, 0.28), R=R)
    # Eingang
    M.box("hell", (0, -1.55, h0 + 0.55), (0.9, 0.8, 1.1))
    M.box("dunkel", (0, -1.96, h0 + 0.5), (0.55, 0.04, 0.9))
    M.sphere("mint", (0, -1.98, h0 + 1.05), 0.06, 8, 4)
    # Teleskop-/Antennenmast, Schüssel dreht (Dreh)
    M.cyl("stahl", (1.6, 1.4, h0), 0.12, 2.4, 12)
    sp = m.part("Dreh", (1.6, 1.4, h0 + 2.4))
    sp.cyl("dunkel", (1.6, 1.4, h0 + 2.4), 0.2, 0.2, 12)
    dish(sp, (1.6, 1.4, h0 + 2.65), 0.75, 0.25, "weiss", 35, 0)
    # Labormodul hinten mit Sensoren
    M.box("weiss", (-1.5, 1.6, h0 + 0.5), (1.0, 0.9, 1.0))
    M.box("akzent", (-1.5, 1.6, h0 + 1.02), (1.04, 0.94, 0.06))
    for x in (-1.75, -1.25):
        M.cyl("stahl", (x, 1.6, h0 + 1.05), 0.03, 0.6, 6)
        M.sphere("mint", (x, 1.6, h0 + 1.68), 0.05, 8, 4)
    M.sphere("glas", (0, 0.2, h0 + 2.38), 0.28, 16, 6, zcut=0)
    return m


def b_raff():
    m = Model("raff", "Hochleistungsraffinerie", "Gebaeude", FP["raff"]); M = m.body
    h0 = pad(M, FP["raff"])
    cols = [(-0.9, 0.5, 0.45, 3.6), (0.15, 0.85, 0.38, 2.9), (0.95, 0.1, 0.3, 2.2)]
    for x, y, r, h in cols:
        M.cyl("weiss", (x, y, h0), r, h, 20)
        M.sphere("weiss", (x, y, h0 + h), r, 20, 5, zcut=0)
        for k in range(int(h / 0.7)):
            M.lathe("akzent" if k % 2 else "hell", (x, y, h0 + 0.4 + k * 0.7), [(r + 0.04, 0), (r + 0.04, 0.07)], 20)
        # Ringplattform mit Geländer
        zp = h0 + h * 0.62
        M.lathe("stahl", (x, y, zp), [(r + 0.35, 0), (r + 0.35, 0.05), (r, 0.05), (r, 0)], 20, closed_loop=True)
        ring = [(x + (r + 0.32) * math.cos(2 * math.pi * i / 10), y + (r + 0.32) * math.sin(2 * math.pi * i / 10), zp + 0.05) for i in range(11)]
        railing(M, ring, 0.45, 0.6, "akzent", 0.02)
    # Kugeltank
    M.sphere("hell", (1.1, -1.05, h0 + 0.95), 0.6, 20, 8)
    for a in range(4):
        ang = math.radians(45 + 90 * a)
        M.tube("stahl", (1.1 + 0.45 * math.cos(ang), -1.05 + 0.45 * math.sin(ang), h0), (1.1 + 0.42 * math.cos(ang), -1.05 + 0.42 * math.sin(ang), h0 + 0.8), 0.05, 6)
    # Rohrbrücke
    M.pipe("kupfer", [(-0.9, 0.5, h0 + 3.0), (-0.9, -0.3, h0 + 3.0), (0.15, -0.3, h0 + 2.3), (0.15, 0.85 - 0.4, h0 + 2.3)], 0.07, 8)
    M.pipe("stahl", [(0.95, 0.1, h0 + 1.6), (1.1, -1.05, h0 + 1.6), (1.1, -1.05, h0 + 1.5)], 0.06, 8)
    M.pipe("stahl", [(-0.9, 0.05, h0 + 0.5), (-0.9, -1.4, h0 + 0.5)], 0.09, 10)
    # Fackel mit Mint-Flamme
    M.cyl("stahl", (-1.4, -1.0, h0), 0.07, 2.6, 8)
    M.sphere("mint", (-1.4, -1.0, h0 + 2.72), 0.12, 10, 5)
    M.box("dunkel", (0.0, -1.2, h0 + 0.4), (0.8, 0.6, 0.8))
    vents(M, (0.0, -1.51, h0 + 0.45), 0.6, 0.45, 3)
    return m


def b_hangar():
    m = Model("hangar", "Drohnenhangar", "Gebaeude", FP["hangar"]); M = m.body
    h0 = pad(M, FP["hangar"])
    # Landefläche mit H und Randlichtern
    M.cyl("dunkel", (0, -0.4, h0), 1.9, 0.12, 36)
    M.lathe("akzent", (0, -0.4, h0 + 0.12), [(1.8, 0), (1.8, 0.02), (1.6, 0.02), (1.6, 0)], 36, closed_loop=True)
    for x in (-0.45, 0.45):
        M.box("weiss", (x, -0.4, h0 + 0.13), (0.16, 1.3, 0.02))
    M.box("weiss", (0, -0.4, h0 + 0.13), (0.9, 0.16, 0.02))
    for k in range(12):
        a = 2 * math.pi * k / 12
        M.sphere("mint" if k % 2 else "lampe", (1.7 * math.cos(a), -0.4 + 1.7 * math.sin(a), h0 + 0.14), 0.05, 6, 3)
    # Hangarbox hinten mit Rolltor
    M.box("weiss", (0, 1.9, h0 + 0.8), (2.6, 1.3, 1.6))
    M.box("akzent", (0, 1.9, h0 + 1.65), (2.7, 1.4, 0.1))
    M.box("dunkel", (0, 1.24, h0 + 0.65), (1.8, 0.04, 1.2))
    for i in range(6):
        M.box("hell", (0, 1.21, h0 + 0.1 + i * 0.2), (1.7, 0.03, 0.13))
    # Ladesäulen
    for x in (-1.9, 1.9):
        M.box("hell", (x, -0.4, h0 + 0.5), (0.3, 0.3, 1.0))
        M.box("mint", (x * 0.92, -0.4, h0 + 0.75), (0.02, 0.18, 0.3))
    # Windsack + Radar (Dreh)
    M.cyl("stahl", (-1.3, 2.5, h0 + 1.7), 0.03, 0.9, 6)
    M.cyl("akzent", (-1.3, 2.5, h0 + 2.5), 0.1, 0.45, 10, rot=(0, 90, 0), r2=0.05)
    rd = m.part("Dreh", (1.1, 1.9, h0 + 1.7))
    rd.cyl("stahl", (1.1, 1.9, h0 + 1.7), 0.06, 0.35, 8)
    rd.box("dunkel", (1.1, 1.9, h0 + 2.1), (0.9, 0.08, 0.22))
    return m


def b_rtg():
    m = Model("rtg", "Radioisotopen-Generator", "Gebaeude", FP["rtg"]); M = m.body
    h0 = pad(M, FP["rtg"])
    M.cyl("dunkel", (0, 0, h0), 0.95, 0.25, 24)
    M.cyl("stahl", (0, 0, h0 + 0.25), 0.5, 1.8, 20)
    for k in range(12):  # Kühlrippen
        a = k * 30
        R = _rot(0, 0, a)
        M.box("hell", _add((0, 0, h0 + 1.15), _mv(R, (0.75, 0, 0))), (0.55, 0.05, 1.6), R=R)
    M.lathe("mint", (0, 0, h0 + 0.6), [(0.53, 0), (0.53, 0.06)], 20)
    M.lathe("mint", (0, 0, h0 + 1.7), [(0.53, 0), (0.53, 0.06)], 20)
    M.lathe("akzent", (0, 0, h0 + 2.05), [(0.55, 0), (0.55, 0.12), (0.2, 0.3), (0, 0.3)], 20)
    # Warnschilder + Zaun
    for k in range(8):
        a = 2 * math.pi * (k + 0.5) / 8
        p = (1.55 * math.cos(a), 1.55 * math.sin(a), h0)
        M.cyl("stahl", p, 0.03, 0.7, 6)
    ring = [(1.55 * math.cos(2 * math.pi * (k + 0.5) / 8), 1.55 * math.sin(2 * math.pi * (k + 0.5) / 8), h0 + 0.65) for k in range(9)]
    M.pipe("akzent", ring, 0.02, 6, joints=False)
    M.box("akzent", (0, -1.56, h0 + 0.4), (0.4, 0.03, 0.4), rot=(0, 45, 0))
    M.pipe("kupfer", [(0.5, 0, h0 + 0.4), (1.2, 0, h0 + 0.4), (1.2, 0, h0 + 0.05)], 0.05, 8)
    return m


def b_geo():
    m = Model("geo", "Geothermie-Kraftwerk", "Gebaeude", FP["geo"]); M = m.body
    h0 = pad(M, FP["geo"])
    # Kühlturm (Hyperboloid, doppelwandig)
    out = [(1.35, 0), (1.15, 0.8), (0.95, 1.7), (0.92, 2.2), (1.0, 2.8), (1.1, 3.2)]
    inn = [(r - 0.08, z) for r, z in reversed(out)]
    M.lathe("weiss", (-0.6, 0.5, h0), out + inn, 28, closed_loop=True)
    M.lathe("akzent", (-0.6, 0.5, h0 + 2.6), [(1.0, 0), (1.0, 0.12)], 28)
    for k in range(8):  # Stützen unten
        a = 2 * math.pi * k / 8
        M.tube("hell", (-0.6 + 1.45 * math.cos(a), 0.5 + 1.45 * math.sin(a), h0), (-0.6 + 1.3 * math.cos(a), 0.5 + 1.3 * math.sin(a), h0 + 0.35), 0.06, 6)
    # Bohrkopf mit Ventilen
    M.cyl("dunkel", (1.3, -0.9, h0), 0.45, 0.4, 16)
    M.cyl("stahl", (1.3, -0.9, h0 + 0.4), 0.22, 0.9, 12)
    for z in (0.7, 1.05):
        M.torus("rot", (1.3, -0.9, h0 + z), 0.3, 0.035, 16, 6)
    M.pipe("kupfer", [(1.3, -0.9, h0 + 1.2), (1.3, -0.9, h0 + 1.5), (0.4, -0.3, h0 + 1.5), (-0.1, 0.0, h0 + 1.0)], 0.1, 10)
    # Turbinenhaus
    M.box("hell", (1.3, 0.8, h0 + 0.6), (1.1, 1.3, 1.2))
    M.box("akzent", (1.3, 0.8, h0 + 1.22), (1.15, 1.35, 0.06))
    vents(M, (0.74, 0.8, h0 + 0.7), 0.9, 0.5, 4, "x")
    M.sphere("glut", (1.3, -0.9, h0 + 0.42), 0.12, 10, 4, zcut=0)
    beacon(M, (1.5, 1.2, h0 + 1.25), 0.5)
    return m


def b_reactor():
    m = Model("reactor", "Kernreaktor", "Gebaeude", FP["reactor"]); M = m.body
    h0 = pad(M, FP["reactor"])
    M.cyl("weiss", (-0.4, 0.3, h0), 1.35, 1.8, 32)
    M.sphere("weiss", (-0.4, 0.3, h0 + 1.8), 1.35, 32, 8, zcut=0)
    M.lathe("akzent", (-0.4, 0.3, h0 + 1.5), [(1.39, 0), (1.39, 0.14)], 32)
    M.lathe("mint", (-0.4, 0.3, h0 + 0.25), [(1.38, 0), (1.38, 0.06)], 32)
    M.box("dunkel", (-0.4, -1.1, h0 + 0.6), (0.7, 0.3, 1.2))
    M.box("warn", (-0.4, -1.26, h0 + 1.3), (0.2, 0.02, 0.2))
    # Kleiner Kühlturm + Turbinenhaus
    out = [(0.6, 0), (0.48, 0.7), (0.45, 1.1), (0.52, 1.6)]
    M.lathe("hell", (1.55, 1.2, h0), out + [(r - 0.05, z) for r, z in reversed(out)], 20, closed_loop=True)
    M.box("hell", (1.5, -0.9, h0 + 0.55), (1.0, 1.1, 1.1))
    vents(M, (1.5, -1.46, h0 + 0.6), 0.8, 0.5, 4)
    M.pipe("kupfer", [(0.9, 0.0, h0 + 0.9), (1.5, 0.0, h0 + 0.9), (1.5, 0.75, h0 + 0.9)], 0.09, 10)
    M.pipe("stahl", [(0.9, 0.6, h0 + 0.4), (1.55, 0.6, h0 + 0.4), (1.55, 1.2, h0 + 0.4)], 0.07, 10)
    for k in range(6):
        a = 2 * math.pi * k / 6
        M.box("stahl", (-0.4 + 1.38 * math.cos(a), 0.3 + 1.38 * math.sin(a), h0 + 0.9), (0.08, 0.08, 1.8), rot=(0, 0, math.degrees(a)))
    beacon(M, (-0.4, 0.3, h0 + 3.15), 0.4, "warn")
    return m


def b_lifter():
    m = Model("lifter", "Schwerlast-Hangar", "Gebaeude", FP["lifter"]); M = m.body
    h0 = pad(M, FP["lifter"])
    M.box("dunkel", (0, -0.2, h0 + 0.06), (3.4, 3.0, 0.12))
    stripes(M, (0, -1.7, h0 + 0.13), 3.2, "x", 0.15, 0.02)
    stripes(M, (0, 1.3, h0 + 0.13), 3.2, "x", 0.15, 0.02)
    # Portal (zwei Gittertürme + Brücke)
    for x in (-1.65, 1.65):
        lattice(M, (x, -0.2, h0 + 0.12), (x, -0.2, h0 + 3.6), 0.5, 0.4, 5, "akzent", 0.045)
    M.box("akzent", (0, -0.2, h0 + 3.75), (3.8, 0.5, 0.3))
    M.box("dunkel", (0, -0.2, h0 + 3.95), (3.6, 0.3, 0.1))
    # Laufkatze mit Haken (Dreh = Haken schwenkt)
    hk = m.part("Dreh", (0.4, -0.2, h0 + 3.6))
    hk.box("dunkel", (0.4, -0.2, h0 + 3.45), (0.6, 0.6, 0.3))
    hk.tube("stahl", (0.4, -0.2, h0 + 3.3), (0.4, -0.2, h0 + 1.6), 0.025, 6)
    hk.torus("stahl", (0.4, -0.2, h0 + 1.45), 0.14, 0.04, 12, 6, rot=(90, 0, 0))
    # Steuerhaus + Lichter
    M.box("weiss", (-0.9, 1.85, h0 + 0.6), (0.9, 0.8, 1.2))
    M.box("glas", (-0.9, 1.44, h0 + 0.9), (0.6, 0.04, 0.35))
    for x in (-1.65, 1.65):
        M.sphere("lampe", (x, -0.45, h0 + 3.4), 0.08, 8, 4)
        M.sphere("warn", (x, -0.2, h0 + 3.92), 0.07, 8, 4)
    return m


def b_port():
    m = Model("port", "Raumhafen", "Gebaeude", FP["port"]); M = m.body
    h0 = pad(M, FP["port"])
    # Kontrollturm
    M.cyl("hell", (-1.9, 1.2, h0), 0.45, 3.6, 20, r2=0.35)
    M.lathe("weiss", (-1.9, 1.2, h0 + 3.6), [(0.35, 0), (0.85, 0.3), (0.85, 0.35)], 24, cap0=False)
    M.lathe("glas", (-1.9, 1.2, h0 + 3.95), [(0.85, 0), (0.95, 0.45)], 24, cap0=False, cap1=False)
    M.lathe("dunkel", (-1.9, 1.2, h0 + 3.94), [(0.83, 0.0), (0.83, 0.02), (0, 0.02)], 24)
    M.lathe("weiss", (-1.9, 1.2, h0 + 4.4), [(0.98, 0), (0.98, 0.12), (0.4, 0.3), (0, 0.32)], 24)
    beacon(M, (-1.9, 1.2, h0 + 4.72), 0.6)
    # Landeplatz mit Lichtring
    M.cyl("dunkel", (0.6, -0.5, h0), 2.2, 0.15, 40)
    M.lathe("akzent", (0.6, -0.5, h0 + 0.15), [(2.1, 0), (2.1, 0.02), (1.9, 0.02), (1.9, 0)], 40, closed_loop=True)
    for k in range(16):
        a = 2 * math.pi * k / 16
        M.sphere("lampe" if k % 4 else "mint", (0.6 + 2.0 * math.cos(a), -0.5 + 2.0 * math.sin(a), h0 + 0.17), 0.05, 6, 3)
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        M.box("weiss", (0.6 + 0.9 * math.cos(a), -0.5 + 0.9 * math.sin(a), h0 + 0.16), (0.6, 0.1, 0.02), rot=(0, 0, math.degrees(a)))
    # Frachtcontainer
    for i, (x, y, c) in enumerate(((1.75, 2.05, "kiste2"), (0.45, 2.55, "akzent"), (1.75, 2.05, "rot"))):
        z = h0 + 0.4 + (0.8 if i == 2 else 0)
        M.box(c, (x, y, z), (1.2, 0.6, 0.78))
        for k in range(5):
            M.box("dunkel", (x - 0.5 + k * 0.25, y - 0.31, z), (0.03, 0.02, 0.7))
    return m


def b_rig():
    m = Model("rig", "Bohrturm", "Anlagen", 1.3); M = m.body
    M.lathe("platte", (0, 0, 0), [(1.3, 0), (1.2, 0.15), (0, 0.15)], 24)
    lattice(M, (0, 0, 0.15), (0, 0, 3.6), 1.2, 0.35, 6, "akzent", 0.045)
    M.box("dunkel", (0, 0, 3.7), (0.55, 0.55, 0.22))
    M.box("dunkel", (0.75, 0.4, 0.5), (0.6, 0.7, 0.7))
    vents(M, (0.75, 0.04, 0.55), 0.5, 0.4, 3)
    M.pipe("stahl", [(0.75, 0.4, 0.85), (0.75, 0.4, 1.3)], 0.06, 8)
    for i in range(4):
        M.cyl("stahl", (-0.8, -0.5 + i * 0.12, 0.2), 0.04, 1.6, 6, rot=(0, 75, 0))
    bo = m.part("Dreh", (0, 0, 0))
    bo.cyl("stahl", (0, 0, 0.05), 0.09, 3.6, 10)
    bo.cyl("dunkel", (0, 0, 2.6), 0.22, 0.5, 12)
    bo.box("akzent", (0, 0, 2.85), (0.5, 0.08, 0.12))
    M.sphere("warn", (0, 0, 3.86), 0.07, 8, 4)
    return m


def b_bagger():
    m = Model("bagger", "Erzbagger", "Anlagen"); M = m.body
    # Raupenfahrwerk
    for x in (-0.8, 0.8):
        track(M, (x, 0, 0), 2.4, 0.5, 0.42)
    M.box("dunkel", (0, 0, 0.6), (1.4, 1.8, 0.25))
    M.cyl("akzent", (0, 0, 0.72), 0.75, 0.2, 24)
    M.box("akzent", (0, 0.4, 1.25), (1.5, 1.4, 0.9))
    M.box("weiss", (0.45, -0.3, 1.55), (0.6, 0.6, 0.6))
    M.box("glas", (0.45, -0.61, 1.6), (0.5, 0.04, 0.35))
    M.box("dunkel", (0, 1.0, 1.15), (1.3, 0.4, 0.6))
    # Ausleger
    M.tube("akzent", (-0.3, -0.2, 1.2), (-0.3, -2.0, 2.0), 0.12, 10)
    M.tube("stahl", (-0.3, 0.6, 1.7), (-0.3, -2.0, 2.0), 0.04, 6)
    M.box("dunkel", (-0.3, -2.05, 2.0), (0.4, 0.3, 0.3))
    # Schaufelrad (Dreh um X)
    wh = m.part("Dreh", (-0.3, -2.2, 2.0))
    wh.cyl("stahl", (-0.55, -2.2, 2.0), 0.9, 0.08, 20, rot=(0, 90, 0))
    wh.cyl("stahl", (-0.05, -2.2, 2.0), 0.9, 0.08, 20, rot=(0, 90, 0))
    wh.cyl("dunkel", (-0.6, -2.2, 2.0), 0.18, 0.6, 12, rot=(0, 90, 0))
    for k in range(8):
        a = 2 * math.pi * k / 8
        p = (-0.3, -2.2 + 0.95 * math.cos(a), 2.0 + 0.95 * math.sin(a))
        wh.box("akzent", p, (0.5, 0.28, 0.22), rot=(math.degrees(a), 0, 0))
        wh.box("stahl", (p[0], p[1] + 0.13 * math.cos(a + 0.6), p[2] + 0.13 * math.sin(a + 0.6)), (0.52, 0.06, 0.06), rot=(math.degrees(a), 0, 0))
    beacon(M, (-0.55, 0.9, 1.7), 0.4)
    return m


def track(M, pos, L, W, H, mat="gummi"):
    """Raupenkette (Stadionprofil seitlich), Laufrollen und Stollen. pos = Mitte unten."""
    x, y, z = pos
    r = H / 2
    prof = []
    for i in range(9):
        a = math.pi / 2 + math.pi * i / 8
        prof.append((-L / 2 + r + r * math.cos(a), r + r * math.sin(a)))
    for i in range(9):
        a = -math.pi / 2 + math.pi * i / 8
        prof.append((L / 2 - r + r * math.cos(a), r + r * math.sin(a)))
    M.extrude(mat, (x, y, z), [(-a, b) for a, b in prof], W, "YZ")
    for k in range(int(L / 0.35)):  # Laufrollen sichtbar an der Außenseite
        yy = -L / 2 + r + (L - 2 * r) * k / max(1, int(L / 0.35) - 1)
        M.cyl("stahl", (x + (W / 2 + 0.005) * (1 if x >= 0 else -1), y + yy, z + r), r * 0.55, 0.04, 12, rot=(0, 90 if x >= 0 else -90, 0))
    for k in range(int(L / 0.18)):  # Stollen oben
        yy = -L / 2 + r + (L - 2 * r) * (k + 0.5) / int(L / 0.18)
        M.box("dunkel", (x, y + yy, z + H + 0.012), (W * 1.02, 0.06, 0.03))


# =============================== Fahrzeuge ==================================
def wheel(m, idx, pos, r, w, rim="stahl", hub="akzent", lugs=14):
    """Rad als eigenes Objekt Rad_<idx> (dreht um X)."""
    P = m.part("Rad_%d" % idx, pos)
    side = 1 if pos[0] >= 0 else -1
    prof = [(0.62 * r, -w / 2), (0.9 * r, -w / 2), (r, -0.32 * w), (r, 0.32 * w), (0.9 * r, w / 2), (0.62 * r, w / 2)]
    P.lathe("gummi", pos, prof, 20, rot=(0, 90, 0), closed_loop=True)
    P.cyl(rim, (pos[0] - w * 0.42, pos[1], pos[2]), 0.63 * r, w * 0.84, 16, rot=(0, 90, 0))
    P.cyl(hub, (pos[0] + side * w * 0.42, pos[1], pos[2]), 0.25 * r, 0.04, 10, rot=(0, 90 * side, 0))
    for k in range(5):
        a = 2 * math.pi * k / 5
        P.cyl("dunkel", (pos[0] + side * w * 0.42, pos[1] + 0.42 * r * math.cos(a), pos[2] + 0.42 * r * math.sin(a)), 0.05 * r, 0.03, 6, rot=(0, 90 * side, 0))
    for k in range(lugs):
        a = 2 * math.pi * k / lugs
        P.box("gummi", (pos[0], pos[1] + r * math.cos(a), pos[2] + r * math.sin(a)), (w * 0.8, 0.09 * r, 0.06 * r), rot=(math.degrees(a), 0, 0))
    return P


def lights(M, y, xs, z, rear_y=None, rear_xs=None, rz=None, size=0.08):
    """Scheinwerfer vorne (y), Rücklichter hinten."""
    for x in xs:
        M.box("dunkel", (x, y + 0.03, z), (size * 2.4, 0.06, size * 1.8))
        M.sphere("lampe", (x, y, z), size, 10, 5)
    if rear_y is not None:
        for x in (rear_xs or xs):
            M.box("warn", (x, rear_y, rz if rz is not None else z), (size * 2.2, 0.04, size * 1.0))


def tool_brush(m, pos, width, r):
    """Bürstenwalze vorne (Werkzeug, dreht um X)."""
    T = m.part("Werkzeug", pos)
    T.cyl("dunkel", (pos[0] - width / 2, pos[1], pos[2]), r * 0.55, width, 14, rot=(0, 90, 0))
    n = max(5, int(width / 0.09))
    for k in range(n):
        x = pos[0] - width / 2 + width * (k + 0.5) / n
        T.cyl("akzent2" if k % 2 else "hell", (x - 0.025, pos[1], pos[2]), r, 0.05, 14, rot=(0, 90, 0))
    return T


def tool_nozzle(m, pos, width, h):
    """Breite Saugdüse (Werkzeug)."""
    T = m.part("Werkzeug", pos)
    prof = [(0.0, 0.0), (-0.35 * h, 0.0), (-0.55 * h, 0.12 * h), (-0.2 * h, h), (0.15 * h, h), (0.2 * h, 0.2 * h)]
    T.extrude("hell", pos, [(-a, b) for a, b in prof], width, "YZ")
    T.box("gummi", (pos[0], pos[1] - 0.45 * h, pos[2] + 0.02), (width * 0.98, 0.12 * h, 0.05))
    for x in (-width / 2 + 0.05, width / 2 - 0.05):
        T.box("akzent", (pos[0] + x, pos[1] - 0.15 * h, pos[2] + 0.5 * h), (0.04, 0.6 * h, 0.9 * h))
    return T


def tool_drum(m, pos, width, r, teeth=10):
    """Fräswalze mit Zähnen (Werkzeug, dreht um X)."""
    T = m.part("Werkzeug", pos)
    T.cyl("stahl", (pos[0] - width / 2, pos[1], pos[2]), r, width, 18, rot=(0, 90, 0))
    for ring in range(max(3, int(width / 0.22))):
        x = pos[0] - width / 2 + width * (ring + 0.5) / max(3, int(width / 0.22))
        for k in range(teeth):
            a = 2 * math.pi * (k + ring * 0.5) / teeth
            T.box("dunkel", (x, pos[1] + (r + 0.04) * math.cos(a), pos[2] + (r + 0.04) * math.sin(a)), (0.05, 0.05, 0.12), rot=(math.degrees(a) + 90, 0, 20))
    for sx in (-1, 1):
        T.box("akzent", (pos[0] + sx * (width / 2 + 0.04), pos[1], pos[2]), (0.06, r * 2.3, r * 2.3))
    return T


def cab(M, pos, w, d, h, body, glass_front=True, roof_light=True):
    """Fahrerkabine: abgeschrägtes Seitenprofil, Glasfront, Seitenfenster."""
    x, y, z = pos
    prof = [(-d / 2, 0), (d / 2, 0), (d / 2, h * 0.8), (d / 2 - 0.12, h), (-d / 2 + d * 0.3, h), (-d / 2, h * 0.45)]
    M.extrude(body, (x, y, z), prof, w, "YZ")
    if glass_front:
        a = math.degrees(math.atan2(h * 0.55, d * 0.3))
        M.box("glas", (x, y - d / 2 + d * 0.15 - 0.015, z + h * 0.72), (w * 0.86, 0.03, math.hypot(h * 0.55, d * 0.3) * 0.9), rot=(90 - a, 0, 0))
    for sx in (-1, 1):
        M.box("glas", (x + sx * (w / 2 + 0.005), y + d * 0.12, z + h * 0.68), (0.02, d * 0.5, h * 0.35))
    if roof_light:
        M.box("dunkel", (x, y + d * 0.1, z + h + 0.03), (w * 0.7, 0.12, 0.06))
        for sx in (-0.25, 0.0, 0.25):
            M.sphere("warn" if sx == 0 else "lampe", (x + sx * w, y + d * 0.1 - 0.05, z + h + 0.06), 0.04, 6, 3)


def v_staubi():
    m = Model("staubi", "Staubi", "Fahrzeuge"); M = m.body
    r = 0.55
    M.lathe("weiss", (0, 0, 0.05), [(r * 0.9, 0), (r, 0.06), (r, 0.22), (r * 0.92, 0.28), (0, 0.29)], 32)
    M.lathe("dunkel", (0, 0, 0.12), [(r + 0.02, 0), (r + 0.02, 0.1)], 32, a0=200, a1=340)
    M.lathe("akzent", (0, 0, 0.33), [(r * 0.5, 0), (r * 0.5, 0.03), (r * 0.35, 0.05), (0, 0.05)], 24)
    M.sphere("glas", (0, -0.12, 0.38), 0.1, 12, 5, zcut=0)
    M.sphere("lampe", (0, -r * 0.98, 0.2), 0.05, 8, 4)
    M.box("mint", (0, r * 0.55, 0.335), (0.16, 0.04, 0.02))
    M.cyl("gummi", (0, 0, 0.0), r * 0.85, 0.05, 24)
    T = m.part("Werkzeug", (0, -r * 0.6, 0.02))
    for sx in (-1, 1):
        c = (sx * r * 0.55, -r * 0.6, 0.02)
        T.cyl("dunkel", c, 0.05, 0.03, 8)
        for k in range(3):
            a = math.radians(k * 120)
            T.box("akzent2", (c[0] + 0.08 * math.cos(a), c[1] + 0.08 * math.sin(a), c[2] + 0.015), (0.16, 0.02, 0.01), rot=(0, 0, math.degrees(a)))
    return m


def v_buggy(key="buggy", name="Reinigungsbuggy", body="akzent", crane=False):
    m = Model(key, name, "Fahrzeuge"); M = m.body
    s = 0.8
    L, W = 2.4 * s, 1.5 * s
    wr, ww = 0.4 * s, 0.32 * s
    M.box("dunkel", (0, 0, 0.42 * s), (W * 0.62, L * 0.9, 0.12 * s))  # Bodenwanne
    # Rohrrahmen / Überrollkäfig
    z0, z1 = 0.5 * s, 1.35 * s
    for sx in (-1, 1):
        x = sx * W * 0.32
        M.pipe(body, [(x, -L * 0.42, z0), (x, -L * 0.22, z1), (x, L * 0.12, z1), (x, L * 0.3, z0 + 0.15)], 0.035, 8)
        M.tube(body, (x, -L * 0.42, z0), (x, L * 0.42, z0), 0.035, 8)
        for y in (-L * 0.36, L * 0.36):  # Achsschwingen
            M.tube("stahl", (x, y, z0), (sx * (W / 2 - ww / 2), y, wr), 0.04, 8)
            M.cyl("akzent2", (sx * W * 0.36, y + 0.06, z0 - 0.05), 0.045, 0.32 * s, 8, rot=(0, sx * -25, 0))
    M.tube(body, (-W * 0.32, -L * 0.22, z1), (W * 0.32, -L * 0.22, z1), 0.035, 8)
    M.tube(body, (-W * 0.32, L * 0.12, z1), (W * 0.32, L * 0.12, z1), 0.035, 8)
    # Sitz, Konsole, Nase
    M.box("gummi", (0, 0.0, 0.62 * s), (0.45 * s, 0.45 * s, 0.12 * s))
    M.box("gummi", (0, 0.2 * s, 0.85 * s), (0.45 * s, 0.1 * s, 0.45 * s), rot=(-12, 0, 0))
    M.box("dunkel", (0, -0.38 * s, 0.72 * s), (0.4 * s, 0.15 * s, 0.22 * s))
    M.box("mint", (0, -0.45 * s, 0.82 * s), (0.25 * s, 0.01, 0.06 * s))
    M.extrude(body, (0, 0, 0.42 * s), [(-L * 0.5, 0.0), (-L * 0.5, 0.18 * s), (-L * 0.28, 0.3 * s), (-L * 0.28, 0.0)], W * 0.6, "YZ")
    lights(M, -L * 0.5 - 0.01, (-0.18 * s, 0.18 * s), 0.52 * s, L * 0.47, (-0.25 * s, 0.25 * s), 0.6 * s, 0.05)
    for x in (-0.2 * s, 0.2 * s):
        M.sphere("lampe", (x, -L * 0.22 - 0.05, z1 + 0.04), 0.045, 8, 4)
    # Tank hinten
    tank_h(M, (0, L * 0.3, 0.78 * s), 0.2 * s, 0.55 * s, "weiss", 0, 1)
    M.cyl("stahl", (0.25 * s, L * 0.3, 0.96 * s), 0.05, 0.25 * s, 8)
    beacon(M, (-W * 0.3, L * 0.12, z1), 0.25 * s, "glut" if crane else "warn")
    if crane:  # Suchtrupp: Bergekran hinten
        M.cyl("dunkel", (0, L * 0.42, 0.62 * s), 0.12, 0.12, 12)
        M.tube("akzent", (0, L * 0.42, 0.72 * s), (0, L * 0.62, 1.5 * s), 0.05, 8)
        M.tube("stahl", (0, L * 0.62, 1.5 * s), (0, L * 0.62, 0.8 * s), 0.012, 4)
        M.torus("stahl", (0, L * 0.62, 0.76 * s), 0.06, 0.018, 10, 5, rot=(90, 0, 0))
        M.box("gummi", (0, L * 0.3, 1.02 * s), (0.5 * s, 0.4 * s, 0.1 * s))
    else:
        tool_brush(m, (0, -L * 0.55, 0.22 * s), W * 0.85, 0.2 * s)
    for i, (x, y) in enumerate(((-1, -1), (1, -1), (-1, 1), (1, 1))):
        wheel(m, i, (x * (W / 2 - ww / 2 + 0.05), y * L * 0.36, wr), wr, ww)
    return m


def v_sauger():
    m = Model("sauger", "Industriesauger", "Fahrzeuge"); M = m.body
    s = 1.15
    L, W = 2.4 * s, 1.5 * s
    wr, ww = 0.42 * s, 0.3 * s
    M.box("dunkel", (0, 0, 0.55 * s), (W * 0.82, L * 0.95, 0.3 * s))
    cab(M, (0, -L * 0.3, 0.7 * s), W * 0.8, 0.9 * s, 0.75 * s, "akzent2")
    tank_h(M, (0, L * 0.18, 1.15 * s), 0.48 * s, 0.9 * s, "weiss", 90, 2)
    for y in (-0.05, 0.42):
        M.box("akzent2", (0, y * s + L * 0.05, 0.85 * s), (W * 0.86, 0.16 * s, 0.25 * s))
    M.pipe("stahl", [(W * 0.32, -L * 0.52, 0.45 * s), (W * 0.32, -L * 0.42, 1.25 * s), (W * 0.2, -L * 0.05, 1.45 * s)], 0.07 * s, 10)
    M.box("dunkel", (0, L * 0.49, 0.75 * s), (W * 0.6, 0.06, 0.4 * s))
    vents(M, (0, L * 0.5, 0.78 * s), W * 0.5, 0.3 * s, 3, "y", 0.04)
    for sx in (-1, 1):
        M.box("akzent2", (sx * W * 0.43, 0, 0.66 * s), (0.08, L * 0.95, 0.12 * s))  # Seitenschweller
        M.box("dunkel", (sx * W * 0.47, -L * 0.36, 0.95 * s), (0.08, 0.08, 0.08))  # Spiegel
    lights(M, -L * 0.48, (-W * 0.3, W * 0.3), 0.72 * s, L * 0.48 + 0.02, None, 0.62 * s, 0.07)
    M.cyl("stahl", (W * 0.25, L * 0.3, 1.6 * s), 0.025, 0.4 * s, 6)
    tool_nozzle(m, (0, -L * 0.52, 0.05 * s), W * 1.05, 0.42 * s)
    for i, (x, y) in enumerate(((-1, -1), (1, -1), (-1, 1), (1, 1))):
        wheel(m, i, (x * (W / 2 - ww / 2 + 0.06), y * L * 0.33, wr), wr, ww)
    return m


def v_truck():
    m = Model("truck", "Schwerer Reinigungs-Truck", "Fahrzeuge"); M = m.body
    s = 1.55
    L, W = 2.4 * s, 1.5 * s
    wr, ww = 0.42 * s, 0.3 * s
    M.box("dunkel", (0, 0.05, 0.6 * s), (W * 0.7, L * 0.98, 0.25 * s))
    cab(M, (0, -L * 0.34, 0.72 * s), W * 0.92, 0.95 * s, 0.85 * s, "rot")
    # Tankaufbau mit Rippen
    M.box("hell", (0, L * 0.15, 1.15 * s), (W * 0.95, L * 0.62, 0.95 * s))
    for k in range(6):
        M.box("rot" if k % 2 else "stahl", (0, L * 0.15 - L * 0.28 + k * L * 0.11, 1.15 * s), (W * 0.98, 0.06, 0.98 * s))
    M.box("akzent", (0, L * 0.15, 1.65 * s), (W * 0.9, L * 0.55, 0.06))
    railing(M, [(-W * 0.42, -L * 0.13, 1.65 * s), (-W * 0.42, L * 0.44, 1.65 * s)], 0.3 * s, 0.5, "akzent", 0.02)
    ladder(M, (W * 0.49, L * 0.4, 0.5 * s), 1.15 * s, (1, 0, 0), 0.35)
    M.box("dunkel", (0, L * 0.47, 1.1 * s), (W * 0.7, 0.06, 0.7 * s))  # Heckklappe
    for x in (-0.3, 0.3):
        M.torus("stahl", (x * s, L * 0.48, 1.1 * s), 0.07 * s, 0.015, 10, 5, rot=(90, 0, 0))
    for sx in (-1, 1):  # Auspuff
        M.cyl("stahl", (sx * W * 0.4, -L * 0.12, 0.7 * s), 0.05 * s, 1.3 * s, 10)
        M.cyl("dunkel", (sx * W * 0.4, -L * 0.12, 2.0 * s), 0.055 * s, 0.05, 10)
        M.box("dunkel", (sx * W * 0.5, -L * 0.42, 1.25 * s), (0.08, 0.1, 0.12))
    lights(M, -L * 0.52, (-W * 0.34, W * 0.34), 0.75 * s, L * 0.5, None, 0.7 * s, 0.08)
    tool_brush(m, (0, -L * 0.58, 0.24 * s), W * 1.05, 0.24 * s)
    for i, (x, y) in enumerate(((-1, -0.36), (1, -0.36), (-1, 0.18), (1, 0.18), (-1, 0.38), (1, 0.38))):
        wheel(m, i, (x * (W / 2 - ww / 2 + 0.08), y * L, wr), wr, ww)
    return m


def tracked(m, L, W, H, body, mat_track="gummi"):
    """Kettenfahrwerk links/rechts + Rahmen."""
    M = m.body
    for sx in (-1, 1):
        track(M, (sx * (W / 2 - 0.2), 0, 0), L, 0.38, H, mat_track)
        M.box(body, (sx * (W / 2 - 0.2), 0, H + 0.08), (0.46, L * 0.92, 0.06))  # Kettenschutz
    M.box("dunkel", (0, 0, H * 0.75), (W - 0.8, L * 0.85, H * 0.5))


def v_eisfraese():
    m = Model("eisfraese", "Eisfräse", "Fahrzeuge"); M = m.body
    L, W, H = 3.0, 1.9, 0.55
    tracked(m, L, W, H, "eis")
    M.extrude("weiss", (0, 0.15, H + 0.1), [(-1.0, 0), (1.25, 0), (1.25, 0.6), (0.6, 0.75), (-0.7, 0.75), (-1.0, 0.45)], W - 0.35, "YZ")
    cab(M, (-0.45, -0.75, H + 0.75), 0.9, 0.8, 0.7, "eis")
    for k in range(4):
        M.box("glut", (0.45, 0.4 + k * 0.18, H + 0.86), (0.6, 0.06, 0.02))
    M.box("eis", (0.45, 0.65, H + 0.95), (0.75, 0.8, 0.18))
    M.cyl("stahl", (0.55, 1.1, H + 0.85), 0.07, 0.6, 8)
    lights(M, -1.0, (-0.55, 0.55), H + 0.4, 1.42, None, H + 0.4, 0.07)
    M.tube("akzent", (-0.7, -1.0, H + 0.3), (-0.6, -1.5, H * 0.75), 0.08, 8)
    M.tube("akzent", (0.7, -1.0, H + 0.3), (0.6, -1.5, H * 0.75), 0.08, 8)
    tool_drum(m, (0, -1.68, H * 0.62), W * 0.95, 0.3)
    beacon(M, (0.6, -0.4, H + 0.85), 0.3)
    return m


def v_schlammraupe():
    m = Model("schlammraupe", "Schlammraupe", "Fahrzeuge"); M = m.body
    L, W, H = 3.2, 2.0, 0.6
    tracked(m, L, W, H, "akzent2")
    cab(M, (0, -0.95, H + 0.1), 1.1, 0.85, 0.75, "akzent2")
    tank_h(M, (0, 0.55, H + 0.75), 0.6, 1.0, "hell", 90, 2)
    for sx in (-1, 1):  # Schmutzlappen
        M.box("gummi", (sx * (W / 2 - 0.2), L / 2 + 0.02, H * 0.6), (0.4, 0.03, H * 0.9))
    # Pumpenarm (Werkzeug schwenkt)
    T = m.part("Werkzeug", (0.5, -0.4, H + 1.1))
    T.cyl("dunkel", (0.5, -0.4, H + 0.9), 0.15, 0.25, 12)
    T.pipe("akzent", [(0.5, -0.4, H + 1.15), (0.5, -1.4, H + 1.35), (0.5, -2.0, H * 0.6), (0.5, -2.0, 0.15)], 0.09, 10)
    T.lathe("dunkel", (0.5, -2.0, 0.02), [(0.25, 0), (0.25, 0.08), (0.1, 0.18), (0.1, 0.3), (0, 0.3)], 14)
    M.pipe("gummi", [(-0.3, 0.0, H + 0.4), (-0.6, 0.6, H + 0.2), (-0.4, 1.1, H + 0.75)], 0.07, 10)
    lights(M, -1.62, (-0.7, 0.7), H + 0.35, 1.62, None, H + 0.4, 0.07)
    beacon(M, (-0.4, -0.7, H + 0.85), 0.25)
    return m


def v_lavapanzer():
    m = Model("lavapanzer", "Lavapanzer", "Fahrzeuge"); M = m.body
    L, W, H = 3.6, 2.4, 0.65
    tracked(m, L, W, H, "rot", "dunkel")
    hull = [(-1.75, 0), (1.75, 0), (1.75, 0.55), (1.2, 0.95), (-0.6, 0.95), (-1.45, 0.45)]
    M.extrude("dunkel", (0, 0, H + 0.05), hull, W - 0.25, "YZ")
    for k in range(5):  # Hitzeschutzplatten
        y = -0.8 + k * 0.5
        M.box("rot", (0, y, H + 1.03), (W - 0.45, 0.42, 0.06))
    for sx in (-1, 1):
        for k in range(3):
            M.box("glut", (sx * (W / 2 - 0.1), 0.2 + k * 0.4, H + 0.4), (0.02, 0.25, 0.08))
        M.box("rot", (sx * (W / 2 - 0.05), 0, H + 0.35), (0.06, L * 0.85, 0.5))
    M.box("glas", (0, -1.1, H + 0.82), (1.0, 0.04, 0.2), rot=(-28, 0, 0))
    for x in (-0.5, 0.5):
        M.cyl("stahl", (x, 1.4, H + 0.95), 0.1, 0.7, 10)
        M.cyl("glut", (x, 1.4, H + 1.65), 0.075, 0.02, 10)
    lights(M, -1.77, (-0.85, 0.85), H + 0.35, 1.77, None, H + 0.45, 0.08)
    # Pflugschild (Werkzeug)
    T = m.part("Werkzeug", (0, -2.0, 0.4))
    T.extrude("akzent", (0, -2.05, 0.02), [(0.25, 0), (-0.05, 0), (-0.3, 0.3), (-0.2, 0.75), (0.05, 0.75), (0.0, 0.3)], W + 0.2, "YZ")
    for sx in (-1, 1):
        T.tube("stahl", (sx * 0.7, -1.95, 0.4), (sx * 0.7, -1.6, H + 0.3), 0.08, 8)
    return m


def hover_pads(M, pts, r=0.22):
    for p in pts:
        M.cyl("dunkel", p, r, 0.12, 16)
        M.cyl("mint", (p[0], p[1], p[2] - 0.015), r * 0.75, 0.02, 16)


def v_schwebe():
    m = Model("schwebe", "Schwebereiniger", "Fahrzeuge"); M = m.body
    z = 0.55
    hull = []
    for i in range(13):
        a = math.pi * i / 12
        hull.append((0.85 * math.cos(a), -0.6 - 0.75 * math.sin(a)))
    hull += [(-0.85, 1.25), (0.85, 1.25)]
    hull = [(x, y) for x, y in hull]
    M.extrude("akzent2", (0, 0, z), hull, 0.35, "XY")
    M.extrude("weiss", (0, 0, z + 0.35), [(x * 0.9, y * 0.92 + 0.05) for x, y in hull], 0.12, "XY")
    M.sphere("glas", (0, -0.55, z + 0.47), 0.5, 20, 6, zcut=0)
    M.lathe("dunkel", (0, -0.55, z + 0.47), [(0.53, 0), (0.53, 0.05), (0, 0.05)], 20)
    for sx in (-1, 1):  # Seitliche Mantelpropeller
        c = (sx * 1.15, 0.5, z + 0.25)
        M.torus("akzent2", c, 0.38, 0.06, 20, 6)
        M.cyl("dunkel", (c[0], c[1], c[2] - 0.05), 0.08, 0.1, 10)
        for k in range(3):
            M.box("hell", c, (0.68, 0.06, 0.02), rot=(15, 0, k * 60))
        M.box("hell", (sx * 0.95, 0.5, z + 0.25), (0.3, 0.12, 0.06))
    hover_pads(M, [(-0.5, -0.5, z - 0.12), (0.5, -0.5, z - 0.12), (-0.5, 0.8, z - 0.12), (0.5, 0.8, z - 0.12)])
    lights(M, -1.36, (-0.4, 0.4), z + 0.2, 1.26, None, z + 0.2, 0.06)
    tool_nozzle(m, (0, -1.0, 0.05), 1.3, 0.38)
    m.meshes[-1].box("gummi", (0, -0.95, 0.5), (0.12, 0.3, 0.12))
    beacon(M, (0.4, 0.9, z + 0.47), 0.3, "mint")
    return m


def v_magnetgleiter():
    m = Model("magnetgleiter", "Magnetgleiter", "Fahrzeuge"); M = m.body
    z = 0.45
    side = [(-2.1, 0.15), (-1.7, 0.05), (1.6, 0.05), (2.0, 0.25), (2.0, 0.55), (0.9, 0.75), (-0.6, 0.75), (-1.6, 0.5)]
    M.extrude("weiss", (0, 0, z), side, 1.6, "YZ")
    M.extrude("akzent", (0, 0, z + 0.3), [(-2.15, 0.0), (2.02, 0.0), (2.02, 0.08), (-2.0, 0.08)], 1.64, "YZ")
    # Kanzel
    can = [(-1.3, 0.55), (-0.85, 1.05), (0.2, 1.1), (0.55, 0.75)]
    M.extrude("glas", (0, 0, z), can + [(0.55, 0.7), (-1.3, 0.5)], 1.0, "YZ")
    # Magnetkufen
    for sx in (-1, 1):
        M.box("dunkel", (sx * 0.65, 0, z - 0.08), (0.22, 3.6, 0.16))
        M.box("mint", (sx * 0.65, 0, z - 0.17), (0.12, 3.4, 0.02))
        for y in (-1.2, 0, 1.2):
            M.box("stahl", (sx * 0.65, y, z + 0.02), (0.1, 0.1, 0.18))
    for sx in (-1, 1):  # Heckflossen
        M.extrude("akzent", (sx * 0.55, 1.75, z + 0.7), [(-0.3, 0), (0.3, 0), (0.35, 0.45), (0.1, 0.45)], 0.06, "YZ")
    M.cyl("mint", (0, 2.02, z + 0.4), 0.18, 0.05, 16, rot=(-90, 0, 0))
    lights(M, -2.12, (-0.5, 0.5), z + 0.2, 2.02, (-0.55, 0.55), z + 0.5, 0.06)
    tool_brush(m, (0, -2.3, 0.22), 1.6, 0.2)
    return m


def v_kryokoloss():
    m = Model("kryokoloss", "Kryo-Koloss", "Fahrzeuge"); M = m.body
    L, W = 4.8, 3.0
    wr, ww = 0.62, 0.45
    M.box("dunkel", (0, 0, 0.85), (W * 0.62, L * 0.95, 0.4))
    # Hochkabine vorne
    M.box("eis", (0, -1.75, 1.35), (W * 0.7, 1.1, 0.6))
    cab(M, (0, -1.75, 1.65), 1.6, 1.0, 0.8, "eis")
    ladder(M, (W * 0.36, -1.2, 0.5), 1.6, (1, 0, 0), 0.4)
    # Drei Kryotanks
    for k, y in enumerate((-0.5, 0.55, 1.6)):
        tank_h(M, (0, y, 1.75), 0.62, 1.7, "weiss" if k != 1 else "hell", 0, 2)
    M.box("stahl", (0, 0.55, 2.42), (W * 0.5, 2.6, 0.06))
    railing(M, [(-0.7, -0.6, 2.45), (-0.7, 1.75, 2.45)], 0.35, 0.6, "akzent", 0.02)
    railing(M, [(0.7, -0.6, 2.45), (0.7, 1.75, 2.45)], 0.35, 0.6, "akzent", 0.02)
    for y in (-0.5, 0.55, 1.6):
        M.cyl("mint", (W * 0.25, y, 2.38), 0.07, 0.08, 10)
    M.box("dunkel", (0, 2.35, 1.2), (W * 0.55, 0.15, 0.6))
    vents(M, (0, 2.43, 1.2), W * 0.45, 0.45, 4, "y", 0.05)
    lights(M, -2.33, (-0.6, -0.3, 0.3, 0.6), 1.35, 2.42, (-0.7, 0.7), 1.1, 0.08)
    beacon(M, (-0.65, -1.6, 2.45), 0.35)
    tool_drum(m, (0, -2.75, 0.45), W * 0.95, 0.42, 12)
    for sx in (-1, 1):
        M.tube("akzent", (sx * 1.0, -2.3, 0.85), (sx * 1.2, -2.7, 0.5), 0.1, 10)
    ys = (-1.55, -0.55, 0.55, 1.55)
    i = 0
    for y in ys:
        for sx in (-1, 1):
            wheel(m, i, (sx * (W / 2 - ww / 2), y, wr), wr, ww, lugs=16)
            i += 1
    return m


# ================================ Drohnen ===================================
def d_drone(heavy=False):
    key, name = ("lastdrohne", "Lastdrohne") if heavy else ("drohne", "Transportdrohne")
    m = Model(key, name, "Drohnen"); M = m.body
    s = 2.0 if heavy else 1.0
    z = 0.55 * s
    octa = [(0.45 * s * math.cos(math.pi / 8 + k * math.pi / 4), 0.55 * s * math.sin(math.pi / 8 + k * math.pi / 4)) for k in range(8)]
    M.extrude("weiss", (0, 0, z), octa, 0.22 * s, "XY")
    M.extrude("akzent" if not heavy else "dunkel", (0, 0, z + 0.22 * s), [(x * 0.8, y * 0.8) for x, y in octa], 0.07 * s, "XY")
    M.sphere("glas", (0, -0.3 * s, z + 0.12 * s), 0.09 * s, 10, 5)
    for k, (sx, sy) in enumerate(((-1, -1), (1, -1), (1, 1), (-1, 1))):
        hub = (sx * 0.75 * s, sy * 0.75 * s, z + 0.16 * s)
        M.tube("dunkel", (sx * 0.3 * s, sy * 0.35 * s, z + 0.11 * s), hub, 0.045 * s, 8)
        M.cyl("dunkel", (hub[0], hub[1], hub[2] - 0.08 * s), 0.08 * s, 0.14 * s, 12)
        M.torus("akzent" if heavy else "hell", (hub[0], hub[1], hub[2] + 0.06 * s), 0.38 * s, 0.03 * s, 24, 6)
        for k2 in range(3):
            a = math.radians(k2 * 120 + 30)
            M.tube("stahl", hub, (hub[0] + 0.38 * s * math.cos(a), hub[1] + 0.38 * s * math.sin(a), hub[2] + 0.06 * s), 0.012 * s, 4)
        M.sphere("mint" if sy < 0 else "warn", (sx * 0.95 * s, sy * 0.95 * s, z + 0.2 * s), 0.035 * s, 6, 3)
        R = m.part("Rotor_%d" % k, (hub[0], hub[1], hub[2] + 0.07 * s))
        R.cyl("akzent2", (hub[0], hub[1], hub[2] + 0.06 * s), 0.05 * s, 0.05 * s, 10)
        for b in (0, 180):
            R.box("dunkel", (hub[0] + 0.17 * s * math.cos(math.radians(b + 20 * k)), hub[1] + 0.17 * s * math.sin(math.radians(b + 20 * k)), hub[2] + 0.085 * s),
                  (0.32 * s, 0.05 * s, 0.012 * s), rot=(8 if b == 0 else -8, 0, b + 20 * k))
    # Landekufen
    for sx in (-1, 1):
        M.tube("stahl", (sx * 0.25 * s, -0.3 * s, z), (sx * 0.35 * s, -0.3 * s, 0.03 * s), 0.02 * s, 6)
        M.tube("stahl", (sx * 0.25 * s, 0.3 * s, z), (sx * 0.35 * s, 0.3 * s, 0.03 * s), 0.02 * s, 6)
        M.tube("dunkel", (sx * 0.35 * s, -0.45 * s, 0.03 * s), (sx * 0.35 * s, 0.45 * s, 0.03 * s), 0.025 * s, 6)
    # Ladung / Greifer (beweglich)
    C = m.part("Ladung", (0, 0, z))
    if heavy:
        C.box("dunkel", (0, 0, z - 0.06 * s), (0.5 * s, 0.5 * s, 0.1 * s))
        for k in range(4):
            a = math.radians(45 + k * 90)
            p = (0.25 * s * math.cos(a), 0.25 * s * math.sin(a), z - 0.1 * s)
            q = (0.33 * s * math.cos(a), 0.33 * s * math.sin(a), z - 0.35 * s)
            C.tube("akzent", p, q, 0.03 * s, 6)
            C.tube("akzent", q, (0.22 * s * math.cos(a), 0.22 * s * math.sin(a), z - 0.5 * s), 0.025 * s, 6)
    else:
        C.box("kiste1", (0, 0, z - 0.17 * s), (0.42 * s, 0.42 * s, 0.26 * s))
        for x in (-0.21 * s, 0.21 * s):
            C.tube("stahl", (x, 0, z), (x, 0, z - 0.3 * s), 0.012 * s, 4)
        C.box("dunkel", (0, 0, z - 0.31 * s), (0.46 * s, 0.46 * s, 0.03 * s))
    return m


# ============================== Förderbänder ================================
# Maße wie im Spiel (WorldView.cs): halbe Bandbreite 0,42 m, Lauffläche 0,55 m
# über dem Boden, goldene Banden, dunkler Rahmen darunter. Bänder laufen
# entlang +Y, beginnen bei y = 0 und liegen mittig auf x = 0.
BW, DECK = 0.42, 0.55


def _arc(R, deg, right=True, n=None, z=DECK):
    n = n or max(4, int(abs(deg) / 7.5))
    pts = []
    for i in range(n + 1):
        a = math.radians(deg) * i / n
        x = R - R * math.cos(a)
        pts.append(((x if right else -x), R * math.sin(a), z))
    return pts


def _line(L, z=DECK, n=None):
    n = n or max(1, int(L / 0.5))
    return [(0.0, L * i / n, z) for i in range(n + 1)]


def belt_on_path(M, path, cleats=True, left=True, right=True):
    """Lauffläche, Banden, Rahmen und Mitnehmer entlang eines Pfades (Pfad = Oberkante Lauffläche)."""
    M.sweep("band", path, [(-BW, -0.04), (BW, -0.04), (BW, 0.0), (-BW, 0.0)])
    if left:
        M.sweep("akzent", path, [(-BW - 0.09, -0.06), (-BW, -0.06), (-BW, 0.2), (-BW - 0.09, 0.2)])
    if right:
        M.sweep("akzent", path, [(BW, -0.06), (BW + 0.09, -0.06), (BW + 0.09, 0.2), (BW, 0.2)])
    M.sweep("dunkel", path, [(-BW - 0.06, -0.32), (BW + 0.06, -0.32), (BW + 0.06, -0.05), (-BW - 0.06, -0.05)])
    if cleats:  # Mitnehmer-Pfeile (laufen im Spiel als Textur mit)
        L = sum(_len(_sub(b, a)) for a, b in zip(path, path[1:]))
        acc, j, nxt = 0.0, 0, 0.25
        for a, b in zip(path, path[1:]):
            seg = _len(_sub(b, a))
            while nxt <= acc + seg and nxt < L - 0.1:
                t = (nxt - acc) / seg
                p = _add(a, _mul(_sub(b, a), t))
                tg = _norm(_sub(b, a))
                yaw = math.degrees(math.atan2(-tg[0], tg[1]))
                pitch = math.degrees(math.asin(max(-1, min(1, tg[2]))))
                R = _mm(_rot(0, 0, yaw), _rot(pitch, 0, 0))
                for sx in (-1, 1):
                    q = _add(p, _mv(R, (sx * 0.17, 0, 0.012)))
                    M.box("platte", q, (0.36, 0.06, 0.025), R=_mm(R, _rot(0, 0, sx * 35)))
                nxt += 0.5
            acc += seg


def support(M, p, top, yaw=0.0):
    """Bandstütze: zwei Pfosten bis unter den Rahmen, Querträger, Fußplatten."""
    R = _rot(0, 0, yaw)
    for sx in (-1, 1):
        q = _add(p, _mv(R, (sx * (BW - 0.1), 0, 0)))
        M.cyl("dunkel", q, 0.07, top, 8, r2=0.06)
        M.cyl("stahl", q, 0.16, 0.06, 10, r2=0.14)
        for k in range(4):
            a = math.radians(45 + 90 * k)
            M.cyl("dunkel", (q[0] + 0.11 * math.cos(a), q[1] + 0.11 * math.sin(a), q[2] + 0.06), 0.02, 0.025, 6)
    M.box("stahl", _add(p, (0, 0, top * 0.45)), ((BW - 0.1) * 2 + 0.12, 0.1, 0.08), R=R)
    M.box("dunkel", _add(p, (0, 0, top - 0.02)), ((BW - 0.1) * 2 + 0.2, 0.18, 0.06), R=R)


def bm(key, name):
    return Model(key, name, "Foerderband")


def f_gerade(L):
    m = bm("band_gerade_%dm" % L, "Förderband gerade %d m" % L)
    belt_on_path(m.body, _line(L))
    return m


def f_kurve(deg, R):
    m = bm("band_kurve_%d_r%s" % (deg, str(R).replace(".", "_")), "Förderband Kurve %d° (Radius %s m)" % (deg, str(R).replace(".", ",")))
    belt_on_path(m.body, _arc(R, deg, True))
    return m


def f_rampe(rise=1.0, L=3.0):
    m = bm("band_rampe", "Förderband Rampe (+%s m auf %s m)" % (str(rise).replace(".", ","), str(L).replace(".", ",")))
    n = 12
    path = [(0.0, L * i / n, DECK + rise * (3 * (i / n) ** 2 - 2 * (i / n) ** 3)) for i in range(n + 1)]
    belt_on_path(m.body, path)
    support(m.body, (0, L * 0.55, 0), DECK + rise * 0.6 - 0.32)
    return m


def f_bruecke(L=4.0, lift=1.0):
    m = bm("band_bruecke", "Förderband Brücke (%s m hoch)" % str(lift).replace(".", ","))
    belt_on_path(m.body, _line(L, DECK + lift))
    for y in (0.4, L - 0.4):
        support(m.body, (0, y, 0), DECK + lift - 0.32)
        m.body.tube("stahl", (-(BW - 0.1), y, 0.3), (-(BW - 0.1), y + (0.8 if y < 1 else -0.8), DECK + lift - 0.4), 0.03, 6)
        m.body.tube("stahl", ((BW - 0.1), y, 0.3), ((BW - 0.1), y + (0.8 if y < 1 else -0.8), DECK + lift - 0.4), 0.03, 6)
    return m


def f_ende():
    m = bm("band_ende", "Förderband Ende (Umlenkrolle)")
    M = m.body
    belt_on_path(M, _line(0.8), cleats=False)
    M.cyl("stahl", (-BW - 0.02, 0.8, DECK - 0.14), 0.16, BW * 2 + 0.04, 16, rot=(0, 90, 0))
    for sx in (-1, 1):
        M.box("dunkel", (sx * (BW + 0.1), 0.75, DECK - 0.14), (0.06, 0.4, 0.3))
        M.cyl("akzent", (sx * (BW + 0.13), 0.8, DECK - 0.14), 0.07, 0.03, 10, rot=(0, 90 * sx, 0))
    return m


def f_weiche():
    m = bm("band_weiche_y", "Förderband Y-Weiche")
    M = m.body
    belt_on_path(M, _line(3.0, n=6), left=False)
    for a, b in ((0.0, 1.2), (2.75, 3.0)):  # linke Bande mit Lücke für den Zulauf
        M.sweep("akzent", [(0.0, a, DECK), (0.0, b, DECK)], [(-BW - 0.09, -0.06), (-BW, -0.06), (-BW, 0.2), (-BW - 0.09, 0.2)])
    P0, P1, P2, P3 = (-1.9, 0.0), (-1.9, 1.0), (-0.9, 1.5), (-0.45, 2.1)
    pts = []
    for i in range(13):
        t = i / 12
        x = (1 - t) ** 3 * P0[0] + 3 * (1 - t) ** 2 * t * P1[0] + 3 * (1 - t) * t * t * P2[0] + t ** 3 * P3[0]
        y = (1 - t) ** 3 * P0[1] + 3 * (1 - t) ** 2 * t * P1[1] + 3 * (1 - t) * t * t * P2[1] + t ** 3 * P3[1]
        pts.append((x, y, DECK + 0.005))
    belt_on_path(M, pts)
    M.box("dunkel", (-BW - 0.05, 1.95, DECK - 0.03), (0.16, 1.5, 0.05))  # Übergangsblech
    return m


def f_stuetze(lift=0.0):
    key = "band_stuetze" if lift == 0 else "band_stuetze_hoch"
    m = bm(key, "Bandstütze" + ("" if lift == 0 else " hoch (+%s m)" % str(lift).replace(".", ",")))
    support(m.body, (0, 0, 0), DECK + lift - 0.32)
    return m


def f_anschluss(out):
    """Wandanschluss am Gebäude: Rahmen mit dunkler Öffnung. Wand liegt bei y = +0,1, das Band kommt von −Y."""
    m = bm("anschluss_" + ("ausgang" if out else "eingang"), "Gebäude-" + ("Ausgang" if out else "Eingang") + " (Bandanschluss)")
    M = m.body
    H = DECK
    w = BW * 2 + 0.5
    M.box("stahl", (0, 0.05, H + 0.5), (w, 0.7, 0.16))
    M.box("stahl", (0, 0.05, H - 0.36), (w, 0.7, 0.12))
    for sx in (-1, 1):
        M.box("stahl", (sx * (w / 2 - 0.08), 0.05, H + 0.07), (0.16, 0.7, 0.98))
    M.box("russ", (0, 0.14, H + 0.1), (w - 0.3, 0.32, 0.74))
    M.box("akzent", (0, -0.28, H + 0.6), (w, 0.06, 0.05))
    for i in range(4):
        M.box("dunkel", (-w / 2 + 0.3 + i * (w - 0.6) / 3, -0.22, H + 0.3), (0.08, 0.02, 0.4))
    # Pfeil auf dem Sturz: Eingang zeigt hinein (+Y), Ausgang heraus (−Y)
    d = -1 if out else 1
    arrow = [(-0.12, -0.1 * d), (0.12, -0.1 * d), (0.12, 0.02 * d), (0.22, 0.02 * d), (0.0, 0.2 * d), (-0.22, 0.02 * d), (-0.12, 0.02 * d)]
    M.extrude("akzent" if out else "mint", (0, -0.05, H + 0.58), arrow, 0.015, "XY")
    M.sphere("glut" if out else "mint", (w / 2 - 0.08, -0.29, H + 0.55), 0.05, 8, 4)
    belt_on_path(M, [(0.0, -0.9, DECK), (0.0, -0.45, DECK), (0.0, 0.1, DECK)])
    return m


def f_uebergabe():
    m = bm("uebergabe_saeule", "Übergabesäule (Band ohne Wand)")
    M = m.body
    H = DECK
    w = BW * 2 + 0.5
    M.box("stahl", (0, 0.25, (H + 0.35) / 2), (w, 0.7, H + 0.35))
    M.box("russ", (0, -0.11, H + 0.1), (w - 0.3, 0.04, 0.5))
    M.box("akzent", (0, 0.25, H + 0.38), (w + 0.04, 0.74, 0.06))
    vents(M, (0, -0.11, 0.3), w - 0.4, 0.3, 3)
    belt_on_path(M, [(0.0, -0.9, DECK), (0.0, -0.1, DECK)])
    return m


def f_zulauf(L=2.0):
    m = bm("zulauf_tunnel", "Zulauf-Tunnel (Band unter Haube)")
    M = m.body
    path = _line(L)
    belt_on_path(M, path)
    hood = [(-BW - 0.25, -0.42), (-BW - 0.15, -0.42), (-BW - 0.15, 0.48), (BW + 0.15, 0.48), (BW + 0.15, -0.42), (BW + 0.25, -0.42), (BW + 0.25, 0.58), (-BW - 0.25, 0.58)]
    M.sweep("hell", path, hood)
    M.sweep("akzent", path, [(-BW - 0.27, 0.5), (BW + 0.27, 0.5), (BW + 0.27, 0.62), (-BW - 0.27, 0.62)])
    for y in (0.3, L - 0.3):
        M.sphere("lampe", (BW + 0.27, y, DECK + 0.3), 0.04, 6, 3)
    return m


# ================================ Produkte ==================================
# Alle Waren aus gamedata.json (Schlüssel, Name, Form, Farbe). Größe wie auf
# dem Band im Spiel; die Ware liegt mit der Unterseite auf z = 0, längs zu Y.
PRODUCTS = [
    ("staub", "Staub", "crate", "#a49c8e"), ("metall", "Metallschrott", "crate", "#9aa3ab"), ("kristall", "Kristallsplitter", "crate", "#b98cff"),
    ("chemie", "Chemieschlamm", "crate", "#9bd35a"), ("staubBrick", "Staub-Brick", "brick", "#cfc3ad"), ("metallBrick", "Metall-Brick", "brick", "#7d8790"),
    ("barren", "Metallbarren", "ingot", "#eef2f6"), ("reinkristall", "Reinkristall", "gem", "#e2d4ff"), ("polymer", "Polymer", "barrel", "#6ff2d2"),
    ("isotop", "Strahlenerz", "crate", "#8aff5a"), ("marsstahl", "Marsstahl", "ingot", "#c8704a"), ("hitzekachel", "Hitzeschild-Kachel", "brick", "#e8d0b0"),
    ("treibstoff", "Raketentreibstoff", "barrel", "#ff9a4a"), ("eisblock", "Eisblock", "brick", "#cfe8ff"), ("kryolinse", "Kryolinse", "gem", "#9fe0ff"),
    ("bitumen", "Bitumenblock", "brick", "#5a4a30"), ("kohlefaser", "Kohlefaser", "ingot", "#3a3a3a"), ("methanol", "Methanol", "barrel", "#ffcf6a"),
    ("lavaziegel", "Lavaziegel", "brick", "#6a3a2a"), ("schwefelstahl", "Schwefelstahl", "ingot", "#e0d060"), ("schwefelsaeure", "Schwefelsäure", "barrel", "#f2e23c"),
    ("titanleg", "Titanlegierung", "ingot", "#d8d0c0"), ("basaltfaser", "Basaltfaser", "brick", "#7a6050"), ("hitzelack", "Hitzelack", "barrel", "#ffb070"),
    ("magnetrohling", "Magnetrohling", "brick", "#6a7a9a"), ("supraleiter", "Supraleiter", "ingot", "#9fc4ff"), ("magnetkristall", "Magnetkristall", "gem", "#8fb0ff"),
    ("salzblock", "Salzblock", "brick", "#f0ece0"), ("elektrolyt", "Elektrolyt", "barrel", "#c0f0ff"), ("reinwasser", "Reinwasser", "barrel", "#bfe8ff"),
    ("stickstoffblock", "Stickstoffblock", "brick", "#f0d8e0"), ("kryostahl", "Kryostahl", "ingot", "#c0d0e8"), ("kuehlmittel", "Kühlmittel", "barrel", "#7fe0c8"),
    ("tholinziegel", "Tholinziegel", "brick", "#9a6a4a"), ("tholinharz", "Tholinharz", "barrel", "#c07a40"), ("methankristall", "Methankristall", "gem", "#f0e8e0"),
    ("eiskern", "Eiskern", "brick", "#e0e8ff"), ("frostglas", "Frostglas", "gem", "#dff4ff"), ("rotharz", "Rotharz-Block", "brick", "#a04030"),
    ("sednit", "Sednit-Verbund", "ingot", "#d86a4a"), ("pellet", "Isotopenpellet", "brick", "#9aff5a"), ("brennelement", "Brennelement", "ingot", "#c8ff9a"),
    ("brennstab", "Brennstab", "barrel", "#7aff7a"), ("ozeaneis", "Ozeaneis-Block", "brick", "#bfe4f4"), ("biokristall", "Biokristall", "gem", "#7affc8"),
    ("algenoel", "Algenöl", "barrel", "#4ad07a"), ("torfblock", "Torfblock", "brick", "#4a3a24"), ("graphen", "Graphen", "ingot", "#2a2e34"),
    ("biosprit", "Biosprit", "barrel", "#c8d04a"), ("rohdiamant", "Rohdiamant-Block", "brick", "#e8e8f0"), ("diamant", "Industriediamant", "gem", "#ffffff"),
    ("graphit", "Graphitplatte", "brick", "#3a3634"), ("druckeis", "Druckeis-Block", "brick", "#a8d0ee"), ("superionik", "Superionisches Eis", "gem", "#7ad8ff"),
    ("kondensat", "Druckkondensat", "barrel", "#a8e8f0"), ("reaktorkern", "Reaktorkern", "ingot", "#7dff6a"), ("strahlstahl", "Strahlenschutzstahl", "ingot", "#9aa89a"),
]
_GLOW = {"isotop", "pellet", "brennelement", "brennstab", "reaktorkern", "polymer"}
_ICE = {"eisblock", "eiskern", "ozeaneis", "druckeis", "stickstoffblock", "frostglas", "kryolinse", "superionik"}


def _pmat(key, shape, col):
    name = "p_" + key
    if name not in MATS:
        if shape == "gem":
            MATS[name] = (col, 0.1, 0.08, col, 1.2)
        elif shape == "ingot":
            MATS[name] = (col, 0.9, 0.3, col if key in _GLOW else None, 1.5 if key in _GLOW else 0)
        elif key in _ICE:
            MATS[name] = (col, 0.05, 0.12, "#9fdcff", 0.2)
        elif key in _GLOW:
            MATS[name] = (col, 0.2, 0.4, col, 1.5)
        else:
            MATS[name] = (col, 0.25 if shape == "barrel" else 0.05, 0.45 if shape == "barrel" else 0.75, None, 0)
    return name


def product(key, name, shape, col):
    m = Model("ware_" + key, name, "Produkte"); M = m.body
    pm = _pmat(key, shape, col)
    if shape == "brick":
        prof = [(-0.15, 0), (0.15, 0), (0.15, 0.17), (0.12, 0.2), (-0.12, 0.2), (-0.15, 0.17)]
        M.extrude(pm, (0, 0, 0), prof, 0.5, "XZ")
        M.box("dunkel", (0, 0, 0.2), (0.14, 0.3, 0.012))
        for y in (-0.18, 0.18):
            M.box(pm, (0, y, 0.205), (0.2, 0.04, 0.012))
    elif shape == "ingot":
        prof = [(-0.11, 0), (0.11, 0), (0.085, 0.16), (-0.085, 0.16)]
        M.extrude(pm, (0, 0, 0), prof, 0.55, "XZ")
        M.box("dunkel", (0, 0.12, 0.16), (0.08, 0.12, 0.01))
        M.box("akzent", (0, -0.12, 0.16), (0.08, 0.06, 0.01))
    elif shape == "gem":
        M.lathe(pm, (0, 0, 0.02), [(0.0, 0.0), (0.085, 0.06), (0.085, 0.2), (0.0, 0.3)], 6)
        M.lathe(pm, (0.07, 0.04, 0.0), [(0.0, 0.0), (0.05, 0.04), (0.05, 0.12), (0.0, 0.19)], 6, rot=(0, 28, 20))
        M.lathe(pm, (-0.06, -0.05, 0.0), [(0.0, 0.0), (0.045, 0.03), (0.045, 0.1), (0.0, 0.16)], 6, rot=(-25, -18, 0))
        M.lathe("felsdkl", (0, 0, 0), [(0.13, 0), (0.1, 0.04), (0, 0.045)], 8)
    elif shape == "barrel":
        M.lathe(pm, (0, 0, 0), [(0.0, 0.0), (0.15, 0.0), (0.16, 0.02), (0.16, 0.38), (0.15, 0.4), (0.0, 0.4)], 18)
        for z in (0.1, 0.3):
            M.torus("stahl", (0, 0, z), 0.162, 0.012, 18, 5)
        M.lathe("weiss", (0, 0, 0.17), [(0.163, 0), (0.163, 0.06)], 18)
        M.cyl("dunkel", (0.07, 0, 0.4), 0.03, 0.02, 8)
    else:  # crate: Rohstoffkiste mit Inhalt
        M.box("stahl", (0, 0, 0.14), (0.42, 0.42, 0.28))
        for sx in (-1, 1):
            M.box(pm, (sx * 0.211, 0, 0.14), (0.01, 0.36, 0.22))
            M.box(pm, (0, sx * 0.211, 0.14), (0.36, 0.01, 0.22))
        M.box("dunkel", (0, 0, 0.27), (0.38, 0.38, 0.02))
        if key in ("staub", "isotop"):
            M.lathe(pm, (0, 0, 0.27), [(0.18, 0), (0.14, 0.05), (0.06, 0.08), (0, 0.085)], 12)
        elif key == "chemie":
            M.cyl(pm, (0, 0, 0.27), 0.17, 0.03, 14)
            for i, (x, y) in enumerate(((0.05, 0.04), (-0.07, -0.02), (0.02, -0.09))):
                M.sphere(pm, (x, y, 0.3), 0.03 + 0.01 * i, 8, 4)
        else:
            for i, (x, y, r) in enumerate(((0.06, 0.05, 0.07), (-0.08, 0.02, 0.06), (0.0, -0.09, 0.065), (0.1, -0.08, 0.05))):
                if key == "kristall":
                    M.lathe(pm, (x, y, 0.27), [(0.0, 0.0), (0.03, 0.02), (0.03, 0.06), (0.0, 0.1)], 6, rot=(10 * i, -12 * i, 0))
                else:
                    M.blob(pm, (x, y, 0.3), r, 17 + i, (1, 0.8, 0.55), 0.45, 8, 4)
    return m


# ============================ Schmutz auf Parzellen ==========================
# Neun Schmutzarten (deb in gamedata.json): rock, scrap, sat, crystal, barrel,
# ice, goo, sulfur, lava – je zwei bis drei Varianten, ca. 0,4–1,2 m groß.
def sm(key, name):
    return Model("schmutz_" + key, name, "Schmutz")


def d_rock(v):
    m = sm("fels_%d" % v, "Felsbrocken %d" % v); M = m.body
    import random
    r = random.Random(100 + v)
    for i in range(2 + v):
        a = r.random() * 6.28
        d = 0.0 if i == 0 else 0.25 + r.random() * 0.3
        s = (0.45 if i == 0 else 0.18 + r.random() * 0.15) * (1 + 0.2 * v)
        M.blob("fels" if i % 2 == 0 else "felsdkl", (d * math.cos(a), d * math.sin(a), s * 0.35), s, 7 * v + i, (1, 0.85, 0.65), 0.35, 10, 6)
    return m


def d_scrap(v):
    m = sm("schrott_%d" % v, "Metallschrott %d" % v); M = m.body
    if v == 1:  # Doppel-T-Träger + verbogenes Blech
        prof = [(-0.08, 0), (0.08, 0), (0.08, 0.02), (0.015, 0.02), (0.015, 0.14), (0.08, 0.14), (0.08, 0.16), (-0.08, 0.16), (-0.08, 0.14), (-0.015, 0.14), (-0.015, 0.02), (-0.08, 0.02)]
        M.extrude("rost", (0, 0, 0.02), prof, 1.1, "XZ", rot=(0, 0, 25))
        M.box("stahl", (0.2, 0.25, 0.1), (0.5, 0.35, 0.02), rot=(18, -12, 40))
        M.box("rost", (-0.25, -0.2, 0.06), (0.35, 0.3, 0.02), rot=(-8, 22, -30))
    elif v == 2:  # Zahnrad + Rohrstück
        M.cyl("rost", (0, 0, 0.0), 0.22, 0.06, 20)
        for k in range(12):
            a = k * 30
            M.box("rost", _add((0, 0, 0.03), _mv(_rot(0, 0, a), (0.24, 0, 0))), (0.07, 0.06, 0.06), rot=(0, 0, a))
        M.cyl("dunkel", (0, 0, 0.06), 0.06, 0.02, 10)
        M.lathe("stahl", (0.35, 0.1, 0.09), [(0.09, -0.3), (0.09, 0.3), (0.07, 0.3), (0.07, -0.3)], 12, rot=(90, 0, 35), closed_loop=True)
    else:  # Maschinengehäuse, halb im Staub
        M.box("rost", (0, 0, 0.15), (0.6, 0.4, 0.35), rot=(0, 12, 8))
        M.box("dunkel", (0.0, -0.21, 0.2), (0.3, 0.02, 0.15), rot=(0, 12, 8))
        M.pipe("stahl", [(0.3, 0.1, 0.25), (0.55, 0.15, 0.3), (0.7, 0.05, 0.05)], 0.03, 8)
        M.blob("fels", (-0.3, 0.2, 0.02), 0.25, 41, (1.4, 1, 0.3), 0.3, 10, 5)
    return m


def d_sat(v):
    m = sm("satellit_%d" % v, "Satellitentrümmer %d" % v); M = m.body
    if v == 1:
        M.box("hell", (0, 0, 0.18), (0.45, 0.45, 0.4), rot=(15, 20, 30))
        M.box("kupfer", (0, 0, 0.18), (0.47, 0.3, 0.3), rot=(15, 20, 30))
        M.box("solar", (0.55, 0.2, 0.05), (0.7, 0.35, 0.02), rot=(5, -10, 30))
        M.box("dunkel", (0.55, 0.2, 0.04), (0.72, 0.37, 0.015), rot=(5, -10, 30))
        M.tube("stahl", (-0.1, 0.1, 0.35), (-0.4, 0.3, 0.7), 0.015, 5)
    else:
        dish(M, (0, 0, 0.08), 0.4, 0.14, "weiss", 70, 20)
        M.box("solar", (-0.4, -0.3, 0.04), (0.5, 0.3, 0.02), rot=(0, 8, -15))
        M.box("solar", (0.35, -0.45, 0.03), (0.3, 0.25, 0.02), rot=(-6, 0, 40))
    return m


def d_crystal(v):
    m = sm("kristall_%d" % v, "Kristallablagerung %d" % v); M = m.body
    import random
    r = random.Random(300 + v)
    M.blob("felsdkl", (0, 0, 0.05), 0.4, 300 + v, (1.2, 1, 0.3), 0.3, 10, 5)
    for i in range(5 + 2 * v):
        a, d = r.random() * 6.28, r.random() * 0.3
        h = 0.25 + r.random() * 0.4
        M.lathe("kristall", (d * math.cos(a), d * math.sin(a), 0.05), [(0, 0), (0.05 + h * 0.1, h * 0.15), (0.05 + h * 0.1, h * 0.75), (0, h)], 6,
                rot=((r.random() - 0.5) * 50, (r.random() - 0.5) * 50, r.random() * 60))
    return m


def d_barrel(v):
    m = sm("fass_%d" % v, "Altfässer %d" % v); M = m.body
    cols = ["rost", "akzent", "rot"]
    for i in range(v + 1):
        c = cols[i % 3]
        if i == 0:
            M.lathe(c, (0.15, 0, 0.18), [(0, -0.3), (0.18, -0.3), (0.18, 0.3), (0, 0.3)], 16, rot=(90, 0, 30))
            M.cyl("schleim", (0.5, -0.25, 0), 0.32, 0.02, 16)
        else:
            M.lathe(c, (-0.3 * i, 0.25 * i, 0), [(0, 0), (0.18, 0), (0.18, 0.55), (0, 0.55)], 16)
            M.torus("dunkel", (-0.3 * i, 0.25 * i, 0.15), 0.182, 0.012, 16, 4)
            M.torus("dunkel", (-0.3 * i, 0.25 * i, 0.4), 0.182, 0.012, 16, 4)
    return m


def d_ice(v):
    m = sm("eis_%d" % v, "Eisablagerung %d" % v); M = m.body
    import random
    r = random.Random(500 + v)
    for i in range(3 + v):
        a, d = r.random() * 6.28, (0 if i == 0 else 0.2 + r.random() * 0.25)
        h = 0.3 + r.random() * 0.4
        M.lathe("kometeis", (d * math.cos(a), d * math.sin(a), 0), [(0, 0), (0.12, 0.08), (0.08, h * 0.8), (0, h)], 5,
                rot=((r.random() - 0.5) * 40, (r.random() - 0.5) * 40, r.random() * 72))
    M.blob("eis", (0, 0, 0.02), 0.45, 500 + v, (1.2, 1, 0.12), 0.3, 10, 4)
    return m


def d_goo(v):
    m = sm("schleim_%d" % v, "Schlamm %d" % v); M = m.body
    import random
    r = random.Random(700 + v)
    M.blob("schleim", (0, 0, 0.0), 0.5, 700 + v, (1.3, 1, 0.12), 0.3, 12, 4)
    for i in range(4 + v):
        a, d = r.random() * 6.28, r.random() * 0.4
        M.sphere("schleim", (d * math.cos(a), d * math.sin(a), 0.04), 0.04 + r.random() * 0.06, 10, 5, zcut=0)
    return m


def d_sulfur(v):
    m = sm("schwefel_%d" % v, "Schwefelkruste %d" % v); M = m.body
    import random
    r = random.Random(900 + v)
    M.blob("fels", (0, 0, 0.0), 0.45, 900 + v, (1.2, 1, 0.15), 0.3, 10, 4)
    for i in range(6 + 2 * v):
        a, d = r.random() * 6.28, r.random() * 0.4
        M.cyl("schwefel", (d * math.cos(a), d * math.sin(a), 0.02), 0.06 + r.random() * 0.06, 0.04 + r.random() * 0.08, 6, rot=(0, 0, r.random() * 60))
    return m


def d_lava(v):
    m = sm("lava_%d" % v, "Lavagestein %d" % v); M = m.body
    import random
    r = random.Random(1100 + v)
    for i in range(2 + v):
        a, d = r.random() * 6.28, (0 if i == 0 else 0.3 + r.random() * 0.2)
        s = 0.4 if i == 0 else 0.22
        p = (d * math.cos(a), d * math.sin(a), s * 0.3)
        sc = (1, 0.9, 0.6)
        M.blob("lavastein", p, s, 1100 + 10 * v + i, sc, 0.45, 12, 7)
        M.blob("glut", p, s * 0.955, 1500 + 10 * v + i, sc, 0.12, 12, 7, facet=0.04)  # Glut scheint in den Senken durch
    return m


# =============================== Ereignisse =================================
def e_wrack():
    m = Model("wrack_raumschiff", "Raumschiff-Wrack (Absturz)", "Ereignisse"); M = m.body
    M.cyl("russ", (0, 0, 0), 3.2, 0.02, 32)  # Brandfleck
    # Rumpf, schräg eingegraben
    hull = [(0.0, -2.4), (0.45, -2.2), (0.85, -1.6), (0.95, -0.6), (0.95, 1.2), (0.8, 1.7), (0.86, 1.9), (0.7, 2.0), (0.82, 2.2), (0.5, 2.3), (0.0, 2.3)]
    M.lathe("hell", (0, 0, 0.55), [(r, z) for r, z in hull], 18, rot=(78, 0, 25))
    R = _rot(78, 0, 25)
    for z in (-1.0, 0.0, 1.0):
        M.lathe("dunkel", _add((0, 0, 0.55), _mv(R, (0, 0, z))), [(0.98, -0.05), (0.98, 0.05)], 18, R=R)
    M.box("glas", _add((0, 0, 0.55), _mv(R, (0, -0.8, -1.7))), (0.6, 0.05, 0.35), R=_mm(R, _rot(-25, 0, 0)))
    # Abgerissener Flügel, Triebwerk, Platten
    M.extrude("hell", (1.9, 1.2, 0.08), [(0, 0), (1.6, 0.3), (1.4, 0.7), (0.2, 0.6)], 0.08, "XY", rot=(6, -10, 10))
    M.extrude("rot", (1.95, 1.25, 0.17), [(0.1, 0.2), (1.4, 0.4), (1.3, 0.5), (0.15, 0.4)], 0.02, "XY", rot=(6, -10, 10))
    M.lathe("dunkel", (-1.9, -1.3, 0.45), [(0.25, 0), (0.45, 0.6), (0.4, 0.62), (0.2, 0.05)], 16, rot=(100, 0, -60), closed_loop=True)
    M.cyl("glut", (-1.9, -1.3, 0.45), 0.22, 0.04, 16, rot=(100, 0, -60))
    for i, (x, y, a) in enumerate(((1.0, -1.8, 20), (-1.4, 1.2, -35), (2.6, -0.4, 60), (-2.4, 0.4, 10))):
        M.box("hell" if i % 2 else "dunkel", (x, y, 0.05), (0.6, 0.4, 0.03), rot=(8 * i, -6, a))
    for i in range(6):  # Glutnester
        a = i * 1.1
        M.sphere("glut", (1.8 * math.cos(a), 1.8 * math.sin(a), 0.03), 0.06, 6, 3)
    beacon(M, (0.4, -0.9, 0.0), 0.9, "warn")
    return m


def e_satwrack():
    m = Model("wrack_satellit", "Satelliten-Wrack", "Ereignisse"); M = m.body
    M.cyl("russ", (0, 0, 0), 2.4, 0.02, 28)
    R = _rot(12, 6, 30)
    M.box("weiss", (0, 0, 1.0), (1.2, 1.2, 1.4), R=R)
    M.box("kupfer", (0, 0, 1.0), (1.24, 0.9, 1.0), R=R)
    M.box("dunkel", _add((0, 0, 1.0), _mv(R, (0, 0, 1.0))), (0.8, 0.8, 0.12), R=R)
    dish(M, _add((0, 0, 1.0), _mv(R, (0, 0, 0.8))), 0.6, 0.2, "weiss", 25, 30)
    for side in (-1, 1):  # Solarflügel, einer abgeknickt
        Rw = _mm(R, _rot(0, 0 if side > 0 else -18, 0))
        base = _add((0, 0, 1.0), _mv(R, (side * 0.62, 0, 0)))
        M.tube("stahl", base, _add(base, _mv(Rw, (side * 0.5, 0, 0))), 0.03, 6)
        for k in range(3):
            c = _add(base, _mv(Rw, (side * (0.95 + k * 0.72), 0, 0)))
            M.box("dunkel", c, (0.7, 0.95, 0.04), R=Rw)
            M.box("solar", _add(c, _mv(Rw, (0, 0, 0.025))), (0.64, 0.88, 0.01), R=Rw)
    M.tube("stahl", (0.3, 0.3, 1.5), (0.9, 1.1, 2.2), 0.02, 5)
    M.sphere("warn", (0.9, 1.1, 2.22), 0.05, 6, 3)
    return m


def e_frachter():
    m = Model("frachter", "Frachter (in Not)", "Ereignisse"); M = m.body
    M.lathe("weiss", (0, 0, 1.2), [(0, -3.0), (0.6, -2.7), (0.9, -1.8), (0.9, 2.2), (0.7, 2.6), (0, 2.7)], 20, rot=(90, 0, 0))
    for y in (-1.4, -0.3, 0.8, 1.9):
        M.box("akzent", (0, y, 1.2), (2.2, 0.8, 0.9))
        M.box("dunkel", (0, y, 1.2), (2.24, 0.06, 0.94))
    M.box("glas", (0, -2.7, 1.4), (0.6, 0.05, 0.25), rot=(-30, 0, 0))
    for sx in (-1, 1):
        M.lathe("dunkel", (sx * 0.6, 2.7, 1.2), [(0.25, 0), (0.4, 0.5), (0.35, 0.52), (0.2, 0.05)], 14, rot=(-90, 0, 0), closed_loop=True)
        M.cyl("glut", (sx * 0.6, 2.75, 1.2), 0.22, 0.04, 14, rot=(-90, 0, 0))
        M.extrude("hell", (sx * 0.9, 1.0, 1.2), [(0, -0.8), (0.9 * sx, -0.2), (0.9 * sx, 0.6), (0, 0.9)], 0.06, "XY")
    M.sphere("warn", (0, 0, 2.15), 0.08, 8, 4)
    return m


def e_komet():
    m = Model("komet", "Komet (Kern)", "Ereignisse"); M = m.body
    M.blob("kometeis", (0, 0, 1.4), 1.3, 2024, (1.25, 1, 0.9), 0.6, 16, 10, facet=0.15)
    import random
    r = random.Random(77)
    for i in range(9):
        a, b = r.random() * 6.28, (r.random() - 0.3) * 1.2
        p = (1.65 * math.cos(a) * math.cos(b), 1.35 * math.sin(a) * math.cos(b), 1.4 + 1.2 * math.sin(b))
        M.blob("felsdkl" if i % 2 else "lavastein", p, 0.3 + r.random() * 0.25, 2100 + i, (1, 1, 0.8), 0.4, 8, 5)
    for i in range(6):
        a = r.random() * 6.28
        M.lathe("kometeis", (1.1 * math.cos(a), 1.0 * math.sin(a), 1.6), [(0, 0), (0.1, 0.1), (0, 0.6)], 5, rot=(r.random() * 60 - 30, r.random() * 60 - 30, 0))
    return m


def e_einschlag():
    m = Model("kometeneinschlag", "Kometeneinschlag (Krater)", "Ereignisse"); M = m.body
    rim = [(2.6, 0.0), (2.3, 0.35), (2.0, 0.45), (1.6, 0.2), (1.0, -0.15), (0.0, -0.2)]
    M.lathe("felsdkl", (0, 0, 0.2), [(2.6, -0.2)] + rim, 32)
    M.lathe("russ", (0, 0, 0.2), [(1.65, 0.14), (1.2, -0.02), (0.0, -0.08), (0.0, -0.1), (1.2, -0.04), (1.65, 0.12)], 28, closed_loop=True)
    M.blob("kometeis", (0, 0, 0.35), 0.6, 31, (1, 1, 0.8), 0.4, 12, 8)
    for k in range(6):
        a = k * 60
        M.box("glut", _add((0, 0, 0.3), _mv(_rot(0, 0, a), (0.75, 0, 0))), (0.6, 0.05, 0.03), rot=(0, 0, a))
    import random
    r = random.Random(5)
    for i in range(10):
        a, d = r.random() * 6.28, 2.0 + r.random() * 1.2
        M.blob("fels" if i % 3 else "kometeis", (d * math.cos(a), d * math.sin(a), 0.12), 0.12 + r.random() * 0.18, 400 + i, (1, 1, 0.7), 0.4, 8, 5)
    return m


def e_meteorit():
    m = Model("meteorit", "Meteorit", "Ereignisse"); M = m.body
    M.lathe("felsdkl", (0, 0, 0), [(1.0, 0), (0.85, 0.12), (0.6, 0.05), (0, 0.04)], 20)
    M.blob("lavastein", (0, 0, 0.45), 0.5, 88, (1, 0.9, 0.85), 0.38, 14, 9)
    M.blob("glut", (0, 0, 0.45), 0.44, 91, (1, 0.9, 0.85), 0.12, 14, 9, facet=0.04)
    return m


def e_erzader():
    m = Model("erzader", "Erzader", "Ereignisse"); M = m.body
    M.blob("felsdkl", (0, 0, 0.05), 1.4, 61, (1.3, 0.7, 0.12), 0.25, 14, 5)
    for i, (x, y, a) in enumerate(((-0.9, -0.1, 15), (-0.3, 0.05, -10), (0.3, -0.05, 20), (0.9, 0.1, -5))):
        M.box("kupfer", (x, y, 0.16), (0.7, 0.12, 0.08), rot=(0, 0, a))
        M.box("glut", (x, y, 0.2), (0.5, 0.05, 0.02), rot=(0, 0, a))
    for k in range(3):
        M.lathe("kristall", (0.2 * k - 0.2, 0.25, 0.1), [(0, 0), (0.05, 0.04), (0.05, 0.2), (0, 0.3)], 6, rot=(15 * k - 15, 10, 0))
    return m


# =============================== Weltobjekte ================================
def w_leuchtkristall():
    m = Model("leuchtkristall", "Leuchtkristall (erntbar)", "Welt"); M = m.body
    import random
    r = random.Random(9)
    M.blob("felsdkl", (0, 0, 0.05), 0.55, 9, (1.1, 1, 0.3), 0.3, 10, 5)
    for i in range(7):
        a, d = r.random() * 6.28, (0 if i == 0 else 0.15 + r.random() * 0.25)
        h = 1.3 if i == 0 else 0.4 + r.random() * 0.6
        M.lathe("kristall", (d * math.cos(a), d * math.sin(a), 0.08), [(0, 0), (0.06 + h * 0.08, h * 0.12), (0.06 + h * 0.08, h * 0.78), (0, h)], 6,
                rot=((r.random() - 0.5) * (10 if i == 0 else 60), (r.random() - 0.5) * (10 if i == 0 else 60), r.random() * 60))
    return m


def w_laterne():
    m = Model("laterne", "Laterne (Lichtmast)", "Welt"); M = m.body
    M.cyl("platte", (0, 0, 0), 0.35, 0.12, 16, r2=0.3)
    M.cyl("dunkel", (0, 0, 0.12), 0.08, 3.0, 10, r2=0.06)
    M.box("akzent", (0, 0, 0.6), (0.18, 0.18, 0.5))
    M.tube("dunkel", (0, 0, 3.0), (0, -0.7, 3.25), 0.04, 8)
    M.box("dunkel", (0, -0.8, 3.25), (0.4, 0.5, 0.12))
    M.box("kaltlicht", (0, -0.8, 3.185), (0.32, 0.42, 0.01))
    M.sphere("warn", (0, 0, 3.15), 0.05, 6, 3)
    return m


def w_astronaut():
    m = Model("astronaut", "Astronaut", "Welt"); M = m.body
    for sx in (-1, 1):
        M.cyl("weiss", (sx * 0.11, 0, 0.05), 0.075, 0.75, 10, r2=0.085)
        M.box("dunkel", (sx * 0.11, -0.03, 0.06), (0.14, 0.24, 0.12))
        M.tube("weiss", (sx * 0.24, 0, 1.38), (sx * 0.3, -0.05, 0.95), 0.06, 8)
        M.sphere("dunkel", (sx * 0.3, -0.06, 0.92), 0.06, 8, 4)
    M.lathe("weiss", (0, 0, 0.78), [(0.0, 0.0), (0.2, 0.02), (0.24, 0.3), (0.25, 0.55), (0.2, 0.66), (0.0, 0.68)], 14)
    M.box("akzent", (0, -0.2, 1.15), (0.22, 0.06, 0.14))
    M.box("mint", (0.05, -0.235, 1.17), (0.04, 0.01, 0.03))
    M.box("hell", (0, 0.2, 1.12), (0.36, 0.2, 0.5))
    M.sphere("weiss", (0, 0, 1.6), 0.17, 16, 8)
    M.sphere("visier", (0, -0.05, 1.62), 0.145, 14, 6, zcut=-0.2)
    M.lathe("dunkel", (0, 0, 1.43), [(0.13, 0), (0.13, 0.05)], 14)
    M.tube("stahl", (0.12, 0.25, 1.35), (0.12, 0.28, 1.75), 0.008, 4)
    return m


def w_sammelwagen():
    m = Model("sammelwagen", "Sammelwagen", "Welt"); M = m.body
    M.box("dunkel", (0, 0, 0.35), (0.9, 1.3, 0.12))
    M.lathe("akzent", (0, 0.1, 0.4), [(0.42, 0), (0.5, 0.45), (0.45, 0.45), (0.37, 0.05)], 16, closed_loop=True)
    M.cyl("russ", (0, 0.1, 0.42), 0.38, 0.03, 16)
    M.tube("stahl", (0, -0.65, 0.35), (0, -1.05, 0.35), 0.03, 6)
    M.torus("stahl", (0, -1.1, 0.35), 0.06, 0.015, 10, 4, rot=(0, 90, 0))
    M.sphere("warn", (0.4, 0.62, 0.45), 0.035, 6, 3)
    M.sphere("warn", (-0.4, 0.62, 0.45), 0.035, 6, 3)
    for i, (x, y) in enumerate(((-1, -1), (1, -1), (-1, 1), (1, 1))):
        wheel(m, i, (x * 0.5, y * 0.42, 0.2), 0.2, 0.14, lugs=10)
    return m


def w_aussenlager():
    m = Model("aussenlager", "Außenlager (Pumpstation)", "Welt", 2.4); M = m.body
    h0 = pad(M, 2.4)
    tank_h(M, (-0.6, 0.6, h0 + 0.5), 0.45, 1.3, "weiss", 0, 2)
    tank_h(M, (-0.6, -0.5, h0 + 0.5), 0.45, 1.3, "weiss", 0, 2)
    M.box("hell", (1.0, 0.0, h0 + 0.55), (0.9, 1.2, 1.1))
    M.box("akzent", (1.0, 0.0, h0 + 1.13), (0.95, 1.25, 0.06))
    vents(M, (1.0, -0.61, h0 + 0.6), 0.6, 0.45, 3)
    M.pipe("kupfer", [(0.2, 0.6, h0 + 0.5), (0.55, 0.6, h0 + 0.5), (0.55, 0.0, h0 + 0.5)], 0.07, 8)
    M.pipe("kupfer", [(0.2, -0.5, h0 + 0.5), (0.55, -0.5, h0 + 0.5)], 0.07, 8)
    M.pipe("stahl", [(1.45, 0.3, h0 + 0.3), (2.1, 0.3, h0 + 0.3), (2.1, 0.3, 0.05)], 0.09, 10)
    beacon(M, (1.6, 1.2, h0), 1.8, "warn")
    M.box("lampe", (1.6, 1.15, h0 + 1.55), (0.25, 0.04, 0.12))
    return m


def w_bauplatz():
    m = Model("bauplatz", "Bauplatz", "Welt", 2.5); M = m.body
    M.cyl("platte", (0, 0, 0), 2.5, 0.06, 32)
    for k in range(12):
        a = 2 * math.pi * k / 12
        M.box("akzent", (2.25 * math.cos(a), 2.25 * math.sin(a), 0.07), (0.18, 0.9, 0.03), rot=(0, 0, math.degrees(a)))
    for k in range(8):  # Absperrpfosten mit Band
        a = 2 * math.pi * (k + 0.5) / 8
        M.cyl("akzent", (2.4 * math.cos(a), 2.4 * math.sin(a), 0.06), 0.04, 0.8, 6)
    ring = [(2.4 * math.cos(2 * math.pi * (k + 0.5) / 8), 2.4 * math.sin(2 * math.pi * (k + 0.5) / 8), 0.75) for k in range(9)]
    M.pipe("rot", ring, 0.015, 4, joints=False)
    for i, (x, y) in enumerate(((-0.8, 0.6), (-0.4, 0.6), (-0.6, 0.6))):  # Trägerstapel
        M.box("stahl", (x, y, 0.12 + 0.0 * i), (0.12, 1.4, 0.12))
    M.box("stahl", (-0.6, 0.6, 0.24), (0.55, 1.4, 0.08))
    for i in range(3):
        crate(M, (0.8, -0.6 + 0.5 * i, 0.26), 0.4, ["kiste1", "kiste2", "kiste3"][i])
    lattice(M, (1.3, 1.1, 0.06), (1.3, 1.1, 3.0), 0.4, 0.3, 5, "akzent", 0.03)  # Baukran
    M.box("akzent", (0.6, 1.1, 3.05), (2.4, 0.18, 0.18))
    M.box("dunkel", (2.0, 1.1, 2.9), (0.4, 0.3, 0.3))
    M.tube("stahl", (0.0, 1.1, 2.95), (0.0, 1.1, 1.3), 0.012, 4)
    M.box("stahl", (0.0, 1.1, 1.25), (0.15, 0.15, 0.1))
    for p in ((-1.5, -1.2), (1.6, -1.3)):  # Leitkegel
        M.lathe("akzent", (p[0], p[1], 0.06), [(0.12, 0), (0.03, 0.35), (0, 0.36)], 10)
    return m


def w_grube():
    m = Model("grube", "Grube (am Andockplatz)", "Welt"); M = m.body
    M.lathe("felsdkl", (0, 0, 0), [(1.2, 0.0), (1.0, 0.18), (0.8, 0.12), (0.7, -0.3), (0.0, -0.35), (0.0, -0.45), (1.2, -0.1)], 24, closed_loop=True)
    M.lathe("russ", (0, 0, 0), [(0.69, -0.27), (0.0, -0.31), (0.0, -0.33), (0.7, -0.29)], 20, closed_loop=True)
    for k in range(10):
        a = 2 * math.pi * k / 10
        M.box("akzent" if k % 2 else "dunkel", (1.0 * math.cos(a), 1.0 * math.sin(a), 0.2), (0.12, 0.6, 0.04), rot=(0, 0, math.degrees(a)))
    ladder(M, (0, -0.75, -0.3), 0.6, (0, -1, 0), 0.3)
    return m


def w_pipeline():
    m = Model("pipeline_trasse", "Pipeline-Trasse (4 m)", "Welt"); M = m.body
    for y in (0.3, 2.0, 3.7):
        for sx in (-1, 1):
            M.box("dunkel", (sx * 0.4, y, 0.4), (0.1, 0.1, 0.8))
        M.box("stahl", (0, y, 0.82), (1.0, 0.14, 0.08))
        M.box("platte", (0, y, 0.02), (1.1, 0.3, 0.04))
    for x, r, c in ((-0.22, 0.14, "kupfer"), (0.22, 0.1, "stahl")):
        M.cyl(c, (x, 0.0, 0.86 + r), r, 4.0, 14, rot=(-90, 0, 0))
        for y in (0.3, 2.0, 3.7):
            M.torus("dunkel", (x, y, 0.86 + r), r + 0.01, 0.015, 14, 4, rot=(90, 0, 0))
    return m


def w_ampel():
    m = Model("ampel", "Ampel (Dachlampe)", "Welt"); M = m.body
    M.box("dunkel", (0, 0, 0.12), (0.9, 0.9, 0.24))
    M.box("akzent", (0, 0, 0.245), (0.92, 0.92, 0.03))
    L = m.part("Licht", (0, 0, 0.52))
    L.sphere("mint", (0, 0, 0.52), 0.375, 16, 8)
    return m


def w_fels(v):
    m = Model("deko_fels_%d" % v, "Deko-Felsen %d" % v, "Welt"); M = m.body
    import random
    r = random.Random(1300 + v)
    for i in range(1 + v):
        a, d = r.random() * 6.28, (0 if i == 0 else 0.8 + r.random() * 0.6)
        s = (1.0 if i == 0 else 0.4 + r.random() * 0.3) * (0.7 + 0.3 * v)
        M.blob("fels" if i % 2 == 0 else "felsdkl", (d * math.cos(a), d * math.sin(a), s * 0.3), s, 1300 + 10 * v + i, (1.1, 0.9, 0.7), 0.4, 12, 8)
    return m


def w_krater():
    m = Model("deko_krater", "Deko-Krater", "Welt"); M = m.body
    M.lathe("felsdkl", (0, 0, 0), [(2.2, 0.0), (1.9, 0.3), (1.6, 0.35), (1.3, 0.05), (0.7, -0.25), (0.0, -0.3), (0.0, -0.4), (2.2, -0.05)], 32, closed_loop=True)
    return m


# ============================== Verzeichnis =================================
# ======================= Build 34: Abwehr und Natur ==========================
def a_schild():
    """Meteorschild-Generator: Säule mit leuchtender Feldkugel, zwei Ringe (der obere dreht sich), vier Stützen."""
    m = Model("schild", "Meteorschild-Generator", "Abwehr", 2.3); M = m.body
    h0 = pad(M, 2.3)
    M.lathe("dunkel", (0, 0, h0), [(1.5, 0), (1.45, 0.3), (1.1, 0.42), (0, 0.42)], 8)
    for k in range(4):
        a = math.radians(45 + 90 * k)
        b = (1.25 * math.cos(a), 1.25 * math.sin(a), h0 + 0.4)
        t = (0.42 * math.cos(a), 0.42 * math.sin(a), h0 + 2.6)
        M.tube("stahl", b, t, 0.09, 8)
        M.box("akzent", b, (0.36, 0.36, 0.22))
    M.cyl("hell", (0, 0, h0 + 0.42), 0.45, 2.5, 16, r2=0.32)
    M.lathe("mint", (0, 0, h0 + 1.0), [(0.47, 0), (0.47, 0.08)], 16)
    M.lathe("mint", (0, 0, h0 + 1.9), [(0.4, 0), (0.4, 0.08)], 16)
    M.torus("akzent", (0, 0, h0 + 2.75), 0.95, 0.07, 28, 8)
    M.cyl("stahl", (0, 0, h0 + 2.9), 0.28, 0.25, 12)
    M.sphere("schildfeld", (0, 0, h0 + 3.55), 0.62, 20, 10)
    for k in range(6):
        a = math.radians(60 * k)
        M.box("dunkel", (0.7 * math.cos(a), 0.7 * math.sin(a), h0 + 3.0), (0.12, 0.12, 0.35))
    D = m.part("Dreh", (0, 0, h0 + 3.55))
    D.torus("stahl", (0, 0, h0 + 3.55), 1.0, 0.05, 32, 6)
    for k in range(3):
        a = math.radians(120 * k)
        D.sphere("schildfeld", (1.0 * math.cos(a), 1.0 * math.sin(a), h0 + 3.55), 0.12, 10, 5)
    beacon(M, (1.6, -1.2, h0), 0.9)
    if DETAIL:  # Kabel, Steuerkästen, Leiter, Warnstreifen
        for k in range(4):
            a = math.radians(90 * k)
            M.pipe("dunkel", [(1.4 * math.cos(a), 1.4 * math.sin(a), h0 + 0.05), (0.9 * math.cos(a), 0.9 * math.sin(a), h0 + 0.5), (0.47 * math.cos(a), 0.47 * math.sin(a), h0 + 0.9)], 0.035, 8)
            M.box("hell", (1.75 * math.cos(a + 0.4), 1.75 * math.sin(a + 0.4), h0 + 0.3), (0.35, 0.35, 0.6), rot=(0, 0, math.degrees(a + 0.4)))
            M.box("mint", (1.75 * math.cos(a + 0.4) * 0.9, 1.75 * math.sin(a + 0.4) * 0.9, h0 + 0.45), (0.12, 0.02, 0.06), rot=(0, 0, math.degrees(a + 0.4) + 90))
        ladder(M, (0, -0.42, h0 + 0.42), 2.3)
        for k in range(24):
            a = 2 * math.pi * k / 24
            M.box("warn" if k % 2 else "dunkel", (1.48 * math.cos(a), 1.48 * math.sin(a), h0 + 0.02), (0.18, 0.12, 0.02), rot=(0, 0, math.degrees(a)))
    return m


def a_flak():
    """Abwehrraketen-Batterie: Sockel mit drehbarem Werfer (2 × 3 Rohre, Raketenspitzen sichtbar) und kleinem Radar."""
    m = Model("flak", "Abwehrraketen-Batterie", "Abwehr", 2.3); M = m.body
    h0 = pad(M, 2.3)
    M.lathe("dunkel", (0, 0, h0), [(1.35, 0), (1.3, 0.35), (0.9, 0.55), (0, 0.55)], 8)
    M.cyl("stahl", (0, 0, h0 + 0.55), 0.55, 0.35, 20)
    for sx in (-1, 1):
        M.box("akzent", (sx * 1.0, 0.9, h0 + 0.3), (0.5, 0.5, 0.4))
        stripes(M, (sx * 1.0, 0.64, h0 + 0.3), 0.45, axis="x")
    # Radar
    M.cyl("stahl", (-1.3, -1.1, h0), 0.05, 1.5, 8)
    dish(M, (-1.3, -1.1, h0 + 1.55), 0.35, 0.12, tilt=30, yaw=200)
    z = h0 + 0.9
    D = m.part("Dreh", (0, 0, z))
    D.cyl("hell", (0, 0, z), 0.62, 0.3, 20)
    for sx in (-1, 1):
        D.box("dunkel", (sx * 0.55, 0, z + 0.65), (0.14, 0.5, 1.0))
    R = _rot(-38, 0, 0)
    D.box("weiss", (0, 0.05, z + 1.0), (0.95, 1.9, 0.62), R=R)
    for i in range(3):
        for j in range(2):
            x = (i - 1) * 0.27; zz = (j - 0.5) * 0.27
            mouth = _add((0, 0, z + 1.0), _mv(R, (x, -0.98, zz)))
            D.cyl("dunkel", mouth, 0.11, 0.06, 10, R=_mv_mat(R, _rot(90, 0, 0)))
            tip = _add((0, 0, z + 1.0), _mv(R, (x, -1.0, zz)))
            D.lathe("warn", tip, [(0.085, 0), (0.07, 0.12), (0, 0.24)], 10, R=_mv_mat(R, _rot(90, 0, 0)))
    D.box("akzent", (0, 0.05, z + 1.0), (0.97, 0.3, 0.64), R=R)
    if DETAIL:  # Hydraulik, Munitionskisten, Kabel
        for sx in (-1, 1):
            D.tube("stahl", (sx * 0.5, 0.3, z + 0.3), (sx * 0.4, -0.2, z + 1.05), 0.05, 8)
            D.tube("dunkel", (sx * 0.5, 0.3, z + 0.3), (sx * 0.45, 0.05, z + 0.65), 0.07, 8)
        for i in range(3):
            M.box("kiste3", (1.3, -0.9 + i * 0.45, h0 + 0.2), (0.5, 0.4, 0.38))
            M.box("warn", (1.3, -0.9 + i * 0.45, h0 + 0.4), (0.3, 0.02, 0.06))
        M.pipe("dunkel", [(-1.3, -1.1, h0 + 0.05), (-0.6, -0.6, h0 + 0.05), (-0.45, -0.3, h0 + 0.5)], 0.04, 8)
    return m


def _mv_mat(A, B):
    return _mm(A, B)


def _leaf_crown(M, mats, seed, cx, cy, cz, r, n, spread):
    import random
    rnd = random.Random(seed)
    for i in range(n):
        a = rnd.random() * 6.283; d = spread * (0.3 + 0.7 * rnd.random()) if i else 0.0
        z = cz + (rnd.random() - 0.35) * r * 0.9
        rr = r * (0.55 + 0.35 * rnd.random()) if i else r * 0.85
        M.blob(mats[i % len(mats)], (cx + d * math.cos(a), cy + d * math.sin(a), z), rr, seed * 31 + i, (1, 1, 0.82), 0.28, 10, 6, facet=0.18)


def n_laub(v):
    """Laubbaum: Stamm mit Wurzelansatz, Äste, Krone aus mehreren weichen Blattballen in zwei Grüntönen. Ca. 5 m hoch."""
    import random
    rnd = random.Random(400 + v)
    m = Model("baum_laub_%d" % v, "Laubbaum %d" % v, "Natur"); M = m.body
    H = 2.2 + 0.3 * v
    M.lathe("rinde", (0, 0, 0), [(0.42, 0), (0.26, 0.25), (0.2, 0.6), (0.16, H * 0.7), (0.12, H)], 9)
    for k in range(3):  # Wurzeln
        a = rnd.random() * 6.283
        M.tube("rinde", (0.1 * math.cos(a), 0.1 * math.sin(a), 0.2), (0.55 * math.cos(a), 0.55 * math.sin(a), -0.05), 0.09, 6, r2=0.03)
    ends = []
    for k in range(3 + v % 2):
        a = k * 6.283 / (3 + v % 2) + rnd.random() * 0.6
        z0 = H * (0.55 + 0.1 * k)
        e = (0.85 * math.cos(a), 0.85 * math.sin(a), z0 + 0.9)
        M.tube("rinde", (0, 0, z0), e, 0.08, 6, r2=0.04)
        ends.append(e)
    mats = ("laub1", "laub2", "laub3") if v != 2 else ("laub2", "laub1")
    _leaf_crown(M, mats, 500 + v, 0, 0, H + 0.9, 1.25, 4, 0.6)
    for i, e in enumerate(ends):
        M.blob(mats[(i + 1) % len(mats)], e, 0.75 + 0.15 * rnd.random(), 600 + v * 10 + i, (1, 1, 0.8), 0.3, 10, 6, facet=0.18)
    if DETAIL:  # feinere Krone: kleine Blattbüschel auf der Oberfläche, Zweige
        for k in range(16):
            a = rnd.random() * 6.283; b = rnd.random() * 1.2 - 0.2
            c = (1.35 * math.cos(a) * math.cos(b), 1.35 * math.sin(a) * math.cos(b), H + 0.9 + 1.0 * math.sin(b))
            M.blob(mats[k % len(mats)], c, 0.32 + 0.12 * rnd.random(), 650 + v * 40 + k, (1, 1, 0.85), 0.35, 8, 5, facet=0.22)
        for e in ends:
            for k in range(2):
                a = rnd.random() * 6.283
                M.tube("rinde", e, (e[0] + 0.4 * math.cos(a), e[1] + 0.4 * math.sin(a), e[2] + 0.25), 0.03, 5, r2=0.012)
    return m


def n_tanne(v):
    """Tanne: schlanker Stamm, fünf hängende, gezackte Zweigkränze, oben eine Spitze. Ca. 5–6 m hoch."""
    m = Model("baum_tanne_%d" % v, "Tanne %d" % v, "Natur"); M = m.body
    M.lathe("rinde", (0, 0, 0), [(0.3, 0), (0.17, 0.2), (0.13, 1.2), (0.06, 4.6)], 8)
    tiers = 5 + (v - 1)
    for t in range(tiers):
        k = t / max(1, tiers - 1)
        z = 0.9 + t * (3.9 / tiers); R0 = 1.55 - 1.15 * k; Hh = 1.15 - 0.35 * k
        n = 14; verts = []; faces = []
        for i in range(n):
            a = 6.283 * i / n + (0.12 if t % 2 else 0)
            rr = R0 * (1.0 if i % 2 == 0 else 0.72)
            verts.append((rr * math.cos(a), rr * math.sin(a), z - (0.18 if i % 2 == 0 else 0.0)))
        for i in range(n):
            a = 6.283 * (i + 0.5) / n
            verts.append((R0 * 0.45 * math.cos(a), R0 * 0.45 * math.sin(a), z + Hh * 0.45))
        top = len(verts); verts.append((0, 0, z + Hh)); bot = len(verts); verts.append((0, 0, z + 0.05))
        for i in range(n):
            j = (i + 1) % n
            faces.append((i, j, n + i)); faces.append((j, n + j, n + i)); faces.append((n + i, n + j, top)); faces.append((j, i, bot))
        M.add(verts, faces, "tanne" if t % 2 == 0 else "laub3")
    M.lathe("tanne", (0, 0, 4.6), [(0.18, 0), (0, 0.7)], 8)
    if DETAIL:  # zweite, versetzte Zweiglage je Kranz – dichter und buschiger
        for t in range(tiers):
            k = t / max(1, tiers - 1); z = 1.1 + t * (3.9 / tiers); R0 = (1.55 - 1.15 * k) * 0.82
            n = 18; verts = []; faces = []
            for i in range(n):
                a = 6.283 * (i + 0.5) / n
                rr = R0 * (1.0 if i % 2 == 0 else 0.7)
                verts.append((rr * math.cos(a), rr * math.sin(a), z - (0.22 if i % 2 == 0 else 0.02)))
            top = len(verts); verts.append((0, 0, z + 0.7 - 0.2 * k))
            for i in range(n):
                j = (i + 1) % n; faces.append((i, j, top)); faces.append((j, i, top))
            M.add(verts, faces, "laub3" if t % 2 == 0 else "tanne", closed=False)
    return m


def n_busch(v):
    m = Model("busch_%d" % v, "Busch %d" % v, "Natur"); M = m.body
    _leaf_crown(M, ("laub2", "laub1") if v == 1 else ("laub3", "laub1"), 700 + v, 0, 0, 0.45, 0.55, 4, 0.45)
    if v == 2:
        import random
        rnd = random.Random(77)
        for i in range(6):
            a = rnd.random() * 6.283
            M.sphere("bluete", (0.55 * math.cos(a), 0.55 * math.sin(a), 0.4 + 0.4 * rnd.random()), 0.07, 10, 5)
    if DETAIL:
        import random
        rr = random.Random(780 + v)
        for i in range(8):
            a = rr.random() * 6.283
            M.blob("laub1" if i % 2 else "laub2", (0.6 * math.cos(a), 0.6 * math.sin(a), 0.3 + 0.35 * rr.random()), 0.18 + 0.1 * rr.random(), 790 + v * 20 + i, (1, 1, 0.85), 0.35, 8, 5, facet=0.2)
    return m


def n_gras():
    """Grasbüschel: neun schmale, gebogene Halme."""
    import random
    rnd = random.Random(9)
    m = Model("gras_1", "Grasbüschel", "Natur"); M = m.body
    for i in range(16 if DETAIL else 9):
        a = rnd.random() * 6.283; tilt = 0.25 + 0.35 * rnd.random(); h = 0.45 + 0.35 * rnd.random(); w = 0.035
        d = (math.cos(a), math.sin(a)); sd = (-d[1] * w, d[0] * w)
        base = (d[0] * 0.05, d[1] * 0.05, 0); mid = (d[0] * tilt * 0.4 * h, d[1] * tilt * 0.4 * h, h * 0.55); tip = (d[0] * tilt * h, d[1] * tilt * h, h)
        vs = [(base[0] - sd[0], base[1] - sd[1], 0), (base[0] + sd[0], base[1] + sd[1], 0), (mid[0] + sd[0] * 0.6, mid[1] + sd[1] * 0.6, mid[2]), (mid[0] - sd[0] * 0.6, mid[1] - sd[1] * 0.6, mid[2]), tip]
        M.add(vs, [(0, 1, 2, 3), (3, 2, 4), (3, 2, 1, 0), (2, 3, 4)], "gras" if i % 3 else "laub2", closed=False)
    return m


def e_lieferrakete():
    m = Model("lieferrakete", "Lieferrakete", "Ereignisse"); M = m.body
    M.lathe("weiss", (0, 0, 0.3), [(0.55, 0), (0.6, 0.4), (0.6, 2.0), (0.45, 2.7), (0.15, 3.2), (0, 3.35)], 20)
    M.lathe("akzent", (0, 0, 0.3), [(0.62, 1.2), (0.62, 1.45)], 20)
    M.sphere("glas", (0, -0.55, 2.3), 0.16, 10, 5)
    for k in range(4):
        a = math.radians(45 + 90 * k)
        M.extrude("akzent", (0.62 * math.cos(a), 0.62 * math.sin(a), 0.3), [(0, 0), (0.55, -0.2), (0.55, 0.3), (0, 0.9)], 0.06, plane="XZ", rot=(0, 0, 45 + 90 * k + 90))
    M.lathe("dunkel", (0, 0, 0), [(0.3, 0), (0.42, 0.3)], 14)
    M.cyl("glut", (0, 0, -0.02), 0.26, 0.03, 12)
    return m


# ================= Build 38: alle noch fehlenden Modelle (02.11.2026) =================
def _rnd(seed):
    import random
    return random.Random(seed)


def e_riesenmeteor():
    """Riesenmeteor: großer, kantiger Brocken mit glühenden Rissen und Glut im Inneren. Ca. 6 m."""
    m = Model("riesenmeteor", "Riesenmeteor", "Ereignisse"); M = m.body; r = _rnd(4100)
    M.blob("lavastein", (0, 0, 3.0), 3.0, 4101, (1, 0.9, 0.85), 0.35, 20, 12, facet=0.16)
    M.blob("glut", (0, 0, 3.0), 2.75, 4102, (1, 0.9, 0.85), 0.12, 16, 10, facet=0.05)
    for i in range(14):
        a = r.random() * 6.283; b = (r.random() - 0.5) * 2.4
        p = (2.85 * math.cos(a) * math.cos(b * 0.5), 2.85 * math.sin(a) * math.cos(b * 0.5), 3.0 + 2.6 * math.sin(b * 0.5))
        M.box("glut", p, (0.12, 1.1 + r.random(), 0.08), rot=(r.random() * 90, r.random() * 90, math.degrees(a)))
    for i in range(5):
        a = r.random() * 6.283
        M.blob("felsdkl", (3.1 * math.cos(a), 3.1 * math.sin(a), 3.0 + (r.random() - 0.5) * 3), 0.6 + 0.4 * r.random(), 4110 + i, (1, 1, 0.8), 0.4, 10, 6)
    if DETAIL:  # mehr Risse und kleine Begleitbrocken
        for i in range(18):
            a = r.random() * 6.283; b = (r.random() - 0.5) * 2.8
            p = (2.95 * math.cos(a) * math.cos(b * 0.5), 2.95 * math.sin(a) * math.cos(b * 0.5), 3.0 + 2.7 * math.sin(b * 0.5))
            M.box("glut", p, (0.06, 0.5 + 0.6 * r.random(), 0.05), rot=(r.random() * 90, r.random() * 90, math.degrees(a)))
        for i in range(6):
            a = r.random() * 6.283
            M.blob("lavastein", (4.2 * math.cos(a), 4.2 * math.sin(a), 3.0 + (r.random() - 0.5) * 2), 0.25 + 0.2 * r.random(), 4130 + i, (1, 1, 0.9), 0.4, 8, 5)
    return m


def e_brocken(v):
    """Meteor-Brocken für das Abfangen: Lavastein mit Glutadern. Ca. 1,5 m."""
    m = Model("brocken_%d" % v, "Meteor-Brocken %d" % v, "Ereignisse"); M = m.body; r = _rnd(4200 + v)
    M.blob("lavastein", (0, 0, 0.75), 0.75, 4200 + v, (1.1, 0.9, 0.85) if v != 2 else (0.9, 1.1, 0.8), 0.4, 14, 8, facet=0.2)
    M.blob("glut", (0, 0, 0.75), 0.66, 4210 + v, (1.1, 0.9, 0.85), 0.15, 12, 7, facet=0.05)
    for i in range(4 + v):
        a = r.random() * 6.283
        M.box("glut", (0.72 * math.cos(a), 0.72 * math.sin(a), 0.75 + (r.random() - 0.5) * 0.8), (0.06, 0.4, 0.05), rot=(0, r.random() * 60, math.degrees(a)))
    return m


def a_abwehrrakete():
    """Abwehrrakete (Geschoss): schlanker Körper, rote Spitze, vier Flossen, Glut am Heck. Zeigt nach oben. Ca. 1,6 m."""
    m = Model("abwehrrakete", "Abwehrrakete", "Abwehr"); M = m.body
    M.lathe("weiss", (0, 0, 0.15), [(0.1, 0), (0.12, 0.1), (0.12, 1.05), (0.1, 1.15)], 12)
    M.lathe("warn", (0, 0, 1.3), [(0.1, 0), (0.06, 0.15), (0, 0.3)], 12)
    M.lathe("akzent", (0, 0, 0.75), [(0.125, 0), (0.125, 0.08)], 12)
    for k in range(4):
        M.extrude("akzent", (0, 0, 0.15), [(0.1, 0), (0.32, -0.08), (0.32, 0.12), (0.1, 0.35)], 0.025, plane="XZ", rot=(0, 0, 90 * k))
    M.lathe("dunkel", (0, 0, 0.0), [(0.07, 0), (0.1, 0.15)], 10)
    M.lathe("glut", (0, 0, -0.35), [(0, 0), (0.08, 0.3), (0.06, 0.35)], 10)
    return m


def a_schildkuppel():
    """Schildkuppel: Gitter aus leuchtenden Streben über dem Generator. Radius 6 m (im Spiel skaliert)."""
    m = Model("schildkuppel", "Schildkuppel", "Abwehr"); M = m.body; R = 6.0
    for k in range(12):  # Meridiane
        a = 2 * math.pi * k / 12
        pts = [(R * math.cos(b) * math.cos(a), R * math.cos(b) * math.sin(a), R * math.sin(b)) for b in [i * math.pi / 2 / 8 for i in range(9)]]
        M.pipe("schildfeld", pts, 0.05, 6, joints=False)
    for j in range(1, 5):  # Breitenkreise
        b = j * math.pi / 2 / 5; rr = R * math.cos(b); z = R * math.sin(b)
        M.torus("schildfeld", (0, 0, z), rr, 0.05, 48, 5)
    M.torus("stahl", (0, 0, 0.05), R, 0.15, 48, 6)
    return m


def n_moos(v):
    """Moos- und Flechtenpolster: flach, weich gewellt, mit kleinen Trieben."""
    m = Model("moos_%d" % v, "Moospolster %d" % v, "Natur"); M = m.body; r = _rnd(4300 + v)
    mats = ("laub3", "laub1") if v != 3 else ("trockengras", "laub2")
    for i in range(3 + v):
        a = r.random() * 6.283; d = 0 if i == 0 else 0.35 + 0.35 * r.random(); s = 0.55 if i == 0 else 0.3 + 0.2 * r.random()
        M.blob(mats[i % 2], (d * math.cos(a), d * math.sin(a), 0.02), s, 4300 + 10 * v + i, (1.3, 1.1, 0.28), 0.25, 12, 5, facet=0.08)
    for i in range(10):
        a = r.random() * 6.283; d = r.random() * 0.6
        p = (d * math.cos(a), d * math.sin(a), 0.1)
        M.cyl("laub2", p, 0.015, 0.12 + 0.08 * r.random(), 5, r2=0.005)
        M.sphere("bluete" if v == 2 else "laub2", (p[0], p[1], p[2] + 0.18), 0.025, 6, 3)
    return m


def n_teich():
    """Teich: Wasserfläche, Uferkranz aus Steinen, Schilf und Seerosen. Ca. 5 m breit."""
    m = Model("teich", "Teich", "Natur"); M = m.body; r = _rnd(4400)
    prof = [(2.3, -0.05), (2.25, 0.02), (0.0, 0.02)]
    M.lathe("wasser", (0, 0, 0), prof, 32)
    M.lathe("fels", (0, 0, -0.05), [(2.7, 0), (2.55, 0.08), (2.3, 0.06), (2.3, 0.0)], 32, closed_loop=False)
    for i in range(18):
        a = 2 * math.pi * i / 18 + r.random() * 0.2
        M.blob("fels" if i % 3 else "felsdkl", (2.45 * math.cos(a), 2.45 * math.sin(a), 0.05), 0.18 + 0.14 * r.random(), 4400 + i, (1.2, 1, 0.6), 0.35, 8, 5)
    for i in range(14):  # Schilf
        a = r.random() * 1.8 + 0.4; d = 2.0 + 0.3 * r.random()
        p = (d * math.cos(a), d * math.sin(a), 0)
        M.cyl("gras", p, 0.02, 0.9 + 0.5 * r.random(), 5, r2=0.006)
        if i % 3 == 0:
            M.cyl("rinde", (p[0], p[1], 0.8), 0.04, 0.22, 6)
    for i in range(5):  # Seerosen
        a = r.random() * 6.283; d = 0.6 + 1.1 * r.random()
        M.cyl("laub1", (d * math.cos(a), d * math.sin(a), 0.025), 0.28, 0.015, 12)
        if i % 2 == 0:
            M.sphere("bluete", (d * math.cos(a) + 0.05, d * math.sin(a), 0.07), 0.06, 8, 4)
    if DETAIL:  # mehr Schilf, Kiesel und Seerosen
        for i in range(12):
            a = r.random() * 1.6 + 3.6; d = 2.05 + 0.25 * r.random()
            M.cyl("gras", (d * math.cos(a), d * math.sin(a), 0), 0.018, 0.7 + 0.5 * r.random(), 5, r2=0.005)
        for i in range(24):
            a = r.random() * 6.283; d = 2.2 + 0.6 * r.random()
            M.blob("fels", (d * math.cos(a), d * math.sin(a), 0.02), 0.06 + 0.05 * r.random(), 4460 + i, (1.2, 1, 0.6), 0.3, 6, 4)
        for i in range(4):
            a = r.random() * 6.283; d = 0.8 + 1.0 * r.random()
            M.cyl("laub2", (d * math.cos(a), d * math.sin(a), 0.025), 0.2, 0.012, 12)
    return m


def a_bohrsockel():
    """Felssockel für Bohrturm und Erzbagger: flacher Hügel aus Bodengestein, oben eben. Radius 2,3 m."""
    m = Model("bohrsockel", "Felssockel", "Anlagen"); M = m.body; r = _rnd(4500)
    M.blob("fels", (0, 0, -0.35), 2.2, 4500, (1, 1, 0.22), 0.18, 18, 7, facet=0.06)
    M.cyl("platte", (0, 0, 0.0), 1.7, 0.08, 24)
    for i in range(9):
        a = 2 * math.pi * i / 9 + r.random() * 0.3
        M.blob("fels" if i % 2 else "felsdkl", (2.1 * math.cos(a), 2.1 * math.sin(a), -0.05), 0.35 + 0.25 * r.random(), 4510 + i, (1.2, 1.1, 0.7), 0.35, 10, 6)
    return m


def e_erzader_neu():
    """Erzader: Felsgruppe mit leuchtenden Erzadern (statt der alten Achteck-Platte)."""
    m = Model("erzader_fels", "Erzader (Felsen)", "Ereignisse"); M = m.body; r = _rnd(4600)
    for i in range(5):
        a = 2 * math.pi * i / 5; d = 0 if i == 0 else 0.8 + 0.3 * r.random(); s = 0.75 if i == 0 else 0.35 + 0.2 * r.random()
        c = (d * math.cos(a), d * math.sin(a), s * 0.45)
        M.blob("felsdkl", c, s, 4600 + i, (1.1, 1, 0.8), 0.35, 12, 7)
        for k in range(3):
            b = r.random() * 6.283
            M.box("glut" if k % 2 == 0 else "kupfer", (c[0] + s * 0.85 * math.cos(b), c[1] + s * 0.85 * math.sin(b), c[2] + (r.random() - 0.3) * s * 0.6), (0.05, s * 0.9, 0.04), rot=(r.random() * 40, 0, math.degrees(b)))
    for k in range(3):
        M.lathe("kristall", (0.3 * k - 0.3, -0.5, 0.05), [(0, 0), (0.05, 0.04), (0.05, 0.2), (0, 0.32)], 6, rot=(15 * k - 15, 10, 0))
    return m


def w_putzi():
    """Putzi-7: kleiner Begleitroboter auf Ketten, runder Kopf mit Visier, Antenne, zwei Greifarme, Bürste vorne. Ca. 1 m."""
    m = Model("putzi", "Putzi-7", "Welt"); M = m.body
    for sx in (-1, 1):
        track(M, (sx * 0.28, 0, 0.0), 0.6, 0.14, 0.18)
    M.box("dunkel", (0, 0, 0.24), (0.5, 0.5, 0.12))
    M.lathe("weiss", (0, 0, 0.3), [(0.28, 0), (0.3, 0.15), (0.26, 0.42), (0.0, 0.46)], 18)
    M.lathe("akzent", (0, 0, 0.42), [(0.305, 0), (0.305, 0.06)], 18)
    M.sphere("weiss", (0, 0, 0.92), 0.22, 16, 8)
    M.sphere("glas", (0, -0.12, 0.94), 0.16, 14, 6, zcut=-0.3)
    for sx in (-1, 1):
        M.sphere("mint", (sx * 0.07, -0.25, 0.97), 0.035, 8, 4)
    M.cyl("stahl", (0.12, 0.05, 1.1), 0.012, 0.25, 6)
    M.sphere("warn", (0.12, 0.05, 1.37), 0.035, 8, 4)
    for sx in (-1, 1):
        M.tube("stahl", (sx * 0.3, -0.05, 0.6), (sx * 0.42, -0.25, 0.45), 0.035, 8)
        M.box("akzent", (sx * 0.44, -0.3, 0.42), (0.08, 0.1, 0.06))
    tool_brush(m, (0, -0.4, 0.12), 0.5, 0.09)
    if DETAIL:  # Nieten, Seitenlichter, kleines Solarpaneel auf dem Rücken
        for k in range(12):
            a = 2 * math.pi * k / 12
            M.sphere("stahl", (0.305 * math.cos(a), 0.305 * math.sin(a), 0.48), 0.018, 6, 3)
        for sx in (-1, 1):
            M.sphere("lampe", (sx * 0.27, -0.12, 0.62), 0.04, 8, 4)
        M.box("solar", (0, 0.27, 0.68), (0.32, 0.03, 0.22), rot=(-20, 0, 0))
    return m


def e_haendler():
    """Händlerschiff (Fabian 11.11. überarbeitet): schlanker Rumpf mit Spitze, Cockpit-Haube, Containerrahmen auf dem Rücken,
    zwei große Triebwerke an Auslegern, Deltaflügel mit Winglets, Landestrahl unten. Ca. 12 m. Akzent = Farbe im Spiel."""
    m = Model("haendlerschiff", "Händlerschiff", "Ereignisse"); M = m.body
    # Rumpf: Drehkörper entlang Y (Spitze vorne −Y), unten leicht abgeflacht durch einen dunklen Kiel
    M.lathe("weiss", (0, 0, 0), [(0, -6.0), (0.35, -5.6), (0.9, -4.6), (1.35, -3.0), (1.55, -1.0), (1.55, 2.6), (1.3, 4.2), (0.95, 5.0), (0, 5.2)], 20, rot=(90, 0, 0))
    M.extrude("dunkel", (0, 0, -1.35), [(-4.2, 0.0), (-3.0, -0.35), (3.8, -0.35), (4.6, 0.0)], 1.5, plane="YZ")
    for y0 in (-2.2, 1.0, 3.6):
        M.lathe("akzent", (0, y0, 0), [(1.58, 0), (1.58, 0.35)], 20, rot=(90, 0, 0))
    # Cockpit-Haube (lang gezogen) und Brücke
    M.lathe("glas", (0, -4.0, 0.95), [(0, -1.0), (0.4, -0.65), (0.55, 0.0), (0.45, 0.8), (0, 1.2)], 14, rot=(90, 0, 0))
    M.extrude("hell", (0, 0, 0), [(-3.0, 1.3), (-2.4, 1.75), (-1.2, 1.75), (-0.8, 1.35)], 1.1, plane="YZ")
    M.box("glas", (0, -2.35, 1.6), (0.9, 0.06, 0.18), rot=(-35, 0, 0))
    # Deltaflügel mit Winglets und Positionslichtern
    for sx in (-1, 1):
        M.extrude("hell", (0, 0, -0.25), [(sx * 1.3, -2.2), (sx * 4.6, 1.6), (sx * 4.6, 2.6), (sx * 1.3, 3.2)], 0.16, plane="XY")
        M.extrude("akzent", (0, 0, -0.25), [(sx * 4.4, 1.5), (sx * 4.6, 1.6), (sx * 4.6, 2.6), (sx * 4.4, 2.6)], 0.18, plane="XY")
        M.extrude("akzent", (sx * 4.6, 0, -0.1), [(1.6, 0.0), (2.6, 0.0), (2.8, 1.1), (2.3, 1.1)], 0.1, plane="YZ")
        M.sphere("mint" if sx > 0 else "warn", (sx * 4.62, 2.75, 1.05), 0.1, 8, 4)
        # Triebwerk am Ausleger
        M.box("dunkel", (sx * 2.25, 3.3, 0.35), (1.6, 1.0, 0.25))
        M.cyl("hell", (sx * 3.0, 2.0, 0.45), 0.72, 2.6, 18, rot=(90, 0, 0), r2=0.62)
        M.cyl("dunkel", (sx * 3.0, 1.85, 0.45), 0.6, 0.2, 18, rot=(90, 0, 0))
        M.lathe("akzent", (sx * 3.0, 2.6, 0.45), [(0.74, 0), (0.74, 0.3)], 18, rot=(90, 0, 0))
        M.cyl("dunkel", (sx * 3.0, 4.6, 0.45), 0.6, 0.35, 18, rot=(90, 0, 0), r2=0.5)
        M.cyl("glut", (sx * 3.0, 4.96, 0.45), 0.46, 0.05, 18, rot=(90, 0, 0))
    # Containerrahmen auf dem Rücken: 2 × 3 Container
    for i in range(3):
        y = -0.6 + i * 1.45
        for sx in (-1, 1):
            M.box(("kiste1", "kiste2", "kiste3")[(i + (sx > 0)) % 3], (sx * 0.62, y, 1.95), (1.15, 1.3, 1.0))
            for k in range(3):
                M.box("dunkel", (sx * 0.62, y - 0.45 + k * 0.45, 1.95), (1.18, 0.05, 0.9))
    for y in (-1.4, 0.15, 1.6, 3.05):
        M.box("stahl", (0, y, 1.45), (2.7, 0.12, 0.12)); M.box("stahl", (0, y, 2.5), (2.7, 0.12, 0.12))
        for sx in (-1, 1):
            M.box("stahl", (sx * 1.3, y, 1.95), (0.12, 0.12, 1.15))
    M.box("stahl", (0, 0.8, 2.52), (0.12, 4.5, 0.1))
    # Heckflosse mit Antenne
    M.extrude("akzent", (0, 0, 0), [(3.0, 1.2), (4.7, 0.9), (5.3, 2.7), (4.6, 2.75)], 0.14, plane="YZ")
    M.cyl("stahl", (0, 5.0, 2.7), 0.03, 0.8, 6); M.sphere("warn", (0, 5.0, 3.55), 0.07, 6, 3)
    # Landestrahl-Öffnung und Lichter unten
    M.cyl("kaltlicht", (0, 0.5, -1.75), 0.6, 0.06, 16)
    M.lathe("dunkel", (0, 0.5, -1.72), [(0.9, 0), (0.7, 0.3), (0, 0.3)], 16)
    for k in range(5):
        M.sphere("lampe", (0, -3.4 + k * 1.7, -1.72), 0.08, 6, 3)
    M.sphere("lampe", (0, -5.9, -0.05), 0.12, 8, 4)
    # Fensterband
    for i in range(7):
        for sx in (-1, 1):
            M.box("glas", (sx * 1.54, -2.6 + i * 0.75, 0.45), (0.05, 0.4, 0.2))
    # Fabian 11.11.: Landebeine – das Schiff landet jetzt auf dem Landeplatz (Unterseite der Füße bei z = −2,4)
    F = m.part("Fahrwerk", (0, 0, -1.5))
    for sx in (-1, 1):
        for y0 in (-2.6, 2.4):
            F.tube("stahl", (sx * 0.8, y0, -1.2), (sx * 1.25, y0 + (-0.25 if y0 < 0 else 0.25), -2.25), 0.09)
            F.tube("dunkel", (sx * 0.5, y0 + (0.5 if y0 < 0 else -0.5), -1.4), (sx * 1.2, y0 + (-0.2 if y0 < 0 else 0.2), -1.9), 0.05)
            F.cyl("gummi", (sx * 1.25, y0 + (-0.25 if y0 < 0 else 0.25), -2.4), 0.3, 0.15, 12)
    return m


def e_lieferschiff():
    """Lieferschiff (Fabian 11.11.): kleines Frachtschiff für Lieferungen von anderen Planeten. Landet senkrecht auf dem Landeplatz,
    öffnet die Heckrampe, lädt ab und startet wieder. Ca. 8 m lang, 7,6 m breit. Bewegliche Teile:
    Duesen (schwenkbare Triebwerke, Drehpunkt auf der Querachse), Rampe (Scharnier unten am Heck), Ladung (Kisten), Fahrwerk.
    Am Boden steht es auf dem Fahrwerk (Rumpfunterseite 1 m hoch). Akzent = Farbe des gelieferten Guts."""
    m = Model("lieferschiff", "Lieferschiff", "Ereignisse"); M = m.body
    side = [(-4.0, 1.55), (-3.4, 1.12), (-1.0, 1.0), (2.9, 1.0), (3.9, 1.25), (3.9, 2.35), (2.6, 2.65), (-1.5, 2.65), (-2.9, 2.3), (-3.8, 1.95)]
    M.extrude("weiss", (0, 0, 0), side, 2.3, plane="YZ")
    M.extrude("hell", (0, 0, 0), [(-1.4, 2.6), (2.5, 2.6), (2.2, 2.95), (-0.9, 2.95)], 1.6, plane="YZ")
    M.extrude("dunkel", (0, 0, 0), [(-3.3, 1.08), (-1.0, 0.95), (2.9, 0.95), (3.8, 1.2), (3.8, 1.3), (-3.3, 1.3)], 2.36, plane="YZ")
    for sx in (-1, 1):
        M.box("akzent", (sx * 1.16, 0.4, 1.95), (0.04, 6.2, 0.2))
        for i in range(4):
            M.box("glas", (sx * 1.16, -2.3 + i * 0.55, 2.3), (0.05, 0.34, 0.18))
        M.box("warn", (sx * 1.16, 3.2, 1.55), (0.05, 0.9, 0.1))
    # Cockpit-Haube
    M.extrude("glas", (0, 0, 0), [(-3.75, 1.9), (-2.8, 2.5), (-2.0, 2.55), (-2.3, 2.1), (-3.3, 1.7)], 1.5, plane="YZ")
    M.box("dunkel", (0, -2.0, 2.56), (1.4, 0.12, 0.08))
    # Stummelflügel mit Ausleger
    for sx in (-1, 1):
        M.extrude("hell", (0, 0, 1.55), [(sx * 1.1, -0.9), (sx * 2.85, -0.2), (sx * 2.85, 1.2), (sx * 1.1, 1.8)], 0.2, plane="XY")
        M.extrude("akzent", (0, 0, 1.56), [(sx * 2.55, -0.32), (sx * 2.85, -0.2), (sx * 2.85, 1.2), (sx * 2.55, 1.3)], 0.2, plane="XY")
        M.sphere("mint" if sx > 0 else "warn", (sx * 2.88, 0.5, 1.68), 0.09, 8, 4)
    # Heckflosse
    M.extrude("akzent", (0, 0, 0), [(2.3, 2.9), (3.6, 2.9), (4.0, 3.9), (3.45, 3.9)], 0.14, plane="YZ")
    M.sphere("warn", (0, 3.75, 3.95), 0.06, 6, 3)
    # Lichter vorne und unten
    M.sphere("lampe", (0, -3.98, 1.5), 0.12, 8, 4)
    for sx in (-0.7, 0.7):
        M.sphere("lampe", (sx, -3.2, 1.1), 0.08, 6, 3)
    M.cyl("kaltlicht", (0, 0.6, 0.93), 0.35, 0.04, 14)
    # Heck: dunkle Ladeöffnung (sichtbar, wenn die Rampe offen ist)
    M.box("dunkel", (0, 3.91, 1.75), (1.8, 0.04, 1.2))
    # Schwenkbare Triebwerke (Gondeln an den Flügelspitzen). Drehpunkt auf der Achse; im Flug zeigen sie nach hinten (+Y), zum Landen nach unten.
    D = m.part("Duesen", (0, 0.5, 1.65))
    for sx in (-1, 1):
        x = sx * 3.35
        D.cyl("hell", (x, -0.6, 1.65), 0.42, 1.9, 16, rot=(90, 0, 0), r2=0.36)
        D.cyl("dunkel", (x, -0.68, 1.65), 0.36, 0.12, 16, rot=(90, 0, 0))
        D.lathe("akzent", (x, -0.15, 1.65), [(0.44, 0), (0.44, 0.22)], 16, rot=(90, 0, 0))
        D.cyl("dunkel", (x, 1.3, 1.65), 0.36, 0.25, 16, rot=(90, 0, 0), r2=0.3)
        D.cyl("glut", (x, 1.56, 1.65), 0.28, 0.04, 16, rot=(90, 0, 0))
        D.box("stahl", (sx * 3.0, 0.5, 1.65), (0.5, 0.3, 0.16))
    # Heckrampe: Scharnier unten am Heck, geschlossen senkrecht
    R = m.part("Rampe", (0, 3.92, 1.15))
    R.box("hell", (0, 3.97, 1.75), (1.9, 0.1, 1.25))
    R.box("akzent", (0, 4.03, 1.75), (1.92, 0.02, 0.12))
    for k in range(4):
        R.box("dunkel", (0, 3.92, 1.3 + k * 0.28), (1.6, 0.03, 0.05))
    # Ladung: drei Kisten im Laderaum (das Spiel schiebt sie über die Rampe hinaus)
    L = m.part("Ladung", (0, 3.4, 1.0))
    for i, c in enumerate(("kiste1", "kiste2", "kiste3")):
        L.box(c, (-0.5 + i * 0.5, 3.4, 1.27), (0.45, 0.45, 0.45))
        L.box("dunkel", (-0.5 + i * 0.5, 3.4, 1.27), (0.47, 0.06, 0.47))
    # Fahrwerk: vier Beine mit Tellerfüßen
    F = m.part("Fahrwerk", (0, 0, 1.0))
    for sx in (-1, 1):
        for y0, y1 in ((-2.3, -2.55), (2.4, 2.65)):
            F.tube("stahl", (sx * 0.85, y0, 1.0), (sx * 1.15, y1, 0.12), 0.07)
            F.tube("dunkel", (sx * 0.85, y0 + (0.4 if y0 < 0 else -0.4), 1.0), (sx * 1.1, y1, 0.35), 0.04)
            F.cyl("gummi", (sx * 1.15, y1, 0.0), 0.24, 0.12, 12)
    return m


def w_landeplatz():
    """Landeplatz (Fabian 11.11.): Plattform für Lieferschiffe von anderen Planeten. 7,8 × 7,8 m mit abgeschrägten Ecken,
    Landekreis, Markierungen, kleiner Turm in einer Ecke. Die Randlichter sind ein eigener Teil (Lichter) zum Blinken. Akzent = Planetenfarbe."""
    m = Model("landeplatz", "Landeplatz", "Welt", fp=4.4); M = m.body
    e, c, h = 3.9, 1.1, 0.3
    octo = [(-e + c, -e), (e - c, -e), (e, -e + c), (e, e - c), (e - c, e), (-e + c, e), (-e, e - c), (-e, -e + c)]
    M.extrude("felsdkl", (0, 0, 0), [(x * 1.03, y * 1.03) for x, y in octo], 0.12, plane="XY")
    M.extrude("platte", (0, 0, 0), octo, h, plane="XY")
    M.extrude("akzent", (0, 0, h), [(-e + c, -e), (e - c, -e), (e - c + 0.05, -e + 0.25), (-e + c - 0.05, -e + 0.25)], 0.02, plane="XY")
    M.extrude("akzent", (0, 0, h), [(-e + c, e), (-e + c - 0.05, e - 0.25), (e - c + 0.05, e - 0.25), (e - c, e)], 0.02, plane="XY")
    # Landekreis und Zielkreuz
    M.lathe("hell", (0, 0, h), [(2.75, 0), (2.75, 0.02), (2.45, 0.02), (2.45, 0)], 40, closed_loop=True)
    M.lathe("warn", (0, 0, h), [(1.15, 0), (1.15, 0.025), (1.0, 0.025), (1.0, 0)], 32, closed_loop=True)
    for rz in (0, 90):
        M.box("hell", (0, 0, h + 0.012), (0.22, 1.7, 0.025), rot=(0, 0, rz))
    # Anflug-Pfeile vorne (−Y)
    for k in range(3):
        y = -e + 0.55 + k * 0.0
    for k in range(2):
        M.extrude("hell", (0, 0, h), [(-0.5, -3.3 + k * 0.45), (0, -3.0 + k * 0.45), (0.5, -3.3 + k * 0.45), (0.5, -3.1 + k * 0.45), (0, -2.8 + k * 0.45), (-0.5, -3.1 + k * 0.45)], 0.02, plane="XY")
    # Warnstreifen an den Seiten
    for sx in (-1, 1):
        for k in range(6):
            M.box("warn" if k % 2 == 0 else "hell", (sx * (e - 0.18), -1.5 + k * 0.6, h + 0.01), (0.2, 0.5, 0.02))
    # Kleiner Turm in der Ecke hinten rechts (innerhalb der abgeschrägten Ecke)
    tx, ty = 2.85, 2.85
    M.box("hell", (tx, ty, h + 0.7), (0.7, 0.7, 1.4))
    M.box("glas", (tx, ty, h + 1.6), (0.85, 0.85, 0.4))
    M.box("dunkel", (tx, ty, h + 1.85), (0.95, 0.95, 0.1))
    M.cyl("stahl", (tx, ty, h + 1.9), 0.025, 0.7, 6)
    M.sphere("warn", (tx, ty, h + 2.65), 0.06, 6, 3)
    # kleiner Tank in der Ecke hinten links
    M.cyl("weiss", (-2.95, 2.95, h), 0.4, 0.9, 14)
    M.lathe("akzent", (-2.95, 2.95, h + 0.5), [(0.42, 0), (0.42, 0.1)], 14)
    # Randlichter (blinken im Spiel)
    Lc = m.part("Lichter", (0, 0, h))
    for i in range(8):
        x, y = octo[i]; nx, ny = octo[(i + 1) % 8]
        for t in (0.25, 0.75):
            Lc.sphere("lampe", (x + (nx - x) * t, y + (ny - y) * t, h + 0.05), 0.07, 8, 4)
    for k in range(3):
        Lc.sphere("mint", (-0.9 + k * 0.9, -e + 0.12, h + 0.05), 0.07, 8, 4)
    return m


def e_muellberg():
    """Müllberg: großer Haufen aus Schrott, Fässern, Kisten und Geröll. Ca. 7 m breit, 3 m hoch."""
    m = Model("muellberg", "Müllberg", "Ereignisse"); M = m.body; r = _rnd(4700)
    M.blob("felsdkl", (0, 0, 0.0), 3.0, 4700, (1.1, 1, 0.7), 0.25, 18, 9)
    for i in range(40):
        a = r.random() * 6.283; d = r.random() * 2.7; z = 2.0 * math.sqrt(max(0.0, 1 - (d / 3.2) ** 2)) + 0.05  # auf der Oberfläche des Haufens
        p = (d * math.cos(a), d * math.sin(a), z); k = i % 5
        if k == 0: M.box(("kiste1", "kiste2", "kiste3")[i % 3], p, (0.5, 0.5, 0.45), rot=(r.random() * 30, r.random() * 30, r.random() * 90))
        elif k == 1: M.cyl(("rost", "akzent", "rot")[i % 3], p, 0.22, 0.6, 12, rot=(r.random() * 90, r.random() * 90, 0))
        elif k == 2: M.box("rost", p, (1.1, 0.12, 0.08), rot=(r.random() * 40, r.random() * 60, r.random() * 180))
        elif k == 3: M.box("stahl", p, (0.6, 0.45, 0.03), rot=(r.random() * 60, r.random() * 60, r.random() * 180))
        else: M.blob("fels", p, 0.25 + 0.2 * r.random(), 4710 + i, (1, 1, 0.8), 0.4, 8, 5)
    M.cyl("schleim", (1.8, -1.6, 0.02), 0.8, 0.03, 16)
    return m


def w_flagge():
    """Flagge: Mast mit Stoff (Akzentfarbe = Planetenfarbe im Spiel) und Spitze. Stoff als eigener Teil zum Wehen. Ca. 3 m."""
    m = Model("flagge", "Flagge", "Welt"); M = m.body
    M.cyl("felsdkl", (0, 0, 0), 0.25, 0.12, 10, r2=0.18)
    M.cyl("stahl", (0, 0, 0.1), 0.035, 2.9, 8)
    M.sphere("akzent2", (0, 0, 3.05), 0.07, 8, 4)
    F = m.part("Stoff", (0, 0, 2.95))
    n = 6; verts = []; faces = []
    for i in range(n + 1):
        x = 1.3 * i / n; wv = 0.08 * math.sin(i * 1.2)
        verts += [(x + 0.04, wv, 2.95), (x + 0.04, wv, 2.15)]
    for i in range(n):
        a = 2 * i; faces.append((a, a + 2, a + 3, a + 1)); faces.append((a + 1, a + 3, a + 2, a))
    F.add(verts, faces, "akzent", closed=False)
    return m


def w_truhe():
    """Schatztruhe: Holzkasten mit Metallbändern und Schloss, Deckel als eigener Teil (Drehpunkt hinten oben)."""
    m = Model("truhe", "Schatztruhe", "Welt"); M = m.body
    M.box("kiste1", (0, 0, 0.25), (0.9, 0.6, 0.5))
    for x in (-0.35, 0.35):
        M.box("stahl", (x, 0, 0.25), (0.06, 0.62, 0.52))
    M.box("visier", (0, -0.31, 0.42), (0.12, 0.03, 0.14))
    D = m.part("Deckel", (0, 0.3, 0.5))
    D.lathe("kiste1", (-0.45, 0.0, 0.5), [(0.3, 0), (0.3, 0.9)], 12, rot=(0, 90, 0), a0=0, a1=180)
    for x in (-0.35, 0.35):
        D.lathe("stahl", (x - 0.03, 0.0, 0.5), [(0.31, 0), (0.31, 0.06)], 12, rot=(0, 90, 0), a0=0, a1=180)
    M.sphere("bluete", (0, 0, 0.45), 0.12, 8, 4)  # Glitzern im Inneren
    return m


def w_wahrzeichen(v):
    """Wahrzeichen: 1 Obelisk mit schwebendem Ring, 2 Steinbogen mit Kristall, 3 Antennenturm mit Schüssel. Bewegliches Teil „Dreh“."""
    names = {1: "Obelisk", 2: "Steinbogen", 3: "Antennenturm"}
    m = Model("wahrzeichen_%d" % v, "Wahrzeichen: " + names[v], "Welt"); M = m.body
    if v == 1:
        M.lathe("felsdkl", (0, 0, 0), [(1.2, 0), (1.1, 0.3), (0.55, 0.35), (0.5, 4.5), (0.0, 5.3)], 4)
        for k in range(4):
            M.box("mint", (0.52 * math.cos(math.radians(45 + 90 * k)), 0.52 * math.sin(math.radians(45 + 90 * k)), 2.5), (0.04, 0.04, 3.0), rot=(0, 0, 45 + 90 * k))
        D = m.part("Dreh", (0, 0, 3.4)); D.torus("akzent", (0, 0, 3.4), 1.3, 0.08, 36, 6); D.torus("schildfeld", (0, 0, 3.4), 1.3, 0.03, 36, 4)
    elif v == 2:
        pts = [(2.0 * math.cos(math.radians(a)), 0, 2.6 * math.sin(math.radians(a))) for a in range(0, 181, 15)]
        for i, (a, b) in enumerate(zip(pts, pts[1:])):
            M.blob("fels" if i % 2 else "felsdkl", ((a[0] + b[0]) / 2, 0, (a[2] + b[2]) / 2), 0.6, 4800 + i, (1, 0.8, 1), 0.3, 10, 6)
        D = m.part("Dreh", (0, 0, 1.3)); D.lathe("kristall", (0, 0, 0.8), [(0, 0), (0.25, 0.3), (0.25, 0.9), (0, 1.3)], 6)
    else:
        lattice(M, (0, 0, 0), (0, 0, 6.0), 1.2, 0.3, 6)
        beacon(M, (0, 0, 6.0), 0.6)
        D = m.part("Dreh", (0, 0, 4.2)); D.cyl("stahl", (0, 0, 4.0), 0.1, 0.4, 8); dish(D, (0, 0, 4.4), 0.9, 0.3, tilt=30)
    return m


def w_geysir():
    """Geysir-Schlot: Kraterrand aus Gestein, Kruste mit Mineralablagerungen, Öffnung in der Mitte (dort startet die Fontäne)."""
    m = Model("geysir_schlot", "Geysir-Schlot", "Welt"); M = m.body; r = _rnd(4900)
    M.lathe("felsdkl", (0, 0, 0), [(1.8, 0), (1.3, 0.35), (0.6, 0.45), (0.35, 0.2), (0.3, -0.3)], 20)
    M.lathe("schwefel", (0, 0, 0.02), [(1.15, 0.3), (0.95, 0.38), (0.75, 0.43), (0.75, 0.3)], 20, closed_loop=True)
    for i in range(8):
        a = 2 * math.pi * i / 8 + r.random() * 0.4
        M.blob("fels", (1.6 * math.cos(a), 1.6 * math.sin(a), 0.1), 0.22 + 0.15 * r.random(), 4900 + i, (1.2, 1, 0.7), 0.35, 8, 5)
    M.cyl("russ", (0, 0, -0.25), 0.3, 0.05, 14)
    return m


def n_baum_spezial(kind):
    """Exotische Bäume: kristall (Kristallbaum), pilz (Riesenpilz), eis (Eisbaum)."""
    names = {"kristall": "Kristallbaum", "pilz": "Riesenpilz", "eis": "Eisbaum"}
    m = Model("baum_" + kind, names[kind], "Natur"); M = m.body; r = _rnd({"kristall": 51, "pilz": 52, "eis": 53}[kind])
    if kind == "kristall":
        M.lathe("felsdkl", (0, 0, 0), [(0.35, 0), (0.18, 0.4), (0.14, 2.2)], 6)
        for i in range(7):
            a = 2 * math.pi * i / 7; h = 1.6 + 0.5 * (i % 3)
            M.lathe("kristall", (0.15 * math.cos(a), 0.15 * math.sin(a), h), [(0, 0), (0.12, 0.15), (0.12, 0.8), (0, 1.1)], 6, rot=(35 * math.cos(a), 35 * math.sin(a), 0))
        M.lathe("kristall", (0, 0, 2.2), [(0, 0), (0.18, 0.2), (0.18, 1.0), (0, 1.5)], 6)
    elif kind == "pilz":
        M.lathe("pilzstiel", (0, 0, 0), [(0.45, 0), (0.3, 0.3), (0.25, 2.0), (0.35, 2.4)], 12)
        M.lathe("pilz", (0, 0, 2.3), [(0.3, 0), (1.6, 0.1), (1.7, 0.35), (1.2, 0.9), (0, 1.15)], 18)
        for i in range(9):
            a = r.random() * 6.283; d = 0.4 + 0.9 * r.random()
            M.sphere("pilzstiel", (d * math.cos(a), d * math.sin(a), 2.3 + 1.0 - 0.5 * d), 0.1, 6, 3)
    else:
        M.lathe("eis", (0, 0, 0), [(0.3, 0), (0.16, 0.4), (0.1, 3.4)], 8)
        for t in range(4):
            z = 1.1 + t * 0.7; R0 = 1.2 - 0.25 * t
            for k in range(6):
                a = 2 * math.pi * k / 6 + t * 0.5
                M.tube("eis", (0, 0, z), (R0 * math.cos(a), R0 * math.sin(a), z - 0.35), 0.06, 6, r2=0.02)
                M.lathe("kometeis", (R0 * math.cos(a), R0 * math.sin(a), z - 0.45), [(0, 0), (0.06, 0.05), (0, 0.3)], 5, rot=(180, 0, 0))
        M.lathe("kometeis", (0, 0, 3.4), [(0.15, 0), (0, 0.6)], 6)
    return m


def n_gras_var(v):
    """Grasbüschel-Varianten: 2 trocken, 3 blühend."""
    m = Model("gras_%d" % v, "Grasbüschel %d" % v, "Natur"); M = m.body; r = _rnd(5000 + v)
    for i in range(18 if DETAIL else 10):
        a = r.random() * 6.283; tilt = 0.25 + 0.35 * r.random(); h = 0.4 + 0.35 * r.random(); w = 0.035
        d = (math.cos(a), math.sin(a)); sd = (-d[1] * w, d[0] * w)
        base = (d[0] * 0.05, d[1] * 0.05, 0); mid = (d[0] * tilt * 0.4 * h, d[1] * tilt * 0.4 * h, h * 0.55); tip = (d[0] * tilt * h, d[1] * tilt * h, h)
        vs = [(base[0] - sd[0], base[1] - sd[1], 0), (base[0] + sd[0], base[1] + sd[1], 0), (mid[0] + sd[0] * 0.6, mid[1] + sd[1] * 0.6, mid[2]), (mid[0] - sd[0] * 0.6, mid[1] - sd[1] * 0.6, mid[2]), tip]
        M.add(vs, [(0, 1, 2, 3), (3, 2, 4), (3, 2, 1, 0), (2, 3, 4)], "trockengras" if v == 2 else ("gras" if i % 3 else "laub2"), closed=False)
        if v == 3 and i % 3 == 0:
            M.sphere("bluete" if i % 2 else "pilz", tip, 0.05, 10, 5)
    return m


def w_bestelllager():
    """Bestelllager: offene Halle mit Regalen und Kisten, Landefeld für Besucher-Shuttles davor, Leuchtschild. Grundkreis 2,4 m."""
    m = Model("bestelllager", "Bestelllager", "Welt", 2.4); M = m.body
    h0 = pad(M, 2.4)
    for sx in (-1, 1):
        M.box("hell", (sx * 1.85, 0.5, h0 + 1.15), (0.22, 2.4, 2.3))
        for k in range(4):
            M.box("akzent", (sx * 1.85, 0.5 - 1.1 + k * 0.73, h0 + 2.32), (0.26, 0.08, 0.08))
    M.box("hell", (0, 1.62, h0 + 1.15), (3.9, 0.22, 2.3))
    M.box("akzent", (0, 0.5, h0 + 2.42), (4.1, 2.7, 0.18))
    M.box("dunkel", (0, 0.5, h0 + 2.56), (3.6, 2.2, 0.1))
    for z in (0.05, 0.85, 1.6):  # Regalböden
        M.box("stahl", (0, 1.15, h0 + z + 0.04), (3.4, 0.7, 0.06))
    for i in range(9):
        x = -1.2 + (i % 3) * 1.2; z = [0.1, 0.9, 1.65][i // 3]
        M.box(("kiste1", "kiste2", "kiste3")[i % 3], (x + 0.05 * (i % 2), 1.15, h0 + z + 0.3), (0.6, 0.55, 0.5))
    M.box("mint", (0, -0.78, h0 + 2.2), (1.6, 0.04, 0.3))  # Leuchtschild „Bestellungen“
    # Landefeld für Shuttles
    M.cyl("platte", (0, -2.0, h0), 1.0, 0.04, 24)
    M.lathe("warn", (0, -2.0, h0 + 0.04), [(0.95, 0), (0.95, 0.02), (0.8, 0.02), (0.8, 0)], 24, closed_loop=True)
    beacon(M, (1.9, -1.9, h0), 0.9, "mint")
    return m


SOFT_MODELS = {"riesenmeteor", "brocken_1", "brocken_2", "brocken_3", "bohrsockel", "erzader_fels", "moos_1", "moos_2", "moos_3", "muellberg", "busch_1", "busch_2"}

# Alle Modelle, die nach dem ersten Paket dazugekommen sind (Build 34–38)
NEW_KEYS = ["schild", "flak", "lieferrakete", "baum_laub_1", "baum_laub_2", "baum_laub_3", "baum_tanne_1", "baum_tanne_2", "busch_1", "busch_2", "gras_1",
            "riesenmeteor", "brocken_1", "brocken_2", "brocken_3", "abwehrrakete", "schildkuppel", "moos_1", "moos_2", "moos_3", "teich", "bohrsockel", "erzader_fels",
            "putzi", "haendlerschiff", "muellberg", "flagge", "truhe", "wahrzeichen_1", "wahrzeichen_2", "wahrzeichen_3", "geysir_schlot",
            "baum_kristall", "baum_pilz", "baum_eis", "gras_2", "gras_3", "bestelllager",
            "lieferschiff", "landeplatz"]


def all_models():
    """(Schlüssel, Bauer) in der Reihenfolge, in der sie in der Szene stehen."""
    return [
        ("hq", b_hq), ("roh", b_roh), ("sell", b_sell), ("prod", b_prod),
        ("u1", lambda: b_unload("u1", 1)), ("u2", lambda: b_unload("u2", 2)), ("u3", lambda: b_unload("u3", 3)),
        ("presse", b_presse), ("schmelze", b_schmelze), ("raff", b_raff),
        ("energy", b_energy), ("rtg", b_rtg), ("geo", b_geo), ("reactor", b_reactor),
        ("werkstatt", b_werkstatt), ("research", b_research), ("hangar", b_hangar), ("lifter", b_lifter), ("port", b_port),
        ("rig", b_rig), ("bagger", b_bagger),
        ("staubi", v_staubi), ("buggy", v_buggy), ("sauger", v_sauger), ("truck", v_truck),
        ("eisfraese", v_eisfraese), ("schlammraupe", v_schlammraupe), ("lavapanzer", v_lavapanzer),
        ("schwebe", v_schwebe), ("magnetgleiter", v_magnetgleiter), ("kryokoloss", v_kryokoloss),
        ("suchtrupp", lambda: v_buggy("suchtrupp", "Suchtrupp", "orange", crane=True)),
        ("drohne", lambda: d_drone(False)), ("lastdrohne", lambda: d_drone(True)),
        # Förderbänder: nur Stützen und Anschlüsse – die Bänder selbst baut das Spiel (Fabian 02.11.: unbenutzte Bandstücke entfernt)
        ("band_stuetze", lambda: f_stuetze(0.0)), ("band_stuetze_hoch", lambda: f_stuetze(1.0)),
        ("anschluss_eingang", lambda: f_anschluss(False)), ("anschluss_ausgang", lambda: f_anschluss(True)),
        ("uebergabe_saeule", f_uebergabe),
    ] + [("ware_" + k, (lambda k=k, n=n, sh=sh, c=c: product(k, n, sh, c))) for k, n, sh, c in PRODUCTS] + [
        ("schmutz_fels_1", lambda: d_rock(1)), ("schmutz_fels_2", lambda: d_rock(2)), ("schmutz_fels_3", lambda: d_rock(3)),
        ("schmutz_schrott_1", lambda: d_scrap(1)), ("schmutz_schrott_2", lambda: d_scrap(2)), ("schmutz_schrott_3", lambda: d_scrap(3)),
        ("schmutz_satellit_1", lambda: d_sat(1)), ("schmutz_satellit_2", lambda: d_sat(2)),
        ("schmutz_kristall_1", lambda: d_crystal(1)), ("schmutz_kristall_2", lambda: d_crystal(2)),
        ("schmutz_fass_1", lambda: d_barrel(1)), ("schmutz_fass_2", lambda: d_barrel(2)),
        ("schmutz_eis_1", lambda: d_ice(1)), ("schmutz_eis_2", lambda: d_ice(2)),
        ("schmutz_schleim_1", lambda: d_goo(1)), ("schmutz_schleim_2", lambda: d_goo(2)),
        ("schmutz_schwefel_1", lambda: d_sulfur(1)), ("schmutz_schwefel_2", lambda: d_sulfur(2)),
        ("schmutz_lava_1", lambda: d_lava(1)), ("schmutz_lava_2", lambda: d_lava(2)),
        ("wrack_raumschiff", e_wrack), ("wrack_satellit", e_satwrack), ("frachter", e_frachter),
        ("komet", e_komet), ("kometeneinschlag", e_einschlag), ("meteorit", e_meteorit), ("leuchtkristall", w_leuchtkristall), ("laterne", w_laterne), ("astronaut", w_astronaut), ("sammelwagen", w_sammelwagen),
        ("aussenlager", w_aussenlager), ("bauplatz", w_bauplatz), ("grube", w_grube), ("ampel", w_ampel),
        ("deko_fels_1", lambda: w_fels(1)), ("deko_fels_2", lambda: w_fels(2)), ("deko_fels_3", lambda: w_fels(3)),         # Build 34 (02.11.2026)
        ("schild", a_schild), ("flak", a_flak), ("lieferrakete", e_lieferrakete),
        ("baum_laub_1", lambda: n_laub(1)), ("baum_laub_2", lambda: n_laub(2)), ("baum_laub_3", lambda: n_laub(3)),
        ("baum_tanne_1", lambda: n_tanne(1)), ("baum_tanne_2", lambda: n_tanne(2)),
        ("busch_1", lambda: n_busch(1)), ("busch_2", lambda: n_busch(2)), ("gras_1", n_gras),
        # Build 38 (02.11.2026): restliche Modelle
        ("riesenmeteor", e_riesenmeteor), ("brocken_1", lambda: e_brocken(1)), ("brocken_2", lambda: e_brocken(2)), ("brocken_3", lambda: e_brocken(3)),
        ("abwehrrakete", a_abwehrrakete), ("schildkuppel", a_schildkuppel),
        ("moos_1", lambda: n_moos(1)), ("moos_2", lambda: n_moos(2)), ("moos_3", lambda: n_moos(3)), ("teich", n_teich),
        ("bohrsockel", a_bohrsockel), ("erzader_fels", e_erzader_neu),
        ("putzi", w_putzi), ("haendlerschiff", e_haendler), ("muellberg", e_muellberg), ("flagge", w_flagge), ("truhe", w_truhe),
        ("wahrzeichen_1", lambda: w_wahrzeichen(1)), ("wahrzeichen_2", lambda: w_wahrzeichen(2)), ("wahrzeichen_3", lambda: w_wahrzeichen(3)),
        ("geysir_schlot", w_geysir),
        ("baum_kristall", lambda: n_baum_spezial("kristall")), ("baum_pilz", lambda: n_baum_spezial("pilz")), ("baum_eis", lambda: n_baum_spezial("eis")),
        ("gras_2", lambda: n_gras_var(2)), ("gras_3", lambda: n_gras_var(3)), ("bestelllager", w_bestelllager),
        ("lieferschiff", e_lieferschiff), ("landeplatz", w_landeplatz),  # Fabian 11.11.
    ]


def build_all(only=None):
    out = []
    for key, fn in all_models():
        if only and key not in only:
            continue
        out.append(fn())
    return out


# ============================ Hilfen für Blender ============================
def _sharp_edges(mesh, angle_deg=40.0):
    """Kanten, an denen die Flächen stärker als angle_deg abknicken (oder offene Kanten)."""
    nrm = []
    for f in mesh.f:
        n = (0.0, 0.0, 0.0)
        for i in range(len(f)):
            p, q = mesh.v[f[i]], mesh.v[f[(i + 1) % len(f)]]
            n = (n[0] + (p[1] - q[1]) * (p[2] + q[2]), n[1] + (p[2] - q[2]) * (p[0] + q[0]), n[2] + (p[0] - q[0]) * (p[1] + q[1]))
        nrm.append(_norm(n))
    ef = {}
    for fi, f in enumerate(mesh.f):
        for i in range(len(f)):
            a, b = f[i], f[(i + 1) % len(f)]
            ef.setdefault((min(a, b), max(a, b)), []).append(fi)
    lim = math.cos(math.radians(angle_deg))
    return {e for e, fs in ef.items() if len(fs) != 2 or _dot(nrm[fs[0]], nrm[fs[1]]) < lim}


def _uvs(mesh):
    """Einfache Würfelprojektion (1 UV-Einheit = 1 m) – reicht für Kachel-Texturen in Unity."""
    out = []
    for f in mesh.f:
        p0, p1, p2 = mesh.v[f[0]], mesh.v[f[1]], mesh.v[f[-1]]
        n = _cross(_sub(p1, p0), _sub(p2, p0))
        ax = max(range(3), key=lambda i: abs(n[i]))
        for i in f:
            p = mesh.v[i]
            out.append((p[1], p[2]) if ax == 0 else (p[0], p[2]) if ax == 1 else (p[0], p[1]))
    return out


def _hex(h):
    h = h.lstrip("#")
    c = [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    return [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c] + [1.0]  # sRGB → linear


def _material(name):
    key = "LC_" + name
    mat = bpy.data.materials.get(key)
    if mat:
        return mat
    col, met, rough, emit, est = MATS[name]
    mat = bpy.data.materials.new(key)
    if bpy.app.version < (5, 0, 0):  # ab Blender 5 immer an
        mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = _hex(col)
    bsdf.inputs["Metallic"].default_value = met
    bsdf.inputs["Roughness"].default_value = rough
    if emit:
        for nm in ("Emission Color", "Emission"):
            if nm in bsdf.inputs:
                bsdf.inputs[nm].default_value = _hex(emit)
                break
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = est
    else:
        # leichte Gebrauchsspuren: Rauheit mit Rauschen variieren (nur in Blender sichtbar)
        tex = nt.nodes.new("ShaderNodeTexNoise")
        tex.inputs["Scale"].default_value = 6.0
        ramp = nt.nodes.new("ShaderNodeMapRange")
        ramp.inputs["To Min"].default_value = max(0.0, rough - 0.12)
        ramp.inputs["To Max"].default_value = min(1.0, rough + 0.12)
        nt.links.new(tex.outputs["Fac"], ramp.inputs["Value"])
        nt.links.new(ramp.outputs["Result"], bsdf.inputs["Roughness"])
        bump = nt.nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = 0.04
        nt.links.new(tex.outputs["Fac"], bump.inputs["Height"])
        nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    if _hq():
        _material_hq(nt, bsdf, name, col)
    mat.diffuse_color = _hex(col)  # Farbe in der Viewport-Ansicht „Solid“
    return mat


ORGANIC = {"laub1", "laub2", "laub3", "tanne", "gras", "trockengras", "rinde", "fels", "felsdkl", "lavastein", "pilz", "pilzstiel", "schwefel", "russ"}


def _set_in(bsdf, names, val):
    for nm in names:
        if nm in bsdf.inputs:
            bsdf.inputs[nm].default_value = val
            return True
    return False


def _material_hq(nt, bsdf, name, col):
    """Fabian 02.11.: schönere Materialien nur in Blender – Farbschwankung und Relief bei Natur/Fels,
    Durchscheinen bei Blättern, Wasser und Eis mit Lichtbrechung."""
    if name in ORGANIC:
        n = nt.nodes.new("ShaderNodeTexNoise"); n.inputs["Scale"].default_value = 14.0 if name != "rinde" else 4.0
        if "Detail" in n.inputs:
            n.inputs["Detail"].default_value = 8.0
        mix = nt.nodes.new("ShaderNodeMixRGB"); c = _hex(col)
        mix.inputs["Color1"].default_value = (c[0] * 0.62, c[1] * 0.62, c[2] * 0.62, 1)
        mix.inputs["Color2"].default_value = (min(1, c[0] * 1.15), min(1, c[1] * 1.15), min(1, c[2] * 1.15), 1)
        nt.links.new(n.outputs["Fac"], mix.inputs["Fac"]); nt.links.new(mix.outputs["Color"], bsdf.inputs["Base Color"])
        if name == "rinde":  # Borke: senkrechte Rillen
            w = nt.nodes.new("ShaderNodeTexWave"); w.inputs["Scale"].default_value = 9.0
            if hasattr(w, "bands_direction"):
                w.bands_direction = "Z"
            src = w.outputs["Fac"]
        else:
            v = nt.nodes.new("ShaderNodeTexVoronoi"); v.inputs["Scale"].default_value = 9.0 if name in ("fels", "felsdkl", "lavastein") else 30.0; src = v.outputs["Distance"]
        b = nt.nodes.new("ShaderNodeBump"); b.inputs["Strength"].default_value = 0.35 if name in ("rinde", "fels", "felsdkl", "lavastein") else 0.18
        nt.links.new(src, b.inputs["Height"]); nt.links.new(b.outputs["Normal"], bsdf.inputs["Normal"])
        if name in ("laub1", "laub2", "laub3", "tanne", "gras", "trockengras"):
            _set_in(bsdf, ("Subsurface Weight", "Subsurface"), 0.12)
            _set_in(bsdf, ("Sheen Weight", "Sheen"), 0.2)
    elif name == "wasser":
        _set_in(bsdf, ("Transmission Weight", "Transmission"), 0.55); _set_in(bsdf, ("IOR",), 1.33); _set_in(bsdf, ("Roughness",), 0.02)
        w = nt.nodes.new("ShaderNodeTexNoise"); w.inputs["Scale"].default_value = 3.0
        b = nt.nodes.new("ShaderNodeBump"); b.inputs["Strength"].default_value = 0.08
        nt.links.new(w.outputs["Fac"], b.inputs["Height"]); nt.links.new(b.outputs["Normal"], bsdf.inputs["Normal"])
    elif name in ("eis", "kometeis", "kristall", "glas"):
        _set_in(bsdf, ("Transmission Weight", "Transmission"), 0.45); _set_in(bsdf, ("IOR",), 1.31 if name != "kristall" else 1.55)
        _set_in(bsdf, ("Coat Weight", "Clearcoat"), 0.5)
    elif name in ("weiss", "hell", "akzent", "akzent2", "rot", "orange"):
        _set_in(bsdf, ("Coat Weight", "Clearcoat"), 0.25)  # lackierte Teile


def _to_object(mesh, coll):
    me = bpy.data.meshes.new(mesh.name)
    me.from_pydata([tuple(v) for v in mesh.v], [], [tuple(f) for f in mesh.f])
    for mname in mesh.mats:
        me.materials.append(_material(mname))
    me.polygons.foreach_set("material_index", mesh.fm)
    me.polygons.foreach_set("use_smooth", [True] * len(mesh.f))
    sharp = _sharp_edges(mesh)
    flags = [((e.vertices[0], e.vertices[1]) if e.vertices[0] < e.vertices[1] else (e.vertices[1], e.vertices[0])) in sharp for e in me.edges]
    me.edges.foreach_set("use_edge_sharp", flags)
    if hasattr(me, "use_auto_smooth"):  # Blender < 4.1
        me.use_auto_smooth = True
        me.auto_smooth_angle = math.radians(40)
    uv = me.uv_layers.new(name="UVMap")
    uv.data.foreach_set("uv", [c for p in _uvs(mesh) for c in p])
    me.validate()
    me.update()
    ob = bpy.data.objects.new(mesh.name, me)
    coll.objects.link(ob)
    if BEVEL:
        bv = ob.modifiers.new("Kanten", "BEVEL")
        bv.width = BEVEL_WIDTH
        bv.segments = BEVEL_SEGMENTS
        bv.limit_method = "ANGLE"
        bv.angle_limit = math.radians(40)
        bv.use_clamp_overlap = True
    return ob


def _collection(name, parent):
    c = bpy.data.collections.get(name)
    if c is None:
        c = bpy.data.collections.new(name)
        parent.children.link(c)
    return c


def _clear_old():
    root = bpy.data.collections.get("LunarCleanup")
    if not root:
        return
    for c in list(root.children_recursive) + [root]:
        for ob in list(c.objects):
            bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes):
        if me.users == 0:
            bpy.data.meshes.remove(me)


def _setup_scene(width, depth):
    sc = bpy.context.scene
    w = sc.world or bpy.data.worlds.new("World")
    sc.world = w
    if bpy.app.version < (5, 0, 0):  # ab Blender 5 immer an
        w.use_nodes = True
    bg = w.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.012, 0.014, 0.02, 1)
        bg.inputs[1].default_value = 1.0
    if not bpy.data.objects.get("LC_Sonne"):
        ld = bpy.data.lights.new("LC_Sonne", "SUN")
        ld.energy = 4.0
        ld.angle = math.radians(2)
        ob = bpy.data.objects.new("LC_Sonne", ld)
        ob.rotation_euler = (math.radians(50), 0, math.radians(35))
        sc.collection.objects.link(ob)
    if not bpy.data.objects.get("LC_Boden"):
        me = bpy.data.meshes.new("LC_Boden")
        s = max(width, depth) + 30
        me.from_pydata([(-10, -s, 0), (s, -s, 0), (s, s, 0), (-10, s, 0)], [], [(0, 1, 2, 3)])
        mat = bpy.data.materials.new("LC_Mondboden")
        if bpy.app.version < (5, 0, 0):  # ab Blender 5 immer an
            mat.use_nodes = True
        b = mat.node_tree.nodes.get("Principled BSDF")
        b.inputs["Base Color"].default_value = (0.09, 0.09, 0.1, 1)
        b.inputs["Roughness"].default_value = 0.95
        me.materials.append(mat)
        ob = bpy.data.objects.new("LC_Boden", me)
        ob.location.z = -0.002
        sc.collection.objects.link(ob)
    if not bpy.data.objects.get("LC_Kamera"):
        cd = bpy.data.cameras.new("LC_Kamera")
        cd.lens = 35
        ob = bpy.data.objects.new("LC_Kamera", cd)
        ob.location = (width / 2, -18, 16)
        ob.rotation_euler = (math.radians(55), 0, 0)
        sc.collection.objects.link(ob)
        sc.camera = ob
    for eng in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            sc.render.engine = eng
            break
        except TypeError:
            pass
    ee = getattr(sc, "eevee", None)
    if ee is not None:
        for attr in ("use_bloom", "use_gtao", "use_soft_shadows"):
            if hasattr(ee, attr):
                setattr(ee, attr, True)


def _export(root, path):
    bpy.ops.object.select_all(action="DESELECT")
    old_loc, old_rot = tuple(root.location), tuple(root.rotation_euler)
    root.location = (0, 0, 0)
    root.rotation_euler = (0, 0, math.pi if FRONT_FLIP else 0)
    obs = [root] + list(root.children_recursive)
    for o in obs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"EMPTY", "MESH"},
                             use_mesh_modifiers=True, mesh_smooth_type="FACE", apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, add_leaf_bones=False, bake_anim=False,
                             path_mode="AUTO")
    root.location, root.rotation_euler = old_loc, old_rot


# ===================== Export ohne Blender: GLB, OBJ, ZIP ===================
# Funktioniert auch außerhalb von Blender:  python3 lunar_models.py --zip Assets.zip
# Achsen: Blender (Z oben, Front −Y)  →  glTF/OBJ (Y oben, Front +Z).
def _conv(p):
    return (p[0], p[2], -p[1])


def _face_normal(pts):
    n = (0.0, 0.0, 0.0)
    for i in range(len(pts)):
        p, q = pts[i], pts[(i + 1) % len(pts)]
        n = (n[0] + (p[1] - q[1]) * (p[2] + q[2]), n[1] + (p[2] - q[2]) * (p[0] + q[0]), n[2] + (p[0] - q[0]) * (p[1] + q[1]))
    return _norm(n)


def _earclip(pts, n):
    """Dreiecke (Indizes in pts) für ein ebenes, auch konkaves Vieleck mit Normale n."""
    if len(pts) == 3:
        return [(0, 1, 2)]
    ax = max(range(3), key=lambda i: abs(n[i]))
    a, b = [(1, 2), (0, 2), (0, 1)][ax]
    P = [(p[a], p[b]) for p in pts]
    sgn = 1.0 if n[ax] >= 0 else -1.0
    if ax == 1:
        sgn = -sgn
    idx = list(range(len(pts)))
    out = []

    def cross(o, p, q):
        return ((P[p][0] - P[o][0]) * (P[q][1] - P[o][1]) - (P[p][1] - P[o][1]) * (P[q][0] - P[o][0])) * sgn

    def inside(t, i0, i1, i2):
        return cross(i0, i1, t) >= -1e-12 and cross(i1, i2, t) >= -1e-12 and cross(i2, i0, t) >= -1e-12

    guard = 0
    while len(idx) > 3 and guard < 10000:
        guard += 1
        found = False
        for k in range(len(idx)):
            i0, i1, i2 = idx[k - 1], idx[k], idx[(k + 1) % len(idx)]
            if cross(i0, i1, i2) <= 1e-12:
                continue
            if any(inside(t, i0, i1, i2) for t in idx if t not in (i0, i1, i2) and P[t] not in (P[i0], P[i1], P[i2])):
                continue
            out.append((i0, i1, i2))
            idx.pop(k)
            found = True
            break
        if not found:  # entartet: Rest als Fächer
            for k in range(1, len(idx) - 1):
                out.append((idx[0], idx[k], idx[k + 1]))
            return out
    out.append(tuple(idx))
    return out


def _tri_data(mesh, angle=40.0):
    """Dreiecke mit Eckennormalen und UVs: [(material, [(Punkt, Normale, UV) ×3]), …] in Modellkoordinaten (ohne Pivot)."""
    fn = [_face_normal([mesh.v[i] for i in f]) for f in mesh.f]
    vf = {}
    for fi, f in enumerate(mesh.f):
        for i in f:
            vf.setdefault(i, []).append(fi)
    lim = math.cos(math.radians(angle))
    uvs = _uvs(mesh)
    out, li = [], 0
    for fi, f in enumerate(mesh.f):
        nf = fn[fi]
        corners = []
        for k, i in enumerate(f):
            s = (0.0, 0.0, 0.0)
            for g in vf[i]:
                if _dot(fn[g], nf) >= lim:
                    s = _add(s, fn[g])
            corners.append((mesh.v[i], _norm(s) if _len(s) > 1e-9 else nf, uvs[li + k], i))
        li += len(f)
        for t in _earclip([mesh.v[i] for i in f], nf):
            out.append((mesh.fm[fi], [corners[t[0]], corners[t[1]], corners[t[2]]]))
    return out


def _srgb(h):
    h = h.lstrip("#")
    return [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]


def _glb(mdl):
    import json
    import struct
    buf = bytearray()
    views, accs = [], []

    def view(data, target):
        while len(buf) % 4:
            buf.append(0)
        views.append({"buffer": 0, "byteOffset": len(buf), "byteLength": len(data), "target": target})
        buf.extend(data)
        return len(views) - 1

    mats, mat_idx = [], {}

    def material(name):
        if name in mat_idx:
            return mat_idx[name]
        col, met, rough, emit, est = MATS[name]
        lin = _hex(col)
        m = {"name": "LC_" + name, "pbrMetallicRoughness": {"baseColorFactor": [round(c, 5) for c in lin], "metallicFactor": met, "roughnessFactor": rough}}
        if emit:
            m["emissiveFactor"] = [round(c, 5) for c in _hex(emit)[:3]]
            if est > 1.0:
                m["extensions"] = {"KHR_materials_emissive_strength": {"emissiveStrength": est}}
        mats.append(m)
        mat_idx[name] = len(mats) - 1
        return mat_idx[name]

    nodes = [{"name": mdl.key, "children": []}]
    meshes = []
    for me in mdl.meshes:
        tris = _tri_data(me)
        prims = []
        for mi in sorted(set(t[0] for t in tris)):
            key_i, pos, nrm, uv, ind = {}, [], [], [], []
            for _, cs in (t for t in tris if t[0] == mi):
                for (p, n, u, vi) in cs:
                    k = (vi, round(n[0], 3), round(n[1], 3), round(n[2], 3), round(u[0], 4), round(u[1], 4))
                    if k not in key_i:
                        key_i[k] = len(pos)
                        pos.append(_conv(p))
                        nrm.append(_conv(n))
                        uv.append((u[0], 1.0 - u[1]))
                    ind.append(key_i[k])
            fmt = "<%dH" % len(ind) if len(pos) < 65536 else "<%dI" % len(ind)
            vp = view(struct.pack("<%df" % (3 * len(pos)), *[c for p in pos for c in p]), 34962)
            vn = view(struct.pack("<%df" % (3 * len(nrm)), *[c for p in nrm for c in p]), 34962)
            vt = view(struct.pack("<%df" % (2 * len(uv)), *[c for p in uv for c in p]), 34962)
            vi_ = view(struct.pack(fmt, *ind), 34963)
            mn = [min(p[i] for p in pos) for i in range(3)]
            mx = [max(p[i] for p in pos) for i in range(3)]
            accs.append({"bufferView": vp, "componentType": 5126, "count": len(pos), "type": "VEC3", "min": mn, "max": mx}); ap = len(accs) - 1
            accs.append({"bufferView": vn, "componentType": 5126, "count": len(nrm), "type": "VEC3"}); an = len(accs) - 1
            accs.append({"bufferView": vt, "componentType": 5126, "count": len(uv), "type": "VEC2"}); at = len(accs) - 1
            accs.append({"bufferView": vi_, "componentType": 5123 if len(pos) < 65536 else 5125, "count": len(ind), "type": "SCALAR"}); ai = len(accs) - 1
            prims.append({"attributes": {"POSITION": ap, "NORMAL": an, "TEXCOORD_0": at}, "indices": ai, "material": material(me.mats[mi])})
        meshes.append({"name": mdl.key + "_" + me.name, "primitives": prims})
        nodes.append({"name": mdl.key + "_" + me.name, "mesh": len(meshes) - 1, "translation": list(_conv(me.pivot))})
        nodes[0]["children"].append(len(nodes) - 1)
    gl = {"asset": {"version": "2.0", "generator": "Lunar Cleanup Tycoon – lunar_models.py"},
          "scene": 0, "scenes": [{"name": mdl.name, "nodes": [0]}], "nodes": nodes, "meshes": meshes,
          "materials": mats, "accessors": accs, "bufferViews": views, "buffers": [{"byteLength": len(buf)}],
          "extras": {"Spielname": mdl.name, "Gruppe": mdl.group, "fp": mdl.fp}}
    if any("extensions" in m for m in mats):
        gl["extensionsUsed"] = ["KHR_materials_emissive_strength"]
    js = json.dumps(gl, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    while len(js) % 4:
        js += b" "
    while len(buf) % 4:
        buf.append(0)
    total = 12 + 8 + len(js) + 8 + len(buf)
    return struct.pack("<III", 0x46546C67, 2, total) + struct.pack("<II", len(js), 0x4E4F534A) + js + struct.pack("<II", len(buf), 0x004E4942) + bytes(buf)


def _obj(mdl):
    lines = ["# Lunar Cleanup Tycoon – %s (%s)" % (mdl.name, mdl.key), "# Y oben, Vorderseite +Z, 1 Einheit = 1 m", "mtllib materialien.mtl"]
    nv = nt = nn = 0
    for me in mdl.meshes:
        lines.append("o %s_%s" % (mdl.key, me.name))
        tris = _tri_data(me)
        vmap = {}
        for i, v in enumerate(me.v):
            p = _conv(_add(v, me.pivot))
            lines.append("v %.5f %.5f %.5f" % p)
        for mi in sorted(set(t[0] for t in tris)):
            lines.append("usemtl LC_" + me.mats[mi])
            for _, cs in (t for t in tris if t[0] == mi):
                fl = []
                for (p, n, u, vi) in cs:
                    cn = _conv(n)
                    lines.append("vt %.4f %.4f" % (u[0], u[1]))
                    lines.append("vn %.4f %.4f %.4f" % cn)
                    nt += 1
                    nn += 1
                    fl.append("%d/%d/%d" % (nv + vi + 1, nt, nn))
                lines.append("f " + " ".join(fl))
        nv += len(me.v)
    return "\n".join(lines) + "\n"


def _mtl(names):
    out = ["# Lunar Cleanup Tycoon – Materialien (PBR-Werte in Pr/Pm)"]
    for name in sorted(names):
        col, met, rough, emit, est = MATS[name]
        c = _srgb(col)
        out += ["", "newmtl LC_" + name, "Kd %.4f %.4f %.4f" % tuple(c), "Ka 0 0 0", "Ks 0.5 0.5 0.5",
                "Ns %d" % int((1 - rough) * 900 + 10), "Pr %.2f" % rough, "Pm %.2f" % met, "d 1", "illum 2"]
        if emit:
            e = _srgb(emit)
            k = min(1.0, est / 4.0) if est else 0
            out.append("Ke %.4f %.4f %.4f" % (e[0] * k, e[1] * k, e[2] * k))
    return "\n".join(out) + "\n"


README = """Lunar Cleanup Tycoon – eigene 3D-Assets
========================================

Alles in diesem Paket wurde mit dem Skript blender/lunar_models.py erzeugt
(prozedural, keine fremden Modelle oder Texturen). Du darfst es in deinem
Spiel frei verwenden, ändern und weitergeben.

Ordner
------
glb/   Jedes Modell als glTF-Binärdatei (.glb) – mit Materialien, Leuchtfarben
       und beweglichen Teilen als eigene Knoten. Empfohlen.
       Unity: Package Manager → „glTFast“ (com.unity.cloud.gltfast) installieren,
       dann .glb einfach in den Assets-Ordner ziehen.
obj/   Dieselben Modelle als .obj + materialien.mtl (öffnet jedes 3D-Programm,
       Unity ohne Zusatzpaket). Bewegliche Teile sind eigene Objekte, aber
       ohne eigenen Drehpunkt.
fbx/   (nur wenn das Paket in Blender gebaut wurde) FBX für Unity.
blender/  Das Skript selbst. In Blender: Reiter „Scripting“ → Öffnen → ▶.
vorschau/ Übersichtsbilder aller Modelle.
manifest.json  Liste aller Modelle: Name, Gruppe, Dateien, Teile, Drehpunkte, Größe.

Maße und Achsen
---------------
1 Einheit = 1 m. Ursprung jedes Modells: Mitte am Boden.
glb/obj: Y nach oben, Vorderseite zeigt nach +Z (wie Unity „forward“).
Gebäude passen in ihren Grundkreis (Radius fp aus gamedata.json).
Förderbänder: laufen entlang +Z, Start bei z = 0, Lauffläche 0,55 m hoch,
0,84 m breit (wie im Spiel). Kurven biegen nach rechts – für links spiegeln.
Waren liegen mit der Unterseite auf 0, die lange Seite längs zum Band.

Bewegliche Teile (Namen)
------------------------
<id>_Koerper   alles Feste
<id>_Rakete    Rakete der Verkaufsstation (Start/Landung)
<id>_Kolben    Pressenkolben (auf/ab)
<id>_Dreh      Drehteil: Schüssel, Bohrgestänge, Schaufelrad, Kranhaken, Radar, Roboterarm
<id>_Rad_0..n  Räder (drehen um die Querachse)
<id>_Werkzeug  Bürste, Düse, Fräse, Pflug oder Pumpenarm vorne
<id>_Rotor_0..3 Drohnenrotoren (drehen um die Hochachse)
<id>_Ladung    Drohnenkorb bzw. Greifer
<id>_Licht     Ampel-Kugel (Farbe wechselt im Spiel)

Gruppen
-------
Gebaeude, Anlagen, Fahrzeuge, Drohnen, Foerderband, Produkte (alle 57 Waren),
Schmutz (9 Schmutzarten), Ereignisse (Wracks, Komet, Einschlag, Meteorit,
Frachter, Erzader), Welt (Leuchtkristall, Laterne, Astronaut, Sammelwagen,
Außenlager, Bauplatz, Grube, Pipeline, Ampel, Deko-Felsen, Krater).
"""


def _own_source():
    try:
        with open(__file__, "r", encoding="utf-8") as fh:
            return fh.read()
    except Exception:
        if HAVE_BPY:
            for t in bpy.data.texts:
                if t.name.endswith("lunar_models.py"):
                    return t.as_string()
    return None


def make_package(zip_path, fbx_dir=None, extra=None, only=None):
    """Baut alle Modelle und packt GLB, OBJ, (FBX), Manifest, Anleitung und Skript in eine ZIP."""
    import json
    import zipfile
    models = build_all(only)
    man = {"spiel": "Lunar Cleanup Tycoon", "einheit": "Meter", "achsen": "Y oben, Vorderseite +Z (glb/obj)", "modelle": []}
    used = set()
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for mdl in models:
            z.writestr("glb/%s/%s.glb" % (mdl.group, mdl.key), _glb(mdl))
            z.writestr("obj/%s/%s.obj" % (mdl.group, mdl.key), _obj(mdl))
            files = ["glb/%s/%s.glb" % (mdl.group, mdl.key), "obj/%s/%s.obj" % (mdl.group, mdl.key)]
            if fbx_dir and os.path.exists(os.path.join(fbx_dir, mdl.key + ".fbx")):
                z.write(os.path.join(fbx_dir, mdl.key + ".fbx"), "fbx/%s/%s.fbx" % (mdl.group, mdl.key))
                files.append("fbx/%s/%s.fbx" % (mdl.group, mdl.key))
            allp = [_add(v, me.pivot) for me in mdl.meshes for v in me.v]
            mn = [round(min(p[i] for p in allp), 3) for i in range(3)]
            mx = [round(max(p[i] for p in allp), 3) for i in range(3)]
            for me in mdl.meshes:
                used.update(me.mats)
            man["modelle"].append({
                "id": mdl.key, "name": mdl.name, "gruppe": mdl.group, "fp": mdl.fp, "dateien": files,
                "groesse_m": {"breite": round(mx[0] - mn[0], 3), "tiefe": round(mx[1] - mn[1], 3), "hoehe": round(mx[2] - mn[2], 3)},
                "dreiecke": sum(sum(len(f) - 2 for f in me.f) for me in mdl.meshes),
                "teile": [{"name": mdl.key + "_" + me.name, "drehpunkt_yoben": [round(c, 3) for c in _conv(me.pivot)]} for me in mdl.meshes]})
        mtl = _mtl(used)
        for g in sorted(set(m.group for m in models)):
            z.writestr("obj/%s/materialien.mtl" % g, mtl)
        z.writestr("manifest.json", json.dumps(man, ensure_ascii=False, indent=1))
        z.writestr("LIESMICH.txt", README)
        src = _own_source()
        if src:
            z.writestr("blender/lunar_models.py", src)
        for arc, path in (extra or {}).items():
            z.write(path, arc)
    return len(models)


GROUPS = [("Gebaeude", 13.0, 10), ("Anlagen", 9.0, 10), ("Fahrzeuge", 8.0, 12), ("Drohnen", 7.0, 10), ("Foerderband", 6.0, 16),
          ("Produkte", 1.2, 20), ("Schmutz", 3.0, 20), ("Ereignisse", 10.0, 10), ("Welt", 7.0, 12), ("Abwehr", 8.0, 10), ("Natur", 5.0, 12)]


def run():
    if CLEAR_OLD:
        _clear_old()
    top = _collection("LunarCleanup", bpy.context.scene.collection)
    models = build_all(ONLY or (NEW_KEYS if NUR_NEUE else None))
    gi = {g: i for i, (g, _, _) in enumerate(GROUPS)}
    count = {g: 0 for g, _, _ in GROUPS}
    for mdl in models:
        count[mdl.group] = count.get(mdl.group, 0) + 1
    # Startzeile jeder Gruppe (Gruppen untereinander, lange Gruppen umbrechen)
    y0, ystart = 0.0, {}
    for g, sp, per in GROUPS:
        ystart[g] = y0
        rows_g = max(1, -(-count.get(g, 0) // per))
        y0 += rows_g * sp + 4.0
    seen = {g: 0 for g, _, _ in GROUPS}
    roots = []
    for mdl in models:
        g = _collection("LC_" + mdl.group, top)
        root = bpy.data.objects.new(mdl.key, None)
        root.empty_display_type = "PLAIN_AXES"
        root.empty_display_size = 0.5
        root["Spielname"] = mdl.name
        if mdl.fp:
            root["fp"] = mdl.fp
        g.objects.link(root)
        for mesh in mdl.meshes:
            ob = _to_object(mesh, g)
            if _hq() and mdl.key in SOFT_MODELS:  # Felsen und Moos weich gerundet
                sub = ob.modifiers.new("Weich", "SUBSURF"); sub.levels = 1; sub.render_levels = 2
            ob.name = mdl.key + "_" + mesh.name
            ob.parent = root
            ob.location = mesh.pivot
        _, sp, per = GROUPS[gi[mdl.group]]
        i = seen[mdl.group]
        seen[mdl.group] += 1
        root.location = ((i % per) * sp, ystart[mdl.group] + (i // per) * sp, 0)
        roots.append(root)
    if SETUP_SCENE:
        _setup_scene(max(per * sp for _, sp, per in GROUPS), y0)
    if EXPORT:
        d = bpy.path.abspath(EXPORT_DIR)
        os.makedirs(d, exist_ok=True)
        for r in roots:
            _export(r, os.path.join(d, r.name + ".fbx"))
        zp = os.path.join(os.path.dirname(os.path.normpath(d)), "LunarCleanup_Assets.zip")
        make_package(zp, fbx_dir=d, only=ONLY)
        print("Lunar Cleanup: %d Modelle exportiert nach %s, Paket: %s" % (len(roots), d, zp))
    tris = sum(sum(len(f) - 2 for f in me.f) for mdl in models for me in mdl.meshes)
    print("Lunar Cleanup: %d Modelle gebaut, %d Dreiecke vor Bevel" % (len(models), tris))


if HAVE_BPY and __name__ == "__main__":
    run()
elif __name__ == "__main__":
    import sys
    if "--zip" in sys.argv:
        out = sys.argv[sys.argv.index("--zip") + 1]
        n = make_package(out)
        print("Lunar Cleanup: %d Modelle in %s gepackt" % (n, out))
    else:
        print("Aufruf: in Blender ausführen, oder: python3 lunar_models.py --zip LunarCleanup_Assets.zip")
