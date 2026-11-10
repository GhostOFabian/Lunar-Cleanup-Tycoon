"""Produktionsmodul: feste Anordnung nach dem Materialfluss (Flussrichtung +x), wird gedreht/gespiegelt in die Basis gesetzt.
Danach verlegt der Planer die Bänder (keine fest vorgegebenen Wege), die übrigen Gebäude werden drumherum gepackt."""
import math, itertools, time, json
from planner import *
MASKS = json.load(open(__import__('os').path.join(__import__('os').path.dirname(__file__), 'basemask.json')))

# Mittelpunkte im Modul (m). Flächen: Eingang/Ausgang werden vom Planer nach Lage gewählt.
MOD = {'u1': (0, -13), 'roh': (0, 0), 'presse': (14, 9), 'raff': (14, -9), 'schmelze': (28, 13), 'prod': (42, 0), 'sell': (42, 13)}
DOCK = (-6, -21, 6, -18)
# Anschlussseiten im Modul (Eingang, Ausgang): 0=+x 1=+y 2=-x 3=-y
FACES = {'u1': (None, 1), 'roh': (3, 0), 'presse': (2, 0), 'raff': (2, 0), 'schmelze': (2, 3), 'prod': (2, 1), 'sell': (3, None)}
def tface(k, rot, mir):
    if k is None: return None
    if mir and k in (1, 3): k = (k + 2) % 4
    return (k + rot) % 4     # Andockfläche der Fahrzeuge vor u1 (Modulkoordinaten)

def tf(p, rot, mir):
    x, y = p
    if mir: y = -y
    for _ in range(rot): x, y = -y, x
    return x, y

def rect_in(mask, x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if not (-44 <= x <= 44 and -44 <= y <= 44) or mask[(y + 44) * 89 + (x + 44)] != "1": return False
    return True

def place_module(blds, mask, orig):
    """Beste Lage des Moduls: alles in der Basis, Modulgebäude nah an ihren alten Plätzen."""
    cands = []
    hs = {k: blds[k].hs for k in MOD}
    for rot in range(4):
        for mir in (0, 1):
            pts = {k: tf(v, rot, mir) for k, v in MOD.items()}
            d0 = tf(DOCK[:2], rot, mir); d1 = tf(DOCK[2:], rot, mir)
            for tx in range(-36, 37):
                for ty in range(-36, 37):
                    ok = True
                    for k, (x, y) in pts.items():
                        X, Y = x + tx, y + ty; h = hs[k] + 1      # Gebäude + 1 m (Bänder prüft die Wegsuche)
                        if not (mask[(Y + 44) * 89 + (X + 44)] == '1' if -44 <= X <= 44 and -44 <= Y <= 44 else False): ok = False; break
                        if not rect_in(mask, X - h, Y - h, X + h, Y + h): ok = False; break
                    if not ok: continue
                    sc = sum(math.hypot(pts[k][0] + tx - orig[k][0], pts[k][1] + ty - orig[k][1]) for k in MOD)
                    cands.append((sc, rot, mir, tx, ty))
    cands.sort()
    return cands

def build(key, tries=400):
    cands = place_module(load(key)[0], MASKS[key], {k: (b.x, b.y) for k, b in load(key)[0].items()})
    if not cands: return None, 'Modul passt nicht in die Basis'
    last = None; seen = 0
    for c in cands:
        P, msg = build_at(key, c)
        if msg.startswith('ok'): return P, msg + ' (Lage %d)' % seen
        last = (P, msg); seen += 1
        if seen >= tries: break
    return last

def build_at(key, pm, gap_other=1, gap_mod=2, area_r=28):
    blds, belts, L, _ = load(key)
    orig = {k: (b.x, b.y) for k, b in blds.items()}
    mask = MASKS[key]
    sc, rot, mir, tx, ty = pm
    for k, v in MOD.items():
        x, y = tf(v, rot, mir); blds[k].x, blds[k].y = x + tx, y + ty
    d0 = tf(DOCK[:2], rot, mir); d1 = tf(DOCK[2:], rot, mir)
    dock = (min(d0[0], d1[0]) + tx, min(d0[1], d1[1]) + ty, max(d0[0], d1[0]) + tx, max(d0[1], d1[1]) + ty)
    # Erst nur die Modulgebäude planen und verlegen
    mod = {k: blds[k] for k in MOD}
    P = Plan(mod, belts, [dock], area_r); P.mask = mask; P.fixed = {k: (tface(a, rot, mir), tface(b, rot, mir)) for k, (a, b) in FACES.items()}; P.ports()
    paths, nc, it = P.route_seq()
    if nc: return P, 'Modul: %d Konflikte' % nc
    # Reservierte Zellen: Bänder + Halo, Vorplätze, Andockfläche
    res = set()
    for lid, p in paths.items():
        res |= P.halo(p)
        if lid in P.merge: res |= P.halo(P.connector(p[-1], P.merge[lid][1]))
    ux, uy = blds['u1'].x, blds['u1'].y; cx, cy = (dock[0] + dock[2]) / 2, (dock[1] + dock[3]) / 2
    ox, oy = (1 if cx > ux + .5 else -1 if cx < ux - .5 else 0), (1 if cy > uy + .5 else -1 if cy < uy - .5 else 0)
    for k_ in range(0, 5):   # Andockfläche + 4 m Zufahrt nach außen
        for x in range(dock[0] - 1, dock[2] + 2):
            for y in range(dock[1] - 1, dock[3] + 2): res.add((x + ox * k_, y + oy * k_))
    # übrige Gebäude packen: größte zuerst, nächster freier Platz zum alten Ort
    others = sorted([k for k in blds if k not in MOD], key=lambda k: -blds[k].hs)
    placed = dict(mod)
    occ = set(res)
    def bfree(x, y):
        return -44 <= x <= 44 and -44 <= y <= 44 and mask[(y + 44) * 89 + (x + 44)] == '1' and (x, y) not in occ
    for k in others:
        b = blds[k]; h = b.hs; ox, oy = orig[k]; best = None
        for y in range(-44 + h, 45 - h):
            for x in range(-44 + h, 45 - h):
                if not rect_in(mask, x - h, y - h, x + h, y + h): continue
                bad = False
                for o in placed.values():
                    g = gap_mod if o.id in MOD else gap_other
                    if abs(x - o.x) < h + o.hs + g + 1 and abs(y - o.y) < h + o.hs + g + 1: bad = True; break
                if bad: continue
                for cx in range(x - h - 1, x + h + 2):
                    for cy in range(y - h - 1, y + h + 2):
                        if (cx, cy) in res: bad = True; break
                    if bad: break
                if bad: continue
                # Kontakt: belegte/äußere Zellen im Ring 2 m um das Gebäude
                ring = 0; r = h + 2
                for cx in range(x - r, x + r + 1):
                    for cy in (y - r, y + r):
                        if not bfree(cx, cy): ring += 1
                for cy in range(y - r + 1, y + r):
                    for cx in (x - r, x + r):
                        if not bfree(cx, cy): ring += 1
                sc = (-ring, abs(x - ox) + abs(y - oy))
                if best is None or sc < best[0]: best = (sc, x, y)
        if not best: return P, 'kein Platz für ' + k
        b.x, b.y = best[1], best[2]; placed[k] = b
        for cx in range(b.x - h - 1, b.x + h + 2):
            for cy in range(b.y - h - 1, b.y + h + 2): occ.add((cx, cy))
    # Gesamtprüfung: alles noch einmal mit allen Gebäuden verlegen
    F = Plan(blds, belts, [dock], area_r); F.mask = mask; F.fixed = P.fixed; F.ports(); paths, nc, it = F.route_seq()
    nc = nc + len(F.conflicts(paths))
    return F, ('ok' if nc == 0 else '%d Konflikte' % nc) + ' (Iter %d)' % it
