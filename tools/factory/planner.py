"""Prototyp des Fabrik-Planers (wird danach 1:1 nach C# übertragen).
Raster 1 m, Zellmitten auf ganzen Metern. Bänder rechtwinklig, Kreuzen verboten, eine Höhe.
"""
import heapq, itertools, json, math, sys, random

EXT = 44                    # Raster ±44 m
N = 2 * EXT + 1
DX = [(1, 0), (0, 1), (-1, 0), (0, -1)]   # 0=+x 1=+y 2=-x 3=-y
MINRUN = 2                  # mind. 2 m gerade zwischen zwei Kurven (Radius 1 m)
CLR = 2                     # Mittellinien verschiedener Bänder mind. 2 m auseinander (Chebyshev)
ML = 3                      # Länge eines Y-Stücks
SLOT = 2                    # Abstand der Anschlüsse an einer Fläche

def idx(x, y): return (y + EXT) * N + (x + EXT)
def inside(x, y): return -EXT <= x <= EXT and -EXT <= y <= EXT

class Bld:
    def __init__(s, id, x, y, hs):
        s.id, s.x, s.y, s.hs = id, x, y, hs

import math as _m
REAL = {'energy': 7.6, 'geo': 3.6, 'hangar': 3.8, 'hq': 4.1, 'lifter': 4.4, 'port': 4.4, 'presse': 2.6, 'prod': 3.6, 'raff': 2.6, 'reactor': 3.6, 'research': 3.4, 'roh': 3.9, 'rtg': 2.5, 'schmelze': 2.5, 'sell': 4.2, 'u1': 4.0, 'werkstatt': 4.6}
def half_id(id, fp): return max(1, int(round(REAL[id] - .5))) if id in REAL else max(1, int(round(fp * .9)))
def half(fp): return max(1, int(round(fp * .9)))

def dir_of(dx, dy):
    if abs(dx) >= abs(dy): return 0 if dx >= 0 else 2
    return 1 if dy >= 0 else 3

def faces_by(dx, dy):
    sc = [(dx * DX[k][0] + dy * DX[k][1], k) for k in range(4)]
    return [k for _, k in sorted(sc, reverse=True)]

class Plan:
    def __init__(s, blds, belts, docks, area_r):
        s.B = blds; s.belts = belts; s.docks = docks; s.area_r = area_r
        s.err = []; s.no_face = {}; s.fixed = {}; s.mask = None

    # ---------- Hindernisse ----------
    def obstacles(s):
        blk = bytearray(N * N)
        for y in range(-EXT, EXT + 1):
            for x in range(-EXT, EXT + 1):
                if not s.inb(x, y): blk[idx(x, y)] = 1
        for b in s.B.values():
            r = b.hs + 1
            for y in range(b.y - r, b.y + r + 1):
                for x in range(b.x - r, b.x + r + 1):
                    if inside(x, y): blk[idx(x, y)] = 1
        for (x0, y0, x1, y1) in s.docks:
            for y in range(y0, y1 + 1):
                for x in range(x0, x1 + 1):
                    if inside(x, y): blk[idx(x, y)] = 1
        return blk

    def inb(s, x, y):
        if s.mask is None: return x * x + y * y <= s.area_r ** 2
        if not (-44 <= x <= 44 and -44 <= y <= 44): return False
        return s.mask[(y + 44) * 89 + (x + 44)] == '1'

    # ---------- Platzbedarf: Produktionsgebäude auseinanderschieben ----------
    def spread(s, gap_prod=6, gap=3, its=300, gap_mix=5):
        prod = set(); 
        for l in s.belts: prod.add(l['from']); prod.add(l['to'])
        ids = list(s.B.keys())
        for it in range(its):
            moved = False
            for a, b in itertools.combinations(ids, 2):
                A, B_ = s.B[a], s.B[b]; g = gap_prod if (a in prod and b in prod) else gap_mix if (a in prod or b in prod) else gap
                need = A.hs + B_.hs + g + 1
                dx, dy = B_.x - A.x, B_.y - A.y
                if max(abs(dx), abs(dy)) >= need: continue
                # entlang der Achse mit dem kleineren Fehlbetrag auseinander
                if abs(dx) >= abs(dy): sh = need - abs(dx); ax = 0; sg = 1 if dx >= 0 else -1
                else: sh = need - abs(dy); ax = 1; sg = 1 if dy >= 0 else -1
                ka = sh // 2; kb = sh - ka
                if a not in prod and b in prod: ka, kb = sh, 0      # Nebengebäude weichen
                if b not in prod and a in prod: ka, kb = 0, sh
                if ax == 0: A.x -= sg * ka; B_.x += sg * kb
                else: A.y -= sg * ka; B_.y += sg * kb
                moved = True
            # in der Basisfläche halten
            for b in s.B.values():
                r = math.hypot(b.x, b.y) + b.hs + 1
                if r > s.area_r: f = (s.area_r - b.hs - 1) / max(1, math.hypot(b.x, b.y)); b.x = int(round(b.x * f)); b.y = int(round(b.y * f)); moved = True
            if not moved: return it
        return -1
    def spread_grow(s):
        for k in range(6):
            r = s.spread()
            if r >= 0: return r, s.area_r
            s.area_r += 2
        return -1, s.area_r

    def apron_free(s, b, k, nports):
        """Vorplatz vor einer Fläche: 4 m tief, so breit wie die Anschlüsse plus 1 m, frei von anderen Gebäuden und Andockflächen."""
        n = DX[k]; t = (-n[1], n[0]); w = (nports - 1) * SLOT // 2 + 1 + 1
        for dep in range(b.hs + 1, b.hs + 4):
            for o in range(-w, w + 1):
                x = b.x + n[0] * dep + t[0] * o; y = b.y + n[1] * dep + t[1] * o
                if not s.inb(x, y): return False
                for c in s.B.values():
                    if c is b: continue
                    if abs(x - c.x) <= c.hs + 1 and abs(y - c.y) <= c.hs + 1: return False
                for (x0, y0, x1, y1) in s.docks:
                    if x0 <= x <= x1 and y0 <= y <= y1: return False
        return True
    def pick_face(s, b, order, nports):
        for k in order:
            if s.apron_free(b, k, nports): return k
        s.err.append(b.id + ': keine freie Seite für Anschlüsse'); return order[0]

    # ---------- Anschlüsse ----------
    def ports(s):
        ins, outs = {}, {}
        for bl in s.belts: outs.setdefault(bl['from'], []).append(bl); ins.setdefault(bl['to'], []).append(bl)
        s.face_in, s.face_out = {}, {}
        for id, b in s.B.items():
            if id not in ins and id not in outs: continue
            fi = fo = None
            if id in s.fixed:
                fi, fo = s.fixed[id]; s.face_in[id], s.face_out[id] = fi, fo; continue
            if id in ins:
                sx = sum(s.B[l['from']].x - b.x for l in ins[id]); sy = sum(s.B[l['from']].y - b.y for l in ins[id])
                fi = s.pick_face(b, faces_by(sx, sy), 1)
            if id in outs:
                tx = sum(s.B[l['to']].x - b.x for l in outs[id]); ty = sum(s.B[l['to']].y - b.y for l in outs[id])
                fo = s.pick_face(b, [k for k in faces_by(tx, ty) if k != fi], len(outs[id]))
            s.face_in[id], s.face_out[id] = fi, fo
        s.start, s.goal = {}, {}
        for id, L in outs.items():
            b = s.B[id]; k = s.face_out[id]; n = DX[k]; t = (-n[1], n[0])
            L = sorted(L, key=lambda l: (s.B[l['to']].x - b.x) * t[0] + (s.B[l['to']].y - b.y) * t[1])
            m = len(L)
            for i, l in enumerate(L):
                o = (2 * i - (m - 1)) * SLOT // 2 if m % 2 else (2 * i - (m - 1)) * SLOT // 2
                s.start[l['id']] = (b.x + n[0] * (b.hs + 1) + t[0] * o, b.y + n[1] * (b.hs + 1) + t[1] * o, k)
        for id in ins:
            b = s.B[id]; k = s.face_in[id]; n = DX[k]
            s.goal[id] = (b.x + n[0] * (b.hs + 1), b.y + n[1] * (b.hs + 1), (k + 2) % 4)
        s.ins, s.outs = ins, outs

    # ---------- Wegsuche ----------
    def astar(s, sx, sy, sd, goals, blk, cost_at, own, mz_trunk=None, need_run=MINRUN):
        """goals: {(x,y): dir}. Start: Zelle (sx,sy) mit Richtung sd, die ersten MINRUN Zellen gerade."""
        INF = 1e18; best = {}
        st = (sx, sy, sd, 0); best[st] = 0; prev = {st: None}
        gl = list(goals.items())
        def h(x, y): return min(abs(x - gx) + abs(y - gy) for (gx, gy), _ in gl)
        pq = [(h(sx, sy), 0, st)]
        found = None; n_exp = 0
        while pq:
            f, g, st = heapq.heappop(pq)
            if g > best.get(st, INF): continue
            x, y, d, run = st
            n_exp += 1
            if n_exp > 400000: break
            if (x, y) in goals and goals[(x, y)] == d and run >= need_run: found = st; break
            for nd in (d, (d + 1) % 4, (d + 3) % 4):
                if nd != d and run < MINRUN: continue
                nx, ny = x + DX[nd][0], y + DX[nd][1]
                if not inside(nx, ny): continue
                c = idx(nx, ny)
                if blk[c] and (nx, ny) not in goals and (nx, ny) not in own: continue
                ng = g + 1 + (4 if nd != d else 0) + cost_at(nx, ny)
                nr = min(need_run, run + 1) if nd == d else 1
                ns = (nx, ny, nd, nr)
                if ng < best.get(ns, INF):
                    best[ns] = ng; prev[ns] = st; heapq.heappush(pq, (ng + h(nx, ny), ng, ns))
        if not found: return None, INF
        path = []; st = found
        while st: path.append((st[0], st[1])); st = prev[st]
        return path[::-1], best[found]

    def route(s, iters=30):
        blk = s.obstacles()
        # Reihenfolge: je Ziel zuerst das Hauptband (längste Luftlinie), die anderen münden per Y hinein
        trunk = {}
        for id, L in s.ins.items():
            trunk[id] = max(L, key=lambda l: abs(s.start[l['id']][0] - s.goal[id][0]) + abs(s.start[l['id']][1] - s.goal[id][1]))['id']
        nets = sorted(s.belts, key=lambda l: 0 if trunk[l['to']] == l['id'] else 1)
        hist = {}; pf = .6; paths = {}; halos = {}; s.merge = {}
        for it in range(iters):
            for l in nets:
                lid = l['id']; halos.pop(lid, None); D = l['to']
                use = {}
                for k_, h_ in halos.items():
                    for c in h_: use[c] = use.get(c, 0) + 1
                tp = paths.get(trunk[D]) if trunk[D] != lid else None
                mz = set()
                goals = {}
                if trunk[D] == lid:
                    g = s.goal[D]; goals[(g[0], g[1])] = (g[2], None)
                elif tp:
                    for gi, (gx, gy, gd, m, sd) in enumerate(s.merge_spots(tp)):
                        if not blk[idx(gx, gy)]: goals[(gx, gy)] = (gd, (m, sd))
                    # um das Y herum darf der Zweig nah am Hauptband laufen
                def cost_at(x, y, use=use):
                    u = use.get((x, y), 0)
                    return hist.get((x, y), 0) + (pf * u * 6 if u else 0)
                sx, sy, sd = s.start[lid]
                if not goals: paths[lid] = None; s.merge.pop(lid, None); continue
                nr_ = MINRUN + (len(s.ins[D]) - 1) * (ML + 2) + 2 if (trunk[D] == lid and len(s.ins[D]) > 1) else MINRUN
                p, c = s.astar(sx, sy, sd, {k: v[0] for k, v in goals.items()}, blk, cost_at, {(sx, sy)}, mz_trunk=tp, need_run=nr_)
                if p is None: paths[lid] = None; s.merge.pop(lid, None); continue
                paths[lid] = p; e = p[-1]; mg = goals[e][1]
                if mg: s.merge[lid] = (lid_trunk := trunk[D], mg[0], mg[1])
                else: s.merge.pop(lid, None)
                halos[lid] = s.halo(p)
                if mg: halos[lid] |= s.halo(s.connector(p[-1], mg[0]))
            conf = s.conflicts(paths)
            if not conf: s.paths = paths; return paths, 0, it + 1
            for c in conf: hist[c] = hist.get(c, 0) + 1.5
            pf *= 1.5
        s.paths = paths
        return paths, len(s.conflicts(paths)), iters

    def route_seq(s, tries=30):
        """Nacheinander verlegen; fertige Bänder (mit 1 m Abstandszone) sind harte Hindernisse. Scheitert ein Band, beim nächsten Versuch nach vorn."""
        blk0 = s.obstacles()
        trunk = {}
        for id, L in s.ins.items():
            trunk[id] = max(L, key=lambda l: abs(s.start[l['id']][0] - s.goal[id][0]) + abs(s.start[l['id']][1] - s.goal[id][1]))['id']
        s.trunk = trunk
        def dist(l): g = s.goal[l['to']]; st = s.start[l['id']]; return abs(st[0] - g[0]) + abs(st[1] - g[1])
        order = sorted(s.belts, key=lambda l: (0 if trunk[l['to']] == l['id'] else 1, dist(l)))
        best = None; rng = random.Random(7)
        def turns(p): return sum(1 for i in range(1, len(p) - 1) if (p[i][0] - p[i-1][0], p[i][1] - p[i-1][1]) != (p[i+1][0] - p[i][0], p[i+1][1] - p[i][1]))
        for t in range(tries):
            blk = bytearray(blk0); paths = {}; merge = {}; failed = []; ownr = {}
            for l in order:
                lid = l['id']; D = l['to']; goals = {}
                if trunk[D] == lid:
                    g = s.goal[D]; goals[(g[0], g[1])] = (g[2], None)
                elif paths.get(trunk[D]):
                    for (gx, gy, gd, m, sd) in s.merge_spots(paths[trunk[D]]):
                        if not blk[idx(gx, gy)] and all((not blk[idx(*c)]) or ownr.get(c) == {trunk[D]} for c in s.connector((gx, gy), m)): goals[(gx, gy)] = (gd, (m, sd))
                sx, sy, sd = s.start[lid]
                if not goals: failed.append(l); continue
                nr_ = MINRUN + (len(s.ins[D]) - 1) * (ML + 2) + 2 if (trunk[D] == lid and len(s.ins[D]) > 1) else MINRUN
                p, c = s.astar(sx, sy, sd, {k: v[0] for k, v in goals.items()}, blk, lambda x, y: 0, {(sx, sy)}, need_run=nr_)
                if p is None: failed.append(l); continue
                paths[lid] = p; mg = goals[p[-1]][1]
                cells = list(p)
                if mg: merge[lid] = (trunk[D], mg[0], mg[1]); cells += s.connector(p[-1], mg[0])
                # Abstandszone sperren – das Hauptband bleibt für weitere Y-Stücke erreichbar (Ränder der Y-Plätze)
                for (x, y) in s.halo(cells):
                    if inside(x, y):
                        if not blk[idx(x, y)] or (x, y) in ownr: ownr.setdefault((x, y), set()).add(lid)
                        blk[idx(x, y)] = 1
            score = (len(failed), sum(len(p) + 4 * turns(p) for p in paths.values()))
            if best is None or score < best[0]: best = (score, paths, merge, list(failed))
            if failed:
                order = failed + [l for l in order if l not in failed]
            else:
                rng.shuffle(order)
            # Hauptbänder müssen vor ihren Zweigen liegen
            order = sorted(order, key=lambda l: 0 if trunk[l['to']] == l['id'] else 1)
        s.paths, s.merge = best[1], best[2]
        s.failed = [l['id'] for l in best[3]]
        return s.paths, len(s.failed), t + 1

    def halo(s, path):
        out = set()
        for (x, y) in path:
            for dy in range(-CLR + 1, CLR):
                for dx in range(-CLR + 1, CLR): out.add((x + dx, y + dy))
        return out

    def connector(s, e, m):
        """Schräges Y-Stück vom Zweig-Ende e bis auf das Hauptband bei m (Zellen dazwischen)."""
        n = max(abs(m[0] - e[0]), abs(m[1] - e[1])); return [(round(e[0] + (m[0] - e[0]) * i / n), round(e[1] + (m[1] - e[1]) * i / n)) for i in range(1, n + 1)]

    def merge_spots(s, tp):
        """Y-Plätze auf geraden Stücken des Hauptbandes: Zweig-Ende ML Zellen davor, 2 m seitlich, gleiche Richtung."""
        res = []
        L = len(tp)
        j = L - 1
        while j > 0 and (tp[j][0] - tp[j - 1][0], tp[j][1] - tp[j - 1][1]) == (tp[L - 1][0] - tp[L - 2][0], tp[L - 1][1] - tp[L - 2][1]): j -= 1
        for i in range(max(ML + 2, j + ML + 2), L - 3):
            seg = tp[i - ML - 2:i + 3]
            d0 = (tp[i][0] - tp[i - 1][0], tp[i][1] - tp[i - 1][1])
            if any((seg[k + 1][0] - seg[k][0], seg[k + 1][1] - seg[k][1]) != d0 for k in range(len(seg) - 1)): continue
            k = DX.index(d0); t = (-d0[1], d0[0])
            b = tp[i - ML]
            for sd in (1, -1):
                res.append((b[0] + t[0] * 2 * sd, b[1] + t[1] * 2 * sd, k, tp[i], sd))
        return res

    def conflicts(s, paths):
        own = {}; bad = set()
        for lid, p in paths.items():
            if p is None: bad.add(('none', lid)); continue
            cells = list(p)
            if lid in s.merge: cells += s.connector(p[-1], s.merge[lid][1])
            for c in cells: own.setdefault(c, set()).add(lid)
        for lid, p in paths.items():
            if p is None: continue
            cells = list(p)
            mg = s.merge.get(lid)
            if mg: cells += s.connector(p[-1], mg[1])
            for (x, y) in cells:
                for dy in range(-CLR + 1, CLR):
                    for dx in range(-CLR + 1, CLR):
                        o = own.get((x + dx, y + dy))
                        if not o: continue
                        o = o - {lid}
                        # Zweig und Hauptband dürfen sich am eigenen Y berühren
                        if mg and mg[0] in o and abs(x - mg[1][0]) + abs(y - mg[1][1]) <= ML + 2: o = o - {mg[0]}
                        for oth in list(o):
                            om = s.merge.get(oth)
                            if om and om[0] == lid and abs(x - om[1][0]) + abs(y - om[1][1]) <= ML + 2: o = o - {oth}
                        if o: bad.add((x, y))
        return bad

def load(key='moon'):
    d = json.load(open('' + __import__('os').path.join(__import__('os').path.dirname(__file__), '../../unity/Assets/Resources/Data/gamedata.json') + ''))
    L = d['layouts'][key]; bd = d['bld']
    blds = {}
    for id, p in L['pos'].items():
        if id in ('u2', 'u3'): continue
        blds[id] = Bld(id, int(round(p[0])), int(round(p[1])), half_id(id, bd[id]['fp']))
    belts = []
    for b in L['belts']:
        fr, to = b['from'], b['to']
        if fr in ('u2', 'u3'): continue
        if fr == 'roh' and to == 'sell': to = 'prod'
        belts.append({'id': b['id'], 'from': fr, 'to': to})
    R = max(math.hypot(b.x, b.y) + b.hs for b in blds.values()) + 8
    return blds, belts, L, R
