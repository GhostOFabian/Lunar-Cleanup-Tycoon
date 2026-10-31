# -*- coding: utf-8 -*-
"""
Lunar Cleanup Tycoon – Modellbaukasten für Blender
===================================================

Baut alle Gebäude, Fahrzeuge, Drohnen, Bohrturm/Erzbagger und die Rakete des
Spiels als detaillierte Low-Poly-Modelle (Mobile-tauglich, ca. 2–12 k Dreiecke
pro Modell) mit PBR-Materialien.

So benutzt du es
----------------
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
ACCENT = "#ffbf4d"                # Akzentfarbe (Mond: Orange/Gelb wie im Spiel)
ACCENT2 = "#58cdb4"               # zweite Akzentfarbe (Türkis)
BEVEL = True                      # weiche Kanten (Bevel-Modifier)
BEVEL_WIDTH = 0.025               # m
BEVEL_SEGMENTS = 2                # 1 = sparsamer fürs Handy
SPACING = 13.0                    # Abstand der Modelle in der Szene
SETUP_SCENE = True                # Licht, Boden, Kamera für schöne Vorschau
FRONT_FLIP = False                # Modelle beim Export um 180° drehen
CLEAR_OLD = True                  # vorherige LunarCleanup-Modelle löschen

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
        prof = []
        a_start = math.asin(max(-1.0, min(1.0, zcut))) if zcut > -1 else -math.pi / 2
        for i in range(rings + 1):
            a = a_start + (math.pi / 2 - a_start) * i / rings
            prof.append((r * math.cos(a) if i < rings else 0.0, r * math.sin(a)))
        if zcut <= -1:
            prof[0] = (0.0, -r)
        return self.lathe(mat, pos, prof, seg)

    def torus(self, mat, pos, R0, r, seg=24, seg2=8, rot=None):
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


# ============================== Verzeichnis =================================
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
    mat.diffuse_color = _hex(col)  # Farbe in der Viewport-Ansicht „Solid“
    return mat


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


def run():
    if CLEAR_OLD:
        _clear_old()
    top = _collection("LunarCleanup", bpy.context.scene.collection)
    groups = {}
    models = build_all(ONLY)
    rows = ["Gebaeude", "Anlagen", "Fahrzeuge", "Drohnen"]
    col_i = {g: 0 for g in rows}
    roots = []
    for mdl in models:
        g = _collection("LC_" + mdl.group, top)
        groups[mdl.group] = g
        root = bpy.data.objects.new(mdl.key, None)
        root.empty_display_type = "PLAIN_AXES"
        root.empty_display_size = 0.5
        root["Spielname"] = mdl.name
        if mdl.fp:
            root["fp"] = mdl.fp
        g.objects.link(root)
        for mesh in mdl.meshes:
            ob = _to_object(mesh, g)
            ob.name = mdl.key + "_" + mesh.name
            ob.parent = root
            ob.location = mesh.pivot
        i = col_i[mdl.group]
        col_i[mdl.group] += 1
        root.location = (i * SPACING, rows.index(mdl.group) * SPACING, 0)
        roots.append(root)
    if SETUP_SCENE:
        _setup_scene(max(col_i.values()) * SPACING, len(rows) * SPACING)
    if EXPORT:
        d = bpy.path.abspath(EXPORT_DIR)
        os.makedirs(d, exist_ok=True)
        for r in roots:
            _export(r, os.path.join(d, r.name + ".fbx"))
        print("Lunar Cleanup: %d Modelle exportiert nach %s" % (len(roots), d))
    tris = sum(sum(len(f) - 2 for f in me.f) for mdl in models for me in mdl.meshes)
    print("Lunar Cleanup: %d Modelle gebaut, %d Dreiecke vor Bevel" % (len(models), tris))


if HAVE_BPY and __name__ == "__main__":
    run()
